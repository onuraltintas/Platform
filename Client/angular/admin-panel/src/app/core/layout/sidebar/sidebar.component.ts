import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { trigger, state, style, transition, animate } from '@angular/animations';
import { AuthService } from '../../../features/access-control/data-access/auth.service';
import { Router } from '@angular/router';

interface NavItem {
  name: string;
  url: string;
  icon: string;
  badge?: {
    text: string;
    color: string;
  };
  children?: NavItem[];
  isExpanded?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
  animations: [
    trigger('slideInOut', [
      state('in', style({ height: '*', opacity: 1 })),
      state('out', style({ height: '0', opacity: 0 })),
      transition('in => out', animate('200ms ease-in-out')),
      transition('out => in', animate('200ms ease-in-out'))
    ])
  ]
})
export class SidebarComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  navItems: NavItem[] = [
    { name: 'Dashboard', url: '/dashboard', icon: 'cil-speedometer' },
    {
      name: 'Kullanıcılar',
      url: '/users',
      icon: 'cil-people',
      children: [
        {
          name: 'Kullanıcı',
          url: '/users',
          icon: 'cil-user',
          children: [
            { name: 'Liste', url: '/users', icon: 'cil-list' },
            { name: 'Kullanıcı Ekle', url: '/users/new', icon: 'cil-user-follow' }
          ]
        },
        {
          name: 'Roller',
          url: '/roles',
          icon: 'cil-shield-alt',
          children: [
            { name: 'Rol Listesi', url: '/roles', icon: 'cil-list' },
            { name: 'Yeni Rol', url: '/roles/new', icon: 'cil-plus' }
          ]
        },
        {
          name: 'İzinler',
          url: '/permissions',
          icon: 'cil-lock-locked',
          children: [
            { name: 'İzin Listesi', url: '/permissions', icon: 'cil-list' },
            { name: 'Yeni İzin', url: '/permissions/new', icon: 'cil-plus' }
          ]
        },
        {
          name: 'Kategoriler',
          url: '/categories',
          icon: 'cil-tags',
          children: [
            { name: 'Kategori Listesi', url: '/categories', icon: 'cil-list' },
            { name: 'Yeni Kategori', url: '/categories/new', icon: 'cil-plus' }
          ]
        }
      ]
    },
    {
      name: 'Hızlı Okuma',
      url: '/sr',
      icon: 'cil-book',
      children: [
        {
          name: 'Metinler',
          url: '/sr/texts',
          icon: 'cil-description'
        },
        {
          name: 'Egzersizler',
          url: '/sr/exercises',
          icon: 'cil-check-circle'
        },
        {
          name: 'Sorular',
          url: '/sr/questions',
          icon: 'bi bi-question-circle'
        },
        {
          name: 'Seviyeler',
          url: '/sr/levels',
          icon: 'cil-layers'
        },
        {
          name: 'Raporlar',
          url: '/sr/reports',
          icon: 'cil-chart'
        }
      ]
    }
  ];

  isNavItemActive(url: string): boolean {
    return window.location.pathname.startsWith(url);
  }

  toggleNavItem(item: NavItem): void {
    if (item.children) {
      item.isExpanded = !item.isExpanded;
    } else {
      this.router.navigate([item.url]);
    }
  }

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => {
        // Don't clear logout flag immediately, let login page handle it
        window.location.href = '/login';
      }
    });
  }
}