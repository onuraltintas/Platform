import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SrProgressApiService } from '../../data-access/sr-progress-api.service';
import { FormsModule } from '@angular/forms';
// Excel/PDF kütüphaneleri dinamik import ile lazy yüklenir

@Component({
  standalone: true,
  selector: 'app-sr-reports',
  template: `
  <div class="vstack gap-3">
    <h5>Hızlı Okuma Raporları</h5>
    <div class="row g-2 align-items-end">
      <div class="col-md-3"><label class="form-label">Kullanıcı</label><input class="form-control" placeholder="UserId" [(ngModel)]="userId" /></div>
      <div class="col-md-3"><label class="form-label">Metin</label><input class="form-control" placeholder="TextId" [(ngModel)]="textId" /></div>
      <div class="col-md-3"><label class="form-label">Egzersiz</label><input class="form-control" placeholder="ExerciseId" [(ngModel)]="exerciseId" /></div>
      <div class="col-md-1"><label class="form-label">Başlangıç</label><input class="form-control" type="date" [(ngModel)]="dateFrom" /></div>
      <div class="col-md-1"><label class="form-label">Bitiş</label><input class="form-control" type="date" [(ngModel)]="dateTo" /></div>
      <div class="col-md-12 d-flex gap-2 mt-2">
        <button class="btn btn-outline-secondary" type="button" (click)="clearFilters()">Temizle</button>
        <button class="btn btn-primary" type="button" (click)="load()">Filtrele</button>
      </div>
    </div>
    <div class="card">
      <div class="card-header d-flex justify-content-between align-items-center">
        <span>Oturumlar</span>
        <div class="d-flex gap-2">
          <button class="btn btn-sm btn-outline-success" (click)="exportSessionsExcel()"><i class="bi bi-file-earmark-excel"></i> Excel</button>
          <button class="btn btn-sm btn-outline-danger" (click)="exportSessionsPdf()"><i class="bi bi-file-earmark-pdf"></i> PDF</button>
        </div>
      </div>
      <div class="card-body p-0">
        <table class="table mb-0">
          <thead><tr><th>SessionId</th><th>User</th><th>Text</th><th>Start</th><th>End</th><th>WPM</th><th>Comp</th></tr></thead>
          <tbody>
            <tr *ngFor="let s of sessions()"><td>{{s.sessionId}}</td><td>{{s.userId}}</td><td>{{s.textId}}</td><td>{{s.sessionStartDate}}</td><td>{{s.sessionEndDate}}</td><td>{{s.wpm}}</td><td>{{s.comprehensionScore}}</td></tr>
          </tbody>
        </table>
      </div>
      <div class="card-footer d-flex align-items-center justify-content-between">
        <div class="d-flex align-items-center gap-2 text-muted small">
          <span>Toplam: {{sessionsTotal}}</span>
          <label class="ms-3 me-1">Sayfa boyutu</label>
          <select class="form-select form-select-sm" style="width: auto" [(ngModel)]="sessionsPageSize" (ngModelChange)="load()">
            <option [value]="10">10</option>
            <option [value]="20">20</option>
            <option [value]="50">50</option>
          </select>
        </div>
        <div class="btn-group">
          <button class="btn btn-sm btn-outline-secondary" (click)="sessionsFirst()" [disabled]="sessionsPage===1">İlk</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="sessionsPrev()" [disabled]="sessionsPage===1">Önceki</button>
          <button class="btn btn-sm btn-primary disabled">{{sessionsPage}}</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="sessionsNext()">Sonraki</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="sessionsLast()">Son</button>
        </div>
      </div>
    </div>
    <div class="card">
      <div class="card-header d-flex justify-content-between align-items-center">
        <span>Denemeler</span>
        <div class="d-flex gap-2">
          <button class="btn btn-sm btn-outline-success" (click)="exportAttemptsExcel()"><i class="bi bi-file-earmark-excel"></i> Excel</button>
          <button class="btn btn-sm btn-outline-danger" (click)="exportAttemptsPdf()"><i class="bi bi-file-earmark-pdf"></i> PDF</button>
        </div>
      </div>
      <div class="card-body p-0">
        <table class="table mb-0">
          <thead><tr><th>AttemptId</th><th>User</th><th>Exercise</th><th>Date</th><th>Score</th><th>WPM</th></tr></thead>
          <tbody>
            <tr *ngFor="let a of attempts()"><td>{{a.attemptId}}</td><td>{{a.userId}}</td><td>{{a.exerciseId}}</td><td>{{a.attemptDate}}</td><td>{{a.score}}</td><td>{{a.wpm}}</td></tr>
          </tbody>
        </table>
      </div>
      <div class="card-footer d-flex align-items-center justify-content-between">
        <div class="d-flex align-items-center gap-2 text-muted small">
          <span>Toplam: {{attemptsTotal}}</span>
          <label class="ms-3 me-1">Sayfa boyutu</label>
          <select class="form-select form-select-sm" style="width: auto" [(ngModel)]="attemptsPageSize" (ngModelChange)="load()">
            <option [value]="10">10</option>
            <option [value]="20">20</option>
            <option [value]="50">50</option>
          </select>
        </div>
        <div class="btn-group">
          <button class="btn btn-sm btn-outline-secondary" (click)="attemptsFirst()" [disabled]="attemptsPage===1">İlk</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="attemptsPrev()" [disabled]="attemptsPage===1">Önceki</button>
          <button class="btn btn-sm btn-primary disabled">{{attemptsPage}}</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="attemptsNext()">Sonraki</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="attemptsLast()">Son</button>
        </div>
      </div>
    </div>
    <div class="card">
      <div class="card-header d-flex justify-content-between align-items-center">
        <span>Cevaplar</span>
        <div class="d-flex gap-2">
          <button class="btn btn-sm btn-outline-success" (click)="exportResponsesExcel()"><i class="bi bi-file-earmark-excel"></i> Excel</button>
          <button class="btn btn-sm btn-outline-danger" (click)="exportResponsesPdf()"><i class="bi bi-file-earmark-pdf"></i> PDF</button>
        </div>
      </div>
      <div class="card-body p-0">
        <table class="table mb-0">
          <thead><tr><th>ResponseId</th><th>Attempt</th><th>Question</th><th>Correct?</th><th>Time(ms)</th></tr></thead>
          <tbody>
            <tr *ngFor="let r of responses()"><td>{{r.responseId}}</td><td>{{r.attemptId}}</td><td>{{r.questionId}}</td><td>{{r.isCorrect}}</td><td>{{r.responseTimeMs}}</td></tr>
          </tbody>
        </table>
      </div>
      <div class="card-footer d-flex align-items-center justify-content-between">
        <div class="d-flex align-items-center gap-2 text-muted small">
          <span>Toplam: {{responsesTotal}}</span>
          <label class="ms-3 me-1">Sayfa boyutu</label>
          <select class="form-select form-select-sm" style="width: auto" [(ngModel)]="responsesPageSize" (ngModelChange)="load()">
            <option [value]="10">10</option>
            <option [value]="20">20</option>
            <option [value]="50">50</option>
          </select>
        </div>
        <div class="btn-group">
          <button class="btn btn-sm btn-outline-secondary" (click)="responsesFirst()" [disabled]="responsesPage===1">İlk</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="responsesPrev()" [disabled]="responsesPage===1">Önceki</button>
          <button class="btn btn-sm btn-primary disabled">{{responsesPage}}</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="responsesNext()">Sonraki</button>
          <button class="btn btn-sm btn-outline-secondary" (click)="responsesLast()">Son</button>
        </div>
      </div>
    </div>
  </div>
  `,
  imports: [CommonModule, FormsModule]
})
export class SrReportsComponent {
  private api = inject(SrProgressApiService);
  userId = '';
  textId = '';
  exerciseId = '';
  dateFrom = '';
  dateTo = '';
  // sessions
  sessions = signal<any[]>([]);
  sessionsTotal = 0; sessionsPage = 1; sessionsPageSize = 10;
  // attempts
  attempts = signal<any[]>([]);
  attemptsTotal = 0; attemptsPage = 1; attemptsPageSize = 10;
  // responses
  responses = signal<any[]>([]);
  responsesTotal = 0; responsesPage = 1; responsesPageSize = 10;
  // Export helpers (Excel/PDF)
  async exportSessionsExcel() {
    const XLSX = await import('xlsx');
    const data = this.sessions().map(s => ({ SessionId: s.sessionId, User: s.userId, Text: s.textId, Start: s.sessionStartDate, End: s.sessionEndDate, WPM: s.wpm, Comp: s.comprehensionScore }));
    const ws = XLSX.utils.json_to_sheet(data as any); const wb = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(wb, ws, 'Oturumlar'); XLSX.writeFile(wb, 'rapor-oturumlar.xlsx');
  }
  async exportSessionsPdf() {
    const pdfMake = (await import('pdfmake/build/pdfmake')).default as any;
    const pdfFonts = await import('pdfmake/build/vfs_fonts');
    pdfMake.vfs = (pdfFonts as any).vfs || (pdfFonts as any).pdfMake?.vfs;
    const body = [[ 'SessionId','User','Text','Start','End','WPM','Comp' ], ...this.sessions().map(s => [s.sessionId, s.userId, s.textId, s.sessionStartDate, s.sessionEndDate, s.wpm, s.comprehensionScore])];
    const def: any = { pageOrientation: 'landscape', content: [ { text: 'Oturumlar', style: 'header' }, { table: { headerRows: 1, body } } ], styles: { header: { fontSize: 14, bold: true, margin: [0,0,0,8] } } };
    pdfMake.createPdf(def).download('rapor-oturumlar.pdf');
  }
  async exportAttemptsExcel() {
    const XLSX = await import('xlsx');
    const data = this.attempts().map(a => ({ AttemptId: a.attemptId, User: a.userId, Exercise: a.exerciseId, Date: a.attemptDate, Score: a.score, WPM: a.wpm }));
    const ws = XLSX.utils.json_to_sheet(data as any); const wb = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(wb, ws, 'Denemeler'); XLSX.writeFile(wb, 'rapor-denemeler.xlsx');
  }
  async exportAttemptsPdf() {
    const pdfMake = (await import('pdfmake/build/pdfmake')).default as any;
    const pdfFonts = await import('pdfmake/build/vfs_fonts');
    pdfMake.vfs = (pdfFonts as any).vfs || (pdfFonts as any).pdfMake?.vfs;
    const body = [[ 'AttemptId','User','Exercise','Date','Score','WPM' ], ...this.attempts().map(a => [a.attemptId, a.userId, a.exerciseId, a.attemptDate, a.score, a.wpm])];
    const def: any = { pageOrientation: 'landscape', content: [ { text: 'Denemeler', style: 'header' }, { table: { headerRows: 1, body } } ], styles: { header: { fontSize: 14, bold: true, margin: [0,0,0,8] } } };
    pdfMake.createPdf(def).download('rapor-denemeler.pdf');
  }
  async exportResponsesExcel() {
    const XLSX = await import('xlsx');
    const data = this.responses().map(r => ({ ResponseId: r.responseId, Attempt: r.attemptId, Question: r.questionId, Correct: r.isCorrect, TimeMs: r.responseTimeMs }));
    const ws = XLSX.utils.json_to_sheet(data as any); const wb = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(wb, ws, 'Cevaplar'); XLSX.writeFile(wb, 'rapor-cevaplar.xlsx');
  }
  async exportResponsesPdf() {
    const pdfMake = (await import('pdfmake/build/pdfmake')).default as any;
    const pdfFonts = await import('pdfmake/build/vfs_fonts');
    pdfMake.vfs = (pdfFonts as any).vfs || (pdfFonts as any).pdfMake?.vfs;
    const body = [[ 'ResponseId','Attempt','Question','Correct?','Time(ms)' ], ...this.responses().map(r => [r.responseId, r.attemptId, r.questionId, r.isCorrect, r.responseTimeMs])];
    const def: any = { pageOrientation: 'landscape', content: [ { text: 'Cevaplar', style: 'header' }, { table: { headerRows: 1, body } } ], styles: { header: { fontSize: 14, bold: true, margin: [0,0,0,8] } } };
    pdfMake.createPdf(def).download('rapor-cevaplar.pdf');
  }
  load() {
    this.api.listSessions({ userId: this.userId || undefined, textId: this.textId || undefined, dateFrom: this.dateFrom || undefined, dateTo: this.dateTo || undefined, page: this.sessionsPage, pageSize: this.sessionsPageSize }).subscribe(r => { const data = (r as any).items ?? r?.items ?? []; const total = (r as any).total ?? r?.total ?? data.length; this.sessions.set(data); this.sessionsTotal = total; });
    this.api.listAttempts({ userId: this.userId || undefined, exerciseId: this.exerciseId || undefined, dateFrom: this.dateFrom || undefined, dateTo: this.dateTo || undefined, page: this.attemptsPage, pageSize: this.attemptsPageSize }).subscribe(r => { const data = (r as any).items ?? r?.items ?? []; const total = (r as any).total ?? r?.total ?? data.length; this.attempts.set(data); this.attemptsTotal = total; });
    this.api.listResponses({ attemptId: undefined, textId: this.textId || undefined, page: this.responsesPage, pageSize: this.responsesPageSize }).subscribe(r => { const data = (r as any).items ?? r?.items ?? []; const total = (r as any).total ?? r?.total ?? data.length; this.responses.set(data); this.responsesTotal = total; });
  }
  ngOnInit() { this.load(); }
  // pagination helpers
  private ceil(n: number) { return Math.ceil(n); }
  // sessions pager
  sessionsFirst() { if (this.sessionsPage === 1) return; this.sessionsPage = 1; this.load(); }
  sessionsPrev() { if (this.sessionsPage === 1) return; this.sessionsPage--; this.load(); }
  sessionsNext() { const last = Math.max(1, this.ceil(this.sessionsTotal / this.sessionsPageSize)); if (this.sessionsPage >= last) return; this.sessionsPage++; this.load(); }
  sessionsLast() { this.sessionsPage = Math.max(1, this.ceil(this.sessionsTotal / this.sessionsPageSize)); this.load(); }
  // attempts pager
  attemptsFirst() { if (this.attemptsPage === 1) return; this.attemptsPage = 1; this.load(); }
  attemptsPrev() { if (this.attemptsPage === 1) return; this.attemptsPage--; this.load(); }
  attemptsNext() { const last = Math.max(1, this.ceil(this.attemptsTotal / this.attemptsPageSize)); if (this.attemptsPage >= last) return; this.attemptsPage++; this.load(); }
  attemptsLast() { this.attemptsPage = Math.max(1, this.ceil(this.attemptsTotal / this.attemptsPageSize)); this.load(); }
  // responses pager
  responsesFirst() { if (this.responsesPage === 1) return; this.responsesPage = 1; this.load(); }
  responsesPrev() { if (this.responsesPage === 1) return; this.responsesPage--; this.load(); }
  responsesNext() { const last = Math.max(1, this.ceil(this.responsesTotal / this.responsesPageSize)); if (this.responsesPage >= last) return; this.responsesPage++; this.load(); }
  responsesLast() { this.responsesPage = Math.max(1, this.ceil(this.responsesTotal / this.responsesPageSize)); this.load(); }
  clearFilters() { this.userId=''; this.textId=''; this.exerciseId=''; this.dateFrom=''; this.dateTo=''; this.sessionsPage=this.attemptsPage=this.responsesPage=1; this.load(); }
}

