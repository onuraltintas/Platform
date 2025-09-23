import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule, Eye, EyeOff, User, Lock, Mail, AlertCircle } from 'lucide-angular';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { GoogleAuthService } from '../../../../core/auth/services/google-auth.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { LoginRequest } from '../../../../core/auth/models/auth.models';
import { ToastService } from '../../../../core/bildirimler/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    LucideAngularModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly googleAuthService = inject(GoogleAuthService);
  private readonly router = inject(Router);
  private readonly loadingService = inject(LoadingService);
  private readonly toast = inject(ToastService);


  loginForm!: FormGroup;
  showPassword = signal(false);
  isLoading = signal(false);
  showResendVerification = signal(false);
  resendEmail = signal('');
  resendCooldown = signal(0);
  private resendTimer?: ReturnType<typeof setInterval>;

  // Lucide icons
  readonly Eye = Eye;
  readonly EyeOff = EyeOff;
  readonly User = User;
  readonly Lock = Lock;
  readonly Mail = Mail;
  readonly AlertCircle = AlertCircle;

  // Access environment in template
  get environment() {
    return (window as any).__env || { features: { enableGoogleAuth: true } };
  }

  ngOnInit(): void {
    this.createForm();
    this.loadingService.isLoading$.subscribe(loading => {
      this.isLoading.set(loading);
    });
  }

  private createForm(): void {
    this.loginForm = this.fb.group({
      emailOrUsername: ['', [Validators.required]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });
  }

  onSubmit(): void {
    if (!this.loginForm.value.emailOrUsername || !this.loginForm.value.password) {
      this.toast.uyari('Lütfen tüm alanları doldurun');
      return;
    }

    if (this.loginForm.valid && !this.isLoading()) {
      const loginRequest: LoginRequest = {
        email: this.loginForm.value.emailOrUsername,
        password: this.loginForm.value.password,
        rememberMe: this.loginForm.value.rememberMe
      };

      this.isLoading.set(true);

      this.authService.login(loginRequest).subscribe({
        next: (response) => {
          this.toast.basari(`Hoş geldiniz, ${response.user.firstName}!`);

          // Redirect to intended page or dashboard
          const redirectUrl = sessionStorage.getItem('redirectUrl') || '/admin';
          sessionStorage.removeItem('redirectUrl');

          console.log('Yönlendirme URL:', redirectUrl);

          // Small delay to ensure auth state is updated
          setTimeout(() => {
            this.router.navigate([redirectUrl]).then(success => {
              console.log('Navigation success:', success);
              if (!success) {
                console.error('Navigation failed, trying alternative route');
                this.router.navigate(['/admin']);
              }
            }).catch(err => {
              console.error('Navigation error:', err);
            });
          }, 100);
        },
        error: (error) => {
          console.error('Login error:', error);

          // Handle specific error cases
          if (error.error && typeof error.error === 'string' && (error.error.includes('EMAIL_NOT_VERIFIED') || error.error.includes('E-posta adresinizi doğrulamanız gerekiyor'))) {
            // Parse the error message to extract email
            let email = this.loginForm.value.emailOrUsername;

            // If the error contains the email address (format: EMAIL_NOT_VERIFIED:email@domain.com)
            if (error.error.includes('EMAIL_NOT_VERIFIED:')) {
              const emailPart = error.error.split('EMAIL_NOT_VERIFIED:')[1];
              if (emailPart && emailPart.includes('@')) {
                email = emailPart;
              }
            }

            // If user entered username (not email), use the email from error response
            if (!this.isEmailFormat(this.loginForm.value.emailOrUsername) && email.includes('@')) {
              this.resendEmail.set(email);
            } else {
              this.resendEmail.set(this.loginForm.value.emailOrUsername);
            }

            this.showResendVerification.set(true);
            this.toast.uyari('E-posta adresiniz doğrulanmamış. Lütfen gelen kutunuzu kontrol edin.');
          } else if (error.error?.error?.code === 'AUTH_EMAIL_NOT_CONFIRMED') {
            const email = this.loginForm.value.emailOrUsername;
            this.resendEmail.set(email);
            this.showResendVerification.set(true);
            this.toast.uyari('E-posta adresinizi doğrulayın. Doğrulama e-postasını kontrol edin.');
          } else if (error.status === 401) {
            this.toast.hata('Kullanıcı adı veya şifre hatalı. Lütfen tekrar deneyin.');
          } else if (error.error?.message) {
            this.toast.hata(error.error.message);
          } else {
            this.toast.hata(error.userMessage || 'Giriş yapılırken bir hata oluştu');
          }
        },
        complete: () => {
          this.isLoading.set(false);
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  loginWithGoogle(): void {
    this.googleAuthService.googleLogin();
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(current => !current);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private markFormGroupTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      const control = this.loginForm.get(key);
      control?.markAsTouched();
    });
  }

  resendVerificationEmail(): void {
    if (this.resendCooldown() === 0) {
      // Check if user entered email or username
      const emailOrUsername = this.loginForm.value.emailOrUsername;

      if (!emailOrUsername) {
        this.toast.uyari('Lütfen kullanıcı adı veya e-posta adresinizi girin.');
        return;
      }

      let emailToSend = '';

      // If user entered email format, use it directly
      if (this.isEmailFormat(emailOrUsername)) {
        emailToSend = emailOrUsername;
      } else {
        // If user entered username, check if we have email from previous error
        if (this.resendEmail() && this.isEmailFormat(this.resendEmail())) {
          emailToSend = this.resendEmail();
        } else {
          this.toast.uyari('Doğrulama e-postası göndermek için e-posta adresinizi girin.');
          return;
        }
      }

      this.isLoading.set(true);

      this.authService.resendVerificationEmail(emailToSend).subscribe({
        next: () => {
          this.startResendCooldown();
          this.toast.basari('Doğrulama e-postası tekrar gönderildi. Lütfen gelen kutunuzu kontrol edin.');
          this.showResendVerification.set(false);
        },
        error: (error) => {
          console.error('Resend verification error:', error);
          this.toast.hata('E-posta gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.');
        },
        complete: () => {
          this.isLoading.set(false);
        }
      });
    }
  }

  private startResendCooldown(): void {
    this.resendCooldown.set(60); // 60 seconds cooldown

    this.resendTimer = setInterval(() => {
      const current = this.resendCooldown();
      if (current > 0) {
        this.resendCooldown.set(current - 1);
      } else {
        if (this.resendTimer) {
          clearInterval(this.resendTimer);
        }
      }
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this.resendTimer) {
      clearInterval(this.resendTimer);
    }
  }

  private isEmailFormat(value: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(value);
  }
}