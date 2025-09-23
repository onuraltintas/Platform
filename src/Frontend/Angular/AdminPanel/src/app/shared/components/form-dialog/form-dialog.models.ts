import { TemplateRef } from '@angular/core';
import { FormGroup, AbstractControl } from '@angular/forms';
import { Observable } from 'rxjs';

export interface FormDialogConfig<T = any> {
  // Dialog Configuration
  title: string;
  subtitle?: string;
  width?: string;
  height?: string;
  maxWidth?: string;
  maxHeight?: string;
  disableClose?: boolean;
  hasBackdrop?: boolean;
  panelClass?: string | string[];

  // Data
  data?: T;
  mode: FormDialogMode;

  // Form Configuration
  formFields: FormFieldConfig[];
  submitText?: string;
  cancelText?: string;
  resetText?: string;

  // Validation
  showValidationSummary?: boolean;
  validateOnChange?: boolean;
  showRequiredIndicator?: boolean;

  // Layout
  layout?: FormLayout;
  columnsCount?: number;
  fieldSpacing?: 'compact' | 'normal' | 'spacious';

  // Behavior
  showProgressBar?: boolean;
  autoSave?: boolean;
  autoSaveInterval?: number;
  confirmBeforeClose?: boolean;
  resetFormOnSubmit?: boolean;

  // Actions
  customActions?: FormAction[];
  showResetButton?: boolean;
  showCancelButton?: boolean;

  // Accessibility
  ariaLabel?: string;
  ariaDescribedBy?: string;
}

export type FormDialogMode = 'create' | 'edit' | 'view' | 'clone';

export type FormLayout = 'vertical' | 'horizontal' | 'grid' | 'tabs' | 'steps';

export interface FormFieldConfig {
  // Basic Properties
  key: string;
  label: string;
  type: FormFieldType;
  placeholder?: string;
  hint?: string;
  required?: boolean;
  disabled?: boolean;
  readonly?: boolean;
  hidden?: boolean;

  // Validation
  validators?: FormFieldValidator[];
  asyncValidators?: FormFieldAsyncValidator[];
  errorMessages?: { [key: string]: string };

  // Layout
  width?: string;
  order?: number;
  colSpan?: number;
  rowSpan?: number;
  breakpoint?: FormFieldBreakpoint;

  // Type-specific configurations
  options?: FormFieldOption[] | Observable<FormFieldOption[]>;
  multiple?: boolean;
  searchable?: boolean;
  clearable?: boolean;
  allowCustomValues?: boolean;

  // File upload specific
  accept?: string;
  maxFileSize?: number;
  maxFiles?: number;

  // Number specific
  min?: number;
  max?: number;
  step?: number;

  // Text specific
  minLength?: number;
  maxLength?: number;
  pattern?: string;

  // Date specific
  minDate?: Date;
  maxDate?: Date;
  dateFormat?: string;

  // Conditional Logic
  conditionalLogic?: FormFieldCondition[];

  // Templates
  customTemplate?: TemplateRef<any>;
  labelTemplate?: TemplateRef<any>;
  errorTemplate?: TemplateRef<any>;

  // Event Handlers
  onChange?: (value: any, form: FormGroup) => void;
  onFocus?: (event: Event) => void;
  onBlur?: (event: Event) => void;
}

export type FormFieldType =
  | 'text'
  | 'email'
  | 'password'
  | 'number'
  | 'tel'
  | 'url'
  | 'textarea'
  | 'select'
  | 'multiselect'
  | 'autocomplete'
  | 'checkbox'
  | 'radio'
  | 'toggle'
  | 'slider'
  | 'date'
  | 'datetime'
  | 'time'
  | 'daterange'
  | 'file'
  | 'image'
  | 'color'
  | 'rating'
  | 'chips'
  | 'editor'
  | 'json'
  | 'custom';

export interface FormFieldOption {
  value: any;
  label: string;
  description?: string;
  icon?: string;
  avatar?: string;
  disabled?: boolean;
  group?: string;
}

export interface FormFieldValidator {
  type: 'required' | 'email' | 'min' | 'max' | 'minLength' | 'maxLength' | 'pattern' | 'custom';
  value?: any;
  message?: string;
  validatorFn?: (control: AbstractControl) => { [key: string]: any } | null;
}

export interface FormFieldAsyncValidator {
  validatorFn: (control: AbstractControl) => Observable<{ [key: string]: any } | null>;
  message?: string;
  debounceTime?: number;
}

