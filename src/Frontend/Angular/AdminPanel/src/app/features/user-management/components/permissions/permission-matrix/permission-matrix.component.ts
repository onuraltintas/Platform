import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Grid3x3, Search, Filter, Download, Eye, EyeOff, RotateCcw, Save, AlertTriangle } from 'lucide-angular';

import { ActionButtonGroupComponent, ActionButton } from '../../../../../shared/components/action-button-group/action-button-group.component';
import { FilterPanelComponent, FilterField } from '../../../../../shared/components/filter-panel/filter-panel.component';
import { ConfirmationModalComponent } from '../../../../../shared/components/confirmation-modal/confirmation-modal.component';

import { RoleService } from '../../../services/role.service';
import { PermissionService } from '../../../services/permission.service';

import { RoleDto } from '../../../models/role.models';
import { PermissionDto } from '../../../models/permission.models';

interface MatrixCell {
  roleId: string;
  permissionId: string;
  granted: boolean;
  inherited: boolean;
  source?: string;
  modified?: boolean;
}

@Component({
  selector: 'app-permission-matrix',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    LucideAngularModule,
    ActionButtonGroupComponent,
    FilterPanelComponent,
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
              <h2 class="page-title">
                <lucide-icon name="grid-3x3" [size]="24" class="me-2"/>
                Yetki Matrisi
              </h2>
              <div class="page-subtitle">
                Rollere atanan yetkileri matrix formatında görüntüleyin ve düzenleyin
              </div>
            </div>
            <div class="col-auto ms-auto d-print-none">
              <app-action-button-group
                [actions]="headerActions"
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
                [searchPlaceholder]="'Rol veya yetki ara...'"
                (filtersChange)="onFiltersChange($event)"/>

              <!-- Legend -->
              <div class="card mt-3">
                <div class="card-header">
                  <h3 class="card-title">Açıklama</h3>
                </div>
                <div class="card-body">
                  <div class="row g-2">
                    <div class="col-12">
                      <span class="badge bg-success me-2">✓</span>
                      <small>Yetki Verildi</small>
                    </div>
                    <div class="col-12">
                      <span class="badge bg-light text-dark me-2">−</span>
                      <small>Yetki Verilmedi</small>
                    </div>
                    <div class="col-12">
                      <span class="badge bg-warning me-2">◗</span>
                      <small>Miras Alınan</small>
                    </div>
                    <div class="col-12">
                      <span class="badge bg-info me-2">★</span>
                      <small>Sistem Rolü</small>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Quick Stats -->
              <div class="card mt-3">
                <div class="card-header">
                  <h3 class="card-title">İstatistikler</h3>
                </div>
                <div class="card-body">
                  <div class="row g-3">
                    <div class="col-12">
                      <div class="text-muted small">Toplam Rol</div>
                      <div class="h4 mb-0">{{ filteredRoles().length }}</div>
                    </div>
                    <div class="col-12">
                      <div class="text-muted small">Görünen Yetki</div>
                      <div class="h4 mb-0">{{ visiblePermissionsCount() }}</div>
                    </div>
                    <div class="col-12">
                      <div class="text-muted small">Değişiklik</div>
                      <div class="h4 mb-0">{{ modifiedCount() }}</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Matrix -->
            <div class="col-9">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">
                    Yetki Matrisi
                    @if (modifiedCount() > 0) {
                      <span class="badge bg-warning ms-2">{{ modifiedCount() }} değişiklik</span>
                    }
                  </h3>

                  <div class="card-actions">
                    <!-- View Controls -->
                    <div class="btn-group me-2" role="group">
                      <button type="button"
                              class="btn btn-sm"
                              [class]="showSystemRoles() ? 'btn-info' : 'btn-outline-secondary'"
                              (click)="toggleSystemRoles()">
                        <lucide-icon name="eye" [size]="14" class="me-1"/>
                        Sistem Rolleri
                      </button>
                      <button type="button"
                              class="btn btn-sm"
                              [class]="compactView() ? 'btn-primary' : 'btn-outline-secondary'"
                              (click)="toggleCompactView()">
                        <lucide-icon name="minimize-2" [size]="14" class="me-1"/>
                        Kompakt
                      </button>
                    </div>

                    @if (modifiedCount() > 0) {
                      <div class="btn-group" role="group">
                        <button type="button" class="btn btn-sm btn-outline-secondary" (click)="resetChanges()">
                          <lucide-icon name="rotate-ccw" [size]="14" class="me-1"/>
                          Sıfırla
                        </button>
                        <button type="button" class="btn btn-sm btn-success" (click)="saveChanges()">
                          <lucide-icon name="save" [size]="14" class="me-1"/>
                          Kaydet
                        </button>
                      </div>
                    }
                  </div>
                </div>

                @if (loading()) {
                  <div class="card-body d-flex justify-content-center py-5">
                    <div class="spinner-border text-primary" role="status">
                      <span class="visually-hidden">Yükleniyor...</span>
                    </div>
                  </div>
                } @else {
                  <div class="table-responsive" style="max-height: 70vh; overflow-y: auto;">
                    <table class="table table-sm table-bordered matrix-table">
                      <thead class="table-light sticky-top">
                        <tr>
                          <th class="permission-header" style="min-width: 300px;">
                            <div class="d-flex align-items-center">
                              <span class="fw-bold">Yetkiler</span>
                              <button class="btn btn-sm ms-auto" (click)="expandAllGroups()">
                                <lucide-icon [name]="allGroupsExpanded() ? 'eye-off' : 'eye'" [size]="14"/>
                              </button>
                            </div>
                          </th>
                          @for (role of filteredRoles(); track role.id) {
                            <th class="text-center role-header" style="min-width: 100px;">
                              <div class="role-header-content">
                                @if (role.isSystemRole) {
                                  <lucide-icon name="star" [size]="12" class="text-warning me-1"/>
                                }
                                <div class="role-name" [title]="role.name">{{ role.name }}</div>
                                <div class="role-users text-muted">{{ role.userCount || 0 }} kullanıcı</div>
                              </div>
                            </th>
                          }
                        </tr>
                      </thead>
                      <tbody>
                        @for (group of permissionGroups(); track group.service) {
                          <!-- Service Group Header -->
                          <tr class="service-group-header">
                            <td class="fw-bold bg-light" (click)="toggleGroup(group.service)">
                              <div class="d-flex align-items-center cursor-pointer">
                                <lucide-icon [name]="group.expanded ? 'chevron-down' : 'chevron-right'" [size]="16" class="me-2"/>
                                <span>{{ group.service }}</span>
                                <span class="badge bg-secondary ms-2">{{ group.permissions.length }}</span>
                              </div>
                            </td>
                            @for (role of filteredRoles(); track role.id) {
                              <td class="text-center bg-light">
                                <span class="badge bg-info small">
                                  {{ getServicePermissionCount(group.service, role.id) }}/{{ group.permissions.length }}
                                </span>
                              </td>
                            }
                          </tr>

                          <!-- Permission Rows -->
                          @if (group.expanded) {
                            @for (permission of group.permissions; track permission.id) {
                              <tr class="permission-row">
                                <td class="permission-cell">
                                  <div class="permission-info">
                                    <div class="permission-name">{{ permission.name }}</div>
                                    @if (permission.description) {
                                      <div class="permission-description">{{ permission.description }}</div>
                                    }
                                  </div>
                                </td>
                                @for (role of filteredRoles(); track role.id) {
                                  <td class="text-center matrix-cell" (click)="togglePermission(role.id, permission.id)">
                                    <div class="permission-toggle"
                                         [class]="getPermissionCellClass(role.id, permission.id)"
                                         [title]="getPermissionCellTitle(role.id, permission.id)">
                                      {{ getPermissionCellSymbol(role.id, permission.id) }}
                                    </div>
                                  </td>
                                }
                              </tr>
                            }
                          }
                        }
                      </tbody>
                    </table>
                  </div>

                  @if (permissionGroups().length === 0) {
                    <div class="card-body text-center py-5">
                      <lucide-icon name="search" [size]="48" class="text-muted mb-3"/>
                      <h4 class="text-muted">Yetki bulunamadı</h4>
                      <p class="text-muted">Filtreleri ayarlayın veya arama kriterlerinizi değiştirin.</p>
                    </div>
                  }
                }
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Confirmation Modal -->
    <app-confirmation-modal
      [visible]="showConfirmModal()"
      [config]="confirmConfig"
      (result)="onConfirmResult($event)"/>
  `,
  styles: [`
    .matrix-table {
      font-size: 0.875rem;
    }

    .permission-header {
      background: var(--bs-light) !important;
      position: sticky;
      left: 0;
      z-index: 10;
    }

    .role-header {
      background: var(--bs-light) !important;
      writing-mode: vertical-rl;
      text-orientation: mixed;
      min-width: 80px !important;
      max-width: 80px !important;
    }

    .role-header-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
    }

    .role-name {
      font-weight: 600;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 60px;
    }

    .role-users {
      font-size: 0.75rem;
    }

    .service-group-header {
      background: var(--bs-gray-100) !important;
    }

    .permission-cell {
      position: sticky;
      left: 0;
      background: white;
      z-index: 5;
      border-right: 2px solid var(--bs-border-color);
    }

    .permission-info {
      padding: 8px;
    }

    .permission-name {
      font-weight: 500;
      color: var(--bs-dark);
    }

    .permission-description {
      font-size: 0.75rem;
      color: var(--bs-secondary);
      margin-top: 2px;
    }

    .matrix-cell {
      padding: 4px !important;
      cursor: pointer;
      transition: background-color 0.2s;
    }

    .matrix-cell:hover {
      background-color: var(--bs-gray-50) !important;
    }

    .permission-toggle {
      width: 32px;
      height: 32px;
      border-radius: 6px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
      font-size: 1.1rem;
      cursor: pointer;
      transition: all 0.2s;
      margin: 0 auto;
    }

    .permission-toggle.granted {
      background: var(--bs-success);
      color: white;
    }

    .permission-toggle.denied {
      background: var(--bs-light);
      color: var(--bs-secondary);
      border: 1px solid var(--bs-border-color);
    }

    .permission-toggle.inherited {
      background: var(--bs-warning);
      color: var(--bs-dark);
    }

    .permission-toggle.modified {
      box-shadow: 0 0 0 2px var(--bs-info);
    }

    .permission-toggle.system {
      background: var(--bs-info);
      color: white;
    }

    .cursor-pointer {
      cursor: pointer;
    }

    .table-responsive {
      border: 1px solid var(--bs-border-color);
      border-radius: 0.5rem;
    }

    .sticky-top {
      position: sticky;
      top: 0;
      z-index: 20;
    }

    /* Compact view styles */
    .compact .matrix-table {
      font-size: 0.75rem;
    }

    .compact .permission-toggle {
      width: 24px;
      height: 24px;
      font-size: 0.875rem;
    }

    .compact .role-header {
      min-width: 60px !important;
      max-width: 60px !important;
    }

    .compact .permission-info {
      padding: 4px 8px;
    }
  `]
})
export class PermissionMatrixComponent implements OnInit {
  // Services
  private roleService = inject(RoleService);
  private permissionService = inject(PermissionService);

  // Icons
  readonly grid3x3Icon = Grid3x3;
  readonly searchIcon = Search;
  readonly filterIcon = Filter;
  readonly downloadIcon = Download;
  readonly eyeIcon = Eye;
  readonly eyeOffIcon = EyeOff;
  readonly rotateCcwIcon = RotateCcw;
  readonly saveIcon = Save;
  readonly alertTriangleIcon = AlertTriangle;

  // State signals
  loading = signal(false);
  roles = signal<RoleDto[]>([]);
  permissions = signal<PermissionDto[]>([]);
  matrix = signal<Map<string, MatrixCell>>(new Map());

  // View state
  filters = signal<any>({});
  showSystemRoles = signal(true);
  compactView = signal(false);
  expandedGroups = signal<Set<string>>(new Set());

  // Modal state
  showConfirmModal = signal(false);
  confirmConfig = signal<any>({});

  // Computed values
  filteredRoles = computed(() => {
    const roles = this.roles();
    const showSystem = this.showSystemRoles();
    const searchTerm = this.filters().search?.toLowerCase() || '';

    return roles.filter(role => {
      if (!showSystem && role.isSystemRole) return false;
      if (searchTerm && !role.name.toLowerCase().includes(searchTerm)) return false;
      return true;
    });
  });

  permissionGroups = computed(() => {
    const permissions = this.permissions();
    const searchTerm = this.filters().search?.toLowerCase() || '';
    const expandedGroups = this.expandedGroups();

    // Group permissions by service
    const groups = new Map<string, PermissionDto[]>();

    permissions.forEach(permission => {
      if (searchTerm && !permission.name.toLowerCase().includes(searchTerm) &&
          !permission.service.toLowerCase().includes(searchTerm)) {
        return;
      }

      if (!groups.has(permission.service)) {
        groups.set(permission.service, []);
      }
      groups.get(permission.service)!.push(permission);
    });

    return Array.from(groups.entries()).map(([service, perms]) => ({
      service,
      permissions: perms.sort((a, b) => a.name.localeCompare(b.name)),
      expanded: expandedGroups.has(service)
    })).sort((a, b) => a.service.localeCompare(b.service));
  });

  visiblePermissionsCount = computed(() => {
    return this.permissionGroups().reduce((sum, group) =>
      group.expanded ? sum + group.permissions.length : sum, 0
    );
  });

  modifiedCount = computed(() => {
    const matrix = this.matrix();
    return Array.from(matrix.values()).filter(cell => cell.modified).length;
  });

  allGroupsExpanded = computed(() => {
    const groups = this.permissionGroups();
    return groups.length > 0 && groups.every(group => group.expanded);
  });

  filterFields = computed(() => [
    {
      key: 'service',
      label: 'Servis',
      type: 'select' as const,
      options: this.getUniqueServices().map(service => ({ label: service, value: service }))
    },
    {
      key: 'roleType',
      label: 'Rol Tipi',
      type: 'select' as const,
      options: [
        { label: 'Tüm Roller', value: '' },
        { label: 'Sistem Rolleri', value: 'system' },
        { label: 'Özel Roller', value: 'custom' }
      ]
    },
    {
      key: 'permissionStatus',
      label: 'Yetki Durumu',
      type: 'select' as const,
      options: [
        { label: 'Tüm Yetkiler', value: '' },
        { label: 'Verilen Yetkiler', value: 'granted' },
        { label: 'Verilmeyen Yetkiler', value: 'denied' },
        { label: 'Miras Yetkiler', value: 'inherited' }
      ]
    }
  ] as FilterField[]);

  // Header actions
  headerActions: ActionButton[] = [
    {
      key: 'export-matrix',
      label: 'Matrisi Dışa Aktar',
      icon: 'download',
      variant: 'outline-primary'
    },
    {
      key: 'bulk-assign',
      label: 'Toplu Atama',
      icon: 'users',
      variant: 'outline-info'
    },
    {
      key: 'role-templates',
      label: 'Rol Şablonları',
      icon: 'bookmark',
      variant: 'outline-secondary'
    }
  ];

  ngOnInit() {
    this.loadData();
    // Expand common service groups by default
    this.expandedGroups.set(new Set(['Identity', 'User', 'Admin']));
  }

  private loadData() {
    this.loading.set(true);

    const roles$ = this.roleService.getRoles({ includePermissions: true });
    const permissions$ = this.permissionService.getPermissions();

    roles$.subscribe({
      next: (rolesResponse) => {
        this.roles.set(rolesResponse?.data || []);

        permissions$.subscribe({
          next: (permissionsResponse) => {
            this.permissions.set(permissionsResponse?.data || []);
            this.buildMatrix();
            this.loading.set(false);
          },
          error: (error) => {
            console.error('Failed to load permissions data:', error);
            this.loading.set(false);
          }
        });
      },
      error: (error) => {
        console.error('Failed to load roles data:', error);
        this.loading.set(false);
      }
    });
  }

  private buildMatrix() {
    const matrix = new Map<string, MatrixCell>();
    const roles = this.roles();
    const permissions = this.permissions();

    roles.forEach(role => {
      permissions.forEach(permission => {
        const key = `${role.id}_${permission.id}`;
        const granted = role.permissions?.some(p => p.id === permission.id) || false;

        matrix.set(key, {
          roleId: role.id,
          permissionId: permission.id,
          granted,
          inherited: false, // TODO: Implement inheritance logic
          modified: false
        });
      });
    });

    this.matrix.set(matrix);
  }

  onFiltersChange(filters: any) {
    this.filters.set(filters);
  }

  onHeaderAction(event: any) {
    switch (event.action) {
      case 'export-matrix':
        this.exportMatrix();
        break;
      case 'bulk-assign':
        // Show bulk assignment dialog
        break;
      case 'role-templates':
        // Navigate to role templates
        break;
    }
  }

  toggleSystemRoles() {
    this.showSystemRoles.set(!this.showSystemRoles());
  }

  toggleCompactView() {
    this.compactView.set(!this.compactView());
  }

  toggleGroup(service: string) {
    const expanded = this.expandedGroups();
    const newExpanded = new Set(expanded);

    if (newExpanded.has(service)) {
      newExpanded.delete(service);
    } else {
      newExpanded.add(service);
    }

    this.expandedGroups.set(newExpanded);
  }

  expandAllGroups() {
    const allExpanded = this.allGroupsExpanded();
    if (allExpanded) {
      this.expandedGroups.set(new Set());
    } else {
      const allServices = this.permissionGroups().map(g => g.service);
      this.expandedGroups.set(new Set(allServices));
    }
  }

  togglePermission(roleId: string, permissionId: string) {
    const matrix = this.matrix();
    const key = `${roleId}_${permissionId}`;
    const cell = matrix.get(key);

    if (cell) {
      const newCell = {
        ...cell,
        granted: !cell.granted,
        modified: true
      };
      matrix.set(key, newCell);
      this.matrix.set(new Map(matrix));
    }
  }

  getPermissionCellClass(roleId: string, permissionId: string): string {
    const cell = this.getMatrixCell(roleId, permissionId);
    const role = this.roles().find(r => r.id === roleId);

    let classes = 'permission-toggle';

    if (cell.granted) {
      classes += ' granted';
    } else {
      classes += ' denied';
    }

    if (cell.inherited) {
      classes += ' inherited';
    }

    if (cell.modified) {
      classes += ' modified';
    }

    if (role?.isSystemRole) {
      classes += ' system';
    }

    return classes;
  }

  getPermissionCellSymbol(roleId: string, permissionId: string): string {
    const cell = this.getMatrixCell(roleId, permissionId);

    if (cell.inherited) return '◗';
    if (cell.granted) return '✓';
    return '−';
  }

  getPermissionCellTitle(roleId: string, permissionId: string): string {
    const cell = this.getMatrixCell(roleId, permissionId);
    const role = this.roles().find(r => r.id === roleId);
    const permission = this.permissions().find(p => p.id === permissionId);

    let title = `${role?.name} - ${permission?.name}`;

    if (cell.granted) {
      title += '\nDurum: Yetki Verildi';
    } else {
      title += '\nDurum: Yetki Verilmedi';
    }

    if (cell.inherited) {
      title += `\nMiras: ${cell.source}`;
    }

    if (cell.modified) {
      title += '\n⚠️ Değiştirildi (Kaydedilmedi)';
    }

    return title;
  }

  getServicePermissionCount(service: string, roleId: string): number {
    const servicePermissions = this.permissions().filter(p => p.service === service);
    return servicePermissions.filter(permission =>
      this.getMatrixCell(roleId, permission.id).granted
    ).length;
  }

  private getMatrixCell(roleId: string, permissionId: string): MatrixCell {
    const key = `${roleId}_${permissionId}`;
    return this.matrix().get(key) || {
      roleId,
      permissionId,
      granted: false,
      inherited: false,
      modified: false
    };
  }

  private getUniqueServices(): string[] {
    const services = this.permissions().map(p => p.service);
    return Array.from(new Set(services)).sort();
  }

  resetChanges() {
    this.confirmConfig.set({
      title: 'Değişiklikleri Sıfırla',
      message: 'Tüm değişiklikler kaybolacak. Devam etmek istediğinizden emin misiniz?',
      type: 'warning',
      confirmText: 'Sıfırla',
      cancelText: 'İptal'
    });

    this.showConfirmModal.set(true);
  }

  saveChanges() {
    const modifiedCells = Array.from(this.matrix().values()).filter(cell => cell.modified);

    this.confirmConfig.set({
      title: 'Değişiklikleri Kaydet',
      message: `${modifiedCells.length} değişiklik kaydedilecek. Devam etmek istediğinizden emin misiniz?`,
      type: 'info',
      confirmText: 'Kaydet',
      cancelText: 'İptal',
      details: modifiedCells.slice(0, 10).map(cell => {
        const role = this.roles().find(r => r.id === cell.roleId);
        const permission = this.permissions().find(p => p.id === cell.permissionId);
        return `${role?.name} → ${permission?.name}: ${cell.granted ? 'Verildi' : 'Kaldırıldı'}`;
      })
    });

    this.showConfirmModal.set(true);
  }

  onConfirmResult(result: any) {
    this.showConfirmModal.set(false);

    if (result.confirmed) {
      const config = this.confirmConfig();

      if (config.title === 'Değişiklikleri Sıfırla') {
        this.buildMatrix(); // Reset to original state
      } else if (config.title === 'Değişiklikleri Kaydet') {
        this.performSave();
      }
    }
  }

  private performSave() {
    const modifiedCells = Array.from(this.matrix().values()).filter(cell => cell.modified);

    // Group by role for batch updates
    const roleUpdates = new Map<string, { granted: string[], revoked: string[] }>();

    modifiedCells.forEach(cell => {
      if (!roleUpdates.has(cell.roleId)) {
        roleUpdates.set(cell.roleId, { granted: [], revoked: [] });
      }

      const update = roleUpdates.get(cell.roleId)!;
      if (cell.granted) {
        update.granted.push(cell.permissionId);
      } else {
        update.revoked.push(cell.permissionId);
      }
    });

    // Send batch updates - convert to expected format
    const updateRequests = Array.from(roleUpdates.entries());
    let completedUpdates = 0;

    if (updateRequests.length === 0) {
      this.loadData();
      return;
    }

    updateRequests.forEach(([roleId, updates]) => {
      this.roleService.updateRolePermissions(roleId, {
        addPermissions: updates.granted,
        removePermissions: updates.revoked
      }).subscribe({
        next: () => {
          completedUpdates++;
          if (completedUpdates === updateRequests.length) {
            // All updates completed, reload data
            this.loadData();
            console.log('Matrix changes saved successfully');
          }
        },
        error: (error) => {
          console.error('Failed to save matrix changes:', error);
        }
      });
    });
  }

  exportMatrix() {
    const exportData = {
      roles: this.filteredRoles().map(role => ({
        id: role.id,
        name: role.name,
        isSystemRole: role.isSystemRole
      })),
      permissions: this.permissions().map(permission => ({
        id: permission.id,
        name: permission.name,
        service: permission.service
      })),
      matrix: Array.from(this.matrix().values()).map(cell => ({
        roleId: cell.roleId,
        permissionId: cell.permissionId,
        granted: cell.granted,
        inherited: cell.inherited
      }))
    };

    this.permissionService.exportMatrix(exportData).subscribe({
      next: () => {
        console.log('Matrix export completed successfully');
      },
      error: (error) => {
        console.error('Export failed:', error);
      }
    });
  }
}