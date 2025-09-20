import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  Role,
  UpdateRoleRequest,
  Permission,
  PermissionCategory
} from '../../models/user-management.models';

@Component({
  selector: 'app-role-edit',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  template: `
    <div class="role-edit-container" *ngIf="role()">
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
                <li class="breadcrumb-item">
                  <a [routerLink]="['/roles', role()?.id]" class="text-decoration-none">
                    {{ role()?.name }}
                  </a>
                </li>
                <li class="breadcrumb-item active">Düzenle</li>
              </ol>
            </nav>
            <h1 class="page-title">
              <i class="fas fa-edit me-3"></i>
              {{ role()?.name }} - Düzenle
            </h1>
            <p class="page-description text-muted">
              Rol bilgilerini ve izinlerini güncelleyin
            </p>
          </div>
          <div class="col-auto">
            <div class="d-flex gap-2">
              <a [routerLink]="['/roles', role()?.id]" class="btn btn-outline-secondary">
                <i class="fas fa-arrow-left me-2"></i>
                Geri Dön
              </a>
            </div>
          </div>
        </div>
      </div>

      <!-- Form Content -->
      <div class="form-content">
        <form [formGroup]="roleForm" (ngSubmit)="onSubmit()" novalidate>
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
                  <div class="row">
                    <div class="col-md-6">
                      <div class="form-group mb-3">
                        <label for="name" class="form-label required">Rol Adı</label>
                        <input
                          type="text"
                          id="name"
                          class="form-control"
                          formControlName="name"
                          placeholder="Rol adını giriniz"
                          [class.is-invalid]="isFieldInvalid('name')"
                          [readonly]="role()?.isDefault"
                        />
                        <div class="invalid-feedback" *ngIf="isFieldInvalid('name')">
                          <div *ngIf="roleForm.get('name')?.errors?.['required']">
                            Rol adı zorunludur
                          </div>
                          <div *ngIf="roleForm.get('name')?.errors?.['minlength']">
                            Rol adı en az 2 karakter olmalıdır
                          </div>
                          <div *ngIf="roleForm.get('name')?.errors?.['maxlength']">
                            Rol adı en fazla 100 karakter olabilir
                          </div>
                        </div>
                        <div class="form-text" *ngIf="role()?.isDefault">
                          Varsayılan rollerin adı değiştirilemez
                        </div>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="form-group mb-3">
                        <label for="normalizedName" class="form-label">Normalleştirilmiş Ad</label>
                        <input
                          type="text"
                          id="normalizedName"
                          class="form-control"
                          formControlName="normalizedName"
                          placeholder="Otomatik oluşturulur"
                          readonly
                        />
                        <div class="form-text">
                          Rol adından otomatik olarak oluşturulur
                        </div>
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
                      placeholder="Rol açıklamasını giriniz"
                      [class.is-invalid]="isFieldInvalid('description')"
                    ></textarea>
                    <div class="invalid-feedback" *ngIf="isFieldInvalid('description')">
                      <div *ngIf="roleForm.get('description')?.errors?.['maxlength']">
                        Açıklama en fazla 500 karakter olabilir
                      </div>
                    </div>
                  </div>

                  <div class="row">
                    <div class="col-md-6">
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
                          Aktif olmayan roller kullanıcılara atanamaz
                        </div>
                      </div>
                    </div>
                    <div class="col-md-6">
                      <div class="form-check">
                        <input
                          type="checkbox"
                          id="isDefault"
                          class="form-check-input"
                          formControlName="isDefault"
                          [disabled]="!!role()?.isDefault"
                        />
                        <label for="isDefault" class="form-check-label">
                          Varsayılan Rol
                        </label>
                        <div class="form-text">
                          {{ role()?.isDefault ? 'Varsayılan roller değiştirilemez' : 'Yeni kullanıcılara otomatik atanır' }}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Permissions -->
              <div class="card">
                <div class="card-header">
                  <div class="d-flex justify-content-between align-items-center">
                    <h5 class="card-title mb-0">
                      <i class="fas fa-shield-alt me-2"></i>
                      İzinler
                    </h5>
                    <div class="d-flex gap-2">
                      <button
                        type="button"
                        class="btn btn-outline-primary btn-sm"
                        (click)="selectAllPermissions()"
                      >
                        <i class="fas fa-check-double me-1"></i>
                        Tümünü Seç
                      </button>
                      <button
                        type="button"
                        class="btn btn-outline-secondary btn-sm"
                        (click)="clearAllPermissions()"
                      >
                        <i class="fas fa-times me-1"></i>
                        Tümünü Temizle
                      </button>
                    </div>
                  </div>
                </div>
                <div class="card-body">
                  <div class="permissions-container" *ngIf="permissionCategories().length > 0">
                    <div
                      class="permission-category mb-4"
                      *ngFor="let category of permissionCategories()"
                    >
                      <div class="category-header">
                        <div class="form-check">
                          <input
                            type="checkbox"
                            [id]="'category-' + category.name"
                            class="form-check-input"
                            [checked]="isCategorySelected(category)"
                            [indeterminate]="isCategoryIndeterminate(category)"
                            (change)="onCategoryChange(category, $event)"
                          />
                          <label [for]="'category-' + category.name" class="form-check-label category-label">
                            <i [class]="category.icon || 'fas fa-folder'" class="me-2"></i>
                            {{ category.name }}
                            <span class="badge bg-secondary ms-2">
                              {{ getSelectedPermissionsCount(category) }}/{{ category.permissions.length }}
                            </span>
                          </label>
                        </div>
                        <small class="text-muted d-block mt-1">{{ category.description }}</small>
                      </div>
                      <div class="permissions-grid">
                        <div
                          class="form-check permission-item"
                          *ngFor="let permission of category.permissions"
                        >
                          <input
                            type="checkbox"
                            [id]="'permission-' + permission.id"
                            class="form-check-input"
                            [value]="permission.id"
                            (change)="onPermissionChange(permission.id, $event)"
                            [checked]="selectedPermissions().has(permission.id)"
                          />
                          <label [for]="'permission-' + permission.id" class="form-check-label">
                            {{ permission.name }}
                            <small class="text-muted d-block">{{ permission.description }}</small>
                          </label>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="text-center py-4" *ngIf="permissionCategories().length === 0">
                    <i class="fas fa-shield-alt fa-3x text-muted mb-3"></i>
                    <p class="text-muted">İzinler yükleniyor...</p>
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
                      [disabled]="roleForm.invalid || loading() || !hasChanges()"
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
                    <a [routerLink]="['/roles', role()?.id]" class="btn btn-outline-secondary">
                      <i class="fas fa-times me-2"></i>
                      İptal
                    </a>
                  </div>

                  <hr class="my-3">

                  <!-- Role Summary -->
                  <div class="role-summary">
                    <h6 class="mb-3">
                      <i class="fas fa-info-circle me-2"></i>
                      Rol Özeti
                    </h6>
                    <div class="summary-item">
                      <span class="label">Seçilen İzin Sayısı:</span>
                      <span class="value">{{ selectedPermissions().size }}</span>
                    </div>
                    <div class="summary-item">
                      <span class="label">Değişen İzin Sayısı:</span>
                      <span class="value">{{ getChangedPermissionsCount() }}</span>
                    </div>
                    <div class="summary-item">
                      <span class="label">Durum:</span>
                      <span class="value">
                        <span class="badge" [class]="roleForm.get('isActive')?.value ? 'bg-success' : 'bg-secondary'">
                          {{ roleForm.get('isActive')?.value ? 'Aktif' : 'Pasif' }}
                        </span>
                      </span>
                    </div>
                    <div class="summary-item">
                      <span class="label">Varsayılan:</span>
                      <span class="value">
                        <span class="badge" [class]="roleForm.get('isDefault')?.value ? 'bg-info' : 'bg-secondary'">
                          {{ roleForm.get('isDefault')?.value ? 'Evet' : 'Hayır' }}
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
    <div class="text-center py-5" *ngIf="!role() && loadingRole()">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Yükleniyor...</span>
      </div>
      <p class="mt-3 text-muted">Rol bilgileri yükleniyor...</p>
    </div>
  `,
  styles: [`
    .role-edit-container {
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

    .permission-category {
      border: 1px solid var(--bs-border-color);
      border-radius: var(--border-radius-md);
      padding: 1rem;
      background: var(--bs-gray-50);
    }

    .category-header {
      margin-bottom: 1rem;
    }

    .category-label {
      font-weight: 600;
      font-size: 1.1rem;
    }

    .permissions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 0.75rem;
      margin-left: 1.5rem;
    }

    .permission-item {
      background: white;
      border: 1px solid var(--bs-border-color);
      border-radius: var(--border-radius);
      padding: 0.75rem;
      transition: all var(--transition-normal);
    }

    .permission-item:hover {
      border-color: var(--bs-primary);
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .permission-item .form-check-input:checked ~ .form-check-label {
      color: var(--bs-primary);
      font-weight: 500;
    }

    .role-summary {
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
      .permissions-grid {
        grid-template-columns: 1fr;
        margin-left: 0;
      }

      .sticky-top {
        position: static;
      }
    }
  `]
})
export class RoleEditComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly roleService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  role = signal<Role | null>(null);
  roleForm: FormGroup;
  loading = signal<boolean>(false);
  loadingRole = signal<boolean>(false);
  permissionCategories = signal<PermissionCategory[]>([]);
  selectedPermissions = signal<Set<string>>(new Set());
  originalPermissions = signal<Set<string>>(new Set());
  originalFormValue: any = null;

  private roleId = computed(() => this.route.snapshot.params['id']);

  constructor() {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      normalizedName: [''],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true],
      isDefault: [false]
    });

    this.roleForm.get('name')?.valueChanges.subscribe(value => {
      if (value && !this.role()?.isDefault) {
        const normalized = value.toUpperCase().replace(/\s+/g, '_');
        this.roleForm.get('normalizedName')?.setValue(normalized, { emitEvent: false });
      }
    });
  }

  ngOnInit(): void {
    this.loadRoleDetail();
    this.loadPermissions();
  }

  loadRoleDetail(): void {
    const id = this.roleId();
    if (!id) return;

    this.loadingRole.set(true);

    this.roleService.getRole(id).subscribe({
      next: (role) => {
        this.role.set(role);
        this.populateForm(role);
        this.loadRolePermissions(id);
        this.loadingRole.set(false);
      },
      error: (error) => {
        console.error('Error loading role:', error);
        this.notificationService.error('Rol bilgileri yüklenirken bir hata oluştu', 'Hata');
        this.loadingRole.set(false);
        this.router.navigate(['/roles']);
      }
    });
  }

  loadPermissions(): void {
    this.roleService.getPermissionCategories().subscribe({
      next: (categories) => {
        this.permissionCategories.set(categories);
      },
      error: (error) => {
        console.error('Error loading permissions:', error);
        this.notificationService.error('İzinler yüklenirken bir hata oluştu', 'Hata');
      }
    });
  }

  loadRolePermissions(roleId: string): void {
    this.roleService.getRolePermissions(roleId).subscribe({
      next: (permissions) => {
        const permissionIds = new Set(permissions.map(p => p.id));
        this.selectedPermissions.set(permissionIds);
        this.originalPermissions.set(new Set(permissionIds));
      },
      error: (error) => {
        console.error('Error loading role permissions:', error);
      }
    });
  }

  populateForm(role: Role): void {
    this.roleForm.patchValue({
      name: role.name,
      normalizedName: role.normalizedName,
      description: role.description,
      isActive: role.isActive,
      isDefault: role.isDefault
    });

    this.originalFormValue = this.roleForm.value;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.roleForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onPermissionChange(permissionId: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const permissions = new Set(this.selectedPermissions());

    if (target.checked) {
      permissions.add(permissionId);
    } else {
      permissions.delete(permissionId);
    }

    this.selectedPermissions.set(permissions);
  }

  onCategoryChange(category: PermissionCategory, event: Event): void {
    const target = event.target as HTMLInputElement;
    const permissions = new Set(this.selectedPermissions());

    if (target.checked) {
      category.permissions.forEach(p => permissions.add(p.id));
    } else {
      category.permissions.forEach(p => permissions.delete(p.id));
    }

    this.selectedPermissions.set(permissions);
  }

  isCategorySelected(category: PermissionCategory): boolean {
    return category.permissions.every(p => this.selectedPermissions().has(p.id));
  }

  isCategoryIndeterminate(category: PermissionCategory): boolean {
    const selectedCount = category.permissions.filter(p => this.selectedPermissions().has(p.id)).length;
    return selectedCount > 0 && selectedCount < category.permissions.length;
  }

  getSelectedPermissionsCount(category: PermissionCategory): number {
    return category.permissions.filter(p => this.selectedPermissions().has(p.id)).length;
  }

  selectAllPermissions(): void {
    const allPermissions = new Set<string>();
    this.permissionCategories().forEach(category => {
      category.permissions.forEach(p => allPermissions.add(p.id));
    });
    this.selectedPermissions.set(allPermissions);
  }

  clearAllPermissions(): void {
    this.selectedPermissions.set(new Set());
  }

  hasChanges(): boolean {
    if (!this.originalFormValue) return false;

    const currentFormValue = this.roleForm.value;
    const formChanged = JSON.stringify(currentFormValue) !== JSON.stringify(this.originalFormValue);

    const currentPermissions = this.selectedPermissions();
    const originalPermissions = this.originalPermissions();
    const permissionsChanged = currentPermissions.size !== originalPermissions.size ||
      [...currentPermissions].some(id => !originalPermissions.has(id));

    return formChanged || permissionsChanged;
  }

  getChangedPermissionsCount(): number {
    const current = this.selectedPermissions();
    const original = this.originalPermissions();

    const added = [...current].filter(id => !original.has(id));
    const removed = [...original].filter(id => !current.has(id));

    return added.length + removed.length;
  }

  resetForm(): void {
    if (this.originalFormValue) {
      this.roleForm.patchValue(this.originalFormValue);
      this.selectedPermissions.set(new Set(this.originalPermissions()));
    }
  }

  onSubmit(): void {
    if (this.roleForm.valid && this.hasChanges()) {
      this.loading.set(true);

      const formValue = this.roleForm.value;
      const role = this.role();
      if (!role) return;

      const request: UpdateRoleRequest = {
        id: role.id,
        name: formValue.name,
        // normalizedName: formValue.normalizedName,
        description: formValue.description,
        isActive: formValue.isActive,
        isDefault: formValue.isDefault,
        permissionIds: Array.from(this.selectedPermissions())
      };

      this.roleService.updateRole(request).subscribe({
        next: (updatedRole) => {
          this.notificationService.success('Rol başarıyla güncellendi', 'İşlem Başarılı');
          this.role.set(updatedRole);
          this.originalFormValue = this.roleForm.value;
          this.originalPermissions.set(new Set(this.selectedPermissions()));
          this.loading.set(false);
        },
        error: (error) => {
          console.error('Update role error:', error);
          this.notificationService.error('Rol güncellenirken bir hata oluştu', 'Hata');
          this.loading.set(false);
        }
      });
    } else if (!this.hasChanges()) {
      this.notificationService.info('Değişiklik yapılmamış', 'Bilgi');
    } else {
      Object.keys(this.roleForm.controls).forEach(key => {
        this.roleForm.get(key)?.markAsTouched();
      });
    }
  }
}