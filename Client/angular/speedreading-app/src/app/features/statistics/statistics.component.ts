import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Subscription, interval } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ReadingProgressApiService } from '../reading/services/reading-progress-api.service';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dark-dashboard" *ngIf="currentUser">
      <header class="dashboard-header">
        <div class="user-section">
          <div class="user-avatar"><i class="bi bi-graph-up"></i></div>
          <div class="user-info">
            <h2>Detaylı İstatistikler</h2>
            <p>{{ currentUser?.email }}</p>
          </div>
        </div>
        <div class="header-actions">
          <button class="logout-btn" (click)="goBack()"><i class="bi bi-arrow-left"></i></button>
        </div>
      </header>

      <main class="dashboard-main">
        <div class="period-filters">
          <button class="period-btn" [class.active]="selectedPeriod === 'day'" (click)="onPeriodChange('day')">Gün</button>
          <button class="period-btn" [class.active]="selectedPeriod === 'week'" (click)="onPeriodChange('week')">Hafta</button>
          <button class="period-btn" [class.active]="selectedPeriod === 'month'" (click)="onPeriodChange('month')">Ay</button>
          <button class="period-btn" [class.active]="selectedPeriod === 'year'" (click)="onPeriodChange('year')">Yıl</button>
          <div style="flex:1"></div>
          <button class="primary-btn" (click)="exportCsv()"><i class="bi bi-download"></i> CSV İndir</button>
        </div>

        <section class="content-area">
          <div class="content-card">
            <h3 class="mb-2"><i class="bi bi-table"></i> Oturumlar</h3>
            <div class="table-responsive">
              <table class="table-dark">
                <thead>
                  <tr>
                    <th>Başlangıç</th>
                    <th>Metin</th>
                    <th>WPM</th>
                    <th>Anlama</th>
                    <th>Süre (sn)</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let s of sessions">
                    <td>{{ s.startTime | date:'short' }}</td>
                    <td>{{ s.textId || '-' }}</td>
                    <td>{{ s.wordsPerMinute || 0 }}</td>
                    <td>{{ s.charactersPerMinute ? (s.charactersPerMinute) : (s.wordsPerMinute ? Math.round(s.wordsPerMinute / 10) : 0) }}%</td>
                    <td>{{ (s.totalDuration || 0) / 1000 | number:'1.0-0' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </section>
      </main>
    </div>
  `,
  styles: [`
    .table-responsive { overflow-x: auto; }
    table.table-dark { width: 100%; border-collapse: collapse; }
    table.table-dark th, table.table-dark td { padding: 10px 12px; border-bottom: 1px solid rgba(255,255,255,0.08); }
    table.table-dark thead th { text-align: left; color: #9bb4ff; background: rgba(255,255,255,0.03); }
  `]
})
export class StatisticsComponent implements OnInit, OnDestroy {
  currentUser: any;
  sessions: any[] = [];
  selectedPeriod: 'day' | 'week' | 'month' | 'year' = 'week';
  autoRefreshSub?: Subscription;
  public Math = Math;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService,
    private readingApi: ReadingProgressApiService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUserValue();
    if (!this.currentUser) {
      this.router.navigate(['/auth/login']);
      return;
    }

    const queryView = (this.route.snapshot.queryParamMap.get('view') || '').toLowerCase();
    const p = (this.route.snapshot.queryParamMap.get('period') || 'week').toLowerCase();
    if (p === 'day' || p === 'week' || p === 'month' || p === 'year') {
      this.selectedPeriod = p as any;
    }

    this.loadSessions();

    // Periyodik yenileme (2 dk)
    this.autoRefreshSub = interval(120000).subscribe(() => this.loadSessions());
  }

  ngOnDestroy(): void {
    if (this.autoRefreshSub) this.autoRefreshSub.unsubscribe();
  }

  onPeriodChange(p: 'day' | 'week' | 'month' | 'year') {
    if (this.selectedPeriod === p) return;
    this.selectedPeriod = p;
    this.router.navigate([], { relativeTo: this.route, queryParams: { period: p }, queryParamsHandling: 'merge' });
    this.loadSessions();
  }

  private loadSessions() {
    const userId = this.currentUser?.id;
    this.readingApi.getUserSessions(userId, { page: 1, pageSize: 100 }).subscribe({
      next: (list) => {
        // Basit dönem filtresi: frontend’de başlangıç tarihine göre eleyelim
        const from = this.calcFromDate(this.selectedPeriod);
        this.sessions = (list || []).filter(x => {
          return x.startTime && new Date(x.startTime) >= from;
        });
      },
      error: () => this.toastr.error('Oturumlar yüklenemedi')
    });
  }

  private calcFromDate(period: 'day' | 'week' | 'month' | 'year'): Date {
    const base = new Date();
    base.setHours(0,0,0,0);
    switch (period) {
      case 'day': return base;
      case 'week': return new Date(base.getFullYear(), base.getMonth(), base.getDate() - 7);
      case 'month': return new Date(base.getFullYear(), base.getMonth() - 1, base.getDate());
      case 'year': return new Date(base.getFullYear() - 1, base.getMonth(), base.getDate());
    }
  }

  exportCsv() {
    if (!this.sessions || this.sessions.length === 0) {
      this.toastr.info('İndirilecek veri bulunamadı');
      return;
    }
    const header = ['sessionId','textId','startTime','endTime','durationMs','wpm','charactersPerMinute'];
    const rows = this.sessions.map(s => [
      s.sessionId,
      s.textId || '',
      s.startTime ? new Date(s.startTime).toISOString() : '',
      s.endTime ? new Date(s.endTime).toISOString() : '',
      s.totalDuration || 0,
      s.wordsPerMinute || 0,
      s.charactersPerMinute || 0
    ]);
    const csv = [header.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `sessions_${new Date().toISOString()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}

