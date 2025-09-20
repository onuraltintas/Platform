import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

import { UserService } from '../../services/user.service';
import { RoleService } from '../../services/role.service';
import { GroupService } from '../../services/group.service';
import { User, Role, Group, CreateUserRequest, UpdateUserRequest } from '../../models/simple.models';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="user-form-container">
      <!-- Header -->
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>{{ isEditMode() ? 'Kullanıcı Düzenle' : 'Yeni Kullanıcı' }}</h2>
        <button class="btn btn-outline-secondary" (click)="goBack()">
          <i class="fas fa-arrow-left me-2"></i>
          Geri
        </button>
      </div>

      <!-- Loading -->
      <div *ngIf="loading()" class="text-center py-4">
        <div class="spinner-border" role="status">
          <span class="visually-hidden">Yükleniyor...</span>
        </div>
      </div>

      <!-- Error -->
      <div *ngIf="error()" class="alert alert-danger">
        {{ error() }}
      </div>

      <!-- Form -->
      <div *ngIf="!loading()" class="card">
        <div class="card-body">
          <form [formGroup]="userForm" (ngSubmit)="onSubmit()">
            <div class="row">
              <!-- Basic Info -->
              <div class="col-md-6">
                <h5 class="mb-3">Temel Bilgiler</h5>

                <div class="mb-3">
                  <label for="firstName" class="form-label">Ad *</label>
                  <input
                    type="text"
                    id="firstName"
                    class="form-control"
                    formControlName="firstName"
                    [class.is-invalid]="isFieldInvalid('firstName')"
                  >
                  <div *ngIf="isFieldInvalid('firstName')" class="invalid-feedback">
                    Ad alanı zorunludur.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="lastName" class="form-label">Soyad *</label>
                  <input
                    type="text"
                    id="lastName"
                    class="form-control"
                    formControlName="lastName"
                    [class.is-invalid]="isFieldInvalid('lastName')"
                  >
                  <div *ngIf="isFieldInvalid('lastName')" class="invalid-feedback">
                    Soyad alanı zorunludur.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="email" class="form-label">E-posta *</label>
                  <input
                    type="email"
                    id="email"
                    class="form-control"
                    formControlName="email"
                    [class.is-invalid]="isFieldInvalid('email')"
                    [readonly]="isEditMode()"
                  >
                  <div *ngIf="isFieldInvalid('email')" class="invalid-feedback">
                    Geçerli bir e-posta adresi giriniz.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="phoneNumber" class="form-label">Telefon</label>
                  <input
                    type="tel"
                    id="phoneNumber"
                    class="form-control"
                    formControlName="phoneNumber"
                    placeholder="+90 555 123 45 67"
                  >
                </div>

                <div *ngIf="!isEditMode()" class="mb-3">
                  <label for="password" class="form-label">Şifre *</label>
                  <input
                    type="password"
                    id="password"
                    class="form-control"
                    formControlName="password"
                    [class.is-invalid]="isFieldInvalid('password')"
                  >
                  <div *ngIf="isFieldInvalid('password')" class="invalid-feedback">
                    Şifre en az 6 karakter olmalıdır.
                  </div>
                </div>

                <div *ngIf="isEditMode()" class="mb-3">
                  <div class="form-check">
                    <input
                      type="checkbox"
                      id="isActive"
                      class="form-check-input"
                      formControlName="isActive"
                    >
                    <label for="isActive" class="form-check-label">
                      Kullanıcı aktif
                    </label>
                  </div>
                </div>
              </div>

              <!-- Roles -->
              <div class="col-md-6">
                <h5 class="mb-3">Roller</h5>

                <div *ngIf="availableRoles().length === 0" class="alert alert-info">
                  Henüz rol tanımlanmamış.
                </div>

                <div *ngFor="let role of availableRoles()" class="form-check mb-2">
                  <input
                    type="checkbox"
                    [id]="'role-' + role.id"
                    class="form-check-input"
                    [value]="role.id"
                    (change)="onRoleChange(role.id, $event)"
                    [checked]="selectedRoleIds().includes(role.id)"
                  >
                  <label [for]="'role-' + role.id" class="form-check-label">
                    <strong>{{ role.name }}</strong>
                    <div *ngIf="role.description" class="text-muted small">
                      {{ role.description }}
                    </div>
                    <div class="text-muted small">
                      {{ role.permissions.length }} izin
                    </div>
                  </label>
                </div>

                <div *ngIf="selectedRoleIds().length === 0" class="alert alert-warning mt-3">
                  <i class="fas fa-exclamation-triangle me-2"></i>
                  En az bir rol seçmelisiniz.
                </div>

                <!-- Groups -->
                <h5 class="mb-3 mt-4">Gruplar</h5>

                <div *ngIf="availableGroups().length === 0" class="alert alert-info">
                  Henüz grup tanımlanmamış.
                </div>

                <div *ngFor="let group of availableGroups()" class="form-check mb-2 group-check">
                  <input
                    type="checkbox"
                    [id]="'group-' + group.id"
                    class="form-check-input"
                    [value]="group.id"
                    (change)="onGroupChange(group.id, $event)"
                    [checked]="selectedGroupIds().includes(group.id)"
                  >
                  <label [for]="'group-' + group.id" class="form-check-label">
                    <div class="d-flex align-items-center">
                      <div
                        class="group-color-indicator me-2"
                        [style.background-color]="group.color || '#6c757d'"
                      ></div>
                      <div>
                        <strong>{{ group.name }}</strong>
                        <div *ngIf="group.description" class="text-muted small">
                          {{ group.description }}
                        </div>
                        <div class="text-muted small">
                          {{ group.memberCount }} üye
                        </div>
                      </div>
                    </div>
                  </label>
                </div>
              </div>
            </div>

            <!-- Actions -->
            <div class="d-flex justify-content-end gap-2 mt-4">
              <button type="button" class="btn btn-outline-secondary" (click)="goBack()">
                İptal
              </button>
              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="userForm.invalid || selectedRoleIds().length === 0 || saving()"
              >
                <span *ngIf="saving()" class="spinner-border spinner-border-sm me-2"></span>
                {{ isEditMode() ? 'Güncelle' : 'Oluştur' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .form-check {
      padding: 0.75rem;
      border: 1px solid #dee2e6;
      border-radius: 0.375rem;
      background-color: #f8f9fa;
    }

    .form-check:hover {
      background-color: #e9ecef;
    }

    .form-check-input:checked + .form-check-label {
      color: #0d6efd;
    }

    .card {
      border: none;
      box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
    }

    .group-check .group-color-indicator {
      width: 12px;
      height: 12px;
      border-radius: 50%;
      flex-shrink: 0;
    }
  `]
})
export class UserFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly userService = inject(UserService);
  private readonly roleService = inject(RoleService);
  private readonly groupService = inject(GroupService);

  // State
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  user = signal<User | null>(null);
  availableRoles = signal<Role[]>([]);
  selectedRoleIds = signal<string[]>([]);
  availableGroups = signal<Group[]>([]);
  selectedGroupIds = signal<string[]>([]);

  // Form
  userForm: FormGroup;

  // Computed
  isEditMode = signal(false);
  userId = signal<string | null>(null);

  constructor() {
    this.userForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      password: [''],
      isActive: [true]
    });
  }

  async ngOnInit(): Promise<void> {
    // Check if this is edit mode
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.userId.set(id);
    } else {
      // Add password validation for create mode
      this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    }

    await this.loadData();
  }

  private async loadData(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      // Load roles
      const roles = await this.roleService.getRoles().toPromise();
      if (roles) {
        this.availableRoles.set(roles.items || roles);
      }

      // Load groups
      const groups = await this.groupService.getGroups({ pageSize: 1000 }).toPromise();
      if (groups) {
        this.availableGroups.set(groups.items || []);
      }

      // Load user data if edit mode
      if (this.isEditMode() && this.userId()) {
        const user = await this.userService.getUser(this.userId()!).toPromise();
        if (user) {
          this.user.set(user);
          this.populateForm(user);
        }
      }
    } catch (error) {
      this.error.set('Veriler yüklenirken hata oluştu.');
      console.error('Data loading error:', error);
    } finally {
      this.loading.set(false);
    }
  }

  private populateForm(user: User): void {
    this.userForm.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      phoneNumber: user.phoneNumber,
      isActive: user.isActive
    });

    // Set selected roles
    const roleIds = user.roles.map(role => role.id);
    this.selectedRoleIds.set(roleIds);

    // Set selected groups
    if (user.groups) {
      const groupIds = user.groups.map(group => group.id);
      this.selectedGroupIds.set(groupIds);
    }
  }

  async onSubmit(): Promise<void> {
    if (this.userForm.invalid || this.selectedRoleIds().length === 0) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    try {
      const formValue = this.userForm.value;

      if (this.isEditMode()) {
        const request: UpdateUserRequest = {
          id: this.userId()!,
          firstName: formValue.firstName,
          lastName: formValue.lastName,
          phoneNumber: formValue.phoneNumber,
          isActive: formValue.isActive,
          roleIds: this.selectedRoleIds(),
          groupIds: this.selectedGroupIds()
        };

        await this.userService.updateUser(request).toPromise();
      } else {
        const request: CreateUserRequest = {
          email: formValue.email,
          firstName: formValue.firstName,
          lastName: formValue.lastName,
          phoneNumber: formValue.phoneNumber,
          password: formValue.password,
          roleIds: this.selectedRoleIds(),
          groupIds: this.selectedGroupIds()
        };

        await this.userService.createUser(request).toPromise();
      }

      // Success - navigate back
      this.goBack();
    } catch (error) {
      this.error.set(this.isEditMode() ? 'Kullanıcı güncellenirken hata oluştu.' : 'Kullanıcı oluşturulurken hata oluştu.');
      console.error('Save error:', error);
    } finally {
      this.saving.set(false);
    }
  }

  onRoleChange(roleId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const currentRoles = this.selectedRoleIds();

    if (checked) {
      this.selectedRoleIds.set([...currentRoles, roleId]);
    } else {
      this.selectedRoleIds.set(currentRoles.filter(id => id !== roleId));
    }
  }

  onGroupChange(groupId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const currentGroups = this.selectedGroupIds();

    if (checked) {
      this.selectedGroupIds.set([...currentGroups, groupId]);
    } else {
      this.selectedGroupIds.set(currentGroups.filter(id => id !== groupId));
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.userForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  goBack(): void {
    this.router.navigate(['/users']);
  }
}