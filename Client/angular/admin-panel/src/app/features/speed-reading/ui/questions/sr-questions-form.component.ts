import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { SrContentApiService, UpsertQuestionRequest, LevelDto } from '../../data-access/sr-content-api.service';

@Component({
  standalone: true,
  selector: 'app-sr-questions-form',
  template: `
  <form class="vstack gap-3" (ngSubmit)="save()">
    <div class="d-flex justify-content-between align-items-center">
      <h5 class="m-0">Soru</h5>
      <div class="hstack gap-2">
        <button class="btn btn-secondary" type="button" (click)="back()">Geri</button>
        <button class="btn btn-primary" type="submit">Kaydet</button>
      </div>
    </div>
    <div class="row g-3">
      <div class="col-md-6">
        <label class="form-label">Metin ID</label>
        <input class="form-control" [(ngModel)]="model.textId" name="textId" required />
      </div>
      <div class="col-md-6">
        <label class="form-label">Tür</label>
        <select class="form-select" [(ngModel)]="model.questionType" name="questionType">
          <option value="">Seçiniz</option>
          <option>MultipleChoice</option>
          <option>TrueFalse</option>
          <option>OpenEnded</option>
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
        <label class="form-label">Soru</label>
        <textarea class="form-control" rows="6" [(ngModel)]="model.questionText" name="questionText" required></textarea>
      </div>
      <div class="col-12">
        <label class="form-label">Doğru Cevap</label>
        <input class="form-control" [(ngModel)]="model.correctAnswer" name="correctAnswer" />
      </div>
      <div class="col-12">
        <label class="form-label">Seçenekler (JSON)</label>
        <textarea class="form-control" rows="4" [(ngModel)]="model.optionsJson" name="optionsJson"></textarea>
      </div>
    </div>
  </form>
  `,
  imports: [CommonModule, FormsModule, RouterModule]
})
export class SrQuestionsFormComponent {
  private api = inject(SrContentApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  id: string | null = null;
  model: UpsertQuestionRequest = { textId: '', questionText: '' } as any;
  levels: LevelDto[] = [];
  ngOnInit() {
    this.api.listLevels().subscribe(list => this.levels = (list as any).items ?? list);
    this.route.paramMap.subscribe(p => {
      this.id = p.get('id');
      if (!this.id) return;
      this.api.getQuestion(this.id).subscribe(r => {
        const d: any = (r as any).data ?? r;
        this.model = { textId: d.textId, questionText: d.questionText, questionType: d.questionType, correctAnswer: d.correctAnswer, optionsJson: d.optionsJson, levelId: d.levelId };
      });
    });
  }
  back() { this.router.navigate(['/sr/questions']); }
  save() {
    const obs = this.id ? this.api.updateQuestion(this.id!, this.model) : this.api.createQuestion(this.model);
    obs.subscribe(() => this.back());
  }
}

