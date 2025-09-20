import { Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';

import { User } from '../../../../core/auth/models/auth.models';
import { AuthActions } from '../../../../store/auth/auth.actions';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <header class="app-header">
      <div class="header-content">
        <!-- Left Side - Menu Toggle & Brand -->
        <div class="header-left">
          <button
            type="button"
            class="btn btn-link header-toggle"
            (click)="onToggleSidebar()"
            title="Toggle navigation"
          >
            <i class="bi bi-list"></i>
          </button>

          <div class="header-brand d-none d-lg-block">
            <h5 class="mb-0 text-primary fw-bold">PlatformV1</h5>
          </div>
        </div>

        <!-- Right Side - User Menu -->
        <div class="header-right">
          <!-- Notifications -->
          <div class="dropdown">
            <button
              type="button"
              class="btn btn-link header-icon position-relative"
              data-bs-toggle="dropdown"
              title="Notifications"
            >
              <i class="bi bi-bell"></i>
              <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
                3
                <span class="visually-hidden">unread notifications</span>
              </span>
            </button>
            <ul class="dropdown-menu dropdown-menu-end notification-dropdown">
              <li class="dropdown-header">
                <i class="bi bi-bell me-2"></i>
                Bildirimler (3)
              </li>
              <li><hr class="dropdown-divider"></li>
              <li>
                <a class="dropdown-item notification-item" href="#">
                  <div class="notification-content">
                    <div class="notification-title">Yeni kullanıcı kaydı</div>
                    <div class="notification-text">John Doe sisteme kaydoldu</div>
                    <div class="notification-time">2 dakika önce</div>
                  </div>
                </a>
              </li>
              <li>
                <a class="dropdown-item notification-item" href="#">
                  <div class="notification-content">
                    <div class="notification-title">Sistem güncellemesi</div>
                    <div class="notification-text">Platform v1.2.0 kullanıma hazır</div>
                    <div class="notification-time">1 saat önce</div>
                  </div>
                </a>
              </li>
              <li>
                <a class="dropdown-item notification-item" href="#">
                  <div class="notification-content">
                    <div class="notification-title">Güvenlik uyarısı</div>
                    <div class="notification-text">Şüpheli giriş denemesi tespit edildi</div>
                    <div class="notification-time">3 saat önce</div>
                  </div>
                </a>
              </li>
              <li><hr class="dropdown-divider"></li>
              <li>
                <a class="dropdown-item text-center" href="#">
                  <small>Tüm bildirimleri gör</small>
                </a>
              </li>
            </ul>
          </div>

          <!-- Direct Logout -->
          <button type="button" class="btn header-logout-outline d-none d-md-inline-flex align-items-center justify-content-center" title="Çıkış Yap" aria-label="Çıkış Yap" (click)="logout()">
            <i class="bi bi-box-arrow-right"></i>
          </button>

          <!-- User Menu -->
          <div class="dropdown" *ngIf="user()">
            <button
              type="button"
              class="btn btn-link header-user"
              data-bs-toggle="dropdown"
              [title]="user()?.firstName + ' ' + user()?.lastName"
            >
              <div class="user-avatar">
                <img
                  [src]="getUserAvatar()"
                  [alt]="user()?.firstName"
                  class="rounded-circle"
                >
              </div>
              <span class="user-name d-none d-md-inline">
                {{ user()?.firstName }} {{ user()?.lastName }}
              </span>
              <i class="bi bi-chevron-down ms-2"></i>
            </button>
            <ul class="dropdown-menu dropdown-menu-end user-dropdown">
              <li class="dropdown-header">
                <div class="user-info">
                  <div class="user-name">{{ user()?.firstName }} {{ user()?.lastName }}</div>
                  <div class="user-email">{{ user()?.email }}</div>
                </div>
              </li>
              <li><hr class="dropdown-divider"></li>
              <li>
                <a class="dropdown-item" routerLink="/profile">
                  <i class="bi bi-person me-2"></i>
                  Profil
                </a>
              </li>
              <li>
                <a class="dropdown-item" routerLink="/settings">
                  <i class="bi bi-gear me-2"></i>
                  Ayarlar
                </a>
              </li>
              <li>
                <a class="dropdown-item" href="#">
                  <i class="bi bi-question-circle me-2"></i>
                  Yardım
                </a>
              </li>
              <li><hr class="dropdown-divider"></li>
              <li>
                <button class="dropdown-item text-danger" (click)="logout()">
                  <i class="bi bi-box-arrow-right me-2"></i>
                  Çıkış Yap
                </button>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .app-header {
      background: var(--bs-header-bg, #ffffff);
      border-bottom: 1px solid var(--bs-border-color, #dee2e6);
      height: 60px;
      position: sticky;
      top: 0;
      z-index: 1020;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .header-content {
      display: flex;
      align-items: center;
      justify-content: space-between;
      height: 100%;
      padding: 0 1rem;
    }

    .header-left,
    .header-right {
      display: flex;
      align-items: center;
    }

    .header-toggle {
      color: var(--bs-nav-link-color, #6c757d);
      font-size: 1.2rem;
      padding: 0.5rem;
      margin-right: 1rem;
    }

    .header-toggle:hover {
      color: var(--bs-nav-link-hover-color, #0d6efd);
    }

    .header-brand h5 {
      color: #0d6efd;
      margin: 0;
    }

    .header-icon {
      color: var(--bs-nav-link-color, #6c757d);
      font-size: 1.1rem;
      padding: 0.5rem;
      margin: 0 0.25rem;
      position: relative;
    }

    .header-icon:hover {
      color: var(--bs-nav-link-hover-color, #0d6efd);
    }

    .header-user {
      display: flex;
      align-items: center;
      color: var(--bs-nav-link-color, #6c757d);
      text-decoration: none;
      padding: 0.5rem;
      margin-left: 0.5rem;
    }

    .header-user:hover {
      color: var(--bs-nav-link-hover-color, #0d6efd);
    }

    .user-avatar img {
      width: 32px;
      height: 32px;
      object-fit: cover;
    }

    .user-name {
      margin-left: 0.5rem;
      font-weight: 500;
    }

    /* Dropdown Styles */
    .notification-dropdown,
    .user-dropdown {
      border: none;
      box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
      border-radius: 0.5rem;
      min-width: 300px;
    }

    .notification-dropdown {
      max-height: 400px;
      overflow-y: auto;
    }

    .dropdown-header {
      background: var(--bs-content-bg, #f8f9fa);
      color: var(--bs-body-color, #212529);
      font-weight: 600;
      padding: 0.75rem 1rem;
    }

    .notification-item {
      padding: 0.75rem 1rem;
      border-bottom: 1px solid var(--bs-border-color, #dee2e6);
    }

    .notification-item:last-child {
      border-bottom: none;
    }

    .notification-content {
      font-size: 0.875rem;
    }

    .notification-title {
      font-weight: 600;
      color: var(--bs-body-color, #212529);
    }

    .notification-text {
      color: var(--bs-nav-link-color, #6c757d);
      margin: 0.25rem 0;
    }

    .notification-time {
      font-size: 0.75rem;
      color: var(--bs-nav-link-color, #6c757d);
    }

    .user-info {
      text-align: center;
    }

    .user-info .user-name {
      font-weight: 600;
      color: var(--bs-body-color, #212529);
    }

    .user-info .user-email {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color, #6c757d);
    }

    /* Badge */
    .badge {
      font-size: 0.65rem;
    }

    /* Responsive */
    @media (max-width: 576px) {
      .header-content {
        padding: 0 0.5rem;
      }

      .header-toggle {
        margin-right: 0.5rem;
      }

      .notification-dropdown,
      .user-dropdown {
        min-width: 250px;
      }
    }

    /* Dark Theme */
    .dark-theme .app-header {
      background: var(--bs-header-bg, #2d3339);
      border-bottom-color: var(--bs-border-color, #495057);
    }

    .dark-theme .dropdown-menu {
      background: var(--bs-card-bg, #2d3339);
      border-color: var(--bs-border-color, #495057);
    }

    .dark-theme .dropdown-item {
      color: var(--bs-body-color, #ffffff);
    }

    .dark-theme .dropdown-item:hover {
      background: var(--bs-content-bg, #1a1d21);
    }
  `]
})
export class HeaderComponent {
  private readonly store = inject(Store);

  user = input<User | null>(null);
  sidebarHidden = input<boolean>(false);

  toggleSidebar = output<void>();

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  getUserAvatar(): string {
    const user = this.user();
    if (user?.profilePicture) {
      return user.profilePicture;
    }

    // Generate avatar based on user initials
    const initials = `${user?.firstName?.charAt(0) || ''}${user?.lastName?.charAt(0) || ''}`;
    return `https://ui-avatars.com/api/?name=${initials}&background=0d6efd&color=fff&size=32`;
  }

  logout(): void {
    this.store.dispatch(AuthActions.logout());
  }
}