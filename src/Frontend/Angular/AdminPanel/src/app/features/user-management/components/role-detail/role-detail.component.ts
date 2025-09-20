import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';

import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  Role,
  Permission,
  User,
  UserQuery,
  PagedResult,
  RoleAuditLog
} from '../../models/user-management.models';

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="role-detail-container" *ngIf="role()">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <nav aria-label="breadcrumb">
              <ol class="breadcrumb">
                <li class="breadcrumb-item">
                  <a routerLink="/roles" class="text-decoration-none">
                    <i class="fas fa-user-tag me-1"></i>
                    Rol Yönetimi
                  </a>
                </li>
                <li class="breadcrumb-item active">{{ role()?.name }}</li>
              </ol>
            </nav>
            <div class="d-flex align-items-center gap-3">
              <h1 class="page-title mb-0">
                <i class="fas fa-user-tag me-3"></i>
                {{ role()?.name }}
              </h1>
              <span class="badge" [class]="role()?.isActive ? 'bg-success' : 'bg-secondary'">
                {{ role()?.isActive ? 'Aktif' : 'Pasif' }}
              </span>
              <span class="badge bg-info" *ngIf="role()?.isDefault">
                Varsayılan
              </span>
            </div>
            <p class="page-description text-muted mt-2" *ngIf="role()?.description">
              {{ role()?.description }}
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
              <a [routerLink]="['/roles', role()?.id, 'edit']" class="btn btn-warning">
                <i class="fas fa-edit me-2"></i>
                Düzenle
              </a>
              <button
                type="button"
                class="btn btn-danger"
                (click)="deleteRole()"
                [disabled]="role()?.isDefault"
              >
                <i class="fas fa-trash me-2"></i>
                Sil
              </button>
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
              [class.active]="activeTab() === 'permissions'"
              (click)="setActiveTab('permissions')"
              type="button"
            >
              <i class="fas fa-shield-alt me-2"></i>
              İzinler
              <span class="badge bg-primary ms-2">{{ rolePermissions().length }}</span>
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
              Kullanıcılar
              <span class="badge bg-primary ms-2">{{ roleUsers().length }}</span>
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
                    Rol Bilgileri
                  </h5>
                </div>
                <div class="card-body">
                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Rol Adı:</label>
                        <span class="info-value">{{ role()?.name }}</span>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Normalleştirilmiş Ad:</label>
                        <span class="info-value">{{ role()?.normalizedName }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="info-item">
                    <label class="info-label">Açıklama:</label>
                    <span class="info-value">{{ role()?.description || 'Açıklama bulunmamaktadır' }}</span>
                  </div>
                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Durum:</label>
                        <span class="badge" [class]="role()?.isActive ? 'bg-success' : 'bg-secondary'">
                          {{ role()?.isActive ? 'Aktif' : 'Pasif' }}
                        </span>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Varsayılan Rol:</label>
                        <span class="badge" [class]="role()?.isDefault ? 'bg-info' : 'bg-secondary'">
                          {{ role()?.isDefault ? 'Evet' : 'Hayır' }}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Oluşturulma Tarihi:</label>
                        <span class="info-value">{{ role()?.createdAt | date:'dd/MM/yyyy HH:mm' }}</span>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-item">
                        <label class="info-label">Son Güncelleme:</label>
                        <span class="info-value">{{ role()?.updatedAt | date:'dd/MM/yyyy HH:mm' }}</span>
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
                      <i class="fas fa-shield-alt"></i>
                    </div>
                    <div class="stat-content">
                      <div class="stat-number">{{ rolePermissions().length }}</div>
                      <div class="stat-label">İzin Sayısı</div>
                    </div>
                  </div>
                  <div class="stat-item">
                    <div class="stat-icon bg-success">
                      <i class="fas fa-users"></i>
                    </div>
                    <div class="stat-content">
                      <div class="stat-number">{{ roleUsers().length }}</div>
                      <div class="stat-label">Kullanıcı Sayısı</div>
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

        <!-- Permissions Tab -->
        <div class="tab-pane" [class.active]="activeTab() === 'permissions'">
          <div class="card">
            <div class="card-header">
              <div class="d-flex justify-content-between align-items-center">
                <h5 class="card-title mb-0">
                  <i class="fas fa-shield-alt me-2"></i>
                  Rol İzinleri
                </h5>
                <a [routerLink]="['/roles', role()?.id, 'edit']" class="btn btn-primary btn-sm">
                  <i class="fas fa-edit me-2"></i>
                  İzinleri Düzenle
                </a>
              </div>
            </div>
            <div class="card-body">
              <div class="permissions-container" *ngIf="rolePermissions().length > 0">
                <div class="row">
                  <div
                    class="col-lg-6 mb-3"
                    *ngFor="let permission of rolePermissions()"
                  >
                    <div class="permission-card">
                      <div class="permission-header">
                        <h6 class="permission-name">{{ permission.displayName }}</h6>
                        <span class="badge bg-secondary">{{ permission.category }}</span>
                      </div>
                      <p class="permission-description">{{ permission.description }}</p>
                      <div class="permission-meta">
                        <small class="text-muted">
                          <i class="fas fa-tag me-1"></i>
                          {{ permission.name }}
                        </small>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div class="text-center py-5" *ngIf="rolePermissions().length === 0">
                <i class="fas fa-shield-alt fa-3x text-muted mb-3"></i>
                <h6 class="text-muted">Bu role henüz hiçbir izin atanmamış</h6>
                <a [routerLink]="['/roles', role()?.id, 'edit']" class="btn btn-primary mt-2">
                  <i class="fas fa-plus me-2"></i>
                  İzin Ekle
                </a>
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
                Bu Role Sahip Kullanıcılar
              </h5>
            </div>
            <div class="card-body">
              <div class="users-container" *ngIf="roleUsers().length > 0">
                <div class="table-responsive">
                  <table class="table table-hover">
                    <thead>
                      <tr>
                        <th>Kullanıcı</th>
                        <th>E-posta</th>
                        <th>Durum</th>
                        <th>Kayıt Tarihi</th>
                        <th>İşlemler</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let user of roleUsers()">
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
                          <span class="badge" [class]="user.isActive ? 'bg-success' : 'bg-secondary'">
                            {{ user.isActive ? 'Aktif' : 'Pasif' }}
                          </span>
                        </td>
                        <td>{{ user.createdAt | date:'dd/MM/yyyy' }}</td>
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
              <div class="text-center py-5" *ngIf="roleUsers().length === 0">
                <i class="fas fa-users fa-3x text-muted mb-3"></i>
                <h6 class="text-muted">Bu role henüz hiçbir kullanıcı atanmamış</h6>
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
                Rol Aktiviteleri
              </h5>
            </div>
            <div class="card-body">
              <div class="activity-container" *ngIf="roleActivities().length > 0">
                <div class="timeline">
                  <div
                    class="timeline-item"
                    *ngFor="let activity of roleActivities()"
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
              <div class="text-center py-5" *ngIf="roleActivities().length === 0">
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
      <p class="mt-3 text-muted">Rol bilgileri yükleniyor...</p>
    </div>
  `,
  styles: [`
    .role-detail-container {
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

    .permission-card {
      border: 1px solid var(--bs-border-color);
      border-radius: var(--border-radius);
      padding: 1rem;
      height: 100%;
      transition: all var(--transition-normal);
    }

    .permission-card:hover {
      border-color: var(--bs-primary);
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .permission-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 0.5rem;
    }

    .permission-name {
      font-size: 0.95rem;
      font-weight: 600;
      margin: 0;
      color: var(--bs-body-color);
    }

    .permission-description {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color);
      margin-bottom: 0.5rem;
    }

    .permission-meta {
      border-top: 1px solid var(--bs-border-color);
      padding-top: 0.5rem;
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
export class RoleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly roleService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  role = signal<Role | null>(null);
  rolePermissions = signal<Permission[]>([]);
  roleUsers = signal<User[]>([]);
  roleActivities = signal<RoleAuditLog[]>([]);
  loading = signal<boolean>(false);
  activeTab = signal<string>('overview');

  private roleId = computed(() => this.route.snapshot.params['id']);

  ngOnInit(): void {
    this.loadRoleDetail();
  }

  loadRoleDetail(): void {
    const id = this.roleId();
    if (!id) return;

    this.loading.set(true);

    this.roleService.getRoleById(id).subscribe({
      next: (role) => {
        this.role.set(role);
        this.loadRolePermissions(id);
        this.loadRoleUsers(id);
        this.loadRoleActivities(id);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading role:', error);
        this.notificationService.error('Rol bilgileri yüklenirken bir hata oluştu', 'Hata');
        this.loading.set(false);
        this.router.navigate(['/roles']);
      }
    });
  }

  loadRolePermissions(roleId: string): void {
    this.roleService.getRolePermissions(roleId).subscribe({
      next: (permissions) => {
        this.rolePermissions.set(permissions);
      },
      error: (error) => {
        console.error('Error loading role permissions:', error);
      }
    });
  }

  loadRoleUsers(roleId: string): void {
    const query: UserQuery = {
      roleId: roleId,
      page: 1,
      pageSize: 50
    };

    this.roleService.getUsers(query).subscribe({
      next: (result) => {
        this.roleUsers.set(result.data);
      },
      error: (error) => {
        console.error('Error loading role users:', error);
      }
    });
  }

  loadRoleActivities(roleId: string): void {
    this.roleService.getRoleAuditLogs(roleId).subscribe({
      next: (activities) => {
        this.roleActivities.set(activities);
      },
      error: (error) => {
        console.error('Error loading role activities:', error);
      }
    });
  }

  refreshData(): void {
    this.loadRoleDetail();
  }

  setActiveTab(tab: string): void {
    this.activeTab.set(tab);
  }

  deleteRole(): void {
    const role = this.role();
    if (!role) return;

    if (role.isDefault) {
      this.notificationService.warning('Varsayılan roller silinemez', 'Uyarı');
      return;
    }

    if (confirm(`${role.name} rolünü silmek istediğinizden emin misiniz?`)) {
      this.roleService.deleteRole(role.id).subscribe({
        next: () => {
          this.notificationService.success('Rol başarıyla silindi', 'İşlem Başarılı');
          this.router.navigate(['/roles']);
        },
        error: (error) => {
          console.error('Delete role error:', error);
          this.notificationService.error('Rol silinirken bir hata oluştu', 'Hata');
        }
      });
    }
  }

  getDaysOld(): number {
    const role = this.role();
    if (!role?.createdAt) return 0;

    const created = new Date(role.createdAt);
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
      case 'create': return 'Rol Oluşturuldu';
      case 'update': return 'Rol Güncellendi';
      case 'delete': return 'Rol Silindi';
      case 'assign': return 'Kullanıcıya Atandı';
      case 'revoke': return 'Kullanıcıdan Kaldırıldı';
      default: return 'Aktivite';
    }
  }
}