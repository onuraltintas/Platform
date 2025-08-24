import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ComprehensionQuestion, ComprehensionTest, QuestionType } from '../../../../shared/models/reading.models';

@Component({
  standalone: true,
  selector: 'app-comprehension-test',
  imports: [CommonModule, FormsModule],
  template: `
  <div class="ct-overlay">
    <div class="ct-modal">
      <h3 class="mb-3">Anlama Testi</h3>
      <p class="text-muted mb-3">Okuduğunuz metinle ilgili soruları cevaplayın. İsterseniz atlayabilirsiniz.</p>

      <form (ngSubmit)="submit()">
        <div class="mb-3" *ngFor="let q of questions; let i = index">
          <div class="fw-semibold mb-1">{{ i + 1 }}. {{ q.question }}</div>

          <ng-container [ngSwitch]="q.type">
            <div *ngSwitchCase="questionType.MULTIPLE_CHOICE">
              <div class="form-check" *ngFor="let opt of q.options; let oi = index">
                <input class="form-check-input" type="radio" [name]="'q_' + i" [value]="oi" [(ngModel)]="answers[i]">
                <label class="form-check-label">{{ opt }}</label>
              </div>
            </div>

            <div *ngSwitchCase="questionType.TRUE_FALSE" class="d-flex gap-3">
              <div class="form-check">
                <input class="form-check-input" type="radio" [name]="'q_' + i" value="true" [(ngModel)]="answers[i]">
                <label class="form-check-label">Doğru</label>
              </div>
              <div class="form-check">
                <input class="form-check-input" type="radio" [name]="'q_' + i" value="false" [(ngModel)]="answers[i]">
                <label class="form-check-label">Yanlış</label>
              </div>
            </div>

            <div *ngSwitchDefault>
              <input class="form-control" placeholder="Kısa cevap" [(ngModel)]="answers[i]" [name]="'q_' + i" />
            </div>
          </ng-container>
        </div>

        <div class="d-flex justify-content-between mt-4">
          <button type="button" class="btn btn-outline-secondary" (click)="skip()">Atla</button>
          <button type="submit" class="btn btn-primary">Bitir</button>
        </div>
      </form>
    </div>
  </div>
  `,
  styles: [`
  .ct-overlay{position:fixed;inset:0;background:rgba(0,0,0,.35);display:flex;align-items:center;justify-content:center;z-index:9999}
  .ct-modal{background:#fff;border-radius:12px;max-width:720px;width:92%;padding:24px;border:1px solid #e5e7eb;box-shadow:0 10px 30px rgba(0,0,0,.12)}
  `]
})
export class ComprehensionTestComponent {
  @Input() textId!: string;
  @Input() questions: ComprehensionQuestion[] = [];
  @Output() completed = new EventEmitter<ComprehensionTest>();
  @Output() skipped = new EventEmitter<void>();

  answers: any[] = [];
  questionType = QuestionType;

  submit(): void {
    const total = this.questions.length;
    let correct = 0;
    this.questions.forEach((q, i) => {
      const a = this.answers[i];
      if (q.type === QuestionType.MULTIPLE_CHOICE) {
        if (String(a) === String(q.correctAnswer)) correct++;
      } else if (q.type === QuestionType.TRUE_FALSE) {
        if (String(a).toLowerCase() === String(q.correctAnswer).toLowerCase()) correct++;
      } else {
        // serbest cevaplar skor dışı bırakılabilir (0 etkisi)
      }
    });

    const score = total > 0 ? Math.round((correct / total) * 100) : 0;
    const test: ComprehensionTest = {
      testId: 'ct_' + (this.textId || 'unknown'),
      textId: this.textId,
      questions: this.questions,
      score,
      answeredQuestions: this.answers.filter(x => x !== undefined && x !== null && x !== '').length,
      correctAnswers: correct,
      comprehensionRate: score,
      completedAt: new Date()
    };
    this.completed.emit(test);
  }

  skip(): void {
    this.skipped.emit();
  }
}

