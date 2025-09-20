import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SpeedReadingService } from '../../services/speed-reading.service';
import { SpeedReadingText, SpeedReadingFilter, CreateSpeedReadingTextDto } from '../../models/speed-reading.models';
import { NotificationService } from '../../../../shared/services/notification.service';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-text-library',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    DataTableComponent
  ],
  template: `
    <div class="text-library-container">
      <!-- Header -->
      <div class="row mb-4">
        <div class="col-md-6">
          <h2 class="h3 mb-0">
            <i class="fas fa-book me-2"></i>
            Metin Kütüphanesi
          </h2>
          <p class="text-muted">Hızlı okuma egzersizleri için metin yönetimi</p>
        </div>
        <div class="col-md-6 text-end">
          <button
            class="btn btn-primary me-2"
            (click)="openCreateModal()"
            [disabled]="loading()">
            <i class="fas fa-plus me-2"></i>
            Yeni Metin
          </button>
          <button
            class="btn btn-outline-secondary me-2"
            (click)="openImportModal()">
            <i class="fas fa-upload me-2"></i>
            İçe Aktar
          </button>
          <button
            class="btn btn-outline-secondary"
            (click)="exportTexts()"
            [disabled]="loading()">
            <i class="fas fa-download me-2"></i>
            Dışa Aktar
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <form [formGroup]="filterForm" class="row g-3">
            <div class="col-md-3">
              <label class="form-label">Arama</label>
              <input
                type="text"
                class="form-control"
                formControlName="search"
                placeholder="Başlık veya içerik ara...">
            </div>
            <div class="col-md-2">
              <label class="form-label">Zorluk</label>
              <select class="form-select" formControlName="difficulty">
                <option value="">Tümü</option>
                <option value="beginner">Başlangıç</option>
                <option value="intermediate">Orta</option>
                <option value="advanced">İleri</option>
                <option value="expert">Uzman</option>
              </select>
            </div>
            <div class="col-md-2">
              <label class="form-label">Kategori</label>
              <select class="form-select" formControlName="category">
                <option value="">Tümü</option>
                <option *ngFor="let cat of categories()" [value]="cat">{{cat}}</option>
              </select>
            </div>
            <div class="col-md-2">
              <label class="form-label">Dil</label>
              <select class="form-select" formControlName="language">
                <option value="">Tümü</option>
                <option value="tr">Türkçe</option>
                <option value="en">English</option>
              </select>
            </div>
            <div class="col-md-2">
              <label class="form-label">Durum</label>
              <select class="form-select" formControlName="isActive">
                <option value="">Tümü</option>
                <option value="true">Aktif</option>
                <option value="false">Pasif</option>
              </select>
            </div>
            <div class="col-md-1 d-flex align-items-end">
              <button
                type="button"
                class="btn btn-outline-secondary w-100"
                (click)="resetFilters()">
                <i class="fas fa-redo"></i>
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Statistics Cards -->
      <div class="row mb-4">
        <div class="col-md-3">
          <div class="card">
            <div class="card-body">
              <h6 class="text-muted mb-2">Toplam Metin</h6>
              <h3 class="mb-0">{{totalTexts()}}</h3>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="card">
            <div class="card-body">
              <h6 class="text-muted mb-2">Aktif Metin</h6>
              <h3 class="mb-0 text-success">{{activeTexts()}}</h3>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="card">
            <div class="card-body">
              <h6 class="text-muted mb-2">Ortalama Kelime</h6>
              <h3 class="mb-0">{{averageWordCount()}}</h3>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="card">
            <div class="card-body">
              <h6 class="text-muted mb-2">Kategori Sayısı</h6>
              <h3 class="mb-0">{{categories().length}}</h3>
            </div>
          </div>
        </div>
      </div>

      <!-- Data Table -->
      <div class="card">
        <div class="card-body">
          <div *ngIf="loading()" class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
              <span class="visually-hidden">Yükleniyor...</span>
            </div>
          </div>

          <div *ngIf="!loading() && texts().length === 0" class="text-center py-5">
            <i class="fas fa-book fa-3x text-muted mb-3"></i>
            <p class="text-muted">Henüz metin bulunmuyor</p>
            <button class="btn btn-primary" (click)="openCreateModal()">
              <i class="fas fa-plus me-2"></i>
              İlk Metni Ekle
            </button>
          </div>

          <div *ngIf="!loading() && texts().length > 0" class="table-responsive">
            <table class="table table-hover">
              <thead>
                <tr>
                  <th>
                    <input
                      type="checkbox"
                      class="form-check-input"
                      [checked]="allSelected()"
                      (change)="toggleSelectAll()">
                  </th>
                  <th>Başlık</th>
                  <th>Zorluk</th>
                  <th>Kategori</th>
                  <th>Kelime Sayısı</th>
                  <th>Dil</th>
                  <th>Durum</th>
                  <th>Oluşturulma</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let text of texts()">
                  <td>
                    <input
                      type="checkbox"
                      class="form-check-input"
                      [checked]="isSelected(text.id)"
                      (change)="toggleSelection(text.id)">
                  </td>
                  <td>
                    <a [routerLink]="['/admin/speed-reading/texts', text.id]" class="text-decoration-none">
                      {{text.title}}
                    </a>
                  </td>
                  <td>
                    <span [class]="'badge bg-' + getDifficultyColor(text.difficulty)">
                      {{getDifficultyLabel(text.difficulty)}}
                    </span>
                  </td>
                  <td>{{text.category}}</td>
                  <td>{{text.wordCount}}</td>
                  <td>
                    <span class="badge bg-secondary">
                      {{text.language === 'tr' ? 'TR' : 'EN'}}
                    </span>
                  </td>
                  <td>
                    <span [class]="'badge bg-' + (text.isActive ? 'success' : 'danger')">
                      {{text.isActive ? 'Aktif' : 'Pasif'}}
                    </span>
                  </td>
                  <td>{{formatDate(text.createdAt)}}</td>
                  <td>
                    <div class="btn-group btn-group-sm">
                      <button
                        class="btn btn-outline-primary"
                        [routerLink]="['/admin/speed-reading/texts', text.id, 'edit']"
                        title="Düzenle">
                        <i class="fas fa-edit"></i>
                      </button>
                      <button
                        class="btn btn-outline-info"
                        (click)="previewText(text)"
                        title="Önizle">
                        <i class="fas fa-eye"></i>
                      </button>
                      <button
                        class="btn btn-outline-danger"
                        (click)="confirmDelete(text)"
                        title="Sil">
                        <i class="fas fa-trash"></i>
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Bulk Actions -->
          <div *ngIf="selectedIds().length > 0" class="mt-3">
            <div class="alert alert-info d-flex justify-content-between align-items-center">
              <span>{{selectedIds().length}} metin seçildi</span>
              <div>
                <button class="btn btn-sm btn-outline-danger me-2" (click)="bulkDelete()">
                  <i class="fas fa-trash me-1"></i>
                  Seçilileri Sil
                </button>
                <button class="btn btn-sm btn-outline-secondary" (click)="clearSelection()">
                  Seçimi Temizle
                </button>
              </div>
            </div>
          </div>

          <!-- Pagination -->
          <nav *ngIf="totalPages() > 1" class="mt-4">
            <ul class="pagination justify-content-center">
              <li class="page-item" [class.disabled]="currentPage() === 1">
                <button class="page-link" (click)="goToPage(currentPage() - 1)">
                  <i class="fas fa-chevron-left"></i>
                </button>
              </li>
              <li
                *ngFor="let page of getPageNumbers()"
                class="page-item"
                [class.active]="page === currentPage()">
                <button class="page-link" (click)="goToPage(page)">{{page}}</button>
              </li>
              <li class="page-item" [class.disabled]="currentPage() === totalPages()">
                <button class="page-link" (click)="goToPage(currentPage() + 1)">
                  <i class="fas fa-chevron-right"></i>
                </button>
              </li>
            </ul>
          </nav>
        </div>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <div class="modal fade" id="textModal" tabindex="-1">
      <div class="modal-dialog modal-lg">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              {{editingText() ? 'Metin Düzenle' : 'Yeni Metin Ekle'}}
            </h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <form [formGroup]="textForm" (ngSubmit)="saveText()">
            <div class="modal-body">
              <div class="row g-3">
                <div class="col-md-8">
                  <label class="form-label">Başlık <span class="text-danger">*</span></label>
                  <input type="text" class="form-control" formControlName="title">
                </div>
                <div class="col-md-4">
                  <label class="form-label">Dil <span class="text-danger">*</span></label>
                  <select class="form-select" formControlName="language">
                    <option value="tr">Türkçe</option>
                    <option value="en">English</option>
                  </select>
                </div>
                <div class="col-12">
                  <label class="form-label">İçerik <span class="text-danger">*</span></label>
                  <textarea
                    class="form-control"
                    formControlName="content"
                    rows="10"
                    (input)="updateWordCount()"></textarea>
                  <small class="text-muted">Kelime sayısı: {{wordCount()}}</small>
                </div>
                <div class="col-md-4">
                  <label class="form-label">Zorluk <span class="text-danger">*</span></label>
                  <select class="form-select" formControlName="difficulty">
                    <option value="beginner">Başlangıç</option>
                    <option value="intermediate">Orta</option>
                    <option value="advanced">İleri</option>
                    <option value="expert">Uzman</option>
                  </select>
                </div>
                <div class="col-md-4">
                  <label class="form-label">Kategori <span class="text-danger">*</span></label>
                  <input type="text" class="form-control" formControlName="category" list="categoryList">
                  <datalist id="categoryList">
                    <option *ngFor="let cat of categories()" [value]="cat">
                  </datalist>
                </div>
                <div class="col-md-4">
                  <label class="form-label">Etiketler</label>
                  <input
                    type="text"
                    class="form-control"
                    formControlName="tags"
                    placeholder="Virgülle ayırın">
                </div>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">İptal</button>
              <button type="submit" class="btn btn-primary" [disabled]="textForm.invalid || saving()">
                <span *ngIf="saving()" class="spinner-border spinner-border-sm me-2"></span>
                {{editingText() ? 'Güncelle' : 'Kaydet'}}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .text-library-container {
      padding: 1.5rem;
    }

    .table th {
      font-weight: 600;
      color: #495057;
      border-bottom: 2px solid #dee2e6;
    }

    .badge {
      font-weight: 500;
    }

    .btn-group-sm .btn {
      padding: 0.25rem 0.5rem;
    }

    .modal-body {
      max-height: 70vh;
      overflow-y: auto;
    }

    textarea {
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      line-height: 1.6;
    }
  `]
})
export class TextLibraryComponent implements OnInit {
  private readonly speedReadingService = inject(SpeedReadingService);
  private readonly notificationService = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  // Signals
  texts = signal<SpeedReadingText[]>([]);
  loading = signal(false);
  saving = signal(false);
  currentPage = signal(1);
  pageSize = signal(10);
  totalItems = signal(0);
  selectedIds = signal<string[]>([]);
  categories = signal<string[]>([]);
  editingText = signal<SpeedReadingText | null>(null);
  wordCount = signal(0);

