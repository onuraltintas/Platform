import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { SrContentApiService, LevelDto } from '../../data-access/sr-content-api.service';

@Component({
  standalone: true,
  selector: 'app-sr-levels-form',
  template: `
  <form class="vstack gap-3" (ngSubmit)="save()">
    <div class="d-flex justify-content-between align-items-center">
      <h5 class="m-0">Seviye</h5>
      <div class="hstack gap-2">
        <button class="btn btn-secondary" type="button" (click)="back()">Geri</button>
        <button class="btn btn-primary" type="submit">Kaydet</button>
      </div>
    </div>
    <div class="row g-3">
      <div class="col-md-6">
        <label class="form-label">Ad</label>
        <input class="form-control" [(ngModel)]="model.levelName" name="levelName" required />
      </div>
      <div class="col-md-3">
        <label class="form-label">Min Yaş</label>
        <input class="form-control" type="number" [(ngModel)]="model.minAge" name="minAge" />
      </div>
      <div class="col-md-3">
        <label class="form-label">Max Yaş</label>
        <input class="form-control" type="number" [(ngModel)]="model.maxAge" name="maxAge" />
      </div>
      <div class="col-md-3">
        <label class="form-label">Min WPM</label>
        <input class="form-control" type="number" [(ngModel)]="model.minWPM" name="minWPM" />
      </div>
      <div class="col-md-3">
        <label class="form-label">Max WPM</label>
        <input class="form-control" type="number" [(ngModel)]="model.maxWPM" name="maxWPM" />
      </div>
      <div class="col-md-3">
        <label class="form-label">Anlama Hedefi %</label>
        <input class="form-control" type="number" [(ngModel)]="model.targetComprehension" name="targetComprehension" />
      </div>
    </div>
  </form>
  `,
  imports: [CommonModule, FormsModule, RouterModule]
})
export class SrLevelsFormComponent {
  private api = inject(SrContentApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  id: string | null = null;
  model: Omit<LevelDto, 'levelId'> = { levelName: '' } as any;
  ngOnInit() {
    this.route.paramMap.subscribe(p => {
      this.id = p.get('id');
      if (!this.id) return;
      this.api.getLevel(this.id).subscribe(r => {
        const d: any = (r as any).data ?? r;
        this.model = { levelName: d.levelName, minAge: d.minAge, maxAge: d.maxAge, minWPM: d.minWPM, maxWPM: d.maxWPM, targetComprehension: d.targetComprehension } as any;
      });
    });
  }
  back() { this.router.navigate(['/sr/levels']); }
  save() {
    const obs = this.id ? this.api.updateLevel(this.id!, this.model) : this.api.createLevel(this.model);
    obs.subscribe(() => this.back());
  }
}

