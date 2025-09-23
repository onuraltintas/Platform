import { Component, Input, Output, EventEmitter, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { ToastService } from '../../../../core/bildirimler/toast.service';
import { LucideAngularModule, Home, Users, UserCheck, Shield, Key, BookOpen, FileText,
         Settings, User, LogOut, ChevronDown, ChevronRight, Activity, Database,
         Mail, Calendar, Lock, Bell, Layers, Package, BarChart3 } from 'lucide-angular';

interface MenuItem {
  label: string;
  icon?: any;
  route?: string;
  permission?: string;
  permissions?: string[]; // For multiple permissions (OR logic)
  roles?: string[];
  children?: MenuItem[];
  expanded?: boolean;
}

@Component({
  selector: 'app-modern-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    LucideAngularModule
  ],
  template: `
    <aside class="navbar navbar-vertical navbar-expand-lg"
           [class.navbar-collapsed]="collapsed"
           [class.navbar-hidden]="hidden">
      <div class="container-fluid">
        <button class="navbar-toggler" type="button" (click)="toggleCollapse()">
          <span class="navbar-toggler-icon"></span>
        </button>

        <!-- Brand -->
        <h1 class="navbar-brand navbar-brand-autodark">
          <a routerLink="/admin" class="navbar-brand-link">
            @if (!collapsed) {
              <img src="logo.png" width="110" height="32" alt="OnAl" class="navbar-brand-image">
            } @else {
              <img src="simge.png" width="32" height="32" alt="OnAl" class="navbar-brand-image">
            }
          </a>
        </h1>

        <!-- User info (mobile) -->
        <div class="navbar-nav flex-row d-lg-none">
          <div class="nav-item dropdown">
            <a href="#" class="nav-link d-flex lh-1 text-reset p-0" data-bs-toggle="dropdown">
              <span class="avatar avatar-sm">
                {{ getUserInitials() }}
              </span>
            </a>
          </div>
        </div>

        <!-- Sidebar content -->
        <div class="navbar-collapse" id="sidebar-menu">
          <ul class="navbar-nav">
            <!-- Dashboard -->
            <li class="nav-item" routerLinkActive="active">
              <a class="nav-link" routerLink="/admin">
                <span class="nav-link-icon">
                  <lucide-icon [name]="homeIcon" [size]="20"></lucide-icon>
                </span>
                @if (!collapsed) {
                  <span class="nav-link-title">Ana Sayfa</span>
                }
              </a>
            </li>

            <!-- Menu items -->
            @for (item of visibleMenuItems(); track item.label) {
              <li class="nav-item" [class.dropdown]="item.children"
                  [class.active]="isMenuActive(item)"
                  [class.show]="item.expanded">
                @if (item.children) {
                  <a class="nav-link dropdown-toggle" href="#"
                     (click)="toggleMenuItem($event, item)"
                     [class.show]="item.expanded">
                    <span class="nav-link-icon">
                      <lucide-icon [name]="item.icon" [size]="20"></lucide-icon>
                    </span>
                    @if (!collapsed) {
                      <span class="nav-link-title">{{ item.label }}</span>
                    }
                  </a>
                  @if (item.expanded) {
                    <div class="dropdown-menu" [class.show]="item.expanded">
                      <div class="dropdown-menu-columns">
                        <div class="dropdown-menu-column">
                          @for (child of item.children; track child.label) {
                            @if (canAccessMenuItem(child)) {
                              <a class="dropdown-item" [routerLink]="child.route"
                                 routerLinkActive="active">
                                @if (child.icon) {
                                  <lucide-icon [name]="child.icon" [size]="16" class="me-2"></lucide-icon>
                                }
                                {{ child.label }}
                              </a>
                            }
                          }
                        </div>
                      </div>
                    </div>
                  }
                } @else {
                  <a class="nav-link" [routerLink]="item.route" routerLinkActive="active">
                    <span class="nav-link-icon">
                      <lucide-icon [name]="item.icon" [size]="20"></lucide-icon>
                    </span>
                    @if (!collapsed) {
                      <span class="nav-link-title">{{ item.label }}</span>
                    }
                  </a>
                }
              </li>
            }

            <!-- Divider -->
            <li class="nav-item">
              <hr class="navbar-divider my-2">
            </li>

            <!-- Profile & Settings -->
            <li class="nav-item" routerLinkActive="active">
              <a class="nav-link" routerLink="/admin/profile">
                <span class="nav-link-icon">
                  <lucide-icon [name]="userIcon" [size]="20"></lucide-icon>
                </span>
                @if (!collapsed) {
                  <span class="nav-link-title">Profil</span>
                }
              </a>
            </li>

            <li class="nav-item" routerLinkActive="active">
              <a class="nav-link" routerLink="/admin/settings">
                <span class="nav-link-icon">
                  <lucide-icon [name]="settingsIcon" [size]="20"></lucide-icon>
                </span>
                @if (!collapsed) {
                  <span class="nav-link-title">Ayarlar</span>
                }
              </a>
            </li>

            <!-- Logout -->
            <li class="nav-item">
              <a class="nav-link" href="#" (click)="logout($event)">
                <span class="nav-link-icon">
                  <lucide-icon [name]="logoutIcon" [size]="20"></lucide-icon>
                </span>
                @if (!collapsed) {
                  <span class="nav-link-title">Çıkış Yap</span>
                }
              </a>
            </li>
          </ul>
        </div>
      </div>
    </aside>
  `,
  styles: [`
    .navbar-vertical {
      position: fixed;
      top: 0;
      left: 0;
      bottom: 0;
      width: 260px;
      z-index: 1040;
      background: var(--bs-sidebar-bg);
      border-right: 1px solid var(--bs-border-color);
      transition: all 0.3s ease;
      overflow-y: auto;
    }

    .navbar-vertical.navbar-collapsed {
      width: 80px;
    }

    .navbar-vertical.navbar-hidden {
      transform: translateX(-100%);
    }

    .navbar-brand {
      padding: 1rem;
      border-bottom: 1px solid var(--bs-border-color);
      min-height: 57px;
      display: flex;
      align-items: center;
    }

    .navbar-brand-link {
      text-decoration: none;
      display: flex;
      align-items: center;
    }

    .navbar-nav {
      padding: 1rem 0;
    }

    .nav-link {
      display: flex;
      align-items: center;
      padding: 0.5rem 1rem;
      color: var(--bs-nav-link-color);
      text-decoration: none;
      transition: all 0.2s ease;
      border-radius: 0.375rem;
      margin: 0 0.5rem 0.25rem;
    }

    .nav-link:hover {
      background: rgba(0, 0, 0, 0.05);
      color: var(--bs-nav-link-hover-color);
    }

    :host-context(.dark-theme) .nav-link:hover {
      background: rgba(255, 255, 255, 0.05);
    }

    .nav-link.active,
    .nav-item.active > .nav-link {
      background: var(--bs-primary);
      color: white;
    }

    .nav-link-icon {
      width: 2rem;
      height: 2rem;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-right: 0.5rem;
    }

    .navbar-collapsed .nav-link-icon {
      margin-right: 0;
    }

    .nav-link-title {
      flex: 1;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .navbar-collapsed .nav-link-title {
      display: none;
    }

    .dropdown-menu {
      position: static;
      background: transparent;
      border: none;
      padding: 0;
      margin: 0;
      box-shadow: none;
      display: none; /* Default gizli */
    }

    .dropdown-menu.show {
      display: block; /* Show class ile görünür */
    }

    .dropdown-item {
      padding: 0.5rem 1rem 0.5rem 3rem;
      color: var(--bs-nav-link-color);
      text-decoration: none;
      border-radius: 0.375rem;
      margin: 0 0.5rem 0.25rem;
      transition: all 0.2s ease;
    }

    .dropdown-item:hover {
      background: rgba(0, 0, 0, 0.05);
      color: var(--bs-nav-link-hover-color);
    }

    :host-context(.dark-theme) .dropdown-item:hover {
      background: rgba(255, 255, 255, 0.05);
    }

    .dropdown-item.active {
      background: var(--bs-primary);
      color: white;
    }

    .navbar-collapsed .dropdown-menu {
      display: none;
    }

    .navbar-divider {
      opacity: 0.15;
      margin: 0.5rem 1rem;
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

    @media (max-width: 991.98px) {
      .navbar-vertical {
        width: 260px;
      }

      .navbar-vertical.navbar-hidden {
        transform: translateX(-100%);
      }
    }
  `]
})
export class ModernSidebarComponent implements OnInit {
  @Input() collapsed = false;
  @Input() hidden = false;
  @Output() collapsedChange = new EventEmitter<boolean>();
  @Output() hiddenChange = new EventEmitter<boolean>();

  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  // Permission service injection through constructor
  constructor(private permissionService: PermissionService) {}

