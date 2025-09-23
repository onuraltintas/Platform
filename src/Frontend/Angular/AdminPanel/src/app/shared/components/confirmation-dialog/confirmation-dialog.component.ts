import { Component, Inject, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatExpansionModule } from '@angular/material/expansion';

import {
  ConfirmationDialogConfig,
  ConfirmationDialogResult
} from './confirmation-dialog.models';

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatExpansionModule
  ],
  templateUrl: './confirmation-dialog.component.html',
  styleUrl: './confirmation-dialog.component.scss'
})
export class ConfirmationDialogComponent {
  public readonly config: ConfirmationDialogConfig;
  public readonly confirmationControl = new FormControl('');

  private readonly dialogRef = inject(MatDialogRef<ConfirmationDialogComponent>);

  // Computed properties
  public readonly headerClass = computed(() => {
    const iconColor = this.config.iconColor;
    return {
      'danger': iconColor === 'warn' && this.config.icon === 'dangerous',
      'warning': iconColor === 'warn' && this.config.icon === 'warning',
      'success': iconColor === 'success',
      'info': iconColor === 'info' || iconColor === 'primary'
    };
  });

  public readonly messageClass = computed(() => {
    const iconColor = this.config.iconColor;
    return {
      'danger': iconColor === 'warn' && this.config.icon === 'dangerous',
      'warning': iconColor === 'warn' && this.config.icon === 'warning'
    };
  });

  public readonly canConfirm = computed(() => {
    if (!this.config.requireTextConfirmation) {
      return true;
    }

    const value = this.confirmationControl.value?.trim() || '';
    const expectedText = this.config.confirmationText || '';
    return value === expectedText;
  });

  constructor(@Inject(MAT_DIALOG_DATA) data: ConfirmationDialogConfig) {
    this.config = this.mergeWithDefaults(data);
    this.setupValidation();
    this.setupKeyboardHandling();
    this.setupAutoFocus();
  }

  private mergeWithDefaults(config: ConfirmationDialogConfig): ConfirmationDialogConfig {
    return {
      confirmText: 'Tamam',
      cancelText: 'İptal',
      showCancelButton: true,
      confirmColor: 'primary',
      autoFocus: 'confirm',
      closeOnEscape: true,
      disableClose: false,
      hasBackdrop: true,
      ...config
    };
  }

  private setupValidation(): void {
    if (this.config.requireTextConfirmation) {
      this.confirmationControl.setValidators([
        Validators.required,
        (control) => {
          const value = control.value?.trim() || '';
          const expectedText = this.config.confirmationText || '';
          return value === expectedText ? null : { mismatch: true };
        }
      ]);
    }
  }

  private setupKeyboardHandling(): void {
    if (this.config.closeOnEscape) {
      this.dialogRef.keydownEvents().subscribe(event => {
        if (event.key === 'Escape') {
          this.cancel();
        }
      });
    }

    // Enter key handling
    this.dialogRef.keydownEvents().subscribe(event => {
      if (event.key === 'Enter' && this.canConfirm()) {
        this.confirm();
      }
    });
  }

  private setupAutoFocus(): void {
    setTimeout(() => {
      const autoFocus = this.config.autoFocus;
      if (autoFocus === 'confirm') {
        const confirmButton = document.querySelector('[mat-raised-button]') as HTMLElement;
        confirmButton?.focus();
      } else if (autoFocus === 'cancel') {
        const cancelButton = document.querySelector('[mat-button]') as HTMLElement;
        cancelButton?.focus();
      } else if (this.config.requireTextConfirmation) {
        const input = document.querySelector('input[matInput]') as HTMLElement;
        input?.focus();
      }
    }, 100);
  }

  public confirm(): void {
    if (!this.canConfirm()) {
      return;
    }

    const result: ConfirmationDialogResult = {
      confirmed: true,
      data: this.config.requireTextConfirmation ? this.confirmationControl.value : undefined
    };

    this.dialogRef.close(result);
  }

  public cancel(): void {
    const result: ConfirmationDialogResult = {
      confirmed: false
    };

    this.dialogRef.close(result);
  }
}