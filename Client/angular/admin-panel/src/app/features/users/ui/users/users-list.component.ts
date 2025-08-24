import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserAdminService, PagedUsersResponse, UserSummaryDto, RoleDto, CategoryDto } from '../../data-access/user-admin.service';

declare var coreui: any;

@Component({
  standalone: true,
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss'],
  imports: [CommonModule, RouterModule, FormsModule]
})
export class UsersListComponent {
  private admin = inject(UserAdminService);
  private router = inject(Router);

  query = signal('');
  page = signal(1);
  pageSize = signal(10);
  loading = signal(false);
  error = signal<string | null>(null);
  users = signal<UserSummaryDto[]>([]);
  total = signal(0);
  // filters
  roles = signal<RoleDto[]>([]);
  categories = signal<CategoryDto[]>([]);
  selectedRoleId = signal<string | null>(null);
  selectedCategoryId = signal<string | null>(null);
  isActiveFilter = signal<'all' | 'true' | 'false'>('all');
  isEmailConfirmedFilter = signal<'all' | 'true' | 'false'>('all');

  // Delete modal state
  deleteTargetId: string | null = null;
  deleteTargetName: string | null = null;
  private deleteModal: any;

  ngOnInit() {
    // preload lookups
    this.admin.listRoles({ page: 1, pageSize: 500 }).subscribe(r => {
      const data = (r as any).data ?? r; this.roles.set(data);
    });
    this.admin.listCategories({ page: 1, pageSize: 500 }).subscribe(r => {
      const data = (r as any).data ?? r; this.categories.set(data);
    });
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.admin.listUsers({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.query() || undefined,
        roleId: this.selectedRoleId() || undefined,
        categoryId: this.selectedCategoryId() || undefined,
        isActive: this.isActiveFilter() === 'all' ? undefined : this.isActiveFilter() === 'true',
        isEmailConfirmed: this.isEmailConfirmedFilter() === 'all' ? undefined : this.isEmailConfirmedFilter() === 'true'
      })
      .subscribe({
        next: (res) => {
          const data = (res as any).data ?? res;
          this.users.set(data.users ?? []);
          this.total.set(data.totalCount ?? 0);
        },
        error: (err) => {
          this.error.set(err?.message ?? 'Listeleme hatası');
        },
        complete: () => this.loading.set(false)
      });
  }

  onSearch(e: Event) {
    e.preventDefault();
    this.page.set(1);
    this.load();
  }

  onFilterChange() {
    this.page.set(1);
    this.load();
  }

  // Pagination helpers
  getTotalPages(): number {
    const total = this.total();
    const size = this.pageSize();
    return Math.max(1, Math.ceil((total || 0) / (size || 1)));
  }

  getPages(): number[] {
    const totalPages = this.getTotalPages();
    const current = this.page();
    const windowSize = 7;
    let start = Math.max(1, current - Math.floor(windowSize / 2));
    let end = start + windowSize - 1;
    if (end > totalPages) {
      end = totalPages;
      start = Math.max(1, end - windowSize + 1);
    }
    const pages: number[] = [];
    for (let p = start; p <= end; p++) pages.push(p);
    return pages;
  }

  // Displayed range helpers
  getStartIndex(): number {
    const total = this.total();
    if (!total) return 0;
    const start = (this.page() - 1) * this.pageSize() + 1;
    return Math.min(start, total);
  }

  getEndIndex(): number {
    const total = this.total();
    if (!total) return 0;
    const end = this.page() * this.pageSize();
    return Math.min(end, total);
  }

  onPageSizeChange(val: any) {
    const size = Number(val);
    if (!isNaN(size) && size > 0) {
      this.pageSize.set(size);
      this.page.set(1);
      this.load();
    }
  }

  goToPage(p: number) {
    const totalPages = this.getTotalPages();
    if (p >= 1 && p <= totalPages && p !== this.page()) {
      this.page.set(p);
      this.load();
    }
  }

  prevPage() { if (this.page() > 1) { this.page.set(this.page() - 1); this.load(); } }
  nextPage() { if (this.page() < this.getTotalPages()) { this.page.set(this.page() + 1); this.load(); } }

  firstPage() { if (this.page() !== 1) { this.page.set(1); this.load(); } }
  lastPage() { const last = this.getTotalPages(); if (this.page() !== last) { this.page.set(last); this.load(); } }

  openNew() { this.router.navigate(['/users/new']); }
  openDetail(id: string) { this.router.navigate(['/users', id]); }

  openDeleteModal(e: Event, id: string, name: string) {
    e.stopPropagation();
    this.deleteTargetId = id;
    this.deleteTargetName = name;
    const el = document.getElementById('confirmDeleteModal');
    if (el) {
      this.deleteModal = new coreui.Modal(el);
      this.deleteModal.show();
    }
  }

  confirmDelete() {
    if (!this.deleteTargetId) return;
    this.loading.set(true);
    this.admin.deleteUser(this.deleteTargetId).subscribe({
      next: () => {
        this.closeDeleteModal();
        this.load();
      },
      error: (err) => {
        this.error.set(err?.message ?? 'Silme hatası');
        this.loading.set(false);
        this.closeDeleteModal();
      }
    });
  }

  closeDeleteModal() {
    if (this.deleteModal) {
      this.deleteModal.hide();
    }
    this.deleteTargetId = null;
    this.deleteTargetName = null;
  }

  editUser(e: Event, id: string) {
    e.stopPropagation();
    this.router.navigate(['/users', id]);
  }
}

