import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

interface DailyGoal {
  type: 'reading' | 'exercise' | 'time';
  target: number;
  current: number;
  unit: string;
  icon: string;
}

@Component({
  selector: 'app-daily-goals',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card daily-goals-card">
      <div class="card-header">
        <h3 class="card-title">
          <i class="bi bi-target"></i>
          Günlük Hedefler
        </h3>
        <div class="goals-date">
          <i class="bi bi-calendar3"></i>
          <span>{{ getCurrentDate() }}</span>
        </div>
      </div>
      
      <div class="card-body">
        <div class="goals-overview">
          <div class="overview-item">
            <span class="overview-label">Tamamlanan</span>
            <span class="overview-value">{{ getCompletedGoals() }}/{{ (goals || []).length }}</span>
          </div>
          <div class="overview-item">
            <span class="overview-label">Genel İlerleme</span>
            <span class="overview-value">{{ getOverallProgress() }}%</span>
          </div>
        </div>

        <div class="goals-list">
          <div 
            class="goal-item" 
            *ngFor="let goal of goals; let i = index"
            [class.completed]="isGoalCompleted(goal)">
            
            <div class="goal-header">
              <div class="goal-info">
                <div class="goal-icon" [class]="getGoalIconClass(goal.type)">
                  <i class="bi" [class]="goal.icon"></i>
                </div>
                <div class="goal-details">
                  <h4 class="goal-title">{{ getGoalTitle(goal.type) }}</h4>
                  <div class="goal-progress-text">
                    <span class="current">{{ goal.current }}</span>
                    <span class="separator">/</span>
                    <span class="target">{{ goal.target }}</span>
                    <span class="unit">{{ goal.unit }}</span>
                  </div>
                </div>
              </div>
              
              <div class="goal-status">
                <div class="status-circle" [class.completed]="isGoalCompleted(goal)">
                  <i class="bi bi-check" *ngIf="isGoalCompleted(goal)"></i>
                  <span *ngIf="!isGoalCompleted(goal)">{{ getGoalProgressPercent(goal) }}%</span>
                </div>
              </div>
            </div>

            <div class="goal-progress">
              <div class="progress-bar">
                <div 
                  class="progress-fill" 
                  [style.width.%]="getGoalProgressPercent(goal)"
                  [class]="getProgressBarClass(goal.type)">
                </div>
              </div>
              
              <div class="progress-actions" *ngIf="!isGoalCompleted(goal)">
                <button 
                  class="quick-action-btn"
                  (click)="quickStart(goal.type)"
                  [title]="getQuickActionTitle(goal.type)">
                  <i class="bi" [class]="getQuickActionIcon(goal.type)"></i>
                  <span>{{ getQuickActionText(goal.type) }}</span>
                </button>
              </div>
            </div>

            <!-- Goal Tips -->
            <div class="goal-tip" *ngIf="getGoalTip(goal)">
              <i class="bi bi-lightbulb"></i>
              <span>{{ getGoalTip(goal) }}</span>
            </div>
          </div>
        </div>

        <!-- Motivational Section -->
        <div class="motivation-section" *ngIf="getCompletedGoals() === (goals || []).length">
          <div class="celebration">
            <i class="bi bi-emoji-smile"></i>
            <h4>Tebrikler! 🎉</h4>
            <p>Bugünkü tüm hedeflerinizi tamamladınız!</p>
            <button class="bonus-challenge-btn" (click)="startBonusChallenge()">
              <i class="bi bi-plus-circle"></i>
              Bonus Meydan Okuma
            </button>
          </div>
        </div>

        <div class="motivation-section" *ngIf="getCompletedGoals() < (goals || []).length && getCompletedGoals() > 0">
          <div class="encouragement">
            <i class="bi bi-lightning-charge"></i>
            <h4>Harika gidiyorsun! ⚡</h4>
            <p>{{ getEncouragementMessage() }}</p>
          </div>
        </div>

        <!-- Weekly Streak -->
        <div class="weekly-streak">
          <div class="streak-header">
            <span class="streak-title">Bu Hafta</span>
            <span class="streak-count">{{ getWeeklyStreak() }}/7 gün</span>
          </div>
          <div class="streak-days">
            <div 
              class="streak-day" 
              *ngFor="let day of getWeekDays(); let i = index"
              [class.completed]="day.completed"
              [class.today]="day.isToday">
              <span class="day-name">{{ day.name }}</span>
              <div class="day-indicator">
                <i class="bi bi-check" *ngIf="day.completed"></i>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./daily-goals.component.scss']
})
export class DailyGoalsComponent {
  @Input() goals: DailyGoal[] = [];

