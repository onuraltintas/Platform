import { TemplateRef } from '@angular/core';
import { Observable } from 'rxjs';

export interface TableColumn<T = any> {
  key: string;
  label: string;
  sortable?: boolean;
  searchable?: boolean;
  type?: ColumnType;
  width?: string;
  minWidth?: string;
  maxWidth?: string;
  sticky?: boolean;
  hidden?: boolean;
  align?: 'left' | 'center' | 'right';
  format?: (value: any, row: T) => string;
  template?: (row: T) => string;
  cellTemplate?: TemplateRef<any>;
  headerTemplate?: TemplateRef<any>;
  cellClass?: string | ((value: any, row: T) => string);
  headerClass?: string;
  tooltip?: boolean | ((value: any, row: T) => string);
}

export type ColumnType =
  | 'text'
  | 'number'
  | 'currency'
  | 'percentage'
  | 'date'
  | 'datetime'
  | 'time'
  | 'boolean'
  | 'boolean-badge'
  | 'badge'
  | 'badge-list'
  | 'chip'
  | 'chip-list'
  | 'avatar'
  | 'image'
  | 'link'
  | 'email'
  | 'phone'
  | 'actions'
  | 'custom';

export interface TableAction<T = any> {
  id: string;
  label: string;
  icon?: string;
  tooltip?: string;
  color?: 'primary' | 'accent' | 'warn';
  disabled?: boolean | ((row: T) => boolean);
  hidden?: boolean | ((row: T) => boolean);
  action: (row: T) => void;
}

export interface BulkAction<T = any> {
  id: string;
  label: string;
  icon?: string;
  tooltip?: string;
  color?: 'primary' | 'accent' | 'warn';
  confirmMessage?: string;
  action: (selectedRows: T[]) => void | Promise<void>;
}

export interface FilterConfig {
  key: string;
  label: string;
  type: FilterType;
  options?: FilterOption[] | Observable<FilterOption[]>;
  placeholder?: string;
  defaultValue?: any;
  multiple?: boolean;
  searchable?: boolean;
  clearable?: boolean;
  width?: string;
}

export type FilterType =
  | 'text'
  | 'number'
  | 'select'
  | 'multiselect'
  | 'date'
  | 'daterange'
  | 'boolean'
  | 'toggle'
  | 'autocomplete';

export interface FilterOption {
  value: any;
  label: string;
  description?: string;
  icon?: string;
  disabled?: boolean;
}

export interface TableConfig<T = any> {
  // Data
  data: T[];
  loading?: boolean;
  error?: string | null;

  // Columns
  columns: TableColumn<T>[];
  displayedColumns?: string[];

  // Selection
  selectable?: boolean;
  multiSelect?: boolean;
  selectOnRowClick?: boolean;

  // Pagination
  pageable?: boolean;
  pageSize?: number;
  pageSizeOptions?: number[];
  showFirstLastButtons?: boolean;

  // Sorting
  sortable?: boolean;
  defaultSort?: { column: string; direction: 'asc' | 'desc' };

  // Filtering
  filterable?: boolean;
  globalSearch?: boolean;
  filters?: FilterConfig[];

  // Actions
  actions?: TableAction<T>[];
  bulkActions?: BulkAction<T>[];

  // Export
  exportable?: boolean;
  exportFormats?: ExportFormat[];

  // Appearance
  striped?: boolean;
  bordered?: boolean;
  hoverable?: boolean;
  dense?: boolean;
  showHeader?: boolean;
  showFooter?: boolean;
  emptyMessage?: string;
  loadingMessage?: string;

  // Responsive
  responsive?: boolean;
  mobileBreakpoint?: number;

  // Virtual scrolling
  virtualScroll?: boolean;
  itemSize?: number;

  // Refresh
  refreshable?: boolean;
  autoRefresh?: boolean;
  refreshInterval?: number;
}

export interface ExportFormat {
  type: 'excel' | 'csv' | 'pdf' | 'json';
  label: string;
  icon?: string;
  filename?: string;
}

export interface TableState<T = any> {
  // Data
  originalData: T[];
  filteredData: T[];
  paginatedData: T[];

  // Selection
  selectedRows: T[];
  selectedAll: boolean;

  // Pagination
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;

  // Sorting
  sortColumn: string | null;
  sortDirection: 'asc' | 'desc' | null;

  // Filtering
  globalSearchTerm: string;
  columnFilters: { [key: string]: any };
  activeFilters: { [key: string]: any };

  // UI State
  loading: boolean;
  error: string | null;
  expandedRows: Set<string>;
}

export interface TableEvents<T = any> {
  // Row events
  rowClick?: (row: T, event: Event) => void;
  rowDoubleClick?: (row: T, event: Event) => void;
  rowSelect?: (row: T, selected: boolean) => void;
  selectionChange?: (selectedRows: T[]) => void;

  // Cell events
  cellClick?: (value: any, row: T, column: TableColumn<T>, event: Event) => void;

  // Pagination events
  pageChange?: (page: number) => void;
  pageSizeChange?: (pageSize: number) => void;

  // Sorting events
  sortChange?: (column: string, direction: 'asc' | 'desc' | null) => void;

  // Filter events
  filterChange?: (filters: { [key: string]: any }) => void;
  globalSearchChange?: (searchTerm: string) => void;

  // Export events
  export?: (format: string, data: T[]) => void;

  // Refresh events
  refresh?: () => void;
}

export interface CellContext<T = any> {
  $implicit: any; // value
  row: T;
  column: TableColumn<T>;
  index: number;
  rowIndex: number;
}

export interface HeaderContext<T = any> {
  $implicit: TableColumn<T>; // column
  index: number;
}

// Utility types
export type SortDirection = 'asc' | 'desc' | null;
export type SelectionMode = 'single' | 'multiple' | 'none';
export type TableSize = 'small' | 'medium' | 'large';
export type TableVariant = 'default' | 'striped' | 'bordered' | 'borderless';

// Configuration presets
export const TABLE_PRESETS = {
  BASIC: {
    selectable: false,
    pageable: true,
    sortable: true,
    filterable: false,
    dense: false
  },
  CRUD: {
    selectable: true,
    multiSelect: true,
    pageable: true,
    sortable: true,
    filterable: true,
    globalSearch: true,
    exportable: true,
    refreshable: true
  },
  READONLY: {
    selectable: false,
    pageable: true,
    sortable: true,
    filterable: true,
    globalSearch: true,
    exportable: true,
    actions: []
  },
  COMPACT: {
    dense: true,
    showHeader: true,
    showFooter: false,
    pageable: false,
    bordered: false
  }
} as const;