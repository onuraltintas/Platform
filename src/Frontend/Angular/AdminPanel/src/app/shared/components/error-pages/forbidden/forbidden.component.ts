import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="error-page">
      <div class="container">
        <div class="row justify-content-center">
          <div class="col-md-6 text-center">
            <div class="error-code">403</div>
            <div class="error-message">
              <h2>Erişim Reddedildi</h2>
              <p class="text-muted">
                Bu sayfaya erişim için gerekli yetkiniz bulunmamaktadır.
              </p>
            </div>
            <div class="error-actions">
              <a routerLink="/dashboard" class="btn btn-primary me-3">
                <i class="fas fa-home me-2"></i>
                Ana Sayfa
              </a>
              <button onclick="history.back()" class="btn btn-outline-secondary">
                <i class="fas fa-arrow-left me-2"></i>
                Geri Dön
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .error-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }

    .error-code {
      font-size: 8rem;
      font-weight: 900;
      color: rgba(255, 255, 255, 0.8);
      line-height: 1;
      margin-bottom: 2rem;
      text-shadow: 0 0 30px rgba(255, 255, 255, 0.5);
    }

    .error-message h2 {
      color: white;
      margin-bottom: 1rem;
      font-weight: 600;
    }

    .error-message p {
      color: rgba(255, 255, 255, 0.8);
      font-size: 1.1rem;
      margin-bottom: 3rem;
    }

    .btn {
      border-radius: 10px;
      padding: 12px 30px;
      font-weight: 600;
    }

    .btn-primary {
      background: rgba(255, 255, 255, 0.2);
      border: 2px solid rgba(255, 255, 255, 0.3);
      color: white;
      backdrop-filter: blur(10px);
    }

    .btn-primary:hover {
      background: rgba(255, 255, 255, 0.3);
      border-color: rgba(255, 255, 255, 0.5);
      transform: translateY(-2px);
    }

    .btn-outline-secondary {
      border: 2px solid rgba(255, 255, 255, 0.3);
      color: white;
      background: transparent;
    }

    .btn-outline-secondary:hover {
      background: rgba(255, 255, 255, 0.1);
      border-color: rgba(255, 255, 255, 0.5);
      color: white;
      transform: translateY(-2px);
    }
  `]
})
export class ForbiddenComponent {}