  getCurrentDate(): string {
    const today = new Date();
    const options: Intl.DateTimeFormatOptions = { 
      day: 'numeric', 
      month: 'long'
    };
    return today.toLocaleDateString('tr-TR', options);
  }

  getCompletedGoals(): number {
    return this.goals?.filter(goal => this.isGoalCompleted(goal)).length || 0;
  }

  getOverallProgress(): number {
    if (!this.goals || this.goals.length === 0) return 0;
    
    const totalProgress = this.goals.reduce((sum, goal) => {
      return sum + this.getGoalProgressPercent(goal);
    }, 0);
    
    return Math.round(totalProgress / this.goals.length);
  }

  isGoalCompleted(goal: DailyGoal): boolean {
    return goal.current >= goal.target;
  }

  getGoalProgressPercent(goal: DailyGoal): number {
    return Math.min(100, Math.round((goal.current / goal.target) * 100));
  }

  getGoalTitle(type: string): string {
    const titles = {
      'reading': 'Okuma Hedefi',
      'exercise': 'Egzersiz Hedefi',
      'time': 'Zaman Hedefi'
    };
    return titles[type as keyof typeof titles] || type;
  }

  getGoalIconClass(type: string): string {
    const classes = {
      'reading': 'reading-goal',
      'exercise': 'exercise-goal',
      'time': 'time-goal'
    };
    return classes[type as keyof typeof classes] || '';
  }

  getProgressBarClass(type: string): string {
    const classes = {
      'reading': 'reading-progress',
      'exercise': 'exercise-progress',
      'time': 'time-progress'
    };
    return classes[type as keyof typeof classes] || '';
  }

  getQuickActionIcon(type: string): string {
    const icons = {
      'reading': 'bi-play-fill',
      'exercise': 'bi-play-fill',
      'time': 'bi-stopwatch'
    };
    return icons[type as keyof typeof icons] || 'bi-play-fill';
  }

  getQuickActionText(type: string): string {
    const texts = {
      'reading': 'Oku',
      'exercise': 'Başla',
      'time': 'Başla'
    };
    return texts[type as keyof typeof texts] || 'Başla';
  }

  getQuickActionTitle(type: string): string {
    const titles = {
      'reading': 'Hızlı okuma başlat',
      'exercise': 'Egzersiz başlat',
      'time': 'Zamanlayıcı başlat'
    };
    return titles[type as keyof typeof titles] || 'Başlat';
  }

  getGoalTip(goal: DailyGoal): string {
    if (this.isGoalCompleted(goal)) return '';
    
    const progress = this.getGoalProgressPercent(goal);
    const remaining = goal.target - goal.current;
    
    if (progress === 0) {
      return `İlk ${goal.unit} için ${this.getQuickActionText(goal.type).toLowerCase()} butonuna tıklayın`;
    } else if (progress < 50) {
      return `${remaining} ${goal.unit} daha kaldı, devam edin!`;
    } else if (progress < 100) {
      return `Neredeyse bitti! ${remaining} ${goal.unit} kaldı`;
    }
    
    return '';
  }

  quickStart(type: string): void {
    // Simulate starting the activity
    console.log(`Quick starting ${type} activity`);
    
    // In a real app, this would navigate to the appropriate section
    switch (type) {
      case 'reading':
        // Navigate to reading section
        break;
      case 'exercise':
        // Navigate to exercises
        break;
      case 'time':
        // Start timer
        break;
    }
  }

  getEncouragementMessage(): string {
    const messages = [
      'Kalan hedeflerini tamamlamak için sadece birkaç adım var!',
      'Bu momentum ile tüm hedefleri tamamlayabilirsin!',
      'Harika ilerleme! Devam et!'
    ];
    return messages[Math.floor(Math.random() * messages.length)];
  }

  startBonusChallenge(): void {
    console.log('Starting bonus challenge');
    // In real app, this would show bonus challenges modal or navigate to challenges
  }

  getWeeklyStreak(): number {
    // Simulate weekly completion data
    return 5; // 5 out of 7 days completed this week
  }

  getWeekDays(): Array<{name: string, completed: boolean, isToday: boolean}> {
    const days = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
    const today = new Date().getDay(); // 0 = Sunday, 1 = Monday, etc.
    const mondayBasedToday = today === 0 ? 6 : today - 1; // Convert to Monday = 0 based
    
    // Simulate completion data
    const completedDays = [true, true, true, false, true, false, false];
    
    return days.map((day, index) => ({
      name: day,
      completed: completedDays[index],
      isToday: index === mondayBasedToday
    }));
  }
}