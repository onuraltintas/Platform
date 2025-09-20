import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <footer class="app-footer">
      <div class="footer-content">
        <div class="footer-left">
          <span class="text-muted">
            © {{ currentYear }} PlatformV1. Tüm hakları saklıdır.
          </span>
        </div>

        <div class="footer-right">
          <div class="footer-links">
            <a href="#" class="footer-link">Gizlilik Politikası</a>
            <span class="footer-separator">|</span>
            <a href="#" class="footer-link">Kullanım Şartları</a>
            <span class="footer-separator">|</span>
            <a href="#" class="footer-link">Destek</a>
          </div>

          <div class="footer-version">
            <small class="text-muted">v{{ version }}</small>
          </div>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    .app-footer {
      background: var(--bs-header-bg, #ffffff);
      border-top: 1px solid var(--bs-border-color, #dee2e6);
      padding: 0.75rem 1rem;
      margin-top: auto;
      flex-shrink: 0;
    }

    .footer-content {
      display: flex;
      align-items: center;
      justify-content: space-between;
      max-width: 100%;
    }

    .footer-left {
      flex: 1;
    }

    .footer-right {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .footer-links {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .footer-link {
      color: var(--bs-nav-link-color, #6c757d);
      text-decoration: none;
      font-size: 0.875rem;
      transition: color 0.3s ease;
    }

    .footer-link:hover {
      color: var(--bs-nav-link-hover-color, #0d6efd);
    }

    .footer-separator {
      color: var(--bs-border-color, #dee2e6);
      font-size: 0.875rem;
    }

    .footer-version {
      font-size: 0.75rem;
    }

    /* Mobile Responsive */
    @media (max-width: 768px) {
      .footer-content {
        flex-direction: column;
        gap: 0.5rem;
        text-align: center;
      }

      .footer-right {
        flex-direction: column;
        gap: 0.5rem;
      }

      .footer-links {
        flex-wrap: wrap;
        justify-content: center;
      }
    }

    @media (max-width: 576px) {
      .app-footer {
        padding: 0.5rem;
      }

      .footer-links {
        flex-direction: column;
        gap: 0.25rem;
      }

      .footer-separator {
        display: none;
      }
    }

    /* Dark Theme */
    .dark-theme .app-footer {
      background: var(--bs-header-bg, #2d3339);
      border-top-color: var(--bs-border-color, #495057);
    }

    .dark-theme .footer-separator {
      color: var(--bs-border-color, #495057);
    }
  `]
})
export class FooterComponent {
  currentYear = new Date().getFullYear();
  version = '1.0.0';
}