import { Component, Output, EventEmitter, computed, signal, input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, ChevronUp, ChevronDown, MoreHorizontal, Eye, Edit, Trash, Download } from 'lucide-angular';
import { FormsModule } from '@angular/forms';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  filterable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
  type?: 'text' | 'number' | 'date' | 'boolean' | 'badge' | 'avatar' | 'actions';
  format?: (value: any) => string;
  className?: string;
}

export interface ActionButton {
  icon: string;
  label: string;
  action: string;
  variant?: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info';
  visible?: boolean;
  disabled?: boolean;
  permission?: string;
}

export interface SortEvent {
  field: string;
  direction: 'asc' | 'desc';
}

export interface TableConfig {
  showSelection?: boolean;
  showActions?: boolean;
  showPagination?: boolean;
  pageSize?: number;
  sortable?: boolean;
  selectable?: boolean;
  stickyHeader?: boolean;
}

export interface TableAction {
  key: string;
  label: string;
  icon: string;
  variant?: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info';
  visible?: boolean;
  disabled?: boolean;
  permission?: string;
}

@Component({
  selector: 'app-advanced-data-table',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    RouterLink
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="table-responsive">
      <table class="table table-vcenter card-table">
        <thead>
          <tr>
            @if (selectable()) {
              <th style="width: 1%">
                <label class="form-check">
                  <input type="checkbox"
                         class="form-check-input"
                         [checked]="allSelected()"
                         [indeterminate]="someSelected()"
                         (change)="toggleSelectAll()">
                </label>
              </th>
            }
            @for (column of columns(); track column.key) {
              <th [style.width]="column.width"
                  [class]="getColumnClass(column)"
                  [class.cursor-pointer]="column.sortable"
                  (click)="onSort(column)">
                <div class="d-flex align-items-center">
                  <span>{{ column.label }}</span>
                  @if (column.sortable && sortBy() === column.key) {
                    <lucide-icon
                      [name]="sortDirection() === 'asc' ? 'chevron-up' : 'chevron-down'"
                      [size]="16"
                      class="ms-1"/>
                  }
                </div>
              </th>
            }
            @if (actions().length > 0) {
              <th style="width: 1%">İşlemler</th>
            }
          </tr>
        </thead>
        <tbody>
          @if (loading()) {
            @for (skeleton of skeletonRows(); track $index) {
              <tr>
                @if (selectable()) {
                  <td><div class="placeholder col-8"></div></td>
                }
                @for (column of columns(); track column.key) {
                  <td>
                    <div class="placeholder"
                         [class]="getSkeletonClass(column.type)"></div>
                  </td>
                }
                @if (actions().length > 0) {
                  <td><div class="placeholder col-6"></div></td>
                }
              </tr>
            }
          } @else if (paginatedData().length === 0) {
            <tr>
              <td [attr.colspan]="totalColumns()" class="text-center py-4">
                <div class="empty">
                  <div class="empty-icon">
                    <lucide-icon name="inbox" [size]="48" class="text-muted"/>
                  </div>
                  <p class="empty-title">Veri bulunamadı</p>
                  <p class="empty-subtitle text-muted">
                    {{ emptyMessage() }}
                  </p>
                </div>
              </td>
            </tr>
          } @else {
            @for (item of paginatedData(); track item.id || item) {
              <tr [class.table-active]="isSelected(item)"
                  [class.table-danger]="item._deleted"
                  [class.table-warning]="item._pending">
                @if (selectable()) {
                  <td>
                    <label class="form-check">
                      <input type="checkbox"
                             class="form-check-input"
                             [checked]="isSelected(item)"
                             (change)="toggleSelect(item)">
                    </label>
                  </td>
                }
                @for (column of columns(); track column.key) {
                  <td [class]="getCellClass(column)"
                      [innerHTML]="getCellContent(item, column)">
                  </td>
                }
                @if (actions().length > 0) {
                  <td>
                    <div class="btn-list">
                      @for (action of getVisibleActions(item); track action.action) {
                        @if (action.action === 'dropdown' && getDropdownActions(item).length > 3) {
                          <div class="dropdown">
                            <button class="btn btn-white btn-sm dropdown-toggle"
                                    type="button"
                                    data-bs-toggle="dropdown">
                              <lucide-icon name="more-horizontal" [size]="16"/>
                            </button>
                            <div class="dropdown-menu">
                              @for (dropAction of getDropdownActions(item); track dropAction.action) {
                                <a class="dropdown-item"
                                   href="#"
                                   [class.disabled]="dropAction.disabled"
                                   (click)="onAction($event, dropAction.action, item)">
                                  <lucide-icon [name]="dropAction.icon" [size]="16" class="me-2"/>
                                  {{ dropAction.label }}
                                </a>
                              }
                            </div>
                          </div>
                        } @else {
                          <button class="btn btn-sm"
                                  [class]="'btn-' + (action.variant || 'white')"
                                  [disabled]="action.disabled"
                                  [title]="action.label"
                                  (click)="onAction($event, action.action, item)">
                            <lucide-icon [name]="action.icon" [size]="16"/>
                          </button>
                        }
                      }
                    </div>
                  </td>
                }
              </tr>
            }
          }
        </tbody>
      </table>
    </div>

    @if (!loading() && paginatedData().length > 0) {
      <div class="card-footer d-flex align-items-center">
        <p class="m-0 text-muted">
          Toplam {{ totalItems() }} kayıttan
          {{ ((currentPage() - 1) * pageSize()) + 1 }}-{{ Math.min(currentPage() * pageSize(), totalItems()) }}
          arası gösteriliyor
        </p>

        <ul class="pagination m-0 ms-auto">
          <li class="page-item" [class.disabled]="currentPage() === 1">
            <a class="page-link" href="#" (click)="onPageChange($event, currentPage() - 1)">
              <lucide-icon name="chevron-left" [size]="16"/>
              Önceki
            </a>
          </li>

          @for (page of visiblePages(); track page) {
            @if (page === '...') {
              <li class="page-item disabled">
                <span class="page-link">…</span>
              </li>
            } @else {
              <li class="page-item" [class.active]="page === currentPage()">
                <a class="page-link" href="#"
                   (click)="onPageChange($event, +page)">{{ page }}</a>
              </li>
            }
          }

          <li class="page-item" [class.disabled]="currentPage() === totalPages()">
            <a class="page-link" href="#" (click)="onPageChange($event, currentPage() + 1)">
              Sonraki
              <lucide-icon name="chevron-right" [size]="16"/>
            </a>
          </li>
        </ul>
      </div>
    }
  `,
  styles: [`
    .cursor-pointer {
      cursor: pointer;
    }

    .table-vcenter td {
      vertical-align: middle;
    }

    .empty {
      padding: 3rem 0;
    }

    .empty-icon {
      margin-bottom: 1rem;
    }

    .placeholder {
      background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
      background-size: 200% 100%;
      animation: loading 1.5s infinite;
      border-radius: 0.25rem;
      height: 1rem;
    }

    .placeholder.col-4 { width: 33.33%; }
    .placeholder.col-6 { width: 50%; }
    .placeholder.col-8 { width: 66.66%; }
    .placeholder.col-12 { width: 100%; }

    @keyframes loading {
      0% { background-position: 200% 0; }
      100% { background-position: -200% 0; }
    }

    .table-danger {
      --bs-table-bg: #fef2f2;
    }

    .table-warning {
      --bs-table-bg: #fefce8;
    }
  `]
})
export class AdvancedDataTableComponent implements OnInit {
  // Inputs using the new signal inputs API
  data = input.required<any[]>();
  columns = input.required<TableColumn[]>();
  loading = input(false);
  selectable = input(false);
  actions = input<ActionButton[]>([]);
  pageSize = input(25);
  emptyMessage = input('Kayıt bulunamadı');
  trackBy = input<(item: any) => any>((item) => item.id || item);

  // Math reference for template
  readonly Math = Math;

  // State signals
  selectedItems = signal<any[]>([]);
  sortBy = signal<string>('');
  sortDirection = signal<'asc' | 'desc'>('asc');
  currentPage = signal(1);

  // Icons
  readonly chevronUpIcon = ChevronUp;
  readonly chevronDownIcon = ChevronDown;
  readonly moreHorizontalIcon = MoreHorizontal;
  readonly eyeIcon = Eye;
  readonly editIcon = Edit;
  readonly trashIcon = Trash;
  readonly downloadIcon = Download;

  // Computed values
  sortedData = computed(() => {
    const data = [...this.data()];
    const sortField = this.sortBy();

    if (!sortField) return data;

    return data.sort((a, b) => {
      const aVal = this.getNestedValue(a, sortField);
      const bVal = this.getNestedValue(b, sortField);

      if (aVal < bVal) return this.sortDirection() === 'asc' ? -1 : 1;
      if (aVal > bVal) return this.sortDirection() === 'asc' ? 1 : -1;
      return 0;
    });
  });

  paginatedData = computed(() => {
    const sorted = this.sortedData();
    const start = (this.currentPage() - 1) * this.pageSize();
    const end = start + this.pageSize();
    return sorted.slice(start, end);
  });

  totalItems = computed(() => this.data().length);
  totalPages = computed(() => Math.ceil(this.totalItems() / this.pageSize()));

  totalColumns = computed(() => {
    let count = this.columns().length;
    if (this.selectable()) count++;
    if (this.actions().length > 0) count++;
    return count;
  });

  allSelected = computed(() => {
    const items = this.paginatedData();
    return items.length > 0 && items.every(item => this.isSelected(item));
  });

  someSelected = computed(() => {
    const items = this.paginatedData();
    return items.some(item => this.isSelected(item)) && !this.allSelected();
  });

  skeletonRows = computed(() => Array(Math.min(this.pageSize(), 5)).fill(0));

  visiblePages = computed(() => {
    const current = this.currentPage();
    const total = this.totalPages();
    const delta = 2;
    const range = [];
    const rangeWithDots = [];

    for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
      range.push(i);
    }

    if (current - delta > 2) {
      rangeWithDots.push(1, '...');
    } else {
      rangeWithDots.push(1);
    }

    rangeWithDots.push(...range);

    if (current + delta < total - 1) {
      rangeWithDots.push('...', total);
    } else if (total > 1) {
      rangeWithDots.push(total);
    }

    return rangeWithDots;
  });

  // Events
  @Output() selectionChange = new EventEmitter<any[]>();
  @Output() actionClick = new EventEmitter<{action: string, item: any}>();
  @Output() sortChange = new EventEmitter<SortEvent>();
  @Output() pageChange = new EventEmitter<number>();

  ngOnInit() {
    // Initialize default sorting if specified
    const defaultSort = this.columns().find(col => col.sortable);
    if (defaultSort && !this.sortBy()) {
      this.sortBy.set(defaultSort.key);
    }
  }

  onSort(column: TableColumn) {
    if (!column.sortable) return;

    if (this.sortBy() === column.key) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column.key);
      this.sortDirection.set('asc');
    }

    this.sortChange.emit({
      field: this.sortBy(),
      direction: this.sortDirection()
    });
  }

  toggleSelect(item: any) {
    const selected = this.selectedItems();
    const index = selected.findIndex(s => this.trackBy()(s) === this.trackBy()(item));

    if (index >= 0) {
      selected.splice(index, 1);
    } else {
      selected.push(item);
    }

    this.selectedItems.set([...selected]);
    this.selectionChange.emit(this.selectedItems());
  }

  toggleSelectAll() {
    const items = this.paginatedData();

    if (this.allSelected()) {
      // Deselect all current page items
      const selected = this.selectedItems().filter(item =>
        !items.some(pageItem => this.trackBy()(pageItem) === this.trackBy()(item))
      );
      this.selectedItems.set(selected);
    } else {
      // Select all current page items
      const selected = [...this.selectedItems()];
      items.forEach(item => {
        if (!selected.some(s => this.trackBy()(s) === this.trackBy()(item))) {
          selected.push(item);
        }
      });
      this.selectedItems.set(selected);
    }

    this.selectionChange.emit(this.selectedItems());
  }

  isSelected(item: any): boolean {
    return this.selectedItems().some(s => this.trackBy()(s) === this.trackBy()(item));
  }

  onAction(event: Event, action: string, item: any) {
    event.preventDefault();
    event.stopPropagation();
    this.actionClick.emit({ action, item });
  }

  onPageChange(event: Event, page: number) {
    event.preventDefault();
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.pageChange.emit(page);
    }
  }

  getColumnClass(column: TableColumn): string {
    const classes = [];
    if (column.align) classes.push(`text-${column.align}`);
    if (column.className) classes.push(column.className);
    return classes.join(' ');
  }

  getCellClass(column: TableColumn): string {
    return this.getColumnClass(column);
  }

  getCellContent(item: any, column: TableColumn): string {
    const value = this.getNestedValue(item, column.key);

    if (column.format) {
      return column.format(value);
    }

    switch (column.type) {
      case 'boolean':
        return value ?
          '<span class="badge badge-success">Aktif</span>' :
          '<span class="badge badge-secondary">Pasif</span>';

      case 'date':
        return value ? new Date(value).toLocaleDateString('tr-TR') : '-';

      case 'badge':
        if (Array.isArray(value)) {
          return value.map(v => `<span class="badge badge-primary me-1">${v}</span>`).join('');
        }
        return value ? `<span class="badge badge-primary">${value}</span>` : '-';

      case 'avatar':
        const name = item.firstName && item.lastName ?
          `${item.firstName} ${item.lastName}` :
          item.userName || item.email || 'User';
        const initials = name.split(' ').map((n: string) => n[0]).join('').toUpperCase().slice(0, 2);

        if (item.profilePictureUrl) {
          return `<span class="avatar avatar-sm" style="background-image: url(${item.profilePictureUrl})"></span>`;
        } else {
          return `<span class="avatar avatar-sm">${initials}</span>`;
        }

      default:
        return value?.toString() || '-';
    }
  }

  getSkeletonClass(type: string | undefined): string {
    switch (type) {
      case 'avatar': return 'col-4';
      case 'boolean': return 'col-4';
      case 'date': return 'col-6';
      case 'badge': return 'col-8';
      default: return 'col-12';
    }
  }

  getVisibleActions(_item: any): ActionButton[] {
    return this.actions().filter(action => action.visible !== false);
  }

  getDropdownActions(item: any): ActionButton[] {
    return this.getVisibleActions(item);
  }

  private getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((o, p) => o?.[p], obj);
  }

  clearSelection() {
    this.selectedItems.set([]);
    this.selectionChange.emit([]);
  }
}