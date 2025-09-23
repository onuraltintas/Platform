import { Component, Input, Output, EventEmitter, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

export interface ActionButton {
  key: string;
  label: string;
  icon?: string;
  variant?: 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info' | 'light' | 'dark' | 'outline-primary' | 'outline-secondary' | 'outline-success' | 'outline-danger' | 'outline-warning' | 'outline-info';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  loading?: boolean;
  hidden?: boolean;
  tooltip?: string;
  badge?: string | number;
  dropdown?: ActionButton[];
  requiresSelection?: boolean;
  requiresPermission?: string | string[];
  confirmMessage?: string;
  destructive?: boolean;
}

export interface ActionEvent {
  action: string;
  data?: any;
  selectedItems?: any[];
}

@Component({
  selector: 'app-action-button-group',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="btn-list" [class]="containerClass">
      @for (action of visibleActions(); track action.key) {
        <!-- Regular Button -->
        @if (!action.dropdown) {
          <button
            type="button"
            [class]="getButtonClasses(action)"
            [disabled]="isActionDisabled(action)"
            [title]="action.tooltip"
            (click)="onActionClick(action)">

            @if (action.loading) {
              <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
            } @else if (action.icon) {
              <lucide-icon [name]="action.icon" [size]="getIconSize(action)" class="me-2"/>
            }

            {{ action.label }}

            @if (action.badge) {
              <span class="badge ms-2" [class]="getBadgeClass(action)">
                {{ action.badge }}
              </span>
            }
          </button>
        }

        <!-- Dropdown Button -->
        @if (action.dropdown) {
          <div class="dropdown">
            <button
              type="button"
              [class]="getButtonClasses(action) + ' dropdown-toggle'"
              [disabled]="isActionDisabled(action)"
              [title]="action.tooltip"
              data-bs-toggle="dropdown"
              aria-expanded="false">

              @if (action.loading) {
                <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              } @else if (action.icon) {
                <lucide-icon [name]="action.icon" [size]="getIconSize(action)" class="me-2"/>
              }

              {{ action.label }}

              @if (action.badge) {
                <span class="badge ms-2" [class]="getBadgeClass(action)">
                  {{ action.badge }}
                </span>
              }
            </button>

            <div class="dropdown-menu">
              @for (dropdownAction of action.dropdown; track dropdownAction.key) {
                @if (!dropdownAction.hidden) {
                  @if (dropdownAction.key === 'divider') {
                    <div class="dropdown-divider"></div>
                  } @else {
                    <button
                      type="button"
                      class="dropdown-item"
                      [class.disabled]="isActionDisabled(dropdownAction)"
                      [class.text-danger]="dropdownAction.destructive"
                      (click)="onActionClick(dropdownAction)">

                      @if (dropdownAction.icon) {
                        <lucide-icon [name]="dropdownAction.icon" [size]="16" class="me-2"/>
                      }

                      {{ dropdownAction.label }}

                      @if (dropdownAction.badge) {
                        <span class="badge ms-auto" [class]="getBadgeClass(dropdownAction)">
                          {{ dropdownAction.badge }}
                        </span>
                      }
                    </button>
                  }
                }
              }
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .btn-list {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .btn-list.vertical {
      flex-direction: column;
      align-items: stretch;
    }

    .btn-list.center {
      justify-content: center;
    }

    .btn-list.end {
      justify-content: flex-end;
    }

    .dropdown-item {
      display: flex;
      align-items: center;
    }

    .dropdown-item:not(.disabled):hover {
      background-color: var(--bs-dropdown-link-hover-bg);
    }

    .dropdown-item.disabled {
      opacity: 0.5;
      pointer-events: none;
    }

    .spinner-border-sm {
      width: 0.875rem;
      height: 0.875rem;
    }

    .badge {
      font-size: 0.75em;
    }
  `]
})
export class ActionButtonGroupComponent {
  @Input() actions: ActionButton[] = [];
  @Input() selectedItems: any[] = [];
  @Input() containerClass = '';
  @Input() defaultSize: 'sm' | 'md' | 'lg' = 'md';
  @Input() defaultVariant: ActionButton['variant'] = 'outline-primary';
  @Input() loading = false;
  @Input() permissions: string[] = [];

  @Output() actionClick = new EventEmitter<ActionEvent>();

  // State signals
  loadingActions = signal<string[]>([]);

  // Computed values
  visibleActions = computed(() =>
    this.actions.filter(action => !action.hidden && this.hasPermission(action))
  );

  onActionClick(action: ActionButton): void {
    if (this.isActionDisabled(action)) {
      return;
    }

    const event: ActionEvent = {
      action: action.key,
      selectedItems: this.selectedItems
    };

    this.actionClick.emit(event);
  }

  isActionDisabled(action: ActionButton): boolean {
    if (action.disabled || this.loading) {
      return true;
    }

    if (action.loading) {
      return true;
    }

    if (action.requiresSelection && (!this.selectedItems || this.selectedItems.length === 0)) {
      return true;
    }

    if (!this.hasPermission(action)) {
      return true;
    }

    return false;
  }

  hasPermission(action: ActionButton): boolean {
    if (!action.requiresPermission) {
      return true;
    }

    const requiredPermissions = Array.isArray(action.requiresPermission)
      ? action.requiresPermission
      : [action.requiresPermission];

    return requiredPermissions.some(permission =>
      this.permissions.includes(permission)
    );
  }

  getButtonClasses(action: ActionButton): string {
    const size = action.size || this.defaultSize;
    const variant = action.variant || this.defaultVariant;

    let classes = `btn btn-${variant}`;

    if (size !== 'md') {
      classes += ` btn-${size}`;
    }

    if (action.destructive && variant && !variant.includes('danger')) {
      classes += ' text-danger';
    }

    return classes;
  }

  getBadgeClass(action: ActionButton): string {
    if (action.destructive) {
      return 'bg-danger';
    }

    if (action.variant?.includes('primary')) {
      return 'bg-white text-primary';
    }

    return 'bg-secondary';
  }

  getIconSize(action: ActionButton): number {
    const size = action.size || this.defaultSize;

    switch (size) {
      case 'sm': return 14;
      case 'lg': return 20;
      default: return 16;
    }
  }

  // Public methods for external control
  setActionLoading(actionKey: string, loading: boolean): void {
    const currentLoading = this.loadingActions();

    if (loading) {
      if (!currentLoading.includes(actionKey)) {
        this.loadingActions.set([...currentLoading, actionKey]);
      }
    } else {
      this.loadingActions.set(currentLoading.filter(key => key !== actionKey));
    }

    // Update the action's loading state
    const action = this.actions.find(a => a.key === actionKey);
    if (action) {
      action.loading = loading;
    }
  }

  setActionDisabled(actionKey: string, disabled: boolean): void {
    const action = this.actions.find(a => a.key === actionKey);
    if (action) {
      action.disabled = disabled;
    }
  }

  setActionBadge(actionKey: string, badge: string | number | undefined): void {
    const action = this.actions.find(a => a.key === actionKey);
    if (action) {
      action.badge = badge;
    }
  }

  updateAction(actionKey: string, updates: Partial<ActionButton>): void {
    const action = this.actions.find(a => a.key === actionKey);
    if (action) {
      Object.assign(action, updates);
    }
  }
}