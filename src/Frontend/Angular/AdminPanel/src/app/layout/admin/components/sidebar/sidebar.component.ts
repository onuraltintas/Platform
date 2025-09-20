import { Component, inject, input, output, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Observable } from 'rxjs';

import { NavigationService, NavigationItem } from '../../../../core/services/navigation.service';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../../../store/auth/auth.actions';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <aside class="app-sidebar" [class.sidebar-hidden]="hidden()">
      <div class="sidebar-header">
        <div class="sidebar-brand">
          <div class="brand-logo">
            <i class="bi bi-speedometer2 text-primary"></i>
          </div>
          <div class="brand-text" [class.d-none]="collapsed()">
            <h6 class="mb-0 fw-bold">PlatformV1</h6>
            <small class="text-muted">Admin Panel</small>
          </div>
        </div>

        <button
          type="button"
          class="btn btn-link sidebar-collapse-btn"
          (click)="toggleCollapse()"
          [title]="collapsed() ? 'Expand sidebar' : 'Collapse sidebar'"
        >
          <i [class]="collapsed() ? 'bi bi-chevron-right' : 'bi bi-chevron-left'"></i>
        </button>
      </div>

      <nav class="sidebar-nav">
        <ul class="nav flex-column">
          <li
            *ngFor="let item of navigationItems$ | async; trackBy: trackByItemId"
            class="nav-item"
            [ngClass]="getNavItemClass(item)"
          >
            <!-- Regular Item -->
            <ng-container *ngIf="item.type === 'item'">
              <a
                [routerLink]="item.url"
                routerLinkActive="active"
                [routerLinkActiveOptions]="{ exact: item.exactMatch || false }"
                class="nav-link"
                [title]="collapsed() ? item.title : ''"
                (click)="onItemClick(item)"
              >
                <i [class]="item.icon" *ngIf="item.icon"></i>
                <span class="nav-text" [class.d-none]="collapsed()">{{ item.title }}</span>
                <span
                  *ngIf="item.badge && !collapsed()"
                  class="badge ms-auto"
                  [class]="'bg-' + item.badge.type"
                >
                  {{ item.badge.title }}
                </span>
              </a>
            </ng-container>

            <!-- Group with Children -->
            <ng-container *ngIf="item.type === 'group' && item.children">
              <a
                href="javascript:void(0)"
                class="nav-link nav-group-toggle"
                [class.active]="isGroupActive(item)"
                [class.collapsed]="!isGroupExpanded(item.id)"
                [title]="collapsed() ? item.title : ''"
                (click)="toggleGroup(item.id)"
                data-bs-toggle="collapse"
                [attr.data-bs-target]="'#nav-group-' + item.id"
              >
                <i [class]="item.icon" *ngIf="item.icon"></i>
                <span class="nav-text" [class.d-none]="collapsed()">{{ item.title }}</span>
                <i
                  class="bi bi-chevron-down nav-arrow ms-auto"
                  [class.rotated]="isGroupExpanded(item.id)"
                  *ngIf="!collapsed()"
                ></i>
              </a>

              <div
                class="collapse nav-group-content"
                [class.show]="isGroupExpanded(item.id)"
                [id]="'nav-group-' + item.id"
              >
                <ul class="nav flex-column nav-sub">
                  <li
                    *ngFor="let child of item.children; trackBy: trackByItemId"
                    class="nav-item"
                    [ngClass]="getNavItemClass(child)"
                  >
                    <a
                      [routerLink]="child.url"
                      routerLinkActive="active"
                      [routerLinkActiveOptions]="{ exact: child.exactMatch || false }"
                      class="nav-link nav-sub-link"
                      [title]="collapsed() ? child.title : ''"
                      (click)="onItemClick(child)"
                    >
                      <i [class]="child.icon" *ngIf="child.icon"></i>
                      <span class="nav-text" [class.d-none]="collapsed()">{{ child.title }}</span>
                      <span
                        *ngIf="child.badge && !collapsed()"
                        class="badge ms-auto"
                        [class]="'bg-' + child.badge.type"
                      >
                        {{ child.badge.title }}
                      </span>
                    </a>
                  </li>
                </ul>
              </div>
            </ng-container>

            <!-- Divider -->
            <hr class="nav-divider" *ngIf="item.type === 'divider'" />
          </li>
        </ul>
      </nav>

      <!-- Sidebar Footer -->
      <div class="sidebar-footer" *ngIf="!collapsed()">
        <button type="button" class="btn btn-outline-light w-100 d-flex align-items-center justify-content-center logout-btn" (click)="logout()">
          <i class="bi bi-box-arrow-right me-2"></i>
          Çıkış Yap
        </button>
      </div>
    </aside>
  `,
  styles: [`
    .app-sidebar {
      width: 240px;
      background: var(--bs-sidebar-bg, #ffffff);
      border-right: 1px solid var(--bs-border-color, #dee2e6);
      position: fixed;
      top: 0;
      left: 0;
      height: 100vh;
      z-index: 1030;
      display: flex;
      flex-direction: column;
      transition: all 0.3s ease;
      overflow: hidden;
    }

    .app-sidebar.sidebar-hidden {
      transform: translateX(-100%);
    }

    .sidebar-collapsed {
      width: 60px;
    }

    /* Header */
    .sidebar-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem;
      border-bottom: 1px solid var(--bs-border-color, #dee2e6);
      height: 60px;
      flex-shrink: 0;
    }

    .sidebar-brand {
      display: flex;
      align-items: center;
      flex: 1;
    }

    .brand-logo {
      font-size: 1.5rem;
      margin-right: 0.75rem;
    }

    .brand-text h6 {
      color: var(--bs-body-color, #212529);
      margin-bottom: 0;
    }

    .brand-text small {
      color: var(--bs-nav-link-color, #6c757d);
    }

    .sidebar-collapse-btn {
      color: var(--bs-nav-link-color, #6c757d);
      padding: 0.25rem;
      font-size: 0.875rem;
    }

    .sidebar-collapse-btn:hover {
      color: var(--bs-nav-link-hover-color, #0d6efd);
    }

    /* Navigation */
    .sidebar-nav {
      flex: 1;
      overflow-y: auto;
      overflow-x: hidden;
      padding: 1rem 0;
    }

    .nav-item {
      margin-bottom: 0.25rem;
    }

    .nav-link {
      display: flex;
      align-items: center;
      padding: 0.75rem 1rem;
      color: var(--bs-nav-link-color, #6c757d);
      text-decoration: none;
      border-radius: 0;
      transition: all 0.3s ease;
      position: relative;
    }

    .nav-link:hover {
      color: var(--bs-nav-link-hover-color, #0d6efd);
      background: rgba(13, 110, 253, 0.1);
    }

    .nav-link.active {
      color: #0d6efd;
      background: rgba(13, 110, 253, 0.1);
      font-weight: 600;
    }

    .nav-link.active::before {
      content: '';
      position: absolute;
      left: 0;
      top: 0;
      bottom: 0;
      width: 3px;
      background: #0d6efd;
    }

    .nav-link i {
      width: 20px;
      font-size: 1rem;
      margin-right: 0.75rem;
      text-align: center;
    }

    .nav-text {
      flex: 1;
      white-space: nowrap;
    }

    /* Group Navigation */
    .nav-group-toggle {
      cursor: pointer;
    }

    .nav-arrow {
      transition: transform 0.3s ease;
      font-size: 0.75rem;
    }

    .nav-arrow.rotated {
      transform: rotate(180deg);
    }

    .nav-group-content {
      background: rgba(0, 0, 0, 0.02);
    }

    .nav-sub {
      padding-left: 0;
    }

    .nav-sub-link {
      padding-left: 2.5rem;
      font-size: 0.875rem;
    }

    .nav-sub-link i {
      width: 16px;
      font-size: 0.875rem;
      margin-right: 0.5rem;
    }

    /* Divider */
    .nav-divider {
      margin: 0.5rem 1rem;
      border-color: var(--bs-border-color, #dee2e6);
    }

    /* Badge */
    .badge {
      font-size: 0.65rem;
      font-weight: 500;
    }

    /* Footer */
    .sidebar-footer {
      padding: 1rem;
      border-top: 1px solid var(--bs-border-color, #dee2e6);
      flex-shrink: 0;
    }

    .logout-btn { border-radius: 0.5rem; }

    /* Collapsed State */
    .sidebar-collapsed .nav-text,
    .sidebar-collapsed .nav-arrow,
    .sidebar-collapsed .badge {
      display: none !important;
    }

    .sidebar-collapsed .nav-link {
      justify-content: center;
      padding: 0.75rem 0.5rem;
    }

    .sidebar-collapsed .nav-link i {
      margin-right: 0;
    }

    .sidebar-collapsed .nav-group-content {
      display: none !important;
    }

    .sidebar-collapsed .sidebar-footer {
      display: none;
    }

    /* Scrollbar */
    .sidebar-nav::-webkit-scrollbar {
      width: 4px;
    }

    .sidebar-nav::-webkit-scrollbar-track {
      background: transparent;
    }

    .sidebar-nav::-webkit-scrollbar-thumb {
      background: var(--bs-border-color, #dee2e6);
      border-radius: 2px;
    }

    /* Mobile */
    @media (max-width: 768px) {
      .app-sidebar {
        transform: translateX(-100%);
      }

      .app-sidebar:not(.sidebar-hidden) {
        transform: translateX(0);
      }
    }

    /* Dark Theme */
    .dark-theme .app-sidebar {
      background: var(--bs-sidebar-bg, #2d3339);
      border-right-color: var(--bs-border-color, #495057);
    }

    .dark-theme .sidebar-header {
      border-bottom-color: var(--bs-border-color, #495057);
    }

    .dark-theme .nav-group-content {
      background: rgba(255, 255, 255, 0.02);
    }

    .dark-theme .nav-divider {
      border-color: var(--bs-border-color, #495057);
    }

    .dark-theme .sidebar-footer {
      border-top-color: var(--bs-border-color, #495057);
    }
  `]
})
export class SidebarComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly navigationService = inject(NavigationService);

  hidden = input<boolean>(false);
  toggleSidebar = output<void>();

  navigationItems$: Observable<NavigationItem[]> = this.navigationService.getNavigationItems();

  collapsed = signal(false);
  expandedGroups = signal<Set<string>>(new Set());

  ngOnInit(): void {
    // Auto-collapse on mobile
    this.checkMobileView();
    window.addEventListener('resize', () => this.checkMobileView());
  }

  private checkMobileView(): void {
    const isMobile = window.innerWidth <= 768;
    if (isMobile) {
      this.collapsed.set(false); // Don't auto-collapse on mobile, use hidden instead
    }
  }

  toggleCollapse(): void {
    this.collapsed.update(current => !current);

    // Collapse all groups when sidebar is collapsed
    if (this.collapsed()) {
      this.expandedGroups.set(new Set());
    }
  }

  toggleGroup(groupId: string): void {
    if (this.collapsed()) {
      return; // Don't allow group toggle when collapsed
    }

    this.expandedGroups.update(groups => {
      const newGroups = new Set(groups);
      if (newGroups.has(groupId)) {
        newGroups.delete(groupId);
      } else {
        newGroups.add(groupId);
      }
      return newGroups;
    });
  }

  isGroupExpanded(groupId: string): boolean {
    return this.expandedGroups().has(groupId);
  }

  isGroupActive(item: NavigationItem): boolean {
    if (!item.children) {
      return false;
    }

    return item.children.some(child =>
      child.url && this.navigationService.isActive(child.id)
    );
  }

  getNavItemClass(item: NavigationItem): string {
    const classes: string[] = [];

    if (item.classes) {
      classes.push(item.classes);
    }

    if (item.type === 'group') {
      classes.push('nav-group');
    }

    return classes.join(' ');
  }

  onItemClick(item: NavigationItem): void {
    if (item.function) {
      item.function();
    }

    // Close sidebar on mobile after item click
    if (window.innerWidth <= 768) {
      this.toggleSidebar.emit();
    }
  }

  trackByItemId(_index: number, item: NavigationItem): string {
    return item.id;
  }

  logout(): void {
    this.store.dispatch(AuthActions.logout());
  }
}