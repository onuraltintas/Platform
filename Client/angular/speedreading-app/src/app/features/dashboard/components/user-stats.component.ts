import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

interface UserProgress {
  currentLevel: number;
  readingSpeed: number;
  comprehensionRate: number;
  totalTextsRead: number;
  totalExercisesCompleted: number;
  streakDays: number;
  weeklyProgress: number[];
  monthlyProgress: { month: string; speed: number; comprehension: number }[];
}

@Component({
  selector: 'app-user-stats',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card user-stats-card">
      <div class="card-header">
        <h3 class="card-title">
          <i class="bi bi-speedometer2"></i>
          Performans Özeti
        </h3>
      </div>
      
      <div class="card-body">
        <!-- Current Level Display -->
        <div class="level-section">
          <div class="level-display">
            <div class="level-circle">
              <div class="level-progress" [style.background]="'conic-gradient(#3b82f6 ' + getLevelProgress() + 'deg, #e5e7eb 0deg)'">
                <div class="level-inner">
                  <span class="level-number">{{ userProgress?.currentLevel || 1 }}</span>
                  <span class="level-label">Seviye</span>
                </div>
              </div>
            </div>
            <div class="level-info">
              <h4>{{ getLevelTitle() }}</h4>
              <p>{{ getLevelDescription() }}</p>
              <div class="level-progress-bar">
                <div class="progress-fill" [style.width.%]="getLevelProgressPercent()"></div>
                <span class="progress-text">{{ getLevelProgressPercent() }}% sonraki seviyeye</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Key Metrics -->
        <div class="metrics-grid">
          <div class="metric-card speed-metric">
            <div class="metric-icon">
              <i class="bi bi-lightning-charge"></i>
            </div>
            <div class="metric-content">
              <div class="metric-value">{{ userProgress?.readingSpeed || 0 }}</div>
              <div class="metric-label">Kelime/Dakika</div>
              <div class="metric-change" [class]="getSpeedChangeClass()">
                <i class="bi" [class]="getSpeedChangeIcon()"></i>
                <span>{{ getSpeedChange() }}%</span>
              </div>
            </div>
          </div>

          <div class="metric-card comprehension-metric">
            <div class="metric-icon">
              <i class="bi bi-bullseye"></i>
            </div>
            <div class="metric-content">
              <div class="metric-value">{{ userProgress?.comprehensionRate || 0 }}%</div>
              <div class="metric-label">Anlama Oranı</div>
              <div class="metric-change positive">
                <i class="bi bi-arrow-up"></i>
                <span>+3%</span>
              </div>
            </div>
          </div>

          <div class="metric-card streak-metric">
            <div class="metric-icon">
              <i class="bi bi-fire"></i>
            </div>
            <div class="metric-content">
              <div class="metric-value">{{ userProgress?.streakDays || 0 }}</div>
              <div class="metric-label">Günlük Seri</div>
              <div class="streak-flames">
                <i class="bi bi-fire" *ngFor="let flame of getStreakFlames(); let i = index" [class.active]="i < (userProgress?.streakDays || 0)"></i>
              </div>
            </div>
          </div>
        </div>

        <!-- Additional Stats -->
        <div class="additional-stats">
          <div class="stat-item">
            <div class="stat-icon">
              <i class="bi bi-book"></i>
            </div>
            <div class="stat-info">
              <span class="stat-value">{{ userProgress?.totalTextsRead || 0 }}</span>
              <span class="stat-label">Okunan Metin</span>
            </div>
          </div>

          <div class="stat-item">
            <div class="stat-icon">
              <i class="bi bi-puzzle"></i>
            </div>
            <div class="stat-info">
              <span class="stat-value">{{ userProgress?.totalExercisesCompleted || 0 }}</span>
              <span class="stat-label">Tamamlanan Egzersiz</span>
            </div>
          </div>

          <div class="stat-item">
            <div class="stat-icon">
              <i class="bi bi-trophy"></i>
            </div>
            <div class="stat-info">
              <span class="stat-value">{{ getTotalPoints() }}</span>
              <span class="stat-label">Toplam Puan</span>
            </div>
          </div>

          <div class="stat-item">
            <div class="stat-icon">
              <i class="bi bi-calendar-week"></i>
            </div>
            <div class="stat-info">
              <span class="stat-value">{{ getWeeklyHours() }}h</span>
              <span class="stat-label">Bu Hafta</span>
            </div>
          </div>
        </div>

        <!-- Achievement Preview -->
        <div class="achievement-preview">
          <div class="achievement-header">
            <span class="achievement-title">Sonraki Hedef</span>
            <button class="view-all-btn">Tümünü Gör</button>
          </div>
          <div class="next-achievement">
            <div class="achievement-icon">
              <i class="bi bi-lightning-charge"></i>
            </div>
            <div class="achievement-info">
              <h4>Hız Canavarı</h4>
              <p>300+ WPM'e ulaş</p>
              <div class="achievement-progress">
                <div class="progress-bar">
                  <div class="progress-fill" [style.width.%]="getAchievementProgress()"></div>
                </div>
                <span class="progress-text">{{ getAchievementProgress() }}%</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./user-stats.component.scss']
})
export class UserStatsComponent {
  @Input() userProgress: UserProgress | null = null;

