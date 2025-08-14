import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router'; // RouterModule import edildi
import { AuthService } from '../../../data-access/auth.service';
import { ToastrService } from 'ngx-toastr';
import { ForgotPasswordRequest, PasswordResetResponse } from '../../../models/auth.models';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule], // RouterModule imports'a eklendi
  templateUrl: './forgot-password.component.html',
  styleUrls: [
    '../login/login.component.scss', // Login stillerini kullan
    './forgot-password.component.scss'
  ]
})
export class ForgotPasswordComponent {
  forgotPasswordRequest: ForgotPasswordRequest = {
    email: '',
  };

  isLoading = false;
  emailSent = false;

  constructor(private authService: AuthService, private toastr: ToastrService) { }

  onSubmit(): void {
    if (!this.forgotPasswordRequest.email) {
      this.toastr.warning('Lütfen email adresinizi girin', 'Uyarı');
      return;
    }

    this.isLoading = true;

    this.authService.forgotPassword(this.forgotPasswordRequest).subscribe({
      next: (response: PasswordResetResponse) => {
        this.isLoading = false;
        this.emailSent = true;
        this.toastr.success('Eğer e-posta adresiniz sistemde kayıtlıysa, şifre sıfırlama talimatları gönderildi.', 'Talep Alındı');
      },
      error: (error: any) => {
        this.isLoading = false;
        console.error('Forgot password error:', error);
        
        if (error.error?.message) {
          this.toastr.error(error.error.message, 'Hata');
        } else {
          this.toastr.error('Şifre sıfırlama talebi başarısız oldu.', 'Hata');
        }
      }
    });
  }
}
