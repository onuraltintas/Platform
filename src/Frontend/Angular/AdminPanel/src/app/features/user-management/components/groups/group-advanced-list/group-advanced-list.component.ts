import { Component, OnInit, OnDestroy, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { RouterModule, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  Users,
  Settings,
  Plus,
  Search,
  Filter,
  Download,
  Upload,
  MoreVertical,
  Edit2,
  Trash2,
  UserPlus,
  Shield,
  Copy,
  FileText,
  BarChart3,
  Grid,
  List,
  RefreshCw,
  ArrowUpDown,
  ChevronDown,
  X,
  Check,
  AlertTriangle,
  LucideAngularModule
} from 'lucide-angular';

import { GroupService } from '../../../services/group.service';
import { ConfirmationService } from '../../../../../shared/services/confirmation.service';
import { ErrorHandlerService } from '../../../../../core/services/error-handler.service';

import {
  GroupDto,
  CreateGroupRequest,
  UpdateGroupRequest,
  GetGroupsRequest,
  GroupStatistics
} from '../../../models/group.models';

interface GroupFilter {
  search: string;
  includeSystemGroups: boolean;
  hasMembers: boolean | null;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

interface BulkOperation {
  type: 'delete' | 'export' | 'addMembers' | 'removeMembers' | 'assignPermissions';
  label: string;
  icon: any;
  confirmMessage: string;
  requiresInput?: boolean;
}

interface StatCard {
  title: string;
  value: string;
  icon: any;
  color: string;
  trend?: {
    value: number;
    isPositive: boolean;
  };
}

@Component({
  selector: 'app-group-advanced-list',
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
    <div class="group-management-container">
      <!-- Header Section -->
      <div class="page-header">
        <div class="header-content">
          <div class="title-section">
            <div class="title-with-icon">
              <lucide-angular [img]="UsersIcon" size="24" class="title-icon"></lucide-angular>
              <h1 class="page-title">Grup Yönetimi</h1>
            </div>
            <p class="page-subtitle">Sistem gruplarını ve üye atamalarını yönetin</p>
          </div>

          <div class="header-actions">
            <button
              type="button"
              class="btn btn-outline-primary"
              (click)="refreshData()"
              [disabled]="loading()">
              <lucide-angular [img]="RefreshCwIcon" size="16"></lucide-angular>
              Yenile
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
                <li><a class="dropdown-item" (click)="exportGroups('excel')">Excel</a></li>
                <li><a class="dropdown-item" (click)="exportGroups('csv')">CSV</a></li>
                <li><a class="dropdown-item" (click)="exportGroups('pdf')">PDF</a></li>
              </ul>
            </div>

            <button
              type="button"
              class="btn btn-primary"
              (click)="openCreateGroupModal()"
              [disabled]="loading()">
              <lucide-angular [img]="PlusIcon" size="16"></lucide-angular>
              Yeni Grup
            </button>
          </div>
        </div>

        <!-- Statistics Cards -->
        <div class="statistics-grid" *ngIf="statistics()">
          <div
            *ngFor="let card of statisticsCards()"
            class="stat-card"
            [class]="'stat-card-' + card.color">
            <div class="stat-card-content">
              <div class="stat-card-header">
                <lucide-angular [img]="card.icon" size="20" class="stat-icon"></lucide-angular>
                <span class="stat-title">{{ card.title }}</span>
              </div>
              <div class="stat-value">{{ card.value }}</div>
              <div class="stat-trend" *ngIf="card.trend">
                <span
                  class="trend-indicator"
                  [class.trend-positive]="card.trend.isPositive"
                  [class.trend-negative]="!card.trend.isPositive">
                  {{ card.trend.isPositive ? '+' : '' }}{{ card.trend.value }}%
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Filters and View Controls -->
      <div class="controls-section">
        <div class="filters-row">
          <!-- Search -->
          <div class="search-input-group">
            <lucide-angular [img]="SearchIcon" size="16" class="search-icon"></lucide-angular>
            <input
              type="text"
              class="form-control search-input"
              placeholder="Grup ara..."
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

          <!-- Advanced Filters -->
          <div class="filter-controls">
            <button
              type="button"
              class="btn btn-outline-secondary filter-toggle"
              (click)="toggleAdvancedFilters()"
              [class.active]="showAdvancedFilters()">
              <lucide-angular [img]="FilterIcon" size="16"></lucide-angular>
              Filtreler
              <lucide-angular [img]="ChevronDownIcon" size="14" class="chevron"></lucide-angular>
            </button>

            <!-- Sort Controls -->
            <div class="sort-controls">
              <select
                class="form-select sort-select"
                [(ngModel)]="filters().sortBy"
                (change)="onSortChange()">
                <option value="name">Ada göre</option>
                <option value="memberCount">Üye sayısına göre</option>
                <option value="createdAt">Oluşturma tarihine göre</option>
                <option value="updatedAt">Güncelleme tarihine göre</option>
              </select>
              <button
                type="button"
                class="btn btn-outline-secondary sort-direction"
                (click)="toggleSortDirection()"
                [title]="filters().sortDirection === 'asc' ? 'Artan sıra' : 'Azalan sıra'">
                <lucide-angular [img]="ArrowUpDownIcon" size="14"></lucide-angular>
              </button>
            </div>

            <!-- View Mode Toggle -->
            <div class="view-toggle">
              <div class="btn-group" role="group">
                <button
                  type="button"
                  class="btn btn-outline-secondary"
                  [class.active]="viewMode() === 'table'"
                  (click)="setViewMode('table')">
                  <lucide-angular [img]="ListIcon" size="16"></lucide-angular>
                </button>
                <button
                  type="button"
                  class="btn btn-outline-secondary"
                  [class.active]="viewMode() === 'grid'"
                  (click)="setViewMode('grid')">
                  <lucide-angular [img]="GridIcon" size="16"></lucide-angular>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Advanced Filters Panel -->
        <div class="advanced-filters" *ngIf="showAdvancedFilters()">
          <div class="filter-options">
            <div class="form-check">
              <input
                class="form-check-input"
                type="checkbox"
                id="includeSystemGroups"
                [(ngModel)]="filters().includeSystemGroups"
                (change)="applyFilters()">
              <label class="form-check-label" for="includeSystemGroups">
                Sistem gruplarını dahil et
              </label>
            </div>

            <div class="filter-group">
              <label class="filter-label">Üye durumu:</label>
              <select
                class="form-select filter-select"
                [(ngModel)]="filters().hasMembers"
                (change)="applyFilters()">
                <option [ngValue]="null">Tümü</option>
                <option [ngValue]="true">Üyeli gruplar</option>
                <option [ngValue]="false">Boş gruplar</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      <!-- Bulk Operations Bar -->
      <div class="bulk-operations-bar" *ngIf="selectedGroups().length > 0">
        <div class="bulk-info">
          <span class="selected-count">{{ selectedGroups().length }} grup seçildi</span>
          <button type="button" class="btn-link" (click)="clearSelection()">
            Seçimi temizle
          </button>
        </div>

        <div class="bulk-actions">
          <button
            *ngFor="let operation of bulkOperations"
            type="button"
            class="btn btn-outline-secondary btn-sm"
            (click)="executeBulkOperation(operation)"
            [disabled]="loading()">
            <lucide-angular [img]="operation.icon" size="14"></lucide-angular>
            {{ operation.label }}
          </button>
        </div>
      </div>

      <!-- Groups List - Table View -->
      <div class="groups-content" *ngIf="viewMode() === 'table'">
        <div class="table-container">
          <table class="table table-hover groups-table">
            <thead>
              <tr>
                <th class="select-column">
                  <input
                    type="checkbox"
                    class="form-check-input"
                    [checked]="isAllSelected()"
                    [indeterminate]="isPartiallySelected()"
                    (change)="toggleSelectAll()">
                </th>
                <th>Grup Adı</th>
                <th>Açıklama</th>
                <th>Üye Sayısı</th>
                <th>Tip</th>
                <th>Oluşturulma</th>
                <th class="actions-column">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let group of groups(); trackBy: trackByGroupId"
                  [class.selected]="isGroupSelected(group)">
                <td>
                  <input
                    type="checkbox"
                    class="form-check-input"
                    [checked]="isGroupSelected(group)"
                    (change)="toggleGroupSelection(group)">
                </td>
                <td>
                  <div class="group-name-cell">
                    <span class="group-name">{{ group.name }}</span>
                    <span class="system-badge" *ngIf="group.isSystemGroup">Sistem</span>
                  </div>
                </td>
                <td>
                  <span class="group-description">{{ group.description || '-' }}</span>
                </td>
                <td>
                  <span class="member-count">{{ group.memberCount }}</span>
                </td>
                <td>
                  <span
                    class="badge"
                    [class.badge-secondary]="group.isSystemGroup"
                    [class.badge-primary]="!group.isSystemGroup">
                    {{ group.isSystemGroup ? 'Sistem' : 'Özel' }}
                  </span>
                </td>
                <td>
                  <span class="creation-date">{{ formatDate(group.createdAt) }}</span>
                </td>
                <td>
                  <div class="action-buttons">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-primary"
                      (click)="viewGroupDetails(group)"
                      title="Detaylar">
                      <lucide-angular [img]="FileTextIcon" size="14"></lucide-angular>
                    </button>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-secondary"
                      (click)="manageGroupMembers(group)"
                      title="Üyeleri Yönet">
                      <lucide-angular [img]="UserPlusIcon" size="14"></lucide-angular>
                    </button>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-warning"
                      (click)="manageGroupPermissions(group)"
                      title="İzinleri Yönet">
                      <lucide-angular [img]="ShieldIcon" size="14"></lucide-angular>
                    </button>

                    <div class="dropdown">
                      <button
                        type="button"
                        class="btn btn-sm btn-outline-secondary dropdown-toggle"
                        data-bs-toggle="dropdown">
                        <lucide-angular [img]="MoreVerticalIcon" size="14"></lucide-angular>
                      </button>
                      <ul class="dropdown-menu">
                        <li>
                          <a class="dropdown-item" (click)="editGroup(group)">
                            <lucide-angular [img]="Edit2Icon" size="14"></lucide-angular>
                            Düzenle
                          </a>
                        </li>
                        <li>
                          <a class="dropdown-item" (click)="cloneGroup(group)">
                            <lucide-angular [img]="CopyIcon" size="14"></lucide-angular>
                            Kopyala
                          </a>
                        </li>
                        <li><hr class="dropdown-divider"></li>
                        <li>
                          <a
                            class="dropdown-item text-danger"
                            (click)="deleteGroup(group)"
                            [class.disabled]="group.isSystemGroup">
                            <lucide-angular [img]="Trash2Icon" size="14"></lucide-angular>
                            Sil
                          </a>
                        </li>
                      </ul>
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Groups List - Grid View -->
      <div class="groups-grid" *ngIf="viewMode() === 'grid'">
        <div class="group-card-container">
          <div
            *ngFor="let group of groups(); trackBy: trackByGroupId"
            class="group-card"
            [class.selected]="isGroupSelected(group)">

            <div class="card-header">
              <div class="card-selection">
                <input
                  type="checkbox"
                  class="form-check-input"
                  [checked]="isGroupSelected(group)"
                  (change)="toggleGroupSelection(group)">
              </div>

              <div class="card-title">
                <h5 class="group-name">{{ group.name }}</h5>
                <span class="system-badge" *ngIf="group.isSystemGroup">Sistem</span>
              </div>

              <div class="card-actions">
                <div class="dropdown">
                  <button
                    type="button"
                    class="btn btn-sm btn-ghost dropdown-toggle"
                    data-bs-toggle="dropdown">
                    <lucide-angular [img]="MoreVerticalIcon" size="16"></lucide-angular>
                  </button>
                  <ul class="dropdown-menu">
                    <li><a class="dropdown-item" (click)="editGroup(group)">Düzenle</a></li>
                    <li><a class="dropdown-item" (click)="cloneGroup(group)">Kopyala</a></li>
                    <li><a class="dropdown-item text-danger" (click)="deleteGroup(group)">Sil</a></li>
                  </ul>
                </div>
              </div>
            </div>

            <div class="card-body">
              <p class="group-description">{{ group.description || 'Açıklama bulunmamaktadır.' }}</p>

              <div class="group-stats">
                <div class="stat-item">
                  <lucide-angular [img]="UsersIcon" size="16" class="stat-icon"></lucide-angular>
                  <span class="stat-value">{{ group.memberCount }}</span>
                  <span class="stat-label">Üye</span>
                </div>

                <div class="stat-item">
                  <lucide-angular [img]="ShieldIcon" size="16" class="stat-icon"></lucide-angular>
                  <span class="stat-value">{{ group.permissions.length || 0 }}</span>
                  <span class="stat-label">İzin</span>
                </div>
              </div>

              <div class="group-meta">
                <span class="creation-date">{{ formatDate(group.createdAt) }}</span>
                <span
                  class="badge"
                  [class.badge-secondary]="group.isSystemGroup"
                  [class.badge-primary]="!group.isSystemGroup">
                  {{ group.isSystemGroup ? 'Sistem' : 'Özel' }}
                </span>
              </div>
            </div>

            <div class="card-footer">
              <button
                type="button"
                class="btn btn-sm btn-outline-primary"
                (click)="viewGroupDetails(group)">
                <lucide-angular [img]="FileTextIcon" size="14"></lucide-angular>
                Detaylar
              </button>
              <button
                type="button"
                class="btn btn-sm btn-outline-secondary"
                (click)="manageGroupMembers(group)">
                <lucide-angular [img]="UserPlusIcon" size="14"></lucide-angular>
                Üyeler
              </button>
              <button
                type="button"
                class="btn btn-sm btn-outline-warning"
                (click)="manageGroupPermissions(group)">
                <lucide-angular [img]="ShieldIcon" size="14"></lucide-angular>
                İzinler
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div class="pagination-section" *ngIf="totalPages() > 1">
        <nav aria-label="Groups pagination">
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
          <span>{{ paginationInfo() }}</span>
        </div>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="groups().length === 0 && !loading()">
        <div class="empty-state-content">
          <lucide-angular [img]="UsersIcon" size="48" class="empty-icon"></lucide-angular>
          <h3 class="empty-title">Grup bulunamadı</h3>
          <p class="empty-description">
            {{ hasActiveFilters() ? 'Filtrelere uygun grup bulunamadı.' : 'Henüz grup oluşturulmamış.' }}
          </p>
          <button
            *ngIf="!hasActiveFilters()"
            type="button"
            class="btn btn-primary"
            (click)="openCreateGroupModal()">
            <lucide-angular [img]="PlusIcon" size="16"></lucide-angular>
            İlk Grubu Oluştur
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
          <p class="loading-text">Gruplar yükleniyor...</p>
        </div>
      </div>
    </div>

    <!-- Create/Edit Group Modal -->
    <div class="modal fade" id="groupModal" tabindex="-1">
      <div class="modal-dialog modal-lg">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              {{ editingGroup() ? 'Grup Düzenle' : 'Yeni Grup Oluştur' }}
            </h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>

          <form [formGroup]="groupForm" (ngSubmit)="saveGroup()">
            <div class="modal-body">
              <div class="row">
                <div class="col-md-6">
                  <div class="mb-3">
                    <label class="form-label required">Grup Adı</label>
                    <input
                      type="text"
                      class="form-control"
                      formControlName="name"
                      placeholder="Grup adını girin">
                    <div class="invalid-feedback" *ngIf="groupForm.get('name')?.invalid && groupForm.get('name')?.touched">
                      Grup adı zorunludur
                    </div>
                  </div>
                </div>

                <div class="col-md-6">
                  <div class="mb-3">
                    <label class="form-label">Açıklama</label>
                    <textarea
                      class="form-control"
                      formControlName="description"
                      rows="3"
                      placeholder="Grup açıklaması (isteğe bağlı)"></textarea>
                  </div>
                </div>
              </div>

              <!-- Initial Members Selection -->
              <div class="mb-3" *ngIf="!editingGroup()">
                <label class="form-label">Başlangıç Üyeleri</label>
                <div class="member-selection">
                  <!-- Add member selection component here -->
                  <p class="text-muted">Grup oluşturduktan sonra üyeleri ekleyebilirsiniz.</p>
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
                [disabled]="groupForm.invalid || saving()">
                <span class="spinner-border spinner-border-sm me-2" *ngIf="saving()"></span>
                {{ editingGroup() ? 'Güncelle' : 'Oluştur' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .group-management-container {
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

    .statistics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      margin-top: 1.5rem;
    }

    .stat-card {
      background: white;
      border-radius: 0.5rem;
      padding: 1.25rem;
      border: 1px solid var(--bs-gray-200);
      transition: all 0.2s ease;
    }

    .stat-card:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      transform: translateY(-2px);
    }

    .stat-card-content {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .stat-card-header {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .stat-icon {
      color: var(--bs-primary);
    }

    .stat-title {
      font-size: 0.85rem;
      color: var(--bs-gray-600);
      font-weight: 500;
    }

    .stat-value {
      font-size: 1.75rem;
      font-weight: 700;
      color: var(--bs-gray-900);
    }

    .stat-trend {
      font-size: 0.8rem;
    }

    .trend-indicator {
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-weight: 500;
    }

    .trend-positive {
      background: var(--bs-success-bg);
      color: var(--bs-success);
    }

    .trend-negative {
      background: var(--bs-danger-bg);
      color: var(--bs-danger);
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

    .filter-controls {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    .filter-toggle {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .filter-toggle.active {
      background: var(--bs-primary);
      color: white;
      border-color: var(--bs-primary);
    }

    .sort-controls {
      display: flex;
      gap: 0.25rem;
    }

    .sort-select {
      min-width: 180px;
    }

    .sort-direction {
      border-left: none;
      border-top-left-radius: 0;
      border-bottom-left-radius: 0;
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

    .advanced-filters {
      margin-top: 1rem;
      padding-top: 1rem;
      border-top: 1px solid var(--bs-gray-200);
    }

    .filter-options {
      display: flex;
      gap: 1.5rem;
      align-items: center;
      flex-wrap: wrap;
    }

    .filter-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .filter-label {
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--bs-gray-700);
      margin: 0;
    }

    .filter-select {
      min-width: 120px;
    }

    .bulk-operations-bar {
      background: var(--bs-primary-bg);
      border: 1px solid var(--bs-primary-border);
      border-radius: 0.5rem;
      padding: 1rem 1.25rem;
      margin-bottom: 1.5rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .bulk-info {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .selected-count {
      font-weight: 600;
      color: var(--bs-primary);
    }

    .btn-link {
      background: none;
      border: none;
      color: var(--bs-primary);
      text-decoration: none;
      font-size: 0.9rem;
    }

    .btn-link:hover {
      text-decoration: underline;
    }

    .bulk-actions {
      display: flex;
      gap: 0.5rem;
    }

    .groups-content {
      background: white;
      border-radius: 0.5rem;
      overflow: hidden;
      border: 1px solid var(--bs-gray-200);
    }

    .table-container {
      overflow-x: auto;
    }

    .groups-table {
      margin: 0;
    }

    .groups-table th {
      background: var(--bs-gray-50);
      border-bottom: 2px solid var(--bs-gray-200);
      font-weight: 600;
      color: var(--bs-gray-700);
      padding: 1rem 0.75rem;
    }

    .groups-table td {
      padding: 1rem 0.75rem;
      vertical-align: middle;
      border-bottom: 1px solid var(--bs-gray-200);
    }

    .groups-table tbody tr:hover {
      background: var(--bs-gray-50);
    }

    .groups-table tbody tr.selected {
      background: var(--bs-primary-bg);
    }

    .select-column,
    .actions-column {
      width: 60px;
    }

    .group-name-cell {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .group-name {
      font-weight: 600;
      color: var(--bs-gray-900);
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
      max-width: 200px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .member-count {
      font-weight: 600;
      color: var(--bs-primary);
    }

    .creation-date {
      color: var(--bs-gray-600);
      font-size: 0.9rem;
    }

    .action-buttons {
      display: flex;
      gap: 0.25rem;
    }

    .action-buttons .btn {
      padding: 0.375rem 0.5rem;
    }

    .groups-grid {
      background: var(--bs-gray-50);
      padding: 1.5rem;
      border-radius: 0.5rem;
    }

    .group-card-container {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.5rem;
    }

    .group-card {
      background: white;
      border-radius: 0.5rem;
      border: 1px solid var(--bs-gray-200);
      transition: all 0.2s ease;
      overflow: hidden;
    }

    .group-card:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      transform: translateY(-2px);
    }

    .group-card.selected {
      border-color: var(--bs-primary);
      box-shadow: 0 0 0 2px var(--bs-primary-bg);
    }

    .card-header {
      padding: 1.25rem;
      border-bottom: 1px solid var(--bs-gray-200);
      display: flex;
      align-items: flex-start;
      gap: 1rem;
    }

    .card-title {
      flex: 1;
    }

    .card-title .group-name {
      margin: 0;
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .card-body {
      padding: 1.25rem;
    }

    .group-description {
      color: var(--bs-gray-600);
      margin-bottom: 1rem;
      font-size: 0.9rem;
      line-height: 1.4;
    }

    .group-stats {
      display: flex;
      gap: 1.5rem;
      margin-bottom: 1rem;
    }

    .stat-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .stat-item .stat-icon {
      color: var(--bs-gray-500);
    }

    .stat-item .stat-value {
      font-weight: 600;
      color: var(--bs-gray-900);
    }

    .stat-item .stat-label {
      color: var(--bs-gray-600);
      font-size: 0.85rem;
    }

    .group-meta {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.85rem;
    }

    .card-footer {
      padding: 1rem 1.25rem;
      background: var(--bs-gray-50);
      border-top: 1px solid var(--bs-gray-200);
      display: flex;
      gap: 0.5rem;
    }

    .card-footer .btn {
      flex: 1;
      font-size: 0.85rem;
      padding: 0.5rem 0.75rem;
    }

    .pagination-section {
      margin-top: 2rem;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
    }

    .pagination-info {
      color: var(--bs-gray-600);
      font-size: 0.9rem;
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

    .required:after {
      content: '*';
      color: var(--bs-danger);
      margin-left: 0.25rem;
    }

    .member-selection {
      min-height: 100px;
      border: 1px dashed var(--bs-gray-300);
      border-radius: 0.5rem;
      padding: 1rem;
      background: var(--bs-gray-50);
      display: flex;
      align-items: center;
      justify-content: center;
    }

    @media (max-width: 768px) {
      .group-management-container {
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

      .statistics-grid {
        grid-template-columns: 1fr;
      }

      .filters-row {
        flex-direction: column;
        align-items: stretch;
      }

      .filter-controls {
        justify-content: space-between;
      }

      .groups-grid .group-card-container {
        grid-template-columns: 1fr;
      }

      .bulk-operations-bar {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .bulk-actions {
        justify-content: center;
      }
    }
  `]
})
export class GroupAdvancedListComponent implements OnInit, OnDestroy {
  // Dependency Injection
  private readonly groupService = inject(GroupService);
  private readonly destroy$ = new Subject<void>();
  private readonly confirmationService = inject(ConfirmationService);
  private readonly errorHandler = inject(ErrorHandlerService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  // Icons
  readonly UsersIcon = Users;
  readonly SettingsIcon = Settings;
  readonly PlusIcon = Plus;
  readonly SearchIcon = Search;
  readonly FilterIcon = Filter;
  readonly DownloadIcon = Download;
  readonly UploadIcon = Upload;
  readonly MoreVerticalIcon = MoreVertical;
  readonly Edit2Icon = Edit2;
  readonly Trash2Icon = Trash2;
  readonly UserPlusIcon = UserPlus;
  readonly ShieldIcon = Shield;
  readonly CopyIcon = Copy;
  readonly FileTextIcon = FileText;
  readonly BarChart3Icon = BarChart3;
  readonly GridIcon = Grid;
  readonly ListIcon = List;
  readonly RefreshCwIcon = RefreshCw;
  readonly ArrowUpDownIcon = ArrowUpDown;
  readonly ChevronDownIcon = ChevronDown;
  readonly XIcon = X;
  readonly CheckIcon = Check;
  readonly AlertTriangleIcon = AlertTriangle;

  // State Signals
  groups = signal<GroupDto[]>([]);
  selectedGroups = signal<GroupDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  statistics = signal<GroupStatistics | null>(null);

  // Pagination
  currentPage = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));

  // Filters
  filters = signal<GroupFilter>({
    search: '',
    includeSystemGroups: true,
    hasMembers: null,
    sortBy: 'name',
    sortDirection: 'asc'
  });

  // UI State
  viewMode = signal<'table' | 'grid'>('table');
  showAdvancedFilters = signal(false);
  editingGroup = signal<GroupDto | null>(null);

  // Form
  groupForm: FormGroup;

  // Computed Values
  statisticsCards = computed(() => {
    const stats = this.statistics();
    if (!stats) return [];

    return [
      {
        title: 'Toplam Grup',
        value: stats.totalGroups.toString(),
        icon: Users,
        color: 'primary',
        trend: { value: 12, isPositive: true }
      },
      {
        title: 'Sistem Grupları',
        value: stats.systemGroups.toString(),
        icon: Shield,
        color: 'warning'
      },
      {
        title: 'Özel Gruplar',
        value: stats.customGroups.toString(),
        icon: Settings,
        color: 'info'
      },
      {
        title: 'Toplam Üye',
        value: stats.totalMembers.toString(),
        icon: Users,
        color: 'success',
        trend: { value: 8, isPositive: true }
      }
    ] as StatCard[];
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

  paginationInfo = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize() + 1;
    const end = Math.min(this.currentPage() * this.pageSize(), this.totalCount());
    return `${start}-${end} / ${this.totalCount()} grup`;
  });

  // Bulk Operations
  bulkOperations: BulkOperation[] = [
    {
      type: 'export',
      label: 'Dışa Aktar',
      icon: Download,
      confirmMessage: 'Seçili grupları dışa aktarmak istediğinizden emin misiniz?'
    },
    {
      type: 'addMembers',
      label: 'Üye Ekle',
      icon: UserPlus,
      confirmMessage: 'Seçili gruplara üye eklemek istediğinizden emin misiniz?',
      requiresInput: true
    },
    {
      type: 'assignPermissions',
      label: 'İzin Ata',
      icon: Shield,
      confirmMessage: 'Seçili gruplara izin atamak istediğinizden emin misiniz?',
      requiresInput: true
    },
    {
      type: 'delete',
      label: 'Sil',
      icon: Trash2,
      confirmMessage: 'Seçili grupları silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.'
    }
  ];

  constructor() {
    this.groupForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['']
    });
  }

  ngOnInit(): void {
    this.loadGroups();
    this.loadStatistics();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Data Loading Methods
  loadGroups(): void {
    this.loading.set(true);

    const request: GetGroupsRequest = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      search: this.filters().search || undefined
    };

    console.log('Loading groups with request:', request);
    this.groupService.getGroups(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          console.log('Groups API response:', response);
          if (response) {
            this.groups.set(response.data ?? []);
            this.totalCount.set(response.totalCount ?? response.pagination?.total ?? 0);
          }
          this.loading.set(false);
        },
        error: (error) => {
          this.errorHandler.handleError(error);
          this.loading.set(false);
        }
      });
  }

  loadStatistics(): void {
    this.groupService.getGroupStatistics()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response) {
            this.statistics.set(response);
          }
        },
        error: () => {
          console.warn('Statistics endpoint not available, using default values');
        }
      });
  }

  refreshData(): void {
    this.loadGroups();
    this.loadStatistics();
  }

  // Filter Methods
  onSearchChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.filters.update(f => ({ ...f, search: target.value }));
    this.debounceSearch();
  }

  private searchTimeout?: number;
  private debounceSearch(): void {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = window.setTimeout(() => {
      this.currentPage.set(1);
      this.loadGroups();
    }, 300);
  }

  clearSearch(): void {
    this.filters.update(f => ({ ...f, search: '' }));
    this.currentPage.set(1);
    this.loadGroups();
  }

  onSortChange(): void {
    this.currentPage.set(1);
    this.loadGroups();
  }

  toggleSortDirection(): void {
    this.filters.update(f => ({
      ...f,
      sortDirection: f.sortDirection === 'asc' ? 'desc' : 'asc'
    }));
    this.loadGroups();
  }

  toggleAdvancedFilters(): void {
    this.showAdvancedFilters.update(show => !show);
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadGroups();
  }

  clearAllFilters(): void {
    this.filters.set({
      search: '',
      includeSystemGroups: true,
      hasMembers: null,
      sortBy: 'name',
      sortDirection: 'asc'
    });
    this.showAdvancedFilters.set(false);
    this.currentPage.set(1);
    this.loadGroups();
  }

  hasActiveFilters(): boolean {
    const f = this.filters();
    return !!(f.search || !f.includeSystemGroups || f.hasMembers !== null);
  }

  // View Mode Methods
  setViewMode(mode: 'table' | 'grid'): void {
    this.viewMode.set(mode);
  }

  // Selection Methods
  toggleSelectAll(): void {
    const allSelected = this.isAllSelected();
    if (allSelected) {
      this.selectedGroups.set([]);
    } else {
      this.selectedGroups.set([...this.groups()]);
    }
  }

  toggleGroupSelection(group: GroupDto): void {
    const selected = this.selectedGroups();
    const index = selected.findIndex(g => g.id === group.id);

    if (index >= 0) {
      this.selectedGroups.set(selected.filter(g => g.id !== group.id));
    } else {
      this.selectedGroups.set([...selected, group]);
    }
  }

  isGroupSelected(group: GroupDto): boolean {
    return this.selectedGroups().some(g => g.id === group.id);
  }

  isAllSelected(): boolean {
    const groups = this.groups();
    const selected = this.selectedGroups();
    return groups.length > 0 && selected.length === groups.length;
  }

  isPartiallySelected(): boolean {
    const selected = this.selectedGroups();
    return selected.length > 0 && selected.length < this.groups().length;
  }

  clearSelection(): void {
    this.selectedGroups.set([]);
  }

  // Pagination Methods
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadGroups();
    }
  }

  // Group CRUD Methods
  openCreateGroupModal(): void {
    this.editingGroup.set(null);
    this.groupForm.reset();
    // Open modal programmatically
  }

  editGroup(group: GroupDto): void {
    this.editingGroup.set(group);
    this.groupForm.patchValue({
      name: group.name,
      description: group.description
    });
    // Open modal programmatically
  }

  saveGroup(): void {
    if (this.groupForm.invalid) return;

    this.saving.set(true);
    const formValue = this.groupForm.value;

    const editing = this.editingGroup();
    if (editing) {
      const request: UpdateGroupRequest = {
        name: formValue.name,
        description: formValue.description
      };

      this.groupService.updateGroup(editing.id, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response) {
              this.loadGroups();
              // Close modal
            }
            this.saving.set(false);
          },
          error: (error) => {
            this.errorHandler.handleError(error);
            this.saving.set(false);
          }
        });
    } else {
      const request: CreateGroupRequest = {
        name: formValue.name,
        description: formValue.description
      };

      this.groupService.createGroup(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response) {
              this.loadGroups();
              // Close modal
            }
            this.saving.set(false);
          },
          error: (error) => {
            this.errorHandler.handleError(error);
            this.saving.set(false);
          }
        });
    }
  }

  async deleteGroup(group: GroupDto): Promise<void> {
    if (group.isSystemGroup) {
      return;
    }

    const confirmed = await this.confirmationService.confirm({
      title: 'Grup Sil',
      message: `"${group.name}" grubunu silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.`,
      confirmText: 'Sil',
    });

    if (confirmed) {
      this.groupService.deleteGroup(group.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.loadGroups();
          },
          error: (error) => {
            this.errorHandler.handleError(error);
          }
        });
    }
  }

  cloneGroup(group: GroupDto): void {
    this.router.navigate(['/user-management/groups/clone', group.id]);
  }

  // Navigation Methods
  viewGroupDetails(group: GroupDto): void {
    this.router.navigate(['/user-management/groups', group.id]);
  }

  manageGroupMembers(group: GroupDto): void {
    this.router.navigate(['/user-management/groups', group.id, 'members']);
  }

  manageGroupPermissions(group: GroupDto): void {
    this.router.navigate(['/user-management/groups', group.id, 'permissions']);
  }

  // Bulk Operations
  async executeBulkOperation(operation: BulkOperation): Promise<void> {
    const selectedIds = this.selectedGroups().map(g => g.id);

    const confirmed = await this.confirmationService.confirm({
      title: `Toplu ${operation.label}`,
      message: operation.confirmMessage,
      confirmText: operation.label,
    });

    if (!confirmed) return;

    try {
      switch (operation.type) {
        case 'export':
          // Export functionality to be implemented
          break;
        case 'delete':
          this.bulkDeleteGroups(selectedIds);
          break;
        case 'addMembers':
          // Navigate to bulk member addition
          break;
        case 'assignPermissions':
          // Navigate to bulk permission assignment
          break;
      }
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  private bulkDeleteGroups(_groupIds: string[]): void {
    // Implementation for bulk delete
    this.loadGroups();
    this.clearSelection();
  }

  // Export Methods
  exportGroups(_format: 'excel' | 'csv' | 'pdf'): void {
    try {
      // Export functionality to be implemented
    } catch (error) {
      this.errorHandler.handleError(error);
    }
  }

  // Utility Methods
  trackByGroupId(_index: number, group: GroupDto): string {
    return group.id;
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(new Date(date));
  }
}