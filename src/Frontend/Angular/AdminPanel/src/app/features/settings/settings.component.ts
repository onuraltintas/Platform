import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {
  generalSettingsForm: FormGroup = new FormGroup({});
  securitySettingsForm: FormGroup = new FormGroup({});
  emailSettingsForm: FormGroup = new FormGroup({});
  backupSettingsForm: FormGroup = new FormGroup({});

  savingGeneral = false;
  savingSecurity = false;
  savingEmail = false;
  savingBackup = false;
  testingEmail = false;
  creatingBackup = false;

  constructor(private fb: FormBuilder) {
    this.initializeForms();
  }

  private initializeForms(): void {
    // General Settings Form
    this.generalSettingsForm = this.fb.group({
      systemName: ['Platform V1', [Validators.required]],
      systemDescription: ['Admin Panel Yönetim Sistemi'],
      language: ['tr'],
      timezone: ['Europe/Istanbul'],
      maintenanceMode: [false]
    });

    // Security Settings Form
    this.securitySettingsForm = this.fb.group({
      sessionTimeout: [30, [Validators.required, Validators.min(5), Validators.max(480)]],
      passwordPolicy: ['medium'],
      maxLoginAttempts: [5, [Validators.required, Validators.min(3), Validators.max(10)]],
      requireTwoFactor: [false],
      enableAuditLog: [true]
    });

    // Email Settings Form
    this.emailSettingsForm = this.fb.group({
      smtpHost: [''],
      smtpPort: [587],
      smtpSecurity: ['tls'],
      smtpUsername: [''],
      smtpPassword: [''],
      fromEmail: ['']
    });

    // Backup Settings Form
    this.backupSettingsForm = this.fb.group({
      enableAutoBackup: [true],
      backupFrequency: ['daily'],
      backupTime: ['02:00'],
      backupRetention: [30, [Validators.required, Validators.min(1), Validators.max(365)]]
    });
  }

  isFieldInvalid(fieldName: string, formType: string): boolean {
    let form: FormGroup;

    switch (formType) {
      case 'general':
        form = this.generalSettingsForm;
        break;
      case 'security':
        form = this.securitySettingsForm;
        break;
      case 'email':
        form = this.emailSettingsForm;
        break;
      case 'backup':
        form = this.backupSettingsForm;
        break;
      default:
        return false;
    }

    const field = form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSaveGeneral(): void {
    if (this.generalSettingsForm.invalid) {
      this.generalSettingsForm.markAllAsTouched();
      return;
    }

    this.savingGeneral = true;
    setTimeout(() => {
      this.savingGeneral = false;
      console.log('General settings saved:', this.generalSettingsForm.value);
    }, 1500);
  }

  onSaveSecurity(): void {
    if (this.securitySettingsForm.invalid) {
      this.securitySettingsForm.markAllAsTouched();
      return;
    }

    this.savingSecurity = true;
    setTimeout(() => {
      this.savingSecurity = false;
      console.log('Security settings saved:', this.securitySettingsForm.value);
    }, 1500);
  }

  onSaveEmail(): void {
    if (this.emailSettingsForm.invalid) {
      this.emailSettingsForm.markAllAsTouched();
      return;
    }

    this.savingEmail = true;
    setTimeout(() => {
      this.savingEmail = false;
      console.log('Email settings saved:', this.emailSettingsForm.value);
    }, 1500);
  }

  onSaveBackup(): void {
    if (this.backupSettingsForm.invalid) {
      this.backupSettingsForm.markAllAsTouched();
      return;
    }

    this.savingBackup = true;
    setTimeout(() => {
      this.savingBackup = false;
      console.log('Backup settings saved:', this.backupSettingsForm.value);
    }, 1500);
  }

  testEmailConnection(): void {
    this.testingEmail = true;
    setTimeout(() => {
      this.testingEmail = false;
      console.log('Email connection test completed');
      // Here you would show a success/error message
    }, 2000);
  }

  createManualBackup(): void {
    this.creatingBackup = true;
    setTimeout(() => {
      this.creatingBackup = false;
      console.log('Manual backup created successfully');
      // Here you would show a success message and download link
    }, 3000);
  }
}