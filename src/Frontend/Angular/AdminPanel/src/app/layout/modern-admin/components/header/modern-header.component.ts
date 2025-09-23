import { Component, Input, Output, EventEmitter, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { ThemeService } from '../../../../core/services/theme.service';
import { ToastService } from '../../../../core/bildirimler/toast.service';
import { LucideAngularModule, Menu, Search, Bell, Sun, Moon, User,
         LogOut, Settings, HelpCircle } from 'lucide-angular';

interface Notification {
  id: string;
  title: string;
  message: string;
  time: Date;
  read: boolean;
  type: 'info' | 'success' | 'warning' | 'error';
}

@Component({
  selector: 'app-modern-header',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule
  ],
  template: `
    <header class="navbar navbar-expand-md d-print-none">
      <div class="container-xl">
        <button class="navbar-toggler" type="button" (click)="toggleSidebar.emit()">
          <lucide-icon [name]="menuIcon" [size]="20"></lucide-icon>
        </button>

        <div class="navbar-nav flex-row order-md-last">
          <!-- Theme Switcher -->
          <div class="nav-item dropdown d-none d-md-flex me-3">
            <button class="nav-link px-0" (click)="toggleTheme()" title="Tema Değiştir">
              @if (isDarkTheme()) {
                <lucide-icon [name]="moonIcon" [size]="20"></lucide-icon>
              } @else {
                <lucide-icon [name]="sunIcon" [size]="20"></lucide-icon>
              }
            </button>
          </div>

          <!-- Notifications -->
          <div class="nav-item dropdown d-none d-md-flex me-3">
            <a href="#" class="nav-link px-0" data-bs-toggle="dropdown"
               tabindex="-1" aria-label="Bildirimler">
              <lucide-icon [name]="bellIcon" [size]="20"></lucide-icon>
              @if (unreadNotifications() > 0) {
                <span class="badge bg-red">{{ unreadNotifications() }}</span>
              }
            </a>
            <div class="dropdown-menu dropdown-menu-arrow dropdown-menu-end dropdown-menu-card">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">Bildirimler</h3>
                </div>
                <div class="list-group list-group-flush list-group-hoverable">
                  @if (notifications().length === 0) {
                    <div class="list-group-item">
                      <div class="text-muted">Yeni bildirim yok</div>
                    </div>
                  }
                  @for (notification of notifications(); track notification.id) {
                    <div class="list-group-item" [class.unread]="!notification.read">
                      <div class="row align-items-center">
                        <div class="col-auto">
                          <span class="status-dot"
                                [class.status-dot-animated]="!notification.read"
                                [class.bg-success]="notification.type === 'success'"
                                [class.bg-danger]="notification.type === 'error'"
                                [class.bg-warning]="notification.type === 'warning'"
                                [class.bg-info]="notification.type === 'info'">
                          </span>
                        </div>
                        <div class="col text-truncate">
                          <a href="#" class="text-body d-block">{{ notification.title }}</a>
                          <div class="d-block text-muted text-truncate mt-n1">
                            {{ notification.message }}
                          </div>
                          <div class="text-muted small mt-1">{{ formatTime(notification.time) }}</div>
                        </div>
                      </div>
                    </div>
                  }
                </div>
                <div class="card-footer">
                  <a href="#" class="text-center text-muted">Tüm bildirimleri gör</a>
                </div>
              </div>
            </div>
          </div>

          <!-- User Menu -->
          <div class="nav-item dropdown">
            <a href="#" class="nav-link d-flex lh-1 text-reset p-0"
               data-bs-toggle="dropdown" aria-label="Kullanıcı menüsü">
              <span class="avatar avatar-sm">{{ getUserInitials() }}</span>
              <div class="d-none d-xl-block ps-2">
                <div>{{ currentUser()?.firstName }} {{ currentUser()?.lastName }}</div>
                <div class="mt-1 small text-muted">{{ getUserRole() }}</div>
              </div>
            </a>
            <div class="dropdown-menu dropdown-menu-end dropdown-menu-arrow">
              <a href="#" class="dropdown-item" routerLink="/admin/profile">
                <lucide-icon [name]="userIcon" [size]="16" class="me-2"></lucide-icon>
                Profilim
              </a>
              <a href="#" class="dropdown-item" routerLink="/admin/settings">
                <lucide-icon [name]="settingsIcon" [size]="16" class="me-2"></lucide-icon>
                Ayarlar
              </a>
              <div class="dropdown-divider"></div>
              <a href="#" class="dropdown-item" (click)="showHelp($event)">
                <lucide-icon [name]="helpIcon" [size]="16" class="me-2"></lucide-icon>
                Yardım
              </a>
              <a href="#" class="dropdown-item" (click)="logout($event)">
                <lucide-icon [name]="logoutIcon" [size]="16" class="me-2"></lucide-icon>
                Çıkış Yap
              </a>
            </div>
          </div>
        </div>

        <!-- Search bar (desktop) -->
        <div class="collapse navbar-collapse">
          <div class="d-flex flex-column flex-md-row flex-fill align-items-stretch align-items-md-center">
            <div class="d-none d-md-flex">
              <form class="ms-3">
                <div class="input-icon">
                  <span class="input-icon-addon">
                    <lucide-icon [name]="searchIcon" [size]="16"></lucide-icon>
                  </span>
                  <input type="text" [(ngModel)]="searchQuery" name="search"
                         class="form-control form-control-sm"
                         placeholder="Ara..."
                         (keyup.enter)="search()">
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .navbar {
      background: var(--bs-header-bg);
      border-bottom: 1px solid var(--bs-border-color);
      min-height: 57px;
      padding: 0.5rem 0;
    }

    .navbar-toggler {
      padding: 0.25rem 0.5rem;
      font-size: 1.25rem;
      line-height: 1;
      background-color: transparent;
      border: 1px solid transparent;
      border-radius: 0.25rem;
      transition: box-shadow 0.15s ease-in-out;
    }

    .navbar-toggler:focus {
      box-shadow: 0 0 0 0.25rem rgba(13, 110, 253, 0.25);
      outline: 0;
    }

    .nav-link {
      padding: 0.5rem;
      color: var(--bs-nav-link-color);
      transition: color 0.15s ease;
      cursor: pointer;
      position: relative;
      display: flex;
      align-items: center;
      background: transparent;
      border: none;
    }

    .nav-link:hover {
      color: var(--bs-nav-link-hover-color);
    }

    .badge {
      position: absolute;
      top: 0;
      right: 0;
      transform: translate(25%, -25%);
      font-size: 0.625rem;
      padding: 0.25em 0.35em;
    }

    .avatar {
      width: 2rem;
      height: 2rem;
      background: var(--bs-primary);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      font-weight: 500;
      font-size: 0.875rem;
    }

    .dropdown-menu {
      margin-top: 0.5rem;
      min-width: 15rem;
    }

    .dropdown-menu-card {
      min-width: 25rem;
    }

    .dropdown-item {
      display: flex;
      align-items: center;
      padding: 0.5rem 1rem;
    }

    .dropdown-item lucide-icon {
      opacity: 0.5;
    }

    .input-icon {
      position: relative;
    }

    .input-icon-addon {
      position: absolute;
      top: 50%;
      transform: translateY(-50%);
      left: 0.75rem;
      z-index: 5;
      display: flex;
      align-items: center;
      pointer-events: none;
      color: var(--bs-secondary);
    }

    .input-icon input {
      padding-left: 2.5rem;
    }

    .form-control-sm {
      min-height: calc(1.5em + 0.5rem + 2px);
      padding: 0.25rem 0.5rem;
      font-size: 0.875rem;
      border-radius: 0.2rem;
    }

    .status-dot {
      display: inline-block;
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
    }

    .status-dot-animated {
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0% {
        box-shadow: 0 0 0 0 rgba(13, 110, 253, 0.7);
      }
      70% {
        box-shadow: 0 0 0 10px rgba(13, 110, 253, 0);
      }
      100% {
        box-shadow: 0 0 0 0 rgba(13, 110, 253, 0);
      }
    }

    .list-group-item.unread {
      background-color: rgba(13, 110, 253, 0.05);
    }

    :host-context(.dark-theme) .list-group-item.unread {
      background-color: rgba(13, 110, 253, 0.1);
    }

    @media (max-width: 767.98px) {
      .navbar {
        padding: 0.5rem;
      }

      .dropdown-menu-card {
        min-width: 100%;
      }
    }
  `]
})
export class ModernHeaderComponent implements OnInit {
  @Input() sidebarCollapsed = false;
  @Input() sidebarHidden = false;
  @Output() toggleSidebar = new EventEmitter<void>();

