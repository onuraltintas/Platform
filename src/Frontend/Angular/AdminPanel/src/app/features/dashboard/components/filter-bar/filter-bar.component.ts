import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type DateRange = 'today' | '7d' | '30d' | 'custom';

@Component({
  selector: 'app-filter-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="filter-bar d-flex flex-wrap align-items-center gap-2">
      <!-- Date Range -->
      <div class="btn-group" role="group" aria-label="Tarih Aralığı">
        <button type="button" class="btn btn-outline-primary" [class.active]="range==='today'" (click)="setRange('today')">Bugün</button>
        <button type="button" class="btn btn-outline-primary" [class.active]="range==='7d'" (click)="setRange('7d')">7 Gün</button>
        <button type="button" class="btn btn-outline-primary" [class.active]="range==='30d'" (click)="setRange('30d')">30 Gün</button>
        <button type="button" class="btn btn-outline-primary" [class.active]="range==='custom'" (click)="setRange('custom')">Özel</button>
      </div>

      <!-- Segment -->
      <div class="ms-auto d-flex align-items-center gap-2">
        <label for="segment" class="form-label m-0 text-muted">Segment</label>
        <select id="segment" class="form-select form-select-sm" style="min-width: 160px" [ngModel]="segment" (ngModelChange)="onSegmentChange($event)">
          <option value="all">Tümü</option>
          <option value="free">Ücretsiz</option>
          <option value="pro">Pro</option>
          <option value="enterprise">Enterprise</option>
        </select>
      </div>

      <!-- Custom Range Pickers -->
      <div class="d-flex align-items-center gap-2" *ngIf="range==='custom'">
        <input type="date" class="form-control form-control-sm" [ngModel]="start" (ngModelChange)="setStart($event)"/>
        <span class="text-muted">—</span>
        <input type="date" class="form-control form-control-sm" [ngModel]="end" (ngModelChange)="setEnd($event)"/>
      </div>
    </div>
  `,
  styles: [`
    .filter-bar {
      background: var(--bs-card-bg, #fff);
      border: 1px solid var(--bs-border-color, #e2e8f0);
      border-radius: 0.75rem;
      padding: 0.5rem;
    }

    .btn-group .btn.active {
      color: #fff;
      background-color: var(--primary-color, #3b82f6);
      border-color: var(--primary-hover, #2563eb);
    }
  `]
})
export class FilterBarComponent {
  @Input() range: DateRange = '7d';
  @Input() segment: string = 'all';
  @Input() start?: string;
  @Input() end?: string;

  @Output() rangeChange = new EventEmitter<DateRange>();
  @Output() segmentChange = new EventEmitter<string>();
  @Output() customRangeChange = new EventEmitter<{ start?: string; end?: string }>();

  setRange(r: DateRange): void {
    if (this.range !== r) {
      this.range = r;
      this.rangeChange.emit(r);
    }
  }

  onSegmentChange(s: string): void {
    this.segment = s;
    this.segmentChange.emit(s);
  }

  setStart(v: string): void {
    this.start = v;
    this.customRangeChange.emit({ start: this.start, end: this.end });
  }

  setEnd(v: string): void {
    this.end = v;
    this.customRangeChange.emit({ start: this.start, end: this.end });
  }
}

