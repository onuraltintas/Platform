import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, UserPlus, Download, Upload, Search, Filter, MoreHorizontal } from 'lucide-angular';

import { AdvancedDataTableComponent, TableColumn, TableConfig } from '../../../../../shared/components/data-table/advanced-data-table.component';
import { FilterPanelComponent, FilterField } from '../../../../../shared/components/filter-panel/filter-panel.component';
import { ActionButtonGroupComponent, ActionButton } from '../../../../../shared/components/action-button-group/action-button-group.component';
import { ConfirmationModalComponent } from '../../../../../shared/components/confirmation-modal/confirmation-modal.component';

import { UserService } from '../../../services/user.service';
import { RoleService } from '../../../services/role.service';
import { GroupService } from '../../../services/group.service';

import { UserDto, GetUsersRequest, BulkUserOperation } from '../../../models/user.models';
import { RoleDto } from '../../../models/role.models';
import { GroupDto } from '../../../models/group.models';
// PagedResponse is available from user.models.ts

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    LucideAngularModule,
    AdvancedDataTableComponent,
    FilterPanelComponent,
    ActionButtonGroupComponent,
    ConfirmationModalComponent
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
              <h2 class="page-title">Kullanıcılar</h2>
            </div>
            <div class="col-auto ms-auto d-print-none">
              <app-action-button-group
                [actions]="headerActions"
                [selectedItems]="selectedUsers()"
                (actionClick)="onHeaderAction($event)"/>
            </div>
          </div>
        </div>
      </div>

      <!-- Page Content -->
      <div class="page-body">
        <div class="container-xl">
          <div class="row row-deck row-cards">
            <!-- Filters -->
            <div class="col-3">
              <app-filter-panel
                [filterFields]="filterFields()"
                [initialFilters]="filters()"
                [showSearch]="true"
                [searchPlaceholder]="'Kullanıcı ara...'"
                (filtersChange)="onFiltersChange($event)"/>
            </div>

            <!-- User List -->
            <div class="col-9">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">
                    Kullanıcı Listesi
                    @if (totalUsers() > 0) {
                      <span class="badge bg-secondary ms-2">{{ totalUsers() }}</span>
                    }
                  </h3>

                  <div class="card-actions">
                    <!-- Bulk Actions -->
                    @if (selectedUsers().length > 0) {
                      <div class="me-3">
                        <app-action-button-group
                          [actions]="bulkActions"
                          [selectedItems]="selectedUsers()"
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

                      <input type="radio" class="btn-check" name="view-mode" id="view-grid"
                             [checked]="viewMode() === 'grid'" (change)="setViewMode('grid')">
                      <label class="btn btn-sm btn-outline-secondary" for="view-grid">
                        <lucide-icon name="grid-3x3" [size]="16"/>
                      </label>
                    </div>
                  </div>
                </div>

                <!-- Table View -->
                @if (viewMode() === 'table') {
                  <app-advanced-data-table
                    [columns]="tableColumns"
                    [data]="users()"
                    [loading]="loading()"
                    [selectable]="true"
                    [actions]="[]"
                    (selectionChange)="onSelectionChange($event)"
                    (actionClick)="onActionClick($event)"
                    (sortChange)="onSortChange($event)"
                    (pageChange)="onPageChange($event)"/>
                }

                <!-- Grid View -->
                @if (viewMode() === 'grid') {
                  <div class="card-body">
                    @if (loading()) {
                      <div class="row">
                        @for (i of [1,2,3,4,5,6]; track i) {
                          <div class="col-md-4 col-lg-3 mb-3">
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
                        @for (user of users(); track user.id) {
                          <div class="col-md-4 col-lg-3 mb-3">
                            <div class="card card-sm cursor-pointer"
                                 [class.border-primary]="selectedUsers().includes(user)"
                                 (click)="toggleUserSelection(user)">
                              <div class="card-body">
                                <div class="d-flex align-items-center">
                                  <div class="me-3">
                                    <span class="avatar">
                                      {{ getInitials(user.firstName, user.lastName) }}
                                    </span>
                                  </div>
                                  <div class="flex-fill">
                                    <div class="font-weight-medium">{{ user.fullName || (user.firstName + ' ' + user.lastName) }}</div>
                                    <div class="text-muted small">{{ user.email }}</div>
                                    <div class="mt-1">
                                      <span [class]="getUserStatusClass(user)">
                                        {{ getUserStatusText(user) }}
                                      </span>
                                    </div>
                                  </div>
                                  <div class="dropdown">
                                    <button type="button" class="btn btn-sm" data-bs-toggle="dropdown">
                                      <lucide-icon name="more-horizontal" [size]="16"/>
                                    </button>
                                    <div class="dropdown-menu dropdown-menu-end">
                                      <a class="dropdown-item" [routerLink]="[user.id]">
                                        <lucide-icon name="eye" [size]="16" class="me-2"/>
                                        Görüntüle
                                      </a>
                                      <a class="dropdown-item" [routerLink]="[user.id, 'edit']">
                                        <lucide-icon name="edit" [size]="16" class="me-2"/>
                                        Düzenle
                                      </a>
                                      <div class="dropdown-divider"></div>
                                      <button class="dropdown-item text-danger" (click)="deleteUser(user)">
                                        <lucide-icon name="trash" [size]="16" class="me-2"/>
                                        Sil
                                      </button>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                        }
                      </div>

                      <!-- Pagination for Grid View -->
                      @if (totalUsers() > pageSize()) {
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

    .card-sm:hover {
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .avatar {
      background-size: cover;
      background-position: center;
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
  `]
})
export class UserListComponent implements OnInit {
  // Services
  private userService = inject(UserService);
  private roleService = inject(RoleService);
  private groupService = inject(GroupService);

  // Icons
  readonly userPlusIcon = UserPlus;
  readonly downloadIcon = Download;
  readonly uploadIcon = Upload;
  readonly searchIcon = Search;
  readonly filterIcon = Filter;
  readonly moreHorizontalIcon = MoreHorizontal;

  // State signals
  loading = signal(false);
  users = signal<UserDto[]>([]);
  totalUsers = signal(0);
  selectedUsers = signal<UserDto[]>([]);
  filters = signal<GetUsersRequest>({});
  viewMode = signal<'table' | 'grid'>('table');
  currentPage = signal(1);
  pageSize = signal(20);

  // Modal state
  showConfirmModal = signal(false);
  confirmConfig = signal<any>({});

  // Available roles and groups for filters
  availableRoles = signal<RoleDto[]>([]);
  availableGroups = signal<GroupDto[]>([]);

  // Computed values
  totalPages = computed(() => Math.ceil(this.totalUsers() / this.pageSize()));

  filterFields = computed(() => {
    const roles = this.availableRoles();
    const groups = this.availableGroups();

    return [
      {
        key: 'status',
        label: 'Durum',
        type: 'select' as const,
        options: [
          { label: 'Aktif', value: 'active' },
          { label: 'Pasif', value: 'inactive' },
          { label: 'Onay Bekleyen', value: 'pending' },
          { label: 'Engelli', value: 'blocked' }
        ]
      },
      {
        key: 'role',
        label: 'Rol',
        type: 'select' as const,
        options: roles.map(role => ({ label: role.name, value: role.id }))
      },
      {
        key: 'group',
        label: 'Grup',
        type: 'multiselect' as const,
        options: groups.map(group => ({ label: group.name, value: group.id }))
      },
      {
        key: 'emailVerified',
        label: 'E-posta Doğrulandı',
        type: 'boolean' as const
      },
      {
        key: 'createdDate',
        label: 'Kayıt Tarihi',
        type: 'daterange' as const,
        advanced: true
      },
      {
        key: 'lastLoginDate',
        label: 'Son Giriş',
        type: 'daterange' as const,
        advanced: true
      }
    ] as FilterField[];
  });

  tableConfig = signal<TableConfig>({
    showSelection: true,
    showActions: true,
    showPagination: true,
    pageSize: this.pageSize(),
    sortable: true,
    selectable: true,
    stickyHeader: true
  });

  // Table columns
  tableColumns: TableColumn[] = [
    {
      key: 'user',
      label: 'Kullanıcı',
      sortable: true,
      width: '300px'
    },
    {
      key: 'email',
      label: 'E-posta',
      sortable: true
    },
    {
      key: 'roles',
      label: 'Roller',
      sortable: false
    },
    {
      key: 'status',
      label: 'Durum',
      sortable: true,
      width: '120px'
    },
    {
      key: 'createdAt',
      label: 'Kayıt Tarihi',
      sortable: true,
      width: '150px'
    },
    {
      key: 'lastLoginAt',
      label: 'Son Giriş',
      sortable: true,
      width: '150px'
    }
  ];

  // Header actions
  headerActions: ActionButton[] = [
    {
      key: 'add-user',
      label: 'Yeni Kullanıcı',
      icon: 'user-plus',
      variant: 'primary',
      requiresPermission: 'Identity.Users.Create'
    },
    {
      key: 'import-export',
      label: 'İçe/Dışa Aktar',
      icon: 'download',
      variant: 'outline-secondary',
      dropdown: [
        {
          key: 'export-excel',
          label: 'Excel Olarak Dışa Aktar',
          icon: 'file-spreadsheet'
        },
        {
          key: 'export-csv',
          label: 'CSV Olarak Dışa Aktar',
          icon: 'file-text'
        },
        { key: 'divider', label: '', icon: '' },
        {
          key: 'import-users',
          label: 'Kullanıcı İçe Aktar',
          icon: 'upload'
        },
        {
          key: 'download-template',
          label: 'Şablon İndir',
          icon: 'download'
        }
      ]
    }
  ];

  // Bulk actions
  bulkActions: ActionButton[] = [
    {
      key: 'bulk-activate',
      label: 'Aktifleştir',
      icon: 'user-check',
      variant: 'outline-success',
      requiresSelection: true,
      requiresPermission: 'Identity.Users.Update'
    },
    {
      key: 'bulk-deactivate',
      label: 'Pasifleştir',
      icon: 'user-x',
      variant: 'outline-warning',
      requiresSelection: true,
      requiresPermission: 'Identity.Users.Update'
    },
    {
      key: 'bulk-delete',
      label: 'Sil',
      icon: 'trash',
      variant: 'outline-danger',
      requiresSelection: true,
      requiresPermission: 'Identity.Users.Delete',
      destructive: true,
      confirmMessage: 'Seçili kullanıcıları silmek istediğinizden emin misiniz?'
    }
  ];

  ngOnInit() {
    this.loadUsers();
    this.loadFilterData();
  }

  private async loadUsers() {
    this.loading.set(true);

    try {
      const request: GetUsersRequest = {
        ...this.filters(),
        page: this.currentPage(),
        pageSize: this.pageSize()
      };

      const response = await this.userService.getUsers(request).toPromise();

      if (response) {
        this.users.set(response.users || response.data || []);
        this.totalUsers.set(response.totalCount);
      }
    } catch (error) {
      console.error('Failed to load users:', error);
      // Show error notification
    } finally {
      this.loading.set(false);
    }
  }

  private async loadFilterData() {
    try {
      const [roles, groups] = await Promise.all([
        this.roleService.getRoles().toPromise(),
        this.groupService.getGroups().toPromise()
      ]);

      this.availableRoles.set(roles?.data || []);
      this.availableGroups.set(groups?.data || []);
    } catch (error) {
      console.error('Failed to load filter data:', error);
    }
  }

  onFiltersChange(filters: any) {
    this.filters.set(filters);
    this.currentPage.set(1);
    this.loadUsers();
  }

  onSelectionChange(selectedItems: UserDto[]) {
    this.selectedUsers.set(selectedItems);
  }

  onRowAction(event: any) {
    const user = event.item as UserDto;

    switch (event.action) {
      case 'view':
        // Navigate to user detail
        break;
      case 'edit':
        // Navigate to user edit
        break;
      case 'delete':
        this.deleteUser(user);
        break;
    }
  }

  onRowClick(_user: UserDto) {
    // Navigate to user detail
  }

  onActionClick(_: {action: string, item: any}) {
    // Handle table action clicks
  }

  onHeaderAction(event: any) {
    switch (event.action) {
      case 'add-user':
        // Navigate to user creation
        break;
      case 'export-excel':
        this.exportUsers('excel');
        break;
      case 'export-csv':
        this.exportUsers('csv');
        break;
      case 'import-users':
        // Show import dialog
        break;
      case 'download-template':
        this.downloadTemplate();
        break;
    }
  }

  onBulkAction(event: any) {
    const selectedUserIds = this.selectedUsers().map(u => u.id);

    switch (event.action) {
      case 'bulk-activate':
        this.bulkUpdateUsers(selectedUserIds, { isActive: true });
        break;
      case 'bulk-deactivate':
        this.bulkUpdateUsers(selectedUserIds, { isActive: false });
        break;
      case 'bulk-delete':
        this.showBulkDeleteConfirmation();
        break;
    }
  }

  onSortChange(event: any) {
    this.filters.set({
      ...this.filters(),
      sortBy: event.column,
      sortDirection: event.direction
    });
    this.loadUsers();
  }

  onPageChange(event: any) {
    this.currentPage.set(event.page);
    this.pageSize.set(event.pageSize);
    this.loadUsers();
  }

  setViewMode(mode: 'table' | 'grid') {
    this.viewMode.set(mode);
  }

  toggleUserSelection(user: UserDto) {
    const selected = this.selectedUsers();
    const index = selected.findIndex(u => u.id === user.id);

    if (index > -1) {
      selected.splice(index, 1);
    } else {
      selected.push(user);
    }

    this.selectedUsers.set([...selected]);
  }

  deleteUser(user: UserDto) {
    this.confirmConfig.set({
      title: 'Kullanıcıyı Sil',
      message: `${user.firstName} ${user.lastName} kullanıcısını silmek istediğinizden emin misiniz?`,
      type: 'danger',
      confirmText: 'Sil',
      cancelText: 'İptal'
    });

    this.showConfirmModal.set(true);
  }

  async bulkUpdateUsers(userIds: string[], updates: any) {
    try {
      const operation: BulkUserOperation = {
        userIds,
        operation: 'update',
        data: updates
      };

      await this.userService.bulkOperation(operation).toPromise();
      this.loadUsers();
      this.selectedUsers.set([]);
    } catch (error) {
      console.error('Bulk update failed:', error);
    }
  }

  showBulkDeleteConfirmation() {
    const count = this.selectedUsers().length;

    this.confirmConfig.set({
      title: 'Kullanıcıları Sil',
      message: `${count} kullanıcıyı silmek istediğinizden emin misiniz?`,
      type: 'danger',
      confirmText: 'Sil',
      cancelText: 'İptal',
      details: this.selectedUsers().map(u => `${u.firstName} ${u.lastName} (${u.email})`)
    });

    this.showConfirmModal.set(true);
  }

  onConfirmResult(result: any) {
    this.showConfirmModal.set(false);

    if (result.confirmed) {
      // Handle confirmation based on the current action
    }
  }

  async exportUsers(format: 'excel' | 'csv') {
    try {
      const filters = this.filters();
      const exportRequest: GetUsersRequest & { format: string } = { ...filters, format };
      await this.userService.exportUsers(exportRequest).toPromise();
    } catch (error) {
      console.error('Export failed:', error);
    }
  }

  async downloadTemplate() {
    try {
      await this.userService.downloadImportTemplate().toPromise();
    } catch (error) {
      console.error('Template download failed:', error);
    }
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadUsers();
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

  getInitials(firstName: string, lastName: string): string {
    return `${firstName?.charAt(0) || ''}${lastName?.charAt(0) || ''}`.toUpperCase();
  }

  getAvatarUrl(userName: string): string {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(userName)}&background=random`;
  }

  getUserStatusClass(user: UserDto): string {
    if (!user.isActive) return 'badge bg-danger';
    if (!user.isEmailConfirmed) return 'badge bg-warning';
    return 'badge bg-success';
  }

  getUserStatusText(user: UserDto): string {
    if (!user.isActive) return 'Pasif';
    if (!user.isEmailConfirmed) return 'Onay Bekleyen';
    return 'Aktif';
  }
}