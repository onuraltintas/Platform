import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { GroupService } from '../../services/group.service';
import { Group, ListQuery } from '../../models/simple.models';

@Component({
  selector: 'app-group-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="group-list-container">
      <!-- Header -->
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>Grup Yönetimi</h2>
        <a routerLink="/groups/create" class="btn btn-primary">
          <i class="fas fa-plus me-2"></i>
          Yeni Grup
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
                placeholder="Grup ara..."
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

      <!-- Stats Cards -->
      <div class="row mb-4">
        <div class="col-md-3">
          <div class="card bg-primary text-white">
            <div class="card-body text-center">
              <h5 class="card-title">{{ totalCount() }}</h5>
              <p class="card-text">Toplam Grup</p>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="card bg-success text-white">
            <div class="card-body text-center">
              <h5 class="card-title">{{ activeGroups() }}</h5>
              <p class="card-text">Aktif Grup</p>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="card bg-info text-white">
            <div class="card-body text-center">
              <h5 class="card-title">{{ totalMembers() }}</h5>
              <p class="card-text">Toplam Üye</p>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="card bg-warning text-white">
            <div class="card-body text-center">
              <h5 class="card-title">{{ averageMembersPerGroup() }}</h5>
              <p class="card-text">Ortalama Üye</p>
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

      <!-- Groups Grid -->
      <div *ngIf="!loading() && !error()" class="row">
        <div *ngFor="let group of groups()" class="col-md-6 col-lg-4 mb-4">
          <div class="card group-card h-100" [class.inactive]="!group.isActive">
            <div class="card-header d-flex justify-content-between align-items-center">
              <div class="d-flex align-items-center">
                <div
                  class="group-color-indicator me-2"
                  [style.background-color]="group.color || '#6c757d'"
                ></div>
                <h6 class="mb-0">{{ group.name }}</h6>
              </div>
              <span class="badge" [class]="group.isActive ? 'bg-success' : 'bg-danger'">
                {{ group.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </div>

            <div class="card-body">
              <p class="card-text text-muted" *ngIf="group.description">
                {{ group.description }}
              </p>
              <p class="card-text text-muted" *ngIf="!group.description">
                <em>Açıklama bulunmuyor</em>
              </p>

              <div class="group-stats">
                <div class="stat-item">
                  <i class="fas fa-users text-primary me-2"></i>
                  <span><strong>{{ group.memberCount }}</strong> üye</span>
                </div>
                <div class="stat-item">
                  <i class="fas fa-calendar text-muted me-2"></i>
                  <span>{{ formatDate(group.createdAt) }}</span>
                </div>
              </div>
            </div>

            <div class="card-footer">
              <div class="btn-group w-100">
                <a
                  [routerLink]="['/groups', group.id]"
                  class="btn btn-outline-primary btn-sm"
                  title="Görüntüle"
                >
                  <i class="fas fa-eye me-1"></i>
                  Görüntüle
                </a>
                <a
                  [routerLink]="['/groups', group.id, 'edit']"
                  class="btn btn-outline-warning btn-sm"
                  title="Düzenle"
                >
                  <i class="fas fa-edit me-1"></i>
                  Düzenle
                </a>
                <button
                  class="btn btn-outline-secondary btn-sm"
                  (click)="toggleGroupStatus(group)"
                  [title]="group.isActive ? 'Pasifleştir' : 'Aktifleştir'"
                >
                  <i [class]="group.isActive ? 'fas fa-ban' : 'fas fa-check'"></i>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div *ngIf="groups().length === 0" class="col-12">
          <div class="text-center py-5">
            <i class="fas fa-users fa-3x text-muted mb-3"></i>
            <h4 class="text-muted">Henüz grup bulunmuyor</h4>
            <p class="text-muted">İlk grubunuzu oluşturmak için "Yeni Grup" butonuna tıklayın.</p>
            <a routerLink="/groups/create" class="btn btn-primary">
              <i class="fas fa-plus me-2"></i>
              İlk Grubunu Oluştur
            </a>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <nav *ngIf="totalPages() > 1" aria-label="Sayfa navigasyonu" class="mt-4">
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
      <div class="text-center text-muted mt-3" *ngIf="groups().length > 0">
        Toplam {{ totalCount() }} gruptan {{ groups().length }} tanesi gösteriliyor
      </div>
    </div>
  `,
  styles: [`
    .group-card {
      transition: all 0.3s ease;
    }

    .group-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }

    .group-card.inactive {
      opacity: 0.7;
    }

    .group-color-indicator {
      width: 12px;
      height: 12px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .group-stats {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .stat-item {
      display: flex;
      align-items: center;
      font-size: 0.9rem;
    }

    .card {
      border: none;
      box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
    }

    .card-header {
      background-color: #f8f9fa;
      border-bottom: 1px solid #dee2e6;
    }

    .btn-group .btn {
      flex: 1;
    }

    .bg-primary, .bg-success, .bg-info, .bg-warning {
      border: none;
    }
  `]
})
export class GroupListComponent implements OnInit {
  private readonly groupService = inject(GroupService);

  // State signals
  groups = signal<Group[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Pagination state
  currentPage = signal(1);
  pageSize = signal(12); // Grid layout works better with 12
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));

  // Search state
  searchText = '';
  private searchTimeout: any;

  // Computed statistics
  activeGroups = computed(() =>
    this.groups().filter(group => group.isActive).length
  );

  totalMembers = computed(() =>
    this.groups().reduce((sum, group) => sum + group.memberCount, 0)
  );

  averageMembersPerGroup = computed(() => {
    const total = this.totalMembers();
    const count = this.groups().length;
    return count > 0 ? Math.round(total / count) : 0;
  });

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
    this.loadGroups();
  }

  async loadGroups(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const query: ListQuery = {
        page: this.currentPage(),
        pageSize: this.pageSize(),
        search: this.searchText || undefined,
        sortBy: 'name',
        sortDirection: 'asc'
      };

      const result = await this.groupService.getGroups(query).toPromise();

      if (result) {
        this.groups.set(result.items);
        this.totalCount.set(result.totalCount);
      }
    } catch (error) {
      this.error.set('Gruplar yüklenirken bir hata oluştu.');
      console.error('Group loading error:', error);
    } finally {
      this.loading.set(false);
    }
  }

  onSearch(): void {
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.currentPage.set(1);
      this.loadGroups();
    }, 500);
  }

  onPageSizeChange(): void {
    this.currentPage.set(1);
    this.loadGroups();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadGroups();
    }
  }

  clearFilters(): void {
    this.searchText = '';
    this.currentPage.set(1);
    this.pageSize.set(12);
    this.loadGroups();
  }

  async toggleGroupStatus(group: Group): Promise<void> {
    try {
      await this.groupService.toggleGroupStatus(group.id, !group.isActive).toPromise();
      // Update local state
      const updatedGroups = this.groups().map(g =>
        g.id === group.id ? { ...g, isActive: !g.isActive } : g
      );
      this.groups.set(updatedGroups);
    } catch (error) {
      this.error.set('Grup durumu güncellenirken hata oluştu.');
      console.error('Toggle group status error:', error);
    }
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}