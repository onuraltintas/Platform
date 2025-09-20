import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

import { HeaderComponent } from './components/header/header.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { FooterComponent } from './components/footer/footer.component';
import { BreadcrumbComponent } from './components/breadcrumb/breadcrumb.component';
import { selectCurrentUser } from '../../store/auth/auth.selectors';
import { User } from '../../core/auth/models/auth.models';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderComponent,
    SidebarComponent,
    FooterComponent,
    BreadcrumbComponent
  ],
  template: `
    <div class="app-wrapper" [class.sidebar-hidden]="sidebarHidden()">
      <!-- Sidebar -->
      <app-sidebar
        [hidden]="sidebarHidden()"
        (toggleSidebar)="toggleSidebar()"
      ></app-sidebar>

      <!-- Main Content -->
      <div class="app-main">
        <!-- Header -->
        <app-header
          [user]="currentUser$ | async"
          [sidebarHidden]="sidebarHidden()"
          (toggleSidebar)="toggleSidebar()"
        ></app-header>

        <!-- Breadcrumb -->
        <app-breadcrumb></app-breadcrumb>

        <!-- Content Area -->
        <main class="app-content">
          <div class="container-fluid">
            <router-outlet></router-outlet>
          </div>
        </main>

        <!-- Footer -->
        <app-footer></app-footer>
      </div>

      <!-- Sidebar Backdrop for Mobile -->
      <div
        class="sidebar-backdrop"
        [class.show]="!sidebarHidden() && isMobile()"
        (click)="hideSidebar()"
      ></div>
    </div>
  `,
  styles: [`
    .app-wrapper {
      display: flex;
      min-height: 100vh;
      transition: all 0.3s ease;
    }

    .app-main {
      flex: 1;
      display: flex;
      flex-direction: column;
      margin-left: 240px;
      transition: margin-left 0.3s ease;
    }

    .sidebar-hidden .app-main {
      margin-left: 0;
    }

    .app-content {
      flex: 1;
      padding: 1rem;
      background: #f8f9fa;
      min-height: calc(100vh - 120px);
    }

    .dark-theme .app-content {
      background: #1a1d21;
    }

    .sidebar-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.5);
      z-index: 1040;
      opacity: 0;
      visibility: hidden;
      transition: all 0.3s ease;
    }

    .sidebar-backdrop.show {
      opacity: 1;
      visibility: visible;
    }

    .container-fluid {
      padding: 0 1rem;
    }

    /* Mobile Responsive */
    @media (max-width: 768px) {
      .app-main {
        margin-left: 0;
      }

      .sidebar-hidden .app-main {
        margin-left: 0;
      }

      .app-content {
        padding: 0.5rem;
      }

      .container-fluid {
        padding: 0 0.5rem;
      }
    }

    /* Dark Theme Styles */
    .dark-theme {
      background: #1a1d21;
      color: #ffffff;
    }

    .dark-theme .app-content {
      background: #1a1d21;
    }

    /* Smooth Transitions */
    * {
      transition: background-color 0.3s ease, color 0.3s ease;
    }
  `]
})
export class AdminLayoutComponent implements OnInit {
  private readonly store = inject(Store);

  currentUser$: Observable<User | null> = this.store.select(selectCurrentUser);

  sidebarHidden = signal(false);
  isMobile = signal(false);

  ngOnInit(): void {
    this.checkMobileView();
    // Listen for window resize
    window.addEventListener('resize', () => this.checkMobileView());
  }

  private checkMobileView(): void {
    const isMobile = window.innerWidth <= 768;
    this.isMobile.set(isMobile);

    // Auto-hide sidebar on mobile
    if (isMobile) {
      this.sidebarHidden.set(true);
    }
  }

  toggleSidebar(): void {
    this.sidebarHidden.update(hidden => !hidden);
  }

  hideSidebar(): void {
    this.sidebarHidden.set(true);
  }
}