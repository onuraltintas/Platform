import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersService } from '../../../users/data-access/users.service';
import { UserProfileDto } from '../../../users/models/user.models';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../access-control/data-access/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="container-fluid">
    <div class="row g-3">
      <div class="col-12 col-lg-4">
        <div class="card">
          <div class="card-body text-center">
            <div class="avatar avatar-xl mb-3">
              <ng-container *ngIf="editProfile.dateOfBirth !== undefined; else ensureInit"></ng-container>
              <ng-container *ngIf="editProfile.avatar; else defaultAvatarTpl">
                <img [src]="editProfile.avatar" alt="Avatar" class="rounded-circle" style="width:96px;height:96px;object-fit:cover;" />
              </ng-container>
              <ng-template #defaultAvatarTpl>
                <div class="avatar-img rounded-circle bg-primary-subtle d-flex align-items-center justify-content-center" style="width:96px;height:96px;">
                  <i class="cil-user text-primary" style="font-size:48px;"></i>
                </div>
              </ng-template>
              <ng-template #ensureInit></ng-template>
            </div>
            <h5 class="mb-1">{{ userName() }}</h5>
            <p class="text-muted small mb-0">{{ userEmail() }}</p>
          </div>
        </div>
        <div class="card mt-3">
          <div class="card-header"><strong>Avatar</strong></div>
          <div class="card-body">
            <div class="mb-3">
              <label class="form-label">Avatar URL</label>
              <input class="form-control" [(ngModel)]="editProfile.avatar" placeholder="https://..." />
            </div>
            <div class="mb-2 text-center text-muted">veya</div>
            <div class="mb-2">
              <label class="form-label">Dosya Yükle</label>
              <input type="file" class="form-control" accept="image/*" (change)="onAvatarFileSelected($event)" />
            </div>
          </div>
        </div>
      </div>
      <div class="col-12 col-lg-8">
        <div class="card">
          <div class="card-header">
            <strong>Profil</strong>
          </div>
          <div class="card-body">
            <div class="row">
              <div class="col-md-12 mb-3">
                <label class="form-label">Biyografi</label>
                <textarea class="form-control" rows="3" [(ngModel)]="editProfile.bio"></textarea>
              </div>
              <div class="col-md-6 mb-3">
                <label class="form-label">Doğum Tarihi</label>
                <input type="date" class="form-control" [(ngModel)]="editProfile.dateOfBirth" />
              </div>
              <div class="col-md-6 mb-3">
                <label class="form-label">Cinsiyet</label>
                <select class="form-select" [(ngModel)]="editProfile.gender">
                  <option [ngValue]="null">Seçiniz</option>
                  <option value="male">Erkek</option>
                  <option value="female">Kadın</option>
                </select>
              </div>
              <div class="col-md-6 mb-3">
                <label class="form-label">Telefon</label>
                <input class="form-control" [(ngModel)]="editProfile.phoneNumber" />
              </div>
              <div class="col-md-6 mb-3">
                <label class="form-label">Şehir</label>
                <input class="form-control" [(ngModel)]="editProfile.city" />
              </div>
              <div class="col-md-6 mb-3">
                <label class="form-label">Ülke</label>
                <input class="form-control" [(ngModel)]="editProfile.country" />
              </div>
              <div class="col-md-6 mb-3">
                <label class="form-label">Posta Kodu</label>
                <input class="form-control" [(ngModel)]="editProfile.postalCode" />
              </div>
              <div class="col-md-12 mb-3">
                <label class="form-label">Adres</label>
                <input class="form-control" [(ngModel)]="editProfile.address" />
              </div>
              <div class="col-md-6 mb-3">
                <label class="form-label">Web Sitesi</label>
                <input class="form-control" [(ngModel)]="editProfile.website" placeholder="https://..." />
              </div>
              <div class="col-md-6 mb-3">
                <label class="form-label">Sosyal Medya</label>
                <input class="form-control" [(ngModel)]="editProfile.socialMediaLinks" placeholder="@kullanici veya URL" />
              </div>
              <div class="col-md-12 mb-3">
                <label class="form-label">Tercihler (JSON veya not)</label>
                <textarea class="form-control" rows="2" [(ngModel)]="editProfile.preferences"></textarea>
              </div>
            </div>
            <button class="btn btn-primary" (click)="save()" [disabled]="saving()">Kaydet</button>
          </div>
        </div>
      </div>
    </div>
  </div>
  `
})
export class ProfileComponent implements OnInit {
  private usersService = inject(UsersService);
  private authService = inject(AuthService);
  private toastr = inject(ToastrService);

  userId = signal<string | null>(null);
  userName = signal<string>('');
  userEmail = signal<string>('');
  profile = signal<UserProfileDto | null>(null);
  editProfile: Partial<UserProfileDto> = {};
  saving = signal(false);

  ngOnInit(): void {
    this.authService.getCurrentUser().subscribe(r => {
      const u = (r as any)?.data;
      if (!u?.id) return;
      this.userId.set(u.id);
      this.userName.set(u.fullName || u.userName || 'Kullanıcı');
      this.userEmail.set(u.email || '');
      this.load(u.id);
    });
  }

  private toDateInput(value?: string | null): string | undefined {
    if (!value) return undefined;
    // ISO veya full datetime geldiyse, sadece YYYY-MM-DD kısmını al
    const idx = value.indexOf('T');
    return idx > 0 ? value.substring(0, idx) : value;
  }

  private load(id: string) {
    this.usersService.getProfile(id).subscribe({
      next: (p) => {
        this.authService.updateCurrentUser({ avatar: p.avatar });
        // Doğum tarihini input formatına normalize et
        const normalized: Partial<UserProfileDto> = { ...p, dateOfBirth: this.toDateInput(p.dateOfBirth) as any };
        this.profile.set(p);
        this.editProfile = normalized;
      },
      error: (err) => {
        if (err?.status === 404) {
          this.usersService.createProfile({ userId: id }).subscribe({
            next: (created) => {
              this.authService.updateCurrentUser({ avatar: created.avatar });
              const normalized: Partial<UserProfileDto> = { ...created, dateOfBirth: this.toDateInput(created.dateOfBirth) as any };
              this.profile.set(created);
              this.editProfile = normalized;
            },
            error: () => { /* sessizce geç */ }
          });
        } else {
          // Diğer hatalar global handler ile gösterilir
        }
      }
    });
  }

  onAvatarFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    const reader = new FileReader();
    reader.onload = () => {
      const base64 = reader.result as string;
      this.editProfile.avatar = base64;
    };
    reader.readAsDataURL(file);
  }

  save() {
    const id = this.userId(); if (!id) return;
    this.saving.set(true);
    const payload: any = { ...this.editProfile };
    // Backend DateTime icin tarih stringi gondermek yeterli; istenirse ISO'ya da cevrilebilir
    delete payload.userId; // immutable
    this.usersService.updateProfile(id, payload).subscribe({
      next: (p) => {
        this.authService.updateCurrentUser({ avatar: p.avatar });
        // Response'taki tarihi de normalize ederek forma yansit
        const normalized: Partial<UserProfileDto> = { ...p, dateOfBirth: this.toDateInput(p.dateOfBirth) as any };
        this.profile.set(p);
        this.editProfile = normalized;
        this.saving.set(false);
        this.toastr.success('Profil güncellendi.');
      },
      error: () => { this.saving.set(false); }
    });
  }
}