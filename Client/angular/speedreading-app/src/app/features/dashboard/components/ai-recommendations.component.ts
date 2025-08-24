import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

interface AiRecommendation {
  type: 'exercise' | 'text' | 'technique';
  title: string;
  description: string;
  difficulty: 'Kolay' | 'Orta' | 'Zor';
  estimatedTime: number;
  reason: string;
}

@Component({
  selector: 'app-ai-recommendations',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card ai-recommendations-card">
      <div class="card-header">
        <h3 class="card-title">
          <i class="bi bi-robot"></i>
          AI Önerileri
        </h3>
        <button class="refresh-btn" (click)="refreshRecommendations()" [disabled]="isRefreshing">
          <i class="bi" [class.bi-arrow-clockwise]="!isRefreshing" [class.bi-hourglass-split]="isRefreshing"></i>
        </button>
      </div>
      
      <div class="card-body">
        <div class="recommendations-list">
          <div 
            class="recommendation-item" 
            *ngFor="let recommendation of recommendations; let i = index"
            [class.featured]="i === 0">
            
            <!-- Recommendation Header -->
            <div class="recommendation-header">
              <div class="recommendation-type">
                <i class="type-icon" [class]="getTypeIcon(recommendation.type)"></i>
                <span class="type-label">{{ getTypeLabel(recommendation.type) }}</span>
              </div>
              <div class="difficulty-badge" [class]="'difficulty-' + recommendation.difficulty.toLowerCase()">
                {{ recommendation.difficulty }}
              </div>
            </div>

            <!-- Recommendation Content -->
            <div class="recommendation-content">
              <h4 class="recommendation-title">{{ recommendation.title }}</h4>
              <p class="recommendation-description">{{ recommendation.description }}</p>
              
              <!-- Meta Info -->
              <div class="recommendation-meta">
                <div class="meta-item">
                  <i class="bi bi-clock"></i>
                  <span>{{ recommendation.estimatedTime }} dk</span>
                </div>
                <div class="meta-item">
                  <i class="bi bi-lightbulb"></i>
                  <span>AI Önerisi</span>
                </div>
              </div>

              <!-- Reason -->
              <div class="recommendation-reason">
                <i class="bi bi-info-circle"></i>
                <span>{{ recommendation.reason }}</span>
              </div>

              <!-- Actions -->
              <div class="recommendation-actions">
                <button 
                  class="btn-primary start-btn" 
                  (click)="startRecommendation(recommendation)">
                  <i class="bi bi-play-fill"></i>
                  Başla
                </button>
                <button 
                  class="btn-secondary save-btn" 
                  (click)="saveForLater(recommendation)">
                  <i class="bi bi-bookmark"></i>
                  Sonraya Sakla
                </button>
              </div>
            </div>

            <!-- Featured badge for first recommendation -->
            <div class="featured-badge" *ngIf="i === 0">
              <i class="bi bi-star-fill"></i>
              <span>Önerilenler</span>
            </div>
          </div>
        </div>

        <!-- No recommendations state -->
        <div class="no-recommendations" *ngIf="recommendations.length === 0">
          <i class="bi bi-cpu"></i>
          <h4>AI Analiz Ediyor</h4>
          <p>Performansınız analiz ediliyor, kısa süre sonra öneriler hazır olacak.</p>
          <button class="btn-outline" (click)="refreshRecommendations()">
            <i class="bi bi-arrow-clockwise"></i>
            Yenile
          </button>
        </div>

        <!-- AI Insight Box -->
        <div class="ai-insight-box" *ngIf="recommendations.length > 0">
          <div class="insight-header">
            <i class="bi bi-lightbulb-fill"></i>
            <span>AI İçgörüsü</span>
          </div>
          <div class="insight-content">
            <p>{{ getAiInsight() }}</p>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./ai-recommendations.component.scss']
})
export class AiRecommendationsComponent {
  @Input() recommendations: AiRecommendation[] = [];
  isRefreshing = false;
  private currentInsight = ''; // Cache the insight

  constructor(private router: Router) {}

  getTypeIcon(type: string): string {
    const icons = {
      'exercise': 'bi-puzzle-fill',
      'text': 'bi-file-text-fill',
      'technique': 'bi-gear-fill'
    };
    return icons[type as keyof typeof icons] || 'bi-circle-fill';
  }

  getTypeLabel(type: string): string {
    const labels = {
      'exercise': 'Egzersiz',
      'text': 'Metin',
      'technique': 'Teknik'
    };
    return labels[type as keyof typeof labels] || type;
  }

  startRecommendation(recommendation: AiRecommendation): void {
    // Analytics tracking
    this.trackRecommendationClick(recommendation, 'start');
    
    // Navigate based on recommendation type
    switch (recommendation.type) {
      case 'exercise':
        this.router.navigate(['/exercises', 'recommended']);
        break;
      case 'text':
        this.router.navigate(['/reading', 'recommended']);
        break;
      case 'technique':
        this.router.navigate(['/techniques', recommendation.title.toLowerCase().replace(/\s+/g, '-')]);
        break;
    }
  }

  saveForLater(recommendation: AiRecommendation): void {
    this.trackRecommendationClick(recommendation, 'save');
    
    // Simulate saving to user's bookmark list
    const savedItems = JSON.parse(localStorage.getItem('savedRecommendations') || '[]');
    savedItems.push({
      ...recommendation,
      savedAt: new Date().toISOString()
    });
    localStorage.setItem('savedRecommendations', JSON.stringify(savedItems));
    
    // You could show a toast notification here
    console.log('Recommendation saved for later:', recommendation.title);
  }

  refreshRecommendations(): void {
    this.isRefreshing = true;
    
    // Reset cached insight when refreshing
    this.currentInsight = '';
    
    // Simulate API call
    setTimeout(() => {
      this.isRefreshing = false;
      // In real app, this would trigger a new API call to get fresh recommendations
      console.log('Recommendations refreshed');
    }, 1500);
  }

  getAiInsight(): string {
    if (this.recommendations.length === 0) return '';
    
    // Only generate new insight if we don't have one cached
    if (!this.currentInsight) {
      const insights = [
        'Göz koordinasyonu egzersizleri okuma hızınızı %12 artırabilir.',
        'Son performansınıza göre bilim metinlerinde daha başarılısınız.',
        'Chunking tekniği ile okuma hızınızı artırma potansiyeliniz var.',
        'Düzenli egzersiz ile 2 hafta içinde bir sonraki seviyeye çıkabilirsiniz.',
        'Anlama oranınız mükemmel, şimdi hız odaklı çalışmanın zamanı.'
      ];
      
      // Cache the insight to prevent constant changes
      this.currentInsight = insights[Math.floor(Math.random() * insights.length)];
    }
    
    return this.currentInsight;
  }

  private trackRecommendationClick(recommendation: AiRecommendation, action: 'start' | 'save'): void {
    // Analytics tracking - in real app this would send to analytics service
    const event = {
      type: 'ai_recommendation_action',
      action: action,
      recommendationType: recommendation.type,
      recommendationTitle: recommendation.title,
      difficulty: recommendation.difficulty,
      estimatedTime: recommendation.estimatedTime,
      timestamp: new Date().toISOString()
    };
    
    console.log('Analytics event:', event);
    
    // Store in localStorage for demo purposes
    const events = JSON.parse(localStorage.getItem('analyticsEvents') || '[]');
    events.push(event);
    localStorage.setItem('analyticsEvents', JSON.stringify(events.slice(-100))); // Keep last 100 events
  }
}