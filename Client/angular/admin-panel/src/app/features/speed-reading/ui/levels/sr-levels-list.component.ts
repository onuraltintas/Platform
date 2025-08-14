import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { SrContentApiService, LevelDto } from '../../data-access/sr-content-api.service';
import { FormsModule } from '@angular/forms';
// Excel/PDF kütüphaneleri dinamik import ile lazy yüklenir

@Component({
  standalone: true,
  selector: 'app-sr-levels-list',
  template: `
  <div class="d-flex align-items-center mb-2 gap-2">
    <input class="form-control" placeholder="Seviye ara" [(ngModel)]="query" (ngModelChange)="reloadFirstPage()" />
    <div class="ms-auto d-flex align-items-center gap-2">
      <button class="btn btn-outline-secondary" type="button" (click)="clearFilters()">Temizle</button>
      <button class="btn btn-outline-success" type="button" (click)="exportExcel()"><i class="bi bi-file-earmark-excel"></i> Excel</button>
      <button class="btn btn-outline-danger" type="button" (click)="exportPdf()"><i class="bi bi-file-earmark-pdf"></i> PDF</button>
    </div>
  </div>
  <div class="mb-3">
    <button class="btn btn-primary btn-lg px-4" (click)="create()"><i class="bi bi-plus-lg"></i> Yeni Seviye</button>
  </div>
  <div class="table-responsive">
    <table class="table table-striped">
      <thead><tr><th>Seviye</th><th>Yaş</th><th>WPM</th><th>Anlama</th><th>İşlemler</th></tr></thead>
      <tbody>
        <tr *ngFor="let l of items()">
          <td>{{l.levelName}}</td>
          <td>{{l.minAge}}-{{l.maxAge}}</td>
          <td>{{l.minWPM}}-{{l.maxWPM}}</td>
          <td>{{l.targetComprehension}}</td>
          <td class="text-end text-nowrap">
            <button class="btn btn-sm btn-outline-secondary me-2" (click)="edit(l)" title="Düzenle"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-sm btn-outline-danger" (click)="remove(l)" title="Sil"><i class="bi bi-trash"></i></button>
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
  imports: [CommonModule, RouterModule, FormsModule]
})
export class SrLevelsListComponent {
  private api = inject(SrContentApiService);
  private router = inject(Router);
  private all = signal<LevelDto[]>([]);
  items = signal<LevelDto[]>([]);
  query = '';
  page = 1; pageSize = 10; total = 0;
  ngOnInit() { this.load(); }
  load() { this.api.listLevels().subscribe(r => { const data = (r as any).items ?? r; this.all.set(data || []); this.apply(); }); }
  apply() {
    const q = (this.query || '').toLowerCase();
    const filtered = (this.all() || []).filter(l => !q || (l.levelName || '').toLowerCase().includes(q));
    this.total = filtered.length;
    const start = (this.page - 1) * this.pageSize;
    const pageItems = filtered.slice(start, start + this.pageSize);
    this.items.set(pageItems);
  }
  reloadFirstPage() { this.page = 1; this.apply(); }
  first() { if (this.page === 1) return; this.page = 1; this.apply(); }
  prev() { if (this.page === 1) return; this.page--; this.apply(); }
  next() { const last = this.max(1, this.ceil(this.total / this.pageSize)); if (this.page >= last) return; this.page++; this.apply(); }
  last() { this.page = this.max(1, this.ceil(this.total / this.pageSize)); this.apply(); }
  min(a: number, b: number) { return a < b ? a : b; }
  max(a: number, b: number) { return a > b ? a : b; }
  ceil(n: number) { return Math.ceil(n); }
  clearFilters() { this.query = ''; this.reloadFirstPage(); }
  async exportExcel() {
    const XLSX = await import('xlsx');
    const data = this.items().map(r => ({
      LevelId: r.levelId,
      Seviye: r.levelName,
      Yaş: `${r.minAge ?? ''}-${r.maxAge ?? ''}`,
      WPM: `${r.minWPM ?? ''}-${r.maxWPM ?? ''}`,
      Anlama: r.targetComprehension ?? ''
    }));
    const ws = XLSX.utils.json_to_sheet(data as any); const wb = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(wb, ws, 'Seviyeler'); XLSX.writeFile(wb, 'seviyeler.xlsx');
  }
  async exportPdf() {
    const pdfMake = (await import('pdfmake/build/pdfmake')).default as any;
    const pdfFonts = await import('pdfmake/build/vfs_fonts');
    pdfMake.vfs = (pdfFonts as any).vfs || (pdfFonts as any).pdfMake?.vfs;
    const body = [['LevelId','Seviye','Yaş','WPM','Anlama'], ...this.items().map(r => [r.levelId, r.levelName, `${r.minAge ?? ''}-${r.maxAge ?? ''}`, `${r.minWPM ?? ''}-${r.maxWPM ?? ''}`, r.targetComprehension ?? ''])];
    const docDefinition: any = { pageOrientation: 'landscape', content: [ { text: 'Seviyeler', style: 'header' }, { table: { headerRows: 1, body } } ], styles: { header: { fontSize: 14, bold: true, margin: [0,0,0,8] } } };
    pdfMake.createPdf(docDefinition).download('seviyeler.pdf');
  }
  create() { this.router.navigate(['/sr/levels/new']); }
  edit(l: LevelDto) { this.router.navigate(['/sr/levels', l.levelId]); }
  remove(l: LevelDto) { if (!confirm('Silinsin mi?')) return; this.api.deleteLevel(l.levelId).subscribe(() => this.load()); }
}

