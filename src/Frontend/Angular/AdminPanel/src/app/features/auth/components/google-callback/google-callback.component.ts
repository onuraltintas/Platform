import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
// Removed unused HttpClient and environment import

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
  styles: [`
    :host {
      display: block;
      height: 100vh;
      background-color: #f8f9fa;
    }
  `]
})
export class GoogleCallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly toastr = inject(ToastrService);

  ngOnInit(): void {
    this.route.queryParams.subscribe(async params => {
      const { success, token, state, error } = params;

      if (error) {
        console.error('Google OAuth error:', error);
        this.toastr.error(`Google ile giriş başarısız: ${error}`, 'Hata');
        this.router.navigate(['/auth/login']);
        return;
      }

      if (success === 'true' && token) {
        try {
          console.log('Processing Google OAuth callback with token');

          // Decode the token from URL
          const decodedToken = atob(decodeURIComponent(token));
          const tokenData = JSON.parse(decodedToken);

          console.log('Received token data:', tokenData);

          // Handle successful authentication
          this.authService.handleGoogleLoginSuccess(tokenData);

          this.toastr.success('Google ile giriş başarılı!', 'Başarılı');

          // Parse state to get redirect URL
          let redirectUrl = '/dashboard';
          if (state) {
            try {
              const stateData = JSON.parse(atob(decodeURIComponent(state)));
              redirectUrl = stateData.redirectUrl || '/dashboard';
            } catch {
              console.warn('Could not parse state, using default redirect');
            }
          }

          this.router.navigate([redirectUrl]);

        } catch (error) {
          console.error('Error processing Google OAuth token:', error);
          this.toastr.error('Kimlik doğrulama sırasında hata oluştu', 'Hata');
          this.router.navigate(['/auth/login']);
        }
      } else {
        console.error('Missing required parameters in callback:', params);
        this.toastr.error('Geçersiz Google geri dönüşü', 'Hata');
        this.router.navigate(['/auth/login']);
      }
    });
  }
}