import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { UserService } from '../../services/user.service';
import { User, ListQuery } from '../../models/simple.models';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="user-list-container">
      <!-- Header -->
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>Kullanıcı Yönetimi</h2>
        <a routerLink="/users/create" class="btn btn-primary">
          <i class="fas fa-plus me-2"></i>
          Yeni Kullanıcı
        </a>
      </div>

      <!-- Search and Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row">
            <div class="col-md-6">
              <input
                type="text"
                class="form-control"
                placeholder="Kullanıcı ara..."
                [(ngModel)]="searchText"
                (input)="onSearch()"
              >
            </div>
            <div class="col-md-3">
              <select class="form-select" [(ngModel)]="pageSize" (change)="onPageSizeChange()">
                <option value="10">10 kayıt</option>
                <option value="25">25 kayıt</option>
                <option value="50">50 kayıt</option>
              </select>
            </div>
            <div class="col-md-3">
              <button class="btn btn-outline-secondary w-100" (click)="clearFilters()">
                <i class="fas fa-times me-2"></i>
                Temizle
              </button>
            </div>
          </div>
        </div>
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

      <!-- Users Table -->
      <div *ngIf="!loading() && !error()" class="card">
        <div class="card-body">
          <div class="table-responsive">
            <table class="table table-hover">
              <thead>
                <tr>
                  <th>Ad Soyad</th>
                  <th>E-posta</th>
                  <th>Telefon</th>
                  <th>Roller</th>
                  <th>Durum</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let user of users()">
                  <td>
                    <div class="d-flex align-items-center">
                      <div class="avatar me-3">
                        {{ getInitials(user) }}
                      </div>
                      <div>
                        <div class="fw-semibold">{{ user.firstName }} {{ user.lastName }}</div>
                        <small class="text-muted">ID: {{ user.id.substring(0, 8) }}...</small>
                      </div>
                    </div>
                  </td>
                  <td>
                    {{ user.email }}
                    <span *ngIf="!user.emailConfirmed" class="badge bg-warning ms-2">
                      E-posta onaylanmamış
                    </span>
                  </td>
                  <td>{{ user.phoneNumber || '-' }}</td>
                  <td>
                    <span *ngFor="let role of user.roles" class="badge bg-secondary me-1">
                      {{ role.name }}
                    </span>
                  </td>
                  <td>
                    <span class="badge" [class]="user.isActive ? 'bg-success' : 'bg-danger'">
                      {{ user.isActive ? 'Aktif' : 'Pasif' }}
                    </span>
                  </td>
                  <td>
                    <div class="btn-group btn-group-sm">
                      <a [routerLink]="['/users', user.id]" class="btn btn-outline-primary" title="Görüntüle">
                        <i class="fas fa-eye"></i>
                      </a>
                      <a [routerLink]="['/users', user.id, 'edit']" class="btn btn-outline-warning" title="Düzenle">
                        <i class="fas fa-edit"></i>
                      </a>
                      <button
                        class="btn btn-outline-secondary"
                        (click)="toggleUserStatus(user)"
                        [title]="user.isActive ? 'Pasifleştir' : 'Aktifleştir'"
                      >
                        <i [class]="user.isActive ? 'fas fa-ban' : 'fas fa-check'"></i>
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Pagination -->
          <nav *ngIf="totalPages() > 1" aria-label="Sayfa navigasyonu">
            <ul class="pagination justify-content-center">
              <li class="page-item" [class.disabled]="currentPage() === 1">
                <button class="page-link" (click)="goToPage(currentPage() - 1)">Önceki</button>
              </li>

              <li
                *ngFor="let page of visiblePages()"
                class="page-item"
                [class.active]="page === currentPage()"
              >
                <button class="page-link" (click)="goToPage(page)">{{ page }}</button>
              </li>

              <li class="page-item" [class.disabled]="currentPage() === totalPages()">
                <button class="page-link" (click)="goToPage(currentPage() + 1)">Sonraki</button>
              </li>
            </ul>
          </nav>

          <!-- Results Info -->
          <div class="text-center text-muted mt-3">
            Toplam {{ totalCount() }} kullanıcıdan {{ users().length }} tanesi gösteriliyor
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #6c757d;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
      font-size: 14px;
    }

    .table th {
      border-top: none;
      font-weight: 600;
      color: #495057;
    }

    .btn-group-sm .btn {
      padding: 0.25rem 0.5rem;
    }

    .card {
      border: none;
      box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
    }
  `]
})
export class UserListComponent implements OnInit {
  private readonly userService = inject(UserService);

  // State signals
  users = signal<User[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Pagination state
  currentPage = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));

  // Search state
  searchText = '';
  private searchTimeout: any;

  // Computed
  visiblePages = computed(() => {
    const current = this.currentPage();
    const total = this.totalPages();
    const pages: number[] = [];

    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  async loadUsers(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const query: ListQuery = {
        page: this.currentPage(),
        pageSize: this.pageSize(),
        search: this.searchText || undefined,
        sortBy: 'firstName',
        sortDirection: 'asc'
      };

      const result = await this.userService.getUsers(query).toPromise();

      if (result) {
        this.users.set(result.items);
        this.totalCount.set(result.totalCount);
      }
    } catch (error) {
      this.error.set('Kullanıcılar yüklenirken bir hata oluştu.');
      console.error('User loading error:', error);
    } finally {
      this.loading.set(false);
    }
  }

  onSearch(): void {
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.currentPage.set(1);
      this.loadUsers();
    }, 500);
  }

  onPageSizeChange(): void {
    this.currentPage.set(1);
    this.loadUsers();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadUsers();
    }
  }

  clearFilters(): void {
    this.searchText = '';
    this.currentPage.set(1);
    this.pageSize.set(10);
    this.loadUsers();
  }

  async toggleUserStatus(user: User): Promise<void> {
    try {
      await this.userService.toggleUserStatus(user.id, !user.isActive).toPromise();
      // Update local state
      const updatedUsers = this.users().map(u =>
        u.id === user.id ? { ...u, isActive: !u.isActive } : u
      );
      this.users.set(updatedUsers);
    } catch (error) {
      this.error.set('Kullanıcı durumu güncellenirken hata oluştu.');
      console.error('Toggle user status error:', error);
    }
  }

  getInitials(user: User): string {
    return (user.firstName.charAt(0) + user.lastName.charAt(0)).toUpperCase();
  }
}