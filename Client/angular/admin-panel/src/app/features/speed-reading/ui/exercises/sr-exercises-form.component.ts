import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { SrContentApiService, UpsertExerciseRequest, ExerciseTypeDto, LevelDto } from '../../data-access/sr-content-api.service';

@Component({
  standalone: true,
  selector: 'app-sr-exercises-form',
  template: `
  <form class="vstack gap-3" (ngSubmit)="save()">
    <div class="d-flex justify-content-between align-items-center">
      <h5 class="m-0">Egzersiz</h5>
      <div class="hstack gap-2">
        <button class="btn btn-secondary" type="button" (click)="back()">Geri</button>
        <button class="btn btn-primary" type="submit">Kaydet</button>
      </div>
    </div>
    <div class="row g-3">
      <div class="col-md-6">
        <label class="form-label">Tür</label>
        <select class="form-select" [(ngModel)]="model.exerciseTypeId" name="exerciseTypeId" required>
          <option value="">Seçiniz</option>
          <option *ngFor="let t of types" [value]="t.exerciseTypeId">{{t.typeName}}</option>
        </select>
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
        <label class="form-label">Başlık</label>
        <input class="form-control" [(ngModel)]="model.title" name="title" required />
      </div>
      <div class="col-12">
        <label class="form-label">Açıklama</label>
        <textarea class="form-control" rows="4" [(ngModel)]="model.description" name="description"></textarea>
      </div>
      <div class="col-12">
        <label class="form-label">İçerik (JSON)</label>
        <textarea class="form-control" rows="8" [(ngModel)]="model.contentJson" name="contentJson"></textarea>
      </div>
      <div class="col-md-3">
        <label class="form-label">Süre (dk)</label>
        <input class="form-control" type="number" [(ngModel)]="model.durationMinutes" name="durationMinutes" />
      </div>
    </div>
  </form>
  `,
  imports: [CommonModule, FormsModule, RouterModule]
})
export class SrExercisesFormComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(SrContentApiService);
  id: string | null = null;
  types: ExerciseTypeDto[] = [];
  model: UpsertExerciseRequest = { exerciseTypeId: '', title: '', difficultyLevel: '' } as any;
  levels: LevelDto[] = [];
  ngOnInit() {
    this.api.listExerciseTypes().subscribe(list => this.types = (list as any).items ?? list);
    this.api.listLevels().subscribe(list => this.levels = (list as any).items ?? list);
    this.route.paramMap.subscribe(p => {
      this.id = p.get('id');
      if (!this.id) return;
      this.api.getExercise(this.id).subscribe(r => {
        const d: any = (r as any).data ?? r;
        this.model = { exerciseTypeId: d.exerciseTypeId, title: d.title, description: d.description, difficultyLevel: d.difficultyLevel, levelId: d.levelId, contentJson: d.contentJson, durationMinutes: d.durationMinutes } as any;
      });
    });
  }
  back() { this.router.navigate(['/sr/exercises']); }
  save() {
    const body = this.model as UpsertExerciseRequest;
    if (this.id) {
      this.api.updateExercise(this.id, body).subscribe(() => this.back());
    } else {
      this.api.createExercise(body).subscribe(() => this.back());
    }
  }
}

