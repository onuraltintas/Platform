import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { SrContentApiService, TextDto, LevelDto } from '../../data-access/sr-content-api.service';
// Excel/PDF kütüphaneleri dinamik import ile lazy yüklenir

@Component({
  standalone: true,
  selector: 'app-sr-texts-list',
  template: `
  <div class="d-flex align-items-center mb-2 gap-2">
    <input class="form-control" placeholder="Ara" [(ngModel)]="query" (ngModelChange)="reloadFirstPage()" />
    <select class="form-select w-auto" [(ngModel)]="selectedLevelId" (ngModelChange)="reloadFirstPage()">
      <option value="">Seviye (tümü)</option>
      <option *ngFor="let l of levels" [value]="l.levelId">{{l.levelName}}</option>
    </select>
    <select class="form-select w-auto" [(ngModel)]="selectedDifficulty" (ngModelChange)="reloadFirstPage()">
      <option value="">Zorluk (tümü)</option>
      <option>Temel</option>
      <option>Orta</option>
      <option>İleri</option>
      <option>Uzman</option>
    </select>
    <div class="ms-auto d-flex align-items-center gap-2">
      <button class="btn btn-outline-secondary" type="button" (click)="clearFilters()">Temizle</button>
      <button class="btn btn-outline-success" type="button" (click)="exportExcel()"><i class="bi bi-file-earmark-excel"></i> Excel</button>
      <button class="btn btn-outline-danger" type="button" (click)="exportPdf()"><i class="bi bi-file-earmark-pdf"></i> PDF</button>
    </div>
  </div>
  <div class="mb-3">
    <button class="btn btn-primary btn-lg px-4" (click)="create()"><i class="bi bi-plus-lg"></i> Yeni Metin</button>
  </div>
  <div class="table-responsive">
    <table class="table table-striped">
      <thead>
        <tr>
          <th>Başlık</th>
          <th>Zorluk</th>
          <th>Seviye</th>
          <th>Güncel</th>
          <th>İşlemler</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let t of texts()">
          <td>{{t.title}}</td>
          <td>{{t.difficultyLevel}}</td>
          <td>{{t.levelName || '-'}}</td>
          <td>{{t.updatedAt | date:'short'}}</td>
          <td class="text-end text-nowrap">
            <button class="btn btn-sm btn-outline-secondary me-2" (click)="edit(t)" title="Düzenle"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-sm btn-outline-danger" (click)="remove(t)" title="Sil"><i class="bi bi-trash"></i></button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
  <div class="d-flex align-items-center justify-content-between mt-2">
    <div class="d-flex align-items-center gap-2 text-muted small">
      <span>Toplam: {{total}}</span>
      <span>| Gösterilen: {{(texts().length || 0) > 0 ? ((page-1)*pageSize+1) : 0}}–{{ min(page*pageSize, total) }} / {{total}}</span>
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
export class SrTextsListComponent {
  private api = inject(SrContentApiService);
  private router = inject(Router);

  query = '';
  texts = signal<TextDto[]>([]);
  levels: LevelDto[] = [];
  selectedLevelId: string = '';
  selectedDifficulty: string = '';
  page = 1;
  pageSize = 10;
  total = 0;

  ngOnInit() { this.api.listLevels().subscribe(list => this.levels = (list as any).items ?? list); this.load(); }

  load() {
    this.api.listTexts({ page: this.page, pageSize: this.pageSize, search: this.query || undefined, level: this.selectedLevelId || undefined, difficultyLevel: this.selectedDifficulty || undefined }).subscribe(r => {
      const items = (r as any).items ?? (r as any).data ?? r?.items ?? [];
      const total = (r as any).total ?? (r as any).count ?? r?.total ?? items.length;
      this.texts.set(items || []);
      this.total = total || 0;
    });
  }

  reloadFirstPage() { this.page = 1; this.load(); }
  first() { if (this.page === 1) return; this.page = 1; this.load(); }
  prev() { if (this.page === 1) return; this.page--; this.load(); }
  next() { const last = this.max(1, this.ceil(this.total / this.pageSize)); if (this.page >= last) return; this.page++; this.load(); }
  last() { this.page = this.max(1, this.ceil(this.total / this.pageSize)); this.load(); }

  // Template yardımcıları
  min(a: number, b: number) { return a < b ? a : b; }
  max(a: number, b: number) { return a > b ? a : b; }
  ceil(n: number) { return Math.ceil(n); }

  clearFilters() {
    this.query = '';
    this.selectedLevelId = '';
    this.selectedDifficulty = '';
    this.reloadFirstPage();
  }

  async exportExcel() {
    const XLSX = await import('xlsx');
    const data = this.texts().map(r => ({
      TextId: r.textId,
      Başlık: r.title,
      Zorluk: r.difficultyLevel,
      Seviye: r.levelName ?? ''
    }));
    const ws = XLSX.utils.json_to_sheet(data as any);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Metinler');
    XLSX.writeFile(wb, 'metinler.xlsx');
  }

  async exportPdf() {
    const pdfMake = (await import('pdfmake/build/pdfmake')).default as any;
    const pdfFonts = await import('pdfmake/build/vfs_fonts');
    pdfMake.vfs = (pdfFonts as any).vfs || (pdfFonts as any).pdfMake?.vfs;
    const body = [ ['TextId','Başlık','Zorluk','Seviye'], ...this.texts().map(r => [r.textId, r.title, r.difficultyLevel, r.levelName ?? '']) ];
    const docDefinition: any = {
      pageOrientation: 'landscape',
      content: [ { text: 'Metinler', style: 'header' }, { table: { headerRows: 1, body } } ],
      styles: { header: { fontSize: 14, bold: true, margin: [0,0,0,8] } }
    };
    pdfMake.createPdf(docDefinition).download('metinler.pdf');
  }

  create() { this.router.navigate(['/sr/texts/new']); }
  edit(t: TextDto) { this.router.navigate(['/sr/texts', t.textId]); }
  remove(t: TextDto) {
    if (!confirm('Silmek istediğinize emin misiniz?')) return;
    this.api.deleteText(t.textId).subscribe(() => this.load());
  }
}

