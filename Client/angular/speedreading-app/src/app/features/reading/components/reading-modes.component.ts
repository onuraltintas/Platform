import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { ReadingMode } from '../../../shared/models/reading.models';

@Component({
  selector: 'app-reading-modes',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="reading-modes-page">
      <div class="page-header">
        <h1>Hızlı Okuma Modları</h1>
        <p>Okuma becerilerinizi geliştirmek için uygun modu seçin</p>
      </div>

      <div class="modes-grid">
        <div 
          class="mode-card" 
          *ngFor="let mode of availableModes"
          (click)="selectMode(mode.id)">
          
          <div class="card-header">
            <div class="mode-icon">
              <i class="bi" [class]="mode.icon"></i>
            </div>
            <h3>{{ mode.name }}</h3>
          </div>

          <div class="card-body">
            <p class="mode-description">{{ mode.description }}</p>
            
            <div class="features-list">
              <h4>Özellikler:</h4>
              <ul>
                <li *ngFor="let feature of mode.features">{{ feature }}</li>
              </ul>
            </div>

            <div class="difficulty-level">
              <span class="difficulty-label">Zorluk:</span>
              <div class="difficulty-stars">
                <i 
                  class="bi bi-star-fill" 
                  *ngFor="let i of getDifficultyStars(mode.difficulty)"
                  [class.active]="true">
                </i>
                <i 
                  class="bi bi-star" 
                  *ngFor="let i of getEmptyStars(mode.difficulty)"
                  [class.empty]="true">
                </i>
              </div>
            </div>
          </div>

          <div class="card-footer">
            <button class="btn-select">
              <span>Seç ve Devam Et</span>
              <i class="bi bi-arrow-right"></i>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./reading-modes.component.scss']
})
export class ReadingModesComponent implements OnInit {
  
  availableModes = [
    {
      id: ReadingMode.CLASSIC,
      name: 'Klasik Okuma',
      description: 'Geleneksel okuma deneyimi ile hızınızı doğal olarak artırın. Göz koordinasyonunuzu geliştirin ve okuma alışkanlıklarınızı iyileştirin.',
      icon: 'bi-book-open',
      difficulty: 1,
      features: [
        'Doğal okuma deneyimi',
        'İlerleme takibi',
        'Vurgu sistemi',
        'Göz hareketi analizi',
        'Hız ayarlaması'
      ]
    },
    {
      id: ReadingMode.RSVP,
      name: 'RSVP Okuma',
      description: 'Rapid Serial Visual Presentation tekniği ile kelimeleri tek tek merkezi noktada görerek okuma hızınızı maksimize edin.',
      icon: 'bi-eye',
      difficulty: 3,
      features: [
        'Merkezi odak noktası',
        'Göz hareketi eliminasyonu',
        'Hız kontrolü',
        'Kelime vurgusu',
        'Ritim ayarlaması'
      ]
    },
    {
      id: ReadingMode.CHUNK,
      name: 'Grup Okuma',
      description: 'Kelimeleri 2-5\'li gruplar halinde görerek çevresel görüş kapasitesini geliştirin ve okuma hızınızı artırın.',
      icon: 'bi-layers',
      difficulty: 2,
      features: [
        'Kelime grupları',
        'Çevresel görüş geliştirme',
        'Grup boyutu ayarı',
        'Ritim kontrolü',
        'Anlama odaklı okuma'
      ]
    },
    {
      id: ReadingMode.GUIDED,
      name: 'Rehberli Okuma',
      description: 'Hareket eden vurgu çubuğu ile rehberli okuma yaparak dikkat dağınıklığını önleyin ve odaklanmanızı artırın.',
      icon: 'bi-arrow-right-square',
      difficulty: 2,
      features: [
        'Hareketli vurgu çubuğu',
        'Fokus penceresi',
        'Dikkat geliştirme',
        'Yumuşak takip',
        'Hız rehberi'
      ]
    }
  ];

  constructor(private router: Router, private route: ActivatedRoute) {}

  ngOnInit(): void {
    // Check if we should start reading immediately
    const queryParams = this.route.snapshot.queryParamMap;
    const shouldStart = queryParams.get('start');
    const textId = queryParams.get('textId');
    const mode = queryParams.get('mode');
    
    if (shouldStart === 'true' && textId && mode) {
      // Navigate directly to interface with the provided parameters
      this.router.navigate(['/reading/interface'], {
        queryParams: {
          textId: textId,
          mode: mode
        }
      });
    }
  }

  selectMode(modeId: ReadingMode): void {
    // Seçili mod ile ayarlar sayfasına git
    this.router.navigate(['/reading/settings'], { 
      queryParams: { mode: modeId } 
    });
  }

  getDifficultyStars(difficulty: number): number[] {
    return Array(difficulty).fill(0);
  }

  getEmptyStars(difficulty: number): number[] {
    return Array(Math.max(0, 3 - difficulty)).fill(0);
  }
}