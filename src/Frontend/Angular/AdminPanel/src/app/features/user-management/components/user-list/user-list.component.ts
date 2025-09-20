import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Observable, debounceTime, distinctUntilChanged, switchMap, startWith, map } from 'rxjs';
import { Store } from '@ngrx/store';

import { DataTableComponent, DataTableConfig, PaginationInfo } from '../../../../shared/components/data-table/data-table.component';
import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  User,
  UserQuery,
  PagedResult,
  TableColumn,
  ActionButton,
  BulkAction,
  FilterGroup,
  UserStatistics,
  BulkUserOperationRequest
} from '../../models/user-management.models';
import { AppState } from '../../../../store';
import { UserActions, RoleActions } from '../../../../store/user-management';
import {
  selectAllUsers,
  selectUsersLoading,
  selectUserStatistics,
  selectUsersLastResult,
  selectAllRoles
} from '../../../../store/user-management/user-management.selectors';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterLink, DataTableComponent],
  template: `
    <div class="user-list-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <h1 class="page-title">
              <i class="fas fa-users me-3"></i>
              Kullanıcı Yönetimi
            </h1>
            <p class="page-description text-muted">
              Sistem kullanıcılarını görüntüleyin, düzenleyin ve yönetin
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
              <a routerLink="/users/create" class="btn btn-primary">
                <i class="fas fa-plus me-2"></i>
                Yeni Kullanıcı
              </a>
            </div>
          </div>
        </div>
      </div>

      <!-- Statistics Cards -->
      <div class="statistics-section mb-4" *ngIf="statistics()">
        <div class="row">
          <div class="col-md-3">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.totalUsers || 0 }}</div>
                <div class="stat-label">Toplam Kullanıcı</div>
              </div>
              <div class="stat-icon bg-primary">
                <i class="fas fa-users"></i>
              </div>
            </div>
          </div>
          <div class="col-md-3">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.activeUsers || 0 }}</div>
                <div class="stat-label">Aktif Kullanıcı</div>
              </div>
              <div class="stat-icon bg-success">
                <i class="fas fa-user-check"></i>
              </div>
            </div>
          </div>
          <div class="col-md-3">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.emailConfirmedUsers || 0 }}</div>
                <div class="stat-label">E-posta Doğrulanmış</div>
              </div>
              <div class="stat-icon bg-info">
                <i class="fas fa-envelope-circle-check"></i>
              </div>
            </div>
          </div>
          <div class="col-md-3">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.newUsersThisMonth || 0 }}</div>
                <div class="stat-label">Bu Ay Yeni</div>
              </div>
              <div class="stat-icon bg-warning">
                <i class="fas fa-user-plus"></i>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Data Table -->
      <app-data-table
        [data]="users()"
        [config]="tableConfig"
        [pagination]="paginationInfo()"
        [filters]="filterGroups()"
        [loading]="loading()"
        (sort)="onSort($event)"
        (pageChange)="onPageChange($event)"
        (pageSizeChange)="onPageSizeChange($event)"
        (search)="onSearch($event)"
        (filterChange)="onFilterChange($event)"
        (action)="onAction($event)"
        (bulkAction)="onBulkAction($event)"
        (export)="onExport()"
      ></app-data-table>
    </div>
  `,
  styles: [`
    .user-list-container {
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

    .page-description {
      font-size: 1rem;
      margin-bottom: 0;
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

    /* Responsive */
    @media (max-width: 768px) {
      .user-list-container {
        padding: 1rem;
      }

      .page-header .row {
        flex-direction: column;
        gap: 1rem;
      }

      .page-header .col-auto {
        width: 100%;
      }

      .stat-card {
        margin-bottom: 1rem;
      }
    }
  `]
})
export class UserListComponent implements OnInit {
  private readonly store = inject(Store<AppState>);
  private readonly userService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  // State
  users = signal<User[]>([]);
  loading = signal<boolean>(false);
  statistics = signal<UserStatistics | null>(null);
  currentQuery = signal<UserQuery>({
    page: 1,
    pageSize: 10,
    sortBy: 'createdAt',
    sortDirection: 'desc'
  });

