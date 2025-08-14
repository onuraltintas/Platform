import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../data-access/auth.service';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-google-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="d-flex justify-content-center align-items-center min-vh-100">
      <div class="text-center">
        <div class="spinner-border text-primary mb-3" role="status">
          <span class="visually-hidden">Yükleniyor...</span>
        </div>
        <h4>Google ile giriş yapılıyor...</h4>
        <p class="text-muted">Lütfen bekleyiniz.</p>
      </div>
    </div>
  `,
  styleUrl: './google-callback.component.scss'
})
export class GoogleCallbackComponent implements OnInit {

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.route.queryParams.subscribe(async params => {
      const accessToken = params['token'] || params['accessToken'] || params['access_token'];
      const refreshToken = params['refresh'] || params['refreshToken'] || params['refresh_token'];
      const error = params['message'] || params['error'] || params['error_description'];

      if (accessToken && refreshToken) {
        try {
          // Set tokens first
          const rememberMe = true; // Google akışında kalıcı oturum tercih ediliyor
          this.authService.setTokens(accessToken, refreshToken, rememberMe);
          this.authService.applyAuthenticatedUser(await this.buildUserFromToken(accessToken), rememberMe);
          
          // Get user info from backend using the access token
          await this.getCurrentUser();
          
          this.toastr.success('Google ile giriş başarılı!', 'Başarılı');
          this.router.navigate(['/dashboard']);
        } catch (error) {
          console.error('Error getting user info:', error);
          this.toastr.error('Kullanıcı bilgileri alınırken hata oluştu.', 'Hata');
          this.router.navigate(['/auth/login']);
        }
      } else if (error) {
        this.toastr.error(`Google ile giriş başarısız: ${error}`, 'Hata');
        this.router.navigate(['/auth/login']);
      } else {
        this.toastr.error('Geçersiz Google geri dönüşü.', 'Hata');
        this.router.navigate(['/auth/login']);
      }
    });
  }

  private async getCurrentUser(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.authService.getCurrentUser().subscribe({
        next: (response) => {
          if (response.success && response.data) {
            // AuthService'teki akışa bırakıyoruz; sadece güncelleme amaçlı
            this.authService.applyAuthenticatedUser(response.data, true);
            resolve();
          } else {
            reject('Failed to get user info');
          }
        },
        error: (error) => {
          console.error('Error getting user info:', error);
          // Create fallback user object if API fails
          const fallbackUser = {
            id: 'google-user',
            userName: 'Google User',
            email: 'user@gmail.com',
            fullName: 'Google User'
          };
          this.authService.applyAuthenticatedUser(fallbackUser, true);
          resolve();
        }
      });
    });
  }

  private async buildUserFromToken(accessToken: string): Promise<any> {
    try {
      const payload = JSON.parse(atob(accessToken.split('.')[1]));
      return {
        id: payload.uid || payload.sub,
        userName: payload.username || payload.email,
        email: payload.email,
        firstName: payload.given_name || 'Google',
        lastName: payload.surname || 'User',
        fullName: `${payload.given_name || 'Google'} ${payload.surname || 'User'}`
      };
    } catch {
      return { id: 'google-user', userName: 'Google User', email: 'user@gmail.com', fullName: 'Google User' };
    }
  }
}