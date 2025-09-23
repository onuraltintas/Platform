import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, Eye, EyeOff, Lock, CheckCircle, Shield } from 'lucide-angular';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { LoadingService } from '../../../../shared/services/loading.service';
import { CustomValidators, getPasswordStrength } from '../../../../shared/utils/validators';
import { ResetPasswordRequest } from '../../../../core/auth/models/auth.models';
import { ToastService } from '../../../../core/bildirimler/toast.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    LucideAngularModule
  ],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
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

  // Lucide icons
  readonly Eye = Eye;
  readonly EyeOff = EyeOff;
  readonly Lock = Lock;
  readonly CheckCircle = CheckCircle;
  readonly Shield = Shield;

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
        this.toast.hata('Şifre sıfırlama bağlantısı geçersiz');
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
          this.toast.basari('Şifreniz başarıyla güncellendi');
        },
        error: (error) => {
          console.error('Reset password error:', error);

          if (error.status === 400 || error.status === 404) {
            this.tokenValid.set(false);
            this.toast.hata('Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş');
          } else {
            this.toast.hata(error.userMessage || 'Şifre güncellenirken bir hata oluştu');
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