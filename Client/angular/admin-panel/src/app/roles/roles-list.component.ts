import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserAdminService, RoleDto } from '../services/user-admin.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  standalone: true,
  selector: 'app-roles-list',
  templateUrl: './roles-list.component.html',
  styleUrls: ['./roles-list.component.scss'],
  imports: [CommonModule, RouterModule, FormsModule]
})
export class RolesListComponent implements OnInit {
  private admin = inject(UserAdminService);
  private toastr = inject(ToastrService);

  loading = false;
  roles: RoleDto[] = [];
  error: string | null = null;

  // Filters
  query = '';
  isActiveFilter: 'all' | 'true' | 'false' = 'all';

  // Pagination state (client-side pagination)
  page = 1;
  pageSize = 10;
  total = 0;

  // Delete modal state
  confirmOpen = false;
  deleting = false;
  roleToDelete: RoleDto | null = null;

  ngOnInit() {
    this.fetch();
  }

  fetch() {
    this.loading = true;
    this.error = null;
    const isActiveParam = this.isActiveFilter === 'all' ? undefined : this.isActiveFilter === 'true';
    this.admin.listRoles({ search: this.query || undefined, isActive: isActiveParam, page: 1, pageSize: 1000 }).subscribe({
      next: (res: any) => {
        this.roles = Array.isArray(res) ? res : res.data;
        this.total = this.roles?.length ?? 0;
        // Page out of bounds guard
        const totalPages = this.getTotalPages();
        if (this.page > totalPages) this.page = Math.max(1, totalPages);
        this.loading = false;
      },
      error: () => {
        this.error = 'Roller yüklenemedi';
        this.loading = false;
      }
    });
  }

  onSearch(e: Event) {
    e.preventDefault();
    this.page = 1;
    this.fetch();
  }

  onFilterChange() {
    this.page = 1;
    this.fetch();
  }

  // Client-side pagination helpers
  getTotalPages(): number {
    return Math.max(1, Math.ceil((this.total || 0) / (this.pageSize || 1)));
  }

  getPages(): number[] {
    const totalPages = this.getTotalPages();
    const current = this.page;
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

  getStartIndex(): number {
    if (!this.total) return 0;
    const start = (this.page - 1) * this.pageSize + 1;
    return Math.min(start, this.total);
  }

  getEndIndex(): number {
    if (!this.total) return 0;
    const end = this.page * this.pageSize;
    return Math.min(end, this.total);
  }

  onPageSizeChange(val: any) {
    const size = Number(val);
    if (!isNaN(size) && size > 0) {
      this.pageSize = size;
      this.page = 1;
      // client-side slice, no reload needed
    }
  }

  goToPage(p: number) {
    const totalPages = this.getTotalPages();
    if (p >= 1 && p <= totalPages && p !== this.page) {
      this.page = p;
    }
  }

  prevPage() { if (this.page > 1) { this.page -= 1; } }
  nextPage() { if (this.page < this.getTotalPages()) { this.page += 1; } }
  firstPage() { if (this.page !== 1) { this.page = 1; } }
  lastPage() { const last = this.getTotalPages(); if (this.page !== last) { this.page = last; } }

  getPagedRoles(): RoleDto[] {
    const start = (this.page - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.roles.slice(start, end);
  }

  openDeleteConfirm(role: RoleDto) {
    this.roleToDelete = role;
    this.confirmOpen = true;
  }

  cancelDelete() {
    this.confirmOpen = false;
    this.roleToDelete = null;
    this.deleting = false;
  }

  confirmDelete() {
    if (!this.roleToDelete) return;
    this.deleting = true;
    this.admin.deleteRole(this.roleToDelete.id).subscribe({
      next: () => {
        this.roles = this.roles.filter(r => r.id !== this.roleToDelete!.id);
        this.total = this.roles.length;
        this.toastr.success('Rol silindi');
        this.cancelDelete();
      },
      error: () => {
        this.toastr.error('Rol silinemedi');
        this.deleting = false;
      }
    });
  }
}