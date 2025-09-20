import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  User,
  UpdateUserRequest,
  Role,
  Group
} from '../../models/user-management.models';

@Component({
  selector: 'app-user-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="user-edit-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <nav aria-label="breadcrumb">
              <ol class="breadcrumb">
                <li class="breadcrumb-item">
                  <a routerLink="/users">Kullanıcılar</a>
                </li>
                <li class="breadcrumb-item">
                  <a [routerLink]="['/users', userId()]">{{ getFullName() }}</a>
                </li>
                <li class="breadcrumb-item active">Düzenle</li>
              </ol>
            </nav>
            <h1 class="page-title">
              <i class="fas fa-user-edit me-3"></i>
              Kullanıcı Düzenle
            </h1>
          </div>
          <div class="col-auto">
            <div class="d-flex gap-2">
              <a [routerLink]="['/users', userId()]" class="btn btn-outline-secondary">
                <i class="fas fa-arrow-left me-2"></i>
                Geri Dön
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

      <!-- Edit Form -->
      <div class="row" *ngIf="!loading() && userForm">
        <div class="col-lg-8 col-xl-9">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-user-circle me-2"></i>
                Kullanıcı Bilgilerini Düzenle
              </h5>
            </div>
            <div class="card-body">
              <form [formGroup]="userForm" (ngSubmit)="onSubmit()" novalidate>
                <!-- Personal Information -->
                <div class="form-section">
                  <h6 class="section-title">Kişisel Bilgiler</h6>

                  <div class="row">
                    <div class="col-md-6">
                      <div class="mb-3">
                        <label for="firstName" class="form-label">
                          Ad <span class="text-danger">*</span>
                        </label>
                        <input
                          type="text"
                          id="firstName"
                          class="form-control"
                          [class.is-invalid]="isFieldInvalid('firstName')"
                          formControlName="firstName"
                          placeholder="Kullanıcının adı"
                        >
                        <div class="invalid-feedback" *ngIf="isFieldInvalid('firstName')">
                          <small *ngIf="userForm.get('firstName')?.errors?.['required']">
                            Ad alanı gereklidir
                          </small>
                        </div>
                      </div>
                    </div>

                    <div class="col-md-6">
                      <div class="mb-3">
                        <label for="lastName" class="form-label">
                          Soyad <span class="text-danger">*</span>
                        </label>
                        <input
                          type="text"
                          id="lastName"
                          class="form-control"
                          [class.is-invalid]="isFieldInvalid('lastName')"
                          formControlName="lastName"
                          placeholder="Kullanıcının soyadı"
                        >
                        <div class="invalid-feedback" *ngIf="isFieldInvalid('lastName')">
                          <small *ngIf="userForm.get('lastName')?.errors?.['required']">
                            Soyad alanı gereklidir
                          </small>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="row">
                    <div class="col-md-6">
                      <div class="mb-3">
                        <label for="email" class="form-label">
                          E-posta <span class="text-danger">*</span>
                        </label>
                        <input
                          type="email"
                          id="email"
                          class="form-control"
                          [class.is-invalid]="isFieldInvalid('email')"
                          formControlName="email"
                          placeholder="ornek@email.com"
                        >
                        <div class="invalid-feedback" *ngIf="isFieldInvalid('email')">
                          <small *ngIf="userForm.get('email')?.errors?.['required']">
                            E-posta alanı gereklidir
                          </small>
                          <small *ngIf="userForm.get('email')?.errors?.['email']">
                            Geçerli bir e-posta adresi giriniz
                          </small>
                        </div>
                      </div>
                    </div>

                    <div class="col-md-6">
                      <div class="mb-3">
                        <label for="userName" class="form-label">
                          Kullanıcı Adı <span class="text-danger">*</span>
                        </label>
                        <input
                          type="text"
                          id="userName"
                          class="form-control"
                          [class.is-invalid]="isFieldInvalid('userName')"
                          formControlName="userName"
                          placeholder="Benzersiz kullanıcı adı"
                        >
                        <div class="invalid-feedback" *ngIf="isFieldInvalid('userName')">
                          <small *ngIf="userForm.get('userName')?.errors?.['required']">
                            Kullanıcı adı gereklidir
                          </small>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="mb-3">
                    <label for="phoneNumber" class="form-label">Telefon</label>
                    <input
                      type="tel"
                      id="phoneNumber"
                      class="form-control"
                      [class.is-invalid]="isFieldInvalid('phoneNumber')"
                      formControlName="phoneNumber"
                      placeholder="+90 555 123 4567"
                    >
                    <div class="invalid-feedback" *ngIf="isFieldInvalid('phoneNumber')">
                      <small *ngIf="userForm.get('phoneNumber')?.errors?.['pattern']">
                        Geçerli bir telefon numarası giriniz
                      </small>
                    </div>
                  </div>
                </div>

                <!-- Roles and Groups -->
                <div class="form-section">
                  <h6 class="section-title">Roller ve Gruplar</h6>

                  <div class="row">
                    <div class="col-md-6">
                      <div class="mb-3">
                        <label for="roles" class="form-label">Roller</label>
                        <select
                          multiple
                          id="roles"
                          class="form-select"
                          formControlName="roleIds"
                          size="8"
                        >
                          <option
                            *ngFor="let role of availableRoles()"
                            [value]="role.id"
                          >
                            {{ role.name }}
                            <span *ngIf="role.description"> - {{ role.description }}</span>
                          </option>
                        </select>
                        <div class="form-text">
                          Ctrl tuşunu basılı tutarak birden fazla rol seçebilirsiniz
                        </div>
                      </div>
                    </div>

                    <div class="col-md-6">
                      <div class="mb-3">
                        <label for="groups" class="form-label">Gruplar</label>
                        <select
                          multiple
                          id="groups"
                          class="form-select"
                          formControlName="groupIds"
                          size="8"
                        >
                          <option
                            *ngFor="let group of availableGroups()"
                            [value]="group.id"
                          >
                            {{ group.name }}
                            <span *ngIf="group.description"> - {{ group.description }}</span>
                          </option>
                        </select>
                        <div class="form-text">
                          Ctrl tuşunu basılı tutarak birden fazla grup seçebilirsiniz
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Status Settings -->
                <div class="form-section">
                  <h6 class="section-title">Durum Ayarları</h6>

                  <div class="row">
                    <div class="col-md-4">
                      <div class="form-check mb-3">
                        <input
                          type="checkbox"
                          id="isActive"
                          class="form-check-input"
                          formControlName="isActive"
                        >
                        <label for="isActive" class="form-check-label">
                          Kullanıcı aktif
                        </label>
                        <div class="form-text">
                          Pasif kullanıcılar sisteme giriş yapamaz
                        </div>
                      </div>
                    </div>

                    <div class="col-md-4">
                      <div class="form-check mb-3">
                        <input
                          type="checkbox"
                          id="emailConfirmed"
                          class="form-check-input"
                          formControlName="emailConfirmed"
                        >
                        <label for="emailConfirmed" class="form-check-label">
                          E-posta doğrulanmış
                        </label>
                        <div class="form-text">
                          E-posta doğrulama durumu
                        </div>
                      </div>
                    </div>

                    <div class="col-md-4">
                      <div class="form-check mb-3">
                        <input
                          type="checkbox"
                          id="phoneNumberConfirmed"
                          class="form-check-input"
                          formControlName="phoneNumberConfirmed"
                        >
                        <label for="phoneNumberConfirmed" class="form-check-label">
                          Telefon doğrulanmış
                        </label>
                        <div class="form-text">
                          Telefon doğrulama durumu
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="row">
                    <div class="col-md-4">
                      <div class="form-check mb-3">
                        <input
                          type="checkbox"
                          id="twoFactorEnabled"
                          class="form-check-input"
                          formControlName="twoFactorEnabled"
                        >
                        <label for="twoFactorEnabled" class="form-check-label">
                          İki faktörlü kimlik doğrulama
                        </label>
                        <div class="form-text">
                          2FA durumu
                        </div>
                      </div>
                    </div>

                    <div class="col-md-4">
                      <div class="form-check mb-3">
                        <input
                          type="checkbox"
                          id="lockoutEnabled"
                          class="form-check-input"
                          formControlName="lockoutEnabled"
                        >
                        <label for="lockoutEnabled" class="form-check-label">
                          Hesap kilitleme etkin
                        </label>
                        <div class="form-text">
                          Başarısız giriş durumunda kilitleme
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Submit Buttons -->
                <div class="d-flex gap-2 pt-3 border-top">
                  <button
                    type="submit"
                    class="btn btn-primary"
                    [disabled]="userForm.invalid || isSubmitting()"
                  >
                    <span *ngIf="isSubmitting()" class="spinner-border spinner-border-sm me-2"></span>
                    <i *ngIf="!isSubmitting()" class="fas fa-save me-2"></i>
                    {{ isSubmitting() ? 'Kaydediliyor...' : 'Değişiklikleri Kaydet' }}
                  </button>
                  <button
                    type="button"
                    class="btn btn-outline-warning"
                    (click)="resetForm()"
                    [disabled]="isSubmitting()"
                  >
                    <i class="fas fa-undo me-2"></i>
                    Sıfırla
                  </button>
                  <a [routerLink]="['/users', userId()]" class="btn btn-outline-secondary">
                    <i class="fas fa-times me-2"></i>
                    İptal
                  </a>
                </div>
              </form>
            </div>
          </div>
        </div>

        <!-- Info Panel -->
        <div class="col-lg-4 col-xl-3">
          <div class="card">
            <div class="card-header">
              <h6 class="card-title mb-0">
                <i class="fas fa-info-circle me-2"></i>
                Kullanıcı Bilgileri
              </h6>
            </div>
            <div class="card-body">
              <div class="user-avatar text-center mb-3">
                <img
                  [src]="getUserAvatar()"
                  [alt]="getFullName()"
                  class="rounded-circle"
                  style="width: 80px; height: 80px;"
                >
              </div>

              <div class="user-info">
                <div class="info-item">
                  <label>Kullanıcı ID:</label>
                  <div>{{ user()?.id }}</div>
                </div>
                <div class="info-item">
                  <label>Oluşturulma:</label>
                  <div>{{ user()?.createdAt | date:'dd.MM.yyyy' }}</div>
                </div>
                <div class="info-item">
                  <label>Son Güncelleme:</label>
                  <div>{{ user()?.updatedAt | date:'dd.MM.yyyy' }}</div>
                </div>
                <div class="info-item" *ngIf="user()?.lastLoginAt">
                  <label>Son Giriş:</label>
                  <div>{{ user()?.lastLoginAt | date:'dd.MM.yyyy HH:mm' }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Password Actions -->
          <div class="card mt-3">
            <div class="card-header">
              <h6 class="card-title mb-0">
                <i class="fas fa-key me-2"></i>
                Şifre İşlemleri
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
                <a
                  [routerLink]="['/users', userId(), 'change-password']"
                  class="btn btn-outline-secondary btn-sm"
                >
                  <i class="fas fa-edit me-2"></i>
                  Şifre Değiştir
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .user-edit-container {
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

    .form-section {
      margin-bottom: 2rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--bs-border-color);
    }

    .form-section:last-of-type {
      border-bottom: none;
      margin-bottom: 0;
    }

    .section-title {
      color: var(--bs-primary);
      font-weight: 600;
      margin-bottom: 1rem;
      font-size: 1.1rem;
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

    .form-select[multiple] {
      min-height: 180px;
    }

    .form-check {
      padding-left: 1.5rem;
    }

    .form-check-input {
      margin-left: -1.5rem;
    }

    .user-info .info-item {
      margin-bottom: 1rem;
      padding-bottom: 0.5rem;
      border-bottom: 1px solid var(--bs-border-color);
    }

    .user-info .info-item:last-child {
      border-bottom: none;
      margin-bottom: 0;
    }

    .user-info label {
      font-size: 0.875rem;
      color: var(--bs-nav-link-color);
      font-weight: 500;
      margin-bottom: 0.25rem;
      display: block;
    }

    .user-info div {
      font-size: 0.9rem;
      color: var(--bs-body-color);
      font-weight: 500;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .user-edit-container {
        padding: 1rem;
      }

      .page-header .row {
        flex-direction: column;
        gap: 1rem;
      }

      .page-header .col-auto {
        width: 100%;
      }
    }
  `]
})
export class UserEditComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  user = signal<User | null>(null);
  userForm!: FormGroup;
  isSubmitting = signal(false);
  loading = signal(false);
  availableRoles = signal<Role[]>([]);
  availableGroups = signal<Group[]>([]);

  userId = computed(() => {
    return this.route.snapshot.paramMap.get('id') || '';
  });

  ngOnInit(): void {
    this.createForm();
    this.loadUser();
    this.loadRoles();
    this.loadGroups();
  }

  private createForm(): void {
    this.userForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      userName: ['', [Validators.required]],
      phoneNumber: ['', [Validators.pattern(/^(\+\d{1,3}[- ]?)?\d{10}$/)]],
      roleIds: [[]],
      groupIds: [[]],
      isActive: [true],
      emailConfirmed: [false],
      phoneNumberConfirmed: [false],
      twoFactorEnabled: [false],
      lockoutEnabled: [true]
    });
  }

  private loadUser(): void {
    const id = this.userId();
    if (!id) {
      this.router.navigate(['/users']);
      return;
    }

    this.loading.set(true);

    this.userService.getUser(id).subscribe({
      next: (user) => {
        this.user.set(user);
        this.populateForm(user);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading user:', error);
        this.notificationService.error(
          'Kullanıcı bilgileri yüklenirken bir hata oluştu',
          'Hata'
        );
        this.loading.set(false);
        this.router.navigate(['/users']);
      }
    });
  }

  private populateForm(user: User): void {
    this.userForm.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      userName: user.userName,
      phoneNumber: user.phoneNumber || '',
      roleIds: user.roles?.map(r => r.id) || [],
      groupIds: user.groups?.map(g => g.id) || [],
      isActive: user.isActive,
      emailConfirmed: user.emailConfirmed,
      phoneNumberConfirmed: user.phoneNumberConfirmed,
      twoFactorEnabled: user.twoFactorEnabled,
      lockoutEnabled: user.lockoutEnabled
    });
  }

  private loadRoles(): void {
    this.userService.getAllRoles().subscribe({
      next: (roles) => {
        this.availableRoles.set(roles);
      },
      error: (error) => {
        console.error('Error loading roles:', error);
      }
    });
  }

  private loadGroups(): void {
    this.userService.getAllGroups().subscribe({
      next: (groups) => {
        this.availableGroups.set(groups);
      },
      error: (error) => {
        console.error('Error loading groups:', error);
      }
    });
  }

  onSubmit(): void {
    if (this.userForm.valid && !this.isSubmitting()) {
      this.isSubmitting.set(true);

      const formValue = this.userForm.value;
      const request: UpdateUserRequest = {
        id: this.userId(),
        userName: formValue.userName,
        email: formValue.email,
        firstName: formValue.firstName,
        lastName: formValue.lastName,
        phoneNumber: formValue.phoneNumber || undefined,
        isActive: formValue.isActive,
        emailConfirmed: formValue.emailConfirmed,
        phoneNumberConfirmed: formValue.phoneNumberConfirmed,
        twoFactorEnabled: formValue.twoFactorEnabled,
        lockoutEnabled: formValue.lockoutEnabled,
        roleIds: formValue.roleIds && formValue.roleIds.length > 0 ? formValue.roleIds : undefined,
        groupIds: formValue.groupIds && formValue.groupIds.length > 0 ? formValue.groupIds : undefined
      };

      this.userService.updateUser(request).subscribe({
        next: (updatedUser) => {
          this.user.set(updatedUser);
          this.notificationService.success(
            'Kullanıcı bilgileri başarıyla güncellendi',
            'İşlem Başarılı'
          );
          this.isSubmitting.set(false);
        },
        error: (error) => {
          console.error('Update user error:', error);
          this.notificationService.error(
            error.userMessage || 'Kullanıcı güncellenirken bir hata oluştu',
            'Hata'
          );
          this.isSubmitting.set(false);
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  resetForm(): void {
    const user = this.user();
    if (user) {
      this.populateForm(user);
      this.notificationService.info('Form orijinal değerlere sıfırlandı', 'Bilgi');
    }
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
    return `https://ui-avatars.com/api/?name=${initials}&background=0d6efd&color=fff&size=80`;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.userForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private markFormGroupTouched(): void {
    Object.keys(this.userForm.controls).forEach(key => {
      const control = this.userForm.get(key);
      control?.markAsTouched();
    });
  }
}