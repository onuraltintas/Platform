import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';

import { UserManagementService } from '../../services/user-management.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { CustomValidators } from '../../../../shared/utils/validators';
import {
  CreateUserRequest,
  Role,
  Group
} from '../../models/user-management.models';
import { AppState } from '../../../../store';
import { UserActions, RoleActions, GroupActions } from '../../../../store/user-management';
import {
  selectUsersCreating,
  selectAllRoles,
  selectAllGroups
} from '../../../../store/user-management/user-management.selectors';

@Component({
  selector: 'app-user-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="user-create-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="row align-items-center">
          <div class="col">
            <nav aria-label="breadcrumb">
              <ol class="breadcrumb">
                <li class="breadcrumb-item">
                  <a routerLink="/users">Kullanıcılar</a>
                </li>
                <li class="breadcrumb-item active">Yeni Kullanıcı</li>
              </ol>
            </nav>
            <h1 class="page-title">
              <i class="fas fa-user-plus me-3"></i>
              Yeni Kullanıcı Ekle
            </h1>
          </div>
          <div class="col-auto">
            <a routerLink="/users" class="btn btn-outline-secondary">
              <i class="fas fa-arrow-left me-2"></i>
              Geri Dön
            </a>
          </div>
        </div>
      </div>

      <!-- Form -->
      <div class="row">
        <div class="col-lg-8 col-xl-6">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-user-circle me-2"></i>
                Kullanıcı Bilgileri
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
                          <small *ngIf="userForm.get('firstName')?.errors?.['minlength']">
                            Ad en az 2 karakter olmalıdır
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
                          <small *ngIf="userForm.get('lastName')?.errors?.['minlength']">
                            Soyad en az 2 karakter olmalıdır
                          </small>
                        </div>
                      </div>
                    </div>
                  </div>

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
                      <small *ngIf="userForm.get('userName')?.errors?.['minlength']">
                        Kullanıcı adı en az 3 karakter olmalıdır
                      </small>
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

                <!-- Password Section -->
                <div class="form-section">
                  <h6 class="section-title">Şifre Bilgileri</h6>

                  <div class="row">
                    <div class="col-md-6">
                      <div class="mb-3">
                        <label for="password" class="form-label">
                          Şifre <span class="text-danger">*</span>
                        </label>
                        <div class="input-group">
                          <input
                            [type]="showPassword() ? 'text' : 'password'"
                            id="password"
                            class="form-control"
                            [class.is-invalid]="isFieldInvalid('password')"
                            formControlName="password"
                            placeholder="Güçlü bir şifre oluşturun"
                          >
                          <button
                            type="button"
                            class="btn btn-outline-secondary"
                            (click)="togglePasswordVisibility()"
                          >
                            <i [class]="showPassword() ? 'fas fa-eye-slash' : 'fas fa-eye'"></i>
                          </button>
                        </div>
                        <div class="invalid-feedback" *ngIf="isFieldInvalid('password')">
                          <small *ngIf="userForm.get('password')?.errors?.['required']">
                            Şifre gereklidir
                          </small>
                          <div *ngIf="userForm.get('password')?.errors?.['password']">
                            <small class="d-block" *ngIf="userForm.get('password')?.errors?.['password']?.['minLength']">
                              Şifre en az 8 karakter olmalıdır
                            </small>
                            <small class="d-block" *ngIf="userForm.get('password')?.errors?.['password']?.['requiresNumber']">
                              Şifre en az bir rakam içermelidir
                            </small>
                            <small class="d-block" *ngIf="userForm.get('password')?.errors?.['password']?.['requiresUppercase']">
                              Şifre en az bir büyük harf içermelidir
                            </small>
                            <small class="d-block" *ngIf="userForm.get('password')?.errors?.['password']?.['requiresLowercase']">
                              Şifre en az bir küçük harf içermelidir
                            </small>
                            <small class="d-block" *ngIf="userForm.get('password')?.errors?.['password']?.['requiresSpecial']">
                              Şifre en az bir özel karakter içermelidir
                            </small>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div class="col-md-6">
                      <div class="mb-3">
                        <label for="confirmPassword" class="form-label">
                          Şifre Tekrarı <span class="text-danger">*</span>
                        </label>
                        <div class="input-group">
                          <input
                            [type]="showConfirmPassword() ? 'text' : 'password'"
                            id="confirmPassword"
                            class="form-control"
                            [class.is-invalid]="isFieldInvalid('confirmPassword')"
                            formControlName="confirmPassword"
                            placeholder="Şifrenizi tekrar giriniz"
                          >
                          <button
                            type="button"
                            class="btn btn-outline-secondary"
                            (click)="toggleConfirmPasswordVisibility()"
                          >
                            <i [class]="showConfirmPassword() ? 'fas fa-eye-slash' : 'fas fa-eye'"></i>
                          </button>
                        </div>
                        <div class="invalid-feedback" *ngIf="isFieldInvalid('confirmPassword')">
                          <small *ngIf="userForm.get('confirmPassword')?.errors?.['required']">
                            Şifre tekrarı gereklidir
                          </small>
                          <small *ngIf="userForm.get('confirmPassword')?.errors?.['passwordMismatch']">
                            Şifreler eşleşmiyor
                          </small>
                        </div>
                      </div>
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
                          size="5"
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
                          size="5"
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
                    <div class="col-md-6">
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

                    <div class="col-md-6">
                      <div class="form-check mb-3">
                        <input
                          type="checkbox"
                          id="emailConfirmed"
                          class="form-check-input"
                          formControlName="emailConfirmed"
                        >
                        <label for="emailConfirmed" class="form-check-label">
                          E-posta doğrulanmış olarak işaretle
                        </label>
                        <div class="form-text">
                          İşaretlenmezse kullanıcıya doğrulama e-postası gönderilir
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
                    {{ isSubmitting() ? 'Kullanıcı oluşturuluyor...' : 'Kullanıcıyı Oluştur' }}
                  </button>
                  <a routerLink="/users" class="btn btn-outline-secondary">
                    <i class="fas fa-times me-2"></i>
                    İptal
                  </a>
                </div>
              </form>
            </div>
          </div>
        </div>

        <!-- Help Panel -->
        <div class="col-lg-4 col-xl-6">
          <div class="card">
            <div class="card-header">
              <h6 class="card-title mb-0">
                <i class="fas fa-question-circle me-2"></i>
                Yardım
              </h6>
            </div>
            <div class="card-body">
              <div class="help-section">
                <h6>Şifre Gereksinimleri</h6>
                <ul class="list-unstyled small">
                  <li><i class="fas fa-check text-success me-2"></i>En az 8 karakter</li>
                  <li><i class="fas fa-check text-success me-2"></i>En az bir büyük harf</li>
                  <li><i class="fas fa-check text-success me-2"></i>En az bir küçük harf</li>
                  <li><i class="fas fa-check text-success me-2"></i>En az bir rakam</li>
                  <li><i class="fas fa-check text-success me-2"></i>En az bir özel karakter</li>
                </ul>
              </div>

              <div class="help-section">
                <h6>Kullanıcı Adı</h6>
                <ul class="list-unstyled small">
                  <li><i class="fas fa-info text-info me-2"></i>Benzersiz olmalıdır</li>
                  <li><i class="fas fa-info text-info me-2"></i>En az 3 karakter</li>
                  <li><i class="fas fa-info text-info me-2"></i>Sadece harf, rakam ve alt çizgi</li>
                </ul>
              </div>

              <div class="help-section">
                <h6>Roller ve İzinler</h6>
                <p class="small text-muted">
                  Kullanıcıya atanan roller, sistem içindeki erişim yetkilerini belirler.
                  Birden fazla rol atayarak kullanıcının yetkilerini genişletebilirsiniz.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .user-create-container {
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

    .help-section {
      margin-bottom: 1.5rem;
    }

    .help-section:last-child {
      margin-bottom: 0;
    }

    .help-section h6 {
      color: var(--bs-body-color);
      font-weight: 600;
      margin-bottom: 0.5rem;
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
      min-height: 120px;
    }

    .form-check {
      padding-left: 1.5rem;
    }

    .form-check-input {
      margin-left: -1.5rem;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .user-create-container {
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
export class UserCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  userForm!: FormGroup;
  isSubmitting = signal(false);
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  availableRoles = signal<Role[]>([]);
  availableGroups = signal<Group[]>([]);

  ngOnInit(): void {
    this.createForm();
    this.loadRoles();
    this.loadGroups();
  }

  private createForm(): void {
    this.userForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      userName: ['', [Validators.required, Validators.minLength(3)]],
      phoneNumber: ['', [Validators.pattern(/^(\+\d{1,3}[- ]?)?\d{10}$/)]],
      password: ['', [Validators.required, CustomValidators.password]],
      confirmPassword: ['', [Validators.required]],
      roleIds: [[]],
      groupIds: [[]],
      isActive: [true],
      emailConfirmed: [false]
    }, {
      validators: [CustomValidators.passwordMatch('password', 'confirmPassword')]
    });

    // Auto-generate username from first and last name
    this.userForm.get('firstName')?.valueChanges.subscribe(() => this.generateUserName());
    this.userForm.get('lastName')?.valueChanges.subscribe(() => this.generateUserName());
  }

  private generateUserName(): void {
    const firstName = this.userForm.get('firstName')?.value || '';
    const lastName = this.userForm.get('lastName')?.value || '';

    if (firstName && lastName) {
      const userName = `${firstName.toLowerCase()}.${lastName.toLowerCase()}`;
      this.userForm.get('userName')?.setValue(userName);
    }
  }

  private loadRoles(): void {
    this.userService.getAllRoles().subscribe({
      next: (roles) => {
        this.availableRoles.set(roles);
      },
      error: (error) => {
        console.error('Error loading roles:', error);
        this.notificationService.error('Roller yüklenirken hata oluştu', 'Hata');
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
        this.notificationService.error('Gruplar yüklenirken hata oluştu', 'Hata');
      }
    });
  }

  onSubmit(): void {
    if (this.userForm.valid && !this.isSubmitting()) {
      this.isSubmitting.set(true);

      const formValue = this.userForm.value;
      const request: CreateUserRequest = {
        userName: formValue.userName,
        email: formValue.email,
        firstName: formValue.firstName,
        lastName: formValue.lastName,
        phoneNumber: formValue.phoneNumber || undefined,
        password: formValue.password,
        confirmPassword: formValue.confirmPassword,
        isActive: formValue.isActive,
        emailConfirmed: formValue.emailConfirmed,
        roleIds: formValue.roleIds && formValue.roleIds.length > 0 ? formValue.roleIds : undefined,
        groupIds: formValue.groupIds && formValue.groupIds.length > 0 ? formValue.groupIds : undefined
      };

      this.userService.createUser(request).subscribe({
        next: (user) => {
          this.notificationService.success(
            `${user.firstName} ${user.lastName} kullanıcısı başarıyla oluşturuldu`,
            'İşlem Başarılı'
          );
          this.router.navigate(['/users']);
        },
        error: (error) => {
          console.error('Create user error:', error);
          this.notificationService.error(
            error.userMessage || 'Kullanıcı oluşturulurken bir hata oluştu',
            'Hata'
          );
          this.isSubmitting.set(false);
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(current => !current);
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update(current => !current);
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