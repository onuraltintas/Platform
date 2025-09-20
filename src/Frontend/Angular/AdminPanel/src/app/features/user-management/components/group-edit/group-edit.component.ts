import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  Group,
  UpdateGroupRequest,
  User,
  Role,
  UserQuery,
  RoleQuery
} from '../../models/user-management.models';

@Component({
  selector: 'app-group-edit',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  template: `
    <div class="group-edit-container" *ngIf="group()">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <nav aria-label="breadcrumb">
              <ol class="breadcrumb">
                <li class="breadcrumb-item">
                  <a routerLink="/groups" class="text-decoration-none">
                    <i class="fas fa-users me-1"></i>
                    Grup Yönetimi
                  </a>
                </li>
                <li class="breadcrumb-item">
                  <a [routerLink]="['/groups', group()?.id]" class="text-decoration-none">
                    {{ group()?.name }}
                  </a>
                </li>
                <li class="breadcrumb-item active">Düzenle</li>
              </ol>
            </nav>
            <h1 class="page-title">
              <i class="fas fa-edit me-3"></i>
              {{ group()?.name }} - Düzenle
            </h1>
            <p class="page-description text-muted">
              Grup bilgilerini ve üyelerini güncelleyin
            </p>
          </div>
          <div class="col-auto">
            <div class="d-flex gap-2">
              <a [routerLink]="['/groups', group()?.id]" class="btn btn-outline-secondary">
                <i class="fas fa-arrow-left me-2"></i>
                Geri Dön
              </a>
            </div>
          </div>
        </div>
      </div>

      <!-- Form Content -->
      <div class="form-content">
        <form [formGroup]="groupForm" (ngSubmit)="onSubmit()" novalidate>
          <div class="row">
            <!-- Basic Information -->
            <div class="col-lg-8">
              <div class="card mb-4">
                <div class="card-header">
                  <h5 class="card-title mb-0">
                    <i class="fas fa-info-circle me-2"></i>
                    Temel Bilgiler
                  </h5>
                </div>
                <div class="card-body">
                  <div class="form-group mb-3">
                    <label for="name" class="form-label required">Grup Adı</label>
                    <input
                      type="text"
                      id="name"
                      class="form-control"
                      formControlName="name"
                      placeholder="Grup adını giriniz"
                      [class.is-invalid]="isFieldInvalid('name')"
                    />
                    <div class="invalid-feedback" *ngIf="isFieldInvalid('name')">
                      <div *ngIf="groupForm.get('name')?.errors?.['required']">
                        Grup adı zorunludur
                      </div>
                      <div *ngIf="groupForm.get('name')?.errors?.['minlength']">
                        Grup adı en az 2 karakter olmalıdır
                      </div>
                      <div *ngIf="groupForm.get('name')?.errors?.['maxlength']">
                        Grup adı en fazla 100 karakter olabilir
                      </div>
                    </div>
                  </div>

                  <div class="form-group mb-3">
                    <label for="description" class="form-label">Açıklama</label>
                    <textarea
                      id="description"
                      class="form-control"
                      formControlName="description"
                      rows="3"
                      placeholder="Grup açıklamasını giriniz"
                      [class.is-invalid]="isFieldInvalid('description')"
                    ></textarea>
                    <div class="invalid-feedback" *ngIf="isFieldInvalid('description')">
                      <div *ngIf="groupForm.get('description')?.errors?.['maxlength']">
                        Açıklama en fazla 500 karakter olabilir
                      </div>
                    </div>
                  </div>

                  <div class="form-check">
                    <input
                      type="checkbox"
                      id="isActive"
                      class="form-check-input"
                      formControlName="isActive"
                    />
                    <label for="isActive" class="form-check-label">
                      Aktif
                    </label>
                    <div class="form-text">
                      Aktif olmayan gruplara kullanıcı atanamaz
                    </div>
                  </div>
                </div>
              </div>

              <!-- Users Selection -->
              <div class="card mb-4">
                <div class="card-header">
                  <div class="d-flex justify-content-between align-items-center">
                    <h5 class="card-title mb-0">
                      <i class="fas fa-users me-2"></i>
                      Kullanıcılar
                    </h5>
                    <div class="d-flex gap-2">
                      <button
                        type="button"
                        class="btn btn-outline-primary btn-sm"
                        (click)="selectAllUsers()"
                      >
                        <i class="fas fa-check-double me-1"></i>
                        Tümünü Seç
                      </button>
                      <button
                        type="button"
                        class="btn btn-outline-secondary btn-sm"
                        (click)="clearAllUsers()"
                      >
                        <i class="fas fa-times me-1"></i>
                        Tümünü Temizle
                      </button>
                    </div>
                  </div>
                </div>
                <div class="card-body">
                  <div class="users-container" *ngIf="availableUsers().length > 0">
                    <div class="users-grid">
                      <div
                        class="form-check user-item"
                        *ngFor="let user of availableUsers()"
                      >
                        <input
                          type="checkbox"
                          [id]="'user-' + user.id"
                          class="form-check-input"
                          [value]="user.id"
                          (change)="onUserChange(user.id, $event)"
                          [checked]="selectedUsers().has(user.id)"
                        />
                        <label [for]="'user-' + user.id" class="form-check-label">
                          <div class="user-info">
                            <div class="user-avatar">
                              {{ getUserInitials(user) }}
                            </div>
                            <div class="user-details">
                              <div class="user-name">{{ user.firstName }} {{ user.lastName }}</div>
                              <div class="user-email">{{ user.email }}</div>
                              <div class="user-roles">
                                <span
                                  class="badge bg-secondary me-1"
                                  *ngFor="let role of user.roles"
                                >
                                  {{ role.name }}
                                </span>
                              </div>
                            </div>
                          </div>
                        </label>
                      </div>
                    </div>
                  </div>
                  <div class="text-center py-4" *ngIf="availableUsers().length === 0">
                    <i class="fas fa-users fa-3x text-muted mb-3"></i>
                    <p class="text-muted">Kullanıcılar yükleniyor...</p>
                  </div>
                </div>
              </div>

              <!-- Roles Selection -->
              <div class="card">
                <div class="card-header">
                  <div class="d-flex justify-content-between align-items-center">
                    <h5 class="card-title mb-0">
                      <i class="fas fa-user-tag me-2"></i>
                      Roller
                    </h5>
                    <div class="d-flex gap-2">
                      <button
                        type="button"
                        class="btn btn-outline-primary btn-sm"
                        (click)="selectAllRoles()"
                      >
                        <i class="fas fa-check-double me-1"></i>
                        Tümünü Seç
                      </button>
                      <button
                        type="button"
                        class="btn btn-outline-secondary btn-sm"
                        (click)="clearAllRoles()"
                      >
                        <i class="fas fa-times me-1"></i>
                        Tümünü Temizle
                      </button>
                    </div>
                  </div>
                </div>
                <div class="card-body">
                  <div class="roles-container" *ngIf="availableRoles().length > 0">
                    <div class="roles-grid">
                      <div
                        class="form-check role-item"
                        *ngFor="let role of availableRoles()"
                      >
                        <input
                          type="checkbox"
                          [id]="'role-' + role.id"
                          class="form-check-input"
                          [value]="role.id"
                          (change)="onRoleChange(role.id, $event)"
                          [checked]="selectedRoles().has(role.id)"
                        />
                        <label [for]="'role-' + role.id" class="form-check-label">
                          <div class="role-info">
                            <div class="role-header">
                              <div class="role-name">{{ role.name }}</div>
                              <div class="role-badges">
                                <span class="badge bg-success" *ngIf="role.isActive">Aktif</span>
                                <span class="badge bg-info" *ngIf="role.isDefault">Varsayılan</span>
                              </div>
                            </div>
                            <div class="role-description">{{ role.description || 'Açıklama yok' }}</div>
                            <div class="role-meta">
                              <small class="text-muted">
                                <i class="fas fa-users me-1"></i>
                                {{ role.userCount || 0 }} kullanıcı
                              </small>
                            </div>
                          </div>
                        </label>
                      </div>
                    </div>
                  </div>
                  <div class="text-center py-4" *ngIf="availableRoles().length === 0">
                    <i class="fas fa-user-tag fa-3x text-muted mb-3"></i>
                    <p class="text-muted">Roller yükleniyor...</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Side Panel -->
            <div class="col-lg-4">
              <div class="card sticky-top">
                <div class="card-header">
                  <h5 class="card-title mb-0">
                    <i class="fas fa-cog me-2"></i>
                    İşlemler
                  </h5>
                </div>
                <div class="card-body">
                  <div class="d-grid gap-2">
                    <button
                      type="submit"
                      class="btn btn-primary"
                      [disabled]="groupForm.invalid || loading() || !hasChanges()"
                    >
                      <i class="fas fa-save me-2" *ngIf="!loading()"></i>
                      <i class="fas fa-spinner fa-spin me-2" *ngIf="loading()"></i>
                      {{ loading() ? 'Güncelleniyor...' : 'Değişiklikleri Kaydet' }}
                    </button>
                    <button
                      type="button"
                      class="btn btn-outline-warning"
                      (click)="resetForm()"
                      [disabled]="loading()"
                    >
                      <i class="fas fa-undo me-2"></i>
                      Sıfırla
                    </button>
                    <a [routerLink]="['/groups', group()?.id]" class="btn btn-outline-secondary">
                      <i class="fas fa-times me-2"></i>
                      İptal
                    </a>
                  </div>

                  <hr class="my-3">

                  <!-- Group Summary -->
                  <div class="group-summary">
                    <h6 class="mb-3">
                      <i class="fas fa-info-circle me-2"></i>
                      Grup Özeti
                    </h6>
                    <div class="summary-item">
                      <span class="label">Seçilen Kullanıcı Sayısı:</span>
                      <span class="value">{{ selectedUsers().size }}</span>
                    </div>
                    <div class="summary-item">
                      <span class="label">Değişen Kullanıcı Sayısı:</span>
                      <span class="value">{{ getChangedUsersCount() }}</span>
                    </div>
                    <div class="summary-item">
                      <span class="label">Seçilen Rol Sayısı:</span>
                      <span class="value">{{ selectedRoles().size }}</span>
                    </div>
                    <div class="summary-item">
                      <span class="label">Değişen Rol Sayısı:</span>
                      <span class="value">{{ getChangedRolesCount() }}</span>
                    </div>
                    <div class="summary-item">
                      <span class="label">Durum:</span>
                      <span class="value">
                        <span class="badge" [class]="groupForm.get('isActive')?.value ? 'bg-success' : 'bg-secondary'">
                          {{ groupForm.get('isActive')?.value ? 'Aktif' : 'Pasif' }}
                        </span>
                      </span>
                    </div>
                  </div>

                  <div class="alert alert-info mt-3" *ngIf="!hasChanges()">
                    <i class="fas fa-info-circle me-2"></i>
                    <small>Henüz değişiklik yapılmamış</small>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </form>
      </div>
    </div>

    <!-- Loading State -->
    <div class="text-center py-5" *ngIf="!group() && loadingGroup()">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Yükleniyor...</span>
      </div>
      <p class="mt-3 text-muted">Grup bilgileri yükleniyor...</p>
    </div>
  `,
  styles: [`
    .group-edit-container {
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

    .required::after {
      content: '*';
      color: var(--bs-danger);
      margin-left: 0.25rem;
    }

    .sticky-top {
      top: 1rem;
    }

    .users-grid, .roles-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
      gap: 1rem;
    }

    .user-item, .role-item {
      background: white;
      border: 1px solid var(--bs-border-color);
      border-radius: var(--border-radius);
      padding: 1rem;
      transition: all var(--transition-normal);
    }

    .user-item:hover, .role-item:hover {
      border-color: var(--bs-primary);
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .user-item .form-check-input:checked ~ .form-check-label,
    .role-item .form-check-input:checked ~ .form-check-label {
      color: var(--bs-primary);
    }

    .user-info {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
    }

    .user-avatar {
      width: 50px;
      height: 50px;
      border-radius: 50%;
      background: var(--bs-primary);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: 1.1rem;
      flex-shrink: 0;
    }

    .user-details {
      flex: 1;
      min-width: 0;
    }

    .user-name {
      font-weight: 600;
      color: var(--bs-body-color);
      margin-bottom: 0.25rem;
    }

    .user-email {
      color: var(--bs-nav-link-color);
      font-size: 0.875rem;
      margin-bottom: 0.5rem;
    }

    .user-roles {
      display: flex;
      flex-wrap: wrap;
      gap: 0.25rem;
    }

    .role-info {
      width: 100%;
    }

    .role-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 0.5rem;
    }

    .role-name {
      font-weight: 600;
      color: var(--bs-body-color);
      font-size: 1.1rem;
    }

    .role-badges {
      display: flex;
      gap: 0.25rem;
    }

    .role-description {
      color: var(--bs-nav-link-color);
      font-size: 0.875rem;
      margin-bottom: 0.5rem;
      line-height: 1.4;
    }

    .role-meta {
      border-top: 1px solid var(--bs-border-color);
      padding-top: 0.5rem;
    }

    .group-summary {
      font-size: 0.875rem;
    }

    .summary-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }

    .summary-item .label {
      color: var(--bs-nav-link-color);
    }

    .summary-item .value {
      font-weight: 500;
    }

    @media (max-width: 768px) {
      .users-grid, .roles-grid {
        grid-template-columns: 1fr;
      }

      .sticky-top {
        position: static;
      }

      .user-info {
        flex-direction: column;
        text-align: center;
      }

      .user-avatar {
        margin: 0 auto;
      }
    }
  `]
})
export class GroupEditComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly groupService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  group = signal<Group | null>(null);
  groupForm: FormGroup;
  loading = signal<boolean>(false);
  loadingGroup = signal<boolean>(false);
  availableUsers = signal<User[]>([]);
  availableRoles = signal<Role[]>([]);
  selectedUsers = signal<Set<string>>(new Set());
  selectedRoles = signal<Set<string>>(new Set());
  originalUsers = signal<Set<string>>(new Set());
  originalRoles = signal<Set<string>>(new Set());
  originalFormValue: any = null;

  private groupId = computed(() => this.route.snapshot.params['id']);

  constructor() {
    this.groupForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.loadGroupDetail();
    this.loadUsers();
    this.loadRoles();
  }

  loadGroupDetail(): void {
    const id = this.groupId();
    if (!id) return;

    this.loadingGroup.set(true);

    this.groupService.getGroupById(id).subscribe({
      next: (group) => {
        this.group.set(group);
        this.populateForm(group);
        this.loadingGroup.set(false);
      },
      error: (error) => {
        console.error('Error loading group:', error);
        this.notificationService.error('Grup bilgileri yüklenirken bir hata oluştu', 'Hata');
        this.loadingGroup.set(false);
        this.router.navigate(['/groups']);
      }
    });
  }

  loadUsers(): void {
    const query: UserQuery = {
      page: 1,
      pageSize: 1000,
      isActive: true
    };

    this.groupService.getUsers(query).subscribe({
      next: (result) => {
        this.availableUsers.set(result.data);
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.notificationService.error('Kullanıcılar yüklenirken bir hata oluştu', 'Hata');
      }
    });
  }

  loadRoles(): void {
    const query: RoleQuery = {
      page: 1,
      pageSize: 1000,
      isActive: true
    };

    this.groupService.getRoles(query).subscribe({
      next: (result) => {
        this.availableRoles.set(result.data);
      },
      error: (error) => {
        console.error('Error loading roles:', error);
        this.notificationService.error('Roller yüklenirken bir hata oluştu', 'Hata');
      }
    });
  }

  populateForm(group: Group): void {
    this.groupForm.patchValue({
      name: group.name,
      description: group.description,
      isActive: group.isActive
    });

    this.originalFormValue = this.groupForm.value;

    const userIds = new Set(group.users?.map(u => u.id) || []);
    const roleIds = new Set(group.roles?.map(r => r.id) || []);

    this.selectedUsers.set(userIds);
    this.selectedRoles.set(roleIds);
    this.originalUsers.set(new Set(userIds));
    this.originalRoles.set(new Set(roleIds));
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.groupForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onUserChange(userId: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const users = new Set(this.selectedUsers());

    if (target.checked) {
      users.add(userId);
    } else {
      users.delete(userId);
    }

    this.selectedUsers.set(users);
  }

  onRoleChange(roleId: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const roles = new Set(this.selectedRoles());

    if (target.checked) {
      roles.add(roleId);
    } else {
      roles.delete(roleId);
    }

    this.selectedRoles.set(roles);
  }

  selectAllUsers(): void {
    const allUserIds = new Set(this.availableUsers().map(u => u.id));
    this.selectedUsers.set(allUserIds);
  }

  clearAllUsers(): void {
    this.selectedUsers.set(new Set());
  }

  selectAllRoles(): void {
    const allRoleIds = new Set(this.availableRoles().map(r => r.id));
    this.selectedRoles.set(allRoleIds);
  }

  clearAllRoles(): void {
    this.selectedRoles.set(new Set());
  }

  hasChanges(): boolean {
    if (!this.originalFormValue) return false;

    const currentFormValue = this.groupForm.value;
    const formChanged = JSON.stringify(currentFormValue) !== JSON.stringify(this.originalFormValue);

    const currentUsers = this.selectedUsers();
    const originalUsers = this.originalUsers();
    const usersChanged = currentUsers.size !== originalUsers.size ||
      [...currentUsers].some(id => !originalUsers.has(id));

    const currentRoles = this.selectedRoles();
    const originalRoles = this.originalRoles();
    const rolesChanged = currentRoles.size !== originalRoles.size ||
      [...currentRoles].some(id => !originalRoles.has(id));

    return formChanged || usersChanged || rolesChanged;
  }

  getChangedUsersCount(): number {
    const current = this.selectedUsers();
    const original = this.originalUsers();

    const added = [...current].filter(id => !original.has(id));
    const removed = [...original].filter(id => !current.has(id));

    return added.length + removed.length;
  }

  getChangedRolesCount(): number {
    const current = this.selectedRoles();
    const original = this.originalRoles();

    const added = [...current].filter(id => !original.has(id));
    const removed = [...original].filter(id => !current.has(id));

    return added.length + removed.length;
  }

  resetForm(): void {
    if (this.originalFormValue) {
      this.groupForm.patchValue(this.originalFormValue);
      this.selectedUsers.set(new Set(this.originalUsers()));
      this.selectedRoles.set(new Set(this.originalRoles()));
    }
  }

  getUserInitials(user: User): string {
    const first = user.firstName?.charAt(0) || '';
    const last = user.lastName?.charAt(0) || '';
    return (first + last).toUpperCase() || user.userName?.charAt(0).toUpperCase() || '?';
  }

  onSubmit(): void {
    if (this.groupForm.valid && this.hasChanges()) {
      this.loading.set(true);

      const formValue = this.groupForm.value;
      const group = this.group();
      if (!group) return;

      const request: UpdateGroupRequest = {
        id: group.id,
        name: formValue.name,
        description: formValue.description,
        isActive: formValue.isActive,
        userIds: Array.from(this.selectedUsers()),
        roleIds: Array.from(this.selectedRoles())
      };

      this.groupService.updateGroup(request).subscribe({
        next: (updatedGroup) => {
          this.notificationService.success('Grup başarıyla güncellendi', 'İşlem Başarılı');
          this.group.set(updatedGroup);
          this.originalFormValue = this.groupForm.value;
          this.originalUsers.set(new Set(this.selectedUsers()));
          this.originalRoles.set(new Set(this.selectedRoles()));
          this.loading.set(false);
        },
        error: (error) => {
          console.error('Update group error:', error);
          this.notificationService.error('Grup güncellenirken bir hata oluştu', 'Hata');
          this.loading.set(false);
        }
      });
    } else if (!this.hasChanges()) {
      this.notificationService.info('Değişiklik yapılmamış', 'Bilgi');
    } else {
      Object.keys(this.groupForm.controls).forEach(key => {
        this.groupForm.get(key)?.markAsTouched();
      });
    }
  }
}