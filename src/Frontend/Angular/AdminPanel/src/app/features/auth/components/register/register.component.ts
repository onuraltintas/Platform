import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule, User, Mail, Eye, EyeOff, Lock, UserPlus } from 'lucide-angular';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { CustomValidators, getPasswordStrength } from '../../../../shared/utils/validators';
import { RegisterRequest } from '../../../../core/auth/models/auth.models';
import { ToastService } from '../../../../core/bildirimler/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    LucideAngularModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly loadingService = inject(LoadingService);
  private readonly toast = inject(ToastService);

  registerForm!: FormGroup;
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  isLoading = signal(false);
  passwordStrengthValue = signal('');

  passwordStrength = computed(() => {
    return getPasswordStrength(this.passwordStrengthValue());
  });

  // Lucide icons
  readonly User = User;
  readonly Mail = Mail;
  readonly Eye = Eye;
  readonly EyeOff = EyeOff;
  readonly Lock = Lock;
  readonly UserPlus = UserPlus;

  // Access environment in template
  get environment() {
    return (window as any).__env || { features: { enableGoogleAuth: true } };
  }

  ngOnInit(): void {
    this.createForm();
    this.loadingService.isLoading$.subscribe(loading => {
      this.isLoading.set(loading);
      // Enable/disable form based on loading state
      if (loading) {
        this.registerForm.disable();
      } else {
        this.registerForm.enable();
      }
    });
  }

  private createForm(): void {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
      email: ['', [Validators.required, CustomValidators.email]],
      phoneNumber: ['', [CustomValidators.phoneNumber]],
      password: ['', [Validators.required, CustomValidators.password]],
      confirmPassword: ['', [Validators.required]],
      acceptTerms: [false, [Validators.requiredTrue]],
      acceptPrivacyPolicy: [false, [Validators.requiredTrue]],
      acceptMarketing: [false]
    }, {
      validators: [CustomValidators.passwordMatch('password', 'confirmPassword')]
    });
  }

  onSubmit(): void {
    if (this.registerForm.valid && !this.isLoading()) {
      const registerRequest: RegisterRequest = {
        email: this.registerForm.value.email,
        userName: this.registerForm.value.userName,
        password: this.registerForm.value.password,
        confirmPassword: this.registerForm.value.confirmPassword,
        firstName: this.registerForm.value.firstName,
        lastName: this.registerForm.value.lastName,
        phoneNumber: this.registerForm.value.phoneNumber || undefined,
        acceptTerms: this.registerForm.value.acceptTerms,
        acceptPrivacyPolicy: this.registerForm.value.acceptPrivacyPolicy,
        acceptMarketing: this.registerForm.value.acceptMarketing || false
      };

      this.isLoading.set(true);

      this.authService.register(registerRequest).subscribe({
        next: () => {
          this.toast.basari('Hesabınız başarıyla oluşturuldu. E-posta adresinizi doğrulamak için gelen kutunuzu kontrol edin.');
          this.router.navigate(['/auth/login']);
        },
        error: (error) => {
          console.error('Register error:', error);

          // Handle specific error cases
          if (error.error?.errors) {
            // FluentValidation errors
            const errors = error.error.errors;
            Object.keys(errors).forEach(field => {
              const messages = errors[field];
              if (messages.length > 0) {
                const control = this.registerForm.get(field.toLowerCase());
                if (control) {
                  this.toast.hata(messages[0]);
                }
              }
            });
          } else if (error.error && typeof error.error === 'string') {
            // Handle string error messages from backend
            const message = error.error;
            if (message.toLowerCase().includes('email') && message.toLowerCase().includes('taken')) {
              this.toast.hata('Bu e-posta adresi zaten kullanılıyor. Lütfen farklı bir e-posta adresi deneyin.');
            } else if (message.toLowerCase().includes('username') && message.toLowerCase().includes('taken')) {
              this.toast.hata('Bu kullanıcı adı zaten alınmış. Lütfen farklı bir kullanıcı adı deneyin.');
            } else {
              this.toast.hata(message);
            }
          } else if (error.error?.message) {
            // Business logic errors with message property
            const message = error.error.message;
            if (message.toLowerCase().includes('email') && message.toLowerCase().includes('taken')) {
              this.toast.hata('Bu e-posta adresi zaten kullanılıyor. Lütfen farklı bir e-posta adresi deneyin.');
            } else if (message.toLowerCase().includes('username') && message.toLowerCase().includes('taken')) {
              this.toast.hata('Bu kullanıcı adı zaten alınmış. Lütfen farklı bir kullanıcı adı deneyin.');
            } else {
              this.toast.hata(message);
            }
          } else {
            this.toast.hata(error.userMessage || 'Hesap oluşturulurken bir hata oluştu');
          }
        },
        complete: () => {
          this.isLoading.set(false);
        }
      });
    } else {
      this.markFormGroupTouched();
      this.toast.uyari('Lütfen tüm gerekli alanları doldurun');
    }
  }

  registerWithGoogle(): void {
    // TODO: Google OAuth2 implementation
    this.toast.bilgi('Google ile kayıt özelliği yakında aktif olacak');
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(current => !current);
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update(current => !current);
  }

  onPasswordInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    const password = target.value || '';
    this.passwordStrengthValue.set(password);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private markFormGroupTouched(): void {
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);
      control?.markAsTouched();
    });
  }
}