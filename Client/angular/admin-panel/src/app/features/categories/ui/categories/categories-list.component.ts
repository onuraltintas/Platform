import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { UserAdminService, CategoryDto } from '../../../users/data-access/user-admin.service';

@Component({
  standalone: true,
  selector: 'app-categories-list',
  templateUrl: './categories-list.component.html',
  styleUrls: ['./categories-list.component.scss'],
  imports: [CommonModule, RouterModule, FormsModule]
})
export class CategoriesListComponent implements OnInit {
  private admin = inject(UserAdminService);
  private toastr = inject(ToastrService);

  loading = false;
  items: CategoryDto[] = [];
  error: string | null = null;

  // Filters
  query = '';
  isActiveFilter: 'all' | 'true' | 'false' = 'all';

  // Pagination state (client-side)
  page = 1;
  pageSize = 10;
  total = 0;

  confirmOpen = false;
  deleting = false;
  itemToDelete: CategoryDto | null = null;

  ngOnInit() { this.fetch(); }

  fetch() {
    this.loading = true;
    const isActiveParam = this.isActiveFilter === 'all' ? undefined : this.isActiveFilter === 'true';
    this.admin.listCategories({ search: this.query || undefined, isActive: isActiveParam, page: 1, pageSize: 1000 }).subscribe({
      next: (res: any) => { 
        this.items = Array.isArray(res) ? res : res.data; 
        this.total = this.items?.length ?? 0;
        const totalPages = this.getTotalPages();
        if (this.page > totalPages) this.page = Math.max(1, totalPages);
        this.loading = false; 
      },
      error: () => { this.error = 'Kategoriler yüklenemedi'; this.loading = false; }
    });
  }

  onSearch(e: Event) { e.preventDefault(); this.page = 1; this.fetch(); }
  onFilterChange() { this.page = 1; this.fetch(); }

  // Pagination helpers
  getTotalPages(): number { return Math.max(1, Math.ceil((this.total || 0) / (this.pageSize || 1))); }
  getPages(): number[] {
    const totalPages = this.getTotalPages();
    const current = this.page;
    const windowSize = 7;
    let start = Math.max(1, current - Math.floor(windowSize / 2));
    let end = start + windowSize - 1;
    if (end > totalPages) { end = totalPages; start = Math.max(1, end - windowSize + 1); }
    const pages: number[] = []; for (let p = start; p <= end; p++) pages.push(p); return pages;
  }
  getStartIndex(): number { if (!this.total) return 0; const start = (this.page - 1) * this.pageSize + 1; return Math.min(start, this.total); }
  getEndIndex(): number { if (!this.total) return 0; const end = this.page * this.pageSize; return Math.min(end, this.total); }
  onPageSizeChange(val: any) { const size = Number(val); if (!isNaN(size) && size > 0) { this.pageSize = size; this.page = 1; } }
  goToPage(p: number) { const totalPages = this.getTotalPages(); if (p >= 1 && p <= totalPages && p !== this.page) { this.page = p; } }
  prevPage() { if (this.page > 1) this.page -= 1; }
  nextPage() { if (this.page < this.getTotalPages()) this.page += 1; }
  firstPage() { if (this.page !== 1) this.page = 1; }
  lastPage() { const last = this.getTotalPages(); if (this.page !== last) this.page = last; }
  getPagedItems(): CategoryDto[] { const start = (this.page - 1) * this.pageSize; const end = start + this.pageSize; return this.items.slice(start, end); }

  openDeleteConfirm(item: CategoryDto) { this.itemToDelete = item; this.confirmOpen = true; }
  cancelDelete() { this.confirmOpen = false; this.itemToDelete = null; this.deleting = false; }
  confirmDelete() {
    if (!this.itemToDelete) return;
    this.deleting = true;
    this.admin.deleteCategory(this.itemToDelete.id).subscribe({
      next: () => { this.items = this.items.filter(i => i.id !== this.itemToDelete!.id); this.total = this.items.length; this.toastr.success('Kategori silindi'); this.cancelDelete(); },
      error: () => { this.toastr.error('Kategori silinemedi'); this.deleting = false; }
    });
  }
}