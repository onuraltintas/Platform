import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LucideAngularModule, Users, Shield, UserCheck, Settings } from 'lucide-angular';

import { StatisticsCardComponent, StatisticConfig } from '../../../../shared/components/statistics-card/statistics-card.component';
import { ActionButtonGroupComponent, ActionButton } from '../../../../shared/components/action-button-group/action-button-group.component';

import { UserService } from '../../services/user.service';
import { RoleService } from '../../services/role.service';

import { UserDto, UserStatistics } from '../../models/user.models';
import { RoleDto } from '../../models/role.models';

@Component({
  selector: 'app-user-management-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LucideAngularModule,
    StatisticsCardComponent,
    ActionButtonGroupComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-wrapper">
      <!-- Page Header -->
      <div class="page-header d-print-none">
        <div class="container-xl">
          <div class="row g-2 align-items-center">
            <div class="col">
              <div class="page-pretitle">Yönetim</div>
              <h2 class="page-title">Kullanıcı Yönetimi</h2>
            </div>
            <div class="col-auto ms-auto d-print-none">
              <app-action-button-group
                [actions]="quickActions"
                (actionClick)="onQuickActionClick($event)"/>
            </div>
          </div>
        </div>
      </div>

      <!-- Page Content -->
      <div class="page-body">
        <div class="container-xl">
          <!-- Statistics Row -->
          <div class="row row-deck row-cards">
            <div class="col-12">
              <div class="row row-cards">
                @for (stat of statisticsCards(); track stat.title) {
                  <div class="col-sm-6 col-lg-3">
                    <app-statistics-card
                      [config]="stat"
                      (cardClick)="onStatisticCardClick($event)"/>
                  </div>
                }
              </div>
            </div>

            <!-- Recent Activity -->
            <div class="col-8">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">Son Aktiviteler</h3>
                  <div class="card-actions">
                    <a href="#" class="btn btn-primary btn-sm">
                      Tümünü Görüntüle
                    </a>
                  </div>
                </div>
                <div class="card-body">
                  @if (loading()) {
                    <div class="text-center py-4">
                      <div class="spinner-border spinner-border-sm" role="status">
                        <span class="visually-hidden">Yükleniyor...</span>
                      </div>
                    </div>
                  } @else {
                    @for (user of recentUsers(); track user.id) {
                      <div class="list-group-item d-flex align-items-center border-0 px-0">
                        <div class="avatar avatar-sm me-3">
                          <span class="avatar-initial rounded-circle bg-primary">
                            {{ user.firstName.charAt(0) }}{{ user.lastName.charAt(0) }}
                          </span>
                        </div>
                        <div class="flex-fill">
                          <div class="font-weight-medium">{{ user.firstName }} {{ user.lastName }}</div>
                          <div class="text-muted small">{{ user.email }}</div>
                        </div>
                        <div class="text-end">
                          <div class="small text-muted">{{ user.lastLoginAt | date:'short' }}</div>
                          <span class="badge" [class]="user.isActive ? 'bg-success' : 'bg-danger'">
                            {{ user.isActive ? 'Aktif' : 'Pasif' }}
                          </span>
                        </div>
                      </div>
                    }
                  }
                </div>
              </div>
            </div>

            <!-- Quick Stats -->
            <div class="col-4">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">Hızlı İstatistikler</h3>
                </div>
                <div class="card-body">
                  <div class="row g-3">
                    <!-- Active Users Today -->
                    <div class="col-12">
                      <div class="d-flex align-items-center">
                        <div class="me-3">
                          <div class="bg-success-subtle text-success rounded p-2">
                            <lucide-icon name="users" [size]="20"/>
                          </div>
                        </div>
                        <div>
                          <div class="text-muted small">Bugün Aktif</div>
                          <div class="h4 mb-0">{{ todayActiveUsers() }}</div>
                        </div>
                      </div>
                    </div>

                    <!-- New Registrations -->
                    <div class="col-12">
                      <div class="d-flex align-items-center">
                        <div class="me-3">
                          <div class="bg-info-subtle text-info rounded p-2">
                            <lucide-icon name="user-check" [size]="20"/>
                          </div>
                        </div>
                        <div>
                          <div class="text-muted small">Bu Hafta Yeni</div>
                          <div class="h4 mb-0">{{ weeklyNewUsers() }}</div>
                        </div>
                      </div>
                    </div>

                    <!-- Pending Approvals -->
                    <div class="col-12">
                      <div class="d-flex align-items-center">
                        <div class="me-3">
                          <div class="bg-warning-subtle text-warning rounded p-2">
                            <lucide-icon name="shield" [size]="20"/>
                          </div>
                        </div>
                        <div>
                          <div class="text-muted small">Onay Bekleyen</div>
                          <div class="h4 mb-0">{{ pendingApprovals() }}</div>
                        </div>
                      </div>
                    </div>

                    <!-- System Health -->
                    <div class="col-12">
                      <div class="d-flex align-items-center">
                        <div class="me-3">
                          <div class="bg-primary-subtle text-primary rounded p-2">
                            <lucide-icon name="settings" [size]="20"/>
                          </div>
                        </div>
                        <div>
                          <div class="text-muted small">Sistem Durumu</div>
                          <div class="h4 mb-0 text-success">Sağlıklı</div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Permission Matrix Overview -->
            <div class="col-12">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">Yetki Dağılımı</h3>
                  <div class="card-actions">
                    <a routerLink="../permissions/matrix" class="btn btn-outline-primary btn-sm">
                      Detaylı Matris
                    </a>
                  </div>
                </div>
                <div class="card-body">
                  <div class="row g-4">
                    @for (role of topRoles(); track role.id) {
                      <div class="col-md-4">
                        <div class="card card-sm">
                          <div class="card-body">
                            <div class="d-flex align-items-center">
                              <div class="me-3">
                                <div [class]="getRoleIconClasses(role)">
                                  <lucide-icon name="shield" [size]="16"/>
                                </div>
                              </div>
                              <div class="flex-fill">
                                <div class="font-weight-medium">{{ role.name }}</div>
                                <div class="text-muted small">{{ role.userCount }} kullanıcı</div>
                              </div>
                              <div class="text-end">
                                <div class="text-muted small">{{ role.permissionCount }} yetki</div>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    }
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-wrapper {
      padding: 0;
    }

    .card-sm {
      box-shadow: none;
      border: 1px solid var(--bs-border-color-translucent);
    }

    .card-sm:hover {
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12);
      border-color: var(--bs-primary);
    }

    .bg-success-subtle,
    .bg-info-subtle,
    .bg-warning-subtle,
    .bg-primary-subtle {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  `]
})
export class UserManagementDashboardComponent implements OnInit {
  // Services
  private userService = inject(UserService);
  private roleService = inject(RoleService);

  // Icons
  readonly usersIcon = Users;
  readonly shieldIcon = Shield;
  readonly userCheckIcon = UserCheck;
  readonly settingsIcon = Settings;

  // State signals
  loading = signal(false);
  statistics = signal<UserStatistics | null>(null);
  recentUsers = signal<UserDto[]>([]);
  topRoles = signal<(RoleDto & { userCount: number; permissionCount: number })[]>([]);

  // Computed values
  statisticsCards = computed(() => {
    const stats = this.statistics();
    if (!stats) {
      return this.getLoadingStatistics();
    }

    return [
      {
        title: 'Toplam Kullanıcı',
        value: stats.totalUsers,
        icon: 'users',
        color: 'primary' as const,
        trend: {
          value: stats.userGrowthRate,
          direction: stats.userGrowthRate > 0 ? 'up' as const : stats.userGrowthRate < 0 ? 'down' as const : 'neutral' as const,
          period: 'Bu ay'
        },
        clickable: true,
        routerLink: ['../users']
      },
      {
        title: 'Aktif Kullanıcılar',
        value: stats.activeUsers,
        icon: 'user-check',
        color: 'success' as const,
        subtitle: `${Math.round((stats.activeUsers / stats.totalUsers) * 100)}% aktif`,
        clickable: true,
        routerLink: ['../users'],
        chart: {
          type: 'line' as const,
          data: stats.dailyActiveUsers || [],
          color: 'var(--bs-success)'
        }
      },
      {
        title: 'Roller',
        value: stats.totalRoles,
        icon: 'shield',
        color: 'info' as const,
        subtitle: `${stats.systemRoles} sistem rolu`,
        clickable: true,
        routerLink: ['../roles']
      },
      {
        title: 'Gruplar',
        value: stats.totalGroups,
        icon: 'users',
        color: 'warning' as const,
        subtitle: `Ortalama ${Math.round(stats.averageGroupSize)} üye`,
        clickable: true,
        routerLink: ['../groups']
      }
    ] as StatisticConfig[];
  });


  todayActiveUsers = computed(() => {
    const stats = this.statistics();
    return stats?.todayActiveUsers || 0;
  });

  weeklyNewUsers = computed(() => {
    const stats = this.statistics();
    return stats?.weeklyNewUsers || 0;
  });

  pendingApprovals = computed(() => {
    const stats = this.statistics();
    return stats?.pendingApprovals || 0;
  });

  // Quick actions
  quickActions: ActionButton[] = [
    {
      key: 'add-user',
      label: 'Kullanıcı Ekle',
      icon: 'user-plus',
      variant: 'primary',
      requiresPermission: 'Identity.Users.Create'
    },
    {
      key: 'bulk-operations',
      label: 'Toplu İşlemler',
      icon: 'settings',
      variant: 'outline-secondary',
      dropdown: [
        {
          key: 'bulk-export',
          label: 'Dışa Aktar',
          icon: 'download',
          requiresPermission: 'Identity.Users.Read'
        },
        {
          key: 'bulk-import',
          label: 'İçe Aktar',
          icon: 'upload',
          requiresPermission: 'Identity.Users.Create'
        },
        { key: 'divider', label: '', icon: '' },
        {
          key: 'bulk-activate',
          label: 'Toplu Aktifleştir',
          icon: 'user-check',
          requiresPermission: 'Identity.Users.Update'
        },
        {
          key: 'bulk-deactivate',
          label: 'Toplu Pasifleştir',
          icon: 'user-x',
          requiresPermission: 'Identity.Users.Update',
          destructive: true
        }
      ]
    },
    {
      key: 'system-settings',
      label: 'Sistem Ayarları',
      icon: 'settings',
      variant: 'outline-primary',
      requiresPermission: 'Identity.System.Settings'
    }
  ];


  ngOnInit() {
    this.loadDashboardData();
  }

  private loadDashboardData() {
    this.loading.set(true);

    // Load statistics
    this.userService.getStatistics().subscribe({
      next: (stats) => {
        this.statistics.set(stats || null);
      },
      error: (error) => {
        console.error('Failed to load statistics:', error);
      }
    });

    // Load recent users
    this.userService.getUsers({
      pageSize: 5,
      sortBy: 'lastLoginAt',
      sortDirection: 'desc'
    }).subscribe({
      next: (users) => {
        this.recentUsers.set(users?.data || []);
      },
      error: (error) => {
        console.error('Failed to load recent users:', error);
      }
    });

    // Load top roles
    this.roleService.getRoles({
      pageSize: 3
    }).subscribe({
      next: (roles) => {
        this.topRoles.set(roles?.data as any || []);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Failed to load roles:', error);
        this.loading.set(false);
      }
    });
  }

  onQuickActionClick(event: any) {
    switch (event.action) {
      case 'add-user':
        // Navigate to user creation
        break;
      case 'bulk-export':
        // Handle bulk export
        break;
      case 'bulk-import':
        // Handle bulk import
        break;
      case 'bulk-activate':
        // Handle bulk activate
        break;
      case 'bulk-deactivate':
        // Handle bulk deactivate
        break;
      case 'system-settings':
        // Navigate to system settings
        break;
    }
  }

  onStatisticCardClick(config: StatisticConfig) {
    if (config.routerLink) {
      // Navigation will be handled by the router
    }
  }


  getRoleIconClasses(role: any): string {
    const colors = ['primary', 'success', 'info', 'warning', 'danger'];
    const colorIndex = Math.abs(role.id.hashCode()) % colors.length;
    const color = colors[colorIndex];

    return `bg-${color}-subtle text-${color} rounded p-2`;
  }

  private getLoadingStatistics(): StatisticConfig[] {
    return [
      {
        title: 'Toplam Kullanıcı',
        value: 0,
        icon: 'users',
        color: 'primary',
        loading: true
      },
      {
        title: 'Aktif Kullanıcılar',
        value: 0,
        icon: 'user-check',
        color: 'success',
        loading: true
      },
      {
        title: 'Roller',
        value: 0,
        icon: 'shield',
        color: 'info',
        loading: true
      },
      {
        title: 'Gruplar',
        value: 0,
        icon: 'users',
        color: 'warning',
        loading: true
      }
    ];
  }
}

// String extension for hash code
declare global {
  interface String {
    hashCode(): number;
  }
}

String.prototype.hashCode = function() {
  let hash = 0;
  if (this.length === 0) return hash;
  for (let i = 0; i < this.length; i++) {
    const char = this.charCodeAt(i);
    hash = ((hash << 5) - hash) + char;
    hash = hash & hash;
  }
  return hash;
};