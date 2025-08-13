import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './confirm-email.component.html',
  styleUrls: ['./confirm-email.component.scss']
})
export class ConfirmEmailComponent implements OnInit {
  isLoading = true;
  isSuccess = false;
  errorMessage: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    const userId = this.route.snapshot.queryParamMap.get('userId');

    if (!token || !userId) {
      this.errorMessage = 'Geçersiz doğrulama linki. Lütfen linki kontrol edin veya yeni bir doğrulama e-postası talep edin.';
      this.isLoading = false;
      return;
    }

    this.authService
      .confirmEmail({ token, userId })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.isSuccess = true;
            this.toastr.success('E-posta adresiniz başarıyla doğrulandı! Şimdi giriş yapabilirsiniz.', 'Başarılı');
            setTimeout(() => this.router.navigate(['/auth/login']), 3000);
          } else {
            this.errorMessage = response.error?.message || 'E-posta doğrulaması başarısız oldu.';
          }
        },
        error: (err) => {
          this.errorMessage = err?.error?.error?.message || 'E-posta doğrulaması sırasında bir hata oluştu.';
        }
      });
  }
}
