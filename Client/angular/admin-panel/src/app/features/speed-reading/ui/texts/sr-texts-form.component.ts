import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { SrContentApiService, UpsertTextRequest, LevelDto } from '../../data-access/sr-content-api.service';

@Component({
  standalone: true,
  selector: 'app-sr-texts-form',
  template: `
  <form class="vstack gap-3" (ngSubmit)="save()">
    <div class="d-flex justify-content-between align-items-center">
      <h5 class="m-0">Metin</h5>
      <div class="hstack gap-2">
        <button class="btn btn-secondary" type="button" (click)="back()">Geri</button>
        <button class="btn btn-primary" type="submit">Kaydet</button>
      </div>
    </div>
    <div class="row g-3">
      <div class="col-12">
        <label class="form-label">Başlık</label>
        <input class="form-control" [(ngModel)]="model.title" name="title" required />
      </div>
      <div class="col-md-6">
        <label class="form-label">Zorluk</label>
        <select class="form-select" [(ngModel)]="model.difficultyLevel" name="difficultyLevel" required>
          <option value="">Seçiniz</option>
          <option>Temel</option>
          <option>Orta</option>
          <option>İleri</option>
          <option>Uzman</option>
        </select>
      </div>
      <div class="col-md-6">
        <label class="form-label">Seviye</label>
        <select class="form-select" [(ngModel)]="model.levelId" name="levelId">
          <option value="">Seçiniz</option>
          <option *ngFor="let l of levels" [value]="l.levelId">{{l.levelName}}</option>
        </select>
      </div>
      <div class="col-12">
        <label class="form-label">İçerik</label>
        <textarea class="form-control" rows="12" [(ngModel)]="model.content" name="content" required></textarea>
      </div>
    </div>
  </form>
  `,
  imports: [CommonModule, FormsModule, RouterModule]
})
export class SrTextsFormComponent {
  private api = inject(SrContentApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  id: string | null = null;
  model: UpsertTextRequest = { title: '', content: '', difficultyLevel: '' };
  levels: LevelDto[] = [];

  ngOnInit() {
    this.api.listLevels().subscribe(list => this.levels = (list as any).items ?? list);
    this.route.paramMap.subscribe(p => {
      this.id = p.get('id');
      if (this.id) {
        this.api.getText(this.id).subscribe(r => {
          const data: any = (r as any).data ?? r;
          this.model = {
            title: data.title,
            content: (data as any).content ?? '',
            difficultyLevel: data.difficultyLevel,
            levelId: data.levelId,
            tagsJson: (data as any).tagsJson
          };
        });
      }
    });
  }

  back() { this.router.navigate(['/sr/texts']); }

  save() {
    if (!this.model.title || !this.model.content || !this.model.difficultyLevel) return;
    const req = this.model;
    const obs = this.id ? this.api.updateText(this.id, req) : this.api.createText(req);
    obs.subscribe(() => this.back());
  }
}

