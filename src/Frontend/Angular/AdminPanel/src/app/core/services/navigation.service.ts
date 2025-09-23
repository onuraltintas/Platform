import { Injectable, inject, signal } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, map } from 'rxjs/operators';
import { BehaviorSubject, Observable } from 'rxjs';
import { PermissionsService } from '../../shared/permissions/permissions.service';

// import { AuthService } from '../auth/services/auth.service'; // Disabled for development

export interface NavigationItem {
  id: string;
  title: string;
  type: 'item' | 'group' | 'divider';
  icon?: string; // legacy icon class
  iconName?: string; // Material icon name
  url?: string;
  classes?: string;
  exactMatch?: boolean;
  external?: boolean;
  target?: string;
  breadcrumbs?: boolean;
  function?: () => void;
  badge?: {
    title: string;
    type: 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info';
  };
  children?: NavigationItem[];
  permissions?: string[];
  roles?: string[];
}

export interface BreadcrumbItem {
  label: string;
  url?: string;
}

@Injectable({
  providedIn: 'root'
})
export class NavigationService {
  private readonly router = inject(Router);
  private readonly perms = inject(PermissionsService);

  private navigationItems: NavigationItem[] = [
    {
      id: 'home',
      title: 'Ana',
      type: 'item',
      iconName: 'home',
      url: '/admin',
      exactMatch: true,
      breadcrumbs: true
    },
    {
      id: 'divider-1',
      title: '',
      type: 'divider'
    },
    {
      id: 'user-management',
      title: 'Kullanıcılar',
      type: 'group',
      iconName: 'group',
      permissions: ['Identity.Users.Read'],
      children: [
        {
          id: 'users',
          title: 'Kullanıcı Listesi',
          type: 'item',
          iconName: 'person',
          url: '/admin/user-management/users',
          permissions: ['Identity.Users.Read'],
          breadcrumbs: true
        },
        {
          id: 'roles',
          title: 'Roller',
          type: 'item',
          iconName: 'admin_panel_settings',
          url: '/admin/user-management/roles',
          permissions: ['Identity.Roles.Read'],
          breadcrumbs: true
        },
        {
          id: 'permissions',
          title: 'İzinler',
          type: 'item',
          iconName: 'vpn_key',
          url: '/admin/user-management/permissions',
          permissions: ['Identity.Permissions.Read'],
          breadcrumbs: true
        },
        {
          id: 'groups',
          title: 'Gruplar',
          type: 'item',
          iconName: 'groups',
          url: '/admin/user-management/groups',
          permissions: ['Identity.Groups.Read'],
          breadcrumbs: true
        }
      ]
    },
    {
      id: 'speed-reading',
      title: 'Hızlı Okuma',
      type: 'group',
      iconName: 'menu_book',
      permissions: ['SpeedReading.Read'],
      children: [
        {
          id: 'speed-reading-sessions',
          title: 'Oturumlar',
          type: 'item',
          iconName: 'play_circle',
          url: '/admin/speed-reading/sessions',
          permissions: ['SpeedReading.Read'],
          breadcrumbs: true
        },
        {
          id: 'speed-reading-texts',
          title: 'Metinler',
          type: 'item',
          iconName: 'article',
          url: '/admin/speed-reading/texts',
          permissions: ['SpeedReading.Read'],
          breadcrumbs: true
        },
        {
          id: 'speed-reading-analytics',
          title: 'Analitik',
          type: 'item',
          iconName: 'trending_up',
          url: '/admin/speed-reading/analytics',
          permissions: ['SpeedReading.Read'],
          breadcrumbs: true
        }
      ]
    },
    {
      id: 'divider-2',
      title: '',
      type: 'divider'
    },
    {
      id: 'system',
      title: 'Sistem',
      type: 'group',
      icon: 'fas fa-cog',
      permissions: ['System.Read'],
      children: [
        {
          id: 'audit-logs',
          title: 'Denetim Kayıtları',
          type: 'item',
          icon: 'fas fa-history',
          url: '/audit-logs',
          permissions: ['AuditLogs.Read'],
          breadcrumbs: true
        },
        {
          id: 'system-settings',
          title: 'Sistem Ayarları',
          type: 'item',
          icon: 'fas fa-sliders-h',
          url: '/system-settings',
          permissions: ['System.Write'],
          breadcrumbs: true
        },
        {
          id: 'backup',
          title: 'Yedekleme',
          type: 'item',
          icon: 'fas fa-database',
          url: '/backup',
          permissions: ['System.Backup'],
          breadcrumbs: true
        }
      ]
    },
    {
      id: 'divider-3',
      title: '',
      type: 'divider'
    },
    {
      id: 'profile',
      title: 'Profil',
      type: 'item',
      iconName: 'account_circle',
      url: '/admin/profile',
      breadcrumbs: true
    },
    {
      id: 'settings',
      title: 'Ayarlar',
      type: 'item',
      iconName: 'settings',
      url: '/admin/settings',
      breadcrumbs: true
    }
  ];

