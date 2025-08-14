import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../access-control/data-access/auth.service';

interface DashboardCard {
  title: string;
  value: string | number;
  icon: string;
  color: string;
  trend?: {
    value: number;
    isPositive: boolean;
  };
}

interface RecentActivity {
  id: string;
  action: string;
  user: string;
  timestamp: Date;
  type: 'success' | 'warning' | 'info' | 'error';
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  
  currentUser: any = {};
  
  dashboardCards: DashboardCard[] = [
    {
      title: 'Toplam Kullanıcı',
      value: 1245,
      icon: 'cil-people',
      color: 'primary',
      trend: { value: 12.5, isPositive: true }
    },
    {
      title: 'Aktif Kurslar',
      value: 89,
      icon: 'cil-education',
      color: 'success',
      trend: { value: 8.2, isPositive: true }
    },
    {
      title: 'Bu Ay Kayıt',
      value: 156,
      icon: 'cil-user-plus',
      color: 'info',
      trend: { value: 5.3, isPositive: false }
    },
    {
      title: 'Toplam Gelir',
      value: '₺45,890',
      icon: 'cil-euro',
      color: 'warning',
      trend: { value: 18.7, isPositive: true }
    }
  ];

  recentActivities: RecentActivity[] = [
    {
      id: '1',
      action: 'Yeni kullanıcı kaydı',
      user: 'Ahmet Yılmaz',
      timestamp: new Date(Date.now() - 1000 * 60 * 5),
      type: 'success'
    },
    {
      id: '2',
      action: 'Kurs tamamlandı',
      user: 'Ayşe Kaya',
      timestamp: new Date(Date.now() - 1000 * 60 * 15),
      type: 'info'
    },
    {
      id: '3',
      action: 'Ödeme alındı',
      user: 'Mehmet Demir',
      timestamp: new Date(Date.now() - 1000 * 60 * 30),
      type: 'success'
    },
    {
      id: '4',
      action: 'Destek talebi',
      user: 'Fatma Özkan',
      timestamp: new Date(Date.now() - 1000 * 60 * 45),
      type: 'warning'
    }
  ];

  ngOnInit() {
    this.loadCurrentUser();
  }

  private loadCurrentUser() {
    try {
      const userStr = localStorage.getItem('user');
      this.currentUser = userStr ? JSON.parse(userStr) : {};
    } catch (error) {
      console.error('Failed to parse user data:', error);
      this.currentUser = {};
    }
  }

  getTimeAgo(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / (1000 * 60));
    
    if (diffMins < 1) return 'Az önce';
    if (diffMins < 60) return `${diffMins} dakika önce`;
    
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours} saat önce`;
    
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays} gün önce`;
  }

  getActivityIcon(type: string): string {
    switch (type) {
      case 'success': return 'cil-check-circle';
      case 'warning': return 'cil-warning';
      case 'info': return 'cil-info';
      case 'error': return 'cil-x-circle';
      default: return 'cil-circle';
    }
  }

  getCurrentDate(): string {
    const now = new Date();
    const options: Intl.DateTimeFormatOptions = { 
      weekday: 'long', 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    };
    return now.toLocaleDateString('tr-TR', options);
  }

  quickAction(action: string): void {
    console.log('Quick action:', action);
    // TODO: Implement navigation or modal opening based on action
    switch (action) {
      case 'users':
        // Navigate to users page
        break;
      case 'course':
        // Navigate to add course page
        break;
      case 'reports':
        // Navigate to reports page
        break;
      case 'settings':
        // Navigate to settings page
        break;
    }
  }
}