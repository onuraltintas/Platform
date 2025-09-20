import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { User } from '../../models/user-management.models';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="user-detail-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <nav aria-label="breadcrumb">
              <ol class="breadcrumb">
                <li class="breadcrumb-item">
                  <a routerLink="/users">Kullanıcılar</a>
                </li>
                <li class="breadcrumb-item active">
                  {{ user()?.firstName }} {{ user()?.lastName }}
                </li>
              </ol>
            </nav>
            <h1 class="page-title">
              <i class="fas fa-user me-3"></i>
              Kullanıcı Detayı
            </h1>
          </div>
          <div class="col-auto">
            <div class="d-flex gap-2">
              <a routerLink="/users" class="btn btn-outline-secondary">
                <i class="fas fa-arrow-left me-2"></i>
                Geri Dön
              </a>
              <a
                [routerLink]="['/users', userId(), 'edit']"
                class="btn btn-primary"
                *ngIf="user()"
              >
                <i class="fas fa-edit me-2"></i>
                Düzenle
              </a>
            </div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div class="text-center py-5" *ngIf="loading()">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Yükleniyor...</span>
        </div>
        <div class="mt-3 text-muted">Kullanıcı bilgileri yükleniyor...</div>
      </div>

      <!-- User Not Found -->
      <div class="alert alert-warning" *ngIf="!loading() && !user()">
        <h5 class="alert-heading">
          <i class="fas fa-exclamation-triangle me-2"></i>
          Kullanıcı Bulunamadı
        </h5>
        <p class="mb-0">
          Aradığınız kullanıcı bulunamadı veya erişim yetkiniz bulunmuyor.
        </p>
      </div>

      <!-- User Details -->
      <div class="row" *ngIf="!loading() && user()">
        <!-- Main Info -->
        <div class="col-lg-4">
          <div class="card user-profile-card">
            <div class="card-body text-center">
              <div class="profile-avatar mb-3">
                <img
                  [src]="getUserAvatar()"
                  [alt]="getFullName()"
                  class="rounded-circle"
                >
              </div>
              <h4 class="mb-1">{{ getFullName() }}</h4>
              <p class="text-muted mb-3">{{ user()?.email }}</p>

              <div class="status-badges mb-3">
                <span [class]="'badge ' + (user()?.isActive ? 'bg-success' : 'bg-danger')">
                  {{ user()?.isActive ? 'Aktif' : 'Pasif' }}
                </span>
                <span [class]="'badge ' + (user()?.emailConfirmed ? 'bg-info' : 'bg-warning')">
                  {{ user()?.emailConfirmed ? 'E-posta Doğrulanmış' : 'E-posta Doğrulanmamış' }}
                </span>
              </div>

              <div class="user-stats">
                <div class="row text-center">
                  <div class="col-4">
                    <div class="stat-value">{{ user()?.roles?.length || 0 }}</div>
                    <div class="stat-label">Rol</div>
                  </div>
                  <div class="col-4">
                    <div class="stat-value">{{ user()?.groups?.length || 0 }}</div>
                    <div class="stat-label">Grup</div>
                  </div>
                  <div class="col-4">
                    <div class="stat-value">{{ getDaysSinceCreated() }}</div>
                    <div class="stat-label">Gün</div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Quick Actions -->
          <div class="card mt-3">
            <div class="card-header">
              <h6 class="card-title mb-0">
                <i class="fas fa-bolt me-2"></i>
                Hızlı İşlemler
              </h6>
            </div>
            <div class="card-body">
              <div class="d-grid gap-2">
                <button
                  type="button"
                  class="btn btn-outline-primary btn-sm"
                  (click)="resetPassword()"
                >
                  <i class="fas fa-key me-2"></i>
                  Şifre Sıfırla
                </button>
                <button
                  type="button"
                  class="btn btn-outline-info btn-sm"
                  (click)="sendVerificationEmail()"
                  *ngIf="!user()?.emailConfirmed"
                >
                  <i class="fas fa-envelope me-2"></i>
                  Doğrulama E-postası Gönder
                </button>
                <button
                  type="button"
                  [class]="'btn btn-outline-' + (user()?.isActive ? 'warning' : 'success') + ' btn-sm'"
                  (click)="toggleUserStatus()"
                >
                  <i [class]="'fas ' + (user()?.isActive ? 'fa-ban' : 'fa-check') + ' me-2'"></i>
                  {{ user()?.isActive ? 'Deaktive Et' : 'Aktifleştir' }}
                </button>
                <button
                  type="button"
                  class="btn btn-outline-danger btn-sm"
                  (click)="deleteUser()"
                >
                  <i class="fas fa-trash me-2"></i>
                  Kullanıcıyı Sil
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Details Tabs -->
        <div class="col-lg-8">
          <!-- Tab Navigation -->
          <ul class="nav nav-tabs" role="tablist">
            <li class="nav-item" role="presentation">
              <button
                class="nav-link active"
                data-bs-toggle="tab"
                data-bs-target="#info-tab"
                type="button"
                role="tab"
              >
                <i class="fas fa-info-circle me-2"></i>
                Genel Bilgiler
              </button>
            </li>
            <li class="nav-item" role="presentation">
              <button
                class="nav-link"
                data-bs-toggle="tab"
                data-bs-target="#roles-tab"
                type="button"
                role="tab"
              >
                <i class="fas fa-user-tag me-2"></i>
                Roller ve İzinler
              </button>
            </li>
            <li class="nav-item" role="presentation">
              <button
                class="nav-link"
                data-bs-toggle="tab"
                data-bs-target="#activity-tab"
                type="button"
                role="tab"
              >
                <i class="fas fa-history me-2"></i>
                Aktivite
              </button>
            </li>
          </ul>

          <!-- Tab Content -->
          <div class="tab-content">
            <!-- General Info Tab -->
            <div class="tab-pane fade show active" id="info-tab" role="tabpanel">
              <div class="card border-0">
                <div class="card-body">
                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Ad</label>
                        <div class="info-value">{{ user()?.firstName }}</div>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Soyad</label>
                        <div class="info-value">{{ user()?.lastName }}</div>
                      </div>
                    </div>
                  </div>

                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">E-posta</label>
                        <div class="info-value">{{ user()?.email }}</div>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Kullanıcı Adı</label>
                        <div class="info-value">{{ user()?.userName }}</div>
                      </div>
                    </div>
                  </div>

                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Telefon</label>
                        <div class="info-value">{{ user()?.phoneNumber || 'Belirtilmemiş' }}</div>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Telefon Doğrulandı</label>
                        <div class="info-value">
                          <i [class]="'fas ' + (user()?.phoneNumberConfirmed ? 'fa-check text-success' : 'fa-times text-danger')"></i>
                          {{ user()?.phoneNumberConfirmed ? 'Evet' : 'Hayır' }}
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">İki Faktörlü Kimlik Doğrulama</label>
                        <div class="info-value">
                          <i [class]="'fas ' + (user()?.twoFactorEnabled ? 'fa-check text-success' : 'fa-times text-danger')"></i>
                          {{ user()?.twoFactorEnabled ? 'Aktif' : 'Pasif' }}
                        </div>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Başarısız Giriş Sayısı</label>
                        <div class="info-value">{{ user()?.accessFailedCount || 0 }}</div>
                      </div>
                    </div>
                  </div>

                  <div class="row">
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Oluşturulma Tarihi</label>
                        <div class="info-value">{{ user()?.createdAt | date:'dd.MM.yyyy HH:mm' }}</div>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Son Güncellenme</label>
                        <div class="info-value">{{ user()?.updatedAt | date:'dd.MM.yyyy HH:mm' }}</div>
                      </div>
                    </div>
                  </div>

                  <div class="row" *ngIf="user()?.lastLoginAt">
                    <div class="col-md-6">
                      <div class="info-group">
                        <label class="info-label">Son Giriş</label>
                        <div class="info-value">{{ user()?.lastLoginAt | date:'dd.MM.yyyy HH:mm' }}</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Roles Tab -->
            <div class="tab-pane fade" id="roles-tab" role="tabpanel">
              <div class="card border-0">
                <div class="card-body">
                  <!-- Roles -->
                  <div class="section">
                    <h6 class="section-title">Atanmış Roller</h6>
                    <div class="role-list" *ngIf="user()?.roles && user()?.roles.length > 0; else noRoles">
                      <div
                        class="role-item"
                        *ngFor="let role of user()?.roles"
                      >
                        <div class="role-info">
                          <div class="role-name">{{ role.name }}</div>
                          <div class="role-description" *ngIf="role.description">
                            {{ role.description }}
                          </div>
                        </div>
                        <div class="role-status">
                          <span [class]="'badge ' + (role.isActive ? 'bg-success' : 'bg-secondary')">
                            {{ role.isActive ? 'Aktif' : 'Pasif' }}
                          </span>
                        </div>
                      </div>
                    </div>
                    <ng-template #noRoles>
                      <div class="text-muted text-center py-3">
                        <i class="fas fa-user-tag fa-2x mb-2"></i>
                        <div>Atanmış rol bulunmuyor</div>
                      </div>
                    </ng-template>
                  </div>

                  <!-- Groups -->
                  <div class="section">
                    <h6 class="section-title">Üye Olduğu Gruplar</h6>
                    <div class="group-list" *ngIf="user()?.groups && user()?.groups.length > 0; else noGroups">
                      <div
                        class="group-item"
                        *ngFor="let group of user()?.groups"
                      >
                        <div class="group-info">
                          <div class="group-name">{{ group.name }}</div>
                          <div class="group-description" *ngIf="group.description">
                            {{ group.description }}
                          </div>
                        </div>
                        <div class="group-status">
                          <span [class]="'badge ' + (group.isActive ? 'bg-success' : 'bg-secondary')">
                            {{ group.isActive ? 'Aktif' : 'Pasif' }}
                          </span>
                        </div>
                      </div>
                    </div>
                    <ng-template #noGroups>
                      <div class="text-muted text-center py-3">
                        <i class="fas fa-users fa-2x mb-2"></i>
                        <div>Üye olunan grup bulunmuyor</div>
                      </div>
                    </ng-template>
                  </div>
                </div>
              </div>
            </div>

            <!-- Activity Tab -->
            <div class="tab-pane fade" id="activity-tab" role="tabpanel">
              <div class="card border-0">
                <div class="card-body">
                  <div class="text-muted text-center py-5">
                    <i class="fas fa-chart-line fa-2x mb-2"></i>
                    <div>Aktivite geçmişi yakında eklenecek</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .user-detail-container {
      padding: 1.5rem;
    }

    .page-header {
      margin-bottom: 2rem;
    }

    .page-title {
      font-size: 2rem;
      font-weight: 600;
      color: var(--bs-body-color);
      margin-bottom: 0;
    }

    .breadcrumb {
      margin-bottom: 1rem;
    }

    .user-profile-card {
      border: none;
      box-shadow: var(--shadow-sm);
      border-radius: var(--border-radius-md);
    }

    .profile-avatar img {
      width: 100px;
      height: 100px;
      object-fit: cover;
      border: 3px solid var(--bs-border-color);
    }

    .status-badges .badge {
      margin: 0 0.25rem;
      font-size: 0.75rem;
    }

    .user-stats {
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--bs-border-color);
    }

    .stat-value {
      font-size: 1.5rem;
      font-weight: 600;
      color: var(--bs-primary);
    }

    .stat-label {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color);
    }

    .nav-tabs {
      border-bottom: 2px solid var(--bs-border-color);
    }

    .nav-tabs .nav-link {
      border: none;
      border-bottom: 2px solid transparent;
      color: var(--bs-nav-link-color);
      font-weight: 500;
    }

    .nav-tabs .nav-link.active {
      background: none;
      border-bottom-color: var(--bs-primary);
      color: var(--bs-primary);
    }

    .tab-content {
      margin-top: 1rem;
    }

    .info-group {
      margin-bottom: 1.5rem;
    }

    .info-label {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color);
      font-weight: 500;
      margin-bottom: 0.25rem;
      display: block;
    }

    .info-value {
      font-size: 1rem;
      color: var(--bs-body-color);
      font-weight: 500;
    }

    .section {
      margin-bottom: 2rem;
    }

    .section-title {
      color: var(--bs-primary);
      font-weight: 600;
      margin-bottom: 1rem;
      padding-bottom: 0.5rem;
      border-bottom: 1px solid var(--bs-border-color);
    }

    .role-item,
    .group-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem;
      margin-bottom: 0.5rem;
      background: var(--bs-content-bg);
      border-radius: var(--border-radius-md);
      border: 1px solid var(--bs-border-color);
    }

    .role-name,
    .group-name {
      font-weight: 600;
      color: var(--bs-body-color);
    }

    .role-description,
    .group-description {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color);
      margin-top: 0.25rem;
    }

    .card {
      border: none;
      box-shadow: var(--shadow-sm);
      border-radius: var(--border-radius-md);
    }

    .card-header {
      background: var(--bs-content-bg);
      border-bottom: 1px solid var(--bs-border-color);
    }

    /* Responsive */
    @media (max-width: 768px) {
      .user-detail-container {
        padding: 1rem;
      }

      .page-header .row {
        flex-direction: column;
        gap: 1rem;
      }

      .page-header .col-auto {
        width: 100%;
      }

      .user-stats .row > div {
        margin-bottom: 1rem;
      }
    }
  `]
})
export class UserDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  user = signal<User | null>(null);
  loading = signal<boolean>(false);

  userId = computed(() => {
    return this.route.snapshot.paramMap.get('id') || '';
  });

  ngOnInit(): void {
    this.loadUser();
  }

  loadUser(): void {
    const id = this.userId();
    if (!id) {
      this.router.navigate(['/users']);
      return;
    }

    this.loading.set(true);

    this.userService.getUser(id).subscribe({
      next: (user) => {
        this.user.set(user);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading user:', error);
        this.notificationService.error(
          'Kullanıcı bilgileri yüklenirken bir hata oluştu',
          'Hata'
        );
        this.loading.set(false);
      }
    });
  }

  getFullName(): string {
    const user = this.user();
    if (!user) return '';
    return `${user.firstName} ${user.lastName}`;
  }

  getUserAvatar(): string {
    const user = this.user();
    if (!user) return '';

    if (user.profilePicture) {
      return user.profilePicture;
    }

    const initials = `${user.firstName?.charAt(0) || ''}${user.lastName?.charAt(0) || ''}`;
    return `https://ui-avatars.com/api/?name=${initials}&background=0d6efd&color=fff&size=100`;
  }

  getDaysSinceCreated(): number {
    const user = this.user();
    if (!user?.createdAt) return 0;

    const createdDate = new Date(user.createdAt);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - createdDate.getTime());
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  resetPassword(): void {
    const user = this.user();
    if (!user) return;

    if (confirm(`${user.firstName} ${user.lastName} kullanıcısının şifresini sıfırlamak istediğinizden emin misiniz?`)) {
      this.userService.resetPassword({ userId: user.id, sendEmail: true }).subscribe({
        next: () => {
          this.notificationService.success(
            'Şifre sıfırlama e-postası gönderildi',
            'İşlem Başarılı'
          );
        },
        error: (error) => {
          console.error('Reset password error:', error);
          this.notificationService.error(
            'Şifre sıfırlanırken bir hata oluştu',
            'Hata'
          );
        }
      });
    }
  }

  sendVerificationEmail(): void {
    const user = this.user();
    if (!user) return;

    this.userService.resendEmailConfirmation(user.id).subscribe({
      next: () => {
        this.notificationService.success(
          'Doğrulama e-postası gönderildi',
          'İşlem Başarılı'
        );
      },
      error: (error) => {
        console.error('Send verification email error:', error);
        this.notificationService.error(
          'Doğrulama e-postası gönderilirken bir hata oluştu',
          'Hata'
        );
      }
    });
  }

  toggleUserStatus(): void {
    const user = this.user();
    if (!user) return;

    const action = user.isActive ? 'deaktive' : 'aktifleştir';
    if (confirm(`${user.firstName} ${user.lastName} kullanıcısını ${action} etmek istediğinizden emin misiniz?`)) {
      this.userService.updateUser({
        id: user.id,
        isActive: !user.isActive
      }).subscribe({
        next: (updatedUser) => {
          this.user.set(updatedUser);
          this.notificationService.success(
            `Kullanıcı başarıyla ${action} edildi`,
            'İşlem Başarılı'
          );
        },
        error: (error) => {
          console.error('Toggle user status error:', error);
          this.notificationService.error(
            'Kullanıcı durumu değiştirilirken bir hata oluştu',
            'Hata'
          );
        }
      });
    }
  }

  deleteUser(): void {
    const user = this.user();
    if (!user) return;

    if (confirm(`${user.firstName} ${user.lastName} kullanıcısını kalıcı olarak silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.`)) {
      this.userService.deleteUser(user.id).subscribe({
        next: () => {
          this.notificationService.success(
            'Kullanıcı başarıyla silindi',
            'İşlem Başarılı'
          );
          this.router.navigate(['/users']);
        },
        error: (error) => {
          console.error('Delete user error:', error);
          this.notificationService.error(
            'Kullanıcı silinirken bir hata oluştu',
            'Hata'
          );
        }
      });
    }
  }
}