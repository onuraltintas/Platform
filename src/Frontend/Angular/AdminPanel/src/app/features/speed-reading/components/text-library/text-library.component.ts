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
  templateUrl: './text-library.component.html',
  styleUrl: './text-library.component.scss'
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