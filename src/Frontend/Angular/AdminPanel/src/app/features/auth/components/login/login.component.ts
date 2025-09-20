import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { GoogleAuthService } from '../../../../core/auth/services/google-auth.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { LoginRequest } from '../../../../core/auth/models/auth.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-wrapper">
        <!-- Sol Panel: Marka Kimliği -->
        <div class="auth-brand-panel">
          <div class="brand-content">
            <div class="brand-logo">
              <i class="bi bi-shield-lock"></i>
            </div>
            <h1 class="brand-title">OnAl Yazılım Otomasyon</h1>
            <p class="brand-tagline">İşinizi Geleceğe Taşıyın</p>
          </div>
        </div>

        <!-- Sağ Panel: Giriş Formu -->
        <div class="auth-form-panel">
          <div class="form-content">
            <div class="form-header">
              <h2>Yönetici Girişi</h2>
              <p>Lütfen devam etmek için giriş yapın.</p>
            </div>

            <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" novalidate>
              <!-- Email Field -->
              <div class="input-group mb-3">
                <span class="input-group-text"><i class="bi bi-person"></i></span>
                <div class="form-floating">
                  <input
                    type="text"
                    id="emailOrUsername"
                    class="form-control"
                    [class.is-invalid]="isFieldInvalid('emailOrUsername')"
                    formControlName="emailOrUsername"
                    placeholder="Kullanıcı Adı veya E-posta"
                    autocomplete="username"
                  />
                  <label for="emailOrUsername">Kullanıcı Adı veya E-posta</label>
                </div>
              </div>
              <div class="invalid-feedback d-block mb-3" *ngIf="isFieldInvalid('emailOrUsername')">
                <small *ngIf="loginForm.get('emailOrUsername')?.errors?.['required']">
                  Kullanıcı adı veya e-posta adresi gereklidir
                </small>
              </div>

              <!-- Password Field -->
              <div class="input-group mb-3">
                <span class="input-group-text"><i class="bi bi-lock"></i></span>
                <div class="form-floating">
                  <input
                    [type]="showPassword() ? 'text' : 'password'"
                    id="password"
                    class="form-control"
                    [class.is-invalid]="isFieldInvalid('password')"
                    formControlName="password"
                    placeholder="Şifre"
                    autocomplete="current-password"
                  />
                  <label for="password">Şifre</label>
                </div>
                <button
                  type="button"
                  class="btn btn-outline-secondary"
                  (click)="togglePasswordVisibility()"
                  tabindex="-1">
                  <i [class]="showPassword() ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
                </button>
              </div>
              <div class="invalid-feedback d-block mb-3" *ngIf="isFieldInvalid('password')">
                <small *ngIf="loginForm.get('password')?.errors?.['required']">
                  Şifre gereklidir
                </small>
              </div>

              <!-- Remember Me & Forgot Password -->
              <div class="d-flex justify-content-between align-items-center mb-4">
                <div class="form-check">
                  <input
                    type="checkbox"
                    class="form-check-input"
                    id="rememberMe"
                    formControlName="rememberMe"
                  />
                  <label class="form-check-label" for="rememberMe">Beni Hatırla</label>
                </div>
                <a routerLink="/auth/forgot-password" class="text-decoration-none">Şifremi Unuttum?</a>
              </div>

              <!-- Submit Button -->
              <div class="d-grid mb-3">
                <button
                  type="submit"
                  class="btn btn-primary btn-lg"
                  [disabled]="loginForm.invalid || isLoading()"
                >
                  <span *ngIf="isLoading()" class="spinner-border spinner-border-sm me-2"></span>
                  {{ isLoading() ? 'Giriş Yapılıyor...' : 'Giriş Yap' }}
                </button>
              </div>
            </form>

            <!-- Divider -->
            <div class="or-separator">
              <span>veya</span>
            </div>

            <!-- Google Login -->
            <div class="d-grid mb-3" *ngIf="environment.features.enableGoogleAuth">
              <button
                type="button"
                class="btn btn-google"
                (click)="loginWithGoogle()"
                [disabled]="isLoading()"
              >
                <i class="bi bi-google"></i> Google ile Devam Et
              </button>
            </div>

            <!-- Register Link -->
            <div class="text-center">
              <p class="text-muted">
                Hesabınız yok mu?
                <a routerLink="/auth/register" class="text-decoration-none">
                  Kayıt Ol
                </a>
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    /* Genel Kapsayıcı ve Hizalama */
    :host {
      display: flex;
      justify-content: center;
      align-items: center;
      height: 100vh;
      background-color: #f4f7f6;
    }

    .auth-container {
      width: 100%;
      height: 100%;
      display: flex;
      justify-content: center;
      align-items: center;
    }

    .auth-wrapper {
      display: flex;
      width: 100%;
      max-width: 1200px;
      height: 80vh;
      min-height: 600px;
      max-height: 700px;
      box-shadow: 0 20px 50px rgba(0, 0, 0, 0.15);
      border-radius: 20px;
      overflow: hidden;
      background-color: #fff;
    }

    /* Sol Panel: Marka Kimliği */
    .auth-brand-panel {
      width: 50%;
      background: linear-gradient(135deg, #007bff, #0056b3);
      color: #fff;
      display: flex;
      justify-content: center;
      align-items: center;
      text-align: center;
      padding: 40px;
    }

    .brand-content .brand-logo {
      font-size: 60px;
      margin-bottom: 20px;
    }

    .brand-content .brand-logo i {
      text-shadow: 0 4px 15px rgba(0, 0, 0, 0.2);
    }

    .brand-content .brand-title {
      font-size: 2.5rem;
      font-weight: 700;
      margin-bottom: 10px;
    }

    .brand-content .brand-tagline {
      font-size: 1.1rem;
      color: rgba(255, 255, 255, 0.85);
    }

    /* Sağ Panel: Giriş Formu */
    .auth-form-panel {
      width: 50%;
      padding: 50px;
      display: flex;
      justify-content: center;
      align-items: center;
      flex-direction: column;
    }

    .form-content {
      width: 100%;
      max-width: 400px;
    }

    .form-header {
      text-align: center;
      margin-bottom: 30px;
    }

    .form-header h2 {
      font-weight: 600;
      color: #333;
    }

    .form-header p {
      color: #777;
    }

    /* Form Elemanları - Bootstrap Override ile Düzeltilmiş */
    .input-group {
      position: relative;
    }

    .input-group .input-group-text {
      background-color: #f8f9fa;
      border: 1px solid #ced4da !important;
      border-right: none !important;
      padding: 0.375rem 0.75rem;
    }

    .input-group .input-group-text i {
      color: #6c757d;
      font-size: 1.4rem;
    }

    .input-group .form-floating {
      flex: 1;
    }

    /* Bootstrap'in form-floating border'ını override et */
    .input-group > .form-floating > .form-control {
      border: 1px solid #ced4da !important;
      border-left: none !important;
      height: calc(3.5rem + 2px);
    }

    /* Focus durumunda tek border */
    .input-group .form-floating .form-control:focus {
      box-shadow: none !important;
      border: 1px solid #007bff !important;
      border-left: none !important;
      outline: 0 !important;
    }

    /* Hata durumu için kırmızı border */
    .input-group .form-floating .form-control.is-invalid {
      border-color: #dc3545 !important;
      border-left: none !important;
      background-image: none !important;
    }

    .input-group .form-floating .form-control.is-invalid:focus {
      border-color: #dc3545 !important;
      border-left: none !important;
      box-shadow: none !important;
    }

    /* Focus durumunda grup elemanları */
    .input-group:focus-within .input-group-text {
      border-color: #007bff !important;
      background-color: #f0f7ff;
    }

    .input-group:focus-within .input-group-text i {
      color: #007bff;
    }

    .input-group:focus-within .btn-outline-secondary {
      border-color: #007bff !important;
      border-left: none !important;
    }

    /* Hata durumunda grup elemanları */
    .input-group:has(.is-invalid) .input-group-text {
      border-color: #dc3545 !important;
    }

    .input-group:has(.is-invalid) .btn-outline-secondary {
      border-color: #dc3545 !important;
      border-left: none !important;
    }

    .input-group .btn-outline-secondary {
      border: 1px solid #ced4da !important;
      border-left: none !important;
      background-color: #f8f9fa;
    }

    .input-group .btn-outline-secondary:hover {
      background-color: #e9ecef;
    }

    .input-group .btn-outline-secondary:focus {
      box-shadow: none !important;
    }

    .form-floating > .form-control:focus ~ label,
    .form-floating > .form-control:not(:placeholder-shown) ~ label {
      color: #007bff;
    }

    .form-floating > .form-control.is-invalid ~ label {
      color: #dc3545;
    }

    /* Bootstrap'in varsayılan floating label border'ını kaldır */
    .form-floating > label {
      border: none !important;
    }

    .form-check-label {
      font-size: 0.9rem;
    }

    a {
      font-size: 0.9rem;
      color: #007bff;
    }

    a:hover {
      text-decoration: underline !important;
    }

    .btn-primary {
      padding: 12px;
      font-weight: 600;
      border-radius: 8px;
      transition: background-color 0.3s ease;
    }

    .btn-primary:hover {
      background-color: #0056b3;
    }

    .or-separator {
      text-align: center;
      margin: 20px 0;
      color: #ccc;
      display: flex;
      align-items: center;
    }

    .or-separator::before,
    .or-separator::after {
      content: '';
      flex-grow: 1;
      height: 1px;
      background-color: #eee;
    }

    .or-separator span {
      padding: 0 15px;
    }

    .btn-google {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 12px;
      font-weight: 600;
      border-radius: 8px;
      background-color: #fff;
      border: 1px solid #ddd;
      color: #555;
      transition: all 0.3s ease;
      font-size: 0.9rem;
    }

    .btn-google:hover {
      background-color: #f8f8f8;
      border-color: #ccc;
      box-shadow: 0 2px 5px rgba(0,0,0,0.05);
    }

    .btn-google i {
      color: #DB4437;
    }

    .btn-google-outline {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 12px;
      font-weight: 600;
      border-radius: 8px;
      background-color: transparent;
      border: 2px solid #DB4437;
      color: #DB4437;
      transition: all 0.3s ease;
      font-size: 0.9rem;
    }

    .btn-google-outline:hover {
      background-color: #DB4437;
      color: white;
      box-shadow: 0 2px 5px rgba(219, 68, 55, 0.2);
    }

    .btn-google-outline i {
      color: inherit;
    }
  `]
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly googleAuthService = inject(GoogleAuthService);
  private readonly router = inject(Router);
  private readonly loadingService = inject(LoadingService);
  private readonly toastr = inject(ToastrService);


  loginForm!: FormGroup;
  showPassword = signal(false);
  isLoading = signal(false);

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
      this.toastr.warning('Lütfen tüm alanları doldurun', 'Uyarı');
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
          this.toastr.success(`Hoş geldiniz, ${response.user.firstName}!`, 'Giriş Başarılı');

          // Redirect to intended page or dashboard
          const redirectUrl = sessionStorage.getItem('redirectUrl') || '/dashboard';
          sessionStorage.removeItem('redirectUrl');
          this.router.navigate([redirectUrl]);
        },
        error: (error) => {
          console.error('Login error:', error);

          // Handle specific error cases
          if (error.error?.error?.code === 'AUTH_EMAIL_NOT_CONFIRMED') {
            this.toastr.warning('E-posta adresinizi doğrulayın. Doğrulama e-postasını kontrol edin.', 'E-posta Doğrulama Gerekli');
          } else if (error.status === 401) {
            this.toastr.error('Kullanıcı adı veya şifre hatalı. Lütfen tekrar deneyin.', 'Giriş Başarısız');
          } else if (error.error?.message) {
            this.toastr.error(error.error.message, 'Hata');
          } else {
            this.toastr.error(error.userMessage || 'Giriş yapılırken bir hata oluştu', 'Giriş Hatası');
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
}