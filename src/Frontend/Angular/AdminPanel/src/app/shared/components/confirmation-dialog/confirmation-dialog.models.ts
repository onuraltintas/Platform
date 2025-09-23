import { TemplateRef } from '@angular/core';

export interface ConfirmationDialogConfig {
  // Dialog Configuration
  title: string;
  message: string;
  icon?: string;
  iconColor?: 'primary' | 'accent' | 'warn' | 'success' | 'info';
  width?: string;
  height?: string;
  maxWidth?: string;
  maxHeight?: string;
  disableClose?: boolean;
  hasBackdrop?: boolean;
  panelClass?: string | string[];

  // Button Configuration
  confirmText?: string;
  cancelText?: string;
  confirmColor?: 'primary' | 'accent' | 'warn';
  showCancelButton?: boolean;

  // Content Configuration
  html?: boolean; // Allow HTML in message
  customContent?: TemplateRef<any>;
  details?: string; // Additional details shown in expandable section

  // Behavior
  autoFocus?: 'confirm' | 'cancel' | 'none';
  closeOnEscape?: boolean;

  // Validation (for input confirmation)
  requireTextConfirmation?: boolean;
  confirmationText?: string; // Text that must be typed to confirm

  // Accessibility
  ariaLabel?: string;
  ariaDescribedBy?: string;
}

export interface ConfirmationDialogResult {
  confirmed: boolean;
  data?: any;
}

export type ConfirmationDialogType =
  | 'info'
  | 'warning'
  | 'danger'
  | 'success'
  | 'question';

export interface ConfirmationDialogPreset {
  type: ConfirmationDialogType;
  config: Partial<ConfirmationDialogConfig>;
}

// Preset configurations
export const CONFIRMATION_DIALOG_PRESETS: Record<ConfirmationDialogType, ConfirmationDialogPreset> = {
  info: {
    type: 'info',
    config: {
      icon: 'info',
      iconColor: 'info',
      confirmText: 'Tamam',
      showCancelButton: false,
      confirmColor: 'primary'
    }
  },
  warning: {
    type: 'warning',
    config: {
      icon: 'warning',
      iconColor: 'warn',
      confirmText: 'Devam Et',
      cancelText: 'İptal',
      confirmColor: 'warn'
    }
  },
  danger: {
    type: 'danger',
    config: {
      icon: 'dangerous',
      iconColor: 'warn',
      confirmText: 'Sil',
      cancelText: 'İptal',
      confirmColor: 'warn',
      requireTextConfirmation: true
    }
  },
  success: {
    type: 'success',
    config: {
      icon: 'check_circle',
      iconColor: 'success',
      confirmText: 'Tamam',
      showCancelButton: false,
      confirmColor: 'primary'
    }
  },
  question: {
    type: 'question',
    config: {
      icon: 'help',
      iconColor: 'primary',
      confirmText: 'Evet',
      cancelText: 'Hayır',
      confirmColor: 'primary'
    }
  }
};

// Quick access functions
export interface ConfirmationDialogOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  details?: string;
  requireTextConfirmation?: boolean;
  confirmationText?: string;
}

export interface IConfirmationDialogService {
  // Basic dialogs
  confirm(options: ConfirmationDialogOptions): Promise<boolean>;
  info(message: string, title?: string): Promise<void>;
  warning(message: string, title?: string): Promise<boolean>;
  danger(message: string, title?: string, requireConfirmation?: boolean): Promise<boolean>;
  success(message: string, title?: string): Promise<void>;

  // Advanced dialog
  open(config: ConfirmationDialogConfig): Promise<ConfirmationDialogResult>;

  // Preset dialogs
  deleteConfirmation(itemName?: string): Promise<boolean>;
  unsavedChanges(): Promise<boolean>;
  logoutConfirmation(): Promise<boolean>;
  bulkDeleteConfirmation(count: number): Promise<boolean>;
}