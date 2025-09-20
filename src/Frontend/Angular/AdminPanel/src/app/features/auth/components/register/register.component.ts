import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { CustomValidators, getPasswordStrength } from '../../../../shared/utils/validators';
import { RegisterRequest } from '../../../../core/auth/models/auth.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="card">
      <div class="card-body p-5">
        <div class="text-center mb-4">
          <h2 class="text-primary mb-2">
            <i class="fas fa-user-plus me-2"></i>
            Kayıt Ol
          </h2>
          <p class="text-muted">PlatformV1 hesabınızı oluşturun</p>
        </div>

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" novalidate>
          <!-- Name Fields Row -->
          <div class="row">
            <div class="col-md-6 mb-3">
              <label for="firstName" class="form-label">
                <i class="fas fa-user me-2"></i>
                Ad
              </label>
              <input
                type="text"
                id="firstName"
                class="form-control"
                [class.is-invalid]="isFieldInvalid('firstName')"
                formControlName="firstName"
                placeholder="Adınız"
                autocomplete="given-name"
              />
              <div class="invalid-feedback" *ngIf="isFieldInvalid('firstName')">
                <small *ngIf="registerForm.get('firstName')?.errors?.['required']">
                  Ad gereklidir
                </small>
                <small *ngIf="registerForm.get('firstName')?.errors?.['minlength']">
                  Ad en az 2 karakter olmalıdır
                </small>
              </div>
            </div>

            <div class="col-md-6 mb-3">
              <label for="lastName" class="form-label">
                <i class="fas fa-user me-2"></i>
                Soyad
              </label>
              <input
                type="text"
                id="lastName"
                class="form-control"
                [class.is-invalid]="isFieldInvalid('lastName')"
                formControlName="lastName"
                placeholder="Soyadınız"
                autocomplete="family-name"
              />
              <div class="invalid-feedback" *ngIf="isFieldInvalid('lastName')">
                <small *ngIf="registerForm.get('lastName')?.errors?.['required']">
                  Soyad gereklidir
                </small>
                <small *ngIf="registerForm.get('lastName')?.errors?.['minlength']">
                  Soyad en az 2 karakter olmalıdır
                </small>
              </div>
            </div>
          </div>

          <!-- Email Field -->
          <div class="mb-3">
            <label for="email" class="form-label">
              <i class="fas fa-envelope me-2"></i>
              E-posta Adresi
            </label>
            <input
              type="email"
              id="email"
              class="form-control"
              [class.is-invalid]="isFieldInvalid('email')"
              formControlName="email"
              placeholder="ornek@platformv1.com"
              autocomplete="username"
            />
            <div class="invalid-feedback" *ngIf="isFieldInvalid('email')">
              <small *ngIf="registerForm.get('email')?.errors?.['required']">
                E-posta adresi gereklidir
              </small>
              <small *ngIf="registerForm.get('email')?.errors?.['email']">
                Geçerli bir e-posta adresi giriniz
              </small>
            </div>
          </div>

          <!-- Phone Number Field -->
          <div class="mb-3">
            <label for="phoneNumber" class="form-label">
              <i class="fas fa-phone me-2"></i>
              Telefon Numarası <span class="text-muted">(Opsiyonel)</span>
            </label>
            <input
              type="tel"
              id="phoneNumber"
              class="form-control"
              [class.is-invalid]="isFieldInvalid('phoneNumber')"
              formControlName="phoneNumber"
              placeholder="+90 5XX XXX XX XX"
              autocomplete="tel"
            />
            <div class="invalid-feedback" *ngIf="isFieldInvalid('phoneNumber')">
              <small *ngIf="registerForm.get('phoneNumber')?.errors?.['phoneNumber']">
                Geçerli bir telefon numarası giriniz
              </small>
            </div>
          </div>

          <!-- Password Field -->
          <div class="mb-3">
            <label for="password" class="form-label">
              <i class="fas fa-lock me-2"></i>
              Şifre
            </label>
            <div class="input-group">
              <input
                [type]="showPassword() ? 'text' : 'password'"
                id="password"
                class="form-control"
                [class.is-invalid]="isFieldInvalid('password')"
                formControlName="password"
                placeholder="Güçlü bir şifre oluşturun"
                autocomplete="new-password"
                (input)="onPasswordChange()"
              />
              <button
                type="button"
                class="btn btn-outline-secondary"
                (click)="togglePasswordVisibility()"
                tabindex="-1"
              >
                <i [class]="showPassword() ? 'fas fa-eye-slash' : 'fas fa-eye'"></i>
              </button>
            </div>

            <!-- Password Strength Indicator -->
            <div class="mt-2" *ngIf="registerForm.get('password')?.value">
              <div class="d-flex align-items-center">
                <div class="flex-grow-1 me-2">
                  <div class="progress" style="height: 6px;">
                    <div
                      class="progress-bar"
                      [class]="'bg-' + passwordStrength().color"
                      [style.width.%]="(passwordStrength().strength / 6) * 100"
                    ></div>
                  </div>
                </div>
                <small [class]="'text-' + passwordStrength().color + ' fw-bold'">
                  {{ passwordStrength().label }}
                </small>
              </div>
            </div>

            <div class="invalid-feedback" *ngIf="isFieldInvalid('password')">
              <small *ngIf="registerForm.get('password')?.errors?.['required']">
                Şifre gereklidir
              </small>
              <div *ngIf="registerForm.get('password')?.errors?.['password']">
                <small class="d-block" *ngIf="registerForm.get('password')?.errors?.['password']?.['minLength']">
                  Şifre en az 8 karakter olmalıdır
                </small>
                <small class="d-block" *ngIf="registerForm.get('password')?.errors?.['password']?.['requiresNumber']">
                  Şifre en az bir rakam içermelidir
                </small>
                <small class="d-block" *ngIf="registerForm.get('password')?.errors?.['password']?.['requiresUppercase']">
                  Şifre en az bir büyük harf içermelidir
                </small>
                <small class="d-block" *ngIf="registerForm.get('password')?.errors?.['password']?.['requiresLowercase']">
                  Şifre en az bir küçük harf içermelidir
                </small>
                <small class="d-block" *ngIf="registerForm.get('password')?.errors?.['password']?.['requiresSpecial']">
                  Şifre en az bir özel karakter içermelidir
                </small>
              </div>
            </div>
          </div>

          <!-- Confirm Password Field -->
          <div class="mb-3">
            <label for="confirmPassword" class="form-label">
              <i class="fas fa-lock me-2"></i>
              Şifre Tekrarı
            </label>
            <div class="input-group">
              <input
                [type]="showConfirmPassword() ? 'text' : 'password'"
                id="confirmPassword"
                class="form-control"
                [class.is-invalid]="isFieldInvalid('confirmPassword')"
                formControlName="confirmPassword"
                placeholder="Şifrenizi tekrar giriniz"
                autocomplete="new-password"
              />
              <button
                type="button"
                class="btn btn-outline-secondary"
                (click)="toggleConfirmPasswordVisibility()"
                tabindex="-1"
              >
                <i [class]="showConfirmPassword() ? 'fas fa-eye-slash' : 'fas fa-eye'"></i>
              </button>
            </div>
            <div class="invalid-feedback" *ngIf="isFieldInvalid('confirmPassword')">
              <small *ngIf="registerForm.get('confirmPassword')?.errors?.['required']">
                Şifre tekrarı gereklidir
              </small>
              <small *ngIf="registerForm.get('confirmPassword')?.errors?.['passwordMismatch']">
                Şifreler eşleşmiyor
              </small>
            </div>
          </div>

          <!-- Terms and Conditions -->
          <div class="mb-3 form-check">
            <input
              type="checkbox"
              class="form-check-input"
              [class.is-invalid]="isFieldInvalid('acceptTerms')"
              id="acceptTerms"
              formControlName="acceptTerms"
            />
            <label class="form-check-label" for="acceptTerms">
              <a href="#" class="text-primary text-decoration-none">Kullanım Şartları</a> ve
              <a href="#" class="text-primary text-decoration-none">Gizlilik Politikası</a>'nı kabul ediyorum
            </label>
            <div class="invalid-feedback" *ngIf="isFieldInvalid('acceptTerms')">
              <small>Kullanım şartlarını kabul etmelisiniz</small>
            </div>
          </div>

          <!-- Submit Button -->
          <div class="d-grid mb-3">
            <button
              type="submit"
              class="btn btn-primary btn-lg"
              [disabled]="registerForm.invalid || isLoading()"
            >
              <span *ngIf="isLoading()" class="spinner-border spinner-border-sm me-2"></span>
              <i *ngIf="!isLoading()" class="fas fa-user-plus me-2"></i>
              {{ isLoading() ? 'Hesap oluşturuluyor...' : 'Hesap Oluştur' }}
            </button>
          </div>

          <!-- Divider -->
          <hr class="my-4">

          <!-- Google Register -->
          <div class="d-grid mb-3" *ngIf="environment.features.enableGoogleAuth">
            <button
              type="button"
              class="btn btn-outline-danger btn-lg"
              (click)="registerWithGoogle()"
              [disabled]="isLoading()"
            >
              <i class="fab fa-google me-2"></i>
              Google ile Kayıt Ol
            </button>
          </div>
        </form>

        <!-- Login Link -->
        <div class="text-center">
          <p class="mb-0">
            Zaten hesabınız var mı?
            <a routerLink="/auth/login" class="text-primary text-decoration-none fw-bold">
              Giriş Yap
            </a>
          </p>
        </div>
      </div>

      <!-- Loading Overlay -->
      <div *ngIf="isLoading()" class="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center bg-white bg-opacity-75">
        <div class="text-center">
          <div class="spinner-border text-primary" role="status"></div>
          <div class="mt-2 text-muted">Hesap oluşturuluyor...</div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card {
      border: none;
      box-shadow: 0 10px 30px rgba(0,0,0,0.1);
      border-radius: 15px;
      position: relative;
      overflow: hidden;
    }

    .card-body {
      background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
    }

    .form-control {
      border-radius: 10px;
      border: 2px solid #e9ecef;
      transition: all 0.3s ease;
    }

    .form-control:focus {
      border-color: #0d6efd;
      box-shadow: 0 0 0 0.2rem rgba(13, 110, 253, 0.15);
    }

    .btn {
      border-radius: 10px;
      font-weight: 600;
      transition: all 0.3s ease;
    }

    .btn-primary {
      background: linear-gradient(135deg, #0d6efd 0%, #0b5ed7 100%);
      border: none;
    }

    .btn-primary:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 5px 15px rgba(13, 110, 253, 0.3);
    }

    .btn-outline-danger {
      border: 2px solid #dc3545;
      color: #dc3545;
      font-weight: 600;
    }

    .btn-outline-danger:hover:not(:disabled) {
      background: #dc3545;
      transform: translateY(-2px);
      box-shadow: 0 5px 15px rgba(220, 53, 69, 0.3);
    }

    .form-check-input:checked {
      background-color: #0d6efd;
      border-color: #0d6efd;
    }

    .text-primary {
      background: linear-gradient(135deg, #0d6efd, #6610f2);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .progress {
      border-radius: 10px;
      background-color: #e9ecef;
    }

    .progress-bar {
      border-radius: 10px;
      transition: all 0.3s ease;
    }

    .invalid-feedback {
      display: block;
    }

    a {
      transition: all 0.3s ease;
    }

    a:hover {
      transform: translateX(3px);
    }

    @media (max-width: 576px) {
      .card-body {
        padding: 2rem !important;
      }
    }
  `]
})
export class RegisterComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly loadingService = inject(LoadingService);
  private readonly toastr = inject(ToastrService);

  registerForm!: FormGroup;
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  isLoading = signal(false);
  passwordStrengthValue = signal('');

  passwordStrength = computed(() => {
    return getPasswordStrength(this.passwordStrengthValue());
  });

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
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, CustomValidators.email]],
      phoneNumber: ['', [CustomValidators.phoneNumber]],
      password: ['', [Validators.required, CustomValidators.password]],
      confirmPassword: ['', [Validators.required]],
      acceptTerms: [false, [Validators.requiredTrue]]
    }, {
      validators: [CustomValidators.passwordMatch('password', 'confirmPassword')]
    });
  }

  onSubmit(): void {
    if (this.registerForm.valid && !this.isLoading()) {
      const registerRequest: RegisterRequest = {
        email: this.registerForm.value.email,
        password: this.registerForm.value.password,
        firstName: this.registerForm.value.firstName,
        lastName: this.registerForm.value.lastName,
        phoneNumber: this.registerForm.value.phoneNumber || undefined,
        acceptTerms: this.registerForm.value.acceptTerms
      };

      this.isLoading.set(true);

      this.authService.register(registerRequest).subscribe({
        next: () => {
          this.toastr.success(
            'Hesabınız başarıyla oluşturuldu. E-posta adresinizi doğrulamak için gelen kutunuzu kontrol edin.',
            'Kayıt Başarılı'
          );
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
                  this.toastr.error(messages[0], 'Doğrulama Hatası');
                }
              }
            });
          } else if (error.error?.message) {
            // Business logic errors
            const message = error.error.message;
            if (message.toLowerCase().includes('email')) {
              this.toastr.error('Bu e-posta adresi zaten kullanılıyor.', 'Hata');
            } else if (message.toLowerCase().includes('username')) {
              this.toastr.error('Bu kullanıcı adı zaten alınmış.', 'Hata');
            } else {
              this.toastr.error(message, 'Hata');
            }
          } else {
            this.toastr.error(error.userMessage || 'Hesap oluşturulurken bir hata oluştu', 'Kayıt Hatası');
          }
        },
        complete: () => {
          this.isLoading.set(false);
        }
      });
    } else {
      this.markFormGroupTouched();
      this.toastr.warning('Lütfen tüm gerekli alanları doldurun', 'Uyarı');
    }
  }

  registerWithGoogle(): void {
    // TODO: Google OAuth2 implementation
    this.toastr.info('Google ile kayıt özelliği yakında aktif olacak');
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(current => !current);
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update(current => !current);
  }

  onPasswordChange(): void {
    const password = this.registerForm.get('password')?.value || '';
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