import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Server, Search, Filter, Download, Eye, EyeOff, RefreshCw, Shield, Users, Key, Activity } from 'lucide-angular';

import { ActionButtonGroupComponent, ActionButton } from '../../../../../shared/components/action-button-group/action-button-group.component';
import { FilterPanelComponent, FilterField } from '../../../../../shared/components/filter-panel/filter-panel.component';
import { StatisticsCardComponent } from '../../../../../shared/components/statistics-card/statistics-card.component';

import { PermissionService } from '../../../services/permission.service';
import { RoleService } from '../../../services/role.service';

import { PermissionDto } from '../../../models/permission.models';
import { RoleDto } from '../../../models/role.models';

interface ServiceGroup {
  name: string;
  description?: string;
  permissions: PermissionWithUsage[];
  totalRoles: number;
  totalUsers: number;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
  expanded: boolean;
}

interface PermissionWithUsage extends PermissionDto {
  roleCount: number;
  userCount: number;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
  lastUsed?: Date;
  usageFrequency: number;
}

@Component({
  selector: 'app-permission-by-service',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    LucideAngularModule,
    ActionButtonGroupComponent,
    FilterPanelComponent,
    StatisticsCardComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-wrapper">
      <!-- Page Header -->
      <div class="page-header d-print-none">
        <div class="container-xl">
          <div class="row g-2 align-items-center">
            <div class="col">
              <div class="page-pretitle">Kullanıcı Yönetimi</div>
              <h2 class="page-title">
                <lucide-icon name="server" [size]="24" class="me-2"/>
                Servis Yetkileri
              </h2>
              <div class="page-subtitle">
                Servislere göre gruplandırılmış yetkileri keşfedin ve yönetin
              </div>
            </div>
            <div class="col-auto ms-auto d-print-none">
              <app-action-button-group
                [actions]="headerActions"
                (actionClick)="onHeaderAction($event)"/>
            </div>
          </div>
        </div>
      </div>

      <!-- Page Content -->
      <div class="page-body">
        <div class="container-xl">
          <!-- Statistics Row -->
          <div class="row row-deck row-cards mb-4">
            @for (stat of statisticsCards(); track stat.title) {
              <div class="col-sm-6 col-lg-3">
                <app-statistics-card
                  [config]="stat"
                  (cardClick)="onStatisticCardClick($event)"/>
              </div>
            }
          </div>

          <div class="row row-deck row-cards">
            <!-- Filters -->
            <div class="col-3">
              <app-filter-panel
                [filterFields]="filterFields()"
                [initialFilters]="filters()"
                [showSearch]="true"
                [searchPlaceholder]="'Servis veya yetki ara...'"
                (filtersChange)="onFiltersChange($event)"/>

              <!-- Risk Level Legend -->
              <div class="card mt-3">
                <div class="card-header">
                  <h3 class="card-title">Risk Seviyeleri</h3>
                </div>
                <div class="card-body">
                  <div class="row g-2">
                    <div class="col-12">
                      <span class="badge bg-success me-2">●</span>
                      <small>Düşük Risk</small>
                    </div>
                    <div class="col-12">
                      <span class="badge bg-warning me-2">●</span>
                      <small>Orta Risk</small>
                    </div>
                    <div class="col-12">
                      <span class="badge bg-danger me-2">●</span>
                      <small>Yüksek Risk</small>
                    </div>
                    <div class="col-12">
                      <span class="badge bg-dark me-2">●</span>
                      <small>Kritik Risk</small>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Quick Actions -->
              <div class="card mt-3">
                <div class="card-header">
                  <h3 class="card-title">Hızlı İşlemler</h3>
                </div>
                <div class="card-body">
                  <div class="d-grid gap-2">
                    <button class="btn btn-outline-primary btn-sm" (click)="expandAllServices()">
                      <lucide-icon [name]="allServicesExpanded() ? 'eye-off' : 'eye'" [size]="14" class="me-1"/>
                      {{ allServicesExpanded() ? 'Tümünü Kapat' : 'Tümünü Aç' }}
                    </button>
                    <button class="btn btn-outline-info btn-sm" (click)="refreshUsageData()">
                      <lucide-icon name="refresh-cw" [size]="14" class="me-1"/>
                      Kullanım Verilerini Yenile
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" (click)="exportServiceReport()">
                      <lucide-icon name="download" [size]="14" class="me-1"/>
                      Servis Raporu
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Service Explorer -->
            <div class="col-9">
              @if (loading()) {
                <div class="card">
                  <div class="card-body d-flex justify-content-center py-5">
                    <div class="spinner-border text-primary" role="status">
                      <span class="visually-hidden">Yükleniyor...</span>
                    </div>
                  </div>
                </div>
              } @else {
                @for (service of filteredServices(); track service.name) {
                  <div class="card mb-3">
                    <div class="card-header" (click)="toggleService(service.name)">
                      <div class="row align-items-center cursor-pointer">
                        <div class="col">
                          <div class="d-flex align-items-center">
                            <lucide-icon [name]="service.expanded ? 'chevron-down' : 'chevron-right'" [size]="20" class="me-2"/>
                            <div>
                              <h3 class="card-title mb-1">{{ service.name }}</h3>
                              @if (service.description) {
                                <div class="text-muted small">{{ service.description }}</div>
                              }
                            </div>
                          </div>
                        </div>
                        <div class="col-auto">
                          <div class="d-flex align-items-center gap-3">
                            <!-- Risk Level -->
                            <span [class]="getServiceRiskClass(service)">
                              {{ getServiceRiskText(service) }}
                            </span>

                            <!-- Statistics -->
                            <div class="text-center">
                              <div class="h4 mb-0">{{ service.permissions.length }}</div>
                              <div class="text-muted small">Yetki</div>
                            </div>
                            <div class="text-center">
                              <div class="h4 mb-0">{{ service.totalRoles }}</div>
                              <div class="text-muted small">Rol</div>
                            </div>
                            <div class="text-center">
                              <div class="h4 mb-0">{{ service.totalUsers }}</div>
                              <div class="text-muted small">Kullanıcı</div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>

                    @if (service.expanded) {
                      <div class="card-body">
                        <!-- Service Permissions -->
                        <div class="row g-3">
                          @for (permission of service.permissions; track permission.id) {
                            <div class="col-md-6 col-lg-4">
                              <div class="card card-sm permission-card"
                                   [class]="getPermissionCardClass(permission)"
                                   (click)="viewPermissionDetail(permission)">
                                <div class="card-body">
                                  <div class="d-flex align-items-start">
                                    <div class="me-3">
                                      <div [class]="getPermissionIconClass(permission)">
                                        <lucide-icon name="key" [size]="16"/>
                                      </div>
                                    </div>
                                    <div class="flex-fill">
                                      <div class="permission-name">{{ permission.name }}</div>
                                      @if (permission.description) {
                                        <div class="permission-description">{{ permission.description }}</div>
                                      }

                                      <!-- Usage Stats -->
                                      <div class="mt-2">
                                        <div class="row g-2 text-center">
                                          <div class="col">
                                            <div class="h6 mb-0">{{ permission.roleCount || 0 }}</div>
                                            <div class="text-muted small">Rol</div>
                                          </div>
                                          <div class="col">
                                            <div class="h6 mb-0">{{ permission.userCount || 0 }}</div>
                                            <div class="text-muted small">Kullanıcı</div>
                                          </div>
                                          <div class="col">
                                            <div class="h6 mb-0">{{ permission.usageFrequency || 0 }}%</div>
                                            <div class="text-muted small">Kullanım</div>
                                          </div>
                                        </div>
                                      </div>

                                      <!-- Risk & Status -->
                                      <div class="mt-2 d-flex justify-content-between align-items-center">
                                        <span [class]="getPermissionRiskClass(permission)">
                                          {{ getPermissionRiskText(permission) }}
                                        </span>
                                        @if (permission.lastUsed) {
                                          <span class="text-muted small">
                                            Son: {{ formatDate(permission.lastUsed) }}
                                          </span>
                                        }
                                      </div>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </div>
                          }
                        </div>

                        @if (service.permissions.length === 0) {
                          <div class="text-center py-4">
                            <lucide-icon name="search" [size]="48" class="text-muted mb-3"/>
                            <h4 class="text-muted">Bu serviste yetki bulunamadı</h4>
                            <p class="text-muted">Filtreleri kontrol edin veya arama kriterlerinizi değiştirin.</p>
                          </div>
                        }
                      </div>

                      <!-- Service Footer with Bulk Actions -->
                      <div class="card-footer">
                        <div class="d-flex justify-content-between align-items-center">
                          <div class="text-muted">
                            {{ service.permissions.length }} yetki gösteriliyor
                          </div>
                          <div class="btn-group" role="group">
                            <button class="btn btn-sm btn-outline-primary" (click)="exportServicePermissions(service.name)">
                              <lucide-icon name="download" [size]="14" class="me-1"/>
                              Dışa Aktar
                            </button>
                            <button class="btn btn-sm btn-outline-info" (click)="viewServiceMatrix(service.name)">
                              <lucide-icon name="grid-3x3" [size]="14" class="me-1"/>
                              Matris
                            </button>
                            <button class="btn btn-sm btn-outline-secondary" (click)="analyzeServiceUsage(service.name)">
                              <lucide-icon name="activity" [size]="14" class="me-1"/>
                              Analiz
                            </button>
                          </div>
                        </div>
                      </div>
                    }
                  </div>
                }

                @if (filteredServices().length === 0) {
                  <div class="card">
                    <div class="card-body text-center py-5">
                      <lucide-icon name="search" [size]="64" class="text-muted mb-3"/>
                      <h3 class="text-muted">Servis bulunamadı</h3>
                      <p class="text-muted">Arama kriterlerinizi değiştirin veya filtreleri sıfırlayın.</p>
                      <button class="btn btn-primary" (click)="clearFilters()">
                        Filtreleri Temizle
                      </button>
                    </div>
                  </div>
                }
              }
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .cursor-pointer {
      cursor: pointer;
    }

    .permission-card {
      transition: all 0.2s ease-in-out;
      cursor: pointer;
    }

    .permission-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
    }

    .permission-card.high-risk {
      border-left: 4px solid var(--bs-danger);
    }

    .permission-card.critical-risk {
      border-left: 4px solid var(--bs-dark);
    }

    .permission-name {
      font-weight: 600;
      color: var(--bs-dark);
      font-size: 0.875rem;
    }

    .permission-description {
      font-size: 0.75rem;
      color: var(--bs-secondary);
      margin-top: 2px;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .permission-icon {
      width: 32px;
      height: 32px;
      border-radius: 6px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .permission-icon.low {
      background: var(--bs-success-subtle);
      color: var(--bs-success);
    }

    .permission-icon.medium {
      background: var(--bs-warning-subtle);
      color: var(--bs-warning);
    }

    .permission-icon.high {
      background: var(--bs-danger-subtle);
      color: var(--bs-danger);
    }

    .permission-icon.critical {
      background: var(--bs-dark);
      color: white;
    }

    .card-header:hover {
      background-color: var(--bs-gray-50);
    }

    .h6 {
      font-size: 0.875rem;
    }

    .service-stats {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
  `]
})
export class PermissionByServiceComponent implements OnInit {
  // Services
  private permissionService = inject(PermissionService);
  private roleService = inject(RoleService);

  // Icons
  readonly serverIcon = Server;
  readonly searchIcon = Search;
  readonly filterIcon = Filter;
  readonly downloadIcon = Download;
  readonly eyeIcon = Eye;
  readonly eyeOffIcon = EyeOff;
  readonly refreshCwIcon = RefreshCw;
  readonly shieldIcon = Shield;
  readonly usersIcon = Users;
  readonly keyIcon = Key;
  readonly activityIcon = Activity;

  // State signals
  loading = signal(false);
  permissions = signal<PermissionWithUsage[]>([]);
  roles = signal<RoleDto[]>([]);
  services = signal<ServiceGroup[]>([]);
  expandedServices = signal<Set<string>>(new Set());
  filters = signal<any>({});

  // Computed values
  filteredServices = computed(() => {
    const services = this.services();
    const searchTerm = this.filters().search?.toLowerCase() || '';
    const riskFilter = this.filters().riskLevel;
    const usageFilter = this.filters().usageLevel;

    return services.filter(service => {
      // Search filter
      if (searchTerm) {
        const matchesService = service.name.toLowerCase().includes(searchTerm);
        const matchesPermission = service.permissions.some(p =>
          p.name.toLowerCase().includes(searchTerm) ||
          p.description?.toLowerCase().includes(searchTerm)
        );
        if (!matchesService && !matchesPermission) return false;
      }

      // Risk filter
      if (riskFilter && service.riskLevel !== riskFilter) return false;

      // Usage filter
      if (usageFilter) {
        const avgUsage = service.permissions.reduce((sum, p) => sum + (p.usageFrequency || 0), 0) / service.permissions.length;
        switch (usageFilter) {
          case 'high': if (avgUsage < 70) return false; break;
          case 'medium': if (avgUsage < 30 || avgUsage >= 70) return false; break;
          case 'low': if (avgUsage >= 30) return false; break;
        }
      }

      return true;
    });
  });

  statisticsCards = computed(() => {
    const services = this.services();
    const totalPermissions = services.reduce((sum, s) => sum + s.permissions.length, 0);
    const totalRoles = services.reduce((sum, s) => sum + s.totalRoles, 0);
    const totalUsers = services.reduce((sum, s) => sum + s.totalUsers, 0);

    const riskCounts = services.reduce((acc, s) => {
      acc[s.riskLevel] = (acc[s.riskLevel] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    return [
      {
        title: 'Toplam Servis',
        value: services.length,
        icon: 'server',
        color: 'primary' as const,
        subtitle: `${totalPermissions} yetki`,
        clickable: true
      },
      {
        title: 'Kritik Servisler',
        value: riskCounts['critical'] || 0,
        icon: 'alert-triangle',
        color: 'danger' as const,
        subtitle: 'Yüksek riskli servisler',
        clickable: true
      },
      {
        title: 'Aktif Roller',
        value: totalRoles,
        icon: 'shield',
        color: 'info' as const,
        subtitle: 'Yetki atanmış roller',
        clickable: true
      },
      {
        title: 'Etkilenen Kullanıcı',
        value: totalUsers,
        icon: 'users',
        color: 'success' as const,
        subtitle: 'Toplam kullanıcı sayısı',
        clickable: true
      }
    ];
  });

  allServicesExpanded = computed(() => {
    const services = this.filteredServices();
    const expanded = this.expandedServices();
    return services.length > 0 && services.every(s => expanded.has(s.name));
  });

  filterFields = computed(() => [
    {
      key: 'riskLevel',
      label: 'Risk Seviyesi',
      type: 'select' as const,
      options: [
        { label: 'Tüm Seviyeler', value: '' },
        { label: 'Düşük Risk', value: 'low' },
        { label: 'Orta Risk', value: 'medium' },
        { label: 'Yüksek Risk', value: 'high' },
        { label: 'Kritik Risk', value: 'critical' }
      ]
    },
    {
      key: 'usageLevel',
      label: 'Kullanım Seviyesi',
      type: 'select' as const,
      options: [
        { label: 'Tüm Seviyeler', value: '' },
        { label: 'Yüksek Kullanım (>70%)', value: 'high' },
        { label: 'Orta Kullanım (30-70%)', value: 'medium' },
        { label: 'Düşük Kullanım (<30%)', value: 'low' }
      ]
    },
    {
      key: 'hasUsers',
      label: 'Kullanıcı Durumu',
      type: 'boolean' as const
    }
  ] as FilterField[]);

  // Header actions
  headerActions: ActionButton[] = [
    {
      key: 'risk-analysis',
      label: 'Risk Analizi',
      icon: 'alert-triangle',
      variant: 'outline-danger'
    },
    {
      key: 'usage-report',
      label: 'Kullanım Raporu',
      icon: 'activity',
      variant: 'outline-info'
    },
    {
      key: 'export-all',
      label: 'Tümünü Dışa Aktar',
      icon: 'download',
      variant: 'outline-primary'
    }
  ];

  ngOnInit() {
    this.loadData();
  }

  private loadData() {
    this.loading.set(true);

    const permissions$ = this.permissionService.getPermissionsWithUsage();
    const roles$ = this.roleService.getRoles({ includePermissions: true });

    permissions$.subscribe({
      next: (permissionsResponse) => {
        this.permissions.set((permissionsResponse || []) as PermissionWithUsage[]);

        roles$.subscribe({
          next: (rolesResponse) => {
            this.roles.set(Array.isArray(rolesResponse) ? rolesResponse : rolesResponse?.data || []);
            this.buildServiceGroups();
            this.loading.set(false);
          },
          error: (error) => {
            console.error('Failed to load roles data:', error);
            this.loading.set(false);
          }
        });
      },
      error: (error) => {
        console.error('Failed to load permissions data:', error);
        this.loading.set(false);
      }
    });
  }

  private buildServiceGroups() {
    const permissions = this.permissions();
    const roles = this.roles();

    // Group permissions by service
    const serviceMap = new Map<string, PermissionWithUsage[]>();

    permissions.forEach(permission => {
      if (!serviceMap.has(permission.service)) {
        serviceMap.set(permission.service, []);
      }
      serviceMap.get(permission.service)!.push(permission);
    });

    // Build service groups with statistics
    const services: ServiceGroup[] = Array.from(serviceMap.entries()).map(([serviceName, servicePermissions]) => {
      const totalRoles = new Set();
      const totalUsers = new Set();
      let riskScore = 0;

      servicePermissions.forEach(permission => {
        // Calculate roles and users for this service
        const permissionRoles = roles.filter(role =>
          role.permissions?.some(p => p.id === permission.id)
        );

        permissionRoles.forEach(role => {
          totalRoles.add(role.id);
          // Add users from role (if available)
          if (role.userCount) {
            for (let i = 0; i < role.userCount; i++) {
              totalUsers.add(`${role.id}_${i}`); // Mock user counting
            }
          }
        });

        // Calculate risk score
        riskScore += this.calculatePermissionRisk(permission);
      });

      const avgRiskScore = riskScore / servicePermissions.length;
      const riskLevel = this.determineRiskLevel(avgRiskScore);

      return {
        name: serviceName,
        description: this.getServiceDescription(serviceName),
        permissions: servicePermissions.sort((a, b) => a.name.localeCompare(b.name)),
        totalRoles: totalRoles.size,
        totalUsers: totalUsers.size,
        riskLevel,
        expanded: this.expandedServices().has(serviceName)
      };
    });

    this.services.set(services.sort((a, b) => a.name.localeCompare(b.name)));
  }

  private calculatePermissionRisk(permission: PermissionWithUsage): number {
    let risk = 0;

    // High usage = higher risk
    risk += (permission.usageFrequency || 0) * 0.3;

    // Many users = higher risk
    risk += Math.min((permission.userCount || 0) / 100, 1) * 40;

    // Administrative permissions = higher risk
    if (permission.name.toLowerCase().includes('admin') ||
        permission.name.toLowerCase().includes('delete') ||
        permission.name.toLowerCase().includes('manage')) {
      risk += 30;
    }

    return Math.min(risk, 100);
  }

  private determineRiskLevel(score: number): 'low' | 'medium' | 'high' | 'critical' {
    if (score >= 80) return 'critical';
    if (score >= 60) return 'high';
    if (score >= 30) return 'medium';
    return 'low';
  }

  private getServiceDescription(serviceName: string): string {
    const descriptions: Record<string, string> = {
      'Identity': 'Kimlik doğrulama ve yetkilendirme servisi',
      'User': 'Kullanıcı yönetimi ve profil servisi',
      'Admin': 'Sistem yönetimi ve konfigürasyon servisi',
      'SpeedReading': 'Hızlı okuma eğitim platformu servisi',
      'Gateway': 'API Gateway ve routing servisi',
      'Security': 'Güvenlik ve audit servisi'
    };
    return descriptions[serviceName] || `${serviceName} servisi`;
  }

  onFiltersChange(filters: any) {
    this.filters.set(filters);
  }

  onHeaderAction(event: any) {
    switch (event.action) {
      case 'risk-analysis':
        this.performRiskAnalysis();
        break;
      case 'usage-report':
        this.generateUsageReport();
        break;
      case 'export-all':
        this.exportAllServices();
        break;
    }
  }

  onStatisticCardClick(config: any) {
    // Handle statistic card clicks
    if (config.title === 'Kritik Servisler') {
      this.filters.set({ ...this.filters(), riskLevel: 'critical' });
    }
  }

  toggleService(serviceName: string) {
    const expanded = this.expandedServices();
    const newExpanded = new Set(expanded);

    if (newExpanded.has(serviceName)) {
      newExpanded.delete(serviceName);
    } else {
      newExpanded.add(serviceName);
    }

    this.expandedServices.set(newExpanded);
  }

  expandAllServices() {
    const allExpanded = this.allServicesExpanded();
    if (allExpanded) {
      this.expandedServices.set(new Set());
    } else {
      const allServiceNames = this.filteredServices().map(s => s.name);
      this.expandedServices.set(new Set(allServiceNames));
    }
  }

  viewPermissionDetail(permission: PermissionWithUsage) {
    // Navigate to permission detail or show modal
    console.log('Viewing permission detail:', permission);
  }

  getServiceRiskClass(service: ServiceGroup): string {
    switch (service.riskLevel) {
      case 'critical': return 'badge bg-dark';
      case 'high': return 'badge bg-danger';
      case 'medium': return 'badge bg-warning';
      default: return 'badge bg-success';
    }
  }

  getServiceRiskText(service: ServiceGroup): string {
    switch (service.riskLevel) {
      case 'critical': return 'Kritik';
      case 'high': return 'Yüksek';
      case 'medium': return 'Orta';
      default: return 'Düşük';
    }
  }

  getPermissionCardClass(permission: PermissionWithUsage): string {
    const riskLevel = permission.riskLevel;
    return riskLevel === 'high' || riskLevel === 'critical' ? `${riskLevel}-risk` : '';
  }

  getPermissionIconClass(permission: PermissionWithUsage): string {
    return `permission-icon ${permission.riskLevel}`;
  }

  getPermissionRiskClass(permission: PermissionWithUsage): string {
    switch (permission.riskLevel) {
      case 'critical': return 'badge bg-dark';
      case 'high': return 'badge bg-danger';
      case 'medium': return 'badge bg-warning';
      default: return 'badge bg-success';
    }
  }

  getPermissionRiskText(permission: PermissionWithUsage): string {
    return this.getServiceRiskText({ riskLevel: permission.riskLevel } as ServiceGroup);
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('tr-TR', {
      month: 'short',
      day: 'numeric'
    }).format(new Date(date));
  }

  clearFilters() {
    this.filters.set({});
  }

  refreshUsageData() {
    this.loadData();
  }

  exportServiceReport() {
    this.permissionService.exportServiceReport(this.filters()).subscribe({
      next: () => {
        console.log('Service report export completed successfully');
      },
      error: (error) => {
        console.error('Export service report failed:', error);
      }
    });
  }

  exportServicePermissions(serviceName: string) {
    this.permissionService.exportServicePermissions(serviceName).subscribe({
      next: () => {
        console.log('Service permissions export completed successfully');
      },
      error: (error) => {
        console.error('Export service permissions failed:', error);
      }
    });
  }

  viewServiceMatrix(serviceName: string) {
    // Navigate to matrix view filtered by service
    console.log('Viewing service matrix:', serviceName);
  }

  analyzeServiceUsage(serviceName: string) {
    // Show service usage analytics
    console.log('Analyzing service usage:', serviceName);
  }

  private performRiskAnalysis() {
    this.permissionService.generateRiskAnalysis().subscribe({
      next: () => {
        console.log('Risk analysis completed successfully');
      },
      error: (error) => {
        console.error('Risk analysis failed:', error);
      }
    });
  }

  private generateUsageReport() {
    this.permissionService.generateUsageReport().subscribe({
      next: () => {
        console.log('Usage report generated successfully');
      },
      error: (error) => {
        console.error('Usage report generation failed:', error);
      }
    });
  }

  private exportAllServices() {
    this.permissionService.exportAllServices().subscribe({
      next: () => {
        console.log('All services export completed successfully');
      },
      error: (error) => {
        console.error('Export all services failed:', error);
      }
    });
  }
}