  getLevelTitle(): string {
    const level = this.userProgress?.currentLevel || 1;
    const titles = [
      'Yeni Başlayan',      // Level 1
      'Gelişen Okuyucu',    // Level 2
      'Hızlı Okuyucu',      // Level 3
      'Uzman Okuyucu',      // Level 4
      'Hız Ustası'          // Level 5+
    ];
    return titles[Math.min(level - 1, titles.length - 1)];
  }

  getLevelDescription(): string {
    const level = this.userProgress?.currentLevel || 1;
    const descriptions = [
      'Okuma yolculuğuna yeni başladınız',
      'Temel teknikleri öğreniyorsunuz',
      'Hızınızı artırmaya başladınız',
      'İleri teknikleri ustaca kullanıyorsunuz',
      'Okuma hızında zirvedesiniz'
    ];
    return descriptions[Math.min(level - 1, descriptions.length - 1)];
  }

  getLevelProgress(): number {
    // Calculate level progress as degrees (0-360)
    const progress = this.getLevelProgressPercent();
    return (progress / 100) * 360;
  }

  getLevelProgressPercent(): number {
    if (!this.userProgress) return 0;
    
    // Calculate progress to next level based on reading speed
    const speedThresholds = [0, 150, 250, 350, 450, 600]; // Speed thresholds for each level
    const currentLevel = this.userProgress.currentLevel;
    const currentSpeed = this.userProgress.readingSpeed;
    
    if (currentLevel >= speedThresholds.length - 1) return 100;
    
    const currentThreshold = speedThresholds[currentLevel - 1];
    const nextThreshold = speedThresholds[currentLevel];
    
    const progress = Math.min(100, Math.max(0, 
      ((currentSpeed - currentThreshold) / (nextThreshold - currentThreshold)) * 100
    ));
    
    return Math.round(progress);
  }

  getSpeedChange(): number {
    if (!this.userProgress?.weeklyProgress || this.userProgress.weeklyProgress.length < 2) return 0;
    
    const current = this.userProgress.weeklyProgress[this.userProgress.weeklyProgress.length - 1];
    const previous = this.userProgress.weeklyProgress[this.userProgress.weeklyProgress.length - 2];
    
    return Math.round(((current - previous) / previous) * 100);
  }

  getSpeedChangeClass(): string {
    const change = this.getSpeedChange();
    if (change > 0) return 'positive';
    if (change < 0) return 'negative';
    return 'neutral';
  }

  getSpeedChangeIcon(): string {
    const change = this.getSpeedChange();
    if (change > 0) return 'bi-arrow-up';
    if (change < 0) return 'bi-arrow-down';
    return 'bi-dash';
  }

  getStreakFlames(): number[] {
    return Array(7).fill(0); // Show 7 flame icons maximum
  }

  getTotalPoints(): number {
    if (!this.userProgress) return 0;
    
    // Calculate points based on activities
    const textPoints = (this.userProgress.totalTextsRead || 0) * 50;
    const exercisePoints = (this.userProgress.totalExercisesCompleted || 0) * 25;
    const speedBonus = Math.floor((this.userProgress.readingSpeed || 0) / 10) * 5;
    const comprehensionBonus = Math.floor((this.userProgress.comprehensionRate || 0) / 5) * 10;
    
    return textPoints + exercisePoints + speedBonus + comprehensionBonus;
  }

  getWeeklyHours(): number {
    if (!this.userProgress) return 0;
    
    // Estimate based on activities (each text ~5min, each exercise ~3min)
    const textTime = (this.userProgress.totalTextsRead || 0) * 5;
    const exerciseTime = (this.userProgress.totalExercisesCompleted || 0) * 3;
    const totalMinutes = textTime + exerciseTime;
    
    // Simulate weekly portion (last 25% of activity)
    const weeklyMinutes = Math.floor(totalMinutes * 0.25);
    return Math.round(weeklyMinutes / 60 * 10) / 10; // Round to 1 decimal
  }

  getAchievementProgress(): number {
    if (!this.userProgress) return 0;
    
    // Progress towards 300 WPM achievement
    const targetSpeed = 300;
    const currentSpeed = this.userProgress.readingSpeed || 0;
    return Math.min(100, Math.round((currentSpeed / targetSpeed) * 100));
  }
}