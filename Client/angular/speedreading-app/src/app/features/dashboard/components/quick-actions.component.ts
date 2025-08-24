import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

interface QuickAction {
  id: string;
  title: string;
  description: string;
  icon: string;
  color: string;
  route?: string;
  action?: () => void;
  badge?: string;
  estimatedTime?: number;
}

@Component({
  selector: 'app-quick-actions',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card quick-actions-card">
      <div class="card-header">
        <h3 class="card-title">
          <i class="bi bi-lightning-charge"></i>
          Hızlı Erişim
        </h3>
      </div>
      
      <div class="card-body">
        <div class="actions-grid">
          <div 
            class="action-item" 
            *ngFor="let action of quickActions"
            [class]="'action-' + action.color"
            (click)="executeAction(action)">
            
            <div class="action-content">
              <div class="action-icon">
                <i class="bi" [class]="action.icon"></i>
              </div>
              
              <div class="action-info">
                <h4 class="action-title">{{ action.title }}</h4>
                <p class="action-description">{{ action.description }}</p>
                
                <div class="action-meta" *ngIf="action.estimatedTime">
                  <i class="bi bi-clock"></i>
                  <span>~{{ action.estimatedTime }} dk</span>
                </div>
              </div>

              <div class="action-badge" *ngIf="action.badge">
                <span>{{ action.badge }}</span>
              </div>
            </div>

            <div class="action-hover-effect"></div>
          </div>
        </div>

        <!-- Recent Actions -->
        <div class="recent-actions">
          <div class="recent-header">
            <h4>Son Kullanılan</h4>
            <button class="clear-recent-btn" (click)="clearRecentActions()" title="Geçmişi temizle">
              <i class="bi bi-x-circle"></i>
            </button>
          </div>
          
          <div class="recent-list" *ngIf="recentActions.length > 0">
            <div 
              class="recent-item" 
              *ngFor="let action of recentActions"
              (click)="executeAction(action)">
              <div class="recent-icon">
                <i class="bi" [class]="action.icon"></i>
              </div>
              <div class="recent-info">
                <span class="recent-title">{{ action.title }}</span>
                <span class="recent-time">{{ action.lastUsed }}</span>
              </div>
              <div class="recent-arrow">
                <i class="bi bi-arrow-right"></i>
              </div>
            </div>
          </div>

          <div class="no-recent" *ngIf="recentActions.length === 0">
            <i class="bi bi-clock-history"></i>
            <p>Henüz kullanılan eylem yok</p>
          </div>
        </div>

        <!-- Shortcuts Info -->
        <div class="shortcuts-info">
          <div class="shortcuts-header">
            <i class="bi bi-keyboard"></i>
            <span>Klavye Kısayolları</span>
          </div>
          <div class="shortcuts-list">
            <div class="shortcut-item">
              <kbd>E</kbd>
              <span>Egzersiz Başlat</span>
            </div>
            <div class="shortcut-item">
              <kbd>R</kbd>
              <span>Hızlı Okuma</span>
            </div>
            <div class="shortcut-item">
              <kbd>T</kbd>
              <span>Test Al</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./quick-actions.component.scss']
})
export class QuickActionsComponent {
  quickActions: QuickAction[] = [
    {
      id: 'speed-reading',
      title: 'Hızlı Okuma',
      description: 'RSVP teknikleri ile okuma alıştırması yap',
      icon: 'bi-lightning-fill',
      color: 'blue',
      action: () => this.router.navigate(['/reading/start'], { 
        queryParams: { 
          textId: 'b6dc1ba2-5cd8-4ca3-b202-26e5b8da1d6d', // Hızlı Okuma Teknikleri (155 kelime)
          mode: 'classic' 
        } 
      }),
      estimatedTime: 10,
      badge: 'Popüler'
    },
    {
      id: 'eye-exercise',
      title: 'Göz Egzersizi',
      description: 'Göz koordinasyonunu geliştir',
      icon: 'bi-eye-fill',
      color: 'green',
      route: '/exercises/eye-coordination',
      estimatedTime: 5
    },
    {
      id: 'comprehension-test',
      title: 'Anlama Testi',
      description: 'Okuduğunu anlama becerini test et',
      icon: 'bi-clipboard-check',
      color: 'purple',
      route: '/tests/comprehension',
      estimatedTime: 15
    },
    {
      id: 'daily-text',
      title: 'Spor ve Sağlık',
      description: 'Temel seviye metin (234 kelime)',
      icon: 'bi-file-text-fill',
      color: 'orange',
      action: () => this.router.navigate(['/reading/start'], { 
        queryParams: { 
          textId: '9bf05f44-4a7f-4e18-8abc-a13c9dcfe36d', // Spor Yapmanın Faydaları (234 kelime)
          mode: 'classic' 
        } 
      }),
      estimatedTime: 8,
      badge: 'Yeni'
    },
    {
      id: 'technique-practice',
      title: 'Uzay Keşifleri',
      description: 'Orta seviye metin (286 kelime)',
      icon: 'bi-rocket-takeoff-fill',
      color: 'teal',
      action: () => this.router.navigate(['/reading/start'], { 
        queryParams: { 
          textId: '4eac6500-6446-4435-ac72-248d5aa1831b', // Uzay Keşifleri (286 kelime)
          mode: 'classic' 
        } 
      }),
      estimatedTime: 12
    },
    {
      id: 'progress-review',
      title: 'İlerleme İncelemesi',
      description: 'Detaylı performans analizi',
      icon: 'bi-graph-up',
      color: 'indigo',
      route: '/reports/detailed',
      estimatedTime: 5
    }
  ];

