import { Component, input, output, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { TableColumn, ActionButton, BulkAction, FilterGroup } from '../../../features/user-management/models/user-management.models';

export interface DataTableConfig {
  columns: TableColumn[];
  actions?: ActionButton[];
  bulkActions?: BulkAction[];
  selectable?: boolean;
  searchable?: boolean;
  filterable?: boolean;
  exportable?: boolean;
  showCheckbox?: boolean;
  showActions?: boolean;
  pageSize?: number;
  pageSizeOptions?: number[];
}

export interface PaginationInfo {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="data-table-container">
      <!-- Table Header Controls -->
      <div class="table-controls" *ngIf="config().searchable || config().filterable || config().exportable">
        <div class="row align-items-center">
          <div class="col-md-6">
            <!-- Search -->
            <div class="search-container" *ngIf="config().searchable">
              <div class="input-group">
                <span class="input-group-text">
                  <i class="fas fa-search"></i>
                </span>
                <input
                  type="text"
                  class="form-control"
                  placeholder="Ara..."
                  [ngModel]="searchTerm()"
                  (ngModelChange)="onSearchChange($event)"
                  (keyup.enter)="search.emit(searchTerm())"
                >
                <button
                  type="button"
                  class="btn btn-outline-secondary"
                  (click)="clearSearch()"
                  [disabled]="!searchTerm()"
                >
                  <i class="fas fa-times"></i>
                </button>
              </div>
            </div>
          </div>

          <div class="col-md-6">
            <div class="d-flex justify-content-end gap-2">
              <!-- Filters Toggle -->
              <button
                type="button"
                class="btn btn-outline-secondary"
                *ngIf="config().filterable && filters().length > 0"
                (click)="toggleFilters()"
                [class.active]="showFilters()"
              >
                <i class="fas fa-filter me-2"></i>
                Filtreler
                <span class="badge bg-primary ms-2" *ngIf="activeFiltersCount() > 0">
                  {{ activeFiltersCount() }}
                </span>
              </button>

              <!-- Export Button -->
              <button
                type="button"
                class="btn btn-outline-success"
                *ngIf="config().exportable"
                (click)="export.emit()"
              >
                <i class="fas fa-download me-2"></i>
                Dışa Aktar
              </button>

              <!-- Bulk Actions -->
              <div class="dropdown" *ngIf="config().bulkActions && (config().bulkActions?.length || 0) > 0 && selectedItems().length > 0">
                <button
                  type="button"
                  class="btn btn-primary dropdown-toggle"
                  data-bs-toggle="dropdown"
                >
                  <i class="fas fa-list me-2"></i>
                  Toplu İşlemler ({{ selectedItems().length }})
                </button>
                <ul class="dropdown-menu">
                  <li *ngFor="let action of config().bulkActions">
                    <a
                      class="dropdown-item"
                      href="javascript:void(0)"
                      (click)="onBulkAction(action)"
                    >
                      <i [class]="action.icon + ' me-2'"></i>
                      {{ action.label }}
                    </a>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </div>

        <!-- Filters Panel -->
        <div class="filters-panel collapse" [class.show]="showFilters()" *ngIf="config().filterable">
          <div class="card mt-3">
            <div class="card-body">
              <div class="row">
                <div class="col-md-3" *ngFor="let filter of filters()">
                  <div class="mb-3">
                    <label class="form-label">{{ filter.label }}</label>

                    <!-- Select Filter -->
                    <select
                      class="form-select"
                      *ngIf="filter.type === 'select'"
                      [ngModel]="filter.value"
                      (ngModelChange)="onFilterChange(filter.key, $event)"
                    >
                      <option value="">Tümü</option>
                      <option
                        *ngFor="let option of filter.options"
                        [value]="option.value"
                      >
                        {{ option.label }}
                        <span *ngIf="option.count">({{ option.count }})</span>
                      </option>
                    </select>

                    <!-- Text Filter -->
                    <input
                      type="text"
                      class="form-control"
                      *ngIf="filter.type === 'text'"
                      [ngModel]="filter.value"
                      (ngModelChange)="onFilterChange(filter.key, $event)"
                      [placeholder]="'Filtrele: ' + filter.label"
                    >

                    <!-- Boolean Filter -->
                    <select
                      class="form-select"
                      *ngIf="filter.type === 'boolean'"
                      [ngModel]="filter.value"
                      (ngModelChange)="onFilterChange(filter.key, $event)"
                    >
                      <option value="">Tümü</option>
                      <option value="true">Evet</option>
                      <option value="false">Hayır</option>
                    </select>

                    <!-- Date Filter -->
                    <input
                      type="date"
                      class="form-control"
                      *ngIf="filter.type === 'date'"
                      [ngModel]="filter.value"
                      (ngModelChange)="onFilterChange(filter.key, $event)"
                    >
                  </div>
                </div>
              </div>

              <div class="d-flex gap-2">
                <button
                  type="button"
                  class="btn btn-primary"
                  (click)="applyFilters()"
                >
                  <i class="fas fa-check me-2"></i>
                  Filtrele
                </button>
                <button
                  type="button"
                  class="btn btn-outline-secondary"
                  (click)="clearFilters()"
                >
                  <i class="fas fa-times me-2"></i>
                  Temizle
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Data Table -->
      <div class="table-responsive mt-3">
        <table class="table table-striped table-hover">
          <thead class="table-dark">
            <tr>
              <!-- Select All Checkbox -->
              <th scope="col" *ngIf="config().selectable" style="width: 50px;">
                <div class="form-check">
                  <input
                    type="checkbox"
                    class="form-check-input"
                    [checked]="isAllSelected()"
                    [indeterminate]="isPartiallySelected()"
                    (change)="toggleSelectAll()"
                  >
                </div>
              </th>

              <!-- Column Headers -->
              <th
                scope="col"
                *ngFor="let column of config().columns"
                [style.width]="column.width"
                [class]="'text-' + (column.align || 'left')"
                [class.sortable]="column.sortable"
                (click)="column.sortable ? onSort(column.key) : null"
              >
                {{ column.label }}
                <span *ngIf="column.sortable && sortBy() === column.key" class="sort-indicator">
                  <i [class]="sortDirection() === 'asc' ? 'fas fa-sort-up' : 'fas fa-sort-down'"></i>
                </span>
                <span *ngIf="column.sortable && sortBy() !== column.key" class="sort-indicator text-muted">
                  <i class="fas fa-sort"></i>
                </span>
              </th>

              <!-- Actions Column -->
              <th scope="col" *ngIf="config().showActions" style="width: 120px;" class="text-center">
                İşlemler
              </th>
            </tr>
          </thead>

          <tbody>
            <tr *ngFor="let item of data(); trackBy: trackByFn; let i = index">
              <!-- Selection Checkbox -->
              <td *ngIf="config().selectable">
                <div class="form-check">
                  <input
                    type="checkbox"
                    class="form-check-input"
                    [checked]="isSelected(item)"
                    (change)="toggleSelect(item)"
                  >
                </div>
              </td>

              <!-- Data Columns -->
              <td
                *ngFor="let column of config().columns"
                [class]="'text-' + (column.align || 'left')"
              >
                <ng-container [ngSwitch]="column.type">
                  <!-- Avatar -->
                  <div *ngSwitchCase="'avatar'" class="d-flex align-items-center">
                    <img
                      [src]="getAvatarUrl(item)"
                      [alt]="getDisplayValue(item, column.key)"
                      class="rounded-circle me-2"
                      style="width: 32px; height: 32px;"
                    >
                    {{ getDisplayValue(item, column.key) }}
                  </div>

                  <!-- Badge -->
                  <span *ngSwitchCase="'badge'" [class]="'badge ' + getBadgeClass(item, column.key)">
                    {{ getDisplayValue(item, column.key) }}
                  </span>

                  <!-- Boolean -->
                  <span *ngSwitchCase="'boolean'">
                    <i [class]="getDisplayValue(item, column.key) ? 'fas fa-check text-success' : 'fas fa-times text-danger'"></i>
                  </span>

                  <!-- Date -->
                  <span *ngSwitchCase="'date'">
                    {{ getDisplayValue(item, column.key) | date:'dd.MM.yyyy HH:mm' }}
                  </span>

                  <!-- Default Text -->
                  <span *ngSwitchDefault>
                    {{ getDisplayValue(item, column.key) }}
                  </span>
                </ng-container>
              </td>

              <!-- Actions -->
              <td *ngIf="config().showActions" class="text-center">
                <div class="btn-group btn-group-sm">
                  <button
                    *ngFor="let action of config().actions"
                    type="button"
                    [class]="'btn btn-outline-' + (action.type || 'primary')"
                    [disabled]="action.disabled"
                    [title]="action.label"
                    (click)="onAction(action.action, item)"
                  >
                    <i [class]="action.icon"></i>
                  </button>
                </div>
              </td>
            </tr>

            <!-- No Data -->
            <tr *ngIf="data().length === 0">
              <td [attr.colspan]="getColspan()" class="text-center py-4">
                <div class="text-muted">
                  <i class="fas fa-inbox fa-2x mb-2"></i>
                  <div>Gösterilecek veri bulunamadı</div>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="d-flex justify-content-between align-items-center mt-3" *ngIf="pagination()">
        <div class="pagination-info">
          <span class="text-muted">
            {{ getPaginationText() }}
          </span>
        </div>

        <div class="d-flex align-items-center gap-3">
          <!-- Page Size Selector -->
          <div class="d-flex align-items-center" *ngIf="config().pageSizeOptions">
            <label class="me-2 text-muted">Sayfa başı:</label>
            <select
              class="form-select form-select-sm"
              style="width: auto;"
              [ngModel]="pagination()?.pageSize"
              (ngModelChange)="onPageSizeChange($event)"
            >
              <option
                *ngFor="let size of config().pageSizeOptions"
                [value]="size"
              >
                {{ size }}
              </option>
            </select>
          </div>

          <!-- Pagination Controls -->
          <nav>
            <ul class="pagination pagination-sm mb-0">
              <li class="page-item" [class.disabled]="pagination()?.page === 1">
                <a class="page-link" href="javascript:void(0)" (click)="onPageChange(1)">
                  <i class="fas fa-angle-double-left"></i>
                </a>
              </li>
              <li class="page-item" [class.disabled]="pagination()?.page === 1">
                <a class="page-link" href="javascript:void(0)" (click)="onPageChange(pagination()!.page - 1)">
                  <i class="fas fa-angle-left"></i>
                </a>
              </li>

              <li
                *ngFor="let page of getVisiblePages()"
                class="page-item"
                [class.active]="page === pagination()?.page"
              >
                <a class="page-link" href="javascript:void(0)" (click)="onPageChange(page)">
                  {{ page }}
                </a>
              </li>

              <li class="page-item" [class.disabled]="pagination()?.page === pagination()?.totalPages">
                <a class="page-link" href="javascript:void(0)" (click)="onPageChange(pagination()!.page + 1)">
                  <i class="fas fa-angle-right"></i>
                </a>
              </li>
              <li class="page-item" [class.disabled]="pagination()?.page === pagination()?.totalPages">
                <a class="page-link" href="javascript:void(0)" (click)="onPageChange(pagination()!.totalPages)">
                  <i class="fas fa-angle-double-right"></i>
                </a>
              </li>
            </ul>
          </nav>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .data-table-container {
      background: var(--bs-card-bg);
      border-radius: var(--border-radius-md);
      padding: 1.5rem;
      box-shadow: var(--shadow-sm);
    }

    .table-controls {
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--bs-border-color);
    }

    .search-container .input-group {
      max-width: 400px;
    }

    .filters-panel .card {
      border: 1px solid var(--bs-border-color);
      background: var(--bs-content-bg);
    }

    .table {
      margin-bottom: 0;
    }

    .table th.sortable {
      cursor: pointer;
      user-select: none;
    }

    .table th.sortable:hover {
      background: rgba(var(--bs-primary-rgb), 0.1);
    }

    .sort-indicator {
      margin-left: 0.5rem;
      font-size: 0.8rem;
    }

    .pagination-info {
      font-size: 0.875rem;
    }

    .btn-group-sm .btn {
      padding: 0.25rem 0.5rem;
      font-size: 0.875rem;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .table-controls .row {
        flex-direction: column;
        gap: 1rem;
      }

      .table-controls .col-md-6 {
        width: 100%;
      }

      .table-responsive {
        margin: 0 -1.5rem;
        padding: 0 1.5rem;
      }

      .pagination-info {
        text-align: center;
        margin-bottom: 1rem;
      }

      .d-flex.justify-content-between {
        flex-direction: column;
        align-items: center;
        gap: 1rem;
      }
    }
  `]
})
export class DataTableComponent<T = any> implements OnInit {
  // Inputs
  data = input.required<T[]>();
  config = input.required<DataTableConfig>();
  pagination = input<PaginationInfo | null>(null);
  filters = input<FilterGroup[]>([]);
  loading = input<boolean>(false);

  // Outputs
  sort = output<{ column: string; direction: 'asc' | 'desc' }>();
  pageChange = output<number>();
  pageSizeChange = output<number>();
  search = output<string>();
  filterChange = output<{ [key: string]: any }>();
  action = output<{ action: string; item: T }>();
  bulkAction = output<{ action: string; items: T[] }>();
  selectionChange = output<T[]>();
  export = output<void>();

  // State
  selectedItems = signal<T[]>([]);
  searchTerm = signal<string>('');
  sortBy = signal<string>('');
  sortDirection = signal<'asc' | 'desc'>('asc');
  showFilters = signal<boolean>(false);

  // Computed
  activeFiltersCount = computed(() => {
    return this.filters().filter(f => f.value && f.value !== '').length;
  });

  isAllSelected = computed(() => {
    return this.data().length > 0 && this.selectedItems().length === this.data().length;
  });

  isPartiallySelected = computed(() => {
    return this.selectedItems().length > 0 && this.selectedItems().length < this.data().length;
  });

  ngOnInit(): void {
    // Initialize sorting if specified in config
    const sortableColumn = this.config().columns.find(c => c.sortable);
    if (sortableColumn) {
      this.sortBy.set(sortableColumn.key);
    }
  }

  // Search Methods
  onSearchChange(term: string): void {
    this.searchTerm.set(term);
  }

  clearSearch(): void {
    this.searchTerm.set('');
    this.search.emit('');
  }

  // Sorting Methods
  onSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDirection.update(dir => dir === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDirection.set('asc');
    }

    this.sort.emit({
      column: this.sortBy(),
      direction: this.sortDirection()
    });
  }

  // Selection Methods
  toggleSelect(item: T): void {
    const selected = this.selectedItems();
    const index = selected.findIndex(s => this.getItemId(s) === this.getItemId(item));

    if (index > -1) {
      this.selectedItems.set(selected.filter((_, i) => i !== index));
    } else {
      this.selectedItems.set([...selected, item]);
    }

    this.selectionChange.emit(this.selectedItems());
  }

  toggleSelectAll(): void {
    if (this.isAllSelected()) {
      this.selectedItems.set([]);
    } else {
      this.selectedItems.set([...this.data()]);
    }

    this.selectionChange.emit(this.selectedItems());
  }

  isSelected(item: T): boolean {
    return this.selectedItems().some(s => this.getItemId(s) === this.getItemId(item));
  }

  // Filter Methods
  onFilterChange(key: string, value: any): void {
    const filters = this.filters();
    const filter = filters.find(f => f.key === key);
    if (filter) {
      filter.value = value;
    }
  }

  applyFilters(): void {
    const activeFilters: { [key: string]: any } = {};
    this.filters().forEach(filter => {
      if (filter.value && filter.value !== '') {
        activeFilters[filter.key] = filter.value;
      }
    });

    this.filterChange.emit(activeFilters);
  }

  clearFilters(): void {
    this.filters().forEach(filter => {
      filter.value = null;
    });
    this.filterChange.emit({});
  }

  // Pagination Methods
  onPageChange(page: number): void {
    if (page >= 1 && page <= (this.pagination()?.totalPages || 1)) {
      this.pageChange.emit(page);
    }
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSizeChange.emit(pageSize);
  }

  getVisiblePages(): number[] {
    const current = this.pagination()?.page || 1;
    const total = this.pagination()?.totalPages || 1;
    const pages: number[] = [];

    let start = Math.max(1, current - 2);
    let end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }

  getPaginationText(): string {
    const p = this.pagination();
    if (!p) return '';

    const start = (p.page - 1) * p.pageSize + 1;
    const end = Math.min(p.page * p.pageSize, p.totalItems);

    return `${start}-${end} / ${p.totalItems} kayıt`;
  }

  // Filter Methods
  toggleFilters(): void {
    this.showFilters.update(show => !show);
  }

  // Action Methods
  onAction(action: string, item: T): void {
    this.action.emit({ action, item });
  }

  onBulkAction(bulkAction: BulkAction): void {
    if (bulkAction.confirmMessage) {
      if (confirm(bulkAction.confirmMessage)) {
        this.bulkAction.emit({ action: bulkAction.action, items: this.selectedItems() });
      }
    } else {
      this.bulkAction.emit({ action: bulkAction.action, items: this.selectedItems() });
    }
  }

  // Utility Methods
  trackByFn(index: number, item: T): any {
    return this.getItemId(item) || index;
  }

  getItemId(item: T): any {
    return (item as any)?.id || (item as any)?.userId || (item as any)?.roleId;
  }

  getDisplayValue(item: T, key: string): any {
    return this.getNestedProperty(item, key);
  }

  getNestedProperty(obj: any, path: string): any {
    return path.split('.').reduce((o, p) => o && o[p], obj);
  }

  getAvatarUrl(item: T): string {
    const profilePicture = this.getDisplayValue(item, 'profilePicture');
    if (profilePicture) {
      return profilePicture;
    }

    const firstName = this.getDisplayValue(item, 'firstName') || '';
    const lastName = this.getDisplayValue(item, 'lastName') || '';
    const initials = `${firstName.charAt(0)}${lastName.charAt(0)}`;

    return `https://ui-avatars.com/api/?name=${initials}&background=0d6efd&color=fff&size=32`;
  }

  getBadgeClass(item: T, key: string): string {
    const value = this.getDisplayValue(item, key);

    switch (key) {
      case 'isActive':
        return value ? 'bg-success' : 'bg-danger';
      case 'emailConfirmed':
        return value ? 'bg-success' : 'bg-warning';
      case 'lockoutEnabled':
        return value ? 'bg-danger' : 'bg-success';
      default:
        return 'bg-secondary';
    }
  }

  getColspan(): number {
    let colspan = this.config().columns.length;
    if (this.config().selectable) colspan++;
    if (this.config().showActions) colspan++;
    return colspan;
  }
}