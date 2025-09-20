import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

import { DataTableComponent, DataTableConfig, PaginationInfo } from '../../../../shared/components/data-table/data-table.component';
import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  Permission,
  PermissionQuery,
  PagedResult,
  PermissionStatistics,
  PermissionCategory
} from '../../models/user-management.models';

@Component({
  selector: 'app-permission-list',
  standalone: true,
  imports: [CommonModule, RouterLink, DataTableComponent],
  template: `
    <div class="permission-list-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <h1 class="page-title">
              <i class="fas fa-shield-alt me-3"></i>
              İzin Yönetimi
            </h1>
            <p class="page-description text-muted">
              Sistem izinlerini görüntüleyin ve kategorilere göre düzenleyin
            </p>
          </div>
          <div class="col-auto">
            <div class="d-flex gap-2">
              <button
                type="button"
                class="btn btn-outline-secondary"
                (click)="refreshData()"
                [disabled]="loading()"
              >
                <i class="fas fa-sync-alt me-2" [class.fa-spin]="loading()"></i>
                Yenile
              </button>
              <div class="dropdown">
                <button
                  class="btn btn-outline-primary dropdown-toggle"
                  type="button"
                  data-bs-toggle="dropdown"
                  aria-expanded="false"
                >
                  <i class="fas fa-filter me-2"></i>
                  Kategori Filtresi
                </button>
                <ul class="dropdown-menu">
                  <li>
                    <a
                      class="dropdown-item"
                      href="#"
                      [class.active]="selectedCategory() === ''"
                      (click)="filterByCategory(''); $event.preventDefault()"
                    >
                      <i class="fas fa-list me-2"></i>
                      Tüm Kategoriler
                    </a>
                  </li>
                  <li><hr class="dropdown-divider"></li>
                  <li *ngFor="let category of permissionCategories()">
                    <a
                      class="dropdown-item"
                      href="#"
                      [class.active]="selectedCategory() === category.name"
                      (click)="filterByCategory(category.name); $event.preventDefault()"
                    >
                      <i [class]="category.icon || 'fas fa-folder'" class="me-2"></i>
                      {{ category.displayName }}
                      <span class="badge bg-secondary ms-2">{{ category.permissions.length }}</span>
                    </a>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Statistics -->
      <div class="statistics-section mb-4" *ngIf="statistics()">
        <div class="row">
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.totalPermissions || 0 }}</div>
                <div class="stat-label">Toplam İzin</div>
              </div>
              <div class="stat-icon bg-primary">
                <i class="fas fa-shield-alt"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.categoryCount || 0 }}</div>
                <div class="stat-label">Kategori Sayısı</div>
              </div>
              <div class="stat-icon bg-success">
                <i class="fas fa-tags"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.systemPermissions || 0 }}</div>
                <div class="stat-label">Sistem İzni</div>
              </div>
              <div class="stat-icon bg-info">
                <i class="fas fa-cog"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.userPermissions || 0 }}</div>
                <div class="stat-label">Kullanıcı İzni</div>
              </div>
              <div class="stat-icon bg-warning">
                <i class="fas fa-users"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.modulePermissions || 0 }}</div>
                <div class="stat-label">Modül İzni</div>
              </div>
              <div class="stat-icon bg-secondary">
                <i class="fas fa-puzzle-piece"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.resourcePermissions || 0 }}</div>
                <div class="stat-label">Kaynak İzni</div>
              </div>
              <div class="stat-icon bg-dark">
                <i class="fas fa-database"></i>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Category Filter Info -->
      <div class="filter-info mb-3" *ngIf="selectedCategory()">
        <div class="alert alert-info d-flex align-items-center">
          <i class="fas fa-info-circle me-2"></i>
          <span>
            <strong>{{ getCategoryDisplayName() }}</strong> kategorisindeki izinler gösteriliyor.
          </span>
          <button
            type="button"
            class="btn btn-sm btn-outline-primary ms-auto"
            (click)="filterByCategory('')"
          >
            <i class="fas fa-times me-1"></i>
            Filtreyi Kaldır
          </button>
        </div>
      </div>

      <!-- Permissions by Categories -->
      <div class="categories-section" *ngIf="viewMode() === 'categories'">
        <div class="row">
          <div class="col-lg-12">
            <div class="d-flex justify-content-between align-items-center mb-4">
              <h5 class="mb-0">
                <i class="fas fa-layer-group me-2"></i>
                Kategorilere Göre İzinler
              </h5>
              <div class="btn-group" role="group">
                <input type="radio" class="btn-check" name="viewMode" id="categories" autocomplete="off" checked>
                <label class="btn btn-outline-primary" for="categories" (click)="setViewMode('categories')">
                  <i class="fas fa-layer-group me-1"></i>
                  Kategoriler
                </label>
                <input type="radio" class="btn-check" name="viewMode" id="table" autocomplete="off">
                <label class="btn btn-outline-primary" for="table" (click)="setViewMode('table')">
                  <i class="fas fa-table me-1"></i>
                  Tablo
                </label>
              </div>
            </div>

            <div class="categories-container">
              <div
                class="category-card mb-4"
                *ngFor="let category of getFilteredCategories()"
              >
                <div class="category-header">
                  <div class="d-flex align-items-center">
                    <div class="category-icon">
                      <i [class]="category.icon || 'fas fa-folder'"></i>
                    </div>
                    <div class="category-info">
                      <h6 class="category-name">{{ category.displayName }}</h6>
                      <p class="category-description">{{ category.description }}</p>
                    </div>
                    <div class="category-meta">
                      <span class="badge bg-primary">{{ category.permissions.length }} İzin</span>
                    </div>
                  </div>
                </div>
                <div class="category-permissions">
                  <div class="row">
                    <div
                      class="col-lg-6 mb-3"
                      *ngFor="let permission of category.permissions"
                    >
                      <div class="permission-card">
                        <div class="permission-header">
                          <h6 class="permission-name">{{ permission.name }}</h6>
                          <span class="badge bg-secondary">{{ permission.name }}</span>
                        </div>
                        <p class="permission-description">{{ permission.description }}</p>
                        <div class="permission-actions">
                          <button
                            type="button"
                            class="btn btn-outline-primary btn-sm"
                            (click)="viewPermission(permission)"
                          >
                            <i class="fas fa-eye me-1"></i>
                            Detay
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Data Table -->
      <div class="table-section" *ngIf="viewMode() === 'table'">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <h5 class="mb-0">
            <i class="fas fa-table me-2"></i>
            İzin Listesi
          </h5>
          <div class="btn-group" role="group">
            <input type="radio" class="btn-check" name="viewMode" id="categories2" autocomplete="off">
            <label class="btn btn-outline-primary" for="categories2" (click)="setViewMode('categories')">
              <i class="fas fa-layer-group me-1"></i>
              Kategoriler
            </label>
            <input type="radio" class="btn-check" name="viewMode" id="table2" autocomplete="off" checked>
            <label class="btn btn-outline-primary" for="table2" (click)="setViewMode('table')">
              <i class="fas fa-table me-1"></i>
              Tablo
            </label>
          </div>
        </div>

        <app-data-table
          [data]="permissions()"
          [config]="tableConfig"
          [pagination]="paginationInfo()"
          [loading]="loading()"
          (sort)="onSort($event)"
          (pageChange)="onPageChange($event)"
          (pageSizeChange)="onPageSizeChange($event)"
          (search)="onSearch($event)"
          (action)="onAction($event)"
          (export)="onExport()"
        ></app-data-table>
      </div>
    </div>
  `,
  styles: [`
    .permission-list-container {
      padding: 1.5rem;
    }

    .page-header {
      margin-bottom: 2rem;
    }

    .page-title {
      font-size: 2rem;
      font-weight: 600;
      color: var(--bs-body-color);
      margin-bottom: 0.5rem;
    }

    .statistics-section {
      margin-bottom: 2rem;
    }

    .stat-card {
      background: var(--bs-card-bg);
      border-radius: var(--border-radius-md);
      padding: 1.5rem;
      box-shadow: var(--shadow-sm);
      display: flex;
      align-items: center;
      justify-content: space-between;
      transition: all var(--transition-normal);
      height: 100px;
    }

    .stat-card:hover {
      box-shadow: var(--shadow-md);
      transform: translateY(-2px);
    }

    .stat-content {
      flex: 1;
    }

    .stat-number {
      font-size: 2rem;
      font-weight: 700;
      color: var(--bs-body-color);
      line-height: 1;
    }

    .stat-label {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color);
      margin-top: 0.25rem;
    }

    .stat-icon {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.5rem;
      color: white;
    }

    .category-card {
      background: var(--bs-card-bg);
      border: 1px solid var(--bs-border-color);
      border-radius: var(--border-radius-md);
      box-shadow: var(--shadow-sm);
      transition: all var(--transition-normal);
    }

    .category-card:hover {
      box-shadow: var(--shadow-md);
    }

    .category-header {
      padding: 1.5rem;
      border-bottom: 1px solid var(--bs-border-color);
      background: var(--bs-gray-50);
    }

    .category-icon {
      width: 50px;
      height: 50px;
      border-radius: 50%;
      background: var(--bs-primary);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.25rem;
      margin-right: 1rem;
    }

    .category-info {
      flex: 1;
    }

    .category-name {
      font-size: 1.25rem;
      font-weight: 600;
      margin-bottom: 0.25rem;
      color: var(--bs-body-color);
    }

    .category-description {
      color: var(--bs-nav-link-color);
      margin: 0;
      font-size: 0.875rem;
    }

    .category-meta {
      text-align: right;
    }

    .category-permissions {
      padding: 1.5rem;
    }

    .permission-card {
      border: 1px solid var(--bs-border-color);
      border-radius: var(--border-radius);
      padding: 1rem;
      height: 100%;
      transition: all var(--transition-normal);
      background: var(--bs-card-bg);
    }

    .permission-card:hover {
      border-color: var(--bs-primary);
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .permission-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 0.75rem;
    }

    .permission-name {
      font-size: 1rem;
      font-weight: 600;
      margin: 0;
      color: var(--bs-body-color);
    }

    .permission-description {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color);
      margin-bottom: 1rem;
      line-height: 1.4;
    }

    .permission-actions {
      text-align: right;
    }

    .dropdown-item.active {
      background-color: var(--bs-primary);
      color: white;
    }

    .btn-check:checked + .btn-outline-primary {
      background-color: var(--bs-primary);
      border-color: var(--bs-primary);
      color: white;
    }

    @media (max-width: 768px) {
      .statistics-section .col-md-2 {
        margin-bottom: 1rem;
      }

      .category-header .d-flex {
        flex-direction: column;
        text-align: center;
      }

      .category-icon {
        margin: 0 auto 1rem auto;
      }

      .category-meta {
        text-align: center;
        margin-top: 1rem;
      }
    }
  `]
})
export class PermissionListComponent implements OnInit {
  private readonly permissionService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  permissions = signal<Permission[]>([]);
  permissionCategories = signal<PermissionCategory[]>([]);
  loading = signal<boolean>(false);
  statistics = signal<PermissionStatistics | null>(null);
  selectedCategory = signal<string>('');
  viewMode = signal<'categories' | 'table'>('categories');
  currentQuery = signal<PermissionQuery>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDirection: 'asc'
  });

  private pagedResult = signal<PagedResult<Permission> | null>(null);

  tableConfig: DataTableConfig = {
    columns: [
      {
        key: 'displayName',
        label: 'İzin Adı',
        type: 'text',
        sortable: true,
        width: '200px'
      },
      {
        key: 'name',
        label: 'Sistem Adı',
        type: 'text',
        sortable: true,
        width: '200px'
      },
      {
        key: 'category',
        label: 'Kategori',
        type: 'badge',
        sortable: true,
        width: '150px',
        align: 'center'
      },
      {
        key: 'description',
        label: 'Açıklama',
        type: 'text',
        width: '300px'
      },
      {
        key: 'createdAt',
        label: 'Oluşturulma',
        type: 'date',
        sortable: true,
        width: '150px'
      }
    ],
    actions: [
      {
        label: 'Görüntüle',
        icon: 'fas fa-eye',
        action: 'view',
        type: 'primary'
      }
    ],
    selectable: false,
    searchable: true,
    filterable: false,
    exportable: true,
    showActions: true,
    pageSize: 10,
    pageSizeOptions: [10, 25, 50, 100]
  };

  paginationInfo = computed<PaginationInfo | null>(() => {
    const result = this.pagedResult();
    if (!result) return null;

    return {
      page: result.pageNumber,
      pageSize: result.pageSize,
      totalItems: result.totalCount,
      totalPages: result.totalPages
    };
  });

  ngOnInit(): void {
    this.loadPermissions();
    this.loadPermissionCategories();
    this.loadStatistics();
  }

  loadPermissions(): void {
    this.loading.set(true);

    this.permissionService.getPermissions(this.currentQuery()).subscribe({
      next: (result) => {
        this.permissions.set(result.data);
        this.pagedResult.set(result);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading permissions:', error);
        this.notificationService.error('İzinler yüklenirken bir hata oluştu', 'Hata');
        this.loading.set(false);
      }
    });
  }

  loadPermissionCategories(): void {
    this.permissionService.getPermissionCategories().subscribe({
      next: (categories) => {
        this.permissionCategories.set(categories);
      },
      error: (error) => {
        console.error('Error loading permission categories:', error);
      }
    });
  }

  loadStatistics(): void {
    this.permissionService.getPermissionStatistics().subscribe({
      next: (stats) => {
        this.statistics.set(stats);
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  refreshData(): void {
    this.loadPermissions();
    this.loadPermissionCategories();
    this.loadStatistics();
  }

  setViewMode(mode: 'categories' | 'table'): void {
    this.viewMode.set(mode);
  }

  filterByCategory(categoryName: string): void {
    this.selectedCategory.set(categoryName);
    this.currentQuery.update(query => ({ ...query, category: categoryName, page: 1 }));
    if (this.viewMode() === 'table') {
      this.loadPermissions();
    }
  }

  getFilteredCategories(): PermissionCategory[] {
    const categories = this.permissionCategories();
    const selectedCat = this.selectedCategory();

    if (!selectedCat) {
      return categories;
    }

    return categories.filter(cat => cat.name === selectedCat);
  }

  getCategoryDisplayName(): string {
    const selectedCat = this.selectedCategory();
    const category = this.permissionCategories().find(cat => cat.name === selectedCat);
    return category?.displayName || selectedCat;
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.currentQuery.update(query => ({
      ...query,
      sortBy: event.column as any,
      sortDirection: event.direction,
      page: 1
    }));
    this.loadPermissions();
  }

  onPageChange(page: number): void {
    this.currentQuery.update(query => ({ ...query, page }));
    this.loadPermissions();
  }

  onPageSizeChange(pageSize: number): void {
    this.currentQuery.update(query => ({ ...query, pageSize, page: 1 }));
    this.loadPermissions();
  }

  onSearch(searchTerm: string): void {
    this.currentQuery.update(query => ({ ...query, searchTerm, page: 1 }));
    this.loadPermissions();
  }

  onAction(event: { action: string; item: Permission }): void {
    switch (event.action) {
      case 'view':
        this.viewPermission(event.item);
        break;
    }
  }

  onExport(): void {
    this.notificationService.info('Dışa aktarma özelliği yakında eklenecek', 'Bilgi');
  }

  viewPermission(permission: Permission): void {
    window.open(`/permissions/${permission.id}`, '_blank');
  }
}