  private readonly authService = inject(AuthService);
  private readonly themeService = inject(ThemeService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  // Icons
  readonly menuIcon = Menu;
  readonly searchIcon = Search;
  readonly bellIcon = Bell;
  readonly sunIcon = Sun;
  readonly moonIcon = Moon;
  readonly userIcon = User;
  readonly logoutIcon = LogOut;
  readonly settingsIcon = Settings;
  readonly helpIcon = HelpCircle;

  currentUser = signal<any>(null);
  searchQuery = '';
  notifications = signal<Notification[]>([]);
  unreadNotifications = signal(0);

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser.set(user);
    });

    // Mock notifications - replace with real API call
    this.loadNotifications();
  }

  private loadNotifications(): void {
    const mockNotifications: Notification[] = [
      {
        id: '1',
        title: 'Yeni kullanıcı kaydı',
        message: 'John Doe sisteme kayıt oldu',
        time: new Date(Date.now() - 1000 * 60 * 5), // 5 minutes ago
        read: false,
        type: 'info'
      },
      {
        id: '2',
        title: 'Sistem güncellemesi',
        message: 'Sistem başarıyla güncellendi',
        time: new Date(Date.now() - 1000 * 60 * 30), // 30 minutes ago
        read: false,
        type: 'success'
      },
      {
        id: '3',
        title: 'Yedekleme tamamlandı',
        message: 'Veritabanı yedekleme işlemi başarıyla tamamlandı',
        time: new Date(Date.now() - 1000 * 60 * 60 * 2), // 2 hours ago
        read: true,
        type: 'success'
      }
    ];

    this.notifications.set(mockNotifications);
    this.unreadNotifications.set(mockNotifications.filter(n => !n.read).length);
  }

  getUserInitials(): string {
    const user = this.currentUser();
    if (user) {
      const firstInitial = user.firstName?.charAt(0) || '';
      const lastInitial = user.lastName?.charAt(0) || '';
      return (firstInitial + lastInitial).toUpperCase() || 'K';
    }
    return 'K';
  }

  getUserRole(): string {
    const user = this.currentUser();
    if (user && user.roles && user.roles.length > 0) {
      // Map English roles to Turkish
      const roleMap: Record<string, string> = {
        'SuperAdmin': 'Süper Admin',
        'Admin': 'Yönetici',
        'Manager': 'Müdür',
        'User': 'Kullanıcı',
        'Student': 'Öğrenci',
        'Guest': 'Misafir'
      };
      return roleMap[user.roles[0]] || user.roles[0];
    }
    return 'Kullanıcı';
  }

  isDarkTheme(): boolean {
    return this.themeService.isDarkMode();
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
    const currentTheme = this.themeService.currentTheme();

    const themeNames = {
      'light': 'Açık Tema',
      'dark': 'Koyu Tema'
    };

    this.toastService.bilgi(
      `${themeNames[currentTheme]} aktif edildi`,
      'Tema Değiştirildi',
      { timeOut: 1500 }
    );
  }

  search(): void {
    if (this.searchQuery.trim()) {
      this.toastService.bilgi(
        `"${this.searchQuery}" için arama yapılıyor...`,
        'Arama',
        { timeOut: 2000 }
      );

      // TODO: Implement actual search functionality
      console.log('Searching for:', this.searchQuery);

      // Example: navigate to search results page
      // this.router.navigate(['/admin/search'], { queryParams: { q: this.searchQuery } });
    } else {
      this.toastService.uyari('Lütfen arama yapmak için bir terim girin', 'Arama');
    }
  }

  formatTime(date: Date): string {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (minutes < 1) return 'Şimdi';
    if (minutes < 60) return `${minutes} dakika önce`;
    if (hours < 24) return `${hours} saat önce`;
    if (days < 7) return `${days} gün önce`;

    return date.toLocaleDateString('tr-TR');
  }

  showHelp(event: Event): void {
    event.preventDefault();

    this.toastService.bilgi(
      'Yardım merkezi yakında açılacak. Şimdilik lütfen destek ekibiyle iletişime geçin.',
      'Yardım',
      { timeOut: 4000 }
    );

    // TODO: Implement help functionality
    console.log('Show help');
  }

  logout(event: Event): void {
    event.preventDefault();

    this.toastService.bilgi('Güvenli çıkış yapılıyor...', 'Çıkış');

    this.authService.logout();

    setTimeout(() => {
      this.toastService.basari('Başarıyla çıkış yapıldı', 'Çıkış Tamamlandı');
      this.router.navigate(['/auth/login']);
    }, 500);
  }
}