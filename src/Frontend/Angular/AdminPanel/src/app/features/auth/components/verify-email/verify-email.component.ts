import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { LoadingService } from '../../../../shared/services/loading.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="card">
      <div class="card-body p-5 text-center">
        <div *ngIf="isLoading(); else content">
          <div class="mb-4">
            <div class="spinner-border text-primary" style="width: 4rem; height: 4rem;"></div>
          </div>
          <h3 class="text-primary mb-3">E-posta Doğrulanıyor...</h3>
          <p class="text-muted">Lütfen bekleyiniz, e-posta adresiniz doğrulanıyor.</p>
        </div>

        <ng-template #content>
          <!-- Success State -->
          <div *ngIf="verificationStatus() === 'success'">
            <div class="mb-4">
              <i class="fas fa-check-circle text-success" style="font-size: 4rem;"></i>
            </div>
            <h3 class="text-success mb-3">E-posta Doğrulandı!</h3>
            <p class="text-muted mb-4">
              E-posta adresiniz başarıyla doğrulandı. Artık tüm özellikleri kullanabilirsiniz.
            </p>
            <div class="d-grid">
              <a routerLink="/auth/login" class="btn btn-primary btn-lg">
                <i class="fas fa-sign-in-alt me-2"></i>
                Giriş Yap
              </a>
            </div>
          </div>

          <!-- Error State -->
          <div *ngIf="verificationStatus() === 'error'">
            <div class="mb-4">
              <i class="fas fa-exclamation-triangle text-warning" style="font-size: 4rem;"></i>
            </div>
            <h3 class="text-warning mb-3">Doğrulama Başarısız</h3>
            <p class="text-muted mb-4">
              E-posta doğrulama bağlantısı geçersiz veya süresi dolmuş.
            </p>
            <div class="d-grid mb-3">
              <button
                class="btn btn-primary"
                (click)="resendVerification()"
                [disabled]="resendCooldown() > 0"
              >
                <i class="fas fa-envelope me-2"></i>
                {{ resendCooldown() > 0 ? 'Tekrar gönder (' + resendCooldown() + 's)' : 'Yeni Doğrulama E-postası Gönder' }}
              </button>
            </div>
            <div class="text-center">
              <a routerLink="/auth/login" class="text-decoration-none">
                <i class="fas fa-arrow-left me-2"></i>
                Giriş sayfasına dön
              </a>
            </div>
          </div>

          <!-- Already Verified State -->
          <div *ngIf="verificationStatus() === 'already-verified'">
            <div class="mb-4">
              <i class="fas fa-info-circle text-info" style="font-size: 4rem;"></i>
            </div>
            <h3 class="text-info mb-3">E-posta Zaten Doğrulanmış</h3>
            <p class="text-muted mb-4">
              Bu e-posta adresi daha önce doğrulanmış.
            </p>
            <div class="d-grid">
              <a routerLink="/auth/login" class="btn btn-primary btn-lg">
                <i class="fas fa-sign-in-alt me-2"></i>
                Giriş Yap
              </a>
            </div>
          </div>

          <!-- No Token State -->
          <div *ngIf="verificationStatus() === 'no-token'">
            <div class="mb-4">
              <i class="fas fa-envelope text-primary" style="font-size: 4rem;"></i>
            </div>
            <h3 class="text-primary mb-3">E-posta Doğrulama</h3>
            <p class="text-muted mb-4">
              E-posta doğrulama bağlantısı bulunamadı. Lütfen e-posta kutunuzdaki doğrulama bağlantısını kullanın.
            </p>
            <div class="d-grid mb-3">
              <button
                class="btn btn-primary"
                (click)="resendVerification()"
                [disabled]="resendCooldown() > 0"
              >
                <i class="fas fa-envelope me-2"></i>
                {{ resendCooldown() > 0 ? 'Tekrar gönder (' + resendCooldown() + 's)' : 'Doğrulama E-postası Gönder' }}
              </button>
            </div>
            <div class="text-center">
              <a routerLink="/auth/login" class="text-decoration-none">
                <i class="fas fa-arrow-left me-2"></i>
                Giriş sayfasına dön
              </a>
            </div>
          </div>
        </ng-template>

        <!-- Help Section -->
        <div class="mt-4 p-3 bg-light rounded text-start">
          <h6 class="text-muted mb-2">
            <i class="fas fa-question-circle me-2"></i>
            Yardım
          </h6>
          <ul class="list-unstyled mb-0 small text-muted">
            <li class="mb-1">• E-posta gelmiyorsa spam klasörünüzü kontrol edin</li>
            <li class="mb-1">• Doğrulama bağlantısı 24 saat geçerlidir</li>
            <li class="mb-1">• Sorun yaşıyorsanız destek ekibimizle iletişime geçin</li>
          </ul>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card {
      border: none;
      box-shadow: 0 10px 30px rgba(0,0,0,0.1);
      border-radius: 15px;
      max-width: 500px;
      margin: 0 auto;
    }

    .card-body {
      background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
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

    .bg-light {
      background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%) !important;
    }

    a {
      transition: all 0.3s ease;
    }

    a:hover {
      transform: translateX(3px);
    }

    .fa-check-circle,
    .fa-exclamation-triangle,
    .fa-info-circle,
    .fa-envelope {
      animation: bounceIn 0.6s ease-out;
    }

    @keyframes bounceIn {
      0% { transform: scale(0.3); opacity: 0; }
      50% { transform: scale(1.05); }
      70% { transform: scale(0.9); }
      100% { transform: scale(1); opacity: 1; }
    }

    .spinner-border {
      animation: spin 1s linear infinite;
    }

    @media (max-width: 576px) {
      .card-body {
        padding: 2rem !important;
      }
    }
  `]
})
export class VerifyEmailComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly notificationService = inject(NotificationService);
  private readonly loadingService = inject(LoadingService);

  isLoading = signal(true);
  verificationStatus = signal<'success' | 'error' | 'already-verified' | 'no-token'>('no-token');
  resendCooldown = signal(0);

  private resendTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.checkVerificationToken();
    this.loadingService.isLoading$.subscribe(loading => {
      this.isLoading.set(loading);
    });
  }

  ngOnDestroy(): void {
    if (this.resendTimer) {
      clearInterval(this.resendTimer);
    }
  }

  private checkVerificationToken(): void {
    this.route.queryParams.subscribe(params => {
      const token = params['token'];

      if (token) {
        this.verifyEmail(token);
      } else {
        this.isLoading.set(false);
        this.verificationStatus.set('no-token');
      }
    });
  }

  private verifyEmail(token: string): void {
    this.isLoading.set(true);

    this.authService.verifyEmail(token).subscribe({
      next: () => {
        this.verificationStatus.set('success');
        this.notificationService.success(
          'E-posta adresiniz başarıyla doğrulandı',
          'Doğrulama Başarılı'
        );
      },
      error: (error) => {
        console.error('Email verification error:', error);

        if (error.status === 409) {
          // Email already verified
          this.verificationStatus.set('already-verified');
          this.notificationService.info(
            'Bu e-posta adresi zaten doğrulanmış',
            'Bilgilendirme'
          );
        } else {
          this.verificationStatus.set('error');
          this.notificationService.error(
            error.userMessage || 'E-posta doğrulanırken bir hata oluştu',
            'Doğrulama Hatası'
          );
        }
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  resendVerification(): void {
    if (this.resendCooldown() === 0) {
      this.authService.resendVerificationEmail().subscribe({
        next: () => {
          this.startResendCooldown();
          this.notificationService.success(
            'Doğrulama e-postası tekrar gönderildi',
            'E-posta Gönderildi'
          );
        },
        error: (error) => {
          console.error('Resend verification error:', error);
          this.notificationService.error(
            error.userMessage || 'E-posta gönderilirken bir hata oluştu',
            'Hata'
          );
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
}