import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, Mail, ArrowLeft, Send, CheckCircle } from 'lucide-angular';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { CustomValidators } from '../../../../shared/utils/validators';
import { ToastService } from '../../../../core/bildirimler/toast.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    LucideAngularModule
  ],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly loadingService = inject(LoadingService);
  private readonly toast = inject(ToastService);

  forgotPasswordForm!: FormGroup;
  isLoading = signal(false);
  emailSent = signal(false);
  sentEmail = signal('');
  resendCooldown = signal(0);

  private resendTimer?: ReturnType<typeof setInterval>;

  // Lucide icons
  readonly Mail = Mail;
  readonly ArrowLeft = ArrowLeft;
  readonly Send = Send;
  readonly CheckCircle = CheckCircle;

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
      this.toast.uyari('Lütfen e-posta adresinizi girin');
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
        this.toast.basari('Şifre sıfırlama bağlantısı e-posta adresinize gönderildi');
      },
      error: (error) => {
        console.error('Forgot password error:', error);

        // Don't reveal if email exists or not for security reasons
        if (error.status === 404) {
          this.sentEmail.set(email);
          this.emailSent.set(true);
          this.startResendCooldown();
          this.toast.basari('Eğer bu e-posta adresine kayıtlı bir hesap varsa, şifre sıfırlama bağlantısı gönderilecektir');
        } else {
          this.toast.hata(error.userMessage || 'Şifre sıfırlama e-postası gönderilirken bir hata oluştu');
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