  recentActions: Array<QuickAction & { lastUsed: string }> = [
    {
      ...this.quickActions[0],
      lastUsed: '2 saat önce'
    },
    {
      ...this.quickActions[2],
      lastUsed: 'Dün'
    }
  ];

  constructor(private router: Router) {
    this.setupKeyboardShortcuts();
  }

  executeAction(action: QuickAction): void {
    // Track action usage
    this.trackActionUsage(action);
    
    // Add to recent actions if not already there
    this.addToRecentActions(action);

    if (action.route) {
      this.router.navigate([action.route]);
    } else if (action.action) {
      action.action();
    } else {
      // Default action based on ID
      this.handleDefaultAction(action.id);
    }
  }

  private trackActionUsage(action: QuickAction): void {
    // Analytics tracking
    const event = {
      type: 'quick_action_used',
      actionId: action.id,
      actionTitle: action.title,
      timestamp: new Date().toISOString()
    };
    
    console.log('Quick action used:', event);
    
    // Store in localStorage for demo
    const events = JSON.parse(localStorage.getItem('quickActionEvents') || '[]');
    events.push(event);
    localStorage.setItem('quickActionEvents', JSON.stringify(events.slice(-50)));
  }

  private addToRecentActions(action: QuickAction): void {
    // Remove if already exists
    this.recentActions = this.recentActions.filter(recent => recent.id !== action.id);
    
    // Add to beginning
    this.recentActions.unshift({
      ...action,
      lastUsed: 'Az önce'
    });
    
    // Keep only last 5
    this.recentActions = this.recentActions.slice(0, 5);
  }

  private handleDefaultAction(actionId: string): void {
    switch (actionId) {
      case 'speed-reading':
        console.log('Starting speed reading exercise');
        break;
      case 'eye-exercise':
        console.log('Starting eye coordination exercise');
        break;
      case 'comprehension-test':
        console.log('Starting comprehension test');
        break;
      case 'daily-text':
        console.log('Opening daily text');
        break;
      case 'technique-practice':
        console.log('Starting technique practice');
        break;
      case 'progress-review':
        console.log('Opening progress review');
        break;
      default:
        console.log(`Unknown action: ${actionId}`);
    }
  }

  clearRecentActions(): void {
    this.recentActions = [];
  }

  private setupKeyboardShortcuts(): void {
    document.addEventListener('keydown', (event) => {
      // Only trigger if no input is focused
      if (document.activeElement?.tagName === 'INPUT' || 
          document.activeElement?.tagName === 'TEXTAREA') {
        return;
      }

      switch (event.key.toLowerCase()) {
        case 'e':
          event.preventDefault();
          this.executeAction(this.quickActions.find(a => a.id === 'eye-exercise')!);
          break;
        case 'r':
          event.preventDefault();
          this.executeAction(this.quickActions.find(a => a.id === 'speed-reading')!);
          break;
        case 't':
          event.preventDefault();
          this.executeAction(this.quickActions.find(a => a.id === 'comprehension-test')!);
          break;
      }
    });
  }
}