import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../data-access/auth.service';
import { LoginRequest } from '../../../models/auth.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  loginRequest: LoginRequest = {
    emailOrUsername: '',
    password: '',
    rememberMe: false
  };

  isLoading = false;
  passwordVisible = false;
  unconfirmedEmailWarning = false;
  resending = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    // Clear logout flag when login page loads
    this.authService.clearLogoutFlag();
  }

  onLogin(): void {
    if (!this.loginRequest.emailOrUsername || !this.loginRequest.password) {
      this.toastr.warning('Lütfen tüm alanları doldurun', 'Uyarı');
      return;
    }

    this.isLoading = true;

    this.authService.login(this.loginRequest).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.toastr.success('Giriş başarılı!', 'Başarılı');
          this.router.navigate(['/dashboard']);
        } else {
          this.toastr.error(response.error?.message || 'Giriş başarısız', 'Hata');
        }
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Login error:', error);
        
        if (error.error?.error?.code === 'AUTH_EMAIL_NOT_CONFIRMED') {
          this.unconfirmedEmailWarning = true;
          this.toastr.warning('E-posta adresinizi doğrulayın. Aşağıdaki düğmeyle doğrulama e-postasını tekrar gönderebilirsiniz.', 'E-posta Doğrulama Gerekli');
        } else {
          this.toastr.error(error.error?.error?.message || 'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.', 'Hata');
        }
      }
    });
  }

  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
  }

  googleLogin(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    console.log('Google Login clicked');
    this.authService.googleLogin();
  }

  // Google ile kayıt için ayrı butona gerek yok; backend otomatik kayıt eder

  resendConfirmationEmail(): void {
    const emailCandidate = this.loginRequest.emailOrUsername?.includes('@') ? this.loginRequest.emailOrUsername : '';
    if (!emailCandidate) {
      this.toastr.info('Lütfen e-posta adresinizi kullanıcı adı yerine e-posta olarak girip tekrar deneyin.', 'Bilgi');
      return;
    }
    this.resending = true;
    this.authService.resendEmailConfirmation(emailCandidate).subscribe({
      next: () => {
        this.resending = false;
        this.toastr.success('Eğer e-posta adresiniz sistemde kayıtlı ve doğrulanmamışsa, doğrulama e-postası gönderildi.', 'Gönderildi');
      },
      error: () => {
        this.resending = false;
        this.toastr.error('İşlem sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.', 'Hata');
      }
    });
  }
}