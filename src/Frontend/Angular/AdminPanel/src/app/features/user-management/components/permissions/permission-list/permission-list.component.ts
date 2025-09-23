import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';

import { AdvancedDataTableComponent, TableColumn } from '../../../../../shared/components/data-table/advanced-data-table.component';
import { ActionButtonGroupComponent, ActionButton } from '../../../../../shared/components/action-button-group/action-button-group.component';
import { ConfirmationModalComponent } from '../../../../../shared/components/confirmation-modal/confirmation-modal.component';
import { StatisticsCardComponent } from '../../../../../shared/components/statistics-card/statistics-card.component';

import { PermissionService } from '../../../services/permission.service';

import { PermissionDto, GetPermissionsRequest } from '../../../models';

@Component({
  selector: 'app-permission-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    LucideAngularModule,
    AdvancedDataTableComponent,
    ActionButtonGroupComponent,
    ConfirmationModalComponent,
    StatisticsCardComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-xl">
      <div class="page-header d-print-none">
        <div class="container-xl">
          <div class="row g-2 align-items-center">
            <div class="col">
              <div class="page-pretitle">Kullanıcı Yönetimi</div>
              <h2 class="page-title">Yetki Listesi</h2>
            </div>
            <div class="col-auto ms-auto d-print-none">
              <div class="btn-list">
                <button class="btn btn-primary d-none d-sm-inline-block"
                        (click)="createPermission()">
                  <lucide-icon name="plus" class="icon"></lucide-icon>
                  Yeni Yetki
                </button>
                <button class="btn btn-outline-secondary"
                        (click)="toggleView()">
                  <lucide-icon [name]="viewMode() === 'table' ? 'grid-3x3' : 'list'" class="icon"></lucide-icon>
                  {{ viewMode() === 'table' ? 'Kart Görünümü' : 'Tablo Görünümü' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="page-body">
        <div class="container-xl">
          <div class="row row-deck row-cards">
            <!-- Statistics Cards -->
            <div class="col-12">
              <div class="row row-cards">
                <div class="col-sm-6 col-lg-3">
                  <app-statistics-card
                    [config]="{
                      title: 'Toplam Yetki',
                      value: totalPermissions(),
                      icon: 'shield',
                      color: 'primary',
                      clickable: true
                    }"
                    (click)="onStatisticCardClick('total')"/>
                </div>
                <div class="col-sm-6 col-lg-3">
                  <app-statistics-card
                    [config]="{
                      title: 'Sistem Yetkileri',
                      value: systemPermissions(),
                      icon: 'settings',
                      color: 'success',
                      clickable: true
                    }"
                    (click)="onStatisticCardClick('system')"/>
                </div>
                <div class="col-sm-6 col-lg-3">
                  <app-statistics-card
                    [config]="{
                      title: 'Özel Yetkiler',
                      value: customPermissions(),
                      icon: 'user',
                      color: 'warning',
                      clickable: true
                    }"
                    (click)="onStatisticCardClick('custom')"/>
                </div>
                <div class="col-sm-6 col-lg-3">
                  <app-statistics-card
                    [config]="{
                      title: 'Atanmamış',
                      value: unassignedPermissions(),
                      icon: 'alert-circle',
                      color: 'danger',
                      clickable: true
                    }"
                    (click)="onStatisticCardClick('unassigned')"/>
                </div>
              </div>
            </div>

            <!-- Main Content -->
            <div class="col-12">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">Yetki Listesi</h3>
                  <div class="card-actions">
                    <app-action-button-group
                      [actions]="headerActions"
                      (actionClick)="onHeaderAction($event)"/>
                  </div>
                </div>

                <div class="card-body">
                  <!-- Filter Panel -->
                  <div class="mb-3">
                    <div class="row g-2">
                      <div class="col-md-4">
                        <input type="text" class="form-control" placeholder="Yetki adı, servis veya açıklama ara..." (input)="onSearchChange($event)">
                      </div>
                      <div class="col-md-2">
                        <button class="btn btn-outline-secondary" (click)="clearFilters()">Temizle</button>
                      </div>
                    </div>
                  </div>

                  <!-- Table View -->
                  @if (viewMode() === 'table') {
                    <app-advanced-data-table
                      [columns]="tableColumns"
                      [data]="permissions()"
                      [loading]="loading()"
                      [selectable]="true"
                      [actions]="[]"
                      (selectionChange)="onSelectionChange($event)"
                      (actionClick)="onActionClick($event)"
                      (sortChange)="onSortChange($event)"
                      (pageChange)="onPageChange($event)"/>
                  }

                  <!-- Card View -->
                  @if (viewMode() === 'cards') {
                    <div class="row row-cards">
                      @for (permission of permissions(); track permission.id) {
                        <div class="col-sm-6 col-lg-4">
                          <div class="card">
                            <div class="card-body">
                              <div class="d-flex align-items-center">
                                <span class="avatar bg-blue text-white me-3">
                                  <lucide-icon name="shield" [size]="24"></lucide-icon>
                                </span>
                                <div class="flex-fill">
                                  <div class="font-weight-medium">{{ permission.displayName }}</div>
                                  <div class="text-muted">{{ permission.name }}</div>
                                </div>
                              </div>
                              <div class="mt-3">
                                <div class="row">
                                  <div class="col">
                                    <div class="text-muted">Servis</div>
                                    <div>{{ permission.service }}</div>
                                  </div>
                                  <div class="col">
                                    <div class="text-muted">Kaynak</div>
                                    <div>{{ permission.resource }}</div>
                                  </div>
                                </div>
                                <div class="mt-2">
                                  <span class="badge"
                                        [class]="permission.isSystemPermission ? 'bg-green' : 'bg-blue'">
                                    {{ permission.isSystemPermission ? 'Sistem' : 'Özel' }}
                                  </span>
                                  <span class="badge bg-gray ms-1">{{ permission.action }}</span>
                                </div>
                              </div>
                            </div>
                            <div class="card-footer">
                              <div class="btn-list justify-content-end">
                                <button class="btn btn-sm btn-outline-primary"
                                        (click)="viewPermission(permission)">
                                  Görüntüle
                                </button>
                                @if (!permission.isSystemPermission) {
                                  <button class="btn btn-sm btn-outline-secondary"
                                          (click)="editPermission(permission)">
                                    Düzenle
                                  </button>
                                  <button class="btn btn-sm btn-outline-danger"
                                          (click)="deletePermission(permission)">
                                    Sil
                                  </button>
                                }
                              </div>
                            </div>
                          </div>
                        </div>
                      }
                    </div>
                  }
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Confirmation Modal -->
    <app-confirmation-modal
      [config]="confirmConfig()"
      (confirmed)="onConfirmAction()"
      (cancelled)="onCancelAction()"/>
  `,
  styles: [`
    .icon {
      width: 18px;
      height: 18px;
    }

    .avatar {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  `]
})
export class PermissionListComponent implements OnInit {
  private readonly permissionService = inject(PermissionService);

  // State signals
  permissions = signal<PermissionDto[]>([]);
  loading = signal(false);
  viewMode = signal<'table' | 'cards'>('table');
  selectedPermissions = signal<PermissionDto[]>([]);
  totalPermissions = signal(0);
  currentFilter = signal<GetPermissionsRequest>({});

  // Statistics
  systemPermissions = computed(() =>
    this.permissions().filter(p => p.isSystemPermission).length
  );

  customPermissions = computed(() =>
    this.permissions().filter(p => !p.isSystemPermission).length
  );

  unassignedPermissions = computed(() =>
    this.permissions().filter(p => !p.roleCount || p.roleCount === 0).length
  );

  // Confirmation modal config
  confirmConfig = signal<any>({
    show: false,
    title: '',
    message: '',
    type: 'danger',
    action: null
  });

  // Table configuration
  tableColumns: TableColumn[] = [
    {
      key: 'permission',
      label: 'Yetki',
      sortable: true,
      width: '300px'
    },
    {
      key: 'service',
      label: 'Servis',
      sortable: true
    },
    {
      key: 'resource',
      label: 'Kaynak',
      sortable: true
    },
    {
      key: 'action',
      label: 'Eylem',
      sortable: true,
      width: '120px'
    },
    {
      key: 'category',
      label: 'Kategori',
      sortable: true
    },
    {
      key: 'type',
      label: 'Tip',
      sortable: true,
      width: '100px'
    }
  ];

  // Header actions
  headerActions: ActionButton[] = [
    {
      key: 'add-permission',
      icon: 'plus',
      label: 'Yeni Yetki',
      variant: 'primary'
    },
    {
      key: 'export-excel',
      icon: 'download',
      label: 'Excel İndir',
      variant: 'secondary'
    },
    {
      key: 'import-permissions',
      icon: 'upload',
      label: 'İçe Aktar',
      variant: 'secondary'
    },
    {
      key: 'sync-permissions',
      icon: 'refresh-cw',
      label: 'Senkronize Et',
      variant: 'secondary'
    }
  ];


  ngOnInit() {
    this.loadPermissions();
  }

  async loadPermissions() {
    try {
      this.loading.set(true);
      const request = this.currentFilter();
      const response = await this.permissionService.getPermissions(request).toPromise();

      if (response) {
        this.permissions.set(response.data ?? []);
        this.totalPermissions.set(response.totalCount ?? response.pagination?.total ?? 0);
      }
    } catch (error) {
      console.error('Failed to load permissions:', error);
    } finally {
      this.loading.set(false);
    }
  }

  onSearchChange(event: any) {
    const searchTerm = event.target.value;
    const newFilter = {
      ...this.currentFilter(),
      search: searchTerm,
      page: 1
    };
    this.currentFilter.set(newFilter);
    this.loadPermissions();
  }

  onFilterChange(filters: any) {
    const newFilter: GetPermissionsRequest = {
      ...this.currentFilter(),
      ...filters,
      page: 1
    };
    this.currentFilter.set(newFilter);
    this.loadPermissions();
  }

  clearFilters() {
    this.currentFilter.set({});
    this.loadPermissions();
  }

  onSortChange(event: any) {
    const newFilter = {
      ...this.currentFilter(),
      sortBy: event.field,
      sortDirection: event.direction as 'asc' | 'desc'
    };
    this.currentFilter.set(newFilter);
    this.loadPermissions();
  }

  onPageChange(page: number) {
    const newFilter = {
      ...this.currentFilter(),
      page
    };
    this.currentFilter.set(newFilter);
    this.loadPermissions();
  }

  onSelectionChange(selected: PermissionDto[]) {
    this.selectedPermissions.set([...selected]);
  }

  onActionClick(event: {action: string, item: any}) {
    console.log('Action clicked:', event);
  }

  toggleView() {
    this.viewMode.set(this.viewMode() === 'table' ? 'cards' : 'table');
  }

  onHeaderAction(event: any) {
    switch (event.action) {
      case 'add-permission':
        this.createPermission();
        break;
      case 'export-excel':
        this.exportPermissions();
        break;
      case 'import-permissions':
        // Show import dialog
        break;
      case 'sync-permissions':
        this.syncPermissions();
        break;
    }
  }

  onStatisticCardClick(_type: string) {
    // Handle statistic card clicks - could filter the list
  }

  createPermission() {
    // Navigate to permission creation
  }

  viewPermission(_permission: PermissionDto) {
    // Navigate to permission detail
  }

  editPermission(_permission: PermissionDto) {
    // Navigate to permission edit
  }

  deletePermission(permission: PermissionDto) {
    this.confirmConfig.set({
      show: true,
      title: 'Yetkiyi Sil',
      message: `"${permission.displayName}" yetkisini silmek istediğinizden emin misiniz?`,
      type: 'danger',
      action: () => this.performDeletePermission(permission.id)
    });
  }

  async performDeletePermission(permissionId: string) {
    try {
      await this.permissionService.deletePermission(permissionId).toPromise();
      await this.loadPermissions();
    } catch (error) {
      console.error('Failed to delete permission:', error);
    }
  }

  async exportPermissions() {
    try {
      const filter = this.currentFilter();
      await this.permissionService.exportPermissions(filter).toPromise();
    } catch (error) {
      console.error('Export failed:', error);
    }
  }

  async syncPermissions() {
    try {
      this.loading.set(true);
      const result = await this.permissionService.syncPermissions().toPromise();
      console.log('Sync result:', result);
      await this.loadPermissions();
    } catch (error) {
      console.error('Sync failed:', error);
    } finally {
      this.loading.set(false);
    }
  }

  onConfirmAction() {
    const config = this.confirmConfig();
    if (config.action) {
      config.action();
    }
    this.onCancelAction();
  }

  onCancelAction() {
    this.confirmConfig.set({
      show: false,
      title: '',
      message: '',
      type: 'danger',
      action: null
    });
  }
}