  private filteredNavigationSubject = new BehaviorSubject<NavigationItem[]>([]);
  private breadcrumbsSubject = new BehaviorSubject<BreadcrumbItem[]>([]);

  navigation$ = this.filteredNavigationSubject.asObservable();
  breadcrumbs$ = this.breadcrumbsSubject.asObservable();

  activeItem = signal<string>('');

  constructor() {
    this.initializeNavigation();
    this.setupRouteListener();
  }

  private async initializeNavigation(): Promise<void> {
    const filteredItems = await this.filterNavigationByPermissions(this.navigationItems);
    this.filteredNavigationSubject.next(filteredItems);
  }

  private setupRouteListener(): void {
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        map(event => (event as NavigationEnd).url)
      )
      .subscribe(url => {
        this.updateActiveItem(url);
        this.updateBreadcrumbs(url);
      });
  }

  private async filterNavigationByPermissions(items: NavigationItem[]): Promise<NavigationItem[]> {
    const filteredItems: NavigationItem[] = [];

    for (const item of items) {
      if (await this.hasAccess(item)) {
        const filteredItem = { ...item };

        if (item.children) {
          const filteredChildren = await this.filterNavigationByPermissions(item.children);
          if (filteredChildren.length > 0) {
            filteredItem.children = filteredChildren;
            filteredItems.push(filteredItem);
          }
        } else {
          filteredItems.push(filteredItem);
        }
      }
    }

    return filteredItems;
  }

  private async hasAccess(item: NavigationItem): Promise<boolean> {
    const required = (item as any).permissions as string[] | undefined;
    if (!required || required.length === 0) return true;
    return this.perms.hasAll(required);
  }

  private updateActiveItem(url: string): void {
    const item = this.findNavigationItemByUrl(url, this.navigationItems);
    this.activeItem.set(item?.id || '');
  }

  private findNavigationItemByUrl(url: string, items: NavigationItem[]): NavigationItem | null {
    for (const item of items) {
      if (item.url) {
        if (item.exactMatch ? item.url === url : url.startsWith(item.url)) {
          return item;
        }
      }

      if (item.children) {
        const found = this.findNavigationItemByUrl(url, item.children);
        if (found) {
          return found;
        }
      }
    }

    return null;
  }

  private updateBreadcrumbs(url: string): void {
    const breadcrumbs = this.generateBreadcrumbs(url);
    this.breadcrumbsSubject.next(breadcrumbs);
  }

  private generateBreadcrumbs(url: string): BreadcrumbItem[] {
    const breadcrumbs: BreadcrumbItem[] = [];

    // For home page, only show "Ana"
    if (url === '/admin' || url === '/admin/') {
      breadcrumbs.push({ label: 'Ana' });
      return breadcrumbs;
    }

    // For other pages, start with "Ana" as home
    breadcrumbs.push({ label: 'Ana', url: '/admin' });

    const item = this.findNavigationItemByUrl(url, this.navigationItems);
    if (item && item.breadcrumbs) {
      // Find parent path
      const parentPath = this.findParentPath(item, this.navigationItems);

      // Add parent breadcrumbs
      parentPath.forEach(parentItem => {
        if (parentItem.breadcrumbs && parentItem.url) {
          breadcrumbs.push({
            label: parentItem.title,
            url: parentItem.url
          });
        }
      });

      // Add current item (without URL since it's the current page)
      breadcrumbs.push({
        label: item.title
      });
    }

    return breadcrumbs;
  }

  private findParentPath(targetItem: NavigationItem, items: NavigationItem[], path: NavigationItem[] = []): NavigationItem[] {
    for (const item of items) {
      const currentPath = [...path, item];

      if (item === targetItem) {
        return path; // Return path without the target item itself
      }

      if (item.children) {
        const found = this.findParentPath(targetItem, item.children, currentPath);
        if (found.length > 0) {
          return found;
        }
      }
    }

    return [];
  }

  getNavigationItems(): Observable<NavigationItem[]> {
    return this.navigation$;
  }

  getBreadcrumbs(): Observable<BreadcrumbItem[]> {
    return this.breadcrumbs$;
  }

  isActive(itemId: string): boolean {
    return this.activeItem() === itemId;
  }

  async refreshNavigation(): Promise<void> {
    await this.initializeNavigation();
  }
}