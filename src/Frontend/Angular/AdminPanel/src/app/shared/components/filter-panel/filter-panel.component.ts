import { Component, Input, Output, EventEmitter, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, ChevronDown, ChevronUp, X, Filter, Search } from 'lucide-angular';

export interface FilterField {
  key: string;
  label: string;
  type: 'text' | 'select' | 'multiselect' | 'daterange' | 'boolean' | 'number';
  placeholder?: string;
  options?: SelectOption[];
  multiple?: boolean;
  clearable?: boolean;
  advanced?: boolean;
}

export interface SelectOption {
  label: string;
  value: any;
  disabled?: boolean;
  description?: string;
  icon?: string;
}

export interface DateRange {
  start?: Date;
  end?: Date;
}

@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card">
      <div class="card-header">
        <h3 class="card-title d-flex align-items-center">
          <lucide-icon name="filter" [size]="16" class="me-2"/>
          Filtreler
        </h3>
        <div class="card-actions">
          @if (hasActiveFilters()) {
            <button type="button"
                    class="btn btn-sm btn-outline-secondary me-2"
                    (click)="clearAllFilters()">
              <lucide-icon name="x" [size]="14" class="me-1"/>
              Temizle
            </button>
          }
          <button type="button"
                  class="btn btn-sm btn-outline-primary"
                  (click)="toggleCollapse()">
            <lucide-icon [name]="collapsed() ? 'chevron-down' : 'chevron-up'" [size]="14"/>
          </button>
        </div>
      </div>

      @if (!collapsed()) {
        <div class="card-body">
          <!-- Quick Search -->
          @if (showSearch) {
            <div class="mb-3">
              <label class="form-label">Hızlı Arama</label>
              <div class="input-group">
                <span class="input-group-text">
                  <lucide-icon name="search" [size]="16"/>
                </span>
                <input type="text"
                       class="form-control"
                       [(ngModel)]="filters.search"
                       (input)="onFilterChange()"
                       [placeholder]="searchPlaceholder">
                @if (filters.search) {
                  <button type="button"
                          class="btn btn-outline-secondary"
                          (click)="clearSearch()">
                    <lucide-icon name="x" [size]="14"/>
                  </button>
                }
              </div>
            </div>
          }

          <!-- Basic Filters -->
          @for (field of basicFields(); track field.key) {
            <div class="mb-3">
              <label class="form-label">{{ field.label }}</label>

              @switch (field.type) {
                @case ('text') {
                  <input type="text"
                         class="form-control"
                         [(ngModel)]="filters[field.key]"
                         (input)="onFilterChange()"
                         [placeholder]="field.placeholder">
                }

                @case ('number') {
                  <input type="number"
                         class="form-control"
                         [(ngModel)]="filters[field.key]"
                         (input)="onFilterChange()"
                         [placeholder]="field.placeholder">
                }

                @case ('select') {
                  <select class="form-select"
                          [(ngModel)]="filters[field.key]"
                          (change)="onFilterChange()">
                    <option value="">{{ field.placeholder || 'Seçiniz...' }}</option>
                    @for (option of field.options; track option.value) {
                      <option [value]="option.value"
                              [disabled]="option.disabled">
                        {{ option.label }}
                      </option>
                    }
                  </select>
                }

                @case ('multiselect') {
                  <div class="form-selectgroup form-selectgroup-pills">
                    @for (option of field.options; track option.value) {
                      <label class="form-selectgroup-item">
                        <input type="checkbox"
                               class="form-selectgroup-input"
                               [value]="option.value"
                               [checked]="isMultiSelected(field.key, option.value)"
                               (change)="onMultiSelectChange(field.key, option.value, $event)">
                        <span class="form-selectgroup-label">
                          @if (option.icon) {
                            <lucide-icon [name]="option.icon" [size]="16" class="me-1"/>
                          }
                          {{ option.label }}
                        </span>
                      </label>
                    }
                  </div>
                }

                @case ('boolean') {
                  <div class="form-selectgroup form-selectgroup-pills">
                    <label class="form-selectgroup-item">
                      <input type="radio"
                             class="form-selectgroup-input"
                             [name]="field.key"
                             [value]="true"
                             [checked]="filters[field.key] === true"
                             (change)="onBooleanChange(field.key, true)">
                      <span class="form-selectgroup-label">Evet</span>
                    </label>
                    <label class="form-selectgroup-item">
                      <input type="radio"
                             class="form-selectgroup-input"
                             [name]="field.key"
                             [value]="false"
                             [checked]="filters[field.key] === false"
                             (change)="onBooleanChange(field.key, false)">
                      <span class="form-selectgroup-label">Hayır</span>
                    </label>
                    <label class="form-selectgroup-item">
                      <input type="radio"
                             class="form-selectgroup-input"
                             [name]="field.key"
                             value=""
                             [checked]="filters[field.key] === undefined"
                             (change)="onBooleanChange(field.key, undefined)">
                      <span class="form-selectgroup-label">Tümü</span>
                    </label>
                  </div>
                }

                @case ('daterange') {
                  <div class="row">
                    <div class="col">
                      <input type="date"
                             class="form-control"
                             [(ngModel)]="filters[field.key + '_start']"
                             (change)="onFilterChange()"
                             placeholder="Başlangıç">
                    </div>
                    <div class="col">
                      <input type="date"
                             class="form-control"
                             [(ngModel)]="filters[field.key + '_end']"
                             (change)="onFilterChange()"
                             placeholder="Bitiş">
                    </div>
                  </div>
                }
              }

              @if (field.clearable && hasFieldValue(field.key)) {
                <div class="mt-1">
                  <button type="button"
                          class="btn btn-sm btn-outline-secondary"
                          (click)="clearField(field.key)">
                    <lucide-icon name="x" [size]="12" class="me-1"/>
                    Temizle
                  </button>
                </div>
              }
            </div>
          }

          <!-- Advanced Filters Toggle -->
          @if (advancedFields().length > 0) {
            <div class="mb-3">
              <button type="button"
                      class="btn btn-outline-primary btn-sm"
                      (click)="toggleAdvanced()">
                {{ showAdvanced() ? 'Basit' : 'Gelişmiş' }} Filtreler
                <lucide-icon [name]="showAdvanced() ? 'chevron-up' : 'chevron-down'" [size]="14" class="ms-1"/>
              </button>
            </div>

            @if (showAdvanced()) {
              <div class="border-top pt-3">
                @for (field of advancedFields(); track field.key) {
                  <div class="mb-3">
                    <label class="form-label">{{ field.label }}</label>
                    <!-- Same field rendering logic as above -->
                    <!-- Implementation similar to basic fields -->
                  </div>
                }
              </div>
            }
          }

          <!-- Custom Filters Slot -->
          <ng-content select="[slot=custom-filters]"></ng-content>
        </div>

        <!-- Footer with Filter Summary -->
        @if (hasActiveFilters()) {
          <div class="card-footer">
            <div class="d-flex align-items-center text-muted">
              <small>
                {{ activeFilterCount() }} filtre aktif
              </small>
              <div class="ms-auto">
                <button type="button"
                        class="btn btn-sm btn-primary"
                        (click)="applyFilters()">
                  Filtrele
                </button>
              </div>
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .form-selectgroup-pills .form-selectgroup-label {
      border-radius: 50px;
    }

    .form-selectgroup-item {
      margin-bottom: 0.5rem;
      margin-right: 0.5rem;
    }

    .card-actions {
      display: flex;
      align-items: center;
    }

    .input-group .btn {
      border-left: 0;
    }

    .input-group .btn:focus {
      box-shadow: none;
      border-color: var(--bs-border-color);
    }
  `]
})
export class FilterPanelComponent implements OnInit {
  @Input() filterFields: FilterField[] = [];
  @Input() initialFilters: any = {};
  @Input() showSearch = true;
  @Input() searchPlaceholder = 'Ara...';
  @Input() autoApply = true;

  // State signals
  filters: any = {};
  collapsed = signal(false);
  showAdvanced = signal(false);

  // Icons
  readonly chevronDownIcon = ChevronDown;
  readonly chevronUpIcon = ChevronUp;
  readonly xIcon = X;
  readonly filterIcon = Filter;
  readonly searchIcon = Search;

  // Computed values
  basicFields = computed(() =>
    this.filterFields.filter(field => !field.advanced)
  );

  advancedFields = computed(() =>
    this.filterFields.filter(field => field.advanced)
  );

  hasActiveFilters = computed(() => {
    const filters = this.filters;
    return Object.keys(filters).some(key => {
      const value = filters[key];
      if (Array.isArray(value)) {
        return value.length > 0;
      }
      return value !== undefined && value !== null && value !== '';
    });
  });

  activeFilterCount = computed(() => {
    const filters = this.filters;
    return Object.keys(filters).filter(key => {
      const value = filters[key];
      if (Array.isArray(value)) {
        return value.length > 0;
      }
      return value !== undefined && value !== null && value !== '';
    }).length;
  });

  @Output() filtersChange = new EventEmitter<any>();
  @Output() filterApply = new EventEmitter<any>();

  private changeTimeout: any;

  ngOnInit() {
    // Initialize filters with defaults
    this.filters = { ...this.initialFilters };
  }

  onFilterChange() {
    if (this.autoApply) {
      // Debounce automatic filter application
      clearTimeout(this.changeTimeout);
      this.changeTimeout = setTimeout(() => {
        this.emitFilters();
      }, 300);
    }
  }

  onMultiSelectChange(fieldKey: string, value: any, event: Event) {
    const target = event.target as HTMLInputElement;
    const currentFilters = this.filters;

    if (!currentFilters[fieldKey]) {
      currentFilters[fieldKey] = [];
    }

    if (target.checked) {
      if (!currentFilters[fieldKey].includes(value)) {
        currentFilters[fieldKey].push(value);
      }
    } else {
      const index = currentFilters[fieldKey].indexOf(value);
      if (index > -1) {
        currentFilters[fieldKey].splice(index, 1);
      }
    }

    this.filters = { ...currentFilters };
    this.onFilterChange();
  }

  onBooleanChange(fieldKey: string, value: boolean | undefined) {
    const currentFilters = this.filters;
    currentFilters[fieldKey] = value;
    this.filters = { ...currentFilters };
    this.onFilterChange();
  }

  isMultiSelected(fieldKey: string, value: any): boolean {
    const fieldValue = this.filters[fieldKey];
    return Array.isArray(fieldValue) && fieldValue.includes(value);
  }

  hasFieldValue(fieldKey: string): boolean {
    const value = this.filters[fieldKey];
    if (Array.isArray(value)) {
      return value.length > 0;
    }
    return value !== undefined && value !== null && value !== '';
  }

  clearField(fieldKey: string) {
    const currentFilters = this.filters;

    // Handle date range fields
    if (fieldKey.includes('_')) {
      const baseKey = fieldKey.split('_')[0];
      delete currentFilters[baseKey + '_start'];
      delete currentFilters[baseKey + '_end'];
    } else {
      delete currentFilters[fieldKey];
    }

    this.filters = { ...currentFilters };
    this.onFilterChange();
  }

  clearSearch() {
    const currentFilters = this.filters;
    delete currentFilters.search;
    this.filters = { ...currentFilters };
    this.onFilterChange();
  }

  clearAllFilters() {
    this.filters = {};
    this.emitFilters();
  }

  toggleCollapse() {
    this.collapsed.set(!this.collapsed());
  }

  toggleAdvanced() {
    this.showAdvanced.set(!this.showAdvanced());
  }

  applyFilters() {
    this.filterApply.emit(this.filters);
  }

  private emitFilters() {
    this.filtersChange.emit(this.filters);
  }

  // Public methods for external control
  setFilters(filters: any) {
    this.filters = { ...filters };
  }

  getFilters() {
    return this.filters;
  }

  resetFilters() {
    this.filters = { ...this.initialFilters };
    this.emitFilters();
  }
}