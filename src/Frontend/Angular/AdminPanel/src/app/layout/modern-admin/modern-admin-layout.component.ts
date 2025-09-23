import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ModernSidebarComponent } from './components/sidebar/modern-sidebar.component';
import { ModernHeaderComponent } from './components/header/modern-header.component';
import { ModernFooterComponent } from './components/footer/modern-footer.component';
import { LoadingService } from '../../shared/services/loading.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-modern-admin-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    ModernSidebarComponent,
    ModernHeaderComponent,
    ModernFooterComponent
  ],
  template: `
    <div class="page" [class.page-sidebar-collapsed]="sidebarCollapsed()" [class.page-sidebar-hidden]="sidebarHidden()">
      <!-- Loading overlay -->
      @if (isLoading()) {
        <div class="page-loading">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Yükleniyor...</span>
          </div>
        </div>
      }

      <!-- Sidebar -->
      <app-modern-sidebar
        [collapsed]="sidebarCollapsed()"
        [hidden]="sidebarHidden()"
        (collapsedChange)="onSidebarCollapsedChange($event)"
        (hiddenChange)="onSidebarHiddenChange($event)">
      </app-modern-sidebar>

      <!-- Page wrapper -->
      <div class="page-wrapper">
        <!-- Header -->
        <app-modern-header
          [sidebarCollapsed]="sidebarCollapsed()"
          [sidebarHidden]="sidebarHidden()"
          (toggleSidebar)="toggleSidebar()">
        </app-modern-header>

        <!-- Page content -->
        <div class="page-body">
          <div class="container-xl">
            <router-outlet></router-outlet>
          </div>
        </div>

        <!-- Footer -->
        <app-modern-footer></app-modern-footer>
      </div>

      <!-- Mobile overlay -->
      @if (!sidebarHidden() && isMobile()) {
        <div class="sidebar-overlay" (click)="hideSidebar()"></div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
    }

    .page {
      display: flex;
      flex-direction: row;
      min-height: 100vh;
      transition: margin-left 0.3s ease;
    }

    .page-wrapper {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-height: 100vh;
      transition: margin-left 0.3s ease;
    }

    .page-body {
      flex: 1;
      padding: 1.5rem 0;
      background: var(--bs-content-bg);
      min-height: calc(100vh - 57px - 57px);
    }

    .page-loading {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(255, 255, 255, 0.9);
      z-index: 9999;
    }

    :host-context(.dark-theme) .page-loading {
      background: rgba(26, 29, 33, 0.9);
    }

    .sidebar-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      z-index: 1039;
      display: none;
    }

    @media (max-width: 991.98px) {
      .sidebar-overlay {
        display: block;
      }

      .page-wrapper {
        margin-left: 0 !important;
      }
    }

    .page-sidebar-collapsed .page-wrapper {
      margin-left: 80px;
    }

    .page-sidebar-hidden .page-wrapper {
      margin-left: 0;
    }

    @media (min-width: 992px) {
      .page-wrapper {
        margin-left: 260px;
      }
    }
  `]
})
export class ModernAdminLayoutComponent implements OnInit, OnDestroy {
  private readonly loadingService = inject(LoadingService);
  private readonly destroy$ = new Subject<void>();

  sidebarCollapsed = signal(false);
  sidebarHidden = signal(false);
  isMobile = signal(false);
  isLoading = signal(false);

  ngOnInit(): void {
    this.checkMobileView();
    window.addEventListener('resize', () => this.checkMobileView());

    // Subscribe to loading state
    this.loadingService.isLoading$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(loading => {
      this.isLoading.set(loading);
    });

    // Load saved sidebar state
    const savedCollapsed = localStorage.getItem('sidebar-collapsed') === 'true';
    this.sidebarCollapsed.set(savedCollapsed);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    window.removeEventListener('resize', () => this.checkMobileView());
  }

  private checkMobileView(): void {
    const isMobile = window.innerWidth < 992;
    this.isMobile.set(isMobile);

    // Auto-hide sidebar on mobile
    if (isMobile) {
      this.sidebarHidden.set(true);
    } else {
      this.sidebarHidden.set(false);
    }
  }

  toggleSidebar(): void {
    if (this.isMobile()) {
      this.sidebarHidden.update(hidden => !hidden);
    } else {
      this.sidebarCollapsed.update(collapsed => {
        const newValue = !collapsed;
        localStorage.setItem('sidebar-collapsed', newValue.toString());
        return newValue;
      });
    }
  }

  hideSidebar(): void {
    this.sidebarHidden.set(true);
  }

  onSidebarCollapsedChange(collapsed: boolean): void {
    this.sidebarCollapsed.set(collapsed);
    localStorage.setItem('sidebar-collapsed', collapsed.toString());
  }

  onSidebarHiddenChange(hidden: boolean): void {
    this.sidebarHidden.set(hidden);
  }
}