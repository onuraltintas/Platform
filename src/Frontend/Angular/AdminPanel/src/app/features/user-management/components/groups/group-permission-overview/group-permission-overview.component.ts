import { Component, OnInit, Input, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import {
  Shield,
  Lock,
  Unlock,
  Eye,
  EyeOff,
  Search,
  Filter,
  Plus,
  Minus,
  Check,
  X,
  AlertTriangle,
  Info,
  Settings,
  Users,
  Server,
  Database,
  Globe,
  FileText,
  MoreVertical,
  ChevronDown,
  ChevronRight,
  Grid,
  List,
  Download,
  Upload,
  RefreshCw,
  Copy,
  Trash2,
  Edit2,
  Save,
  RotateCcw,
  LucideAngularModule
} from 'lucide-angular';

import { GroupService } from '../../../services/group.service';
import { ConfirmationService } from '../../../../../shared/services/confirmation.service';
import { ErrorHandlerService } from '../../../../../core/services/error-handler.service';

import {
  GroupDto,
  GroupPermissionAssignment
} from '../../../models/group.models';
import { PermissionDto } from '../../../models/permission.models';

interface PermissionGroup {
  service: string;
  displayName: string;
  description?: string;
  permissions: PermissionWithStatus[];
  expanded: boolean;
  summary: {
    total: number;
    granted: number;
    denied: number;
    inherited: number;
  };
}

interface PermissionWithStatus extends PermissionDto {
  granted: boolean;
  inherited: boolean;
  inheritedFrom?: string;
  modified: boolean;
  originalState: boolean;
  createdAt: Date;
  roleCount: number;
  userCount: number;
}

interface PermissionFilter {
  search: string;
  service: string;
  status: 'all' | 'granted' | 'denied' | 'inherited' | 'modified';
  category: string;
}

interface PermissionChangeLog {
  permissionId: string;
  permissionName: string;
  action: 'grant' | 'revoke';
  previousState: boolean;
  newState: boolean;
}

interface ServiceMetrics {
  serviceName: string;
  totalPermissions: number;
  grantedPermissions: number;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
  coverage: number;
}

@Component({
  selector: 'app-group-permission-overview',
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
    <div class="permission-overview-container">
      <!-- Header Section -->
      <div class="page-header">
        <div class="header-content">
          <div class="title-section">
            <div class="breadcrumb-nav">
              <a [routerLink]="['/user-management/groups']" class="breadcrumb-link">Gruplar</a>
              <span class="breadcrumb-separator">/</span>
              <span class="breadcrumb-current">{{ group()?.name || 'Grup' }}</span>
              <span class="breadcrumb-separator">/</span>
              <span class="breadcrumb-current">İzin Yönetimi</span>
            </div>

            <div class="title-with-icon">
              <lucide-angular [img]="ShieldIcon" size="24" class="title-icon"></lucide-angular>
              <h1 class="page-title">{{ group()?.name }} - İzin Yönetimi</h1>
            </div>
            <p class="page-subtitle">Grup izinlerini görüntüleyin, düzenleyin ve yönetin</p>
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

            <button
              type="button"
              class="btn btn-outline-info"
              (click)="viewPermissionMatrix()"
              [disabled]="loading()">
              <lucide-angular [img]="GridIcon" size="16"></lucide-angular>
              İzin Matrisi
            </button>

            <div class="dropdown">
              <button
                type="button"
                class="btn btn-outline-secondary dropdown-toggle"
                data-bs-toggle="dropdown">
                <lucide-angular [img]="DownloadIcon" size="16"></lucide-angular>
                Dışa Aktar
              </button>
              <ul class="dropdown-menu">
                <li><a class="dropdown-item" (click)="exportPermissions('excel')">Excel</a></li>
                <li><a class="dropdown-item" (click)="exportPermissions('csv')">CSV</a></li>
                <li><a class="dropdown-item" (click)="exportPermissions('pdf')">PDF Rapor</a></li>
              </ul>
            </div>

            <button
              type="button"
              class="btn btn-primary"
              (click)="openPermissionWizard()"
              [disabled]="loading()">
              <lucide-angular [img]="PlusIcon" size="16"></lucide-angular>
              İzin Ekle
            </button>
          </div>
        </div>

        <!-- Group Info and Summary -->
        <div class="group-permission-summary" *ngIf="group()">
          <div class="group-info-section">
            <div class="group-basic-info">
              <h4 class="group-name">{{ group()!.name }}</h4>
              <span class="system-badge" *ngIf="group()!.isSystemGroup">Sistem Grubu</span>
              <p class="group-description">{{ group()!.description || 'Açıklama bulunmamaktadır.' }}</p>
            </div>

            <div class="permission-stats">
              <div class="stat-card">
                <div class="stat-value">{{ permissionSummary().total }}</div>
                <div class="stat-label">Toplam İzin</div>
                <lucide-angular [img]="ShieldIcon" size="20" class="stat-icon"></lucide-angular>
              </div>
              <div class="stat-card granted">
                <div class="stat-value">{{ permissionSummary().granted }}</div>
                <div class="stat-label">Verilen</div>
                <lucide-angular [img]="CheckIcon" size="20" class="stat-icon"></lucide-angular>
              </div>
              <div class="stat-card denied">
                <div class="stat-value">{{ permissionSummary().denied }}</div>
                <div class="stat-label">Reddedilen</div>
                <lucide-angular [img]="XIcon" size="20" class="stat-icon"></lucide-angular>
              </div>
              <div class="stat-card inherited">
                <div class="stat-value">{{ permissionSummary().inherited }}</div>
                <div class="stat-label">Miras Alınan</div>
                <lucide-angular [img]="InfoIcon" size="20" class="stat-icon"></lucide-angular>
              </div>
            </div>
          </div>

          <!-- Service Metrics -->
          <div class="service-metrics" *ngIf="serviceMetrics().length > 0">
            <h6 class="metrics-title">Servis Bazında İzin Durumu</h6>
            <div class="metrics-grid">
              <div
                *ngFor="let metric of serviceMetrics()"
                class="metric-card"
                [class]="'risk-' + metric.riskLevel">
                <div class="metric-header">
                  <span class="service-name">{{ metric.serviceName }}</span>
                  <span class="risk-badge" [class]="'risk-' + metric.riskLevel">
                    {{ getRiskLevelText(metric.riskLevel) }}
                  </span>
                </div>
                <div class="metric-body">
                  <div class="coverage-bar">
                    <div
                      class="coverage-fill"
                      [style.width.%]="metric.coverage">
                    </div>
                  </div>
                  <div class="metric-stats">
                    <span class="coverage-text">{{ metric.coverage }}% kapsama</span>
                    <span class="permission-count">{{ metric.grantedPermissions }}/{{ metric.totalPermissions }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Controls and Filters -->
      <div class="controls-section">
        <div class="filters-row">
          <!-- Search -->
          <div class="search-input-group">
            <lucide-angular [img]="SearchIcon" size="16" class="search-icon"></lucide-angular>
            <input
              type="text"
              class="form-control search-input"
              placeholder="İzin ara..."
              [(ngModel)]="filters().search"
              (input)="onSearchChange($event)"
              [disabled]="loading()">
            <button
              *ngIf="filters().search"
              type="button"
              class="btn-clear-search"
              (click)="clearSearch()">
              <lucide-angular [img]="XIcon" size="14"></lucide-angular>
            </button>
          </div>

          <!-- Service Filter -->
          <div class="filter-group">
            <select
              class="form-select"
              [(ngModel)]="filters().service"
              (change)="applyFilters()">
              <option value="">Tüm Servisler</option>
              <option *ngFor="let service of availableServices()" [value]="service">
                {{ service }}
              </option>
            </select>
          </div>

          <!-- Status Filter -->
          <div class="filter-group">
            <select
              class="form-select"
              [(ngModel)]="filters().status"
              (change)="applyFilters()">
              <option value="all">Tüm Durumlar</option>
              <option value="granted">Verilen İzinler</option>
              <option value="denied">Reddedilen İzinler</option>
              <option value="inherited">Miras Alınan</option>
              <option value="modified">Değiştirilmiş</option>
            </select>
          </div>

          <!-- Category Filter -->
          <div class="filter-group">
            <select
              class="form-select"
              [(ngModel)]="filters().category"
              (change)="applyFilters()">
              <option value="">Tüm Kategoriler</option>
              <option *ngFor="let category of availableCategories()" [value]="category">
                {{ category }}
              </option>
            </select>
          </div>

          <!-- View Mode Toggle -->
          <div class="view-toggle">
            <div class="btn-group" role="group">
              <button
                type="button"
                class="btn btn-outline-secondary"
                [class.active]="viewMode() === 'grouped'"
                (click)="setViewMode('grouped')">
                <lucide-angular [img]="ListIcon" size="16"></lucide-angular>
                Gruplu
              </button>
              <button
                type="button"
                class="btn btn-outline-secondary"
                [class.active]="viewMode() === 'flat'"
                (click)="setViewMode('flat')">
                <lucide-angular [img]="GridIcon" size="16"></lucide-angular>
                Düz Liste
              </button>
            </div>
          </div>
        </div>

        <!-- Quick Actions -->
        <div class="quick-actions" *ngIf="hasModifications()">
          <div class="modification-info">
            <lucide-angular [img]="AlertTriangleIcon" size="16" class="warning-icon"></lucide-angular>
            <span class="modification-text">{{ modificationCount() }} değişiklik yapıldı</span>
          </div>

          <div class="action-buttons">
            <button
              type="button"
              class="btn btn-outline-secondary"
              (click)="resetChanges()"
              [disabled]="saving()">
              <lucide-angular [img]="RotateCcwIcon" size="16"></lucide-angular>
              Değişiklikleri Geri Al
            </button>
            <button
              type="button"
              class="btn btn-success"
              (click)="saveChanges()"
              [disabled]="saving()">
              <lucide-angular [img]="SaveIcon" size="16"></lucide-angular>
              Değişiklikleri Kaydet
            </button>
          </div>
        </div>
      </div>

      <!-- Permission Groups - Grouped View -->
      <div class="permissions-content" *ngIf="viewMode() === 'grouped'">
        <div class="permission-groups">
          <div
            *ngFor="let group of filteredPermissionGroups(); trackBy: trackByGroupService"
            class="permission-group"
            [class.expanded]="group.expanded">

            <!-- Group Header -->
            <div class="group-header" (click)="toggleGroupExpansion(group)">
              <div class="group-title-section">
                <button type="button" class="expand-button">
                  <lucide-angular
                    [img]="group.expanded ? ChevronDownIcon : ChevronRightIcon"
                    size="16">
                  </lucide-angular>
                </button>
                <div class="group-info">
                  <h5 class="group-title">{{ group.displayName }}</h5>
                  <p class="group-description" *ngIf="group.description">{{ group.description }}</p>
                </div>
              </div>

              <div class="group-summary">
                <div class="summary-stats">
                  <span class="stat-item">
                    <span class="stat-value">{{ group.summary.granted }}</span>
                    <span class="stat-label">Verilen</span>
                  </span>
                  <span class="stat-item">
                    <span class="stat-value">{{ group.summary.total }}</span>
                    <span class="stat-label">Toplam</span>
                  </span>
                </div>
                <div class="group-progress">
                  <div class="progress-bar">
                    <div
                      class="progress-fill"
                      [style.width.%]="(group.summary.granted / group.summary.total) * 100">
                    </div>
                  </div>
                  <span class="progress-text">
                    {{ Math.round((group.summary.granted / group.summary.total) * 100) }}%
                  </span>
                </div>
              </div>

              <div class="group-actions">
                <div class="dropdown">
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-secondary dropdown-toggle"
                    data-bs-toggle="dropdown"
                    (click)="$event.stopPropagation()">
                    <lucide-angular [img]="MoreVerticalIcon" size="14"></lucide-angular>
                  </button>
                  <ul class="dropdown-menu">
                    <li>
                      <a class="dropdown-item" (click)="grantAllInGroup(group); $event.stopPropagation()">
                        <lucide-angular [img]="CheckIcon" size="14"></lucide-angular>
                        Tümünü Ver
                      </a>
                    </li>
                    <li>
                      <a class="dropdown-item" (click)="revokeAllInGroup(group); $event.stopPropagation()">
                        <lucide-angular [img]="XIcon" size="14"></lucide-angular>
                        Tümünü Reddet
                      </a>
                    </li>
                    <li><hr class="dropdown-divider"></li>
                    <li>
                      <a class="dropdown-item" (click)="copyGroupPermissions(group); $event.stopPropagation()">
                        <lucide-angular [img]="CopyIcon" size="14"></lucide-angular>
                        İzinleri Kopyala
                      </a>
                    </li>
                  </ul>
                </div>
              </div>
            </div>

            <!-- Group Content -->
            <div class="group-content" *ngIf="group.expanded">
              <div class="permissions-list">
                <div
                  *ngFor="let permission of group.permissions; trackBy: trackByPermissionId"
                  class="permission-item"
                  [class.granted]="permission.granted"
                  [class.denied]="!permission.granted"
                  [class.inherited]="permission.inherited"
                  [class.modified]="permission.modified">

                  <div class="permission-toggle">
                    <label class="permission-switch">
                      <input
                        type="checkbox"
                        [checked]="permission.granted"
                        (change)="togglePermission(permission)"
                        [disabled]="permission.inherited">
                      <span class="switch-slider"></span>
                    </label>
                  </div>

                  <div class="permission-info">
                    <div class="permission-main">
                      <span class="permission-name">{{ permission.displayName }}</span>
                      <span class="permission-code">{{ permission.name }}</span>
                    </div>
                    <div class="permission-details">
                      <span class="permission-description" *ngIf="permission.description">
                        {{ permission.description }}
                      </span>
                      <div class="permission-metadata">
                        <span class="metadata-item">{{ permission.resource }}.{{ permission.action }}</span>
                        <span class="metadata-item category">{{ permission.category }}</span>
                      </div>
                    </div>
                  </div>

                  <div class="permission-status">
                    <div class="status-indicators">
                      <span class="status-badge granted" *ngIf="permission.granted && !permission.inherited">
                        <lucide-angular [img]="CheckIcon" size="12"></lucide-angular>
                        Verilen
                      </span>
                      <span class="status-badge denied" *ngIf="!permission.granted && !permission.inherited">
                        <lucide-angular [img]="XIcon" size="12"></lucide-angular>
                        Reddedilen
                      </span>
                      <span class="status-badge inherited" *ngIf="permission.inherited">
                        <lucide-angular [img]="InfoIcon" size="12"></lucide-angular>
                        Miras ({{ permission.inheritedFrom }})
                      </span>
                      <span class="status-badge modified" *ngIf="permission.modified">
                        <lucide-angular [img]="EditIcon" size="12"></lucide-angular>
                        Değiştirildi
                      </span>
                    </div>

                    <div class="permission-actions">
                      <button
                        type="button"
                        class="btn btn-sm btn-outline-info"
                        (click)="viewPermissionDetails(permission)"
                        title="Detayları Görüntüle">
                        <lucide-angular [img]="EyeIcon" size="12"></lucide-angular>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Permission List - Flat View -->
      <div class="permissions-flat" *ngIf="viewMode() === 'flat'">
        <div class="table-container">
          <table class="table table-hover permissions-table">
            <thead>
              <tr>
                <th class="toggle-column">İzin</th>
                <th>Adı</th>
                <th>Servis</th>
                <th>Kategori</th>
                <th>Durum</th>
                <th class="actions-column">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              <tr
                *ngFor="let permission of filteredFlatPermissions(); trackBy: trackByPermissionId"
                [class.granted]="permission.granted"
                [class.denied]="!permission.granted"
                [class.inherited]="permission.inherited"
                [class.modified]="permission.modified">

                <td>
                  <label class="permission-switch">
                    <input
                      type="checkbox"
                      [checked]="permission.granted"
                      (change)="togglePermission(permission)"
                      [disabled]="permission.inherited">
                    <span class="switch-slider"></span>
                  </label>
                </td>

                <td>
                  <div class="permission-cell">
                    <span class="permission-name">{{ permission.displayName }}</span>
                    <span class="permission-code">{{ permission.name }}</span>
                    <span class="permission-description" *ngIf="permission.description">
                      {{ permission.description }}
                    </span>
                  </div>
                </td>

                <td>
                  <span class="service-name">{{ permission.service }}</span>
                </td>

                <td>
                  <span class="category-name">{{ permission.category }}</span>
                </td>

                <td>
                  <div class="status-column">
                    <span class="status-badge granted" *ngIf="permission.granted && !permission.inherited">
                      <lucide-angular [img]="CheckIcon" size="12"></lucide-angular>
                      Verilen
                    </span>
                    <span class="status-badge denied" *ngIf="!permission.granted && !permission.inherited">
                      <lucide-angular [img]="XIcon" size="12"></lucide-angular>
                      Reddedilen
                    </span>
                    <span class="status-badge inherited" *ngIf="permission.inherited">
                      <lucide-angular [img]="InfoIcon" size="12"></lucide-angular>
                      Miras
                    </span>
                    <span class="status-badge modified" *ngIf="permission.modified">
                      <lucide-angular [img]="EditIcon" size="12"></lucide-angular>
                      Değiştirildi
                    </span>
                  </div>
                </td>

                <td>
                  <div class="action-buttons">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-info"
                      (click)="viewPermissionDetails(permission)"
                      title="Detayları Görüntüle">
                      <lucide-angular [img]="EyeIcon" size="14"></lucide-angular>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="allPermissions().length === 0 && !loading()">
        <div class="empty-state-content">
          <lucide-angular [img]="ShieldIcon" size="48" class="empty-icon"></lucide-angular>
          <h3 class="empty-title">İzin bulunamadı</h3>
          <p class="empty-description">
            {{ hasActiveFilters() ? 'Filtrelere uygun izin bulunamadı.' : 'Bu grupta henüz izin tanımlanmamış.' }}
          </p>
          <button
            *ngIf="!hasActiveFilters()"
            type="button"
            class="btn btn-primary"
            (click)="openPermissionWizard()">
            <lucide-angular [img]="PlusIcon" size="16"></lucide-angular>
            İlk İzni Ekle
          </button>
          <button
            *ngIf="hasActiveFilters()"
            type="button"
            class="btn btn-outline-secondary"
            (click)="clearAllFilters()">
            Filtreleri Temizle
          </button>
        </div>
      </div>

      <!-- Loading Overlay -->
      <div class="loading-overlay" *ngIf="loading()">
        <div class="loading-spinner">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Yükleniyor...</span>
          </div>
          <p class="loading-text">İzinler yükleniyor...</p>
        </div>
      </div>
    </div>

    <!-- Permission Details Modal -->
    <div class="modal fade" id="permissionDetailsModal" tabindex="-1">
      <div class="modal-dialog modal-lg">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">İzin Detayları</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>

          <div class="modal-body" *ngIf="selectedPermissionForDetails()">
            <div class="permission-details-content">
              <div class="detail-section">
                <h6 class="detail-title">Temel Bilgiler</h6>
                <div class="detail-grid">
                  <div class="detail-item">
                    <span class="detail-label">Görünen Ad:</span>
                    <span class="detail-value">{{ selectedPermissionForDetails()!.displayName }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Sistem Adı:</span>
                    <span class="detail-value">{{ selectedPermissionForDetails()!.name }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Servis:</span>
                    <span class="detail-value">{{ selectedPermissionForDetails()!.service }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Kategori:</span>
                    <span class="detail-value">{{ selectedPermissionForDetails()!.category }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Kaynak:</span>
                    <span class="detail-value">{{ selectedPermissionForDetails()!.resource }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="detail-label">Eylem:</span>
                    <span class="detail-value">{{ selectedPermissionForDetails()!.action }}</span>
                  </div>
                </div>
              </div>

              <div class="detail-section" *ngIf="selectedPermissionForDetails()!.description">
                <h6 class="detail-title">Açıklama</h6>
                <p class="detail-description">{{ selectedPermissionForDetails()!.description }}</p>
              </div>

              <div class="detail-section">
                <h6 class="detail-title">Durum Bilgileri</h6>
                <div class="status-info">
                  <div class="status-item">
                    <span class="status-label">Mevcut Durum:</span>
                    <span
                      class="status-badge"
                      [class.granted]="selectedPermissionForDetails()!.granted"
                      [class.denied]="!selectedPermissionForDetails()!.granted">
                      {{ selectedPermissionForDetails()!.granted ? 'Verilen' : 'Reddedilen' }}
                    </span>
                  </div>
                  <div class="status-item" *ngIf="selectedPermissionForDetails()!.inherited">
                    <span class="status-label">Miras Durumu:</span>
                    <span class="status-badge inherited">
                      {{ selectedPermissionForDetails()!.inheritedFrom }} tarafından miras alındı
                    </span>
                  </div>
                  <div class="status-item" *ngIf="selectedPermissionForDetails()!.modified">
                    <span class="status-label">Değişiklik:</span>
                    <span class="status-badge modified">
                      Orijinal: {{ selectedPermissionForDetails()!.originalState ? 'Verilen' : 'Reddedilen' }}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Kapat
            </button>
            <button
              type="button"
              class="btn btn-primary"
              (click)="editPermissionFromModal()"
              *ngIf="!selectedPermissionForDetails()?.inherited">
              <lucide-angular [img]="Edit2Icon" size="16"></lucide-angular>
              Düzenle
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .permission-overview-container {
      padding: 1.5rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    .page-header {
      margin-bottom: 2rem;
    }

    .header-content {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1.5rem;
    }

    .breadcrumb-nav {
      font-size: 0.9rem;
      color: var(--bs-gray-600);
      margin-bottom: 0.5rem;
    }

    .breadcrumb-link {
      color: var(--bs-primary);
      text-decoration: none;
    }

    .breadcrumb-link:hover {
      text-decoration: underline;
    }

    .breadcrumb-separator {
      margin: 0 0.5rem;
    }

    .breadcrumb-current {
      color: var(--bs-gray-700);
    }

    .title-section {
      flex: 1;
    }

    .title-with-icon {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.5rem;
    }

    .title-icon {
      color: var(--bs-primary);
    }

    .page-title {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .page-subtitle {
      margin: 0;
      color: var(--bs-gray-600);
      font-size: 0.95rem;
    }

    .header-actions {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    .group-permission-summary {
      background: white;
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      padding: 1.5rem;
      margin-bottom: 1.5rem;
    }

    .group-info-section {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1.5rem;
    }

    .group-basic-info {
      flex: 1;
    }

    .group-name {
      margin: 0 0 0.5rem 0;
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--bs-gray-900);
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .system-badge {
      background: var(--bs-warning-bg);
      color: var(--bs-warning);
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .group-description {
      color: var(--bs-gray-600);
      margin: 0;
    }

    .permission-stats {
      display: flex;
      gap: 1rem;
    }

    .stat-card {
      background: var(--bs-gray-50);
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      padding: 1rem;
      min-width: 120px;
      text-align: center;
      position: relative;
    }

    .stat-card.granted {
      background: var(--bs-success-bg);
      border-color: var(--bs-success-border);
    }

    .stat-card.denied {
      background: var(--bs-danger-bg);
      border-color: var(--bs-danger-border);
    }

    .stat-card.inherited {
      background: var(--bs-info-bg);
      border-color: var(--bs-info-border);
    }

    .stat-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--bs-gray-900);
      display: block;
    }

    .stat-label {
      font-size: 0.85rem;
      color: var(--bs-gray-600);
      display: block;
      margin-top: 0.25rem;
    }

    .stat-icon {
      position: absolute;
      top: 0.75rem;
      right: 0.75rem;
      color: var(--bs-gray-400);
    }

    .service-metrics {
      border-top: 1px solid var(--bs-gray-200);
      padding-top: 1.5rem;
    }

    .metrics-title {
      font-weight: 600;
      color: var(--bs-gray-700);
      margin-bottom: 1rem;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1rem;
    }

    .metric-card {
      background: var(--bs-gray-50);
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.375rem;
      padding: 1rem;
    }

    .metric-card.risk-low { border-left: 4px solid var(--bs-success); }
    .metric-card.risk-medium { border-left: 4px solid var(--bs-warning); }
    .metric-card.risk-high { border-left: 4px solid var(--bs-orange); }
    .metric-card.risk-critical { border-left: 4px solid var(--bs-danger); }

    .metric-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.75rem;
    }

    .service-name {
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .risk-badge {
      padding: 0.125rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .risk-badge.risk-low {
      background: var(--bs-success-bg);
      color: var(--bs-success);
    }

    .risk-badge.risk-medium {
      background: var(--bs-warning-bg);
      color: var(--bs-warning);
    }

    .risk-badge.risk-high {
      background: var(--bs-orange-bg);
      color: var(--bs-orange);
    }

    .risk-badge.risk-critical {
      background: var(--bs-danger-bg);
      color: var(--bs-danger);
    }

    .metric-body {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .coverage-bar {
      background: var(--bs-gray-200);
      border-radius: 0.25rem;
      height: 8px;
      overflow: hidden;
    }

    .coverage-fill {
      background: var(--bs-primary);
      height: 100%;
      transition: width 0.3s ease;
    }

    .metric-stats {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.85rem;
    }

    .coverage-text {
      color: var(--bs-gray-600);
    }

    .permission-count {
      color: var(--bs-gray-900);
      font-weight: 500;
    }

    .controls-section {
      background: white;
      border-radius: 0.5rem;
      padding: 1.25rem;
      margin-bottom: 1.5rem;
      border: 1px solid var(--bs-gray-200);
    }

    .filters-row {
      display: flex;
      gap: 1rem;
      align-items: center;
      flex-wrap: wrap;
      margin-bottom: 1rem;
    }

    .search-input-group {
      position: relative;
      flex: 1;
      min-width: 250px;
    }

    .search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--bs-gray-500);
      z-index: 2;
    }

    .search-input {
      padding-left: 2.5rem;
      padding-right: 2.5rem;
    }

    .btn-clear-search {
      position: absolute;
      right: 0.5rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      color: var(--bs-gray-500);
      padding: 0.25rem;
      border-radius: 0.25rem;
    }

    .btn-clear-search:hover {
      background: var(--bs-gray-100);
      color: var(--bs-gray-700);
    }

    .filter-group {
      min-width: 140px;
    }

    .view-toggle .btn-group {
      box-shadow: none;
    }

    .view-toggle .btn {
      border-color: var(--bs-gray-300);
    }

    .view-toggle .btn.active {
      background: var(--bs-primary);
      border-color: var(--bs-primary);
      color: white;
    }

    .quick-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: var(--bs-warning-bg);
      border: 1px solid var(--bs-warning-border);
      border-radius: 0.375rem;
      padding: 0.75rem 1rem;
    }

    .modification-info {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .warning-icon {
      color: var(--bs-warning);
    }

    .modification-text {
      color: var(--bs-warning);
      font-weight: 500;
    }

    .action-buttons {
      display: flex;
      gap: 0.5rem;
    }

    .permissions-content {
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
      overflow: hidden;
    }

    .permission-groups {
      padding: 1.5rem;
    }

    .permission-group {
      border: 1px solid var(--bs-gray-200);
      border-radius: 0.5rem;
      margin-bottom: 1rem;
      overflow: hidden;
    }

    .permission-group:last-child {
      margin-bottom: 0;
    }

    .group-header {
      background: var(--bs-gray-50);
      padding: 1rem 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 1rem;
      transition: background-color 0.2s ease;
    }

    .group-header:hover {
      background: var(--bs-gray-100);
    }

    .group-title-section {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex: 1;
    }

    .expand-button {
      background: none;
      border: none;
      padding: 0.25rem;
      color: var(--bs-gray-600);
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .group-info {
      flex: 1;
    }

    .group-title {
      margin: 0 0 0.25rem 0;
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .group-description {
      margin: 0;
      font-size: 0.9rem;
      color: var(--bs-gray-600);
    }

    .group-summary {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .summary-stats {
      display: flex;
      gap: 1rem;
    }

    .stat-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.125rem;
    }

    .stat-item .stat-value {
      font-weight: 700;
      color: var(--bs-gray-900);
    }

    .stat-item .stat-label {
      font-size: 0.75rem;
      color: var(--bs-gray-600);
    }

    .group-progress {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      min-width: 100px;
    }

    .progress-bar {
      background: var(--bs-gray-200);
      border-radius: 0.25rem;
      height: 6px;
      flex: 1;
      overflow: hidden;
    }

    .progress-fill {
      background: var(--bs-success);
      height: 100%;
      transition: width 0.3s ease;
    }

    .progress-text {
      font-size: 0.85rem;
      color: var(--bs-gray-600);
      font-weight: 500;
      min-width: 35px;
    }

    .group-actions {
      display: flex;
      align-items: center;
    }

    .group-content {
      padding: 0;
    }

    .permissions-list {
      max-height: 400px;
      overflow-y: auto;
    }

    .permission-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
      transition: background-color 0.2s ease;
    }

    .permission-item:hover {
      background: var(--bs-gray-50);
    }

    .permission-item:last-child {
      border-bottom: none;
    }

    .permission-item.granted {
      background: var(--bs-success-bg);
    }

    .permission-item.denied {
      background: var(--bs-danger-bg);
    }

    .permission-item.inherited {
      background: var(--bs-info-bg);
    }

    .permission-item.modified {
      background: var(--bs-warning-bg);
    }

    .permission-toggle {
      flex-shrink: 0;
    }

    .permission-switch {
      position: relative;
      display: inline-block;
      width: 44px;
      height: 24px;
    }

    .permission-switch input {
      opacity: 0;
      width: 0;
      height: 0;
    }

    .switch-slider {
      position: absolute;
      cursor: pointer;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: var(--bs-gray-300);
      transition: 0.4s;
      border-radius: 24px;
    }

    .switch-slider:before {
      position: absolute;
      content: "";
      height: 18px;
      width: 18px;
      left: 3px;
      bottom: 3px;
      background-color: white;
      transition: 0.4s;
      border-radius: 50%;
    }

    input:checked + .switch-slider {
      background-color: var(--bs-success);
    }

    input:checked + .switch-slider:before {
      transform: translateX(20px);
    }

    input:disabled + .switch-slider {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .permission-info {
      flex: 1;
      min-width: 0;
    }

    .permission-main {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.25rem;
    }

    .permission-name {
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .permission-code {
      font-family: var(--bs-font-monospace);
      font-size: 0.85rem;
      color: var(--bs-gray-600);
      background: var(--bs-gray-100);
      padding: 0.125rem 0.375rem;
      border-radius: 0.25rem;
    }

    .permission-details {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .permission-description {
      font-size: 0.9rem;
      color: var(--bs-gray-600);
      line-height: 1.4;
    }

    .permission-metadata {
      display: flex;
      gap: 0.75rem;
    }

    .metadata-item {
      font-size: 0.8rem;
      color: var(--bs-gray-500);
    }

    .metadata-item.category {
      background: var(--bs-primary-bg);
      color: var(--bs-primary);
      padding: 0.125rem 0.375rem;
      border-radius: 0.25rem;
    }

    .permission-status {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 0.5rem;
    }

    .status-indicators {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      align-items: flex-end;
    }

    .status-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .status-badge.granted {
      background: var(--bs-success-bg);
      color: var(--bs-success);
    }

    .status-badge.denied {
      background: var(--bs-danger-bg);
      color: var(--bs-danger);
    }

    .status-badge.inherited {
      background: var(--bs-info-bg);
      color: var(--bs-info);
    }

    .status-badge.modified {
      background: var(--bs-warning-bg);
      color: var(--bs-warning);
    }

    .permission-actions {
      display: flex;
      gap: 0.25rem;
    }

    .permissions-flat {
      overflow: hidden;
    }

    .table-container {
      overflow-x: auto;
    }

    .permissions-table {
      margin: 0;
    }

    .permissions-table th {
      background: var(--bs-gray-50);
      border-bottom: 2px solid var(--bs-gray-200);
      font-weight: 600;
      color: var(--bs-gray-700);
      padding: 1rem 0.75rem;
    }

    .permissions-table td {
      padding: 1rem 0.75rem;
      vertical-align: middle;
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .permissions-table tbody tr:hover {
      background: var(--bs-gray-50);
    }

    .permissions-table tbody tr.granted {
      background: var(--bs-success-bg);
    }

    .permissions-table tbody tr.denied {
      background: var(--bs-danger-bg);
    }

    .permissions-table tbody tr.inherited {
      background: var(--bs-info-bg);
    }

    .permissions-table tbody tr.modified {
      background: var(--bs-warning-bg);
    }

    .toggle-column {
      width: 80px;
    }

    .actions-column {
      width: 100px;
    }

    .permission-cell {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .service-name,
    .category-name {
      font-weight: 500;
      color: var(--bs-gray-700);
    }

    .status-column {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .action-buttons {
      display: flex;
      gap: 0.25rem;
    }

    .action-buttons .btn {
      padding: 0.375rem 0.5rem;
    }

    .empty-state {
      text-align: center;
      padding: 4rem 2rem;
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
    }

    .empty-state-content {
      max-width: 400px;
      margin: 0 auto;
    }

    .empty-icon {
      color: var(--bs-gray-400);
      margin-bottom: 1.5rem;
    }

    .empty-title {
      margin-bottom: 1rem;
      color: var(--bs-gray-700);
    }

    .empty-description {
      color: var(--bs-gray-600);
      margin-bottom: 2rem;
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

    .modal-lg {
      max-width: 800px;
    }

    .permission-details-content {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .detail-section {
      border-bottom: 1px solid var(--bs-gray-200);
      padding-bottom: 1rem;
    }

    .detail-section:last-child {
      border-bottom: none;
      padding-bottom: 0;
    }

    .detail-title {
      font-weight: 600;
      color: var(--bs-gray-700);
      margin-bottom: 1rem;
    }

    .detail-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 0.75rem;
    }

    .detail-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .detail-label {
      font-weight: 500;
      color: var(--bs-gray-600);
    }

    .detail-value {
      color: var(--bs-gray-900);
      font-family: var(--bs-font-monospace);
      font-size: 0.9rem;
    }

    .detail-description {
      color: var(--bs-gray-700);
      line-height: 1.5;
      margin: 0;
    }

    .status-info {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .status-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .status-label {
      font-weight: 500;
      color: var(--bs-gray-600);
    }

    @media (max-width: 768px) {
      .permission-overview-container {
        padding: 1rem;
      }

      .header-content {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .header-actions {
        justify-content: stretch;
      }

      .group-info-section {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .permission-stats {
        justify-content: space-around;
      }

      .filters-row {
        flex-direction: column;
        align-items: stretch;
      }

      .search-input-group {
        min-width: auto;
      }

      .metrics-grid {
        grid-template-columns: 1fr;
      }

      .permission-item {
        flex-direction: column;
        align-items: stretch;
        gap: 0.75rem;
      }

      .permission-status {
        align-items: flex-start;
      }

      .status-indicators {
        flex-direction: row;
        flex-wrap: wrap;
        align-items: flex-start;
      }

      .group-header {
        flex-direction: column;
        gap: 0.75rem;
        align-items: stretch;
      }

      .group-summary {
        justify-content: space-between;
      }

      .detail-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class GroupPermissionOverviewComponent implements OnInit {
  // Input for group ID (can be set via route parameter)
  @Input() groupId?: string;

  // Dependency Injection
  private readonly groupService = inject(GroupService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly errorHandler = inject(ErrorHandlerService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  // Icons
  readonly ShieldIcon = Shield;
  readonly LockIcon = Lock;
  readonly UnlockIcon = Unlock;
  readonly EyeIcon = Eye;
  readonly EyeOffIcon = EyeOff;
  readonly SearchIcon = Search;
  readonly FilterIcon = Filter;
  readonly PlusIcon = Plus;
  readonly MinusIcon = Minus;
  readonly CheckIcon = Check;
  readonly XIcon = X;
  readonly AlertTriangleIcon = AlertTriangle;
  readonly InfoIcon = Info;
  readonly SettingsIcon = Settings;
  readonly UsersIcon = Users;
  readonly ServerIcon = Server;
  readonly DatabaseIcon = Database;
  readonly GlobeIcon = Globe;
  readonly FileTextIcon = FileText;
  readonly MoreVerticalIcon = MoreVertical;
  readonly ChevronDownIcon = ChevronDown;
  readonly ChevronRightIcon = ChevronRight;
  readonly GridIcon = Grid;
  readonly ListIcon = List;
  readonly DownloadIcon = Download;
  readonly UploadIcon = Upload;
  readonly RefreshCwIcon = RefreshCw;
  readonly CopyIcon = Copy;
  readonly Trash2Icon = Trash2;
  readonly Edit2Icon = Edit2;
  readonly SaveIcon = Save;
  readonly RotateCcwIcon = RotateCcw;
  readonly EditIcon = Edit2;

  // State Signals
  group = signal<GroupDto | null>(null);
  allPermissions = signal<PermissionWithStatus[]>([]);
  loading = signal(false);
  saving = signal(false);
  selectedPermissionForDetails = signal<PermissionWithStatus | null>(null);

  // UI State
  viewMode = signal<'grouped' | 'flat'>('grouped');
  changeLogs = signal<PermissionChangeLog[]>([]);

  // Filters
  filters = signal<PermissionFilter>({
    search: '',
    service: '',
    status: 'all',
    category: ''
  });

  // Computed Values
  availableServices = computed(() => {
    const services = new Set(this.allPermissions().map(p => p.service));
    return Array.from(services).sort();
  });

  availableCategories = computed(() => {
    const categories = new Set(this.allPermissions().map(p => p.category));
    return Array.from(categories).sort();
  });

  filteredPermissionGroups = computed(() => {
    const permissions = this.getFilteredPermissions();
    const groups = new Map<string, PermissionGroup>();

    permissions.forEach(permission => {
      if (!groups.has(permission.service)) {
        groups.set(permission.service, {
          service: permission.service,
          displayName: this.getServiceDisplayName(permission.service),
          description: this.getServiceDescription(permission.service),
          permissions: [],
          expanded: true,
          summary: {
            total: 0,
            granted: 0,
            denied: 0,
            inherited: 0
          }
        });
      }

      const group = groups.get(permission.service)!;
      group.permissions.push(permission);
      group.summary.total++;

      if (permission.granted) {
        group.summary.granted++;
      } else {
        group.summary.denied++;
      }

      if (permission.inherited) {
        group.summary.inherited++;
      }
    });

    return Array.from(groups.values());
  });

  filteredFlatPermissions = computed(() => {
    return this.getFilteredPermissions();
  });

  permissionSummary = computed(() => {
    const permissions = this.allPermissions();
    return {
      total: permissions.length,
      granted: permissions.filter(p => p.granted).length,
      denied: permissions.filter(p => !p.granted).length,
      inherited: permissions.filter(p => p.inherited).length
    };
  });

  serviceMetrics = computed(() => {
    const groups = this.filteredPermissionGroups();
    return groups.map(group => {
      const coverage = group.summary.total > 0 ?
        (group.summary.granted / group.summary.total) * 100 : 0;

      let riskLevel: 'low' | 'medium' | 'high' | 'critical' = 'low';
      if (coverage < 25) riskLevel = 'critical';
      else if (coverage < 50) riskLevel = 'high';
      else if (coverage < 75) riskLevel = 'medium';

      return {
        serviceName: group.displayName,
        totalPermissions: group.summary.total,
        grantedPermissions: group.summary.granted,
        riskLevel,
        coverage: Math.round(coverage)
      } as ServiceMetrics;
    });
  });

  hasModifications = computed(() => {
    return this.allPermissions().some(p => p.modified);
  });

  modificationCount = computed(() => {
    return this.allPermissions().filter(p => p.modified).length;
  });

  // Expose Math for template
  Math = Math;

  ngOnInit(): void {
    // Get group ID from route parameter if not provided as input
    if (!this.groupId) {
      this.route.params.subscribe(params => {
        this.groupId = params['id'];
        if (this.groupId) {
          this.loadGroupAndPermissions();
        }
      });
    } else {
      this.loadGroupAndPermissions();
    }
  }

  // Data Loading Methods
  async loadGroupAndPermissions(): Promise<void> {
    if (!this.groupId) return;

    await Promise.all([
      this.loadGroup(),
      this.loadPermissions()
    ]);
  }

  async loadGroup(): Promise<void> {
    if (!this.groupId) return;

    try {
      const response = await this.groupService.getGroup(this.groupId).toPromise();
      if (response) {
        this.group.set(response);
      }
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  async loadPermissions(): Promise<void> {
    if (!this.groupId) return;

    try {
      this.loading.set(true);
      const response = await this.groupService.getGroupPermissions(this.groupId).toPromise();
      if (response) {
        const permissionsWithStatus: PermissionWithStatus[] = response.map(permission => ({
          ...permission,
          granted: true, // This would come from the actual permission assignment
          inherited: false, // This would be calculated based on inheritance rules
          modified: false,
          originalState: true,
          createdAt: new Date(),
          roleCount: 0,
          userCount: 0
        }));
        this.allPermissions.set(permissionsWithStatus);
      }
    } catch (error) {
      this.errorHandler.handleError(error);
    } finally {
      this.loading.set(false);
    }
  }

  async refreshData(): Promise<void> {
    await this.loadGroupAndPermissions();
  }

  // Filter Methods
  private getFilteredPermissions(): PermissionWithStatus[] {
    const permissions = this.allPermissions();
    const f = this.filters();

    return permissions.filter(permission => {
      // Search filter
      const searchTerm = f.search.toLowerCase();
      const matchesSearch = !searchTerm ||
        permission.displayName.toLowerCase().includes(searchTerm) ||
        permission.name.toLowerCase().includes(searchTerm) ||
        permission.description?.toLowerCase().includes(searchTerm) ||
        permission.service.toLowerCase().includes(searchTerm);

      // Service filter
      const matchesService = !f.service || permission.service === f.service;

      // Status filter
      const matchesStatus = f.status === 'all' ||
        (f.status === 'granted' && permission.granted && !permission.inherited) ||
        (f.status === 'denied' && !permission.granted && !permission.inherited) ||
        (f.status === 'inherited' && permission.inherited) ||
        (f.status === 'modified' && permission.modified);

      // Category filter
      const matchesCategory = !f.category || permission.category === f.category;

      return matchesSearch && matchesService && matchesStatus && matchesCategory;
    });
  }

  onSearchChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.filters.update(f => ({ ...f, search: target.value }));
  }

  clearSearch(): void {
    this.filters.update(f => ({ ...f, search: '' }));
  }

  applyFilters(): void {
    // Filters are applied automatically through computed property
  }

  clearAllFilters(): void {
    this.filters.set({
      search: '',
      service: '',
      status: 'all',
      category: ''
    });
  }

  hasActiveFilters(): boolean {
    const f = this.filters();
    return !!(f.search || f.service || f.status !== 'all' || f.category);
  }

  // View Mode Methods
  setViewMode(mode: 'grouped' | 'flat'): void {
    this.viewMode.set(mode);
  }

  // Permission Group Methods
  toggleGroupExpansion(group: PermissionGroup): void {
    group.expanded = !group.expanded;
  }

  async grantAllInGroup(group: PermissionGroup): Promise<void> {
    const confirmed = await this.confirmationService.confirm({
      title: 'Tüm İzinleri Ver',
      message: `${group.displayName} servisindeki tüm izinleri vermek istediğinizden emin misiniz?`,
      confirmText: 'Ver',
      type: 'success'
    });

    if (confirmed) {
      group.permissions.forEach(permission => {
        if (!permission.inherited) {
          this.changePermissionStatus(permission, true);
        }
      });
    }
  }

  async revokeAllInGroup(group: PermissionGroup): Promise<void> {
    const confirmed = await this.confirmationService.confirm({
      title: 'Tüm İzinleri Reddet',
      message: `${group.displayName} servisindeki tüm izinleri reddetmek istediğinizden emin misiniz?`,
      confirmText: 'Reddet',
      type: 'danger'
    });

    if (confirmed) {
      group.permissions.forEach(permission => {
        if (!permission.inherited) {
          this.changePermissionStatus(permission, false);
        }
      });
    }
  }

  copyGroupPermissions(group: PermissionGroup): void {
    const permissionIds = group.permissions
      .filter(p => p.granted)
      .map(p => p.id);

    // Copy to clipboard or show modal for target selection
    navigator.clipboard.writeText(JSON.stringify(permissionIds));
  }

  // Permission Management Methods
  togglePermission(permission: PermissionWithStatus): void {
    if (permission.inherited) return;

    this.changePermissionStatus(permission, !permission.granted);
  }

  private changePermissionStatus(permission: PermissionWithStatus, granted: boolean): void {
    if (permission.inherited) return;

    const wasModified = permission.modified;
    const originalState = wasModified ? permission.originalState : permission.granted;

    permission.granted = granted;
    permission.modified = granted !== originalState;

    if (!wasModified && permission.modified) {
      permission.originalState = !granted;
    }

    // Add to change log
    this.changeLogs.update(logs => [
      ...logs,
      {
        permissionId: permission.id,
        permissionName: permission.displayName,
        action: granted ? 'grant' : 'revoke',
        previousState: !granted,
        newState: granted
      }
    ]);

    // Update the permissions array to trigger reactivity
    this.allPermissions.update(permissions => [...permissions]);
  }

  // Change Management Methods
  async saveChanges(): Promise<void> {
    if (!this.groupId) return;

    const modifiedPermissions = this.allPermissions().filter(p => p.modified);
    if (modifiedPermissions.length === 0) return;

    try {
      this.saving.set(true);

      const assignment: GroupPermissionAssignment = {
        groupId: this.groupId,
        permissionIds: modifiedPermissions.filter(p => p.granted).map(p => p.id),
        operation: 'assign'
      };

      await this.groupService.assignPermissionsToGroup(assignment).toPromise();
      // Reset modification flags
      this.allPermissions.update(permissions =>
        permissions.map(p => ({ ...p, modified: false, originalState: p.granted }))
      );
      this.changeLogs.set([]);
      await this.loadPermissions();
    } catch (error) {
      this.errorHandler.handleError(error);
    } finally {
      this.saving.set(false);
    }
  }

  resetChanges(): void {
    this.allPermissions.update(permissions =>
      permissions.map(p => ({
        ...p,
        granted: p.modified ? p.originalState : p.granted,
        modified: false
      }))
    );
    this.changeLogs.set([]);
  }

  // Navigation Methods
  viewPermissionMatrix(): void {
    this.router.navigate(['/user-management/permissions/matrix'], {
      queryParams: { groupId: this.groupId }
    });
  }

  openPermissionWizard(): void {
    // Open permission wizard modal or navigate to wizard page
  }

  viewPermissionDetails(permission: PermissionWithStatus): void {
    this.selectedPermissionForDetails.set(permission);
    // Open modal programmatically
  }

  editPermissionFromModal(): void {
    const permission = this.selectedPermissionForDetails();
    if (permission) {
      // Close modal and open edit interface
    }
  }

  // Export Methods
  async exportPermissions(format: 'excel' | 'csv' | 'pdf'): Promise<void> {
    try {
      const permissions = this.getFilteredPermissions();
      console.log(`Exporting ${permissions.length} permissions in ${format} format`);
      // TODO: Implementation for export
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  // Utility Methods
  trackByGroupService(_index: number, group: PermissionGroup): string {
    return group.service;
  }

  trackByPermissionId(_index: number, permission: PermissionWithStatus): string {
    return permission.id;
  }

  getServiceDisplayName(service: string): string {
    const serviceNames: Record<string, string> = {
      'identity': 'Kimlik Yönetimi',
      'user': 'Kullanıcı Yönetimi',
      'gateway': 'API Gateway',
      'notification': 'Bildirim Servisi',
      'audit': 'Denetim Servisi',
      'file': 'Dosya Yönetimi'
    };
    return serviceNames[service] || service;
  }

  getServiceDescription(service: string): string {
    const descriptions: Record<string, string> = {
      'identity': 'Kimlik doğrulama ve yetkilendirme işlemleri',
      'user': 'Kullanıcı hesapları ve profil yönetimi',
      'gateway': 'API erişim kontrolü ve yönlendirme',
      'notification': 'Bildirim gönderme ve yönetimi',
      'audit': 'Sistem aktivitelerinin izlenmesi',
      'file': 'Dosya yükleme ve saklama işlemleri'
    };
    return descriptions[service];
  }

  getRiskLevelText(riskLevel: string): string {
    const texts: Record<string, string> = {
      'low': 'Düşük',
      'medium': 'Orta',
      'high': 'Yüksek',
      'critical': 'Kritik'
    };
    return texts[riskLevel] || riskLevel;
  }
}