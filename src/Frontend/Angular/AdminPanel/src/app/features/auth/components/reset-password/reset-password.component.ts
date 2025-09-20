import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { CustomValidators, getPasswordStrength } from '../../../../shared/utils/validators';
import { ResetPasswordRequest } from '../../../../core/auth/models/auth.models';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="card">
      <div class="card-body p-5">
        <div class="text-center mb-4">
          <div class="mb-3">
            <i class="fas fa-lock text-primary" style="font-size: 3rem;"></i>
          </div>
          <h2 class="text-primary mb-2">Şifre Sıfırlama</h2>
          <p class="text-muted">
            Yeni şifrenizi belirleyin
          </p>
        </div>

        <div *ngIf="!passwordReset(); else successMessage">
          <div *ngIf="!tokenValid(); else resetForm" class="text-center">
            <div class="mb-4">
              <i class="fas fa-exclamation-triangle text-warning" style="font-size: 3rem;"></i>
            </div>
            <h3 class="text-warning mb-3">Geçersiz Token</h3>
            <p class="text-muted mb-4">
              Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş. Lütfen yeni bir bağlantı talep edin.
            </p>
            <div class="d-grid">
              <a routerLink="/auth/forgot-password" class="btn btn-primary">
                <i class="fas fa-redo me-2"></i>
                Yeni Bağlantı Talep Et
              </a>
            </div>
          </div>

          <ng-template #resetForm>
            <form [formGroup]="resetPasswordForm" (ngSubmit)="onSubmit()" novalidate>
              <!-- Email Display -->
              <div class="mb-3">
                <label class="form-label text-muted">
                  <i class="fas fa-envelope me-2"></i>
                  E-posta Adresi
                </label>
                <div class="form-control-plaintext fw-bold">{{ userEmail() }}</div>
              </div>

              <!-- New Password Field -->
              <div class="mb-3">
                <label for="newPassword" class="form-label">
                  <i class="fas fa-lock me-2"></i>
                  Yeni Şifre
                </label>
                <div class="input-group">
                  <input
                    [type]="showPassword() ? 'text' : 'password'"
                    id="newPassword"
                    class="form-control form-control-lg"
                    [class.is-invalid]="isFieldInvalid('newPassword')"
                    formControlName="newPassword"
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
                <div class="mt-2" *ngIf="resetPasswordForm.get('newPassword')?.value">
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

                <div class="invalid-feedback" *ngIf="isFieldInvalid('newPassword')">
                  <small *ngIf="resetPasswordForm.get('newPassword')?.errors?.['required']">
                    Yeni şifre gereklidir
                  </small>
                  <div *ngIf="resetPasswordForm.get('newPassword')?.errors?.['password']">
                    <small class="d-block" *ngIf="resetPasswordForm.get('newPassword')?.errors?.['password']?.['minLength']">
                      Şifre en az 8 karakter olmalıdır
                    </small>
                    <small class="d-block" *ngIf="resetPasswordForm.get('newPassword')?.errors?.['password']?.['requiresNumber']">
                      Şifre en az bir rakam içermelidir
                    </small>
                    <small class="d-block" *ngIf="resetPasswordForm.get('newPassword')?.errors?.['password']?.['requiresUppercase']">
                      Şifre en az bir büyük harf içermelidir
                    </small>
                    <small class="d-block" *ngIf="resetPasswordForm.get('newPassword')?.errors?.['password']?.['requiresLowercase']">
                      Şifre en az bir küçük harf içermelidir
                    </small>
                    <small class="d-block" *ngIf="resetPasswordForm.get('newPassword')?.errors?.['password']?.['requiresSpecial']">
                      Şifre en az bir özel karakter içermelidir
                    </small>
                  </div>
                </div>
              </div>

              <!-- Confirm Password Field -->
              <div class="mb-4">
                <label for="confirmPassword" class="form-label">
                  <i class="fas fa-lock me-2"></i>
                  Şifre Tekrarı
                </label>
                <div class="input-group">
                  <input
                    [type]="showConfirmPassword() ? 'text' : 'password'"
                    id="confirmPassword"
                    class="form-control form-control-lg"
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
                  <small *ngIf="resetPasswordForm.get('confirmPassword')?.errors?.['required']">
                    Şifre tekrarı gereklidir
                  </small>
                  <small *ngIf="resetPasswordForm.get('confirmPassword')?.errors?.['passwordMismatch']">
                    Şifreler eşleşmiyor
                  </small>
                </div>
              </div>

              <!-- Submit Button -->
              <div class="d-grid mb-3">
                <button
                  type="submit"
                  class="btn btn-primary btn-lg"
                  [disabled]="resetPasswordForm.invalid || isLoading()"
                >
                  <span *ngIf="isLoading()" class="spinner-border spinner-border-sm me-2"></span>
                  <i *ngIf="!isLoading()" class="fas fa-save me-2"></i>
                  {{ isLoading() ? 'Şifre güncelleniyor...' : 'Şifreyi Güncelle' }}
                </button>
              </div>
            </form>
          </ng-template>
        </div>

        <ng-template #successMessage>
          <div class="text-center">
            <div class="mb-4">
              <i class="fas fa-check-circle text-success" style="font-size: 4rem;"></i>
            </div>
            <h3 class="text-success mb-3">Şifre Başarıyla Güncellendi!</h3>
            <p class="text-muted mb-4">
              Şifreniz başarıyla güncellendi. Artık yeni şifrenizle giriş yapabilirsiniz.
            </p>
            <div class="d-grid">
              <a routerLink="/auth/login" class="btn btn-primary btn-lg">
                <i class="fas fa-sign-in-alt me-2"></i>
                Giriş Yap
              </a>
            </div>
          </div>
        </ng-template>

        <!-- Back to Login -->
        <div class="text-center" *ngIf="!passwordReset()">
          <a routerLink="/auth/login" class="text-decoration-none">
            <i class="fas fa-arrow-left me-2"></i>
            Giriş sayfasına dön
          </a>
        </div>

        <!-- Security Tips -->
        <div class="mt-4 p-3 bg-light rounded" *ngIf="tokenValid() && !passwordReset()">
          <h6 class="text-muted mb-2">
            <i class="fas fa-shield-alt me-2"></i>
            Güvenlik İpuçları
          </h6>
          <ul class="list-unstyled mb-0 small text-muted">
            <li class="mb-1">• En az 8 karakter uzunluğunda olsun</li>
            <li class="mb-1">• Büyük harf, küçük harf, rakam ve özel karakter içersin</li>
            <li class="mb-1">• Kişisel bilgilerinizi içermesin</li>
            <li>• Diğer hesaplarınızda kullandığınız şifrelerden farklı olsun</li>
          </ul>
        </div>
      </div>

      <!-- Loading Overlay -->
      <div *ngIf="isLoading()" class="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center bg-white bg-opacity-75">
        <div class="text-center">
          <div class="spinner-border text-primary" role="status"></div>
          <div class="mt-2 text-muted">Şifre güncelleniyor...</div>
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
      max-width: 500px;
      margin: 0 auto;
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

    .bg-light {
      background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%) !important;
    }

    a {
      transition: all 0.3s ease;
    }

    a:hover {
      transform: translateX(3px);
    }

    .invalid-feedback {
      display: block;
    }

    .fa-check-circle {
      animation: bounceIn 0.6s ease-out;
    }

    @keyframes bounceIn {
      0% { transform: scale(0.3); opacity: 0; }
      50% { transform: scale(1.05); }
      70% { transform: scale(0.9); }
      100% { transform: scale(1); opacity: 1; }
    }

    @media (max-width: 576px) {
      .card-body {
        padding: 2rem !important;
      }
    }
  `]
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly notificationService = inject(NotificationService);
  private readonly loadingService = inject(LoadingService);

  resetPasswordForm!: FormGroup;
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  isLoading = signal(false);
  passwordReset = signal(false);
  tokenValid = signal(false);
  userEmail = signal('');
  passwordStrengthValue = signal('');

  private resetToken = '';

  passwordStrength = computed(() => {
    return getPasswordStrength(this.passwordStrengthValue());
  });

  ngOnInit(): void {
    this.createForm();
    this.checkResetToken();
    this.loadingService.isLoading$.subscribe(loading => {
      this.isLoading.set(loading);
    });
  }

  private createForm(): void {
    this.resetPasswordForm = this.fb.group({
      newPassword: ['', [Validators.required, CustomValidators.password]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: [CustomValidators.passwordMatch('newPassword', 'confirmPassword')]
    });
  }

  private checkResetToken(): void {
    this.route.queryParams.subscribe(params => {
      const token = params['token'];
      const email = params['email'];

      if (token && email) {
        this.resetToken = token;
        this.userEmail.set(email);
        this.tokenValid.set(true);
      } else {
        this.tokenValid.set(false);
        this.notificationService.error(
          'Şifre sıfırlama bağlantısı geçersiz',
          'Geçersiz Bağlantı'
        );
      }
    });
  }

  onSubmit(): void {
    if (this.resetPasswordForm.valid && !this.isLoading()) {
      const resetRequest: ResetPasswordRequest = {
        email: this.userEmail(),
        token: this.resetToken,
        newPassword: this.resetPasswordForm.value.newPassword
      };

      this.isLoading.set(true);

      this.authService.resetPassword(resetRequest).subscribe({
        next: () => {
          this.passwordReset.set(true);
          this.notificationService.success(
            'Şifreniz başarıyla güncellendi',
            'İşlem Başarılı'
          );
        },
        error: (error) => {
          console.error('Reset password error:', error);

          if (error.status === 400 || error.status === 404) {
            this.tokenValid.set(false);
            this.notificationService.error(
              'Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş',
              'Geçersiz Token'
            );
          } else {
            this.notificationService.error(
              error.userMessage || 'Şifre güncellenirken bir hata oluştu',
              'Hata'
            );
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

  togglePasswordVisibility(): void {
    this.showPassword.update(current => !current);
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update(current => !current);
  }

  onPasswordChange(): void {
    const password = this.resetPasswordForm.get('newPassword')?.value || '';
    this.passwordStrengthValue.set(password);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.resetPasswordForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private markFormGroupTouched(): void {
    Object.keys(this.resetPasswordForm.controls).forEach(key => {
      const control = this.resetPasswordForm.get(key);
      control?.markAsTouched();
    });
  }
}