import { Component, Input, Output, EventEmitter, computed, OnInit, ViewChild, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { SelectionModel } from '@angular/cdk/collections';

import { TableColumn, TableConfig, TableAction, ColumnType } from './data-table.models';

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatPaginatorModule,
    MatSortModule,
    MatCheckboxModule,
    MatChipsModule,
    MatMenuModule
  ],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss'
})
export class DataTableComponent<T = any> implements OnInit {
  @Input() config: TableConfig<T> = {
    data: [],
    columns: [],
    selectable: false,
    pageable: true,
    sortable: true,
    showHeader: true,
    showFooter: true
  };

  @Output() rowClick = new EventEmitter<T>();
  @Output() selectionChange = new EventEmitter<T[]>();
  @Output() actionExecuted = new EventEmitter<{ action: TableAction<T>; row: T }>();
  @Output() pageChange = new EventEmitter<PageEvent>();

  @ViewChild(MatPaginator) paginator?: MatPaginator;
  @ViewChild(MatSort) sort?: MatSort;

  // Table state
  dataSource = new MatTableDataSource<T>([]);
  selection = new SelectionModel<T>(true, []);

  // Computed
  public readonly isLoading = computed(() => this.config.loading || false);
  public readonly isEmpty = computed(() =>
    !this.config.data?.length && !this.isLoading()
  );

  public readonly visibleColumns = computed(() =>
    this.config.columns.filter(col => !col.hidden)
  );

  public readonly displayedColumns = computed(() => {
    const columns: string[] = [];

    if (this.config.selectable) {
      columns.push('select');
    }

    columns.push(...this.visibleColumns().map(col => col.key));

    if (this.hasActions()) {
      columns.push('actions');
    }

    return columns;
  });

  ngOnInit(): void {
    // Update data source when config changes
    effect(() => {
      const config = this.config;
      this.dataSource.data = config.data || [];

      if (this.paginator) {
        this.dataSource.paginator = this.paginator;
      }

      if (this.sort) {
        this.dataSource.sort = this.sort;
      }
    });

    // Selection change event
    this.selection.changed.subscribe(() => {
      this.selectionChange.emit(this.selection.selected);
    });
  }

  // Selection methods
  isAllSelected(): boolean {
    const numSelected = this.selection.selected.length;
    const numRows = this.dataSource.data.length;
    return numSelected === numRows;
  }

  toggleAllRows(): void {
    if (this.isAllSelected()) {
      this.selection.clear();
    } else {
      this.selection.select(...this.dataSource.data);
    }
  }

  // Action methods
  hasActions(): boolean {
    return !!(this.config.actions && this.config.actions!.length > 0);
  }

  getVisibleActions(row: T): TableAction<T>[] {
    return (this.config.actions || []).filter(action => {
      if (typeof action.hidden === 'function') {
        return !action.hidden(row);
      }
      return !action.hidden;
    });
  }

  isActionDisabled(action: TableAction<T>, row: T): boolean {
    if (typeof action.disabled === 'function') {
      return action.disabled(row);
    }
    return !!action.disabled;
  }

  executeAction(action: TableAction<T>, row: T): void {
    if (!this.isActionDisabled(action, row)) {
      action.action(row);
      this.actionExecuted.emit({ action, row });
    }
  }

  // Cell formatting methods
  getFormattedValue(column: TableColumn<T>, row: T): any {
    const value = this.getCellValue(column, row);

    if (column.format) {
      return column.format(value, row);
    }

    return value;
  }

  private getCellValue(column: TableColumn<T>, row: T): any {
    return (row as any)[column.key];
  }

  getCellClass(column: TableColumn<T>, row: T): string {
    if (column.cellClass) {
      if (typeof column.cellClass === 'function') {
        return column.cellClass(this.getCellValue(column, row), row);
      }
      return column.cellClass;
    }
    return '';
  }

  getRowClass(row: T): string {
    const classes: string[] = [];

    if (this.selection.isSelected(row)) {
      classes.push('selected-row');
    }

    return classes.join(' ');
  }

  // Type checking methods
  isTextType(type?: ColumnType): boolean {
    return ['text', 'number', 'currency', 'percentage'].includes(type || 'text');
  }

  isDateType(type?: ColumnType): boolean {
    return ['date', 'datetime', 'time'].includes(type || '');
  }

  // Boolean methods
  getBooleanIcon(column: TableColumn<T>, row: T): string {
    const value = this.getCellValue(column, row);
    return value ? 'check_circle' : 'cancel';
  }

  getBooleanIconClass(column: TableColumn<T>, row: T): string {
    const value = this.getCellValue(column, row);
    return value ? 'boolean-true' : 'boolean-false';
  }

  getBooleanBadgeClass(column: TableColumn<T>, row: T): string {
    const value = this.getCellValue(column, row);
    return value ? 'status-active' : 'status-inactive';
  }

  // Chip methods
  getChipList(column: TableColumn<T>, row: T): any[] {
    const value = this.getFormattedValue(column, row);
    return Array.isArray(value) ? value : [value];
  }

  getChipClass(column: TableColumn<T>, row: T, chip: any): string {
    if (column.cellClass) {
      if (typeof column.cellClass === 'function') {
        return column.cellClass(chip, row);
      }
      return column.cellClass;
    }
    return '';
  }

  // Event handlers
  onRowClick(row: T): void {
    if (this.config.selectOnRowClick) {
      this.selection.toggle(row);
    }
    this.rowClick.emit(row);
  }

  onPageChange(event: PageEvent): void {
    this.pageChange.emit(event);
  }
}