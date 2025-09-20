import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

import { DataTableComponent, DataTableConfig, PaginationInfo } from '../../../../shared/components/data-table/data-table.component';
import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  Group,
  GroupQuery,
  PagedResult,
  GroupStatistics
} from '../../models/user-management.models';

@Component({
  selector: 'app-group-list',
  standalone: true,
  imports: [CommonModule, RouterLink, DataTableComponent],
  template: `
    <div class="group-list-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <h1 class="page-title">
              <i class="fas fa-users me-3"></i>
              Grup Yönetimi
            </h1>
            <p class="page-description text-muted">
              Kullanıcı gruplarını görüntüleyin, düzenleyin ve yönetin
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
              <a routerLink="/groups/create" class="btn btn-primary">
                <i class="fas fa-plus me-2"></i>
                Yeni Grup
              </a>
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
                <div class="stat-number">{{ statistics()?.totalGroups || 0 }}</div>
                <div class="stat-label">Toplam Grup</div>
              </div>
              <div class="stat-icon bg-primary">
                <i class="fas fa-users"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.activeGroups || 0 }}</div>
                <div class="stat-label">Aktif Grup</div>
              </div>
              <div class="stat-icon bg-success">
                <i class="fas fa-check-circle"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.departmentGroups || 0 }}</div>
                <div class="stat-label">Departman</div>
              </div>
              <div class="stat-icon bg-info">
                <i class="fas fa-building"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.teamGroups || 0 }}</div>
                <div class="stat-label">Takım</div>
              </div>
              <div class="stat-icon bg-warning">
                <i class="fas fa-user-friends"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.projectGroups || 0 }}</div>
                <div class="stat-label">Proje</div>
              </div>
              <div class="stat-icon bg-secondary">
                <i class="fas fa-project-diagram"></i>
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <div class="stat-card">
              <div class="stat-content">
                <div class="stat-number">{{ statistics()?.averageUsersPerGroup?.toFixed(1) || 0 }}</div>
                <div class="stat-label">Ort. Kullanıcı/Grup</div>
              </div>
              <div class="stat-icon bg-dark">
                <i class="fas fa-chart-line"></i>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Data Table -->
      <app-data-table
        [data]="groups()"
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
    .group-list-container {
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

    @media (max-width: 768px) {
      .statistics-section .col-md-2 {
        margin-bottom: 1rem;
      }
    }
  `]
})
export class GroupListComponent implements OnInit {
  private readonly groupService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  groups = signal<Group[]>([]);
  loading = signal<boolean>(false);
  statistics = signal<GroupStatistics | null>(null);
  currentQuery = signal<GroupQuery>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDirection: 'asc'
  });

  private pagedResult = signal<PagedResult<Group> | null>(null);

  tableConfig: DataTableConfig = {
    columns: [
      {
        key: 'name',
        label: 'Grup Adı',
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
        key: 'userCount',
        label: 'Kullanıcı Sayısı',
        type: 'number',
        sortable: true,
        width: '120px',
        align: 'center'
      },
      {
        key: 'roleCount',
        label: 'Rol Sayısı',
        type: 'number',
        width: '100px',
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
    this.loadGroups();
    this.loadStatistics();
  }

  loadGroups(): void {
    this.loading.set(true);

    this.groupService.getGroups(this.currentQuery()).subscribe({
      next: (result) => {
        this.groups.set(result.data);
        this.pagedResult.set(result);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading groups:', error);
        this.notificationService.error('Gruplar yüklenirken bir hata oluştu', 'Hata');
        this.loading.set(false);
      }
    });
  }

  loadStatistics(): void {
    this.groupService.getGroupStatistics().subscribe({
      next: (stats) => {
        this.statistics.set(stats);
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  refreshData(): void {
    this.loadGroups();
    this.loadStatistics();
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.currentQuery.update(query => ({
      ...query,
      sortBy: event.column as any,
      sortDirection: event.direction,
      page: 1
    }));
    this.loadGroups();
  }

  onPageChange(page: number): void {
    this.currentQuery.update(query => ({ ...query, page }));
    this.loadGroups();
  }

  onPageSizeChange(pageSize: number): void {
    this.currentQuery.update(query => ({ ...query, pageSize, page: 1 }));
    this.loadGroups();
  }

  onSearch(searchTerm: string): void {
    this.currentQuery.update(query => ({ ...query, searchTerm, page: 1 }));
    this.loadGroups();
  }

  onAction(event: { action: string; item: Group }): void {
    switch (event.action) {
      case 'view':
        this.viewGroup(event.item);
        break;
      case 'edit':
        this.editGroup(event.item);
        break;
      case 'delete':
        this.deleteGroup(event.item);
        break;
    }
  }

  onExport(): void {
    this.notificationService.info('Dışa aktarma özelliği yakında eklenecek', 'Bilgi');
  }

  viewGroup(group: Group): void {
    window.open(`/groups/${group.id}`, '_blank');
  }

  editGroup(group: Group): void {
    window.open(`/groups/${group.id}/edit`, '_blank');
  }

  deleteGroup(group: Group): void {
    if (confirm(`${group.name} grubunu silmek istediğinizden emin misiniz?`)) {
      this.groupService.deleteGroup(group.id).subscribe({
        next: () => {
          this.notificationService.success('Grup başarıyla silindi', 'İşlem Başarılı');
          this.loadGroups();
        },
        error: (error) => {
          console.error('Delete group error:', error);
          this.notificationService.error('Grup silinirken bir hata oluştu', 'Hata');
        }
      });
    }
  }
}