export interface FormFieldCondition {
  field: string;
  operator: 'equals' | 'notEquals' | 'contains' | 'notContains' | 'greaterThan' | 'lessThan' | 'in' | 'notIn';
  value: any;
  action: 'show' | 'hide' | 'enable' | 'disable' | 'require' | 'unrequire';
}

export interface FormFieldBreakpoint {
  xs?: number;
  sm?: number;
  md?: number;
  lg?: number;
  xl?: number;
}

export interface FormAction {
  id: string;
  label: string;
  icon?: string;
  color?: 'primary' | 'accent' | 'warn';
  type?: 'button' | 'submit';
  disabled?: boolean | ((form: FormGroup) => boolean);
  hidden?: boolean | ((form: FormGroup) => boolean);
  action: (form: FormGroup, data?: any) => void | Promise<void>;
}

export interface FormDialogResult<T = any> {
  action: 'submit' | 'cancel' | 'reset' | 'custom';
  data?: T;
  formData?: any;
  customActionId?: string;
}

export interface FormValidationSummary {
  hasErrors: boolean;
  errorCount: number;
  errors: FormFieldError[];
}

export interface FormFieldError {
  field: string;
  label: string;
  errors: string[];
}

export interface FormDialogState {
  // Form state
  formValid: boolean;
  formTouched: boolean;
  formDirty: boolean;
  formPending: boolean;

  // UI state
  loading: boolean;
  saving: boolean;
  autoSaving: boolean;
  lastSaved?: Date;

  // Validation state
  validationSummary: FormValidationSummary;
  showValidationErrors: boolean;

  // Step state (for multi-step forms)
  currentStep?: number;
  totalSteps?: number;
  stepValid?: boolean;
}

export interface FormDialogEvents<T = any> {
  // Form events
  formValueChange?: (value: any, form: FormGroup) => void;
  formValidityChange?: (valid: boolean, form: FormGroup) => void;
  fieldValueChange?: (fieldKey: string, value: any, form: FormGroup) => void;

  // Dialog events
  beforeSubmit?: (data: T, form: FormGroup) => boolean | Promise<boolean>;
  afterSubmit?: (result: any, data: T, form: FormGroup) => void;
  beforeCancel?: (form: FormGroup) => boolean | Promise<boolean>;
  beforeReset?: (form: FormGroup) => boolean | Promise<boolean>;

  // Auto-save events
  autoSave?: (data: any, form: FormGroup) => Promise<void>;
  autoSaveSuccess?: (data: any) => void;
  autoSaveError?: (error: any) => void;

  // Step events (for multi-step forms)
  stepChange?: (currentStep: number, previousStep: number) => void;
  stepValidate?: (step: number, form: FormGroup) => boolean | Promise<boolean>;
}

// Preset configurations
export const FORM_DIALOG_PRESETS = {
  SIMPLE: {
    layout: 'vertical' as FormLayout,
    showResetButton: false,
    showValidationSummary: false,
    fieldSpacing: 'normal' as const
  },
  ADVANCED: {
    layout: 'grid' as FormLayout,
    showResetButton: true,
    showValidationSummary: true,
    showRequiredIndicator: true,
    validateOnChange: true,
    fieldSpacing: 'normal' as const
  },
  COMPACT: {
    layout: 'vertical' as FormLayout,
    fieldSpacing: 'compact' as const,
    showCancelButton: false,
    width: '400px'
  },
  FULL_FEATURED: {
    layout: 'tabs' as FormLayout,
    showResetButton: true,
    showValidationSummary: true,
    showRequiredIndicator: true,
    validateOnChange: true,
    autoSave: true,
    autoSaveInterval: 30000,
    confirmBeforeClose: true,
    showProgressBar: true
  }
} as const;

// Utility types
export type FormFieldValue<T extends FormFieldType> =
  T extends 'number' ? number :
  T extends 'date' | 'datetime' | 'time' ? Date :
  T extends 'checkbox' | 'toggle' ? boolean :
  T extends 'multiselect' | 'chips' ? any[] :
  T extends 'file' | 'image' ? File | File[] :
  string;

export type FormData<T> = {
  [K in keyof T]: T[K];
};

// Form builder helper types
export interface FormBuilderConfig<T = any> {
  data?: T;
  fields: FormFieldConfig[];
  mode: FormDialogMode;
}

export interface DynamicFormControl {
  config: FormFieldConfig;
  control: AbstractControl;
  visible: boolean;
  enabled: boolean;
  required: boolean;
}