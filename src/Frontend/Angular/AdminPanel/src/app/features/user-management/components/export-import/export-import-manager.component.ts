import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import {
  Download,
  Upload,
  FileText,
  CheckCircle,
  AlertTriangle,
  Clock,
  X,
  Settings,
  Filter,
  Calendar,
  Database,
  Users,
  Shield,
  Key,
  Layers,
  Archive,
  RefreshCw,
  Eye,
  Trash2,
  Play,
  Pause,
  MoreVertical,
  Info,
  AlertCircle,
  FileSpreadsheet,
  FileImage,
  LucideAngularModule
} from 'lucide-angular';

import { UserService } from '../../services/user.service';
import { RoleService } from '../../services/role.service';
import { GroupService } from '../../services/group.service';
import { PermissionService } from '../../services/permission.service';
import { ExportService } from '../../../../shared/services/export.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ErrorHandlerService } from '../../../../core/services/error-handler.service';
import { ConfirmationService } from '../../../../shared/services/confirmation.service';

interface ExportTemplate {
  id: string;
  name: string;
  description: string;
  type: 'users' | 'roles' | 'groups' | 'permissions' | 'all';
  format: 'excel' | 'csv' | 'json' | 'xml' | 'pdf';
  includeFields: string[];
  filters?: Record<string, any>;
  schedule?: ExportSchedule;
  createdAt: Date;
  lastUsed?: Date;
  usageCount: number;
}

interface ExportSchedule {
  enabled: boolean;
  frequency: 'daily' | 'weekly' | 'monthly';
  time: string;
  dayOfWeek?: number;
  dayOfMonth?: number;
  recipients: string[];
}

interface ExportJob {
  id: string;
  templateId?: string;
  templateName: string;
  type: 'users' | 'roles' | 'groups' | 'permissions' | 'all';
  format: 'excel' | 'csv' | 'json' | 'xml' | 'pdf';
  status: 'pending' | 'running' | 'completed' | 'failed' | 'cancelled';
  progress: number;
  totalRecords: number;
  processedRecords: number;
  fileName?: string;
  fileSize?: number;
  downloadUrl?: string;
  createdAt: Date;
  completedAt?: Date;
  error?: string;
  requestedBy: string;
}

interface ImportJob {
  id: string;
  fileName: string;
  fileSize: number;
  type: 'users' | 'roles' | 'groups' | 'permissions';
  format: 'excel' | 'csv' | 'json' | 'xml';
  status: 'pending' | 'validating' | 'importing' | 'completed' | 'failed' | 'cancelled';
  progress: number;
  totalRecords: number;
  processedRecords: number;
  successfulRecords: number;
  failedRecords: number;
  validationErrors: ImportValidationError[];
  preview?: ImportPreview;
  options: ImportOptions;
  createdAt: Date;
  completedAt?: Date;
  uploadedBy: string;
}

interface ImportValidationError {
  row: number;
  field: string;
  value: any;
  error: string;
  severity: 'error' | 'warning';
}

interface ImportPreview {
  headers: string[];
  sampleData: any[][];
  totalRows: number;
  detectedType: string;
  mappingSuggestions: Record<string, string>;
}

interface ImportOptions {
  skipHeader: boolean;
  updateExisting: boolean;
  createMissing: boolean;
  dryRun: boolean;
  batchSize: number;
  fieldMapping: Record<string, string>;
  defaultValues: Record<string, any>;
}

interface ExportStatistics {
  totalExports: number;
  activeJobs: number;
  completedToday: number;
  failedToday: number;
  storageUsed: number;
  mostUsedFormat: string;
  averageFileSize: number;
}

