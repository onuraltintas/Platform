import {
  Component,
  Inject,
  OnInit,
  OnDestroy,
  signal,
  computed,
  effect,
  inject,
  ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import {
  MatDialogModule,
  MatDialogRef,
  MAT_DIALOG_DATA
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';

import { ToastService } from '../../../core/bildirimler/toast.service';
import { LoadingService } from '../../../core/services/loading.service';
import {
  FormDialogConfig,
  FormDialogResult,
  FormDialogState,
  FormDialogEvents,
  FormFieldConfig,
  FormFieldType,
  FormValidationSummary,
  FormFieldError,
  DynamicFormControl
} from './form-dialog.models';

@Component({
  selector: 'app-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatChipsModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatDividerModule,
    MatTooltipModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-dialog.component.html',
  styleUrl: './form-dialog.component.scss'
})
export class FormDialogComponent<T = any> implements OnInit, OnDestroy {
  // Injects
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);
  private readonly loading = inject(LoadingService);
  private readonly dialogRef = inject(MatDialogRef<FormDialogComponent<T>>);

  // Form
  public form!: FormGroup;
  public formControls = new Map<string, DynamicFormControl>();

  // Signals
  public readonly state = signal<FormDialogState>({
    formValid: false,
    formTouched: false,
    formDirty: false,
    formPending: false,
    loading: false,
    saving: false,
    autoSaving: false,
    validationSummary: { hasErrors: false, errorCount: 0, errors: [] },
    showValidationErrors: false
  });

  // Computed
  public readonly visibleFields = computed(() =>
    this.config.formFields
      .filter(field => !field.hidden && this.shouldShowField(field))
      .sort((a, b) => (a.order || 0) - (b.order || 0))
  );

  private readonly destroy$ = new Subject<void>();
  private autoSaveTimer?: any;

  constructor(
    @Inject(MAT_DIALOG_DATA) public config: FormDialogConfig<T>,
    public events: FormDialogEvents<T> = {}
  ) {
    // Form state effects
    effect(() => {
      if (this.form) {
        this.updateFormState();
        this.updateValidationSummary();
      }
    });

    // Auto-save effect
    effect(() => {
      if (this.config.autoSave && this.form && this.state().formDirty) {
        this.setupAutoSave();
      }
    });
  }

  ngOnInit(): void {
    this.buildForm();
    this.setupFormSubscriptions();
    this.loadInitialData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();

    if (this.autoSaveTimer) {
      clearInterval(this.autoSaveTimer);
    }
  }

  // Public methods
  public async onSubmit(): Promise<void> {
    if (!this.canSubmit()) {
      this.state.update(s => ({ ...s, showValidationErrors: true }));
      return;
    }

    const formData = this.form.value;

    this.state.update(s => ({ ...s, saving: true }));

    try {
      // Validate before submit
      if (this.events.beforeSubmit) {
        const canProceed = await this.events.beforeSubmit(formData, this.form);
        if (!canProceed) {
          return;
        }
      }

      // Submit logic will be handled by parent
      const result: FormDialogResult<T> = {
        action: 'submit',
        data: formData,
        formData: formData
      };

      if (this.events.afterSubmit) {
        this.events.afterSubmit(result, formData, this.form);
      }

      this.toast.basari(`${this.config.mode === 'create' ? 'Oluşturma' : 'Güncelleme'} başarılı`);
      this.dialogRef.close(result);

    } catch (error: any) {
      this.toast.apiHatasi(error);
    } finally {
      this.state.update(s => ({ ...s, saving: false }));
    }
  }

  public async onReset(): Promise<void> {
    if (this.events.beforeReset) {
      const canProceed = await this.events.beforeReset(this.form);
      if (!canProceed) {
        return;
      }
    }

    this.form.reset();
    this.loadInitialData();
    this.toast.bilgi('Form sıfırlandı');
  }

  public async executeCustomAction(action: any): Promise<void> {
    try {
      await action.action(this.form, this.config.data);
    } catch (error: any) {
      this.toast.apiHatasi(error);
    }
  }

  // Helper methods
  public canSubmit(): boolean {
    return this.form.valid && !this.state().saving && this.config.mode !== 'view';
  }

  public isTextFieldType(type: FormFieldType): boolean {
    return ['text', 'email', 'password', 'number', 'tel', 'url'].includes(type);
  }

  public isDateFieldType(type: FormFieldType): boolean {
    return ['date', 'datetime', 'time'].includes(type);
  }

  public getInputType(fieldType: FormFieldType): string {
    switch (fieldType) {
      case 'email': return 'email';
      case 'password': return 'password';
      case 'number': return 'number';
      case 'tel': return 'tel';
      case 'url': return 'url';
      default: return 'text';
    }
  }

  public isFieldRequired(fieldKey: string): boolean {
    const control = this.form.get(fieldKey);
    return control?.hasError('required') ?? false;
  }

  public hasFieldError(fieldKey: string): boolean {
    const control = this.form.get(fieldKey);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  public getFieldErrorMessage(fieldKey: string, field: FormFieldConfig): string {
    const control = this.form.get(fieldKey);
    if (!control || !control.errors) return '';

    const errors = control.errors;

    // Custom error messages
    if (field.errorMessages) {
      for (const errorType in errors) {
        if (field.errorMessages[errorType]) {
          return field.errorMessages[errorType];
        }
      }
    }

    // Default error messages
    if (errors['required']) return `${field.label} gereklidir`;
    if (errors['email']) return 'Geçerli bir e-posta adresi girin';
    if (errors['minlength']) return `En az ${errors['minlength'].requiredLength} karakter olmalıdır`;
    if (errors['maxlength']) return `En fazla ${errors['maxlength'].requiredLength} karakter olabilir`;
    if (errors['min']) return `En az ${errors['min'].min} olmalıdır`;
    if (errors['max']) return `En fazla ${errors['max'].max} olabilir`;
    if (errors['pattern']) return 'Geçersiz format';

    return 'Geçersiz değer';
  }

  public getFieldOptions(field: FormFieldConfig): any[] {
    return Array.isArray(field.options) ? field.options : [];
  }

  public shouldShowField(field: FormFieldConfig): boolean {
    if (!field.conditionalLogic) return true;

    return field.conditionalLogic.every(condition => {
      const dependentControl = this.form.get(condition.field);
      if (!dependentControl) return true;

      const dependentValue = dependentControl.value;
      let conditionMet = false;

      switch (condition.operator) {
        case 'equals':
          conditionMet = dependentValue === condition.value;
          break;
        case 'notEquals':
          conditionMet = dependentValue !== condition.value;
          break;
        case 'contains':
          conditionMet = String(dependentValue).includes(String(condition.value));
          break;
        case 'in':
          conditionMet = Array.isArray(condition.value) && condition.value.includes(dependentValue);
          break;
        default:
          conditionMet = true;
      }

      return condition.action === 'show' ? conditionMet : !conditionMet;
    });
  }

  public trackByField(index: number, field: FormFieldConfig): string {
    return field.key;
  }

  public getFieldWrapperClass(field: FormFieldConfig): string {
    const classes = ['form-field'];

    if (field.colSpan) {
      classes.push(`col-span-${field.colSpan}`);
    }

    if (field.width) {
      classes.push('custom-width');
    }

    return classes.join(' ');
  }

  public getGridColumns(): string {
    const columns = this.config.columnsCount || 2;
    return `repeat(${columns}, 1fr)`;
  }

  public getVisibleActions(): any[] {
    return (this.config.customActions || []).filter(action => {
      if (typeof action.hidden === 'function') {
        return !action.hidden(this.form);
      }
      return !action.hidden;
    });
  }

  public isActionDisabled(action: any): boolean {
    if (typeof action.disabled === 'function') {
      return action.disabled(this.form);
    }
    return !!action.disabled;
  }

  public getSubmitIcon(): string {
    switch (this.config.mode) {
      case 'create': return 'add';
      case 'edit': return 'save';
      case 'clone': return 'content_copy';
      default: return 'save';
    }
  }

  public getSubmitText(): string {
    if (this.state().saving) {
      return this.config.mode === 'create' ? 'Oluşturuluyor...' : 'Güncelleniyor...';
    }

    return this.config.submitText ||
           (this.config.mode === 'create' ? 'Oluştur' : 'Güncelle');
  }

  // Private methods
  private buildForm(): void {
    const group: any = {};

    this.config.formFields.forEach(field => {
      const validators = this.buildValidators(field);
      const initialValue = this.getInitialValue(field);

      group[field.key] = [
        { value: initialValue, disabled: field.disabled || field.readonly },
        validators
      ];
    });

    this.form = this.fb.group(group);
  }

  private buildValidators(field: FormFieldConfig): any[] {
    const validators: any[] = [];

    if (field.required) {
      validators.push(Validators.required);
    }

    if (field.type === 'email') {
      validators.push(Validators.email);
    }

    if (field.minLength) {
      validators.push(Validators.minLength(field.minLength));
    }

    if (field.maxLength) {
      validators.push(Validators.maxLength(field.maxLength));
    }

    if (field.min !== undefined) {
      validators.push(Validators.min(field.min));
    }

    if (field.max !== undefined) {
      validators.push(Validators.max(field.max));
    }

    if (field.pattern) {
      validators.push(Validators.pattern(field.pattern));
    }

    // Custom validators
    if (field.validators) {
      field.validators.forEach(validator => {
        if (validator.validatorFn) {
          validators.push(validator.validatorFn);
        }
      });
    }

    return validators;
  }

  private getInitialValue(field: FormFieldConfig): any {
    if (this.config.data && this.config.data.hasOwnProperty(field.key)) {
      return (this.config.data as any)[field.key];
    }

    switch (field.type) {
      case 'checkbox':
      case 'toggle':
        return false;
      case 'multiselect':
      case 'chips':
        return [];
      case 'number':
        return field.min || 0;
      default:
        return '';
    }
  }

  private setupFormSubscriptions(): void {
    // Form value changes
    this.form.valueChanges
      .pipe(
        debounceTime(100),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(value => {
        if (this.events.formValueChange) {
          this.events.formValueChange(value, this.form);
        }
      });

    // Form status changes
    this.form.statusChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(status => {
        if (this.events.formValidityChange) {
          this.events.formValidityChange(status === 'VALID', this.form);
        }
      });

    // Field value changes
    Object.keys(this.form.controls).forEach(fieldKey => {
      const control = this.form.get(fieldKey);
      if (control) {
        control.valueChanges
          .pipe(
            debounceTime(100),
            distinctUntilChanged(),
            takeUntil(this.destroy$)
          )
          .subscribe(value => {
            if (this.events.fieldValueChange) {
              this.events.fieldValueChange(fieldKey, value, this.form);
            }
          });
      }
    });
  }

  private loadInitialData(): void {
    if (this.config.data && this.config.mode !== 'create') {
      this.form.patchValue(this.config.data);
    }
  }

  private updateFormState(): void {
    this.state.update(state => ({
      ...state,
      formValid: this.form.valid,
      formTouched: this.form.touched,
      formDirty: this.form.dirty,
      formPending: this.form.pending
    }));
  }

  private updateValidationSummary(): void {
    const errors: FormFieldError[] = [];

    this.config.formFields.forEach(field => {
      const control = this.form.get(field.key);
      if (control && control.invalid && (control.dirty || control.touched)) {
        const fieldErrors: string[] = [];

        if (control.errors) {
          Object.keys(control.errors).forEach(errorType => {
            fieldErrors.push(this.getFieldErrorMessage(field.key, field));
          });
        }

        if (fieldErrors.length > 0) {
          errors.push({
            field: field.key,
            label: field.label,
            errors: fieldErrors
          });
        }
      }
    });

    const validationSummary: FormValidationSummary = {
      hasErrors: errors.length > 0,
      errorCount: errors.length,
      errors
    };

    this.state.update(state => ({
      ...state,
      validationSummary
    }));
  }

  private setupAutoSave(): void {
    if (this.autoSaveTimer) {
      clearInterval(this.autoSaveTimer);
    }

    const interval = this.config.autoSaveInterval || 30000; // 30 seconds default

    this.autoSaveTimer = setInterval(async () => {
      if (this.form.valid && this.form.dirty && this.events.autoSave) {
        this.state.update(s => ({ ...s, autoSaving: true }));

        try {
          await this.events.autoSave(this.form.value, this.form);

          this.state.update(s => ({
            ...s,
            autoSaving: false,
            lastSaved: new Date()
          }));

          if (this.events.autoSaveSuccess) {
            this.events.autoSaveSuccess(this.form.value);
          }
        } catch (error) {
          this.state.update(s => ({ ...s, autoSaving: false }));

          if (this.events.autoSaveError) {
            this.events.autoSaveError(error);
          }
        }
      }
    }, interval);
  }
}