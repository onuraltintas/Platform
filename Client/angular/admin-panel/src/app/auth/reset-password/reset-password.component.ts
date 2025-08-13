import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { ResetPasswordRequest, PasswordResetResponse } from '../../models/auth.models';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss']
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordRequest: ResetPasswordRequest = {
    email: '',
    token: '',
    newPassword: '',
    confirmPassword: '',
  };

  passwordVisible = false;
  confirmPasswordVisible = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.resetPasswordRequest.email = params['email'] || '';
      const rawToken = params['token'] || '';
      // 1) URL decode (backend linkte token urlencode edildi)
      let decoded = '';
      try { decoded = decodeURIComponent(rawToken as string); } catch { decoded = rawToken as string; }
      // 2) Bazı istemciler + işaretini boşluk yapabilir; boşlukları geri + yap
      this.resetPasswordRequest.token = (decoded as string).replace(/ /g, '+');
    });
  }

  // Checklist flags
  get hasMinLen(): boolean { return (this.resetPasswordRequest.newPassword || '').length >= 8; }
  get hasUpper(): boolean { return /[A-Z]/.test(this.resetPasswordRequest.newPassword || ''); }
  get hasDigit(): boolean { return /\d/.test(this.resetPasswordRequest.newPassword || ''); }
  get hasSpecial(): boolean { return /[^\da-zA-Z]/.test(this.resetPasswordRequest.newPassword || ''); }

  get passwordMeetsPolicy(): boolean {
    return this.hasMinLen && this.hasUpper && this.hasDigit && this.hasSpecial;
  }

  get passwordsMatch(): boolean {
    return this.resetPasswordRequest.newPassword === this.resetPasswordRequest.confirmPassword;
  }

  toggle(field: 'password' | 'confirm'): void {
    if (field === 'password') this.passwordVisible = !this.passwordVisible;
    else this.confirmPasswordVisible = !this.confirmPasswordVisible;
  }

  onSubmit(): void {
    if (!this.passwordMeetsPolicy) {
      this.toastr.error('Şifre en az 8 karakter olmalı; bir büyük harf, bir rakam ve bir özel karakter içermelidir.', 'Doğrulama Hatası');
      return;
    }
    if (!this.passwordsMatch) {
      this.toastr.error('Şifre ve şifre tekrarı eşleşmiyor.', 'Doğrulama Hatası');
      return;
    }

    this.authService.resetPassword(this.resetPasswordRequest).subscribe({
      next: (response: PasswordResetResponse) => {
        this.toastr.success('Şifreniz başarıyla sıfırlandı. Lütfen giriş yapın.', 'Başarılı');
        this.router.navigate(['/auth/login']);
      },
      error: (error: any) => {
        const body = error?.error;
        if (body?.errors) {
          const msgs = Object.values(body.errors as Record<string, string[]>).flat();
          const friendly = msgs
            .map(m => m.includes('one uppercase letter')
              ? 'Şifre en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.'
              : m)
            .join('\n');
          this.toastr.error(friendly || 'Şifre sıfırlama doğrulama hatası.', 'Doğrulama Hatası');
        } else if (body?.error?.message) {
          this.toastr.error(body.error.message, 'Hata');
        } else {
          this.toastr.error('Şifre sıfırlama sırasında bir hata oluştu. Lütfen bilgilerinizi kontrol edin.', 'Hata');
        }
        console.error('Reset password error:', error);
      }
    });
  }
}