@Component({
  selector: 'app-export-import-manager',
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
    <div class="export-import-container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-content">
          <div class="title-section">
            <h1 class="page-title">
              <lucide-angular [img]="DatabaseIcon" size="28" class="title-icon"></lucide-angular>
              Dışa/İçe Aktarım Yöneticisi
            </h1>
            <p class="page-subtitle">Veri aktarım işlemlerini yönetin ve izleyin</p>
          </div>

          <div class="header-actions">
            <button
              type="button"
              class="btn btn-outline-secondary"
              (click)="refreshData()"
              [disabled]="loading()">
              <lucide-angular [img]="RefreshCwIcon" size="16"></lucide-angular>
              Yenile
            </button>

            <div class="dropdown">
              <button
                type="button"
                class="btn btn-outline-primary dropdown-toggle"
                data-bs-toggle="dropdown">
                <lucide-angular [img]="SettingsIcon" size="16"></lucide-angular>
                Ayarlar
              </button>
              <ul class="dropdown-menu">
                <li><a class="dropdown-item" (click)="openScheduleSettings()">Zamanlama Ayarları</a></li>
                <li><a class="dropdown-item" (click)="openStorageSettings()">Depolama Ayarları</a></li>
                <li><a class="dropdown-item" (click)="openNotificationSettings()">Bildirim Ayarları</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item" (click)="cleanupOldFiles()">Eski Dosyaları Temizle</a></li>
              </ul>
            </div>
          </div>
        </div>

        <!-- Statistics -->
        <div class="statistics-row" *ngIf="statistics()">
          <div class="stat-card">
            <div class="stat-icon">
              <lucide-angular [img]="DownloadIcon" size="20"></lucide-angular>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ statistics()!.totalExports }}</div>
              <div class="stat-label">Toplam Dışa Aktarım</div>
            </div>
          </div>

          <div class="stat-card">
            <div class="stat-icon">
              <lucide-angular [img]="ClockIcon" size="20"></lucide-angular>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ statistics()!.activeJobs }}</div>
              <div class="stat-label">Aktif İşlem</div>
            </div>
          </div>

          <div class="stat-card">
            <div class="stat-icon">
              <lucide-angular [img]="CheckCircleIcon" size="20"></lucide-angular>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ statistics()!.completedToday }}</div>
              <div class="stat-label">Bugün Tamamlanan</div>
            </div>
          </div>

          <div class="stat-card">
            <div class="stat-icon">
              <lucide-angular [img]="ArchiveIcon" size="20"></lucide-angular>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ formatFileSize(statistics()!.storageUsed) }}</div>
              <div class="stat-label">Kullanılan Depolama</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-navigation">
        <nav class="nav nav-tabs">
          <button
            type="button"
            class="nav-link"
            [class.active]="activeTab() === 'export'"
            (click)="setActiveTab('export')">
            <lucide-angular [img]="DownloadIcon" size="16"></lucide-angular>
            Dışa Aktarım
          </button>
          <button
            type="button"
            class="nav-link"
            [class.active]="activeTab() === 'import'"
            (click)="setActiveTab('import')">
            <lucide-angular [img]="UploadIcon" size="16"></lucide-angular>
            İçe Aktarım
          </button>
          <button
            type="button"
            class="nav-link"
            [class.active]="activeTab() === 'templates'"
            (click)="setActiveTab('templates')">
            <lucide-angular [img]="LayersIcon" size="16"></lucide-angular>
            Şablonlar
          </button>
          <button
            type="button"
            class="nav-link"
            [class.active]="activeTab() === 'history'"
            (click)="setActiveTab('history')">
            <lucide-angular [img]="ClockIcon" size="16"></lucide-angular>
            Geçmiş
          </button>
        </nav>
      </div>

      <!-- Export Tab -->
      <div class="tab-content" *ngIf="activeTab() === 'export'">
        <div class="export-section">
          <!-- Quick Export Actions -->
          <div class="quick-actions-grid">
            <div class="quick-action-card" (click)="startQuickExport('users')">
              <div class="action-icon">
                <lucide-angular [img]="UsersIcon" size="24"></lucide-angular>
              </div>
              <div class="action-content">
                <h4 class="action-title">Kullanıcıları Dışa Aktar</h4>
                <p class="action-description">Tüm kullanıcı verilerini Excel/CSV formatında dışa aktarın</p>
              </div>
              <div class="action-arrow">
                <lucide-angular [img]="PlayIcon" size="16"></lucide-angular>
              </div>
            </div>

            <div class="quick-action-card" (click)="startQuickExport('roles')">
              <div class="action-icon">
                <lucide-angular [img]="ShieldIcon" size="24"></lucide-angular>
              </div>
              <div class="action-content">
                <h4 class="action-title">Rolleri Dışa Aktar</h4>
                <p class="action-description">Rol tanımları ve izinlerini dışa aktarın</p>
              </div>
              <div class="action-arrow">
                <lucide-angular [img]="PlayIcon" size="16"></lucide-angular>
              </div>
            </div>

            <div class="quick-action-card" (click)="startQuickExport('groups')">
              <div class="action-icon">
                <lucide-angular [img]="LayersIcon" size="24"></lucide-angular>
              </div>
              <div class="action-content">
                <h4 class="action-title">Grupları Dışa Aktar</h4>
                <p class="action-description">Grup üyelikleri ve izinlerini dışa aktarın</p>
              </div>
              <div class="action-arrow">
                <lucide-angular [img]="PlayIcon" size="16"></lucide-angular>
              </div>
            </div>

            <div class="quick-action-card" (click)="startCustomExport()">
              <div class="action-icon">
                <lucide-angular [img]="SettingsIcon" size="24"></lucide-angular>
              </div>
              <div class="action-content">
                <h4 class="action-title">Özel Dışa Aktarım</h4>
                <p class="action-description">Gelişmiş seçeneklerle özel dışa aktarım yapın</p>
              </div>
              <div class="action-arrow">
                <lucide-angular [img]="PlayIcon" size="16"></lucide-angular>
              </div>
            </div>
          </div>

          <!-- Active Export Jobs -->
          <div class="active-jobs-section" *ngIf="activeExportJobs().length > 0">
            <h3 class="section-title">Aktif Dışa Aktarım İşlemleri</h3>
            <div class="jobs-list">
              <div
                *ngFor="let job of activeExportJobs()"
                class="job-item"
                [class]="'job-' + job.status">
                <div class="job-info">
                  <div class="job-icon">
                    <lucide-angular [img]="getJobStatusIcon(job.status)" size="20"></lucide-angular>
                  </div>
                  <div class="job-details">
                    <div class="job-title">{{ job.templateName }}</div>
                    <div class="job-meta">
                      <span class="job-type">{{ getJobTypeLabel(job.type) }}</span>
                      <span class="job-format">{{ job.format.toUpperCase() }}</span>
                      <span class="job-time">{{ formatRelativeTime(job.createdAt) }}</span>
                    </div>
                  </div>
                </div>

                <div class="job-progress">
                  <div class="progress-info">
                    <span class="progress-text">{{ job.processedRecords }}/{{ job.totalRecords }}</span>
                    <span class="progress-percentage">{{ job.progress }}%</span>
                  </div>
                  <div class="progress-bar">
                    <div
                      class="progress-fill"
                      [style.width.%]="job.progress"
                      [class]="'progress-' + job.status">
                    </div>
                  </div>
                </div>

                <div class="job-actions">
                  <button
                    *ngIf="job.status === 'completed' && job.downloadUrl"
                    type="button"
                    class="btn btn-sm btn-outline-success"
                    (click)="downloadFile(job)">
                    <lucide-angular [img]="DownloadIcon" size="14"></lucide-angular>
                  </button>
                  <button
                    *ngIf="job.status === 'running'"
                    type="button"
                    class="btn btn-sm btn-outline-warning"
                    (click)="cancelJob(job)">
                    <lucide-angular [img]="PauseIcon" size="14"></lucide-angular>
                  </button>
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-secondary"
                    (click)="viewJobDetails(job)">
                    <lucide-angular [img]="EyeIcon" size="14"></lucide-angular>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Import Tab -->
      <div class="tab-content" *ngIf="activeTab() === 'import'">
        <div class="import-section">
          <!-- File Upload Area -->
          <div class="upload-area" (dragover)="onDragOver($event)" (drop)="onFileDrop($event)">
            <div class="upload-content">
              <lucide-angular [img]="UploadIcon" size="48" class="upload-icon"></lucide-angular>
              <h3 class="upload-title">Dosya Yükleyin</h3>
              <p class="upload-description">
                Excel, CSV, JSON veya XML dosyalarını sürükleyip bırakın ya da seçin
              </p>
              <input
                type="file"
                id="fileInput"
                class="file-input"
                accept=".xlsx,.xls,.csv,.json,.xml"
                (change)="onFileSelect($event)"
                multiple>
              <button
                type="button"
                class="btn btn-primary"
                (click)="triggerFileSelect()">
                Dosya Seç
              </button>
              <div class="upload-info">
                <p class="info-text">
                  <lucide-angular [img]="InfoIcon" size="16"></lucide-angular>
                  Desteklenen formatlar: Excel (.xlsx, .xls), CSV (.csv), JSON (.json), XML (.xml)
                </p>
                <p class="info-text">Maksimum dosya boyutu: 50MB</p>
              </div>
            </div>
          </div>

          <!-- Active Import Jobs -->
          <div class="active-import-jobs" *ngIf="activeImportJobs().length > 0">
            <h3 class="section-title">Aktif İçe Aktarım İşlemleri</h3>
            <div class="import-jobs-list">
              <div
                *ngFor="let job of activeImportJobs()"
                class="import-job-item"
                [class]="'job-' + job.status">
                <div class="job-header">
                  <div class="job-file-info">
                    <div class="file-icon">
                      <lucide-angular [img]="getFileIcon(job.format)" size="20"></lucide-angular>
                    </div>
                    <div class="file-details">
                      <div class="file-name">{{ job.fileName }}</div>
                      <div class="file-meta">
                        <span class="file-size">{{ formatFileSize(job.fileSize) }}</span>
                        <span class="file-type">{{ job.type }}</span>
                        <span class="file-records">{{ job.totalRecords }} kayıt</span>
                      </div>
                    </div>
                  </div>

                  <div class="job-status">
                    <span class="status-badge" [class]="'status-' + job.status">
                      <lucide-angular [img]="getJobStatusIcon(job.status)" size="14"></lucide-angular>
                      {{ getJobStatusLabel(job.status) }}
                    </span>
                  </div>
                </div>

                <div class="job-progress" *ngIf="job.status !== 'pending'">
                  <div class="progress-details">
                    <div class="progress-stats">
                      <span class="stat successful">✓ {{ job.successfulRecords }} başarılı</span>
                      <span class="stat failed" *ngIf="job.failedRecords > 0">✗ {{ job.failedRecords }} hata</span>
                      <span class="stat processing" *ngIf="job.status === 'importing'">⟳ İşleniyor...</span>
                    </div>
                    <div class="progress-percentage">{{ job.progress }}%</div>
                  </div>
                  <div class="progress-bar">
                    <div
                      class="progress-fill"
                      [style.width.%]="job.progress"
                      [class]="'progress-' + job.status">
                    </div>
                  </div>
                </div>

                <div class="job-validation" *ngIf="job.validationErrors.length > 0">
                  <div class="validation-summary">
                    <lucide-angular [img]="AlertTriangleIcon" size="16" class="warning-icon"></lucide-angular>
                    <span class="validation-text">{{ job.validationErrors.length }} doğrulama hatası</span>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-warning"
                      (click)="viewValidationErrors(job)">
                      Detayları Gör
                    </button>
                  </div>
                </div>

                <div class="job-actions">
                  <button
                    *ngIf="job.status === 'pending'"
                    type="button"
                    class="btn btn-sm btn-primary"
                    (click)="startImport(job)">
                    <lucide-angular [img]="PlayIcon" size="14"></lucide-angular>
                    Başlat
                  </button>
                  <button
                    *ngIf="job.status === 'validating' || job.status === 'importing'"
                    type="button"
                    class="btn btn-sm btn-outline-danger"
                    (click)="cancelImport(job)">
                    <lucide-angular [img]="XIcon" size="14"></lucide-angular>
                    İptal
                  </button>
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-secondary"
                    (click)="viewImportDetails(job)">
                    <lucide-angular [img]="EyeIcon" size="14"></lucide-angular>
                    Detay
                  </button>
                  <button
                    *ngIf="job.status === 'completed' || job.status === 'failed'"
                    type="button"
                    class="btn btn-sm btn-outline-danger"
                    (click)="removeImportJob(job)">
                    <lucide-angular [img]="Trash2Icon" size="14"></lucide-angular>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Templates Tab -->
      <div class="tab-content" *ngIf="activeTab() === 'templates'">
        <div class="templates-section">
          <div class="templates-header">
            <h3 class="section-title">Dışa Aktarım Şablonları</h3>
            <button
              type="button"
              class="btn btn-primary"
              (click)="createNewTemplate()">
              <lucide-angular [img]="SettingsIcon" size="16"></lucide-angular>
              Yeni Şablon
            </button>
          </div>

          <div class="templates-grid">
            <div
              *ngFor="let template of exportTemplates()"
              class="template-card">
              <div class="template-header">
                <div class="template-icon">
                  <lucide-angular [img]="getTemplateIcon(template.type)" size="24"></lucide-angular>
                </div>
                <div class="template-info">
                  <h4 class="template-name">{{ template.name }}</h4>
                  <p class="template-description">{{ template.description }}</p>
                </div>
                <div class="template-actions">
                  <div class="dropdown">
                    <button
                      type="button"
                      class="btn btn-sm btn-ghost dropdown-toggle"
                      data-bs-toggle="dropdown">
                      <lucide-angular [img]="MoreVerticalIcon" size="16"></lucide-angular>
                    </button>
                    <ul class="dropdown-menu">
                      <li><a class="dropdown-item" (click)="useTemplate(template)">Kullan</a></li>
                      <li><a class="dropdown-item" (click)="editTemplate(template)">Düzenle</a></li>
                      <li><a class="dropdown-item" (click)="duplicateTemplate(template)">Kopyala</a></li>
                      <li><hr class="dropdown-divider"></li>
                      <li><a class="dropdown-item text-danger" (click)="deleteTemplate(template)">Sil</a></li>
                    </ul>
                  </div>
                </div>
              </div>

              <div class="template-details">
                <div class="template-meta">
                  <span class="meta-item">
                    <lucide-angular [img]="FileTextIcon" size="14"></lucide-angular>
                    {{ template.format.toUpperCase() }}
                  </span>
                  <span class="meta-item">
                    <lucide-angular [img]="CalendarIcon" size="14"></lucide-angular>
                    {{ formatDate(template.createdAt) }}
                  </span>
                  <span class="meta-item">
                    <lucide-angular [img]="EyeIcon" size="14"></lucide-angular>
                    {{ template.usageCount }} kez kullanıldı
                  </span>
                </div>

                <div class="template-schedule" *ngIf="template.schedule?.enabled">
                  <div class="schedule-info">
                    <lucide-angular [img]="ClockIcon" size="14" class="schedule-icon"></lucide-angular>
                    <span class="schedule-text">
                      {{ getScheduleText(template.schedule) }}
                    </span>
                  </div>
                </div>
              </div>

              <div class="template-footer">
                <button
                  type="button"
                  class="btn btn-sm btn-outline-primary"
                  (click)="useTemplate(template)">
                  <lucide-angular [img]="PlayIcon" size="14"></lucide-angular>
                  Çalıştır
                </button>
                <button
                  type="button"
                  class="btn btn-sm btn-outline-secondary"
                  (click)="previewTemplate(template)">
                  <lucide-angular [img]="EyeIcon" size="14"></lucide-angular>
                  Önizle
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- History Tab -->
      <div class="tab-content" *ngIf="activeTab() === 'history'">
        <div class="history-section">
          <div class="history-filters">
            <div class="filter-group">
              <label class="filter-label">Tarih Aralığı:</label>
              <input type="date" class="form-control" [(ngModel)]="historyFilters.startDate">
              <span class="filter-separator">-</span>
              <input type="date" class="form-control" [(ngModel)]="historyFilters.endDate">
            </div>

            <div class="filter-group">
              <label class="filter-label">Tür:</label>
              <select class="form-select" [(ngModel)]="historyFilters.type">
                <option value="">Tümü</option>
                <option value="export">Dışa Aktarım</option>
                <option value="import">İçe Aktarım</option>
              </select>
            </div>

            <div class="filter-group">
              <label class="filter-label">Durum:</label>
              <select class="form-select" [(ngModel)]="historyFilters.status">
                <option value="">Tümü</option>
                <option value="completed">Tamamlandı</option>
                <option value="failed">Başarısız</option>
                <option value="cancelled">İptal Edildi</option>
              </select>
            </div>

            <button
              type="button"
              class="btn btn-outline-secondary"
              (click)="applyHistoryFilters()">
              <lucide-angular [img]="FilterIcon" size="16"></lucide-angular>
              Filtrele
            </button>
          </div>

          <div class="history-list">
            <div
              *ngFor="let job of jobHistory()"
              class="history-item"
              [class]="'history-' + job.status">
              <div class="history-icon">
                <lucide-angular [img]="getJobTypeIcon(job)" size="20"></lucide-angular>
              </div>

              <div class="history-content">
                <div class="history-main">
                  <div class="history-title">{{ job.templateName || job.fileName }}</div>
                  <div class="history-meta">
                    <span class="history-type">{{ getJobTypeLabel(job.type) }}</span>
                    <span class="history-format">{{ job.format.toUpperCase() }}</span>
                    <span class="history-date">{{ formatDate(job.createdAt) }}</span>
                    <span class="history-user">{{ job.requestedBy || job.uploadedBy }}</span>
                  </div>
                </div>

                <div class="history-stats">
                  <span class="stat-item">{{ job.totalRecords }} kayıt</span>
                  <span class="stat-item" *ngIf="job.fileSize">{{ formatFileSize(job.fileSize) }}</span>
                  <span class="stat-item" *ngIf="job.completedAt">
                    {{ formatDuration(job.createdAt, job.completedAt) }}
                  </span>
                </div>
              </div>

              <div class="history-status">
                <span class="status-badge" [class]="'status-' + job.status">
                  <lucide-angular [img]="getJobStatusIcon(job.status)" size="14"></lucide-angular>
                  {{ getJobStatusLabel(job.status) }}
                </span>
              </div>

              <div class="history-actions">
                <button
                  *ngIf="job.status === 'completed' && job.downloadUrl"
                  type="button"
                  class="btn btn-sm btn-outline-primary"
                  (click)="downloadFile(job)">
                  <lucide-angular [img]="DownloadIcon" size="14"></lucide-angular>
                </button>
                <button
                  type="button"
                  class="btn btn-sm btn-outline-secondary"
                  (click)="viewJobDetails(job)">
                  <lucide-angular [img]="EyeIcon" size="14"></lucide-angular>
                </button>
                <button
                  type="button"
                  class="btn btn-sm btn-outline-danger"
                  (click)="deleteJob(job)">
                  <lucide-angular [img]="Trash2Icon" size="14"></lucide-angular>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Loading Overlay -->
      <div class="loading-overlay" *ngIf="loading()">
        <div class="loading-spinner">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Yükleniyor...</span>
          </div>
          <p class="loading-text">Veriler yükleniyor...</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .export-import-container {
      padding: 1.5rem;
      max-width: 1400px;
      margin: 0 auto;
      background: var(--bs-gray-50);
      min-height: 100vh;
    }

    .page-header {
      background: white;
      border-radius: 0.75rem;
      padding: 1.5rem;
      margin-bottom: 2rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    }

    .header-content {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1.5rem;
    }

    .title-section {
      flex: 1;
    }

    .page-title {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin: 0 0 0.5rem 0;
      font-size: 2rem;
      font-weight: 700;
      color: var(--bs-gray-900);
    }

    .title-icon {
      color: var(--bs-primary);
    }

    .page-subtitle {
      margin: 0;
      color: var(--bs-gray-600);
      font-size: 1.1rem;
    }

    .header-actions {
      display: flex;
      gap: 1rem;
      align-items: center;
    }

    .statistics-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
    }

    .stat-card {
      background: var(--bs-gray-50);
      border-radius: 0.5rem;
      padding: 1.25rem;
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .stat-icon {
      background: var(--bs-primary);
      color: white;
      border-radius: 0.5rem;
      padding: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .stat-content {
      flex: 1;
    }

    .stat-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--bs-gray-900);
      margin-bottom: 0.25rem;
    }

    .stat-label {
      font-size: 0.9rem;
      color: var(--bs-gray-600);
    }

    .tab-navigation {
      background: white;
      border-radius: 0.75rem 0.75rem 0 0;
      padding: 0 1.5rem;
      margin-bottom: 0;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    }

    .nav-tabs {
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .nav-link {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 1rem 1.5rem;
      border: none;
      background: none;
      color: var(--bs-gray-600);
      font-weight: 500;
      border-bottom: 2px solid transparent;
      transition: all 0.2s ease;
    }

    .nav-link:hover {
      color: var(--bs-primary);
      background: var(--bs-gray-50);
    }

    .nav-link.active {
      color: var(--bs-primary);
      border-bottom-color: var(--bs-primary);
    }

    .tab-content {
      background: white;
      border-radius: 0 0 0.75rem 0.75rem;
      padding: 2rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
      min-height: 600px;
    }

    .quick-actions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .quick-action-card {
      background: var(--bs-gray-50);
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.75rem;
      padding: 1.5rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .quick-action-card:hover {
      background: var(--bs-primary-bg);
      border-color: var(--bs-primary);
      transform: translateY(-2px);
      box-shadow: 0 8px 25px rgba(0, 0, 0, 0.12);
    }

    .action-icon {
      background: var(--bs-primary);
      color: white;
      border-radius: 0.75rem;
      padding: 1rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .action-content {
      flex: 1;
    }

    .action-title {
      margin: 0 0 0.5rem 0;
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .action-description {
      margin: 0;
      color: var(--bs-gray-600);
      font-size: 0.9rem;
    }

    .action-arrow {
      color: var(--bs-gray-400);
    }

    .section-title {
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--bs-gray-900);
      margin: 0 0 1.5rem 0;
    }

    .jobs-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .job-item {
      background: var(--bs-gray-50);
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      padding: 1.25rem;
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }

    .job-item.job-running {
      border-color: var(--bs-primary);
      background: var(--bs-primary-bg);
    }

    .job-item.job-completed {
      border-color: var(--bs-success);
      background: var(--bs-success-bg);
    }

    .job-item.job-failed {
      border-color: var(--bs-danger);
      background: var(--bs-danger-bg);
    }

    .job-info {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex: 1;
    }

    .job-icon {
      background: white;
      border-radius: 0.5rem;
      padding: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .job-details {
      flex: 1;
    }

    .job-title {
      font-weight: 600;
      color: var(--bs-gray-900);
      margin-bottom: 0.25rem;
    }

    .job-meta {
      display: flex;
      gap: 1rem;
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .job-progress {
      min-width: 200px;
    }

    .progress-info {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .progress-bar {
      background: var(--bs-gray-200);
      border-radius: 0.25rem;
      height: 8px;
      overflow: hidden;
    }

    .progress-fill {
      height: 100%;
      border-radius: 0.25rem;
      transition: width 0.3s ease;
    }

    .progress-running { background: var(--bs-primary); }
    .progress-completed { background: var(--bs-success); }
    .progress-failed { background: var(--bs-danger); }

    .job-actions {
      display: flex;
      gap: 0.5rem;
    }

    .upload-area {
      border: 2px dashed var(--bs-gray-300);
      border-radius: 0.75rem;
      padding: 3rem;
      text-align: center;
      background: var(--bs-gray-50);
      transition: all 0.3s ease;
      margin-bottom: 2rem;
    }

    .upload-area:hover,
    .upload-area.drag-over {
      border-color: var(--bs-primary);
      background: var(--bs-primary-bg);
    }

    .upload-content {
      max-width: 400px;
      margin: 0 auto;
    }

    .upload-icon {
      color: var(--bs-gray-400);
      margin-bottom: 1rem;
    }

    .upload-title {
      font-size: 1.5rem;
      font-weight: 600;
      color: var(--bs-gray-900);
      margin-bottom: 0.75rem;
    }

    .upload-description {
      color: var(--bs-gray-600);
      margin-bottom: 2rem;
    }

    .file-input {
      display: none;
    }

    .upload-info {
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--bs-gray-200);
    }

    .info-text {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      margin: 0.5rem 0;
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .import-jobs-list {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .import-job-item {
      background: var(--bs-gray-50);
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.75rem;
      padding: 1.5rem;
    }

    .job-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .job-file-info {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .file-icon {
      background: var(--bs-primary);
      color: white;
      border-radius: 0.5rem;
      padding: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .file-details {
      flex: 1;
    }

    .file-name {
      font-weight: 600;
      color: var(--bs-gray-900);
      margin-bottom: 0.25rem;
    }

    .file-meta {
      display: flex;
      gap: 1rem;
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .status-badge {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.75rem;
      border-radius: 0.375rem;
      font-size: 0.85rem;
      font-weight: 500;
    }

    .status-pending {
      background: var(--bs-secondary-bg);
      color: var(--bs-secondary);
    }

    .status-running,
    .status-validating,
    .status-importing {
      background: var(--bs-primary-bg);
      color: var(--bs-primary);
    }

    .status-completed {
      background: var(--bs-success-bg);
      color: var(--bs-success);
    }

    .status-failed,
    .status-cancelled {
      background: var(--bs-danger-bg);
      color: var(--bs-danger);
    }

    .progress-details {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.75rem;
    }

    .progress-stats {
      display: flex;
      gap: 1rem;
      font-size: 0.85rem;
    }

    .stat.successful { color: var(--bs-success); }
    .stat.failed { color: var(--bs-danger); }
    .stat.processing { color: var(--bs-primary); }

    .job-validation {
      background: var(--bs-warning-bg);
      border: 1px solid var(--bs-warning-border);
      border-radius: 0.5rem;
      padding: 1rem;
      margin: 1rem 0;
    }

    .validation-summary {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .warning-icon {
      color: var(--bs-warning);
    }

    .validation-text {
      flex: 1;
      color: var(--bs-warning);
      font-weight: 500;
    }

    .templates-section {
      margin: 0;
    }

    .templates-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
    }

    .templates-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 1.5rem;
    }

    .template-card {
      background: var(--bs-gray-50);
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.75rem;
      padding: 1.5rem;
      transition: all 0.3s ease;
    }

    .template-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 25px rgba(0, 0, 0, 0.12);
      border-color: var(--bs-primary);
    }

    .template-header {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .template-icon {
      background: var(--bs-primary);
      color: white;
      border-radius: 0.5rem;
      padding: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .template-info {
      flex: 1;
    }

    .template-name {
      margin: 0 0 0.5rem 0;
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .template-description {
      margin: 0;
      color: var(--bs-gray-600);
      font-size: 0.9rem;
    }

    .template-details {
      margin-bottom: 1.5rem;
    }

    .template-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .meta-item {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .template-schedule {
      background: var(--bs-info-bg);
      border: 1px solid var(--bs-info-border);
      border-radius: 0.375rem;
      padding: 0.75rem;
    }

    .schedule-info {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .schedule-icon {
      color: var(--bs-info);
    }

    .schedule-text {
      color: var(--bs-info);
      font-size: 0.85rem;
      font-weight: 500;
    }

    .template-footer {
      display: flex;
      gap: 0.75rem;
    }

    .template-footer .btn {
      flex: 1;
    }

    .history-filters {
      background: var(--bs-gray-50);
      border-radius: 0.5rem;
      padding: 1.25rem;
      margin-bottom: 1.5rem;
      display: flex;
      gap: 1.5rem;
      align-items: end;
      flex-wrap: wrap;
    }

    .filter-group {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      min-width: 120px;
    }

    .filter-group.date-range {
      display: flex;
      flex-direction: row;
      align-items: center;
      gap: 0.5rem;
    }

    .filter-label {
      font-size: 0.85rem;
      font-weight: 500;
      color: var(--bs-gray-700);
      margin: 0;
    }

    .filter-separator {
      color: var(--bs-gray-500);
      font-weight: 500;
    }

    .history-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .history-item {
      background: var(--bs-gray-50);
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      padding: 1.25rem;
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }

    .history-icon {
      background: white;
      border-radius: 0.5rem;
      padding: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .history-content {
      flex: 1;
    }

    .history-main {
      margin-bottom: 0.5rem;
    }

    .history-title {
      font-weight: 600;
      color: var(--bs-gray-900);
      margin-bottom: 0.25rem;
    }

    .history-meta {
      display: flex;
      gap: 1rem;
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .history-stats {
      display: flex;
      gap: 1rem;
      font-size: 0.85rem;
      color: var(--bs-gray-600);
    }

    .history-status {
      min-width: 120px;
    }

    .history-actions {
      display: flex;
      gap: 0.5rem;
    }

    .loading-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255, 255, 255, 0.8);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1050;
    }

    .loading-spinner {
      text-align: center;
    }

    .loading-text {
      margin-top: 1rem;
      color: var(--bs-gray-600);
    }

    @media (max-width: 768px) {
      .export-import-container {
        padding: 1rem;
      }

      .header-content {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .header-actions {
        justify-content: space-between;
      }

      .statistics-row {
        grid-template-columns: 1fr;
      }

      .quick-actions-grid {
        grid-template-columns: 1fr;
      }

      .templates-grid {
        grid-template-columns: 1fr;
      }

      .history-filters {
        flex-direction: column;
        align-items: stretch;
      }

      .job-item,
      .import-job-item,
      .history-item {
        flex-direction: column;
        align-items: stretch;
        gap: 1rem;
      }

      .job-progress {
        min-width: auto;
      }
    }
  `]
})
export class ExportImportManagerComponent implements OnInit {
  // Dependency Injection
  private readonly userService = inject(UserService);
  private readonly roleService = inject(RoleService);
  private readonly groupService = inject(GroupService);
  private readonly permissionService = inject(PermissionService);
  private readonly exportService = inject(ExportService);
  private readonly loadingService = inject(LoadingService);
  private readonly errorHandler = inject(ErrorHandlerService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);

  // Icons
  readonly DownloadIcon = Download;
  readonly UploadIcon = Upload;
  readonly FileTextIcon = FileText;
  readonly CheckCircleIcon = CheckCircle;
  readonly AlertTriangleIcon = AlertTriangle;
  readonly ClockIcon = Clock;
  readonly XIcon = X;
  readonly SettingsIcon = Settings;
  readonly FilterIcon = Filter;
  readonly CalendarIcon = Calendar;
  readonly DatabaseIcon = Database;
  readonly UsersIcon = Users;
  readonly ShieldIcon = Shield;
  readonly KeyIcon = Key;
  readonly LayersIcon = Layers;
  readonly ArchiveIcon = Archive;
  readonly RefreshCwIcon = RefreshCw;
  readonly EyeIcon = Eye;
  readonly Trash2Icon = Trash2;
  readonly PlayIcon = Play;
  readonly PauseIcon = Pause;
  readonly MoreVerticalIcon = MoreVertical;
  readonly InfoIcon = Info;
  readonly AlertCircleIcon = AlertCircle;
  readonly FileSpreadsheetIcon = FileSpreadsheet;
  readonly FileImageIcon = FileImage;

  // State Signals
  loading = signal(false);
  activeTab = signal<'export' | 'import' | 'templates' | 'history'>('export');
  statistics = signal<ExportStatistics | null>(null);
  exportJobs = signal<ExportJob[]>([]);
  importJobs = signal<ImportJob[]>([]);
  exportTemplates = signal<ExportTemplate[]>([]);
  jobHistory = signal<(ExportJob | ImportJob)[]>([]);

  // Filters
  historyFilters = {
    startDate: '',
    endDate: '',
    type: '',
    status: ''
  };

  // Computed Values
  activeExportJobs = computed(() => {
    return this.exportJobs().filter(job =>
      job.status === 'pending' || job.status === 'running'
    );
  });

  activeImportJobs = computed(() => {
    return this.importJobs().filter(job =>
      job.status === 'pending' || job.status === 'validating' || job.status === 'importing'
    );
  });

  ngOnInit(): void {
    this.loadData();
  }

  // Data Loading Methods
  async loadData(): Promise<void> {
    try {
      this.loading.set(true);
      await Promise.all([
        this.loadStatistics(),
        this.loadExportJobs(),
        this.loadImportJobs(),
        this.loadExportTemplates(),
        this.loadJobHistory()
      ]);
    } catch (error) {
      this.errorHandler.handleError(error, 'Veriler yüklenirken hata oluştu');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadStatistics(): Promise<void> {
    // Mock data - replace with actual service call
    const mockStats: ExportStatistics = {
      totalExports: 1247,
      activeJobs: 3,
      completedToday: 28,
      failedToday: 2,
      storageUsed: 1024 * 1024 * 512, // 512MB
      mostUsedFormat: 'excel',
      averageFileSize: 1024 * 1024 * 2.5 // 2.5MB
    };
    this.statistics.set(mockStats);
  }

  private async loadExportJobs(): Promise<void> {
    // Mock data - replace with actual service call
    const mockJobs: ExportJob[] = [
      {
        id: '1',
        templateName: 'Kullanıcı Listesi',
        type: 'users',
        format: 'excel',
        status: 'running',
        progress: 65,
        totalRecords: 1000,
        processedRecords: 650,
        createdAt: new Date(Date.now() - 1000 * 60 * 5),
        requestedBy: 'admin@platform.com'
      }
    ];
    this.exportJobs.set(mockJobs);
  }

  private async loadImportJobs(): Promise<void> {
    // Mock data - replace with actual service call
    const mockJobs: ImportJob[] = [];
    this.importJobs.set(mockJobs);
  }

  private async loadExportTemplates(): Promise<void> {
    // Mock data - replace with actual service call
    const mockTemplates: ExportTemplate[] = [
      {
        id: '1',
        name: 'Tüm Kullanıcılar',
        description: 'Sistemdeki tüm kullanıcıları detaylı bilgilerle birlikte dışa aktarır',
        type: 'users',
        format: 'excel',
        includeFields: ['userName', 'email', 'firstName', 'lastName', 'isActive'],
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 7),
        usageCount: 25
      }
    ];
    this.exportTemplates.set(mockTemplates);
  }

  private async loadJobHistory(): Promise<void> {
    // Mock data - replace with actual service call
    this.jobHistory.set([]);
  }

  async refreshData(): Promise<void> {
    await this.loadData();
  }

  // Tab Navigation
  setActiveTab(tab: 'export' | 'import' | 'templates' | 'history'): void {
    this.activeTab.set(tab);
  }

  // Export Methods
  startQuickExport(type: 'users' | 'roles' | 'groups' | 'permissions'): void {
    // Start quick export with default template
  }

  startCustomExport(): void {
    // Open custom export configuration modal
  }

  // Import Methods
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    const target = event.target as HTMLElement;
    target.classList.add('drag-over');
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    const target = event.target as HTMLElement;
    target.classList.remove('drag-over');

    const files = event.dataTransfer?.files;
    if (files) {
      this.processFiles(Array.from(files));
    }
  }

  triggerFileSelect(): void {
    const fileInput = document.getElementById('fileInput') as HTMLInputElement;
    fileInput.click();
  }

  onFileSelect(event: Event): void {
    const target = event.target as HTMLInputElement;
    const files = target.files;
    if (files) {
      this.processFiles(Array.from(files));
    }
  }

  private processFiles(files: File[]): void {
    files.forEach(file => {
      if (this.validateFile(file)) {
        this.createImportJob(file);
      }
    });
  }

  private validateFile(file: File): boolean {
    const allowedTypes = [
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      'application/vnd.ms-excel',
      'text/csv',
      'application/json',
      'application/xml'
    ];

    const maxSize = 50 * 1024 * 1024; // 50MB

    if (!allowedTypes.includes(file.type)) {
      this.errorHandler.handleError(null, 'Desteklenmeyen dosya formatı');
      return false;
    }

    if (file.size > maxSize) {
      this.errorHandler.handleError(null, 'Dosya boyutu çok büyük (max 50MB)');
      return false;
    }

    return true;
  }

  private createImportJob(file: File): void {
    const job: ImportJob = {
      id: Date.now().toString(),
      fileName: file.name,
      fileSize: file.size,
      type: 'users', // This would be detected from file content
      format: this.getFileFormat(file),
      status: 'pending',
      progress: 0,
      totalRecords: 0,
      processedRecords: 0,
      successfulRecords: 0,
      failedRecords: 0,
      validationErrors: [],
      options: {
        skipHeader: true,
        updateExisting: false,
        createMissing: true,
        dryRun: false,
        batchSize: 100,
        fieldMapping: {},
        defaultValues: {}
      },
      createdAt: new Date(),
      uploadedBy: 'current-user@platform.com'
    };

    this.importJobs.update(jobs => [...jobs, job]);
  }

  private getFileFormat(file: File): 'excel' | 'csv' | 'json' | 'xml' {
    if (file.type.includes('sheet') || file.type.includes('excel')) return 'excel';
    if (file.type.includes('csv')) return 'csv';
    if (file.type.includes('json')) return 'json';
    if (file.type.includes('xml')) return 'xml';
    return 'excel';
  }

  startImport(job: ImportJob): void {
    // Start import process
    job.status = 'validating';
    this.importJobs.update(jobs => [...jobs]);
  }

  cancelImport(job: ImportJob): void {
    job.status = 'cancelled';
    this.importJobs.update(jobs => [...jobs]);
  }

  removeImportJob(job: ImportJob): void {
    this.importJobs.update(jobs => jobs.filter(j => j.id !== job.id));
  }

  // Template Methods
  createNewTemplate(): void {
    // Open template creation modal
  }

  useTemplate(template: ExportTemplate): void {
    // Start export using template
  }

  editTemplate(template: ExportTemplate): void {
    // Open template edit modal
  }

  duplicateTemplate(template: ExportTemplate): void {
    // Create a copy of the template
  }

  deleteTemplate(template: ExportTemplate): void {
    // Delete template after confirmation
  }

  previewTemplate(template: ExportTemplate): void {
    // Show template preview
  }

  // Job Management Methods
  cancelJob(job: ExportJob): void {
    job.status = 'cancelled';
    this.exportJobs.update(jobs => [...jobs]);
  }

  downloadFile(job: ExportJob | ImportJob): void {
    // Download the generated file
    if ('downloadUrl' in job && job.downloadUrl) {
      window.open(job.downloadUrl, '_blank');
    }
  }

  viewJobDetails(job: ExportJob | ImportJob): void {
    // Open job details modal
  }

  viewValidationErrors(job: ImportJob): void {
    // Open validation errors modal
  }

  viewImportDetails(job: ImportJob): void {
    // Open import details modal
  }

  deleteJob(job: ExportJob | ImportJob): void {
    // Delete job after confirmation
  }

  // History Methods
  applyHistoryFilters(): void {
    // Apply filters to history list
    this.loadJobHistory();
  }

  // Settings Methods
  openScheduleSettings(): void {
    // Open schedule settings modal
  }

  openStorageSettings(): void {
    // Open storage settings modal
  }

  openNotificationSettings(): void {
    // Open notification settings modal
  }

  cleanupOldFiles(): void {
    // Clean up old export files
  }

  // Utility Methods
  getJobStatusIcon(status: string): any {
    const iconMap: Record<string, any> = {
      'pending': ClockIcon,
      'running': RefreshCwIcon,
      'validating': RefreshCwIcon,
      'importing': RefreshCwIcon,
      'completed': CheckCircleIcon,
      'failed': AlertTriangleIcon,
      'cancelled': XIcon
    };
    return iconMap[status] || ClockIcon;
  }

  getJobStatusLabel(status: string): string {
    const labelMap: Record<string, string> = {
      'pending': 'Bekliyor',
      'running': 'Çalışıyor',
      'validating': 'Doğrulanıyor',
      'importing': 'İçe Aktarılıyor',
      'completed': 'Tamamlandı',
      'failed': 'Başarısız',
      'cancelled': 'İptal Edildi'
    };
    return labelMap[status] || status;
  }

  getJobTypeLabel(type: string): string {
    const labelMap: Record<string, string> = {
      'users': 'Kullanıcılar',
      'roles': 'Roller',
      'groups': 'Gruplar',
      'permissions': 'İzinler',
      'all': 'Tüm Veriler'
    };
    return labelMap[type] || type;
  }

  getJobTypeIcon(job: any): any {
    if ('downloadUrl' in job) {
      return DownloadIcon;
    } else {
      return UploadIcon;
    }
  }

  getTemplateIcon(type: string): any {
    const iconMap: Record<string, any> = {
      'users': UsersIcon,
      'roles': ShieldIcon,
      'groups': LayersIcon,
      'permissions': KeyIcon,
      'all': DatabaseIcon
    };
    return iconMap[type] || FileTextIcon;
  }

  getFileIcon(format: string): any {
    const iconMap: Record<string, any> = {
      'excel': FileSpreadsheetIcon,
      'csv': FileTextIcon,
      'json': FileTextIcon,
      'xml': FileTextIcon
    };
    return iconMap[format] || FileTextIcon;
  }

  getScheduleText(schedule: ExportSchedule): string {
    const frequencyMap: Record<string, string> = {
      'daily': 'Günlük',
      'weekly': 'Haftalık',
      'monthly': 'Aylık'
    };
    return `${frequencyMap[schedule.frequency]} ${schedule.time}`;
  }

  formatFileSize(bytes: number): string {
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 Bytes';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  }

  formatRelativeTime(date: Date): string {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / (1000 * 60));
    const hours = Math.floor(diff / (1000 * 60 * 60));

    if (minutes < 1) return 'Az önce';
    if (minutes < 60) return `${minutes} dakika önce`;
    if (hours < 24) return `${hours} saat önce`;
    return date.toLocaleDateString('tr-TR');
  }

  formatDate(date: Date): string {
    return date.toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatDuration(start: Date, end: Date): string {
    const diff = end.getTime() - start.getTime();
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (hours > 0) return `${hours}s ${minutes % 60}d`;
    if (minutes > 0) return `${minutes}d ${seconds % 60}s`;
    return `${seconds}s`;
  }
}