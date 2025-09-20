import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';

import { DataTableComponent, DataTableConfig, PaginationInfo } from '../../../../shared/components/data-table/data-table.component';
import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  Role,
  RoleQuery,
  PagedResult,
  TableColumn,
  ActionButton,
  RoleStatistics
} from '../../models/user-management.models';
import { AppState } from '../../../../store';
import { RoleActions } from '../../../../store/user-management';
import {
  selectAllRoles,
  selectRolesLoading,
  selectRoleStatistics,
  selectRolesLastResult
} from '../../../../store/user-management/user-management.selectors';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [CommonModule, RouterLink, DataTableComponent],
  template: `
    <div class="role-list-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <h1 class="page-title">
              <i class="fas fa-user-tag me-3"></i>
              Rol Yönetimi
            </h1>
            <p class="page-description text-muted">
              Sistem rollerini görüntüleyin, düzenleyin ve yönetin
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
              <a routerLink="/roles/create" class="btn btn-primary">
                <i class="fas fa-plus me-2"></i>
                Yeni Rol
              </a>
            </div>
          </div>
        </div>
      </div>

      <!-- Statistics -->
      <div class="statistics-section mb-4" *ngIf="statistics()">
        <div class="row">
          <div class="col-md-3">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.totalRoles || 0 }}</div>
                <div class="stat-label">Toplam Rol</div>
              </div>
              <div class="stat-icon bg-primary">
                <i class="fas fa-user-tag"></i>
              </div>
            </div>
          </div>
          <div class="col-md-3">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.activeRoles || 0 }}</div>
                <div class="stat-label">Aktif Rol</div>
              </div>
              <div class="stat-icon bg-success">
                <i class="fas fa-check-circle"></i>
              </div>
            </div>
          </div>
          <div class="col-md-3">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.defaultRoles || 0 }}</div>
                <div class="stat-label">Varsayılan Rol</div>
              </div>
              <div class="stat-icon bg-info">
                <i class="fas fa-star"></i>
              </div>
            </div>
          </div>
          <div class="col-md-3">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.customRoles || 0 }}</div>
                <div class="stat-label">Özel Rol</div>
              </div>
              <div class="stat-icon bg-warning">
                <i class="fas fa-cog"></i>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Data Table -->
      <app-data-table
        [data]="roles()"
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
  `,
  styles: [`
    .role-list-container {
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
  `]
})
export class RoleListComponent implements OnInit {
  private readonly store = inject(Store<AppState>);
  private readonly roleService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  roles = signal<Role[]>([]);
  loading = signal<boolean>(false);
  statistics = signal<RoleStatistics | null>(null);
  currentQuery = signal<RoleQuery>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDirection: 'asc'
  });

  private pagedResult = signal<PagedResult<Role> | null>(null);

  tableConfig: DataTableConfig = {
    columns: [
      {
        key: 'name',
        label: 'Rol Adı',
        type: 'text',
        sortable: true,
        width: '200px'
      },
      {
        key: 'description',
        label: 'Açıklama',
        type: 'text',
        width: '300px'
      },
      {
        key: 'isActive',
        label: 'Durum',
        type: 'badge',
        sortable: true,
        width: '100px',
        align: 'center'
      },
      {
        key: 'isDefault',
        label: 'Varsayılan',
        type: 'boolean',
        sortable: true,
        width: '100px',
        align: 'center'
      },
      {
        key: 'userCount',
        label: 'Kullanıcı Sayısı',
        type: 'number',
        sortable: true,
        width: '120px',
        align: 'center'
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
      },
      {
        label: 'Düzenle',
        icon: 'fas fa-edit',
        action: 'edit',
        type: 'secondary'
      },
      {
        label: 'Sil',
        icon: 'fas fa-trash',
        action: 'delete',
        type: 'danger'
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
    this.loadRoles();
    this.loadStatistics();
  }

  loadRoles(): void {
    this.loading.set(true);

    this.roleService.getRoles(this.currentQuery()).subscribe({
      next: (result) => {
        this.roles.set(result.data);
        this.pagedResult.set(result);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading roles:', error);
        this.notificationService.error('Roller yüklenirken bir hata oluştu', 'Hata');
        this.loading.set(false);
      }
    });
  }

  loadStatistics(): void {
    this.roleService.getRoleStatistics().subscribe({
      next: (stats) => {
        this.statistics.set(stats);
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  refreshData(): void {
    this.loadRoles();
    this.loadStatistics();
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.currentQuery.update(query => ({
      ...query,
      sortBy: event.column as any,
      sortDirection: event.direction,
      page: 1
    }));
    this.loadRoles();
  }

  onPageChange(page: number): void {
    this.currentQuery.update(query => ({ ...query, page }));
    this.loadRoles();
  }

  onPageSizeChange(pageSize: number): void {
    this.currentQuery.update(query => ({ ...query, pageSize, page: 1 }));
    this.loadRoles();
  }

  onSearch(searchTerm: string): void {
    this.currentQuery.update(query => ({ ...query, searchTerm, page: 1 }));
    this.loadRoles();
  }

  onAction(event: { action: string; item: Role }): void {
    switch (event.action) {
      case 'view':
        this.viewRole(event.item);
        break;
      case 'edit':
        this.editRole(event.item);
        break;
      case 'delete':
        this.deleteRole(event.item);
        break;
    }
  }

  onExport(): void {
    this.notificationService.info('Dışa aktarma özelliği yakında eklenecek', 'Bilgi');
  }

  viewRole(role: Role): void {
    window.open(`/roles/${role.id}`, '_blank');
  }

  editRole(role: Role): void {
    window.open(`/roles/${role.id}/edit`, '_blank');
  }

  deleteRole(role: Role): void {
    if (confirm(`${role.name} rolünü silmek istediğinizden emin misiniz?`)) {
      this.roleService.deleteRole(role.id).subscribe({
        next: () => {
          this.notificationService.success('Rol başarıyla silindi', 'İşlem Başarılı');
          this.loadRoles();
        },
        error: (error) => {
          console.error('Delete role error:', error);
          this.notificationService.error('Rol silinirken bir hata oluştu', 'Hata');
        }
      });
    }
  }
}