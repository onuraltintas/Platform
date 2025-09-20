import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';

import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  Permission,
  Role,
  User,
  RoleQuery,
  UserQuery,
  PagedResult,
  PermissionAuditLog
} from '../../models/user-management.models';

@Component({
  selector: 'app-permission-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="permission-detail-container" *ngIf="permission()">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <nav aria-label="breadcrumb">
              <ol class="breadcrumb">
                <li class="breadcrumb-item">
                  <a routerLink="/permissions" class="text-decoration-none">
                    <i class="fas fa-shield-alt me-1"></i>
                    İzin Yönetimi
                  </a>
                </li>
                <li class="breadcrumb-item active">{{ permission()?.displayName }}</li>
              </ol>
            </nav>
            <div class="d-flex align-items-center gap-3">
              <h1 class="page-title mb-0">
                <i class="fas fa-shield-alt me-3"></i>
                {{ permission()?.displayName }}
              </h1>
              <span class="badge bg-secondary">{{ permission()?.category }}</span>
            </div>
            <p class="page-description text-muted mt-2" *ngIf="permission()?.description">
              {{ permission()?.description }}
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
              <a routerLink="/permissions" class="btn btn-outline-primary">
                <i class="fas fa-arrow-left me-2"></i>
                Geri Dön
              </a>
            </div>
          </div>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-navigation mb-4">
        <ul class="nav nav-tabs" role="tablist">
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'overview'"
              (click)="setActiveTab('overview')"
              type="button"
            >
              <i class="fas fa-info-circle me-2"></i>
              Genel Bakış
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'roles'"
              (click)="setActiveTab('roles')"
              type="button"
            >
              <i class="fas fa-user-tag me-2"></i>
              Bu İzne Sahip Roller
              <span class="badge bg-primary ms-2">{{ permissionRoles().length }}</span>
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'users'"
              (click)="setActiveTab('users')"
              type="button"
            >
              <i class="fas fa-users me-2"></i>
              Bu İzne Sahip Kullanıcılar
              <span class="badge bg-primary ms-2">{{ permissionUsers().length }}</span>
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'activity'"
              (click)="setActiveTab('activity')"
              type="button"
            >
              <i class="fas fa-history me-2"></i>
              Aktivite
            </button>
          </li>
        </ul>
      </div>

      <!-- Tab Content -->
      <div class="tab-content">
        <!-- Overview Tab -->
        <div class="tab-pane" [class.active]="activeTab() === 'overview'">
          <div class="row">
            <div class="col-lg-8">
              <div class="card mb-4">
                <div class="card-header">
                  <h5 class="card-title mb-0">
                    <i class="fas fa-info-circle me-2"></i>
                    İzin Detayları
                  </h5>
                </div>
                <div class="card-body">
                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Görünen Ad:</label>
                        <span class="info-value">{{ permission()?.displayName }}</span>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Sistem Adı:</label>
                        <span class="info-value">{{ permission()?.name }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Kategori:</label>
                        <span class="badge bg-secondary">{{ permission()?.category }}</span>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Grup:</label>
                        <span class="info-value">{{ permission()?.group || 'Belirtilmemiş' }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="info-item">
                    <label class="info-label">Açıklama:</label>
                    <span class="info-value">{{ permission()?.description || 'Açıklama bulunmamaktadır' }}</span>
                  </div>
                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Kaynak:</label>
                        <span class="info-value">{{ permission()?.resource || 'Belirtilmemiş' }}</span>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">İşlem:</label>
                        <span class="info-value">{{ permission()?.action || 'Belirtilmemiş' }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Oluşturulma Tarihi:</label>
                        <span class="info-value">{{ permission()?.createdAt | date:'dd/MM/yyyy HH:mm' }}</span>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Son Güncelleme:</label>
                        <span class="info-value">{{ permission()?.updatedAt | date:'dd/MM/yyyy HH:mm' }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Permission Hierarchy -->
              <div class="card" *ngIf="permission()?.parentId || getChildPermissions().length > 0">
                <div class="card-header">
                  <h5 class="card-title mb-0">
                    <i class="fas fa-sitemap me-2"></i>
                    İzin Hiyerarşisi
                  </h5>
                </div>
                <div class="card-body">
                  <!-- Parent Permission -->
                  <div class="hierarchy-item parent" *ngIf="permission()?.parentId">
                    <div class="hierarchy-icon">
                      <i class="fas fa-level-up-alt"></i>
                    </div>
                    <div class="hierarchy-content">
                      <h6>Üst İzin</h6>
                      <p class="text-muted">Bu izin başka bir iznin alt kümesidir</p>
                      <a [routerLink]="['/permissions', permission()?.parentId]" class="btn btn-outline-primary btn-sm">
                        <i class="fas fa-external-link-alt me-1"></i>
                        Üst İzni Görüntüle
                      </a>
                    </div>
                  </div>

                  <!-- Child Permissions -->
                  <div class="hierarchy-item children" *ngIf="getChildPermissions().length > 0">
                    <div class="hierarchy-icon">
                      <i class="fas fa-level-down-alt"></i>
                    </div>
                    <div class="hierarchy-content">
                      <h6>Alt İzinler</h6>
                      <p class="text-muted">Bu iznin {{ getChildPermissions().length }} alt izni vardır</p>
                      <div class="child-permissions">
                        <div
                          class="child-permission-item"
                          *ngFor="let child of getChildPermissions()"
                        >
                          <span class="child-name">{{ child.displayName }}</span>
                          <a [routerLink]="['/permissions', child.id]" class="btn btn-outline-primary btn-sm ms-2">
                            <i class="fas fa-eye"></i>
                          </a>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="col-lg-4">
              <div class="card">
                <div class="card-header">
                  <h5 class="card-title mb-0">
                    <i class="fas fa-chart-bar me-2"></i>
                    İstatistikler
                  </h5>
                </div>
                <div class="card-body">
                  <div class="stat-item">
                    <div class="stat-icon bg-primary">
                      <i class="fas fa-user-tag"></i>
                    </div>
                    <div class="stat-content">
                      <div class="stat-number">{{ permissionRoles().length }}</div>
                      <div class="stat-label">Bu İzne Sahip Rol</div>
                    </div>
                  </div>
                  <div class="stat-item">
                    <div class="stat-icon bg-success">
                      <i class="fas fa-users"></i>
                    </div>
                    <div class="stat-content">
                      <div class="stat-number">{{ permissionUsers().length }}</div>
                      <div class="stat-label">Bu İzne Sahip Kullanıcı</div>
                    </div>
                  </div>
                  <div class="stat-item">
                    <div class="stat-icon bg-info">
                      <i class="fas fa-calendar"></i>
                    </div>
                    <div class="stat-content">
                      <div class="stat-number">{{ getDaysOld() }}</div>
                      <div class="stat-label">Gün Önce Oluşturuldu</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Roles Tab -->
        <div class="tab-pane" [class.active]="activeTab() === 'roles'">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-user-tag me-2"></i>
                Bu İzne Sahip Roller
              </h5>
            </div>
            <div class="card-body">
              <div class="roles-container" *ngIf="permissionRoles().length > 0">
                <div class="table-responsive">
                  <table class="table table-hover">
                    <thead>
                      <tr>
                        <th>Rol Adı</th>
                        <th>Açıklama</th>
                        <th>Durum</th>
                        <th>Kullanıcı Sayısı</th>
                        <th>İşlemler</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let role of permissionRoles()">
                        <td>
                          <div>
                            <div class="fw-medium">{{ role.name }}</div>
                            <small class="text-muted" *ngIf="role.isDefault">
                              <i class="fas fa-star me-1"></i>
                              Varsayılan Rol
                            </small>
                          </div>
                        </td>
                        <td>{{ role.description || '-' }}</td>
                        <td>
                          <span class="badge" [class]="role.isActive ? 'bg-success' : 'bg-secondary'">
                            {{ role.isActive ? 'Aktif' : 'Pasif' }}
                          </span>
                        </td>
                        <td>{{ role.userCount || 0 }}</td>
                        <td>
                          <div class="d-flex gap-1">
                            <a [routerLink]="['/roles', role.id]" class="btn btn-outline-primary btn-sm">
                              <i class="fas fa-eye"></i>
                            </a>
                            <a [routerLink]="['/roles', role.id, 'edit']" class="btn btn-outline-warning btn-sm">
                              <i class="fas fa-edit"></i>
                            </a>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
              <div class="text-center py-5" *ngIf="permissionRoles().length === 0">
                <i class="fas fa-user-tag fa-3x text-muted mb-3"></i>
                <h6 class="text-muted">Bu izne henüz hiçbir rol sahip değil</h6>
              </div>
            </div>
          </div>
        </div>

        <!-- Users Tab -->
        <div class="tab-pane" [class.active]="activeTab() === 'users'">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-users me-2"></i>
                Bu İzne Sahip Kullanıcılar
              </h5>
            </div>
            <div class="card-body">
              <div class="users-container" *ngIf="permissionUsers().length > 0">
                <div class="table-responsive">
                  <table class="table table-hover">
                    <thead>
                      <tr>
                        <th>Kullanıcı</th>
                        <th>E-posta</th>
                        <th>Rol</th>
                        <th>Durum</th>
                        <th>İşlemler</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let user of permissionUsers()">
                        <td>
                          <div class="d-flex align-items-center">
                            <div class="user-avatar me-2">
                              {{ getUserInitials(user) }}
                            </div>
                            <div>
                              <div class="fw-medium">{{ user.firstName }} {{ user.lastName }}</div>
                              <small class="text-muted">{{ user.userName }}</small>
                            </div>
                          </div>
                        </td>
                        <td>{{ user.email }}</td>
                        <td>
                          <div class="roles-list">
                            <span
                              class="badge bg-secondary me-1 mb-1"
                              *ngFor="let role of user.roles"
                            >
                              {{ role }}
                            </span>
                          </div>
                        </td>
                        <td>
                          <span class="badge" [class]="user.isActive ? 'bg-success' : 'bg-secondary'">
                            {{ user.isActive ? 'Aktif' : 'Pasif' }}
                          </span>
                        </td>
                        <td>
                          <div class="d-flex gap-1">
                            <a [routerLink]="['/users', user.id]" class="btn btn-outline-primary btn-sm">
                              <i class="fas fa-eye"></i>
                            </a>
                            <a [routerLink]="['/users', user.id, 'edit']" class="btn btn-outline-warning btn-sm">
                              <i class="fas fa-edit"></i>
                            </a>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
              <div class="text-center py-5" *ngIf="permissionUsers().length === 0">
                <i class="fas fa-users fa-3x text-muted mb-3"></i>
                <h6 class="text-muted">Bu izne henüz hiçbir kullanıcı sahip değil</h6>
              </div>
            </div>
          </div>
        </div>

        <!-- Activity Tab -->
        <div class="tab-pane" [class.active]="activeTab() === 'activity'">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-history me-2"></i>
                İzin Aktiviteleri
              </h5>
            </div>
            <div class="card-body">
              <div class="activity-container" *ngIf="permissionActivities().length > 0">
                <div class="timeline">
                  <div
                    class="timeline-item"
                    *ngFor="let activity of permissionActivities()"
                  >
                    <div class="timeline-marker" [class]="getActivityMarkerClass(activity.action)">
                      <i [class]="getActivityIcon(activity.action)"></i>
                    </div>
                    <div class="timeline-content">
                      <div class="timeline-header">
                        <h6 class="timeline-title">{{ getActivityTitle(activity.action) }}</h6>
                        <small class="timeline-date">{{ activity.timestamp | date:'dd/MM/yyyy HH:mm' }}</small>
                      </div>
                      <p class="timeline-description">{{ activity.description }}</p>
                      <small class="text-muted">
                        <i class="fas fa-user me-1"></i>
                        {{ activity.performedBy }}
                      </small>
                    </div>
                  </div>
                </div>
              </div>
              <div class="text-center py-5" *ngIf="permissionActivities().length === 0">
                <i class="fas fa-history fa-3x text-muted mb-3"></i>
                <h6 class="text-muted">Henüz aktivite bulunmamaktadır</h6>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div class="text-center py-5" *ngIf="loading()">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Yükleniyor...</span>
      </div>
      <p class="mt-3 text-muted">İzin bilgileri yükleniyor...</p>
    </div>
  `,
  styles: [`
    .permission-detail-container {
      padding: 1.5rem;
    }

    .page-header {
      margin-bottom: 2rem;
    }

    .page-title {
      font-size: 2rem;
      font-weight: 600;
      color: var(--bs-body-color);
    }

    .tab-navigation {
      border-bottom: 1px solid var(--bs-border-color);
    }

    .tab-content {
      padding-top: 1.5rem;
    }

    .tab-pane {
      display: none;
    }

    .tab-pane.active {
      display: block;
    }

    .info-item {
      margin-bottom: 1rem;
    }

    .info-label {
      font-weight: 600;
      color: var(--bs-nav-link-color);
      display: block;
      margin-bottom: 0.25rem;
    }

    .info-value {
      color: var(--bs-body-color);
    }

    .stat-item {
      display: flex;
      align-items: center;
      margin-bottom: 1.5rem;
    }

    .stat-icon {
      width: 50px;
      height: 50px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      margin-right: 1rem;
    }

    .stat-content {
      flex: 1;
    }

    .stat-number {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--bs-body-color);
      line-height: 1;
    }

    .stat-label {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color);
    }

    .hierarchy-item {
      display: flex;
      align-items: flex-start;
      margin-bottom: 2rem;
    }

    .hierarchy-icon {
      width: 50px;
      height: 50px;
      border-radius: 50%;
      background: var(--bs-primary);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-right: 1rem;
      flex-shrink: 0;
    }

    .hierarchy-content h6 {
      margin-bottom: 0.5rem;
      color: var(--bs-body-color);
    }

    .child-permissions {
      margin-top: 1rem;
    }

    .child-permission-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      background: var(--bs-gray-50);
      border: 1px solid var(--bs-border-color);
      border-radius: var(--border-radius);
      padding: 0.75rem;
      margin-bottom: 0.5rem;
    }

    .child-name {
      font-weight: 500;
    }

    .user-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: var(--bs-primary);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: 0.875rem;
    }

    .roles-list {
      max-width: 200px;
    }

    .timeline {
      position: relative;
      padding-left: 2rem;
    }

    .timeline::before {
      content: '';
      position: absolute;
      left: 20px;
      top: 0;
      bottom: 0;
      width: 2px;
      background: var(--bs-border-color);
    }

    .timeline-item {
      position: relative;
      margin-bottom: 2rem;
    }

    .timeline-marker {
      position: absolute;
      left: -2rem;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      z-index: 1;
    }

    .timeline-marker.bg-primary { background: var(--bs-primary); }
    .timeline-marker.bg-success { background: var(--bs-success); }
    .timeline-marker.bg-warning { background: var(--bs-warning); }
    .timeline-marker.bg-danger { background: var(--bs-danger); }

    .timeline-content {
      background: var(--bs-card-bg);
      border: 1px solid var(--bs-border-color);
      border-radius: var(--border-radius);
      padding: 1rem;
    }

    .timeline-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }

    .timeline-title {
      font-size: 1rem;
      font-weight: 600;
      margin: 0;
    }

    .timeline-date {
      color: var(--bs-nav-link-color);
    }

    .timeline-description {
      margin-bottom: 0.5rem;
      color: var(--bs-body-color);
    }
  `]
})
export class PermissionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly permissionService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  permission = signal<Permission | null>(null);
  permissionRoles = signal<Role[]>([]);
  permissionUsers = signal<User[]>([]);
  permissionActivities = signal<PermissionAuditLog[]>([]);
  childPermissions = signal<Permission[]>([]);
  loading = signal<boolean>(false);
  activeTab = signal<string>('overview');

  private permissionId = computed(() => this.route.snapshot.params['id']);

  ngOnInit(): void {
    this.loadPermissionDetail();
  }

  loadPermissionDetail(): void {
    const id = this.permissionId();
    if (!id) return;

    this.loading.set(true);

    this.permissionService.getPermissionById(id).subscribe({
      next: (permission) => {
        this.permission.set(permission);
        this.loadPermissionRoles(id);
        this.loadPermissionUsers(id);
        this.loadPermissionActivities(id);
        this.loadChildPermissions(id);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading permission:', error);
        this.notificationService.error('İzin bilgileri yüklenirken bir hata oluştu', 'Hata');
        this.loading.set(false);
        this.router.navigate(['/permissions']);
      }
    });
  }

  loadPermissionRoles(permissionId: string): void {
    const query: RoleQuery = {
      permissionId: permissionId,
      page: 1,
      pageSize: 50
    };

    this.permissionService.getRoles(query).subscribe({
      next: (result) => {
        this.permissionRoles.set(result.data);
      },
      error: (error) => {
        console.error('Error loading permission roles:', error);
      }
    });
  }

  loadPermissionUsers(permissionId: string): void {
    const query: UserQuery = {
      permissionId: permissionId,
      page: 1,
      pageSize: 50
    };

    this.permissionService.getUsers(query).subscribe({
      next: (result) => {
        this.permissionUsers.set(result.data);
      },
      error: (error) => {
        console.error('Error loading permission users:', error);
      }
    });
  }

  loadPermissionActivities(permissionId: string): void {
    this.permissionService.getPermissionAuditLogs(permissionId).subscribe({
      next: (activities) => {
        this.permissionActivities.set(activities);
      },
      error: (error) => {
        console.error('Error loading permission activities:', error);
      }
    });
  }

  loadChildPermissions(permissionId: string): void {
    this.permissionService.getChildPermissions(permissionId).subscribe({
      next: (children) => {
        this.childPermissions.set(children);
      },
      error: (error) => {
        console.error('Error loading child permissions:', error);
      }
    });
  }

  refreshData(): void {
    this.loadPermissionDetail();
  }

  setActiveTab(tab: string): void {
    this.activeTab.set(tab);
  }

  getChildPermissions(): Permission[] {
    return this.childPermissions();
  }

  getDaysOld(): number {
    const permission = this.permission();
    if (!permission?.createdAt) return 0;

    const created = new Date(permission.createdAt);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - created.getTime());
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  getUserInitials(user: User): string {
    const first = user.firstName?.charAt(0) || '';
    const last = user.lastName?.charAt(0) || '';
    return (first + last).toUpperCase() || user.userName?.charAt(0).toUpperCase() || '?';
  }

  getActivityIcon(action: string): string {
    switch (action.toLowerCase()) {
      case 'create': return 'fas fa-plus';
      case 'update': return 'fas fa-edit';
      case 'delete': return 'fas fa-trash';
      case 'assign': return 'fas fa-user-plus';
      case 'revoke': return 'fas fa-user-minus';
      default: return 'fas fa-history';
    }
  }

  getActivityMarkerClass(action: string): string {
    switch (action.toLowerCase()) {
      case 'create': return 'bg-success';
      case 'update': return 'bg-warning';
      case 'delete': return 'bg-danger';
      case 'assign': return 'bg-primary';
      case 'revoke': return 'bg-secondary';
      default: return 'bg-info';
    }
  }

  getActivityTitle(action: string): string {
    switch (action.toLowerCase()) {
      case 'create': return 'İzin Oluşturuldu';
      case 'update': return 'İzin Güncellendi';
      case 'delete': return 'İzin Silindi';
      case 'assign': return 'Role/Kullanıcıya Atandı';
      case 'revoke': return 'Rol/Kullanıcıdan Kaldırıldı';
      default: return 'Aktivite';
    }
  }
}