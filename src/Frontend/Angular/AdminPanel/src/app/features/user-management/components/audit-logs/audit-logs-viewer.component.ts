import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { LucideAngularModule, Shield, Activity, User, Database, Search, Filter, Calendar, Clock,
         Eye, Download, RefreshCw, AlertTriangle, CheckCircle, XCircle, Info, Settings,
         FileText, BarChart3, Trash2, Archive, Plus, MoreVertical, ChevronDown, ChevronRight } from 'lucide-angular';

import { AuditLogService } from '../../services/audit-log.service';
import { ExportService } from '../../../../shared/services/export.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ErrorHandlerService } from '../../../../core/services/error-handler.service';

interface AuditLog {
  id: string;
  timestamp: Date;
  userId: string;
  userName: string;
  userEmail: string;
  action: string;
  resource: string;
  resourceId?: string;
  resourceName?: string;
  changes?: AuditLogChange[];
  ipAddress: string;
  userAgent: string;
  sessionId: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  category: 'authentication' | 'authorization' | 'data_modification' | 'system' | 'security';
  success: boolean;
  errorMessage?: string;
  metadata?: Record<string, any>;
}

interface AuditLogChange {
  field: string;
  oldValue: any;
  newValue: any;
  fieldType: 'string' | 'number' | 'boolean' | 'date' | 'array' | 'object';
}

