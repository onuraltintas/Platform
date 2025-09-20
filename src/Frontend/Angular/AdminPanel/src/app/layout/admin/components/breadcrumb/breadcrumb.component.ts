import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { NavigationService, BreadcrumbItem } from '../../../../core/services/navigation.service';

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="breadcrumb-container" *ngIf="(breadcrumbs$ | async)?.length">
      <nav aria-label="breadcrumb">
        <ol class="breadcrumb">
          <li
            *ngFor="let item of breadcrumbs$ | async; let last = last; trackBy: trackByLabel"
            class="breadcrumb-item"
            [class.active]="last"
          >
            <ng-container *ngIf="!last && item.url; else textOnly">
              <a [routerLink]="item.url" class="breadcrumb-link">
                <i class="fas fa-home me-1" *ngIf="item.url === '/dashboard'"></i>
                {{ item.label }}
              </a>
            </ng-container>
            <ng-template #textOnly>
              <span class="breadcrumb-text">{{ item.label }}</span>
            </ng-template>
          </li>
        </ol>
      </nav>

      <!-- Page Actions (can be extended in the future) -->
      <div class="breadcrumb-actions">
        <button
          type="button"
          class="btn btn-outline-secondary btn-sm"
          (click)="goBack()"
          title="Geri git"
        >
          <i class="fas fa-arrow-left"></i>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .breadcrumb-container {
      background: var(--bs-content-bg, #f8f9fa);
      border-bottom: 1px solid var(--bs-border-color, #dee2e6);
      padding: 0.75rem 1rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      min-height: 60px;
    }

    .breadcrumb {
      margin-bottom: 0;
      background: transparent;
      padding: 0;
      font-size: 0.875rem;
    }

    .breadcrumb-item {
      display: flex;
      align-items: center;
    }

    .breadcrumb-item + .breadcrumb-item::before {
      content: ">";
      color: var(--bs-nav-link-color, #6c757d);
      margin: 0 0.5rem;
      font-weight: 600;
    }

    .breadcrumb-link {
      color: var(--bs-nav-link-color, #6c757d);
      text-decoration: none;
      transition: color 0.3s ease;
      display: flex;
      align-items: center;
    }

    .breadcrumb-link:hover {
      color: var(--bs-nav-link-hover-color, #0d6efd);
    }

    .breadcrumb-text {
      color: var(--bs-body-color, #212529);
      font-weight: 500;
    }

    .breadcrumb-item.active .breadcrumb-text {
      color: var(--bs-body-color, #212529);
    }

    .breadcrumb-actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .btn-sm {
      padding: 0.375rem 0.5rem;
      font-size: 0.875rem;
      border-radius: 0.375rem;
    }

    .btn-outline-secondary {
      color: var(--bs-nav-link-color, #6c757d);
      border-color: var(--bs-border-color, #dee2e6);
      background: transparent;
    }

    .btn-outline-secondary:hover {
      color: var(--bs-body-color, #212529);
      background: var(--bs-border-color, #dee2e6);
      border-color: var(--bs-border-color, #dee2e6);
    }

    /* Icons */
    .fas {
      font-size: 0.75rem;
    }

    /* Mobile Responsive */
    @media (max-width: 768px) {
      .breadcrumb-container {
        padding: 0.5rem;
        flex-direction: column;
        gap: 0.5rem;
        align-items: flex-start;
      }

      .breadcrumb {
        font-size: 0.8rem;
      }

      .breadcrumb-item + .breadcrumb-item::before {
        margin: 0 0.25rem;
      }

      .breadcrumb-actions {
        width: 100%;
        justify-content: flex-end;
      }
    }

    @media (max-width: 576px) {
      .breadcrumb-container {
        min-height: auto;
      }

      .breadcrumb {
        flex-wrap: wrap;
      }

      .breadcrumb-item {
        margin-bottom: 0.25rem;
      }
    }

    /* Dark Theme */
    .dark-theme .breadcrumb-container {
      background: var(--bs-content-bg, #1a1d21);
      border-bottom-color: var(--bs-border-color, #495057);
    }

    .dark-theme .breadcrumb-item + .breadcrumb-item::before {
      color: var(--bs-nav-link-color, #adb5bd);
    }

    .dark-theme .btn-outline-secondary {
      color: var(--bs-nav-link-color, #adb5bd);
      border-color: var(--bs-border-color, #495057);
    }

    .dark-theme .btn-outline-secondary:hover {
      color: var(--bs-body-color, #ffffff);
      background: var(--bs-border-color, #495057);
      border-color: var(--bs-border-color, #495057);
    }

    /* Accessibility */
    @media (prefers-reduced-motion: reduce) {
      .breadcrumb-link {
        transition: none;
      }
    }

    /* Print Styles */
    @media print {
      .breadcrumb-container {
        border-bottom: 1px solid #000;
        background: transparent !important;
      }

      .breadcrumb-actions {
        display: none;
      }
    }
  `]
})
export class BreadcrumbComponent implements OnInit {
  private readonly navigationService = inject(NavigationService);

  breadcrumbs$: Observable<BreadcrumbItem[]> = this.navigationService.getBreadcrumbs();

  ngOnInit(): void {
    // Component will automatically receive breadcrumb updates from navigation service
  }

  goBack(): void {
    window.history.back();
  }

  trackByLabel(_index: number, item: BreadcrumbItem): string {
    return item.label + (item.url || '');
  }
}