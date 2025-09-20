import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule, AvatarModule, BadgeModule } from '@coreui/angular';

export type ActivityItem = {
  type: string;
  title: string;
  description: string;
  timestamp: Date;
};

@Component({
  selector: 'app-activity-list',
  standalone: true,
  imports: [CommonModule, CardModule, AvatarModule, BadgeModule],
  template: `
    <c-card class="border-0 shadow-sm">
      <c-card-header class="bg-transparent border-0 pb-0">
        <h5 class="card-title d-flex align-items-center mb-0">
          <i class="bi bi-clock-history text-primary me-2"></i>
          {{ title }}
        </h5>
      </c-card-header>
      <c-card-body>
        <div *ngIf="items?.length === 0" class="text-center py-5 text-muted">
          <i class="bi bi-info-circle display-6 mb-3"></i>
          <p class="mb-0">{{ emptyText }}</p>
        </div>

        <div *ngFor="let activity of items; let last = last"
             class="d-flex align-items-center py-3"
             [class.border-bottom]="!last">
          <c-avatar [color]="getActivityColor(activity.type)" size="md" class="me-3 flex-shrink-0">
            <i [class]="getActivityIcon(activity.type)" class="text-white"></i>
          </c-avatar>
          <div class="flex-grow-1 min-width-0">
            <h6 class="mb-1 fw-semibold text-truncate">{{ activity.title }}</h6>
            <p class="mb-0 text-muted small text-truncate">{{ activity.description }}</p>
          </div>
          <div class="text-end flex-shrink-0 ms-2">
            <c-badge [color]="getActivityBadgeColor(activity.type)" class="mb-1">
              {{ getActivityTypeText(activity.type) }}
            </c-badge>
            <div class="text-muted small">{{ formatActivityDate(activity.timestamp) }}</div>
          </div>
        </div>
      </c-card-body>
    </c-card>
  `,
  styles: [`
    .min-width-0 { min-width: 0; }
  `]
})
export class ActivityListComponent {
  @Input() title: string = 'Son Aktiviteler';
  @Input() emptyText: string = 'Henüz aktivite bulunmuyor';
  @Input() items: ActivityItem[] = [];

  getActivityIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'user_created': 'bi bi-person-plus',
      'group_updated': 'bi bi-people',
      'role_assigned': 'bi bi-shield-check',
      'system_update': 'bi bi-arrow-up-circle',
      'backup_completed': 'bi bi-cloud-check',
      'default': 'bi bi-info-circle'
    };
    return icons[type] || icons['default'];
  }

  getActivityColor(type: string): string {
    const colors: { [key: string]: string } = {
      'user_created': 'success',
      'group_updated': 'info',
      'role_assigned': 'warning',
      'system_update': 'primary',
      'backup_completed': 'success',
      'default': 'secondary'
    };
    return colors[type] || colors['default'];
  }

  getActivityBadgeColor(type: string): string {
    const colors: { [key: string]: string } = {
      'user_created': 'success-subtle',
      'group_updated': 'info-subtle',
      'role_assigned': 'warning-subtle',
      'system_update': 'primary-subtle',
      'backup_completed': 'success-subtle',
      'default': 'secondary-subtle'
    };
    return colors[type] || colors['default'];
  }

  getActivityTypeText(type: string): string {
    const texts: { [key: string]: string } = {
      'user_created': 'Kullanıcı',
      'group_updated': 'Grup',
      'role_assigned': 'Yetki',
      'system_update': 'Sistem',
      'backup_completed': 'Yedek',
      'default': 'Diğer'
    };
    return texts[type] || texts['default'];
  }

  formatActivityDate(date: Date): string {
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));
    if (diffInMinutes < 1) return 'Şimdi';
    if (diffInMinutes < 60) return `${diffInMinutes} dk önce`;
    const diffInHours = Math.floor(diffInMinutes / 60);
    if (diffInHours < 24) return `${diffInHours} sa önce`;
    const diffInDays = Math.floor(diffInHours / 24);
    return `${diffInDays} gün önce`;
  }
}