  // Table Configuration
  tableConfig: DataTableConfig = {
    columns: [
      {
        key: 'firstName',
        label: 'Kullanıcı',
        type: 'avatar',
        sortable: true,
        width: '200px'
      },
      {
        key: 'email',
        label: 'E-posta',
        type: 'text',
        sortable: true,
        width: '200px'
      },
      {
        key: 'roles',
        label: 'Roller',
        type: 'text',
        width: '150px'
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
        key: 'emailConfirmed',
        label: 'E-posta Doğrulandı',
        type: 'boolean',
        sortable: true,
        width: '120px',
        align: 'center'
      },
      {
        key: 'lastLoginAt',
        label: 'Son Giriş',
        type: 'date',
        sortable: true,
        width: '150px'
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
    bulkActions: [
      {
        label: 'Aktifleştir',
        icon: 'fas fa-check',
        action: 'activate',
        type: 'success'
      },
      {
        label: 'Deaktive Et',
        icon: 'fas fa-ban',
        action: 'deactivate',
        type: 'warning'
      },
      {
        label: 'E-posta Gönder',
        icon: 'fas fa-envelope',
        action: 'sendEmail',
        type: 'info'
      },
      {
        label: 'Sil',
        icon: 'fas fa-trash',
        action: 'delete',
        type: 'danger',
        confirmMessage: 'Seçili kullanıcıları silmek istediğinizden emin misiniz?'
      }
    ],
    selectable: true,
    searchable: true,
    filterable: true,
    exportable: true,
    showCheckbox: true,
    showActions: true,
    pageSize: 10,
    pageSizeOptions: [10, 25, 50, 100]
  };

  // Computed properties
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

  filterGroups = computed<FilterGroup[]>(() => [
    {
      label: 'Durum',
      key: 'isActive',
      type: 'select',
      options: [
        { label: 'Aktif', value: true },
        { label: 'Pasif', value: false }
      ]
    },
    {
      label: 'E-posta Durumu',
      key: 'emailConfirmed',
      type: 'select',
      options: [
        { label: 'Doğrulanmış', value: true },
        { label: 'Doğrulanmamış', value: false }
      ]
    },
    {
      label: 'Rol',
      key: 'roleId',
      type: 'select',
      options: this.roleOptions()
    }
  ]);

  // Additional state for pagination
  private pagedResult = signal<PagedResult<User> | null>(null);
  private roleOptions = signal<Array<{label: string, value: string}>>([]);

  ngOnInit(): void {
    // Subscribe to store selectors
    this.store.select(selectAllUsers).subscribe(users => {
      this.users.set(users);
    });

    this.store.select(selectUsersLoading).subscribe(loading => {
      this.loading.set(loading);
    });

    this.store.select(selectUserStatistics).subscribe(statistics => {
      this.statistics.set(statistics);
    });

    this.store.select(selectUsersLastResult).subscribe(result => {
      this.pagedResult.set(result);
    });

    this.store.select(selectAllRoles).subscribe(roles => {
      this.roleOptions.set(
        roles.map(role => ({
          label: role.name,
          value: role.id
        }))
      );
    });

    // Initial data load
    this.loadUsers();
    this.loadStatistics();
    this.loadRoleOptions();
  }

  // Data Loading Methods
  loadUsers(): void {
    this.store.dispatch(UserActions.loadUsers({ query: this.currentQuery() }));
  }

  loadStatistics(): void {
    this.store.dispatch(UserActions.loadUserStatistics());
  }

  loadRoleOptions(): void {
    this.store.dispatch(RoleActions.loadRoles({ query: { page: 1, pageSize: 1000 } }));
  }

  refreshData(): void {
    this.loadUsers();
    this.loadStatistics();
  }

  // Event Handlers
  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.currentQuery.update(query => ({
      ...query,
      sortBy: event.column as any,
      sortDirection: event.direction,
      page: 1
    }));
    this.loadUsers();
  }

  onPageChange(page: number): void {
    this.currentQuery.update(query => ({
      ...query,
      page
    }));
    this.loadUsers();
  }

  onPageSizeChange(pageSize: number): void {
    this.currentQuery.update(query => ({
      ...query,
      pageSize,
      page: 1
    }));
    this.loadUsers();
  }

  onSearch(searchTerm: string): void {
    this.currentQuery.update(query => ({
      ...query,
      searchTerm,
      page: 1
    }));
    this.loadUsers();
  }

  onFilterChange(filters: { [key: string]: any }): void {
    this.currentQuery.update(query => ({
      ...query,
      ...filters,
      page: 1
    }));
    this.loadUsers();
  }

  onAction(event: { action: string; item: User }): void {
    switch (event.action) {
      case 'view':
        this.viewUser(event.item);
        break;
      case 'edit':
        this.editUser(event.item);
        break;
      case 'delete':
        this.deleteUser(event.item);
        break;
    }
  }

  onBulkAction(event: { action: string; items: User[] }): void {
    const userIds = event.items.map(user => user.id);

    const request: BulkUserOperationRequest = {
      userIds,
      operation: event.action as any
    };

    this.store.dispatch(UserActions.bulkUserOperation({ request }));
  }

  onExport(): void {
    // Export functionality will be implemented later
    this.notificationService.info('Dışa aktarma özelliği yakında eklenecek', 'Bilgi');
  }

  // Action Methods
  viewUser(user: User): void {
    // Navigation will be handled by router
    window.open(`/users/${user.id}`, '_blank');
  }

  editUser(user: User): void {
    // Navigation will be handled by router
    window.open(`/users/${user.id}/edit`, '_blank');
  }

  deleteUser(user: User): void {
    if (confirm(`${user.firstName} ${user.lastName} kullanıcısını silmek istediğinizden emin misiniz?`)) {
      this.store.dispatch(UserActions.deleteUser({ id: user.id }));
    }
  }
}