  // Computed
  totalPages = computed(() => Math.ceil(this.totalItems() / this.pageSize()));
  allSelected = computed(() =>
    this.texts().length > 0 && this.selectedIds().length === this.texts().length
  );
  totalTexts = computed(() => this.totalItems());
  activeTexts = computed(() => this.texts().filter(t => t.isActive).length);
  averageWordCount = computed(() => {
    const texts = this.texts();
    if (texts.length === 0) return 0;
    const total = texts.reduce((sum, t) => sum + t.wordCount, 0);
    return Math.round(total / texts.length);
  });

  // Forms
  filterForm = this.fb.group({
    search: [''],
    difficulty: [''],
    category: [''],
    language: [''],
    isActive: ['']
  });

  textForm = this.fb.group({
    title: ['', Validators.required],
    content: ['', Validators.required],
    difficulty: ['beginner', Validators.required],
    category: ['', Validators.required],
    language: ['tr', Validators.required],
    tags: ['']
  });

  ngOnInit(): void {
    this.loadTexts();
    this.loadCategories();
    this.setupFilterSubscription();
  }

  private setupFilterSubscription(): void {
    this.filterForm.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadTexts();
      });
  }

  loadTexts(): void {
    this.loading.set(true);
    const filters = this.getFilters();

    this.speedReadingService.getTexts({
      pageSize: this.pageSize(),
      ...filters
    }).subscribe({
      next: (result) => {
        this.texts.set(result.items);
        this.totalItems.set(result.totalCount);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading texts:', error);
        this.notificationService.error('Metinler yüklenirken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  loadCategories(): void {
    this.speedReadingService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
      error: (error) => console.error('Error loading categories:', error)
    });
  }

  private getFilters(): SpeedReadingFilter {
    const formValue = this.filterForm.value;
    const filters: any = {};

    if (formValue.search) {
      filters.search = formValue.search;
    }
    if (formValue.difficulty) {
      filters.difficulty = formValue.difficulty;
    }
    if (formValue.category) {
      filters.category = formValue.category;
    }
    if (formValue.language) {
      filters.language = formValue.language;
    }
    if (formValue.isActive !== '') {
      filters.isActive = formValue.isActive === 'true';
    }

    return filters;
  }

  openCreateModal(): void {
    this.editingText.set(null);
    this.textForm.reset({
      title: '',
      content: '',
      difficulty: 'beginner',
      category: '',
      language: 'tr',
      tags: ''
    });
    this.wordCount.set(0);
    // Bootstrap modal göster
    const modal = new (window as any).bootstrap.Modal(document.getElementById('textModal'));
    modal.show();
  }

  openEditModal(text: SpeedReadingText): void {
    this.editingText.set(text);
    this.textForm.patchValue({
      title: text.title,
      content: text.content,
      difficulty: text.difficulty,
      category: text.category,
      language: text.language,
      tags: text.tags.join(', ')
    });
    this.wordCount.set(text.wordCount);
    const modal = new (window as any).bootstrap.Modal(document.getElementById('textModal'));
    modal.show();
  }

  saveText(): void {
    if (this.textForm.invalid) return;

    this.saving.set(true);
    const formValue = this.textForm.value;
    const dto: CreateSpeedReadingTextDto = {
      title: formValue.title!,
      content: formValue.content!,
      difficulty: formValue.difficulty as any,
      category: formValue.category!,
      language: formValue.language!,
      tags: formValue.tags ? formValue.tags.split(',').map(t => t.trim()) : []
    };

    const saveObservable = this.editingText()
      ? this.speedReadingService.updateText(this.editingText()!.id, dto)
      : this.speedReadingService.createText(dto);

    saveObservable.subscribe({
      next: () => {
        this.notificationService.success(
          this.editingText() ? 'Metin güncellendi' : 'Metin oluşturuldu'
        );
        this.loadTexts();
        this.saving.set(false);
        const modal = (window as any).bootstrap.Modal.getInstance(document.getElementById('textModal'));
        modal.hide();
      },
      error: (error) => {
        console.error('Error saving text:', error);
        this.notificationService.error('Kaydetme işlemi başarısız');
        this.saving.set(false);
      }
    });
  }

  confirmDelete(text: SpeedReadingText): void {
    if (confirm(`"${text.title}" metnini silmek istediğinize emin misiniz?`)) {
      this.speedReadingService.deleteText(text.id).subscribe({
        next: () => {
          this.notificationService.success('Metin silindi');
          this.loadTexts();
        },
        error: (error) => {
          console.error('Error deleting text:', error);
          this.notificationService.error('Silme işlemi başarısız');
        }
      });
    }
  }

  bulkDelete(): void {
    const count = this.selectedIds().length;
    if (confirm(`${count} metin silinecek. Emin misiniz?`)) {
      this.speedReadingService.bulkDeleteTexts(this.selectedIds()).subscribe({
        next: () => {
          this.notificationService.success(`${count} metin silindi`);
          this.selectedIds.set([]);
          this.loadTexts();
        },
        error: (error) => {
          console.error('Error bulk deleting:', error);
          this.notificationService.error('Toplu silme işlemi başarısız');
        }
      });
    }
  }

  exportTexts(): void {
    this.loading.set(true);
    this.speedReadingService.exportTexts(this.getFilters()).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `texts-${new Date().getTime()}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error exporting texts:', error);
        this.notificationService.error('Dışa aktarma başarısız');
        this.loading.set(false);
      }
    });
  }

  openImportModal(): void {
    // Import modal implementation
    this.notificationService.info('İçe aktarma özelliği yakında eklenecek');
  }

  previewText(_text: SpeedReadingText): void {
    // Preview modal implementation
    this.notificationService.info('Önizleme özelliği yakında eklenecek');
  }

  updateWordCount(): void {
    const content = this.textForm.get('content')?.value || '';
    this.wordCount.set(content.split(/\s+/).filter(w => w.length > 0).length);
  }

  toggleSelection(id: string): void {
    const current = this.selectedIds();
    if (current.includes(id)) {
      this.selectedIds.set(current.filter(i => i !== id));
    } else {
      this.selectedIds.set([...current, id]);
    }
  }

  toggleSelectAll(): void {
    if (this.allSelected()) {
      this.selectedIds.set([]);
    } else {
      this.selectedIds.set(this.texts().map(t => t.id));
    }
  }

  clearSelection(): void {
    this.selectedIds.set([]);
  }

  isSelected(id: string): boolean {
    return this.selectedIds().includes(id);
  }

  resetFilters(): void {
    this.filterForm.reset({
      search: '',
      difficulty: '',
      category: '',
      language: '',
      isActive: ''
    });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadTexts();
  }

  getPageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) {
        pages.push(i);
      }
    } else {
      if (current <= 3) {
        for (let i = 1; i <= 5; i++) {
          pages.push(i);
        }
        pages.push(-1); // Ellipsis
        pages.push(total);
      } else if (current >= total - 2) {
        pages.push(1);
        pages.push(-1); // Ellipsis
        for (let i = total - 4; i <= total; i++) {
          pages.push(i);
        }
      } else {
        pages.push(1);
        pages.push(-1); // Ellipsis
        for (let i = current - 1; i <= current + 1; i++) {
          pages.push(i);
        }
        pages.push(-1); // Ellipsis
        pages.push(total);
      }
    }

    return pages;
  }

  getDifficultyColor(difficulty: string): string {
    switch (difficulty) {
      case 'beginner': return 'success';
      case 'intermediate': return 'info';
      case 'advanced': return 'warning';
      case 'expert': return 'danger';
      default: return 'secondary';
    }
  }

  getDifficultyLabel(difficulty: string): string {
    switch (difficulty) {
      case 'beginner': return 'Başlangıç';
      case 'intermediate': return 'Orta';
      case 'advanced': return 'İleri';
      case 'expert': return 'Uzman';
      default: return difficulty;
    }
  }

  formatDate(date: Date | string): string {
    const d = new Date(date);
    return d.toLocaleDateString('tr-TR');
  }
}