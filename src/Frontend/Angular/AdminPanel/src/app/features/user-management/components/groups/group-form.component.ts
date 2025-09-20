import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';

import { GroupService } from '../../services/group.service';
import { UserService } from '../../services/user.service';
import { Group, User, CreateGroupRequest, UpdateGroupRequest } from '../../models/simple.models';

@Component({
  selector: 'app-group-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  template: `
    <div class="group-form-container">
      <!-- Header -->
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>{{ isEditMode() ? 'Grup Düzenle' : 'Yeni Grup' }}</h2>
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
          <form [formGroup]="groupForm" (ngSubmit)="onSubmit()">
            <div class="row">
              <!-- Basic Info -->
              <div class="col-md-6">
                <h5 class="mb-3">Temel Bilgiler</h5>

                <div class="mb-3">
                  <label for="name" class="form-label">Grup Adı *</label>
                  <input
                    type="text"
                    id="name"
                    class="form-control"
                    formControlName="name"
                    [class.is-invalid]="isFieldInvalid('name')"
                    placeholder="Ör. Geliştiriciler, Yöneticiler"
                  >
                  <div *ngIf="isFieldInvalid('name')" class="invalid-feedback">
                    Grup adı alanı zorunludur.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="description" class="form-label">Açıklama</label>
                  <textarea
                    id="description"
                    class="form-control"
                    formControlName="description"
                    rows="3"
                    placeholder="Grup hakkında kısa açıklama..."
                  ></textarea>
                </div>

                <!-- Color Selection -->
                <div class="mb-3">
                  <label class="form-label">Grup Rengi</label>
                  <div class="color-selection">
                    <div
                      *ngFor="let color of availableColors"
                      class="color-option"
                      [style.background-color]="color"
                      [class.selected]="selectedColor() === color"
                      (click)="selectColor(color)"
                      [title]="getColorName(color)"
                    ></div>
                  </div>
                  <small class="form-text text-muted">
                    Grupları görsel olarak ayırt etmek için bir renk seçin
                  </small>
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
                      Grup aktif
                    </label>
                  </div>
                </div>
              </div>

              <!-- User Assignment -->
              <div class="col-md-6">
                <h5 class="mb-3">Üye Atama</h5>

                <!-- Search Users -->
                <div class="mb-3">
                  <input
                    type="text"
                    class="form-control"
                    placeholder="Kullanıcı ara..."
                    [(ngModel)]="userSearchText"
                    [ngModelOptions]="{standalone: true}"
                    (input)="onUserSearch()"
                  >
                </div>

                <!-- Selected Users -->
                <div *ngIf="selectedUserIds().length > 0" class="mb-3">
                  <label class="form-label">Seçilen Üyeler ({{ selectedUserIds().length }})</label>
                  <div class="selected-users">
                    <span
                      *ngFor="let user of getSelectedUsers()"
                      class="badge bg-primary me-2 mb-2 user-badge"
                    >
                      {{ user.firstName }} {{ user.lastName }}
                      <button
                        type="button"
                        class="btn-close btn-close-white ms-2"
                        (click)="removeUser(user.id)"
                        [attr.aria-label]="'Remove ' + user.firstName"
                      ></button>
                    </span>
                  </div>
                </div>

                <!-- Available Users -->
                <div class="available-users">
                  <label class="form-label">Kullanılabilir Kullanıcılar</label>
                  <div class="users-list">
                    <div
                      *ngFor="let user of filteredUsers()"
                      class="user-item"
                      [class.selected]="selectedUserIds().includes(user.id)"
                      (click)="toggleUser(user.id)"
                    >
                      <div class="user-info">
                        <div class="user-name">{{ user.firstName }} {{ user.lastName }}</div>
                        <div class="user-email">{{ user.email }}</div>
                      </div>
                      <div class="user-status">
                        <span *ngIf="selectedUserIds().includes(user.id)" class="text-success">
                          <i class="fas fa-check"></i>
                        </span>
                        <span *ngFor="let role of user.roles" class="badge bg-secondary ms-1">
                          {{ role.name }}
                        </span>
                      </div>
                    </div>

                    <div *ngIf="filteredUsers().length === 0" class="text-center text-muted py-3">
                      <i class="fas fa-search me-2"></i>
                      Kullanıcı bulunamadı
                    </div>
                  </div>
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
                [disabled]="groupForm.invalid || saving()"
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
    .color-selection {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
    }

    .color-option {
      width: 30px;
      height: 30px;
      border-radius: 50%;
      cursor: pointer;
      border: 3px solid transparent;
      transition: all 0.2s ease;
    }

    .color-option:hover {
      transform: scale(1.1);
      border-color: #dee2e6;
    }

    .color-option.selected {
      transform: scale(1.1);
      border-color: #0d6efd;
      box-shadow: 0 0 0 2px rgba(13, 110, 253, 0.25);
    }

    .users-list {
      max-height: 300px;
      overflow-y: auto;
      border: 1px solid #dee2e6;
      border-radius: 0.375rem;
      background: #f8f9fa;
    }

    .user-item {
      display: flex;
      justify-content: between;
      align-items: center;
      padding: 0.75rem;
      border-bottom: 1px solid #dee2e6;
      cursor: pointer;
      transition: background-color 0.2s ease;
    }

    .user-item:hover {
      background-color: #e9ecef;
    }

    .user-item.selected {
      background-color: #d1ecf1;
      border-color: #bee5eb;
    }

    .user-item:last-child {
      border-bottom: none;
    }

    .user-info {
      flex-grow: 1;
    }

    .user-name {
      font-weight: 500;
      color: #495057;
    }

    .user-email {
      font-size: 0.875rem;
      color: #6c757d;
    }

    .user-status {
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }

    .user-badge {
      display: inline-flex;
      align-items: center;
    }

    .user-badge .btn-close {
      font-size: 0.7rem;
      margin-left: 0.5rem;
    }

    .selected-users {
      max-height: 150px;
      overflow-y: auto;
      padding: 0.5rem;
      border: 1px solid #dee2e6;
      border-radius: 0.375rem;
      background-color: #f8f9fa;
    }

    .card {
      border: none;
      box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
    }
  `]
})
export class GroupFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly groupService = inject(GroupService);
  private readonly userService = inject(UserService);

  // State
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  group = signal<Group | null>(null);
  availableUsers = signal<User[]>([]);
  selectedUserIds = signal<string[]>([]);
  selectedColor = signal<string>('#007bff');

  // Form
  groupForm: FormGroup;

  // Computed
  isEditMode = signal(false);
  groupId = signal<string | null>(null);

  // User search
  userSearchText = '';
  filteredUsers = signal<User[]>([]);

  // Available colors
  availableColors = this.groupService.getAvailableColors();

  constructor() {
    this.groupForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: [''],
      isActive: [true]
    });
  }

  async ngOnInit(): Promise<void> {
    // Check if this is edit mode
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.groupId.set(id);
    }

    await this.loadData();
  }

  private async loadData(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      // Load users
      const usersResult = await this.userService.getUsers({ pageSize: 1000 }).toPromise();
      if (usersResult) {
        this.availableUsers.set(usersResult.items || []);
        this.filteredUsers.set(usersResult.items || []);
      }

      // Load group data if edit mode
      if (this.isEditMode() && this.groupId()) {
        const group = await this.groupService.getGroup(this.groupId()!).toPromise();
        if (group) {
          this.group.set(group);
          this.populateForm(group);
        }
      }
    } catch (error) {
      this.error.set('Veriler yüklenirken hata oluştu.');
      console.error('Data loading error:', error);
    } finally {
      this.loading.set(false);
    }
  }

  private populateForm(group: Group): void {
    this.groupForm.patchValue({
      name: group.name,
      description: group.description,
      isActive: group.isActive
    });

    // Set selected color
    if (group.color) {
      this.selectedColor.set(group.color);
    }

    // Set selected users
    if (group.users) {
      const userIds = group.users.map(user => user.id);
      this.selectedUserIds.set(userIds);
    }
  }

  selectColor(color: string): void {
    this.selectedColor.set(color);
  }

  onUserSearch(): void {
    const searchText = this.userSearchText.toLowerCase();
    if (!searchText) {
      this.filteredUsers.set(this.availableUsers());
    } else {
      const filtered = this.availableUsers().filter(user =>
        user.firstName.toLowerCase().includes(searchText) ||
        user.lastName.toLowerCase().includes(searchText) ||
        user.email.toLowerCase().includes(searchText)
      );
      this.filteredUsers.set(filtered);
    }
  }

  toggleUser(userId: string): void {
    const currentIds = this.selectedUserIds();
    if (currentIds.includes(userId)) {
      this.selectedUserIds.set(currentIds.filter(id => id !== userId));
    } else {
      this.selectedUserIds.set([...currentIds, userId]);
    }
  }

  removeUser(userId: string): void {
    this.selectedUserIds.set(this.selectedUserIds().filter(id => id !== userId));
  }

  getSelectedUsers(): User[] {
    const selectedIds = this.selectedUserIds();
    return this.availableUsers().filter(user => selectedIds.includes(user.id));
  }

  async onSubmit(): Promise<void> {
    if (this.groupForm.invalid) {
      this.groupForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    try {
      const formValue = this.groupForm.value;

      if (this.isEditMode()) {
        const request: UpdateGroupRequest = {
          id: this.groupId()!,
          name: formValue.name,
          description: formValue.description,
          color: this.selectedColor(),
          isActive: formValue.isActive,
          userIds: this.selectedUserIds()
        };

        await this.groupService.updateGroup(request).toPromise();
      } else {
        const request: CreateGroupRequest = {
          name: formValue.name,
          description: formValue.description,
          color: this.selectedColor(),
          userIds: this.selectedUserIds()
        };

        await this.groupService.createGroup(request).toPromise();
      }

      // Success - navigate back
      this.goBack();
    } catch (error) {
      this.error.set(this.isEditMode() ? 'Grup güncellenirken hata oluştu.' : 'Grup oluşturulurken hata oluştu.');
      console.error('Save error:', error);
    } finally {
      this.saving.set(false);
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.groupForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getColorName(color: string): string {
    const colorNames: { [key: string]: string } = {
      '#007bff': 'Mavi',
      '#28a745': 'Yeşil',
      '#dc3545': 'Kırmızı',
      '#ffc107': 'Sarı',
      '#6f42c1': 'Mor',
      '#fd7e14': 'Turuncu',
      '#20c997': 'Teal',
      '#e83e8c': 'Pembe',
      '#6c757d': 'Gri',
      '#17a2b8': 'Cyan'
    };
    return colorNames[color] || 'Bilinmeyen';
  }

  goBack(): void {
    this.router.navigate(['/groups']);
  }
}