  // Icons
  readonly homeIcon = Home;
  readonly usersIcon = Users;
  readonly userCheckIcon = UserCheck;
  readonly shieldIcon = Shield;
  readonly keyIcon = Key;
  readonly bookOpenIcon = BookOpen;
  readonly fileTextIcon = FileText;
  readonly settingsIcon = Settings;
  readonly userIcon = User;
  readonly logoutIcon = LogOut;
  readonly chevronDownIcon = ChevronDown;
  readonly chevronRightIcon = ChevronRight;
  readonly activityIcon = Activity;
  readonly databaseIcon = Database;
  readonly mailIcon = Mail;
  readonly calendarIcon = Calendar;
  readonly lockIcon = Lock;
  readonly bellIcon = Bell;
  readonly layersIcon = Layers;
  readonly packageIcon = Package;
  readonly barChartIcon = BarChart3;

  currentUser = signal<any>(null);
  visibleMenuItems = signal<MenuItem[]>([]);

  private menuItems: MenuItem[] = [
    {
      label: 'Kullanıcı Yönetimi',
      icon: this.usersIcon,
      permissions: ['Identity.Users.Read', 'Identity.Roles.Read', 'Identity.Groups.Read', 'Identity.Permissions.Read'],
      children: [
        {
          label: 'Kullanıcılar',
          route: '/admin/user-management/users',
          permission: 'Identity.Users.Read',
          icon: this.userCheckIcon
        },
        {
          label: 'Roller',
          route: '/admin/user-management/roles',
          permission: 'Identity.Roles.Read',
          icon: this.shieldIcon
        },
        {
          label: 'Yetkiler',
          route: '/admin/user-management/permissions',
          permission: 'Identity.Permissions.Read',
          icon: this.keyIcon
        },
        {
          label: 'Gruplar',
          route: '/admin/user-management/groups',
          permission: 'Identity.Groups.Read',
          icon: this.usersIcon
        }
      ]
    },
    {
      label: 'Hızlı Okuma',
      icon: this.bookOpenIcon,
      permissions: ['SpeedReading.Exercises.Read', 'SpeedReading.ReadingTexts.Read', 'SpeedReading.Progress.Read', 'SpeedReading.Statistics.Read'],
      children: [
        {
          label: 'Egzersizler',
          route: '/admin/speed-reading/exercises',
          permission: 'SpeedReading.Exercises.Read',
          icon: this.fileTextIcon
        },
        {
          label: 'Metin Kütüphanesi',
          route: '/admin/speed-reading/texts',
          permission: 'SpeedReading.ReadingTexts.Read',
          icon: this.packageIcon
        },
        {
          label: 'Oturumlar',
          route: '/admin/speed-reading/sessions',
          permission: 'SpeedReading.Sessions.Read',
          icon: this.activityIcon
        },
        {
          label: 'İlerleme Takibi',
          route: '/admin/speed-reading/progress',
          permission: 'SpeedReading.Progress.Read',
          icon: this.barChartIcon
        },
        {
          label: 'İstatistikler',
          route: '/admin/speed-reading/statistics',
          permission: 'SpeedReading.Statistics.Read',
          icon: this.layersIcon
        }
      ]
    },
    {
      label: 'Platform Yönetimi',
      icon: this.layersIcon,
      roles: ['SuperAdmin', 'Admin'],
      children: [
        {
          label: 'Cache Yönetimi',
          route: '/admin/platform/cache',
          icon: this.databaseIcon,
          permission: 'Platform.Cache.Read'
        },
        {
          label: 'Email Sistemi',
          route: '/admin/platform/email',
          icon: this.mailIcon,
          permission: 'Platform.Email.Read'
        },
        {
          label: 'Event Bus',
          route: '/admin/platform/events',
          icon: this.activityIcon,
          permission: 'Platform.Events.Read'
        },
        {
          label: 'Güvenlik',
          route: '/admin/platform/security',
          icon: this.lockIcon,
          permission: 'Platform.Security.Read'
        },
        {
          label: 'Bildirimler',
          route: '/admin/platform/notifications',
          icon: this.bellIcon,
          permission: 'Platform.Notifications.Read'
        },
        {
          label: 'İzleme',
          route: '/admin/platform/monitoring',
          icon: this.barChartIcon,
          permission: 'Platform.Monitoring.Read'
        }
      ]
    }
  ];

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser.set(user);
      this.filterMenuItems();
    });
  }

  private filterMenuItems(): void {
    const filtered = this.menuItems.filter(item => this.canAccessMenuItem(item));
    this.visibleMenuItems.set(filtered);
  }

  canAccessMenuItem(item: MenuItem): boolean {
    const currentUser = this.currentUser();

    if (!currentUser) {
      return false;
    }

    // Check single permission
    if (item.permission) {
      const canAccess = this.permissionService.canAccess(item.permission);
      return canAccess;
    }

    // Check multiple permissions (OR logic)
    if (item.permissions && item.permissions.length > 0) {
      const canAccess = this.permissionService.canAccessAny(item.permissions);
      return canAccess;
    }

    // Check roles
    if (item.roles && item.roles.length > 0) {
      const canAccess = this.permissionService.isInAnyRole(item.roles);
      return canAccess;
    }

    // If no permission or role requirements, allow access
    return true;
  }

  toggleCollapse(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }

  toggleMenuItem(event: Event, item: MenuItem): void {
    event.preventDefault();

    if (item.children) {
      item.expanded = !item.expanded;
    } else {
    }
  }

  isMenuActive(item: MenuItem): boolean {
    if (item.route) {
      return this.router.isActive(item.route, false);
    }
    if (item.children) {
      return item.children.some(child => child.route && this.router.isActive(child.route, false));
    }
    return false;
  }

  getUserInitials(): string {
    const user = this.currentUser();
    if (user) {
      const firstInitial = user.firstName?.charAt(0) || '';
      const lastInitial = user.lastName?.charAt(0) || '';
      return (firstInitial + lastInitial).toUpperCase() || 'U';
    }
    return 'U';
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