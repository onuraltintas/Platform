import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="settings-container">
      <!-- Header -->
      <div class="row mb-4">
        <div class="col-12">
          <h1 class="h3 mb-1">Sistem Ayarları</h1>
          <p class="text-muted mb-0">Sistem geneli ayarlar ve konfigürasyonlar</p>
        </div>
      </div>

      <div class="row">
        <!-- General Settings -->
        <div class="col-lg-6 mb-4">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-cog me-2"></i>
                Genel Ayarlar
              </h5>
            </div>
            <div class="card-body">
              <form [formGroup]="generalSettingsForm" (ngSubmit)="onSaveGeneral()">
                <div class="mb-3">
                  <label for="systemName" class="form-label">Sistem Adı</label>
                  <input
                    type="text"
                    id="systemName"
                    class="form-control"
                    formControlName="systemName"
                    [class.is-invalid]="isFieldInvalid('systemName', 'general')"
                  >
                  <div *ngIf="isFieldInvalid('systemName', 'general')" class="invalid-feedback">
                    Sistem adı zorunludur.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="systemDescription" class="form-label">Sistem Açıklaması</label>
                  <textarea
                    id="systemDescription"
                    class="form-control"
                    formControlName="systemDescription"
                    rows="3"
                  ></textarea>
                </div>

                <div class="mb-3">
                  <label for="language" class="form-label">Varsayılan Dil</label>
                  <select id="language" class="form-select" formControlName="language">
                    <option value="tr">Türkçe</option>
                    <option value="en">English</option>
                    <option value="de">Deutsch</option>
                  </select>
                </div>

                <div class="mb-3">
                  <label for="timezone" class="form-label">Zaman Dilimi</label>
                  <select id="timezone" class="form-select" formControlName="timezone">
                    <option value="Europe/Istanbul">İstanbul (UTC+3)</option>
                    <option value="UTC">UTC (UTC+0)</option>
                    <option value="Europe/London">Londra (UTC+0)</option>
                    <option value="America/New_York">New York (UTC-5)</option>
                  </select>
                </div>

                <div class="form-check mb-3">
                  <input
                    type="checkbox"
                    id="maintenanceMode"
                    class="form-check-input"
                    formControlName="maintenanceMode"
                  >
                  <label for="maintenanceMode" class="form-check-label">
                    Bakım Modu
                  </label>
                  <small class="form-text text-muted d-block">
                    Aktif olduğunda sadece yöneticiler sisteme erişebilir
                  </small>
                </div>

                <button
                  type="submit"
                  class="btn btn-primary"
                  [disabled]="generalSettingsForm.invalid || savingGeneral"
                >
                  <span *ngIf="savingGeneral" class="spinner-border spinner-border-sm me-2"></span>
                  <i *ngIf="!savingGeneral" class="fas fa-save me-2"></i>
                  Kaydet
                </button>
              </form>
            </div>
          </div>
        </div>

        <!-- Security Settings -->
        <div class="col-lg-6 mb-4">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-shield-alt me-2"></i>
                Güvenlik Ayarları
              </h5>
            </div>
            <div class="card-body">
              <form [formGroup]="securitySettingsForm" (ngSubmit)="onSaveSecurity()">
                <div class="mb-3">
                  <label for="sessionTimeout" class="form-label">Oturum Zaman Aşımı (dakika)</label>
                  <input
                    type="number"
                    id="sessionTimeout"
                    class="form-control"
                    formControlName="sessionTimeout"
                    min="5"
                    max="480"
                  >
                </div>

                <div class="mb-3">
                  <label for="passwordPolicy" class="form-label">Şifre Politikası</label>
                  <select id="passwordPolicy" class="form-select" formControlName="passwordPolicy">
                    <option value="basic">Temel (8 karakter minimum)</option>
                    <option value="medium">Orta (8 karakter, büyük/küçük harf)</option>
                    <option value="strong">Güçlü (8 karakter, büyük/küçük harf, sayı, özel karakter)</option>
                  </select>
                </div>

                <div class="mb-3">
                  <label for="maxLoginAttempts" class="form-label">Maksimum Giriş Denemesi</label>
                  <input
                    type="number"
                    id="maxLoginAttempts"
                    class="form-control"
                    formControlName="maxLoginAttempts"
                    min="3"
                    max="10"
                  >
                </div>

                <div class="form-check mb-3">
                  <input
                    type="checkbox"
                    id="requireTwoFactor"
                    class="form-check-input"
                    formControlName="requireTwoFactor"
                  >
                  <label for="requireTwoFactor" class="form-check-label">
                    İki Faktörlü Kimlik Doğrulama Zorunlu
                  </label>
                </div>

                <div class="form-check mb-3">
                  <input
                    type="checkbox"
                    id="enableAuditLog"
                    class="form-check-input"
                    formControlName="enableAuditLog"
                  >
                  <label for="enableAuditLog" class="form-check-label">
                    Denetim Günlüğü
                  </label>
                </div>

                <button
                  type="submit"
                  class="btn btn-primary"
                  [disabled]="securitySettingsForm.invalid || savingSecurity"
                >
                  <span *ngIf="savingSecurity" class="spinner-border spinner-border-sm me-2"></span>
                  <i *ngIf="!savingSecurity" class="fas fa-save me-2"></i>
                  Kaydet
                </button>
              </form>
            </div>
          </div>
        </div>

        <!-- Email Settings -->
        <div class="col-lg-6 mb-4">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-envelope me-2"></i>
                E-posta Ayarları
              </h5>
            </div>
            <div class="card-body">
              <form [formGroup]="emailSettingsForm" (ngSubmit)="onSaveEmail()">
                <div class="mb-3">
                  <label for="smtpHost" class="form-label">SMTP Sunucu</label>
                  <input
                    type="text"
                    id="smtpHost"
                    class="form-control"
                    formControlName="smtpHost"
                    placeholder="smtp.gmail.com"
                  >
                </div>

                <div class="row mb-3">
                  <div class="col-md-6">
                    <label for="smtpPort" class="form-label">Port</label>
                    <input
                      type="number"
                      id="smtpPort"
                      class="form-control"
                      formControlName="smtpPort"
                      placeholder="587"
                    >
                  </div>
                  <div class="col-md-6">
                    <label for="smtpSecurity" class="form-label">Güvenlik</label>
                    <select id="smtpSecurity" class="form-select" formControlName="smtpSecurity">
                      <option value="none">Yok</option>
                      <option value="tls">TLS</option>
                      <option value="ssl">SSL</option>
                    </select>
                  </div>
                </div>

                <div class="mb-3">
                  <label for="smtpUsername" class="form-label">Kullanıcı Adı</label>
                  <input
                    type="text"
                    id="smtpUsername"
                    class="form-control"
                    formControlName="smtpUsername"
                  >
                </div>

                <div class="mb-3">
                  <label for="smtpPassword" class="form-label">Şifre</label>
                  <input
                    type="password"
                    id="smtpPassword"
                    class="form-control"
                    formControlName="smtpPassword"
                  >
                </div>

                <div class="mb-3">
                  <label for="fromEmail" class="form-label">Gönderen E-posta</label>
                  <input
                    type="email"
                    id="fromEmail"
                    class="form-control"
                    formControlName="fromEmail"
                    placeholder="noreply@company.com"
                  >
                </div>

                <div class="d-flex gap-2">
                  <button
                    type="button"
                    class="btn btn-outline-secondary"
                    (click)="testEmailConnection()"
                    [disabled]="testingEmail"
                  >
                    <span *ngIf="testingEmail" class="spinner-border spinner-border-sm me-2"></span>
                    <i *ngIf="!testingEmail" class="fas fa-vial me-2"></i>
                    Test Et
                  </button>
                  <button
                    type="submit"
                    class="btn btn-primary"
                    [disabled]="emailSettingsForm.invalid || savingEmail"
                  >
                    <span *ngIf="savingEmail" class="spinner-border spinner-border-sm me-2"></span>
                    <i *ngIf="!savingEmail" class="fas fa-save me-2"></i>
                    Kaydet
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>

        <!-- Backup Settings -->
        <div class="col-lg-6 mb-4">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                <i class="fas fa-database me-2"></i>
                Yedekleme Ayarları
              </h5>
            </div>
            <div class="card-body">
              <form [formGroup]="backupSettingsForm" (ngSubmit)="onSaveBackup()">
                <div class="form-check mb-3">
                  <input
                    type="checkbox"
                    id="enableAutoBackup"
                    class="form-check-input"
                    formControlName="enableAutoBackup"
                  >
                  <label for="enableAutoBackup" class="form-check-label">
                    Otomatik Yedekleme
                  </label>
                </div>

                <div class="mb-3" *ngIf="backupSettingsForm.get('enableAutoBackup')?.value">
                  <label for="backupFrequency" class="form-label">Yedekleme Sıklığı</label>
                  <select id="backupFrequency" class="form-select" formControlName="backupFrequency">
                    <option value="daily">Günlük</option>
                    <option value="weekly">Haftalık</option>
                    <option value="monthly">Aylık</option>
                  </select>
                </div>

                <div class="mb-3" *ngIf="backupSettingsForm.get('enableAutoBackup')?.value">
                  <label for="backupTime" class="form-label">Yedekleme Saati</label>
                  <input
                    type="time"
                    id="backupTime"
                    class="form-control"
                    formControlName="backupTime"
                  >
                </div>

                <div class="mb-3">
                  <label for="backupRetention" class="form-label">Yedek Saklama Süresi (gün)</label>
                  <input
                    type="number"
                    id="backupRetention"
                    class="form-control"
                    formControlName="backupRetention"
                    min="1"
                    max="365"
                  >
                </div>

                <div class="d-flex gap-2">
                  <button
                    type="button"
                    class="btn btn-outline-info"
                    (click)="createManualBackup()"
                    [disabled]="creatingBackup"
                  >
                    <span *ngIf="creatingBackup" class="spinner-border spinner-border-sm me-2"></span>
                    <i *ngIf="!creatingBackup" class="fas fa-download me-2"></i>
                    Manuel Yedek Al
                  </button>
                  <button
                    type="submit"
                    class="btn btn-primary"
                    [disabled]="backupSettingsForm.invalid || savingBackup"
                  >
                    <span *ngIf="savingBackup" class="spinner-border spinner-border-sm me-2"></span>
                    <i *ngIf="!savingBackup" class="fas fa-save me-2"></i>
                    Kaydet
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .settings-container {
      padding: 1rem;
    }

    .card {
      border: none;
      box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
      transition: box-shadow 0.3s ease;
    }

    .card:hover {
      box-shadow: 0 0.25rem 0.5rem rgba(0, 0, 0, 0.1);
    }

    .card-header {
      background: #f8f9fa;
      border-bottom: 1px solid #e9ecef;
    }

    .btn {
      transition: all 0.3s ease;
    }

    .btn:hover {
      transform: translateY(-1px);
    }

    .form-control:focus,
    .form-select:focus {
      border-color: #80bdff;
      box-shadow: 0 0 0 0.2rem rgba(0, 123, 255, 0.25);
    }

    .form-check-input:checked {
      background-color: #007bff;
      border-color: #007bff;
    }

    .invalid-feedback {
      display: block;
    }

    @media (max-width: 768px) {
      .settings-container {
        padding: 0.5rem;
      }

      .d-flex.gap-2 {
        flex-direction: column;
      }

      .d-flex.gap-2 .btn {
        margin-bottom: 0.5rem;
      }
    }
  `]
})
export class SettingsComponent {
  generalSettingsForm: FormGroup;
  securitySettingsForm: FormGroup;
  emailSettingsForm: FormGroup;
  backupSettingsForm: FormGroup;

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