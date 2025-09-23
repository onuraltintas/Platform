import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-modern-footer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <footer class="footer footer-transparent d-print-none">
      <div class="container-xl">
        <div class="row text-center align-items-center flex-row-reverse">
          <div class="col-lg-auto ms-lg-auto">
            <ul class="list-inline list-inline-dots mb-0">
              <li class="list-inline-item">
                <a routerLink="/terms" class="link-secondary">Kullanım Şartları</a>
              </li>
              <li class="list-inline-item">
                <a routerLink="/privacy" class="link-secondary">Gizlilik Politikası</a>
              </li>
              <li class="list-inline-item">
                <a href="#" class="link-secondary">Destek</a>
              </li>
            </ul>
          </div>
          <div class="col-12 col-lg-auto mt-3 mt-lg-0">
            <ul class="list-inline list-inline-dots mb-0">
              <li class="list-inline-item">
                &copy; {{ currentYear }} <strong>OnAl Platform</strong>. Tüm hakları saklıdır.
              </li>
              <li class="list-inline-item">
                <a href="#" class="link-secondary" rel="noopener">v1.0.0</a>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    .footer {
      border-top: 1px solid var(--bs-border-color);
      background: var(--bs-header-bg);
      padding: 1rem 0;
      margin-top: auto;
    }

    .footer-transparent {
      background: transparent;
    }

    .list-inline-dots .list-inline-item:not(:last-child)::after {
      content: "·";
      margin: 0 0.5rem;
      color: var(--bs-secondary);
    }

    .link-secondary {
      color: var(--bs-secondary);
      text-decoration: none;
      transition: color 0.15s ease;
    }

    .link-secondary:hover {
      color: var(--bs-primary);
    }

    @media (max-width: 991.98px) {
      .footer {
        padding: 1rem;
        font-size: 0.875rem;
      }
    }
  `]
})
export class ModernFooterComponent {
  currentYear = new Date().getFullYear();
}