import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-module-hub',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LucideAngularModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-wrapper">
      <!-- Page Header -->
      <div class="page-header d-print-none">
        <div class="container-xl">
          <div class="row g-2 align-items-center">
            <div class="col">
              <h2 class="page-title">Ana Sayfa</h2>
              <div class="page-subtitle">Hoş geldiniz! Platform modüllerine buradan erişebilirsiniz.</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Page Content -->
      <div class="page-body">
        <div class="container-xl">
          <div class="row row-deck row-cards">
            <!-- User Management Module -->
            <div class="col-6 col-lg-4 col-xl-3">
              <a class="card card-link" routerLink="/admin/user-management">
                <div class="card-body">
                  <div class="d-flex align-items-center">
                    <div class="subheader">Yönetim</div>
                  </div>
                  <div class="h1 my-3">
                    <lucide-icon name="users" [size]="32" class="text-primary"/>
                  </div>
                  <div class="h3 m-0">Kullanıcı Yönetimi</div>
                  <div class="text-muted small mt-1">
                    Kullanıcılar, roller, yetkiler ve grupları yönetin
                  </div>
                </div>
              </a>
            </div>

            <!-- Speed Reading Module -->
            <div class="col-6 col-lg-4 col-xl-3">
              <a class="card card-link" routerLink="/admin/speed-reading">
                <div class="card-body">
                  <div class="d-flex align-items-center">
                    <div class="subheader">Eğitim</div>
                  </div>
                  <div class="h1 my-3">
                    <lucide-icon name="book-open" [size]="32" class="text-success"/>
                  </div>
                  <div class="h3 m-0">Hızlı Okuma</div>
                  <div class="text-muted small mt-1">
                    Hızlı okuma egzersizleri ve analitik raporlar
                  </div>
                </div>
              </a>
            </div>

            <!-- Profile Module -->
            <div class="col-6 col-lg-4 col-xl-3">
              <a class="card card-link" routerLink="/admin/profile">
                <div class="card-body">
                  <div class="d-flex align-items-center">
                    <div class="subheader">Hesap</div>
                  </div>
                  <div class="h1 my-3">
                    <lucide-icon name="user" [size]="32" class="text-info"/>
                  </div>
                  <div class="h3 m-0">Profil</div>
                  <div class="text-muted small mt-1">
                    Kişisel bilgilerinizi ve ayarlarınızı yönetin
                  </div>
                </div>
              </a>
            </div>

            <!-- Settings Module -->
            <div class="col-6 col-lg-4 col-xl-3">
              <a class="card card-link" routerLink="/admin/settings">
                <div class="card-body">
                  <div class="d-flex align-items-center">
                    <div class="subheader">Sistem</div>
                  </div>
                  <div class="h1 my-3">
                    <lucide-icon name="settings" [size]="32" class="text-warning"/>
                  </div>
                  <div class="h3 m-0">Ayarlar</div>
                  <div class="text-muted small mt-1">
                    Sistem ayarları ve konfigürasyonlar
                  </div>
                </div>
              </a>
            </div>
          </div>

          <!-- Quick Stats Row -->
          <div class="row mt-4">
            <div class="col-12">
              <div class="card">
                <div class="card-header">
                  <h3 class="card-title">Hızlı Erişim</h3>
                </div>
                <div class="card-body">
                  <div class="row">
                    <div class="col-md-6">
                      <h4>Son Aktiviteler</h4>
                      <p class="text-muted">
                        Sisteme giriş yaptınız ve modüllere erişim sağladınız.
                      </p>
                    </div>
                    <div class="col-md-6">
                      <h4>Sistem Durumu</h4>
                      <div class="d-flex align-items-center">
                        <div class="text-success me-2">
                          <lucide-icon name="check-circle" [size]="20"/>
                        </div>
                        <span>Tüm sistemler çalışıyor</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card-link {
      text-decoration: none;
      color: inherit;
      transition: all 0.2s ease-in-out;
    }

    .card-link:hover {
      text-decoration: none;
      color: inherit;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
    }

    .subheader {
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      color: var(--bs-secondary);
      letter-spacing: 0.5px;
    }

    .h1 {
      margin: 1rem 0;
    }

    .card-body {
      padding: 1.5rem;
    }
  `]
})
export class ModuleHubComponent {
}