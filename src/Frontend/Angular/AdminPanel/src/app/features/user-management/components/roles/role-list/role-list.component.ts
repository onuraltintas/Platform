import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Shield, Plus, Download, Upload, Search, Filter, MoreHorizontal, Copy, Users } from 'lucide-angular';

import { AdvancedDataTableComponent, TableColumn } from '../../../../../shared/components/data-table/advanced-data-table.component';
import { FilterPanelComponent, FilterField } from '../../../../../shared/components/filter-panel/filter-panel.component';
import { ActionButtonGroupComponent, ActionButton } from '../../../../../shared/components/action-button-group/action-button-group.component';
import { ConfirmationModalComponent } from '../../../../../shared/components/confirmation-modal/confirmation-modal.component';
import { StatisticsCardComponent } from '../../../../../shared/components/statistics-card/statistics-card.component';

import { RoleService } from '../../../services/role.service';
import { PermissionService } from '../../../services/permission.service';

import { RoleDto, GetRolesRequest } from '../../../models/role.models';
import { PermissionDto } from '../../../models/permission.models';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    LucideAngularModule,
    AdvancedDataTableComponent,
    FilterPanelComponent,
    ActionButtonGroupComponent,
    ConfirmationModalComponent,
    StatisticsCardComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-wrapper">
      <!-- Page Header -->
      <div class="page-header d-print-none">
        <div class="container-xl">
          <div class="row g-2 align-items-center">
            <div class="col">
              <div class="page-pretitle">Kullanıcı Yönetimi</div>
              <h2 class="page-title">Roller</h2>
            </div>
            <div class="col-auto ms-auto d-print-none">
              <app-action-button-group
                [actions]="headerActions"
                [selectedItems]="selectedRoles()"
                (actionClick)="onHeaderAction($event)"/>
            </div>
          </div>
        </div>
      </div>

      <!-- Page Content -->
      <div class="page-body">
        <div class="container-xl">
          <!-- Statistics Row -->
          <div class="row row-deck row-cards mb-4">
            @for (stat of statisticsCards(); track stat.title) {
              <div class="col-sm-6 col-lg-3">
                <app-statistics-card
                  [config]="stat"
                  (cardClick)="onStatisticCardClick($event)"/>
              </div>
            }
          </div>

          <div class="row row-deck row-cards">
            <!-- Filters -->
            <div class="col-3">
              <app-filter-panel
                [filterFields]="filterFields()"
                [initialFilters]="filters()"
                [showSearch]="true"
                [searchPlaceholder]="'Rol ara...'"
                (filtersChange)="onFiltersChange($event)"/>
            </div>

            <!-- Role List -->
            <div class="col-9">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">
                    Rol Listesi
                    @if (totalRoles() > 0) {
                      <span class="badge bg-secondary ms-2">{{ totalRoles() }}</span>
                    }
                  </h3>

                  <div class="card-actions">
                    <!-- Bulk Actions -->
                    @if (selectedRoles().length > 0) {
                      <div class="me-3">
                        <app-action-button-group
                          [actions]="bulkActions"
                          [selectedItems]="selectedRoles()"
                          [defaultSize]="'sm'"
                          (actionClick)="onBulkAction($event)"/>
                      </div>
                    }

                    <!-- View Options -->
                    <div class="btn-group" role="group">
                      <input type="radio" class="btn-check" name="view-mode" id="view-table"
                             [checked]="viewMode() === 'table'" (change)="setViewMode('table')">
                      <label class="btn btn-sm btn-outline-secondary" for="view-table">
                        <lucide-icon name="list" [size]="16"/>
                      </label>

                      <input type="radio" class="btn-check" name="view-mode" id="view-cards"
                             [checked]="viewMode() === 'cards'" (change)="setViewMode('cards')">
                      <label class="btn btn-sm btn-outline-secondary" for="view-cards">
                        <lucide-icon name="grid-3x3" [size]="16"/>
                      </label>
                    </div>
                  </div>
                </div>

                <!-- Table View -->
                @if (viewMode() === 'table') {
                  <app-advanced-data-table
                    [columns]="tableColumns"
                    [data]="roles()"
                    [loading]="loading()"
                    [selectable]="true"
                    [actions]="[]"
                    (selectionChange)="onSelectionChange($event)"
                    (actionClick)="onActionClick($event)"
                    (sortChange)="onSortChange($event)"
                    (pageChange)="onPageChange($event)"/>
                }

                <!-- Cards View -->
                @if (viewMode() === 'cards') {
                  <div class="card-body">
                    @if (loading()) {
                      <div class="row">
                        @for (i of [1,2,3,4,5,6]; track i) {
                          <div class="col-md-6 col-lg-4 mb-3">
                            <div class="card placeholder-glow">
                              <div class="card-body">
                                <div class="d-flex align-items-center">
                                  <div class="avatar placeholder me-3"></div>
                                  <div class="flex-fill">
                                    <div class="placeholder col-8 mb-1"></div>
                                    <div class="placeholder col-6"></div>
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                        }
                      </div>
                    } @else {
                      <div class="row">
                        @for (role of roles(); track role.id) {
                          <div class="col-md-6 col-lg-4 mb-3">
                            <div class="card cursor-pointer"
                                 [class.border-primary]="selectedRoles().includes(role)"
                                 (click)="toggleRoleSelection(role)">
                              <div class="card-body">
                                <div class="d-flex align-items-center mb-3">
                                  <div class="me-3">
                                    <div [class]="getRoleIconClasses(role)">
                                      <lucide-icon name="shield" [size]="20"/>
                                    </div>
                                  </div>
                                  <div class="flex-fill">
                                    <div class="font-weight-medium">{{ role.name }}</div>
                                    <div class="text-muted small">{{ role.description || 'Açıklama yok' }}</div>
                                  </div>
                                  <div class="dropdown">
                                    <button type="button" class="btn btn-sm" data-bs-toggle="dropdown">
                                      <lucide-icon name="more-horizontal" [size]="16"/>
                                    </button>
                                    <div class="dropdown-menu dropdown-menu-end">
                                      <a class="dropdown-item" [routerLink]="[role.id]">
                                        <lucide-icon name="eye" [size]="16" class="me-2"/>
                                        Görüntüle
                                      </a>
                                      <a class="dropdown-item" [routerLink]="[role.id, 'edit']">
                                        <lucide-icon name="edit" [size]="16" class="me-2"/>
                                        Düzenle
                                      </a>
                                      <a class="dropdown-item" [routerLink]="[role.id, 'permissions']">
                                        <lucide-icon name="shield" [size]="16" class="me-2"/>
                                        Yetkiler
                                      </a>
                                      <a class="dropdown-item" [routerLink]="[role.id, 'clone']">
                                        <lucide-icon name="copy" [size]="16" class="me-2"/>
                                        Kopyala
                                      </a>
                                      <div class="dropdown-divider"></div>
                                      <button class="dropdown-item text-danger" (click)="deleteRole(role)">
                                        <lucide-icon name="trash" [size]="16" class="me-2"/>
                                        Sil
                                      </button>
                                    </div>
                                  </div>
                                </div>

                                <!-- Role Stats -->
                                <div class="row text-center">
                                  <div class="col">
                                    <div class="h4 mb-0">{{ role.userCount || 0 }}</div>
                                    <div class="text-muted small">Kullanıcı</div>
                                  </div>
                                  <div class="col">
                                    <div class="h4 mb-0">{{ role.permissions.length || 0 }}</div>
                                    <div class="text-muted small">Yetki</div>
                                  </div>
                                </div>

                                <!-- Role Type Badge -->
                                <div class="mt-3">
                                  <span [class]="getRoleTypeClass(role)">
                                    {{ getRoleTypeText(role) }}
                                  </span>
                                  @if (role.isDefault) {
                                    <span class="badge bg-info ms-2">Varsayılan</span>
                                  }
                                </div>
                              </div>
                            </div>
                          </div>
                        }
                      </div>

                      <!-- Pagination for Cards View -->
                      @if (totalRoles() > pageSize()) {
                        <nav class="d-flex justify-content-center">
                          <ul class="pagination">
                            <li class="page-item" [class.disabled]="currentPage() === 1">
                              <button class="page-link" (click)="goToPage(currentPage() - 1)">Önceki</button>
                            </li>
                            @for (page of getVisiblePages(); track page) {
                              <li class="page-item" [class.active]="page === currentPage()">
                                <button class="page-link" (click)="goToPage(page)">{{ page }}</button>
                              </li>
                            }
                            <li class="page-item" [class.disabled]="currentPage() === totalPages()">
                              <button class="page-link" (click)="goToPage(currentPage() + 1)">Sonraki</button>
                            </li>
                          </ul>
                        </nav>
                      }
                    }
                  </div>
                }
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Confirmation Modal -->
    <app-confirmation-modal
      [visible]="showConfirmModal"
      [config]="confirmConfig"
      (result)="onConfirmResult($event)"/>
  `,
  styles: [`
    .cursor-pointer {
      cursor: pointer;
    }

    .card:hover {
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .placeholder-glow .placeholder {
      animation: placeholder-glow 2s ease-in-out infinite alternate;
    }

    @keyframes placeholder-glow {
      50% {
        opacity: 0.2;
      }
    }

    .btn-check:checked + .btn {
      background-color: var(--bs-primary);
      border-color: var(--bs-primary);
      color: var(--bs-white);
    }

    .role-icon {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 8px;
    }
  `]
})
export class RoleListComponent implements OnInit {
  // Services
  private roleService = inject(RoleService);
  private permissionService = inject(PermissionService);

  // Icons
  readonly shieldIcon = Shield;
  readonly plusIcon = Plus;
  readonly downloadIcon = Download;
  readonly uploadIcon = Upload;
  readonly searchIcon = Search;
  readonly filterIcon = Filter;
  readonly moreHorizontalIcon = MoreHorizontal;
  readonly copyIcon = Copy;
  readonly usersIcon = Users;

  // State signals
  loading = signal(false);
  roles = signal<RoleDto[]>([]);
  totalRoles = signal(0);
  selectedRoles = signal<RoleDto[]>([]);
  filters = signal<GetRolesRequest>({});
  viewMode = signal<'table' | 'cards'>('table');
  currentPage = signal(1);
  pageSize = signal(20);

  // Modal state
  showConfirmModal = signal(false);
  confirmConfig = signal<any>({});

  // Available permissions for filters
  availablePermissions = signal<PermissionDto[]>([]);

  // Computed values
  totalPages = computed(() => Math.ceil(this.totalRoles() / this.pageSize()));

  statisticsCards = computed(() => {
    const rolesData = this.roles();
    const totalCount = this.totalRoles();

    const systemRoles = rolesData.filter(r => r.isSystemRole).length;
    const customRoles = totalCount - systemRoles;
    const defaultRoles = rolesData.filter(r => r.isDefault).length;
    const totalPermissions = rolesData.reduce((sum, role) => sum + (role.permissions.length || 0), 0);

    return [
      {
        title: 'Toplam Rol',
        value: totalCount,
        icon: 'shield',
        color: 'primary' as const,
        subtitle: `${systemRoles} sistem, ${customRoles} özel`,
        clickable: true
      },
      {
        title: 'Varsayılan Roller',
        value: defaultRoles,
        icon: 'star',
        color: 'warning' as const,
        subtitle: 'Otomatik atanan roller',
        clickable: true
      },
      {
        title: 'Toplam Yetki',
        value: totalPermissions,
        icon: 'key',
        color: 'info' as const,
        subtitle: 'Atanmış yetki sayısı',
        clickable: true
      },
      {
        title: 'Aktif Kullanıcı',
        value: rolesData.reduce((sum, role) => sum + (role.userCount || 0), 0),
        icon: 'users',
        color: 'success' as const,
        subtitle: 'Role atanmış kullanıcılar',
        clickable: true
      }
    ];
  });

  filterFields = computed(() => {
    return [
      {
        key: 'isSystemRole',
        label: 'Rol Tipi',
        type: 'select' as const,
        options: [
          { label: 'Sistem Rolleri', value: 'true' },
          { label: 'Özel Roller', value: 'false' }
        ]
      },
      {
        key: 'isDefault',
        label: 'Varsayılan',
        type: 'boolean' as const
      },
      {
        key: 'hasUsers',
        label: 'Kullanıcı Durumu',
        type: 'select' as const,
        options: [
          { label: 'Kullanıcıları Olan', value: 'true' },
          { label: 'Kullanıcısız', value: 'false' }
        ]
      },
      {
        key: 'createdDate',
        label: 'Oluşturulma Tarihi',
        type: 'daterange' as const,
        advanced: true
      }
    ] as FilterField[];
  });

  // Table columns
  tableColumns: TableColumn[] = [
    {
      key: 'name',
      label: 'Rol Adı',
      sortable: true,
      width: '200px'
    },
    {
      key: 'description',
      label: 'Açıklama',
      sortable: false
    },
    {
      key: 'userCount',
      label: 'Kullanıcı Sayısı',
      sortable: true,
      align: 'center',
      width: '120px'
    },
    {
      key: 'permissionCount',
      label: 'Yetki Sayısı',
      sortable: false,
      align: 'center',
      width: '120px'
    },
    {
      key: 'type',
      label: 'Tip',
      sortable: false,
      width: '100px',
      type: 'badge'
    },
    {
      key: 'createdAt',
      label: 'Oluşturulma',
      sortable: true,
      type: 'date',
      width: '120px'
    }
  ];

  // Header actions
  headerActions: ActionButton[] = [
    {
      key: 'add-role',
      label: 'Yeni Rol',
      icon: 'plus',
      variant: 'primary',
      requiresPermission: 'Identity.Roles.Create'
    },
    {
      key: 'role-matrix',
      label: 'Yetki Matrisi',
      icon: 'grid-3x3',
      variant: 'outline-info'
    },
    {
      key: 'import-export',
      label: 'İçe/Dışa Aktar',
      icon: 'download',
      variant: 'outline-secondary',
      dropdown: [
        {
          key: 'export-roles',
          label: 'Rolleri Dışa Aktar',
          icon: 'download'
        },
        {
          key: 'export-permissions',
          label: 'Yetkileri Dışa Aktar',
          icon: 'file-text'
        },
        { key: 'divider', label: '', icon: '' },
        {
          key: 'import-roles',
          label: 'Rol İçe Aktar',
          icon: 'upload'
        }
      ]
    }
  ];

  // Bulk actions
  bulkActions: ActionButton[] = [
    {
      key: 'bulk-clone',
      label: 'Kopyala',
      icon: 'copy',
      variant: 'outline-primary',
      requiresSelection: true,
      requiresPermission: 'Identity.Roles.Create'
    },
    {
      key: 'bulk-export',
      label: 'Dışa Aktar',
      icon: 'download',
      variant: 'outline-info',
      requiresSelection: true
    },
    {
      key: 'bulk-delete',
      label: 'Sil',
      icon: 'trash',
      variant: 'outline-danger',
      requiresSelection: true,
      requiresPermission: 'Identity.Roles.Delete',
      destructive: true,
      confirmMessage: 'Seçili rolleri silmek istediğinizden emin misiniz?'
    }
  ];

  ngOnInit() {
    this.loadRoles();
    this.loadPermissions();
  }

  private async loadRoles() {
    this.loading.set(true);

    try {
      const request: GetRolesRequest = {
        ...this.filters(),
        page: this.currentPage(),
        pageSize: this.pageSize()
      };

      const response = await this.roleService.getRoles(request).toPromise();

      if (response) {
        this.roles.set(response.data ?? []);
        this.totalRoles.set(response.totalCount ?? response.pagination?.total ?? 0);
      }
    } catch (error) {
      console.error('Failed to load roles:', error);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadPermissions() {
    try {
      const response = await this.permissionService.getPermissions().toPromise();
      this.availablePermissions.set(response?.data || []);
    } catch (error) {
      console.error('Failed to load permissions:', error);
    }
  }

  onFiltersChange(filters: any) {
    this.filters.set(filters);
    this.currentPage.set(1);
    this.loadRoles();
  }

  onSelectionChange(selectedItems: RoleDto[]) {
    this.selectedRoles.set(selectedItems);
  }

  onRowAction(event: any) {
    const role = event.item as RoleDto;

    switch (event.action) {
      case 'view':
        // Navigate to role detail
        break;
      case 'edit':
        // Navigate to role edit
        break;
      case 'clone':
        this.cloneRole(role);
        break;
      case 'delete':
        this.deleteRole(role);
        break;
    }
  }

  onRowClick(_role: RoleDto) {
    // Navigate to role detail
  }

  onActionClick(event: {action: string, item: any}) {
    // Handle table action clicks
    console.log('Action clicked:', event);
  }

  onHeaderAction(event: any) {
    switch (event.action) {
      case 'add-role':
        // Navigate to role creation
        break;
      case 'role-matrix':
        // Navigate to role matrix
        break;
      case 'export-roles':
        this.exportRoles();
        break;
      case 'export-permissions':
        this.exportPermissions();
        break;
      case 'import-roles':
        // Show import dialog
        break;
    }
  }

  onBulkAction(event: any) {
    const selectedRoleIds = this.selectedRoles().map(r => r.id);

    switch (event.action) {
      case 'bulk-clone':
        this.bulkCloneRoles(selectedRoleIds);
        break;
      case 'bulk-export':
        this.bulkExportRoles(selectedRoleIds);
        break;
      case 'bulk-delete':
        this.showBulkDeleteConfirmation();
        break;
    }
  }

  onSortChange(event: any) {
    const filters = this.filters();
    filters.sortBy = event.column;
    filters.sortDirection = event.direction;
    this.filters.set(filters);
    this.loadRoles();
  }

  onPageChange(event: any) {
    this.currentPage.set(event.page);
    this.pageSize.set(event.pageSize);
    this.loadRoles();
  }

  setViewMode(mode: 'table' | 'cards') {
    this.viewMode.set(mode);
  }

  toggleRoleSelection(role: RoleDto) {
    const selected = this.selectedRoles();
    const index = selected.findIndex(r => r.id === role.id);

    if (index > -1) {
      selected.splice(index, 1);
    } else {
      selected.push(role);
    }

    this.selectedRoles.set([...selected]);
  }

  onStatisticCardClick(_config: any) {
    // Handle statistic card clicks - could filter the list
  }

  deleteRole(role: RoleDto) {
    this.confirmConfig.set({
      title: 'Rolü Sil',
      message: `${role.name} rolünü silmek istediğinizden emin misiniz?`,
      type: 'danger',
      confirmText: 'Sil',
      cancelText: 'İptal',
      details: role.userCount ? [`Bu rolde ${role.userCount} kullanıcı bulunmaktadır`] : []
    });

    this.showConfirmModal.set(true);
  }

  cloneRole(role: RoleDto) {
    // Implement role cloning logic
    console.log('Cloning role:', role);
  }

  async bulkCloneRoles(roleIds: string[]) {
    try {
      // Implement bulk clone logic
      console.log('Bulk cloning roles:', roleIds);
    } catch (error) {
      console.error('Bulk clone failed:', error);
    }
  }

  showBulkDeleteConfirmation() {
    const count = this.selectedRoles().length;
    const totalUsers = this.selectedRoles().reduce((sum, role) => sum + (role.userCount || 0), 0);

    this.confirmConfig.set({
      title: 'Rolleri Sil',
      message: `${count} rolü silmek istediğinizden emin misiniz?`,
      type: 'danger',
      confirmText: 'Sil',
      cancelText: 'İptal',
      details: [
        `${totalUsers} kullanıcı bu rollerden etkilenecek`,
        ...this.selectedRoles().map(r => `${r.name} (${r.userCount || 0} kullanıcı)`)
      ]
    });

    this.showConfirmModal.set(true);
  }

  onConfirmResult(result: any) {
    this.showConfirmModal.set(false);

    if (result.confirmed) {
      // Handle confirmation based on the current action
    }
  }

  async exportRoles() {
    try {
      await this.roleService.exportRoles(this.filters()).toPromise();
    } catch (error) {
      console.error('Export roles failed:', error);
    }
  }

  async exportPermissions() {
    try {
      await this.permissionService.exportPermissions().toPromise();
    } catch (error) {
      console.error('Export permissions failed:', error);
    }
  }

  async bulkExportRoles(roleIds: string[]) {
    try {
      await this.roleService.exportRoles({ ids: roleIds }).toPromise();
    } catch (error) {
      console.error('Bulk export failed:', error);
    }
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadRoles();
    }
  }

  getVisiblePages(): number[] {
    const current = this.currentPage();
    const total = this.totalPages();
    const delta = 2;

    const range: number[] = [];
    const rangeWithDots: number[] = [];

    for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
      range.push(i);
    }

    if (current - delta > 2) {
      rangeWithDots.push(1, -1);
    } else {
      rangeWithDots.push(1);
    }

    rangeWithDots.push(...range);

    if (current + delta < total - 1) {
      rangeWithDots.push(-1, total);
    } else {
      if (total > 1) {
        rangeWithDots.push(total);
      }
    }

    return rangeWithDots.filter(n => n > 0);
  }

  getRoleIconClasses(role: RoleDto): string {
    const baseClass = 'role-icon';

    if (role.isSystemRole) {
      return `${baseClass} bg-danger-subtle text-danger`;
    } else if (role.isDefault) {
      return `${baseClass} bg-warning-subtle text-warning`;
    } else {
      return `${baseClass} bg-primary-subtle text-primary`;
    }
  }

  getRoleTypeClass(role: RoleDto): string {
    if (role.isSystemRole) {
      return 'badge bg-danger';
    } else {
      return 'badge bg-primary';
    }
  }

  getRoleTypeText(role: RoleDto): string {
    return role.isSystemRole ? 'Sistem' : 'Özel';
  }
}