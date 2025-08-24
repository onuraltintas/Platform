import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';
import { ReadingProgressApiService } from '../reading/services/reading-progress-api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dark-dashboard">
      <!-- Header -->
      <header class="dashboard-header">
        <div class="user-section">
          <div class="user-avatar">
            <i class="bi bi-person-circle"></i>
          </div>
          <div class="user-info">
            <h2>{{ currentUser?.firstName || 'Kullanıcı' }}</h2>
            <p>{{ currentUser?.email || 'Hızlı Okuma Öğrencisi' }}</p>
          </div>
        </div>
        <div class="header-actions">
          <button class="notification-btn">
            <i class="bi bi-bell"></i>
            <span class="badge">3</span>
          </button>
          <button class="logout-btn" (click)="logout()">
            <i class="bi bi-box-arrow-right"></i>
          </button>
        </div>
      </header>

      <!-- Navigation -->
      <nav class="dashboard-nav">
        <button 
          *ngFor="let nav of navigation" 
          class="nav-btn"
          [class.active]="activeNav === nav.id"
          (click)="setActiveNav(nav.id)">
          <i [class]="nav.icon"></i>
          <span>{{ nav.title }}</span>
        </button>
      </nav>

      <!-- Main Content -->
      <main class="dashboard-main">
        <!-- Period Filters -->
        <div class="period-filters">
          <button class="period-btn" [class.active]="selectedPeriod === 'day'" (click)="onPeriodChange('day')">Gün</button>
          <button class="period-btn" [class.active]="selectedPeriod === 'week'" (click)="onPeriodChange('week')">Hafta</button>
          <button class="period-btn" [class.active]="selectedPeriod === 'month'" (click)="onPeriodChange('month')">Ay</button>
          <button class="period-btn" [class.active]="selectedPeriod === 'year'" (click)="onPeriodChange('year')">Yıl</button>
        </div>

        <!-- Stats Cards -->
        <section class="stats-section">
          <div class="stat-card speed-card" (click)="goToStatistics('reading')" style="cursor:pointer">
            <div class="stat-icon">
              <i class="bi bi-lightning-charge"></i>
            </div>
            <div class="stat-content">
              <h3>{{ stats.speed }}</h3>
              <p>WPM Ortalama</p>
              <span class="change positive">+12%</span>
            </div>
          </div>

          <div class="stat-card comprehension-card" (click)="goToStatistics('reading')" style="cursor:pointer">
            <div class="stat-icon">
              <i class="bi bi-bullseye"></i>
            </div>
            <div class="stat-content">
              <h3>{{ stats.comprehension }}%</h3>
              <p>Anlama Oranı</p>
              <span class="change positive">+8%</span>
            </div>
          </div>

          <div class="stat-card streak-card" (click)="goToStatistics('general')" style="cursor:pointer">
            <div class="stat-icon">
              <i class="bi bi-fire"></i>
            </div>
            <div class="stat-content">
              <h3>{{ stats.streak }}</h3>
              <p>Günlük Seri</p>
              <span class="change neutral">Aynı</span>
            </div>
          </div>

          <div class="stat-card level-card" (click)="goToStatistics('general')" style="cursor:pointer">
            <div class="stat-icon">
              <i class="bi bi-trophy"></i>
            </div>
            <div class="stat-content">
              <h3>Seviye {{ stats.level }}</h3>
              <p>Mevcut Seviye</p>
              <div class="progress-bar">
                <div class="progress" [style.width.%]="stats.progress"></div>
              </div>
            </div>
          </div>
        </section>

        <!-- Mini Charts Row -->
        <section class="trends-section" *ngIf="wpmPath || compPath">
          <div class="trend-card">
            <div class="trend-header">
              <span><i class="bi bi-activity"></i> WPM Trend</span>
            </div>
            <svg [attr.viewBox]="'0 0 ' + sparklineWidth + ' ' + sparklineHeight" [attr.width]="sparklineWidth" [attr.height]="sparklineHeight" class="sparkline">
              <path [attr.d]="wpmPath" class="sparkline-path wpm"></path>
            </svg>
          </div>
          <div class="trend-card">
            <div class="trend-header">
              <span><i class="bi bi-bullseye"></i> Anlama Trend</span>
            </div>
            <svg [attr.viewBox]="'0 0 ' + sparklineWidth + ' ' + sparklineHeight" [attr.width]="sparklineWidth" [attr.height]="sparklineHeight" class="sparkline">
              <path [attr.d]="compPath" class="sparkline-path comp"></path>
            </svg>
          </div>
        </section>

        <!-- Content Area -->
        <section class="content-area">
          <div class="content-card" [ngSwitch]="activeNav">
            
            <!-- Dashboard Content -->
            <div *ngSwitchCase="'dashboard'" class="dashboard-content">
              <h3><i class="bi bi-speedometer2"></i> Genel Bakış</h3>
              
              <!-- Recent Activities -->
              <div class="activities-section">
                <h4>Son Aktiviteler</h4>
                <div class="activity-list">
                  <div *ngFor="let activity of recentActivities" class="activity-item">
                    <div class="activity-icon">
                      <i [class]="activity.icon"></i>
                    </div>
                    <div class="activity-info">
                      <h5>{{ activity.title }}</h5>
                      <p>{{ activity.description }}</p>
                      <small>{{ activity.time }}</small>
                    </div>
                    <div *ngIf="activity.score" class="activity-score">
                      {{ activity.score }}%
                    </div>
                  </div>
                </div>
              </div>

              <!-- Recent Sessions from DB -->
              <div class="activities-section" *ngIf="recentSessions && recentSessions.length">
                <h4>Son Oturumlar</h4>
                <div class="activity-list">
                  <div *ngFor="let s of recentSessions" class="activity-item">
                    <div class="activity-icon">
                      <i class="bi bi-clock-history"></i>
                    </div>
                    <div class="activity-info">
                      <h5>{{ s.startTime | date:'short' }}</h5>
                      <p>WPM: {{ s.wordsPerMinute || 0 }} • Süre: {{ (s.totalDuration || 0) / 1000 | number:'1.0-0' }} sn</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Reading Content -->
            <div *ngSwitchCase="'reading'" class="reading-content">
              <h3><i class="bi bi-book"></i> Okuma Modülü</h3>
              <div class="content-placeholder">
                <i class="bi bi-book-half placeholder-icon"></i>
                <p>Okuma egzersizleri ve testleri</p>
                <button class="primary-btn" (click)="navigateToReading()">
                  <i class="bi bi-play-circle"></i>
                  Okuma Başlat
                </button>
              </div>
            </div>

            <!-- Exercises Content -->
            <div *ngSwitchCase="'exercises'" class="exercises-content">
              <h3><i class="bi bi-puzzle"></i> Egzersizler</h3>
              <div class="content-placeholder">
                <i class="bi bi-puzzle placeholder-icon"></i>
                <p>Hız ve anlama egzersizleri</p>
                <button class="primary-btn">
                  <i class="bi bi-play-circle"></i>
                  Egzersiz Başlat
                </button>
              </div>
            </div>

            <!-- Statistics Content -->
            <div *ngSwitchCase="'statistics'" class="statistics-content">
              <h3><i class="bi bi-graph-up"></i> İstatistikler</h3>
              <div class="content-placeholder">
                <i class="bi bi-bar-chart placeholder-icon"></i>
                <p>Detaylı performans analizleri</p>
              </div>
            </div>

          </div>
        </section>
        
      </main>

      <!-- Bottom Stats -->
      <footer class="bottom-stats">
        <div class="stats-tabs">
          <button 
            *ngFor="let tab of statsTabs" 
            class="stats-tab"
            [class.active]="activeStatsTab === tab.id"
            (click)="setActiveStatsTab(tab.id)">
            <i [class]="tab.icon"></i>
            <span>{{ tab.title }}</span>
            <span *ngIf="tab.count" class="tab-count">{{ tab.count }}</span>
          </button>
        </div>
        
        <div class="stats-content" [ngSwitch]="activeStatsTab">
          <div *ngSwitchCase="'general'" class="general-stats">
            <div class="mini-stat">
              <i class="bi bi-book"></i>
              <span class="value">{{ stats.totalTexts }}</span>
              <span class="label">Okunan Metin</span>
            </div>
            <div class="mini-stat">
              <i class="bi bi-puzzle"></i>
              <span class="value">{{ stats.totalExercises }}</span>
              <span class="label">Egzersiz</span>
            </div>
            <div class="mini-stat">
              <i class="bi bi-clock"></i>
              <span class="value">{{ stats.totalHours }}h</span>
              <span class="label">Toplam Süre</span>
            </div>
          </div>
          <div *ngSwitchCase="'reading'" class="general-stats">
            <div class="mini-stat">
              <i class="bi bi-lightning-charge"></i>
              <span class="value">{{ exerciseReading?.averageWPM || 0 }}</span>
              <span class="label">Ortalama WPM</span>
            </div>
            <div class="mini-stat">
              <i class="bi bi-list-check"></i>
              <span class="value">{{ exerciseReading?.totalCount || 0 }}</span>
              <span class="label">Oturum</span>
            </div>
            <div class="mini-stat">
              <i class="bi bi-clock"></i>
              <span class="value">{{ (exerciseReading?.totalDurationSeconds || 0) / 60 | number:'1.0-0' }} dk</span>
              <span class="label">Süre</span>
            </div>
          </div>
          <div *ngSwitchCase="'muscle'" class="general-stats">
            <div class="mini-stat">
              <i class="bi bi-graph-up"></i>
              <span class="value">{{ exerciseMuscle?.averageScore || 0 }}</span>
              <span class="label">Ortalama Skor</span>
            </div>
            <div class="mini-stat">
              <i class="bi bi-list-check"></i>
              <span class="value">{{ exerciseMuscle?.totalCount || 0 }}</span>
              <span class="label">Egzersiz</span>
            </div>
            <div class="mini-stat">
              <i class="bi bi-clock"></i>
              <span class="value">{{ (exerciseMuscle?.totalDurationSeconds || 0) / 60 | number:'1.0-0' }} dk</span>
              <span class="label">Süre</span>
            </div>
          </div>
          <div *ngSwitchCase="'goals'" class="general-stats">
            <div class="mini-stat" *ngFor="let g of goals">
              <i class="bi" [ngClass]="g.icon"></i>
              <div>
                <div class="value">{{ g.value }} / {{ g.target }} {{ g.unit }}</div>
                <div class="label">{{ g.title }}</div>
                <div class="progress-bar" style="margin-top:6px">
                  <div class="progress" [style.width.%]="g.progress"></div>
                </div>
              </div>
            </div>
            <div class="mini-stat" *ngFor="let b of badges">
              <i class="bi" [ngClass]="b.icon" [style.color]="b.earned ? '#10b981' : '#8892aa'"></i>
              <div>
                <div class="value">{{ b.name }}</div>
                <div class="label">{{ b.desc }}</div>
              </div>
              <span class="badge" [style.background]="b.earned ? 'var(--accent-green)' : 'var(--dark-bg-hover)'" [style.color]="b.earned ? '#fff' : 'var(--dark-text-muted)'">{{ b.earned ? 'Kazanıldı' : 'Kilitsiz' }}</span>
            </div>
          </div>
        </div>
      </footer>
    </div>
  `,
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  currentUser: any;
  activeNav = 'dashboard';
  activeStatsTab = 'general';
  isLoading = true;
  errorMessage: string | null = null;

  readingSummary: any = null;
  readingStats: any = null;
  exerciseReading: any = null;
  exerciseMuscle: any = null;
  recentSessions: any[] = [];
  // Filters & mini charts
  selectedPeriod: 'day' | 'week' | 'month' | 'year' = 'week';
  sparklineWidth = 220;
  sparklineHeight = 48;
  wpmPath: string = '';
  compPath: string = '';
  // Goals & Badges
  goals: any[] = [];
  badges: any[] = [];

  navigation = [
    { id: 'dashboard', title: 'Dashboard', icon: 'bi bi-speedometer2' },
    { id: 'reading', title: 'Okuma', icon: 'bi bi-book' },
    { id: 'muscle', title: 'Kas Egzersizi', icon: 'bi bi-eye' }
  ];

  statsTabs = [
    { id: 'general', title: 'Genel', icon: 'bi bi-info-circle', count: 4 },
    { id: 'reading', title: 'Hızlı Okuma', icon: 'bi bi-graph-up' },
    { id: 'muscle', title: 'Kas', icon: 'bi bi-bar-chart' },
    { id: 'goals', title: 'Hedefler', icon: 'bi bi-award' }
  ];

  stats = {
    speed: 245,
    comprehension: 87,
    streak: 5,
    level: 3,
    progress: 65,
    totalTexts: 42,
    totalExercises: 28,
    totalHours: 12.5
  };

  recentActivities = [
    {
      title: 'Okuma Oturumu',
      description: 'JavaScript Temelleri - 245 WPM',
      time: '2 saat önce',
      icon: 'bi bi-check-circle text-success',
      score: 87
    },
    {
      title: 'Hız Egzersizi',
      description: 'Göz Hareketi Egzersizi',
      time: '5 saat önce',
      icon: 'bi bi-puzzle text-warning',
      score: 92
    },
    {
      title: 'Başarım',
      description: 'İlk Adım başarımı kazanıldı',
      time: '1 gün önce',
      icon: 'bi bi-trophy text-success',
      score: null
    }
  ];

  constructor(
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService,
    private readingApi: ReadingProgressApiService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUserValue();
    if (!this.currentUser) {
      this.router.navigate(['/auth/login']);
    }
    else {
      this.loadData();
      // 5) Basit gerçek-zamanlı otomatik yenileme (her 2 dakikada bir)
      setInterval(() => {
        this.loadData();
      }, 120000);
    }
  }

  setActiveNav(navId: string): void {
    this.activeNav = navId;
  }

  setActiveStatsTab(tabId: string): void {
    this.activeStatsTab = tabId;
  }

  navigateToReading(): void {
    this.router.navigate(['/reading']);
  }

  navigateToMuscle(): void {
    // Şimdilik okuma arayüzüne yönlendiriyoruz; kas modu ileride ayrı route alacak
    this.router.navigate(['/reading']);
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/home']);
      },
      error: (error) => {
        console.error('Logout error:', error);
        this.router.navigate(['/home']);
      }
    });
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = null;

    const userId: string = this.currentUser?.id;

    this.readingApi.getDashboardSummary(userId, this.selectedPeriod).subscribe({
      next: (data) => {
        this.readingSummary = data?.summary;
        this.readingStats = data?.stats;
        this.recentSessions = data?.recentSessions || [];
        this.computeSparklinePaths(this.readingStats);

        // Üst kartlar
        const summary = this.readingSummary;
        const stats = this.readingStats;
        this.stats.speed = summary?.averageWPM ?? this.stats.speed;
        this.stats.streak = summary?.totalSessions ?? this.stats.streak;
        this.stats.level = this.stats.level; // Seviyeyi şimdilik yerinde bırak
        this.stats.progress = Math.max(0, Math.min(100, (summary?.improvementRate ?? 0)));

        // Anlama oranı: son trend değeri
        const lastComprehension = Array.isArray(stats?.comprehensionTrend) && stats?.comprehensionTrend.length > 0
          ? (stats.comprehensionTrend[stats.comprehensionTrend.length - 1]?.comprehension ?? 0)
          : 0;
        this.stats.comprehension = Math.round(Number(lastComprehension) as number);

        // Alt genel istatistikler
        this.stats.totalExercises = summary?.totalSessions ?? this.stats.totalExercises;
        const distinctTextCount = Array.isArray(this.recentSessions)
          ? new Set(this.recentSessions.map((x: any) => x.textId).filter(Boolean)).size
          : 0;
        this.stats.totalTexts = distinctTextCount || this.stats.totalTexts;
        this.stats.totalHours = Math.round(((summary?.totalReadingTime ?? 0) / 3600) * 10) / 10;

        // Son aktiviteler
        this.recentActivities = (summary?.recentSessions || []).map((s: any) => ({
          title: 'Okuma Oturumu',
          description: `${new Date(s.sessionStartDate).toLocaleString()} - ${s.wpm ?? 0} WPM`,
          time: s.sessionEndDate ? new Date(s.sessionEndDate).toLocaleString() : 'Devam etti',
          icon: 'bi bi-check-circle text-success',
          score: s.comprehensionScore ?? null
        }));

        // Per-egzersiz istatistiklerini de getirelim
        this.exerciseReading = data?.readingExercise || null;
        this.exerciseMuscle = data?.muscleExercise || null;
        this.updateGoalsAndBadges();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Dashboard data load error:', err);
        this.errorMessage = 'Veriler yüklenemedi';
        this.toastr.error('Dashboard verileri yüklenemedi');
        this.isLoading = false;
      }
    });
  }

  onPeriodChange(period: 'day' | 'week' | 'month' | 'year'): void {
    if (this.selectedPeriod === period) return;
    this.selectedPeriod = period;
    this.loadData();
  }

  goToStatistics(view: 'general' | 'reading' | 'muscle' = 'general') {
    this.router.navigate(['/statistics'], { queryParams: { period: this.selectedPeriod, view } });
  }

  private computeSparklinePaths(stats: any): void {
    const wpmSeries = Array.isArray(stats?.wpmTrend) ? stats.wpmTrend.map((p: any) => Number(p.wpm ?? p.value ?? 0)) : [];
    const compSeries = Array.isArray(stats?.comprehensionTrend) ? stats.comprehensionTrend.map((p: any) => Number(p.comprehension ?? p.value ?? 0)) : [];
    this.wpmPath = this.buildSparklinePath(wpmSeries);
    this.compPath = this.buildSparklinePath(compSeries);
  }

  private buildSparklinePath(values: number[]): string {
    if (!values || values.length === 0) return '';
    const w = this.sparklineWidth;
    const h = this.sparklineHeight;
    const maxVal = Math.max(...values, 1);
    const minVal = Math.min(...values, 0);
    const range = Math.max(maxVal - minVal, 1);
    const stepX = values.length > 1 ? w / (values.length - 1) : w;

    const points = values.map((v, i) => {
      const x = Math.round(i * stepX);
      const y = Math.round(h - ((v - minVal) / range) * h);
      return { x, y };
    });

    const d = points.reduce((acc, p, i) => acc + (i === 0 ? `M${p.x},${p.y}` : ` L${p.x},${p.y}`), '');
    return d;
  }

  private updateGoalsAndBadges(): void {
    const summary = this.readingSummary || {};
    const stats = this.readingStats || {};
    const totalSessionsOverall = Number(summary.totalSessions || 0);
    const avgWpmOverall = Number(summary.averageWPM || 0);
    const totalReadingSecondsOverall = Number(summary.totalReadingTime || 0);

    const period = this.selectedPeriod;
    const periodSessions = Number(stats.totalSessions || 0);
    const periodMinutesTarget = this.getPeriodMinutesTarget(period);

    const periodMinutes = Math.round((summary.totalReadingTime || 0) / 60);

    const weeklySessionsTarget = period === 'week' ? 5 : (period === 'day' ? 1 : period === 'month' ? 20 : 200);
    const speedTarget = 350;

    this.goals = [
      { id: 'sessions', title: 'Dönem Oturum Hedefi', value: periodSessions, target: weeklySessionsTarget, unit: 'oturum', icon: 'bi-list-check', progress: Math.min(100, Math.round((periodSessions / Math.max(1, weeklySessionsTarget)) * 100)) },
      { id: 'minutes', title: 'Okuma Süresi Hedefi', value: periodMinutes, target: periodMinutesTarget, unit: 'dk', icon: 'bi-clock', progress: Math.min(100, Math.round((periodMinutes / Math.max(1, periodMinutesTarget)) * 100)) },
      { id: 'speed', title: 'Hız Hedefi', value: avgWpmOverall, target: speedTarget, unit: 'WPM', icon: 'bi-lightning-charge', progress: Math.min(100, Math.round((avgWpmOverall / Math.max(1, speedTarget)) * 100)) }
    ];

    this.badges = [
      { id: 'first', name: 'İlk Adım', desc: 'İlk okuma oturumunu tamamladın', icon: 'bi-flag', earned: totalSessionsOverall >= 1 },
      { id: 'ten', name: '10 Oturum', desc: '10 okuma oturumu tamamladın', icon: 'bi-123', earned: totalSessionsOverall >= 10 },
      { id: 'speedster', name: 'Hız Şampiyonu', desc: 'Ortalama WPM 300+ seviyesine ulaştı', icon: 'bi-speedometer2', earned: avgWpmOverall >= 300 },
      { id: 'marathon', name: 'Maraton Okur', desc: 'Toplam 10+ saat okuma', icon: 'bi-trophy', earned: totalReadingSecondsOverall >= (10 * 3600) }
    ];
  }

  private getPeriodMinutesTarget(p: 'day' | 'week' | 'month' | 'year'): number {
    switch (p) {
      case 'day': return 30;
      case 'week': return 120;
      case 'month': return 600;
      case 'year': return 7200;
    }
  }
}