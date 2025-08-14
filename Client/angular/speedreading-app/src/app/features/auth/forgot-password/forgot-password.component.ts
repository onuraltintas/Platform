import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ForgotPasswordRequest } from '../../../shared/models/auth.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-container">
      <div class="auth-wrapper">
        <!-- Sol Panel: Marka Kimliği -->
        <div class="auth-brand-panel">
          <div class="brand-content">
            <div class="brand-logo">
              <i class="bi bi-key"></i>
            </div>
            <h1 class="brand-title">Şifre Sıfırlama</h1>
            <p class="brand-tagline">Hesabınıza Tekrar Erişin</p>
          </div>
        </div>

        <!-- Sağ Panel: Şifre Sıfırlama Formu -->
        <div class="auth-form-panel">
          <div class="form-content">
            <div class="form-header">
              <h2>Şifremi Unuttum</h2>
              <p>E-posta adresinizi girin, size şifre sıfırlama bağlantısı gönderelim.</p>
            </div>

            <form (ngSubmit)="onSubmit()">
              <div class="input-group mb-4">
                <span class="input-group-text"><i class="bi bi-envelope"></i></span>
                <div class="form-floating">
                  <input
                    type="email"
                    class="form-control"
                    id="email"
                    placeholder="E-posta Adresiniz"
                    name="email"
                    [(ngModel)]="forgotPasswordRequest.email"
                    required
                  />
                  <label for="email">E-posta Adresiniz</label>
                </div>
              </div>

              <div class="d-grid mb-3">
                <button 
                  type="submit" 
                  class="btn btn-primary btn-lg"
                  [disabled]="isLoading">
                  <span *ngIf="isLoading" class="spinner-border spinner-border-sm me-2"></span>
                  {{ isLoading ? 'Gönderiliyor...' : 'Şifre Sıfırlama Bağlantısı Gönder' }}
                </button>
              </div>
            </form>

            <div class="text-center mt-4">
              <p class="text-muted">
                Şifrenizi hatırladınız mı? 
                <a [routerLink]="['/auth/login']" class="text-decoration-none">
                  Giriş Yapın
                </a>
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  forgotPasswordRequest: ForgotPasswordRequest = {
    email: ''
  };

  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  onSubmit(): void {
    if (!this.forgotPasswordRequest.email) {
      this.toastr.warning('Lütfen e-posta adresinizi girin', 'Uyarı');
      return;
    }

    if (!this.isValidEmail(this.forgotPasswordRequest.email)) {
      this.toastr.warning('Lütfen geçerli bir e-posta adresi girin', 'Uyarı');
      return;
    }

    this.isLoading = true;

    this.authService.forgotPassword(this.forgotPasswordRequest).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.toastr.success('Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.', 'Başarılı');
          this.router.navigate(['/auth/login']);
        } else {
          this.toastr.error(response.message || 'İşlem başarısız', 'Hata');
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.toastr.error(error.error?.message || 'Bağlantı hatası oluştu', 'Hata');
      }
    });
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
}