import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TextProcessingService } from '../../reading/services/text-processing.service';
import { TextContent } from '../../../shared/models/reading.models';

@Component({
  standalone: true,
  selector: 'app-texts-list',
  imports: [CommonModule, FormsModule],
  template: `
  <div class="container py-3">
    <div class="d-flex align-items-center gap-2 mb-3">
      <input class="form-control" placeholder="Metin ara" [(ngModel)]="query" (ngModelChange)="reload()" />
      <select class="form-select w-auto" [(ngModel)]="selectedDifficulty" (ngModelChange)="reload()">
        <option value="">Zorluk (tümü)</option>
        <option value="Temel">Temel</option>
        <option value="Orta">Orta</option>
        <option value="İleri">İleri</option>
        <option value="Uzman">Uzman</option>
      </select>
      <div class="ms-auto">
        <button class="btn btn-outline-secondary" (click)="clearFilters()">Temizle</button>
        <button class="btn btn-success ms-2" (click)="startWithSample()">Örnek metinle başla</button>
      </div>
    </div>

    <div class="row g-3">
      <div class="col-md-4" *ngFor="let t of filteredTexts">
        <div class="card h-100">
          <div class="card-body d-flex flex-column">
            <h5 class="card-title">{{t.title}}</h5>
            <div class="text-muted small mb-2">{{ t.wordCount }} kelime • {{ mapDifficulty(t.difficultyLevel) }}</div>
            <p class="card-text text-truncate" style="max-height: 3.5em; white-space: normal;">
              {{ t.content }}
            </p>
            <div class="mt-auto d-flex gap-2">
              <button class="btn btn-primary" (click)="startReading(t)">Okumaya Başla</button>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="d-flex justify-content-between align-items-center mt-3">
      <div class="text-muted small" *ngIf="!loading">Sayfa {{ page }}</div>
      <div class="btn-group">
        <button class="btn btn-outline-secondary btn-sm" (click)="page = (page > 1 ? page-1 : 1); reload()" [disabled]="page===1">Önceki</button>
        <button class="btn btn-outline-secondary btn-sm" (click)="page = page+1; reload()">Sonraki</button>
      </div>
    </div>
  </div>
  `
})
export class TextsListComponent implements OnInit {
  private readonly textService = inject(TextProcessingService);
  private readonly router = inject(Router);

  texts: TextContent[] = [];
  filteredTexts: TextContent[] = [];
  query: string = '';
  selectedDifficulty: string = '';
  page = 1;
  pageSize = 12;
  loading = false;

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading = true;
    this.textService.getTexts({ search: this.query || undefined, difficultyLevel: this.selectedDifficulty || undefined, page: this.page, pageSize: this.pageSize })
      .subscribe(list => {
        this.texts = list || [];
        this.applyFilters();
        this.loading = false;
      }, () => this.loading = false);
  }

  clearFilters(): void {
    this.query = '';
    this.selectedDifficulty = '';
    this.applyFilters();
  }

  applyFilters(): void {
    const q = (this.query || '').toLowerCase();
    this.filteredTexts = (this.texts || []).filter(t => {
      const matchesQuery = !q || t.title.toLowerCase().includes(q) || t.content.toLowerCase().includes(q);
      const matchesDiff = !this.selectedDifficulty || ('' + t.difficultyLevel).toLowerCase().includes(this.selectedDifficulty.toLowerCase());
      return matchesQuery && matchesDiff;
    });
  }

  mapDifficulty(level: number): string {
    if (level >= 9) return 'Uzman';
    if (level >= 7) return 'İleri';
    if (level >= 4) return 'Orta';
    return 'Temel';
  }

  startReading(text: TextContent): void {
    this.router.navigate(['/reading'], { queryParams: { textId: text.id } });
  }

  startWithSample(): void {
    this.router.navigate(['/reading'], { queryParams: { sample: 1 } });
  }
}

