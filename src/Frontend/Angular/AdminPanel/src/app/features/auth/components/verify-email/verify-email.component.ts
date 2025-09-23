import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, CheckCircle, AlertTriangle, Info, Mail, ArrowLeft, Loader2, HelpCircle } from 'lucide-angular';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { ToastService } from '../../../../core/bildirimler/toast.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    LucideAngularModule
  ],
  templateUrl: './verify-email.component.html',
  styleUrl: './verify-email.component.scss'
})
export class VerifyEmailComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly loadingService = inject(LoadingService);

  isLoading = signal(true);
  verificationStatus = signal<'success' | 'error' | 'already-verified' | 'no-token'>('no-token');
  resendCooldown = signal(0);

  private resendTimer?: ReturnType<typeof setInterval>;

  // Lucide icons
  readonly CheckCircle = CheckCircle;
  readonly AlertTriangle = AlertTriangle;
  readonly Info = Info;
  readonly Mail = Mail;
  readonly ArrowLeft = ArrowLeft;
  readonly Loader2 = Loader2;
  readonly HelpCircle = HelpCircle;

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
        this.toast.basari('E-posta adresiniz başarıyla doğrulandı');
      },
      error: (error) => {
        console.error('Email verification error:', error);

        if (error.status === 409) {
          // Email already verified
          this.verificationStatus.set('already-verified');
          this.toast.bilgi('Bu e-posta adresi zaten doğrulanmış');
        } else {
          this.verificationStatus.set('error');
          this.toast.hata(error.userMessage || 'E-posta doğrulanırken bir hata oluştu');
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
          this.toast.basari('Doğrulama e-postası tekrar gönderildi');
        },
        error: (error) => {
          console.error('Resend verification error:', error);
          this.toast.hata(error.userMessage || 'E-posta gönderilirken bir hata oluştu');
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