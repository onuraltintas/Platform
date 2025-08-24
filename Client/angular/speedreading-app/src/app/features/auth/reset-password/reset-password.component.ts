import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../core/services/auth.service';
import { ResetPasswordRequest } from '../../../shared/models/auth.models';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mt-5">
      <div class="row justify-content-center">
        <div class="col-md-8 col-lg-6">
          <div class="card shadow-sm">
            <div class="card-body p-4">
              <h4 class="mb-3">Şifreyi Sıfırla</h4>
              <form (ngSubmit)="onSubmit()">
                <div class="mb-3">
                  <label class="form-label">Yeni Şifre</label>
                  <div class="input-group">
                    <input [type]="passwordVisible ? 'text' : 'password'" class="form-control" [(ngModel)]="model.newPassword" name="newPassword" required />
                    <button type="button" class="btn btn-outline-secondary" (click)="toggle('password')">
                      <i class="bi" [class.bi-eye]="!passwordVisible" [class.bi-eye-slash]="passwordVisible"></i>
                    </button>
                  </div>
                </div>
                <div class="mb-3">
                  <label class="form-label">Şifre Tekrar</label>
                  <div class="input-group">
                    <input [type]="confirmPasswordVisible ? 'text' : 'password'" class="form-control" [(ngModel)]="model.confirmPassword" name="confirmPassword" required />
                    <button type="button" class="btn btn-outline-secondary" (click)="toggle('confirm')">
                      <i class="bi" [class.bi-eye]="!confirmPasswordVisible" [class.bi-eye-slash]="confirmPasswordVisible"></i>
                    </button>
                  </div>
                </div>
                <div class="d-grid">
                  <button class="btn btn-success" type="submit">Şifreyi Sıfırla</button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: []
})
export class ResetPasswordComponent implements OnInit {
  model: ResetPasswordRequest = { email: '', token: '', newPassword: '', confirmPassword: '' };
  passwordVisible = false;
  confirmPasswordVisible = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.model.email = params['email'] || '';
      const rawToken = params['token'] || '';
      let decoded = '';
      try { decoded = decodeURIComponent(rawToken as string); } catch { decoded = rawToken as string; }
      this.model.token = (decoded as string).replace(/ /g, '+');
    });
  }

  get passwordsMatch(): boolean {
    return (this.model.newPassword || '') === (this.model.confirmPassword || '');
  }

  toggle(field: 'password' | 'confirm'): void {
    if (field === 'password') this.passwordVisible = !this.passwordVisible;
    else this.confirmPasswordVisible = !this.confirmPasswordVisible;
  }

  onSubmit(): void {
    if (!this.passwordsMatch) {
      this.toastr.error('Şifre ve şifre tekrarı eşleşmiyor.', 'Doğrulama Hatası');
      return;
    }
    this.authService.resetPassword(this.model).subscribe({
      next: () => {
        this.toastr.success('Şifreniz başarıyla sıfırlandı. Lütfen giriş yapın.', 'Başarılı');
        this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.toastr.error(err?.error?.message || 'Şifre sıfırlama sırasında bir hata oluştu.', 'Hata');
      }
    });
  }
}