interface AuditLogFilter {
  dateFrom: string;
  dateTo: string;
  userId: string;
  action: string;
  resource: string;
  category: string;
  severity: string;
  success: boolean | null;
  search: string;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

interface AuditLogSummary {
  totalLogs: number;
  todayLogs: number;
  weekLogs: number;
  monthLogs: number;
  criticalLogs: number;
  failedAttempts: number;
  topUsers: { userId: string; userName: string; count: number }[];
  topActions: { action: string; count: number }[];
  activityTrend: { date: string; count: number }[];
}

interface AuditLogExportOptions {
  format: 'excel' | 'csv' | 'json' | 'pdf';
  includeMetadata: boolean;
  includeChanges: boolean;
  dateRange: 'today' | 'week' | 'month' | 'custom';
  customDateFrom?: string;
  customDateTo?: string;
}

@Component({
  selector: 'app-audit-logs-viewer',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="audit-logs-container">
      <!-- Header Section -->
      <div class="page-header">
        <div class="title-section">
          <div class="title-with-icon">
            <lucide-angular [img]="ShieldIcon" size="24" class="title-icon"></lucide-angular>
            <div>
              <h1 class="page-title">Denetim Günlükleri</h1>
              <p class="page-subtitle">Sistem aktivitelerini ve güvenlik olaylarını takip edin</p>
            </div>
          </div>
        </div>

        <div class="header-actions">
          <button
            type="button"
            class="btn btn-outline-secondary"
            (click)="toggleRealTime()"
            [class.active]="realTimeEnabled()">
            <lucide-angular [img]="ActivityIcon" size="16"></lucide-angular>
            {{ realTimeEnabled() ? 'Canlı: Açık' : 'Canlı: Kapalı' }}
          </button>

          <div class="dropdown">
            <button
              type="button"
              class="btn btn-outline-primary dropdown-toggle"
              data-bs-toggle="dropdown">
              <lucide-angular [img]="DownloadIcon" size="16"></lucide-angular>
              Dışa Aktar
            </button>
            <ul class="dropdown-menu">
              <li><a class="dropdown-item" (click)="openExportModal()">Özelleştirilmiş Dışa Aktarma</a></li>
              <li><hr class="dropdown-divider"></li>
              <li><a class="dropdown-item" (click)="quickExport('excel')">Excel olarak dışa aktar</a></li>
              <li><a class="dropdown-item" (click)="quickExport('csv')">CSV olarak dışa aktar</a></li>
              <li><a class="dropdown-item" (click)="quickExport('json')">JSON olarak dışa aktar</a></li>
            </ul>
          </div>

          <button
            type="button"
            class="btn btn-primary"
            (click)="refreshData()"
            [disabled]="loading()">
            <lucide-angular [img]="RefreshCwIcon" size="16"></lucide-angular>
            Yenile
          </button>
        </div>
      </div>

      <!-- Summary Cards -->
      <div class="summary-section" *ngIf="auditSummary()">
        <div class="summary-cards">
          <div class="summary-card">
            <div class="card-icon">
              <lucide-angular [img]="FileTextIcon" size="20" class="text-primary"></lucide-angular>
            </div>
            <div class="card-content">
              <div class="card-value">{{ auditSummary()?.totalLogs | number }}</div>
              <div class="card-label">Toplam Günlük</div>
            </div>
          </div>

          <div class="summary-card">
            <div class="card-icon">
              <lucide-angular [img]="ActivityIcon" size="20" class="text-info"></lucide-angular>
            </div>
            <div class="card-content">
              <div class="card-value">{{ auditSummary()?.todayLogs | number }}</div>
              <div class="card-label">Bugün</div>
            </div>
          </div>

          <div class="summary-card">
            <div class="card-icon">
              <lucide-angular [img]="AlertTriangleIcon" size="20" class="text-danger"></lucide-angular>
            </div>
            <div class="card-content">
              <div class="card-value">{{ auditSummary()?.criticalLogs | number }}</div>
              <div class="card-label">Kritik Olaylar</div>
            </div>
          </div>

          <div class="summary-card">
            <div class="card-icon">
              <lucide-angular [img]="XCircleIcon" size="20" class="text-warning"></lucide-angular>
            </div>
            <div class="card-content">
              <div class="card-value">{{ auditSummary()?.failedAttempts | number }}</div>
              <div class="card-label">Başarısız Denemeler</div>
            </div>
          </div>
        </div>

        <!-- Quick Stats -->
        <div class="quick-stats">
          <div class="stat-group">
            <h6>En Aktif Kullanıcılar</h6>
            <div class="stat-list">
              <div
                *ngFor="let user of auditSummary()?.topUsers?.slice(0, 3)"
                class="stat-item">
                <span class="stat-name">{{ user.userName }}</span>
                <span class="stat-value">{{ user.count }}</span>
              </div>
            </div>
          </div>

          <div class="stat-group">
            <h6>En Sık Eylemler</h6>
            <div class="stat-list">
              <div
                *ngFor="let action of auditSummary()?.topActions?.slice(0, 3)"
                class="stat-item">
                <span class="stat-name">{{ getActionLabel(action.action) }}</span>
                <span class="stat-value">{{ action.count }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Filters Section -->
      <div class="filters-section">
        <form [formGroup]="filterForm" (ngSubmit)="applyFilters()">
          <div class="filters-header">
            <h5>Filtreler</h5>
            <div class="filter-actions">
              <button type="button" class="btn btn-sm btn-outline-secondary" (click)="toggleAdvancedFilters()">
                <lucide-angular [img]="FilterIcon" size="14"></lucide-angular>
                {{ showAdvancedFilters() ? 'Basit' : 'Gelişmiş' }} Filtreler
              </button>
              <button type="button" class="btn btn-sm btn-outline-secondary" (click)="clearFilters()">
                Temizle
              </button>
              <button type="submit" class="btn btn-sm btn-primary">Uygula</button>
            </div>
          </div>

          <div class="filters-content">
            <!-- Basic Filters -->
            <div class="basic-filters">
              <div class="row">
                <div class="col-md-3">
                  <label class="form-label">Tarih Aralığı</label>
                  <div class="input-group">
                    <input type="datetime-local" class="form-control form-control-sm" formControlName="dateFrom">
                    <span class="input-group-text">-</span>
                    <input type="datetime-local" class="form-control form-control-sm" formControlName="dateTo">
                  </div>
                </div>

                <div class="col-md-2">
                  <label class="form-label">Kategori</label>
                  <select class="form-select form-select-sm" formControlName="category">
                    <option value="">Tümü</option>
                    <option value="authentication">Kimlik Doğrulama</option>
                    <option value="authorization">Yetkilendirme</option>
                    <option value="data_modification">Veri Değişikliği</option>
                    <option value="system">Sistem</option>
                    <option value="security">Güvenlik</option>
                  </select>
                </div>

                <div class="col-md-2">
                  <label class="form-label">Önem Derecesi</label>
                  <select class="form-select form-select-sm" formControlName="severity">
                    <option value="">Tümü</option>
                    <option value="low">Düşük</option>
                    <option value="medium">Orta</option>
                    <option value="high">Yüksek</option>
                    <option value="critical">Kritik</option>
                  </select>
                </div>

                <div class="col-md-2">
                  <label class="form-label">Durum</label>
                  <select class="form-select form-select-sm" formControlName="success">
                    <option value="">Tümü</option>
                    <option value="true">Başarılı</option>
                    <option value="false">Başarısız</option>
                  </select>
                </div>

                <div class="col-md-3">
                  <label class="form-label">Arama</label>
                  <div class="input-group">
                    <span class="input-group-text">
                      <lucide-angular [img]="SearchIcon" size="14"></lucide-angular>
                    </span>
                    <input
                      type="text"
                      class="form-control form-control-sm"
                      placeholder="Kullanıcı, eylem, kaynak ara..."
                      formControlName="search">
                  </div>
                </div>
              </div>
            </div>

            <!-- Advanced Filters -->
            <div class="advanced-filters" *ngIf="showAdvancedFilters()">
              <div class="row">
                <div class="col-md-3">
                  <label class="form-label">Kullanıcı</label>
                  <input type="text" class="form-control form-control-sm" formControlName="userId" placeholder="Kullanıcı ID veya e-posta">
                </div>

                <div class="col-md-3">
                  <label class="form-label">Eylem</label>
                  <select class="form-select form-select-sm" formControlName="action">
                    <option value="">Tüm Eylemler</option>
                    <option value="LOGIN">Giriş</option>
                    <option value="LOGOUT">Çıkış</option>
                    <option value="CREATE_USER">Kullanıcı Oluştur</option>
                    <option value="UPDATE_USER">Kullanıcı Güncelle</option>
                    <option value="DELETE_USER">Kullanıcı Sil</option>
                    <option value="ASSIGN_ROLE">Rol Ata</option>
                    <option value="REMOVE_ROLE">Rol Kaldır</option>
                    <option value="GRANT_PERMISSION">İzin Ver</option>
                    <option value="REVOKE_PERMISSION">İzin Kaldır</option>
                  </select>
                </div>

                <div class="col-md-3">
                  <label class="form-label">Kaynak</label>
                  <input type="text" class="form-control form-control-sm" formControlName="resource" placeholder="Kaynak türü">
                </div>

                <div class="col-md-3">
                  <label class="form-label">Sıralama</label>
                  <div class="input-group">
                    <select class="form-select form-select-sm" formControlName="sortBy">
                      <option value="timestamp">Zaman</option>
                      <option value="userName">Kullanıcı</option>
                      <option value="action">Eylem</option>
                      <option value="severity">Önem</option>
                    </select>
                    <button
                      type="button"
                      class="btn btn-outline-secondary btn-sm"
                      (click)="toggleSortDirection()">
                      <lucide-angular [img]="sortDirection() === 'asc' ? 'ChevronDown' : 'ChevronRight'" size="14"></lucide-angular>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </form>
      </div>

      <!-- Logs Table -->
      <div class="logs-section">
        <div class="logs-table-container">
          <table class="table table-hover logs-table">
            <thead>
              <tr>
                <th class="timestamp-column">
                  <button class="table-sort-btn" (click)="sortBy('timestamp')">
                    Zaman
                    <lucide-angular [img]="getSortIcon('timestamp')" size="12"></lucide-angular>
                  </button>
                </th>
                <th class="user-column">
                  <button class="table-sort-btn" (click)="sortBy('userName')">
                    Kullanıcı
                    <lucide-angular [img]="getSortIcon('userName')" size="12"></lucide-angular>
                  </button>
                </th>
                <th class="action-column">
                  <button class="table-sort-btn" (click)="sortBy('action')">
                    Eylem
                    <lucide-angular [img]="getSortIcon('action')" size="12"></lucide-angular>
                  </button>
                </th>
                <th class="resource-column">Kaynak</th>
                <th class="severity-column">Önem</th>
                <th class="status-column">Durum</th>
                <th class="details-column">Detaylar</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let log of paginatedLogs(); trackBy: trackByLogId" [class]="getLogRowClass(log)">
                <td class="timestamp-cell">
                  <div class="timestamp-info">
                    <div class="timestamp-date">{{ formatDate(log.timestamp) }}</div>
                    <div class="timestamp-time">{{ formatTime(log.timestamp) }}</div>
                  </div>
                </td>

                <td class="user-cell">
                  <div class="user-info">
                    <div class="user-name">{{ log.userName }}</div>
                    <div class="user-email">{{ log.userEmail }}</div>
                    <div class="user-ip">{{ log.ipAddress }}</div>
                  </div>
                </td>

                <td class="action-cell">
                  <div class="action-info">
                    <div class="action-name">{{ getActionLabel(log.action) }}</div>
                    <div class="action-category">{{ getCategoryLabel(log.category) }}</div>
                  </div>
                </td>

                <td class="resource-cell">
                  <div class="resource-info">
                    <div class="resource-type">{{ log.resource }}</div>
                    <div class="resource-name" *ngIf="log.resourceName">{{ log.resourceName }}</div>
                    <div class="resource-id" *ngIf="log.resourceId">ID: {{ log.resourceId }}</div>
                  </div>
                </td>

                <td class="severity-cell">
                  <span [class]="'severity-badge severity-' + log.severity">
                    {{ getSeverityLabel(log.severity) }}
                  </span>
                </td>

                <td class="status-cell">
                  <div class="status-info">
                    <span [class]="'status-badge ' + (log.success ? 'status-success' : 'status-failure')">
                      <lucide-angular [img]="log.success ? CheckCircleIcon : XCircleIcon" size="12"></lucide-angular>
                      {{ log.success ? 'Başarılı' : 'Başarısız' }}
                    </span>
                    <div class="error-message" *ngIf="!log.success && log.errorMessage">
                      {{ log.errorMessage }}
                    </div>
                  </div>
                </td>

                <td class="details-cell">
                  <div class="detail-actions">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-secondary"
                      (click)="viewLogDetails(log)"
                      title="Detayları Görüntüle">
                      <lucide-angular [img]="EyeIcon" size="12"></lucide-angular>
                    </button>

                    <div class="dropdown" *ngIf="log.changes?.length || log.metadata">
                      <button
                        type="button"
                        class="btn btn-sm btn-outline-secondary dropdown-toggle"
                        data-bs-toggle="dropdown">
                        <lucide-angular [img]="MoreVerticalIcon" size="12"></lucide-angular>
                      </button>
                      <ul class="dropdown-menu dropdown-menu-end">
                        <li *ngIf="log.changes?.length">
                          <a class="dropdown-item" (click)="viewChanges(log)">
                            Değişiklikleri Görüntüle
                          </a>
                        </li>
                        <li *ngIf="log.metadata">
                          <a class="dropdown-item" (click)="viewMetadata(log)">
                            Metadata Görüntüle
                          </a>
                        </li>
                        <li><hr class="dropdown-divider"></li>
                        <li>
                          <a class="dropdown-item" (click)="viewRelatedLogs(log)">
                            İlgili Günlükleri Görüntüle
                          </a>
                        </li>
                      </ul>
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>

          <!-- Empty State -->
          <div class="empty-state" *ngIf="paginatedLogs().length === 0 && !loading()">
            <lucide-angular [img]="FileTextIcon" size="48" class="empty-icon"></lucide-angular>
            <h5>Günlük bulunamadı</h5>
            <p>Belirtilen kriterlere uygun günlük kaydı bulunamadı.</p>
            <button type="button" class="btn btn-outline-secondary" (click)="clearFilters()">
              Filtreleri Temizle
            </button>
          </div>

          <!-- Loading State -->
          <div class="loading-state" *ngIf="loading()">
            <div class="loading-spinner">
              <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Yükleniyor...</span>
              </div>
              <p class="loading-text">Günlükler yükleniyor...</p>
            </div>
          </div>
        </div>

        <!-- Pagination -->
        <div class="pagination-section" *ngIf="totalPages() > 1">
          <nav aria-label="Audit logs pagination">
            <ul class="pagination justify-content-center">
              <li class="page-item" [class.disabled]="currentPage() === 1">
                <a class="page-link" (click)="goToPage(currentPage() - 1)">Önceki</a>
              </li>

              <li
                *ngFor="let page of visiblePages()"
                class="page-item"
                [class.active]="page === currentPage()">
                <a class="page-link" (click)="goToPage(page)">{{ page }}</a>
              </li>

              <li class="page-item" [class.disabled]="currentPage() === totalPages()">
                <a class="page-link" (click)="goToPage(currentPage() + 1)">Sonraki</a>
              </li>
            </ul>
          </nav>

          <div class="pagination-info">
            <span>{{ getPaginationInfo() }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Log Details Modal -->
    <div class="modal fade" id="logDetailsModal" tabindex="-1">
      <div class="modal-dialog modal-lg">
        <div class="modal-content" *ngIf="selectedLog()">
          <div class="modal-header">
            <h5 class="modal-title">Günlük Detayları</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>

          <div class="modal-body">
            <div class="log-details">
              <!-- Basic Information -->
              <div class="detail-section">
                <h6>Temel Bilgiler</h6>
                <div class="detail-grid">
                  <div class="detail-item">
                    <span class="detail-label">Zaman:</span>
                    <span class="detail-value">{{ formatDateTime(selectedLog()?.timestamp) }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Kullanıcı:</span>
                    <span class="detail-value">{{ selectedLog()?.userName }} ({{ selectedLog()?.userEmail }})</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Eylem:</span>
                    <span class="detail-value">{{ getActionLabel(selectedLog()?.action) }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Kaynak:</span>
                    <span class="detail-value">{{ selectedLog()?.resource }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">IP Adresi:</span>
                    <span class="detail-value">{{ selectedLog()?.ipAddress }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Tarayıcı:</span>
                    <span class="detail-value">{{ selectedLog()?.userAgent }}</span>
                  </div>
                </div>
              </div>

              <!-- Changes -->
              <div class="detail-section" *ngIf="selectedLog()?.changes?.length">
                <h6>Değişiklikler</h6>
                <div class="changes-list">
                  <div
                    *ngFor="let change of selectedLog()?.changes"
                    class="change-item">
                    <div class="change-field">{{ change.field }}</div>
                    <div class="change-values">
                      <div class="old-value">
                        <span class="value-label">Eski:</span>
                        <span class="value-content">{{ formatChangeValue(change.oldValue, change.fieldType) }}</span>
                      </div>
                      <div class="new-value">
                        <span class="value-label">Yeni:</span>
                        <span class="value-content">{{ formatChangeValue(change.newValue, change.fieldType) }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Metadata -->
              <div class="detail-section" *ngIf="selectedLog()?.metadata">
                <h6>Ek Bilgiler</h6>
                <div class="metadata-content">
                  <pre>{{ formatMetadata(selectedLog()?.metadata) }}</pre>
                </div>
              </div>

              <!-- Error Information -->
              <div class="detail-section" *ngIf="!selectedLog()?.success">
                <h6>Hata Bilgileri</h6>
                <div class="error-info">
                  <div class="alert alert-danger">
                    {{ selectedLog()?.errorMessage }}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Kapat
            </button>
            <button type="button" class="btn btn-primary" (click)="exportSingleLog()">
              <lucide-angular [img]="DownloadIcon" size="14" class="me-2"></lucide-angular>
              Dışa Aktar
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Export Modal -->
    <div class="modal fade" id="exportModal" tabindex="-1">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Günlükleri Dışa Aktar</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>

          <form [formGroup]="exportForm" (ngSubmit)="executeExport()">
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Format</label>
                <div class="format-options">
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="excel" formControlName="format" id="formatExcel">
                    <label class="form-check-label" for="formatExcel">Excel (XLSX)</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="csv" formControlName="format" id="formatCsv">
                    <label class="form-check-label" for="formatCsv">CSV</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="json" formControlName="format" id="formatJson">
                    <label class="form-check-label" for="formatJson">JSON</label>
                  </div>
                </div>
              </div>

              <div class="mb-3">
                <label class="form-label">Tarih Aralığı</label>
                <div class="date-range-options">
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="today" formControlName="dateRange" id="rangeToday">
                    <label class="form-check-label" for="rangeToday">Bugün</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="week" formControlName="dateRange" id="rangeWeek">
                    <label class="form-check-label" for="rangeWeek">Son 7 gün</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="month" formControlName="dateRange" id="rangeMonth">
                    <label class="form-check-label" for="rangeMonth">Son 30 gün</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="custom" formControlName="dateRange" id="rangeCustom">
                    <label class="form-check-label" for="rangeCustom">Özel Aralık</label>
                  </div>
                </div>

                <div class="custom-date-range" *ngIf="exportForm.get('dateRange')?.value === 'custom'">
                  <div class="input-group mt-2">
                    <input type="date" class="form-control" formControlName="customDateFrom">
                    <span class="input-group-text">-</span>
                    <input type="date" class="form-control" formControlName="customDateTo">
                  </div>
                </div>
              </div>

              <div class="mb-3">
                <label class="form-label">Dahil Edilecek Bilgiler</label>
                <div class="include-options">
                  <div class="form-check">
                    <input class="form-check-input" type="checkbox" formControlName="includeMetadata" id="includeMetadata">
                    <label class="form-check-label" for="includeMetadata">Metadata</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input" type="checkbox" formControlName="includeChanges" id="includeChanges">
                    <label class="form-check-label" for="includeChanges">Değişiklik Detayları</label>
                  </div>
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                İptal
              </button>
              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="exportForm.invalid || exporting()">
                <span class="spinner-border spinner-border-sm me-2" *ngIf="exporting()"></span>
                <lucide-angular [img]="DownloadIcon" size="14" class="me-2"></lucide-angular>
                Dışa Aktar
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .audit-logs-container {
      padding: 1.5rem;
      max-width: 1600px;
      margin: 0 auto;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 2rem;
    }

    .title-section {
      flex: 1;
    }

    .title-with-icon {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
    }

    .title-icon {
      color: var(--bs-primary);
      margin-top: 0.25rem;
    }

    .page-title {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .page-subtitle {
      margin: 0.25rem 0 0 0;
      color: var(--bs-gray-600);
      font-size: 0.95rem;
    }

    .header-actions {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    .header-actions .btn.active {
      background: var(--bs-success);
      border-color: var(--bs-success);
      color: white;
    }

    .summary-section {
      margin-bottom: 2rem;
    }

    .summary-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .summary-card {
      background: white;
      border-radius: 0.5rem;
      padding: 1.25rem;
      border: 1px solid var(--bs-gray-200);
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .card-icon {
      flex-shrink: 0;
    }

    .card-content {
      flex: 1;
    }

    .card-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--bs-gray-900);
      margin-bottom: 0.25rem;
    }

    .card-label {
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .quick-stats {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
      background: white;
      border-radius: 0.5rem;
      padding: 1.5rem;
      border: 1px solid var(--bs-gray-200);
    }

    .stat-group h6 {
      margin-bottom: 1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .stat-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .stat-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .stat-name {
      color: var(--bs-gray-700);
      font-size: 0.9rem;
    }

    .stat-value {
      font-weight: 600;
      color: var(--bs-primary);
    }

    .filters-section {
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
      margin-bottom: 1.5rem;
    }

    .filters-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .filters-header h5 {
      margin: 0;
      font-weight: 600;
    }

    .filter-actions {
      display: flex;
      gap: 0.5rem;
    }

    .filters-content {
      padding: 1.25rem;
    }

    .basic-filters,
    .advanced-filters {
      margin-bottom: 1rem;
    }

    .advanced-filters {
      padding-top: 1rem;
      border-top: 1px solid var(--bs-gray-200);
    }

    .logs-section {
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
      overflow: hidden;
    }

    .logs-table-container {
      overflow-x: auto;
    }

    .logs-table {
      margin: 0;
      font-size: 0.9rem;
    }

    .logs-table th {
      background: var(--bs-gray-50);
      border-bottom: 2px solid var(--bs-gray-200);
      font-weight: 600;
      color: var(--bs-gray-700);
      padding: 1rem 0.75rem;
      white-space: nowrap;
    }

    .logs-table td {
      padding: 1rem 0.75rem;
      vertical-align: top;
      border-bottom: 1px solid var(--bs-gray-100);
    }

    .table-sort-btn {
      background: none;
      border: none;
      color: inherit;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-weight: inherit;
      padding: 0;
    }

    .timestamp-column { width: 140px; }
    .user-column { width: 180px; }
    .action-column { width: 150px; }
    .resource-column { width: 150px; }
    .severity-column { width: 100px; }
    .status-column { width: 120px; }
    .details-column { width: 80px; }

    .timestamp-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .timestamp-date {
      font-weight: 500;
      color: var(--bs-gray-900);
    }

    .timestamp-time {
      font-size: 0.8rem;
      color: var(--bs-gray-600);
    }

    .user-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .user-name {
      font-weight: 500;
      color: var(--bs-gray-900);
    }

    .user-email {
      font-size: 0.8rem;
      color: var(--bs-gray-600);
    }

    .user-ip {
      font-size: 0.75rem;
      color: var(--bs-gray-500);
    }

    .action-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .action-name {
      font-weight: 500;
      color: var(--bs-gray-900);
    }

    .action-category {
      font-size: 0.8rem;
      color: var(--bs-gray-600);
    }

    .resource-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .resource-type {
      font-weight: 500;
      color: var(--bs-gray-900);
    }

    .resource-name {
      font-size: 0.8rem;
      color: var(--bs-gray-600);
    }

    .resource-id {
      font-size: 0.75rem;
      color: var(--bs-gray-500);
    }

    .severity-badge {
      display: inline-block;
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
      font-weight: 500;
      text-transform: uppercase;
    }

    .severity-badge.severity-low {
      background: var(--bs-success-bg);
      color: var(--bs-success);
    }

    .severity-badge.severity-medium {
      background: var(--bs-info-bg);
      color: var(--bs-info);
    }

    .severity-badge.severity-high {
      background: var(--bs-warning-bg);
      color: var(--bs-warning);
    }

    .severity-badge.severity-critical {
      background: var(--bs-danger-bg);
      color: var(--bs-danger);
    }

    .status-badge {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.8rem;
      font-weight: 500;
    }

    .status-badge.status-success {
      background: var(--bs-success-bg);
      color: var(--bs-success);
    }

    .status-badge.status-failure {
      background: var(--bs-danger-bg);
      color: var(--bs-danger);
    }

    .error-message {
      font-size: 0.75rem;
      color: var(--bs-danger);
      margin-top: 0.25rem;
    }

    .detail-actions {
      display: flex;
      gap: 0.25rem;
    }

    .logs-table tbody tr.log-critical {
      background: var(--bs-danger-bg);
    }

    .logs-table tbody tr.log-high {
      background: var(--bs-warning-bg);
    }

    .logs-table tbody tr.log-failure {
      background: rgba(var(--bs-danger-rgb), 0.1);
    }

    .pagination-section {
      padding: 1rem;
      border-top: 1px solid var(--bs-gray-200);
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
    }

    .pagination-info {
      color: var(--bs-gray-600);
      font-size: 0.9rem;
    }

    .empty-state,
    .loading-state {
      text-align: center;
      padding: 4rem 2rem;
    }

    .empty-icon {
      color: var(--bs-gray-400);
      margin-bottom: 1.5rem;
    }

    .loading-spinner {
      text-align: center;
    }

    .loading-text {
      margin-top: 1rem;
      color: var(--bs-gray-600);
    }

    /* Modal Styles */
    .detail-section {
      margin-bottom: 2rem;
    }

    .detail-section h6 {
      margin-bottom: 1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
      border-bottom: 1px solid var(--bs-gray-200);
      padding-bottom: 0.5rem;
    }

    .detail-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 1rem;
    }

    .detail-item {
      display: flex;
      justify-content: space-between;
      padding: 0.5rem 0;
      border-bottom: 1px solid var(--bs-gray-100);
    }

    .detail-label {
      font-weight: 500;
      color: var(--bs-gray-700);
    }

    .detail-value {
      color: var(--bs-gray-900);
      word-break: break-word;
    }

    .changes-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .change-item {
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      padding: 1rem;
    }

    .change-field {
      font-weight: 600;
      color: var(--bs-gray-900);
      margin-bottom: 0.5rem;
    }

    .change-values {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .old-value,
    .new-value {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .value-label {
      font-size: 0.8rem;
      font-weight: 500;
      color: var(--bs-gray-600);
    }

    .value-content {
      padding: 0.5rem;
      background: var(--bs-gray-50);
      border-radius: 0.25rem;
      font-family: monospace;
      font-size: 0.9rem;
    }

    .old-value .value-content {
      background: var(--bs-danger-bg);
    }

    .new-value .value-content {
      background: var(--bs-success-bg);
    }

    .metadata-content {
      background: var(--bs-gray-50);
      border-radius: 0.5rem;
      padding: 1rem;
    }

    .metadata-content pre {
      margin: 0;
      font-size: 0.85rem;
      color: var(--bs-gray-800);
    }

    .format-options,
    .date-range-options,
    .include-options {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .custom-date-range {
      margin-top: 1rem;
    }

    @media (max-width: 768px) {
      .audit-logs-container {
        padding: 1rem;
      }

      .page-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .summary-cards {
        grid-template-columns: 1fr;
      }

      .quick-stats {
        grid-template-columns: 1fr;
      }

      .filters-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .filter-actions {
        justify-content: space-between;
      }

      .detail-grid {
        grid-template-columns: 1fr;
      }

      .change-values {
        grid-template-columns: 1fr;
      }

      .logs-table {
        font-size: 0.8rem;
      }

      .logs-table th,
      .logs-table td {
        padding: 0.5rem 0.25rem;
      }
    }
  `]
})
export class AuditLogsViewerComponent implements OnInit, OnDestroy {
  // Dependency Injection
  private readonly auditLogService = inject(AuditLogService);
  private readonly exportService = inject(ExportService);
  private readonly loadingService = inject(LoadingService);
  private readonly errorHandler = inject(ErrorHandlerService);
  private readonly fb = inject(FormBuilder);

  // Icons
  readonly ShieldIcon = Shield;
  readonly ActivityIcon = Activity;
  readonly UserIcon = User;
  readonly DatabaseIcon = Database;
  readonly SearchIcon = Search;
  readonly FilterIcon = Filter;
  readonly CalendarIcon = Calendar;
  readonly ClockIcon = Clock;
  readonly EyeIcon = Eye;
  readonly DownloadIcon = Download;
  readonly RefreshCwIcon = RefreshCw;
  readonly AlertTriangleIcon = AlertTriangle;
  readonly CheckCircleIcon = CheckCircle;
  readonly XCircleIcon = XCircle;
  readonly InfoIcon = Info;
  readonly SettingsIcon = Settings;
  readonly FileTextIcon = FileText;
  readonly BarChart3Icon = BarChart3;
  readonly Trash2Icon = Trash2;
  readonly ArchiveIcon = Archive;
  readonly PlusIcon = Plus;
  readonly MoreVerticalIcon = MoreVertical;
  readonly ChevronDownIcon = ChevronDown;
  readonly ChevronRightIcon = ChevronRight;

  // State Signals
  auditLogs = signal<AuditLog[]>([]);
  filteredLogs = signal<AuditLog[]>([]);
  selectedLog = signal<AuditLog | null>(null);
  auditSummary = signal<AuditLogSummary | null>(null);
  loading = signal(false);
  exporting = signal(false);
  realTimeEnabled = signal(false);
  showAdvancedFilters = signal(false);

  // Pagination
  currentPage = signal(1);
  pageSize = signal(25);
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));

  // Sorting
  sortField = signal('timestamp');
  sortDirection = signal<'asc' | 'desc'>('desc');

  // Forms
  filterForm: FormGroup;
  exportForm: FormGroup;

  // Computed Values
  paginatedLogs = computed(() => {
    const logs = this.filteredLogs();
    const startIndex = (this.currentPage() - 1) * this.pageSize();
    const endIndex = startIndex + this.pageSize();
    return logs.slice(startIndex, endIndex);
  });

  visiblePages = computed(() => {
    const current = this.currentPage();
    const total = this.totalPages();
    const pages: number[] = [];

    let start = Math.max(1, current - 2);
    let end = Math.min(total, current + 2);

    if (end - start < 4) {
      if (start === 1) {
        end = Math.min(total, 5);
      } else if (end === total) {
        start = Math.max(1, total - 4);
      }
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  });

  // Real-time update interval
  private realTimeInterval?: NodeJS.Timeout;

  constructor() {
    this.filterForm = this.fb.group({
      dateFrom: [''],
      dateTo: [''],
      userId: [''],
      action: [''],
      resource: [''],
      category: [''],
      severity: [''],
      success: [''],
      search: [''],
      sortBy: ['timestamp'],
      sortDirection: ['desc']
    });

    this.exportForm = this.fb.group({
      format: ['excel'],
      dateRange: ['today'],
      customDateFrom: [''],
      customDateTo: [''],
      includeMetadata: [false],
      includeChanges: [true]
    });
  }

  ngOnInit(): void {
    this.loadData();
    this.setupFilterSubscription();
  }

  ngOnDestroy(): void {
    if (this.realTimeInterval) {
      clearInterval(this.realTimeInterval);
    }
  }

  // Data Loading
  async loadData(): Promise<void> {
    await Promise.all([
      this.loadAuditLogs(),
      this.loadAuditSummary()
    ]);
  }

  async loadAuditLogs(): Promise<void> {
    try {
      this.loading.set(true);

      // Build filter from form
      const filters = this.buildFiltersFromForm();

      // Mock data for now - replace with actual service call
      const mockLogs: AuditLog[] = this.generateMockLogs();

      // Apply filters
      const filtered = this.applyFiltersToLogs(mockLogs, filters);

      this.auditLogs.set(mockLogs);
      this.filteredLogs.set(filtered);
      this.totalCount.set(filtered.length);

    } catch (error) {
      this.errorHandler.handleError(error, 'Günlükler yüklenirken hata oluştu');
    } finally {
      this.loading.set(false);
    }
  }

  async loadAuditSummary(): Promise<void> {
    try {
      // Mock summary data
      const summary: AuditLogSummary = {
        totalLogs: 15420,
        todayLogs: 234,
        weekLogs: 1680,
        monthLogs: 7235,
        criticalLogs: 12,
        failedAttempts: 89,
        topUsers: [
          { userId: '1', userName: 'John Doe', count: 45 },
          { userId: '2', userName: 'Jane Smith', count: 38 },
          { userId: '3', userName: 'Bob Wilson', count: 32 }
        ],
        topActions: [
          { action: 'LOGIN', count: 1230 },
          { action: 'UPDATE_USER', count: 892 },
          { action: 'ASSIGN_ROLE', count: 654 }
        ],
        activityTrend: []
      };

      this.auditSummary.set(summary);
    } catch (error) {
      console.error('Summary loading failed:', error);
    }
  }

  async refreshData(): Promise<void> {
    await this.loadData();
  }

  // Filter Methods
  private setupFilterSubscription(): void {
    this.filterForm.valueChanges.subscribe(() => {
      this.currentPage.set(1);
      this.loadAuditLogs();
    });
  }

  private buildFiltersFromForm(): AuditLogFilter {
    const formValue = this.filterForm.value;
    return {
      dateFrom: formValue.dateFrom,
      dateTo: formValue.dateTo,
      userId: formValue.userId,
      action: formValue.action,
      resource: formValue.resource,
      category: formValue.category,
      severity: formValue.severity,
      success: formValue.success === '' ? null : formValue.success === 'true',
      search: formValue.search,
      sortBy: formValue.sortBy,
      sortDirection: formValue.sortDirection
    };
  }

  private applyFiltersToLogs(logs: AuditLog[], filters: AuditLogFilter): AuditLog[] {
    let filtered = [...logs];

    // Apply filters
    if (filters.search) {
      const search = filters.search.toLowerCase();
      filtered = filtered.filter(log =>
        log.userName.toLowerCase().includes(search) ||
        log.userEmail.toLowerCase().includes(search) ||
        log.action.toLowerCase().includes(search) ||
        log.resource.toLowerCase().includes(search)
      );
    }

    if (filters.userId) {
      filtered = filtered.filter(log =>
        log.userId.includes(filters.userId) ||
        log.userEmail.toLowerCase().includes(filters.userId.toLowerCase())
      );
    }

    if (filters.action) {
      filtered = filtered.filter(log => log.action === filters.action);
    }

    if (filters.resource) {
      filtered = filtered.filter(log => log.resource.toLowerCase().includes(filters.resource.toLowerCase()));
    }

    if (filters.category) {
      filtered = filtered.filter(log => log.category === filters.category);
    }

    if (filters.severity) {
      filtered = filtered.filter(log => log.severity === filters.severity);
    }

    if (filters.success !== null) {
      filtered = filtered.filter(log => log.success === filters.success);
    }

    // Apply date range filter
    if (filters.dateFrom) {
      const fromDate = new Date(filters.dateFrom);
      filtered = filtered.filter(log => log.timestamp >= fromDate);
    }

    if (filters.dateTo) {
      const toDate = new Date(filters.dateTo);
      filtered = filtered.filter(log => log.timestamp <= toDate);
    }

    // Apply sorting
    filtered.sort((a, b) => {
      let aValue = a[filters.sortBy as keyof AuditLog];
      let bValue = b[filters.sortBy as keyof AuditLog];

      if (aValue instanceof Date && bValue instanceof Date) {
        aValue = aValue.getTime();
        bValue = bValue.getTime();
      }

      if (typeof aValue === 'string' && typeof bValue === 'string') {
        aValue = aValue.toLowerCase();
        bValue = bValue.toLowerCase();
      }

      if (aValue < bValue) return filters.sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return filters.sortDirection === 'asc' ? 1 : -1;
      return 0;
    });

    return filtered;
  }

  applyFilters(): void {
    this.loadAuditLogs();
  }

  clearFilters(): void {
    this.filterForm.reset({
      sortBy: 'timestamp',
      sortDirection: 'desc'
    });
  }

  toggleAdvancedFilters(): void {
    this.showAdvancedFilters.update(show => !show);
  }

  // Sorting
  sortBy(field: string): void {
    if (this.sortField() === field) {
      this.sortDirection.update(dir => dir === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }

    this.filterForm.patchValue({
      sortBy: field,
      sortDirection: this.sortDirection()
    });
  }

  getSortIcon(field: string): any {
    if (this.sortField() !== field) return ChevronDownIcon;
    return this.sortDirection() === 'asc' ? ChevronDownIcon : ChevronRightIcon;
  }

  toggleSortDirection(): void {
    this.sortDirection.update(dir => dir === 'asc' ? 'desc' : 'asc');
    this.filterForm.patchValue({ sortDirection: this.sortDirection() });
  }

  // Pagination
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  getPaginationInfo(): string {
    const start = (this.currentPage() - 1) * this.pageSize() + 1;
    const end = Math.min(this.currentPage() * this.pageSize(), this.totalCount());
    return `${start}-${end} / ${this.totalCount()} günlük`;
  }

  // Real-time Updates
  toggleRealTime(): void {
    const enabled = this.realTimeEnabled();
    this.realTimeEnabled.set(!enabled);

    if (!enabled) {
      // Start real-time updates
      this.realTimeInterval = setInterval(() => {
        this.loadAuditLogs();
      }, 30000); // Update every 30 seconds
    } else {
      // Stop real-time updates
      if (this.realTimeInterval) {
        clearInterval(this.realTimeInterval);
        this.realTimeInterval = undefined;
      }
    }
  }

  // Log Details
  viewLogDetails(log: AuditLog): void {
    this.selectedLog.set(log);
    // Open modal programmatically
  }

  viewChanges(log: AuditLog): void {
    this.selectedLog.set(log);
    // Open changes modal
  }

  viewMetadata(log: AuditLog): void {
    this.selectedLog.set(log);
    // Open metadata modal
  }

  viewRelatedLogs(log: AuditLog): void {
    // Filter to show related logs
    this.filterForm.patchValue({
      userId: log.userId,
      resource: log.resource
    });
  }

  // Export Methods
  openExportModal(): void {
    // Open export modal
  }

  async quickExport(format: 'excel' | 'csv' | 'json'): Promise<void> {
    try {
      this.exporting.set(true);

      const exportOptions: AuditLogExportOptions = {
        format,
        includeMetadata: false,
        includeChanges: true,
        dateRange: 'today'
      };

      await this.exportService.exportAuditLogs(this.filteredLogs(), exportOptions);

    } catch (error) {
      this.errorHandler.handleError(error, 'Dışa aktarma sırasında hata oluştu');
    } finally {
      this.exporting.set(false);
    }
  }

  async executeExport(): Promise<void> {
    if (this.exportForm.invalid) return;

    try {
      this.exporting.set(true);
      const formValue = this.exportForm.value;

      const exportOptions: AuditLogExportOptions = {
        format: formValue.format,
        includeMetadata: formValue.includeMetadata,
        includeChanges: formValue.includeChanges,
        dateRange: formValue.dateRange,
        customDateFrom: formValue.customDateFrom,
        customDateTo: formValue.customDateTo
      };

      await this.exportService.exportAuditLogs(this.filteredLogs(), exportOptions);

    } catch (error) {
      this.errorHandler.handleError(error, 'Dışa aktarma sırasında hata oluştu');
    } finally {
      this.exporting.set(false);
    }
  }

  async exportSingleLog(): Promise<void> {
    const log = this.selectedLog();
    if (!log) return;

    try {
      const exportOptions: AuditLogExportOptions = {
        format: 'json',
        includeMetadata: true,
        includeChanges: true,
        dateRange: 'custom'
      };

      await this.exportService.exportAuditLogs([log], exportOptions);
    } catch (error) {
      this.errorHandler.handleError(error, 'Dışa aktarma sırasında hata oluştu');
    }
  }

  // Helper Methods
  getLogRowClass(log: AuditLog): string {
    const classes: string[] = [];

    if (log.severity === 'critical') {
      classes.push('log-critical');
    } else if (log.severity === 'high') {
      classes.push('log-high');
    }

    if (!log.success) {
      classes.push('log-failure');
    }

    return classes.join(' ');
  }

  getActionLabel(action: string): string {
    const actionLabels: Record<string, string> = {
      'LOGIN': 'Giriş',
      'LOGOUT': 'Çıkış',
      'CREATE_USER': 'Kullanıcı Oluştur',
      'UPDATE_USER': 'Kullanıcı Güncelle',
      'DELETE_USER': 'Kullanıcı Sil',
      'ASSIGN_ROLE': 'Rol Ata',
      'REMOVE_ROLE': 'Rol Kaldır',
      'GRANT_PERMISSION': 'İzin Ver',
      'REVOKE_PERMISSION': 'İzin Kaldır'
    };

    return actionLabels[action] || action;
  }

  getCategoryLabel(category: string): string {
    const categoryLabels: Record<string, string> = {
      'authentication': 'Kimlik Doğrulama',
      'authorization': 'Yetkilendirme',
      'data_modification': 'Veri Değişikliği',
      'system': 'Sistem',
      'security': 'Güvenlik'
    };

    return categoryLabels[category] || category;
  }

  getSeverityLabel(severity: string): string {
    const severityLabels: Record<string, string> = {
      'low': 'Düşük',
      'medium': 'Orta',
      'high': 'Yüksek',
      'critical': 'Kritik'
    };

    return severityLabels[severity] || severity;
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('tr-TR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).format(date);
  }

  formatTime(date: Date): string {
    return new Intl.DateTimeFormat('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    }).format(date);
  }

  formatDateTime(date: Date | undefined): string {
    if (!date) return '';
    return new Intl.DateTimeFormat('tr-TR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    }).format(date);
  }

  formatChangeValue(value: any, fieldType: string): string {
    if (value === null || value === undefined) return 'null';

    switch (fieldType) {
      case 'date':
        return new Date(value).toLocaleString('tr-TR');
      case 'boolean':
        return value ? 'Evet' : 'Hayır';
      case 'array':
        return Array.isArray(value) ? value.join(', ') : String(value);
      case 'object':
        return JSON.stringify(value, null, 2);
      default:
        return String(value);
    }
  }

  formatMetadata(metadata: Record<string, any> | undefined): string {
    if (!metadata) return '';
    return JSON.stringify(metadata, null, 2);
  }

  trackByLogId(index: number, log: AuditLog): string {
    return log.id;
  }

  // Mock Data Generation
  private generateMockLogs(): AuditLog[] {
    const logs: AuditLog[] = [];
    const actions = ['LOGIN', 'LOGOUT', 'CREATE_USER', 'UPDATE_USER', 'DELETE_USER', 'ASSIGN_ROLE', 'REMOVE_ROLE'];
    const resources = ['User', 'Role', 'Permission', 'Group'];
    const severities: ('low' | 'medium' | 'high' | 'critical')[] = ['low', 'medium', 'high', 'critical'];
    const categories: ('authentication' | 'authorization' | 'data_modification' | 'system' | 'security')[] =
      ['authentication', 'authorization', 'data_modification', 'system', 'security'];
    const users = [
      { id: '1', name: 'John Doe', email: 'john@example.com' },
      { id: '2', name: 'Jane Smith', email: 'jane@example.com' },
      { id: '3', name: 'Bob Wilson', email: 'bob@example.com' }
    ];

    for (let i = 0; i < 100; i++) {
      const user = users[Math.floor(Math.random() * users.length)];
      const action = actions[Math.floor(Math.random() * actions.length)];
      const resource = resources[Math.floor(Math.random() * resources.length)];
      const severity = severities[Math.floor(Math.random() * severities.length)];
      const category = categories[Math.floor(Math.random() * categories.length)];
      const success = Math.random() > 0.1; // 90% success rate

      logs.push({
        id: `log-${i}`,
        timestamp: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000), // Last 7 days
        userId: user.id,
        userName: user.name,
        userEmail: user.email,
        action,
        resource,
        resourceId: `${resource.toLowerCase()}-${Math.floor(Math.random() * 1000)}`,
        resourceName: `${resource} ${Math.floor(Math.random() * 100)}`,
        changes: action.includes('UPDATE') ? [
          {
            field: 'name',
            oldValue: 'Old Name',
            newValue: 'New Name',
            fieldType: 'string'
          }
        ] : undefined,
        ipAddress: `192.168.1.${Math.floor(Math.random() * 255)}`,
        userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
        sessionId: `session-${Math.random().toString(36).substr(2, 9)}`,
        severity,
        category,
        success,
        errorMessage: !success ? 'Yetki hatası veya geçersiz işlem' : undefined,
        metadata: {
          source: 'web',
          device: 'desktop',
          location: 'Istanbul, Turkey'
        }
      });
    }

    return logs.sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime());
  }
}