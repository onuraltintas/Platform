import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="home-container">
      <header class="header">
        <div class="container">
          <div class="logo">
            <h1>SpeedRead</h1>
          </div>
          <nav class="nav">
            <a routerLink="/auth/login" class="btn-outline">Giriş Yap</a>
            <a routerLink="/auth/register" class="btn-primary">Üye Ol</a>
          </nav>
        </div>
      </header>

      <main class="main">
        <section class="hero">
          <div class="container">
            <div class="hero-content">
              <h1 class="hero-title">
                Okuma Hızınızı 
                <span class="highlight">3 Katına</span> 
                Çıkarın
              </h1>
              <p class="hero-description">
                Bilimsel yöntemlerle desteklenen egzersizler ve AI destekli kişiselleştirme ile 
                okuma hızınızı artırın, kavrama yetinizi geliştirin.
              </p>
              <div class="hero-actions">
                <button (click)="startLearning()" class="btn-hero">
                  Eğitime Başla
                  <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6"></path>
                  </svg>
                </button>
                <a routerLink="/auth/login" class="btn-secondary">Zaten üyeyim</a>
              </div>
            </div>
            <div class="hero-visual">
              <div class="speed-indicator">
                <div class="speed-meter">
                  <div class="speed-number">450</div>
                  <div class="speed-label">Kelime/Dakika</div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section class="features">
          <div class="container">
            <h2 class="section-title">Neler Sunuyoruz?</h2>
            <div class="features-grid">
              <div class="feature-card">
                <div class="feature-icon">📚</div>
                <h3>Kişiselleştirilmiş Egzersizler</h3>
                <p>AI destekli sistem ile seviyenize uygun egzersizler</p>
              </div>
              <div class="feature-card">
                <div class="feature-icon">📊</div>
                <h3>Detaylı İlerleme Takibi</h3>
                <p>Okuma hızınız ve kavrama oranınızı görsel grafiklerle takip edin</p>
              </div>
              <div class="feature-card">
                <div class="feature-icon">🎯</div>
                <h3>Odaklanma Teknikleri</h3>
                <p>Dikkat dağınıklığını azaltacak özel egzersizler</p>
              </div>
              <div class="feature-card">
                <div class="feature-icon">⚡</div>
                <h3>RSVP Okuma Modu</h3>
                <p>Hızlı seriyal görsel sunum ile okuma hızınızı artırın</p>
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer class="footer">
        <div class="container">
          <p>&copy; 2024 SpeedRead Platform. Tüm hakları saklıdır.</p>
        </div>
      </footer>
    </div>
  `,
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  startLearning(): void {
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    } else {
      this.router.navigate(['/auth/register']);
    }
  }
}