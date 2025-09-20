import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { CustomValidators } from '../../../../shared/utils/validators';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="card">
      <div class="card-body p-5">
        <div class="text-center mb-4">
          <div class="mb-3">
            <i class="fas fa-key text-primary" style="font-size: 3rem;"></i>
          </div>
          <h2 class="text-primary mb-2">Şifre Sıfırlama</h2>
          <p class="text-muted">
            E-posta adresinizi girin, şifre sıfırlama bağlantısını gönderelim
          </p>
        </div>

        <div *ngIf="!emailSent(); else successMessage">
          <form [formGroup]="forgotPasswordForm" (ngSubmit)="onSubmit()" novalidate>
            <!-- Email Field -->
            <div class="mb-4">
              <label for="email" class="form-label">
                <i class="fas fa-envelope me-2"></i>
                E-posta Adresi
              </label>
              <input
                type="email"
                id="email"
                class="form-control form-control-lg"
                [class.is-invalid]="isFieldInvalid('email')"
                formControlName="email"
                placeholder="ornek@platformv1.com"
                autocomplete="username"
                [disabled]="isLoading()"
              />
              <div class="invalid-feedback" *ngIf="isFieldInvalid('email')">
                <small *ngIf="forgotPasswordForm.get('email')?.errors?.['required']">
                  E-posta adresi gereklidir
                </small>
                <small *ngIf="forgotPasswordForm.get('email')?.errors?.['email']">
                  Geçerli bir e-posta adresi giriniz
                </small>
              </div>
            </div>

            <!-- Submit Button -->
            <div class="d-grid mb-4">
              <button
                type="submit"
                class="btn btn-primary btn-lg"
                [disabled]="forgotPasswordForm.invalid || isLoading()"
              >
                <span *ngIf="isLoading()" class="spinner-border spinner-border-sm me-2"></span>
                <i *ngIf="!isLoading()" class="fas fa-paper-plane me-2"></i>
                {{ isLoading() ? 'Gönderiliyor...' : 'Sıfırlama Bağlantısı Gönder' }}
              </button>
            </div>
          </form>
        </div>

        <ng-template #successMessage>
          <div class="text-center">
            <div class="mb-4">
              <i class="fas fa-check-circle text-success" style="font-size: 4rem;"></i>
            </div>
            <h3 class="text-success mb-3">E-posta Gönderildi!</h3>
            <p class="text-muted mb-4">
              <strong>{{ sentEmail() }}</strong> adresine şifre sıfırlama bağlantısı gönderildi.
              E-posta gelmediyse spam klasörünüzü kontrol edin.
            </p>
            <div class="d-grid mb-3">
              <button
                class="btn btn-outline-primary"
                (click)="resendEmail()"
                [disabled]="resendCooldown() > 0"
              >
                <i class="fas fa-redo me-2"></i>
                {{ resendCooldown() > 0 ? 'Tekrar gönder (' + resendCooldown() + 's)' : 'Tekrar Gönder' }}
              </button>
            </div>
          </div>
        </ng-template>

        <!-- Back to Login -->
        <div class="text-center">
          <a routerLink="/auth/login" class="text-decoration-none">
            <i class="fas fa-arrow-left me-2"></i>
            Giriş sayfasına dön
          </a>
        </div>

        <!-- Help Section -->
        <div class="mt-4 p-3 bg-light rounded">
          <h6 class="text-muted mb-2">
            <i class="fas fa-question-circle me-2"></i>
            Yardım
          </h6>
          <ul class="list-unstyled mb-0 small text-muted">
            <li class="mb-1">• E-posta gelmiyorsa spam klasörünüzü kontrol edin</li>
            <li class="mb-1">• Bağlantı 24 saat geçerlidir</li>
            <li class="mb-1">• Sorun yaşıyorsanız destek ekibimizle iletişime geçin</li>
          </ul>
        </div>
      </div>

      <!-- Loading Overlay -->
      <div *ngIf="isLoading()" class="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center bg-white bg-opacity-75">
        <div class="text-center">
          <div class="spinner-border text-primary" role="status"></div>
          <div class="mt-2 text-muted">E-posta gönderiliyor...</div>
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

    .btn-outline-primary {
      border: 2px solid #0d6efd;
      color: #0d6efd;
      font-weight: 600;
    }

    .btn-outline-primary:hover:not(:disabled) {
      background: #0d6efd;
      transform: translateY(-2px);
      box-shadow: 0 5px 15px rgba(13, 110, 253, 0.3);
    }

    .text-primary {
      background: linear-gradient(135deg, #0d6efd, #6610f2);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
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
export class ForgotPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly loadingService = inject(LoadingService);
  private readonly toastr = inject(ToastrService);

  forgotPasswordForm!: FormGroup;
  isLoading = signal(false);
  emailSent = signal(false);
  sentEmail = signal('');
  resendCooldown = signal(0);

  private resendTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.createForm();
    this.loadingService.isLoading$.subscribe(loading => {
      this.isLoading.set(loading);
    });
  }

  ngOnDestroy(): void {
    if (this.resendTimer) {
      clearInterval(this.resendTimer);
    }
  }

  private createForm(): void {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, CustomValidators.email]]
    });
  }

  onSubmit(): void {
    if (!this.forgotPasswordForm.value.email) {
      this.toastr.warning('Lütfen e-posta adresinizi girin', 'Uyarı');
      return;
    }

    if (this.forgotPasswordForm.valid && !this.isLoading()) {
      const email = this.forgotPasswordForm.value.email;
      this.sendResetEmail(email);
    } else {
      this.markFormGroupTouched();
    }
  }

  resendEmail(): void {
    if (this.resendCooldown() === 0) {
      this.sendResetEmail(this.sentEmail());
    }
  }

  private sendResetEmail(email: string): void {
    this.isLoading.set(true);

    this.authService.requestPasswordReset(email).subscribe({
      next: () => {
        this.sentEmail.set(email);
        this.emailSent.set(true);
        this.startResendCooldown();
        this.toastr.success(
          'Şifre sıfırlama bağlantısı e-posta adresinize gönderildi',
          'E-posta Gönderildi'
        );
      },
      error: (error) => {
        console.error('Forgot password error:', error);

        // Don't reveal if email exists or not for security reasons
        if (error.status === 404) {
          this.sentEmail.set(email);
          this.emailSent.set(true);
          this.startResendCooldown();
          this.toastr.success(
            'Eğer bu e-posta adresine kayıtlı bir hesap varsa, şifre sıfırlama bağlantısı gönderilecektir',
            'İşlem Tamamlandı'
          );
        } else {
          this.toastr.error(
            error.userMessage || 'Şifre sıfırlama e-postası gönderilirken bir hata oluştu',
            'Hata'
          );
        }
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
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

  isFieldInvalid(fieldName: string): boolean {
    const field = this.forgotPasswordForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private markFormGroupTouched(): void {
    Object.keys(this.forgotPasswordForm.controls).forEach(key => {
      const control = this.forgotPasswordForm.get(key);
      control?.markAsTouched();
    });
  }
}