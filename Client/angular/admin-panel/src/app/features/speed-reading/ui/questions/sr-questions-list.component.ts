import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { SrContentApiService, QuestionDto, LevelDto } from '../../data-access/sr-content-api.service';
// Excel/PDF kütüphaneleri dinamik import ile lazy yüklenir

@Component({
  standalone: true,
  selector: 'app-sr-questions-list',
  template: `
  <div class="d-flex align-items-center mb-2 gap-2">
    <input class="form-control" placeholder="Metin Id ile filtrele" [(ngModel)]="textId" />
    <select class="form-select w-auto" [(ngModel)]="selectedLevelId" (ngModelChange)="reloadFirstPage()">
      <option value="">Seviye (tümü)</option>
      <option *ngFor="let l of levels" [value]="l.levelId">{{l.levelName}}</option>
    </select>
    <select class="form-select w-auto" [(ngModel)]="selectedType" (ngModelChange)="reloadFirstPage()">
      <option value="">Tür (tümü)</option>
      <option>MultipleChoice</option>
      <option>TrueFalse</option>
      <option>OpenEnded</option>
    </select>
    <div class="ms-auto d-flex align-items-center gap-2">
      <button class="btn btn-outline-secondary" (click)="clearFilters()">Temizle</button>
      <button class="btn btn-outline-success" type="button" (click)="exportExcel()"><i class="bi bi-file-earmark-excel"></i> Excel</button>
      <button class="btn btn-outline-danger" type="button" (click)="exportPdf()"><i class="bi bi-file-earmark-pdf"></i> PDF</button>
    </div>
  </div>
  <div class="mb-3">
    <button class="btn btn-primary btn-lg px-4" (click)="create()"><i class="bi bi-plus-lg"></i> Yeni Soru</button>
  </div>
  <div class="table-responsive">
    <table class="table table-striped">
      <thead><tr><th>Soru</th><th>Tür</th><th>Seviye</th><th>İşlemler</th></tr></thead>
      <tbody>
        <tr *ngFor="let q of items()">
          <td>{{q.questionText}}</td>
          <td>{{q.questionType}}</td>
          <td>{{q.levelId || '-'}}</td>
          <td class="text-end text-nowrap">
            <button class="btn btn-sm btn-outline-secondary me-2" (click)="edit(q)" title="Düzenle"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-sm btn-outline-danger" (click)="remove(q)" title="Sil"><i class="bi bi-trash"></i></button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
  <div class="d-flex align-items-center justify-content-between mt-2">
    <div class="d-flex align-items-center gap-2 text-muted small">
      <span>Toplam: {{total}}</span>
      <span>| Gösterilen: {{(items().length || 0) > 0 ? ((page-1)*pageSize+1) : 0}}–{{ min(page*pageSize, total) }} / {{total}}</span>
      <label class="ms-3 me-1">Sayfa boyutu</label>
      <select class="form-select form-select-sm" style="width: auto" [(ngModel)]="pageSize" (ngModelChange)="reloadFirstPage()">
        <option [value]="10">10</option>
        <option [value]="20">20</option>
        <option [value]="50">50</option>
      </select>
    </div>
    <div class="btn-group">
      <button class="btn btn-sm btn-outline-secondary" (click)="first()" [disabled]="page===1">İlk</button>
      <button class="btn btn-sm btn-outline-secondary" (click)="prev()" [disabled]="page===1">Önceki</button>
      <button class="btn btn-sm btn-primary disabled">{{page}}</button>
      <button class="btn btn-sm btn-outline-secondary" (click)="next()" [disabled]="page>=max(1, ceil(total/pageSize))">Sonraki</button>
      <button class="btn btn-sm btn-outline-secondary" (click)="last()" [disabled]="page>=max(1, ceil(total/pageSize))">Son</button>
    </div>
  </div>
  `,
  imports: [CommonModule, FormsModule, RouterModule]
})
export class SrQuestionsListComponent {
  private api = inject(SrContentApiService);
  private router = inject(Router);
  textId = '';
  items = signal<QuestionDto[]>([]);
  levels: LevelDto[] = [];
  selectedLevelId: string = '';
  selectedType: string = '';
  page = 1; pageSize = 10; total = 0;
  ngOnInit() { this.api.listLevels().subscribe(list => this.levels = (list as any).items ?? list); this.load(); }
  load() { this.api.listQuestions({ textId: this.textId || undefined, page: this.page, pageSize: this.pageSize, level: this.selectedLevelId || undefined, questionType: this.selectedType || undefined } as any).subscribe(r => { const data = (r as any).items ?? (r as any).data ?? r?.items ?? []; const total = (r as any).total ?? (r as any).count ?? r?.total ?? data.length; this.items.set(data || []); this.total = total || 0; }); }
  reloadFirstPage() { this.page = 1; this.load(); }
  first() { if (this.page === 1) return; this.page = 1; this.load(); }
  prev() { if (this.page === 1) return; this.page--; this.load(); }
  next() { const last = this.max(1, this.ceil(this.total / this.pageSize)); if (this.page >= last) return; this.page++; this.load(); }
  last() { this.page = this.max(1, this.ceil(this.total / this.pageSize)); this.load(); }
  min(a: number, b: number) { return a < b ? a : b; }
  max(a: number, b: number) { return a > b ? a : b; }
  ceil(n: number) { return Math.ceil(n); }
  // Export & clear (opsiyonel)
  clearFilters() { this.textId = ''; this.selectedLevelId = ''; this.selectedType = ''; this.reloadFirstPage(); }
  async exportExcel() {
    const XLSX = await import('xlsx');
    const data = this.items().map(r => ({ QuestionId: r.questionId, TextId: r.textId, Type: r.questionType ?? '', LevelId: r.levelId ?? '' }));
    const ws = XLSX.utils.json_to_sheet(data as any); const wb = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(wb, ws, 'Sorular'); XLSX.writeFile(wb, 'sorular.xlsx');
  }
  async exportPdf() {
    const pdfMake = (await import('pdfmake/build/pdfmake')).default as any;
    const pdfFonts = await import('pdfmake/build/vfs_fonts');
    pdfMake.vfs = (pdfFonts as any).vfs || (pdfFonts as any).pdfMake?.vfs;
    const body = [['QuestionId','TextId','Type','LevelId'], ...this.items().map(r => [r.questionId, r.textId, r.questionType ?? '', r.levelId ?? ''])];
    const docDefinition: any = { pageOrientation: 'landscape', content: [ { text: 'Sorular', style: 'header' }, { table: { headerRows: 1, body } } ], styles: { header: { fontSize: 14, bold: true, margin: [0,0,0,8] } } };
    pdfMake.createPdf(docDefinition).download('sorular.pdf');
  }
  create() { this.router.navigate(['/sr/questions/new']); }
  edit(q: QuestionDto) { this.router.navigate(['/sr/questions', q.questionId]); }
  remove(q: QuestionDto) { if (!confirm('Silinsin mi?')) return; this.api.deleteQuestion(q.questionId).subscribe(() => this.load()); }
}

