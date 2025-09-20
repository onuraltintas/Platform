import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CardModule, GridModule, ButtonModule, ProgressModule } from '@coreui/angular';
import { KpiCardComponent } from './components/kpi-card/kpi-card.component';
import { ActivityListComponent } from './components/activity-list/activity-list.component';
import { FilterBarComponent, DateRange } from './components/filter-bar/filter-bar.component';
import { ChartWidgetComponent } from './components/chart-widget/chart-widget.component';
import { DashboardService } from './data-access/dashboard.service';
import { ToastrService } from 'ngx-toastr';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    CardModule,
    GridModule,
    ButtonModule,
    ProgressModule,
    KpiCardComponent,
    ActivityListComponent,
    FilterBarComponent,
    ChartWidgetComponent
  ],
  template: `
    <c-container fluid class="dashboard-container">
      <!-- Dashboard Header -->
      <c-row class="mb-4">
        <c-col>
          <div class="d-flex justify-content-between align-items-center">
            <div>
              <h1 class="display-6 mb-2 text-primary">Dashboard</h1>
              <p class="text-muted mb-0 lead">OnAl Yazılım Admin Paneline Hoş Geldiniz</p>
            </div>
            <div class="text-end">
              <span class="badge bg-light text-dark px-3 py-2">
                <i class="bi bi-clock me-2"></i>
                Son Güncelleme: {{ currentDate | date:'dd/MM/yyyy HH:mm' }}
              </span>
            </div>
          </div>
        </c-col>
      </c-row>

      <!-- Filters -->
      <c-row class="mb-4">
        <c-col>
          <app-filter-bar
            [range]="range"
            [segment]="segment"
            [start]="start"
            [end]="end"
            (rangeChange)="onRangeChange($event)"
            (segmentChange)="onSegmentChange($event)"
            (customRangeChange)="onCustomRange($event)"
          ></app-filter-bar>
        </c-col>
      </c-row>

      <!-- KPI Cards -->
      <c-row class="mb-4 g-3">
        <c-col lg="3" md="6">
          <app-kpi-card
            title="Toplam Kullanıcı"
            [value]="stats.totalUsers"
            trendText="+12% bu ay"
            trendIcon="bi bi-arrow-up"
            trend="up"
            icon="bi bi-people"
            gradient="primary"/>
        </c-col>
        <c-col lg="3" md="6">
          <app-kpi-card
            title="Aktif Kullanıcı"
            [value]="stats.activeUsers"
            trendText="+8% bu hafta"
            trendIcon="bi bi-arrow-up"
            trend="up"
            icon="bi bi-person-check"
            gradient="light"/>
        </c-col>
        <c-col lg="3" md="6">
          <app-kpi-card
            title="Toplam Grup"
            [value]="stats.totalGroups"
            trendText="%0 değişim"
            trendIcon="bi bi-dash"
            trend="neutral"
            icon="bi bi-collection"
            gradient="info"/>
        </c-col>
        <c-col lg="3" md="6">
          <app-kpi-card
            title="Toplam Rol"
            [value]="stats.totalRoles"
            trendText="+2 yeni rol"
            trendIcon="bi bi-arrow-up"
            trend="up"
            icon="bi bi-shield-check"
            gradient="warning"/>
        </c-col>
      </c-row>

      <!-- Main Content Row -->
      <c-row class="mb-4 g-3">
        <!-- Quick Actions -->
        <c-col lg="4">
          <c-card class="h-100 border-0 shadow-sm">
            <c-card-header class="bg-transparent border-0 pb-0">
              <h5 class="card-title d-flex align-items-center mb-0">
                <i class="bi bi-lightning-charge-fill text-primary me-2"></i>
                Hızlı İşlemler
              </h5>
            </c-card-header>
            <c-card-body>
              <div class="d-grid gap-2">
                <button
                  cButton
                  color="primary"
                  variant="outline"
                  class="d-flex align-items-center justify-content-start"
                  routerLink="/users/create">
                  <i class="bi bi-person-plus me-2"></i>
                  Yeni Kullanıcı Ekle
                </button>
                <button
                  cButton
                  color="success"
                  variant="outline"
                  class="d-flex align-items-center justify-content-start"
                  routerLink="/groups/create">
                  <i class="bi bi-people me-2"></i>
                  Yeni Grup Oluştur
                </button>
                <button
                  cButton
                  color="info"
                  variant="outline"
                  class="d-flex align-items-center justify-content-start"
                  routerLink="/users">
                  <i class="bi bi-list-check me-2"></i>
                  Kullanıcıları Yönet
                </button>
                <button
                  cButton
                  color="warning"
                  variant="outline"
                  class="d-flex align-items-center justify-content-start"
                  routerLink="/settings">
                  <i class="bi bi-gear me-2"></i>
                  Sistem Ayarları
                </button>
              </div>
            </c-card-body>
          </c-card>
        </c-col>

        <!-- Trend Chart -->
        <c-col lg="4">
          <div style="min-height: 260px">
            <app-chart-widget
              title="Aktif Kullanıcılar"
              [subtitle]="chartSubtitle"
              [config]="trendConfig"
            />
          </div>
        </c-col>

        <!-- Doughnut Chart -->
        <c-col lg="4">
          <div style="min-height: 260px">
            <app-chart-widget
            title="Trafik Dağılımı"
            [subtitle]="chartSubtitle"
            [config]="doughnutConfig"
            />
          </div>
        </c-col>
      </c-row>

      <!-- Recent Activities -->
      <c-row>
        <c-col>
          <app-activity-list [items]="recentActivities"></app-activity-list>
        </c-col>
      </c-row>
    </c-container>
  `,
  styles: [`
    .dashboard-container {
      padding: 1.5rem;
      background: var(--bs-content-bg);
      min-height: calc(100vh - 120px);
    }

    .stat-card {
      transform: translateY(0);
      transition: all 0.3s ease;
      overflow: hidden;
      position: relative;
    }

    .stat-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
    }

    .stat-icon {
      position: relative;
      z-index: 1;
    }

    .stat-card::before {
      content: '';
      position: absolute;
      top: -50%;
      right: -20%;
      width: 120px;
      height: 120px;
      background: rgba(255, 255, 255, 0.1);
      border-radius: 50%;
      z-index: 0;
    }

    .bg-gradient-primary {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
    }

    .bg-gradient-success {
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
    }

    .bg-gradient-info {
      background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%);
    }

    .bg-gradient-warning {
      background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
    }

    .text-white-50 {
      color: rgba(255, 255, 255, 0.75) !important;
    }

    .text-white-75 {
      color: rgba(255, 255, 255, 0.85) !important;
    }

    .shadow-sm {
      box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075) !important;
    }

    .card {
      transition: all 0.3s ease;
      border-radius: 0.75rem;
    }

    .card:hover {
      transform: translateY(-1px);
      box-shadow: 0 0.25rem 0.75rem rgba(0, 0, 0, 0.1) !important;
    }

    .btn {
      transition: all 0.3s ease;
      font-weight: 500;
      border-radius: 0.5rem;
    }

    .btn:hover {
      transform: translateY(-1px);
    }

    .display-6 {
      font-size: 1.75rem;
      font-weight: 600;
      line-height: 1.2;
    }

    .lead {
      font-size: 1.125rem;
      font-weight: 300;
    }

    .text-truncate {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .min-width-0 {
      min-width: 0;
    }

    .bg-light {
      background-color: var(--bs-gray-100) !important;
      border: 1px solid var(--bs-gray-200);
    }

    @media (max-width: 768px) {
      .dashboard-container {
        padding: 1rem;
      }

      .stat-card .fw-bold {
        font-size: 1.5rem;
      }

      .display-6 {
        font-size: 1.5rem;
      }

      .lead {
        font-size: 1rem;
      }
    }

    @media (max-width: 576px) {
      .dashboard-container {
        padding: 0.75rem;
      }

      .text-end {
        text-align: center !important;
        margin-top: 1rem;
      }

      .d-flex.justify-content-between {
        flex-direction: column;
        align-items: flex-start !important;
      }
    }
  `]
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly toastr = inject(ToastrService);

  currentDate = new Date();
  range: DateRange = '7d';
  segment: string = 'all';
  chartSubtitle = 'Son 7 gün';
  start?: string;
  end?: string;
  loading = false;

  stats = {
    totalUsers: 0,
    activeUsers: 0,
    totalGroups: 0,
    totalRoles: 0
  };

  systemStatus = {
    uptime: '15d 8h',
    memoryUsage: 68,
    activeConnections: 45
  };

  dailyStats = {
    newUsers: 0,
    activeToday: 0,
    logins: 0,
    actions: 0
  };

  recentActivities = [
    {
      type: 'user_created',
      title: 'Yeni kullanıcı eklendi',
      description: 'Ahmet Yılmaz sisteme katıldı',
      timestamp: new Date(Date.now() - 3 * 60 * 1000) // 3 dakika önce
    },
    {
      type: 'group_updated',
      title: 'Grup güncellendi',
      description: 'Muhasebe departmanına 3 yeni üye eklendi',
      timestamp: new Date(Date.now() - 12 * 60 * 1000) // 12 dakika önce
    },
    {
      type: 'role_assigned',
      title: 'Yetki atandı',
      description: 'Moderatör yetkisi Zehra Kaya\'ya verildi',
      timestamp: new Date(Date.now() - 45 * 60 * 1000) // 45 dakika önce
    },
    {
      type: 'system_update',
      title: 'Sistem güncellendi',
      description: 'Güvenlik yamaları uygulandı',
      timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000) // 2 saat önce
    },
    {
      type: 'backup_completed',
      title: 'Yedekleme tamamlandı',
      description: 'Günlük otomatik yedekleme başarıyla tamamlandı',
      timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000) // 6 saat önce
    }
  ];

  trendConfig = {
    type: 'line',
    data: {
      labels: [],
      datasets: [{
        label: 'Aktif Kullanıcı',
        data: [],
        borderColor: '#3b82f6',
        backgroundColor: 'rgba(59,130,246,0.2)',
        tension: 0.3,
        fill: true
      }]
    },
    options: { responsive: true, maintainAspectRatio: false }
  } as any;

  barConfig = {
    type: 'bar',
    data: {
      labels: [],
      datasets: [{
        label: 'İşlem',
        data: [],
        backgroundColor: '#06b6d4'
      }]
    },
    options: { responsive: true, maintainAspectRatio: false }
  } as any;

  doughnutConfig = {
    type: 'doughnut',
    data: {
      labels: ['Web', 'Mobil', 'API'],
      datasets: [{
        label: 'Trafik',
        data: [45, 35, 20],
        backgroundColor: ['#3b82f6','#10b981','#f59e0b'],
        borderColor: ['#3b82f6','#10b981','#f59e0b']
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: { position: 'right' } }
    }
  } as any;

  ngOnInit(): void {
    this.fetchAll();
  }

  onRangeChange(r: DateRange): void {
    this.range = r;
    this.chartSubtitle = r === 'today' ? 'Bugün' : r === '7d' ? 'Son 7 gün' : 'Son 30 gün';
    this.fetchAll();
  }

  onSegmentChange(s: string): void {
    this.segment = s;
    this.fetchAll();
  }

  onCustomRange(v: { start?: string; end?: string }): void {
    this.start = v.start;
    this.end = v.end;
    this.chartSubtitle = this.start && this.end ? `${this.start} — ${this.end}` : 'Özel aralık';
    // Custom aralık tamamlanınca veri çek
    if (this.range === 'custom' && this.start && this.end) {
      this.fetchAll();
    }
  }

  private fetchAll(): void {
    this.loading = true;
    const range = this.range;
    const segment = this.segment;
    const start = this.start;
    const end = this.end;

    forkJoin({
      summary: this.dashboardService.getSummary(range, segment, start, end).pipe(catchError(() => of(null))),
      trend: this.dashboardService.getTrend(range, segment, start, end).pipe(catchError(() => of([]))),
      actions: this.dashboardService.getActions(range, segment, start, end).pipe(catchError(() => of([])))
    }).subscribe(({ summary, trend, actions }) => {
      try {
        if (summary) {
          this.stats.totalUsers = summary.totalUsers;
          this.stats.activeUsers = summary.activeUsers;
          this.stats.totalGroups = summary.totalGroups;
          this.stats.totalRoles = summary.totalRoles;
          if (summary.dailyStats) {
            this.dailyStats = summary.dailyStats;
          }
        }

        if (trend && Array.isArray(trend)) {
          this.trendConfig = {
            ...this.trendConfig,
            data: {
              ...this.trendConfig.data,
              labels: trend.map(p => p.label),
              datasets: [{ ...this.trendConfig.data.datasets[0], data: trend.map(p => p.value) }]
            }
          } as any;
        }

        if (actions && Array.isArray(actions)) {
          this.barConfig = {
            ...this.barConfig,
            data: {
              ...this.barConfig.data,
              labels: actions.map(p => p.label),
              datasets: [{ ...this.barConfig.data.datasets[0], data: actions.map(p => p.value) }]
            }
          } as any;
        }
      } catch (err) {
        this.toastr.error('Dashboard verileri işlenirken hata oluştu');
      } finally {
        this.loading = false;
      }
    }, _ => {
      this.loading = false;
      this.toastr.error('Dashboard verileri alınamadı');
    });
  }

  getActivityIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'user_created': 'bi bi-person-plus',
      'group_updated': 'bi bi-people',
      'role_assigned': 'bi bi-shield-check',
      'system_update': 'bi bi-arrow-up-circle',
      'backup_completed': 'bi bi-cloud-check',
      'default': 'bi bi-info-circle'
    };
    return icons[type] || icons['default'];
  }

  getActivityColor(type: string): string {
    const colors: { [key: string]: string } = {
      'user_created': 'success',
      'group_updated': 'info',
      'role_assigned': 'warning',
      'system_update': 'primary',
      'backup_completed': 'success',
      'default': 'secondary'
    };
    return colors[type] || colors['default'];
  }

  getActivityBadgeColor(type: string): string {
    const colors: { [key: string]: string } = {
      'user_created': 'success-subtle',
      'group_updated': 'info-subtle',
      'role_assigned': 'warning-subtle',
      'system_update': 'primary-subtle',
      'backup_completed': 'success-subtle',
      'default': 'secondary-subtle'
    };
    return colors[type] || colors['default'];
  }

  getActivityTypeText(type: string): string {
    const texts: { [key: string]: string } = {
      'user_created': 'Kullanıcı',
      'group_updated': 'Grup',
      'role_assigned': 'Yetki',
      'system_update': 'Sistem',
      'backup_completed': 'Yedek',
      'default': 'Diğer'
    };
    return texts[type] || texts['default'];
  }

  formatActivityDate(date: Date): string {
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));

    if (diffInMinutes < 1) return 'Şimdi';
    if (diffInMinutes < 60) return `${diffInMinutes} dk önce`;

    const diffInHours = Math.floor(diffInMinutes / 60);
    if (diffInHours < 24) return `${diffInHours} sa önce`;

    const diffInDays = Math.floor(diffInHours / 24);
    return `${diffInDays} gün önce`;
  }
}