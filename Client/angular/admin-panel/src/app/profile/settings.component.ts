import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersService } from '../services/users.service';
import { UserSettingsDto, UpdateUserSettingsRequest } from '../models/user.models';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <ul class="nav nav-tabs mb-3">
    <li class="nav-item">
      <button class="nav-link" [class.active]="activeTab() === 'general'" (click)="activeTab.set('general')">Genel Ayarlar</button>
    </li>
    <li class="nav-item">
      <button class="nav-link" [class.active]="activeTab() === 'security'" (click)="activeTab.set('security')">Hesap ve Güvenlik</button>
    </li>
  </ul>

  <div *ngIf="activeTab() === 'general'">
    <div class="card">
      <div class="card-header"><strong>Ayarlar</strong></div>
      <div class="card-body">
        <div class="row">
          <div class="col-md-4 mb-3">
            <label class="form-label">Tema</label>
            <select class="form-select" [(ngModel)]="edit.theme">
              <option value="light">Açık</option>
              <option value="dark">Koyu</option>
            </select>
          </div>
          <div class="col-md-4 mb-3">
            <label class="form-label">Dil</label>
            <input class="form-control" [(ngModel)]="edit.language" placeholder="tr" />
          </div>
          <div class="col-md-4 mb-3">
            <label class="form-label">Zaman Dilimi</label>
            <input class="form-control" [(ngModel)]="edit.timeZone" placeholder="Europe/Istanbul" />
          </div>
        </div>
        <div class="form-check form-switch mb-2">
          <input class="form-check-input" type="checkbox" [(ngModel)]="edit.emailNotifications" id="emailN" />
          <label class="form-check-label" for="emailN">E-posta Bildirimleri</label>
        </div>
        <div class="form-check form-switch mb-2">
          <input class="form-check-input" type="checkbox" [(ngModel)]="edit.pushNotifications" id="pushN" />
          <label class="form-check-label" for="pushN">Push Bildirimleri</label>
        </div>
        <div class="form-check form-switch mb-3">
          <input class="form-check-input" type="checkbox" [(ngModel)]="edit.smsNotifications" id="smsN" />
          <label class="form-check-label" for="smsN">SMS Bildirimleri</label>
        </div>
        <div class="mb-3">
          <label class="form-label">Tercihler (JSON veya not)</label>
          <textarea class="form-control" rows="3" [(ngModel)]="edit.preferences"></textarea>
        </div>
        <button class="btn btn-primary" (click)="save()" [disabled]="saving()">Kaydet</button>
      </div>
    </div>
  </div>

  <div *ngIf="activeTab() === 'security'">
    <div class="card mb-3">
      <div class="card-header"><strong>Şifre Değiştir</strong></div>
      <div class="card-body">
        <div class="row g-3">
          <div class="col-md-4">
            <label class="form-label">Mevcut Şifre</label>
            <input type="password" class="form-control" [(ngModel)]="passwordForm.currentPassword" />
          </div>
          <div class="col-md-4">
            <label class="form-label">Yeni Şifre</label>
            <input type="password" class="form-control" [(ngModel)]="passwordForm.newPassword" />
          </div>
          <div class="col-md-4">
            <label class="form-label">Yeni Şifre (Tekrar)</label>
            <input type="password" class="form-control" [(ngModel)]="passwordForm.confirmNewPassword" />
          </div>
        </div>
        <button class="btn btn-outline-primary mt-3" (click)="changePassword()" [disabled]="changingPassword()">Şifreyi Güncelle</button>
        <div class="text-muted small mt-2" *ngIf="passwordMessage">{{ passwordMessage }}</div>
      </div>
    </div>

    <div class="card">
      <div class="card-header"><strong>Kullanıcı Adı Değiştir</strong></div>
      <div class="card-body">
        <div class="row g-3">
          <div class="col-md-6">
            <label class="form-label">Yeni Kullanıcı Adı</label>
            <input class="form-control" [(ngModel)]="usernameForm.newUserName" />
          </div>
          <div class="col-md-6 d-flex align-items-end">
            <button class="btn btn-outline-secondary" (click)="changeUsername()" [disabled]="changingUsername()">Güncelle</button>
          </div>
        </div>
        <div class="text-muted small mt-2" *ngIf="usernameMessage">{{ usernameMessage }}</div>
      </div>
    </div>
  </div>
  `
})
export class SettingsComponent implements OnInit {
  private usersService = inject(UsersService);
  private authService = inject(AuthService);
  private http = inject(HttpClient);
  private toastr = inject(ToastrService);

  userId = signal<string | null>(null);
  settings = signal<UserSettingsDto | null>(null);
  edit: Partial<UserSettingsDto> = {};
  saving = signal(false);

  activeTab = signal<'general' | 'security'>('general');

  passwordForm = { currentPassword: '', newPassword: '', confirmNewPassword: '' };
  changingPassword = signal(false);
  passwordMessage: string | null = null;

  usernameForm = { newUserName: '' };
  changingUsername = signal(false);
  usernameMessage: string | null = null;

  ngOnInit(): void {
    this.authService.getCurrentUser().subscribe(r => {
      const u = (r as any)?.data;
      if (!u?.id) return;
      this.userId.set(u.id);
      this.load(u.id);
    });
  }

  private load(id: string) {
    this.usersService.getSettings(id).subscribe(s => {
      this.settings.set(s);
      this.edit = { ...s };
    });
  }

  save() {
    const id = this.userId(); if (!id) return;
    this.saving.set(true);
    const payload: any = { ...this.edit };
    delete payload.userId; // immutable
    this.usersService.updateSettings(id, payload).subscribe({
      next: (s) => { this.settings.set(s); this.edit = { ...s }; this.saving.set(false); this.toastr.success('Ayarlar kaydedildi.'); },
      error: () => { this.saving.set(false); }
    });
  }

  changePassword() {
    this.passwordMessage = null;
    if (!this.passwordForm.currentPassword || !this.passwordForm.newPassword || !this.passwordForm.confirmNewPassword) {
      this.passwordMessage = 'Lütfen tüm alanları doldurun.';
      return;
    }
    if (this.passwordForm.newPassword !== this.passwordForm.confirmNewPassword) {
      this.passwordMessage = 'Yeni şifreler eşleşmiyor.';
      return;
    }
    this.changingPassword.set(true);
    const url = `${environment.apiUrl}/v1/auth/change-password`;
    this.http.post<any>(url, {
      currentPassword: this.passwordForm.currentPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe({
      next: () => {
        this.passwordMessage = 'Şifre başarıyla güncellendi.';
        this.toastr.success('Şifre güncellendi.');
        this.passwordForm = { currentPassword: '', newPassword: '', confirmNewPassword: '' };
        this.changingPassword.set(false);
      },
      error: (err) => {
        this.passwordMessage = err?.error?.error?.message || 'Şifre güncelleme başarısız.';
        this.changingPassword.set(false);
      }
    });
  }

  changeUsername() {
    this.usernameMessage = null;
    if (!this.usernameForm.newUserName || this.usernameForm.newUserName.length < 3) {
      this.usernameMessage = 'Kullanıcı adı en az 3 karakter olmalı.';
      return;
    }
    this.changingUsername.set(true);
    const url = `${environment.apiUrl}/v1/auth/change-username`;
    this.http.post<any>(url, { newUserName: this.usernameForm.newUserName }).subscribe({
      next: () => {
        this.usernameMessage = 'Kullanıcı adı güncellendi.';
        this.toastr.success('Kullanıcı adı güncellendi.');
        this.changingUsername.set(false);
      },
      error: (err) => {
        this.usernameMessage = err?.error?.error?.message || 'Kullanıcı adı güncellenemedi.';
        this.changingUsername.set(false);
      }
    });
  }
}