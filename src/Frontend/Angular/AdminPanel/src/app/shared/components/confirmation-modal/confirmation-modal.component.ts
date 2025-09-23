import { Component, Input, Output, EventEmitter, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, AlertTriangle, Info, CheckCircle, XCircle, X } from 'lucide-angular';

export interface ConfirmationConfig {
  title: string;
  message: string;
  type?: 'info' | 'warning' | 'danger' | 'success';
  confirmText?: string;
  cancelText?: string;
  confirmVariant?: 'primary' | 'danger' | 'warning' | 'success';
  showInput?: boolean;
  inputLabel?: string;
  inputPlaceholder?: string;
  inputRequired?: boolean;
  inputValue?: string;
  details?: string[];
  icon?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  backdrop?: boolean;
  keyboard?: boolean;
  focus?: boolean;
}

export interface ConfirmationResult {
  confirmed: boolean;
  inputValue?: string;
  data?: any;
}

@Component({
  selector: 'app-confirmation-modal',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (visible()) {
      <div class="modal fade show"
           [class.modal-blur]="config().backdrop !== false"
           style="display: block;"
           tabindex="-1"
           role="dialog"
           aria-hidden="false"
           (click)="onBackdropClick($event)">

        <div class="modal-dialog"
             [class]="getModalClasses()"
             role="document">

          <div class="modal-content">
            <!-- Header -->
            <div class="modal-header" [class]="getHeaderClasses()">
              <h4 class="modal-title d-flex align-items-center">
                @if (getIcon()) {
                  <lucide-icon [name]="getIcon()" [size]="20" class="me-2"/>
                }
                {{ config().title }}
              </h4>

              <button type="button"
                      class="btn-close"
                      (click)="onCancel()"
                      [disabled]="loading()"></button>
            </div>

            <!-- Body -->
            <div class="modal-body">
              <div [class]="getMessageClasses()">
                {{ config().message }}
              </div>

              <!-- Details -->
              @if (config().details?.length) {
                <div class="mt-3">
                  <ul class="list-unstyled mb-0">
                    @for (detail of config().details; track $index) {
                      <li class="d-flex align-items-start mb-1">
                        <lucide-icon name="chevron-right" [size]="16" class="me-2 mt-1 text-muted"/>
                        <span>{{ detail }}</span>
                      </li>
                    }
                  </ul>
                </div>
              }

              <!-- Input Field -->
              @if (config().showInput) {
                <div class="mt-3">
                  @if (config().inputLabel) {
                    <label class="form-label">{{ config().inputLabel }}</label>
                  }
                  <input type="text"
                         class="form-control"
                         [(ngModel)]="inputValue"
                         [placeholder]="config().inputPlaceholder"
                         [disabled]="loading()"
                         #inputRef>

                  @if (config().inputRequired && showValidation() && !inputValue.trim()) {
                    <div class="invalid-feedback d-block">
                      Bu alan zorunludur.
                    </div>
                  }
                </div>
              }
            </div>

            <!-- Footer -->
            <div class="modal-footer">
              <button type="button"
                      class="btn btn-outline-secondary"
                      [disabled]="loading()"
                      (click)="onCancel()">
                {{ config().cancelText || 'İptal' }}
              </button>

              <button type="button"
                      [class]="getConfirmButtonClasses()"
                      [disabled]="loading() || !isValid()"
                      (click)="onConfirm()">

                @if (loading()) {
                  <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                }

                {{ config().confirmText || 'Onayla' }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Backdrop -->
      <div class="modal-backdrop fade show"></div>
    }
  `,
  styles: [`
    .modal {
      z-index: 1055;
    }

    .modal-backdrop {
      z-index: 1054;
    }

    .modal-blur {
      backdrop-filter: blur(2px);
    }

    .modal-header.border-danger {
      border-bottom-color: var(--bs-danger) !important;
    }

    .modal-header.border-warning {
      border-bottom-color: var(--bs-warning) !important;
    }

    .modal-header.border-success {
      border-bottom-color: var(--bs-success) !important;
    }

    .modal-header.border-info {
      border-bottom-color: var(--bs-info) !important;
    }

    .btn-close:disabled {
      opacity: 0.3;
    }

    .list-unstyled li {
      word-break: break-word;
    }

    .invalid-feedback {
      font-size: 0.875rem;
    }
  `]
})
export class ConfirmationModalComponent implements OnInit {
  @Input() visible = signal(false);
  @Input() config = signal<ConfirmationConfig>({
    title: 'Onay',
    message: 'Bu işlemi gerçekleştirmek istediğinizden emin misiniz?'
  });

  @Output() result = new EventEmitter<ConfirmationResult>();
  @Output() cancel = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<ConfirmationResult>();

  // Icons
  readonly alertTriangleIcon = AlertTriangle;
  readonly infoIcon = Info;
  readonly checkCircleIcon = CheckCircle;
  readonly xCircleIcon = XCircle;
  readonly xIcon = X;

  // State
  loading = signal(false);
  showValidation = signal(false);
  inputValue = '';

  // Computed
  isValid = computed(() => {
    if (!this.config().showInput) {
      return true;
    }

    if (this.config().inputRequired) {
      return this.inputValue.trim().length > 0;
    }

    return true;
  });

  ngOnInit() {
    if (this.config().inputValue) {
      this.inputValue = this.config().inputValue || '';
    }

    // Handle keyboard events
    if (this.config().keyboard !== false) {
      document.addEventListener('keydown', this.onKeyDown.bind(this));
    }
  }

  ngOnDestroy() {
    document.removeEventListener('keydown', this.onKeyDown.bind(this));
  }

  onKeyDown(event: KeyboardEvent) {
    if (!this.visible()) return;

    if (event.key === 'Escape' && this.config().keyboard !== false) {
      this.onCancel();
    }

    if (event.key === 'Enter' && !event.shiftKey && !event.ctrlKey) {
      event.preventDefault();
      if (this.isValid() && !this.loading()) {
        this.onConfirm();
      }
    }
  }

  onBackdropClick(event: MouseEvent) {
    if (event.target === event.currentTarget && this.config().backdrop !== false) {
      this.onCancel();
    }
  }

  onCancel() {
    if (this.loading()) return;

    const result: ConfirmationResult = {
      confirmed: false,
      inputValue: this.inputValue
    };

    this.visible.set(false);
    this.cancel.emit();
    this.result.emit(result);
    this.resetState();
  }

  onConfirm() {
    if (this.loading() || !this.isValid()) {
      this.showValidation.set(true);
      return;
    }

    const result: ConfirmationResult = {
      confirmed: true,
      inputValue: this.inputValue
    };

    this.confirm.emit(result);
    this.result.emit(result);
    this.visible.set(false);
    this.resetState();
  }

  getModalClasses(): string {
    const size = this.config().size || 'md';
    let classes = 'modal-dialog-centered';

    if (size !== 'md') {
      classes += ` modal-${size}`;
    }

    return classes;
  }

  getHeaderClasses(): string {
    const type = this.config().type || 'info';
    return `border-bottom border-2 border-${this.getTypeColor(type)}`;
  }

  getMessageClasses(): string {
    const type = this.config().type || 'info';
    return `text-${this.getTypeColor(type)} fw-medium`;
  }

  getConfirmButtonClasses(): string {
    const variant = this.config().confirmVariant || this.getDefaultConfirmVariant();
    return `btn btn-${variant}`;
  }

  getDefaultConfirmVariant(): string {
    const type = this.config().type || 'info';

    switch (type) {
      case 'danger': return 'danger';
      case 'warning': return 'warning';
      case 'success': return 'success';
      default: return 'primary';
    }
  }

  getTypeColor(type: string): string {
    switch (type) {
      case 'danger': return 'danger';
      case 'warning': return 'warning';
      case 'success': return 'success';
      default: return 'info';
    }
  }

  getIcon(): string {
    if (this.config().icon) {
      return this.config().icon || 'info';
    }

    const type = this.config().type || 'info';

    switch (type) {
      case 'danger': return 'x-circle';
      case 'warning': return 'alert-triangle';
      case 'success': return 'check-circle';
      default: return 'info';
    }
  }

  private resetState() {
    this.loading.set(false);
    this.showValidation.set(false);
    this.inputValue = this.config().inputValue || '';
  }

  // Public methods for external control
  show(config: ConfirmationConfig): Promise<ConfirmationResult> {
    this.config.set({ ...config });
    this.visible.set(true);
    this.resetState();

    if (config.inputValue) {
      this.inputValue = config.inputValue;
    }

    return new Promise((resolve) => {
      const subscription = this.result.subscribe((result) => {
        resolve(result);
        subscription.unsubscribe();
      });
    });
  }

  hide() {
    this.visible.set(false);
    this.resetState();
  }

  setLoading(loading: boolean) {
    this.loading.set(loading);
  }

  setInputValue(value: string) {
    this.inputValue = value;
  }

  updateConfig(updates: Partial<ConfirmationConfig>) {
    this.config.set({ ...this.config(), ...updates });
  }
}