import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { UserAdminService, CreateCategoryBody, UpdateCategoryBody } from '../../../users/data-access/user-admin.service';

@Component({
  standalone: true,
  selector: 'app-category-form',
  templateUrl: './category-form.component.html',
  styleUrls: ['./category-form.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class CategoryFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private admin = inject(UserAdminService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  categoryId: string | null = null;
  loading = false;

  form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
    type: [''],
    isActive: [true]
  });

  ngOnInit(): void {
    this.categoryId = this.route.snapshot.paramMap.get('id');
    if (this.categoryId) {
      this.load(this.categoryId);
    }
  }

  load(id: string) {
    this.loading = true;
    this.admin.getCategory(id).subscribe({
      next: (c) => { this.form.patchValue({ name: c.name, description: c.description || '', type: c.type || '', isActive: c.isActive }); this.loading = false; },
      error: () => { this.toastr.error('Kategori yüklenemedi'); this.loading = false; }
    });
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); this.toastr.error('Lütfen formdaki hataları düzeltin.'); return; }
    this.loading = true;
    const v = this.form.getRawValue();

    if (!this.categoryId) {
      const body: CreateCategoryBody = { name: v.name!, description: v.description || undefined, type: v.type || undefined, isActive: !!v.isActive };
      this.admin.createCategory(body).subscribe({
        next: () => { this.toastr.success('Kategori oluşturuldu'); this.loading = false; this.router.navigate(['/categories']); },
        error: () => { this.toastr.error('Kategori oluşturulamadı'); this.loading = false; }
      });
    } else {
      const body: UpdateCategoryBody = { name: v.name!, description: v.description || undefined, type: v.type || undefined, isActive: !!v.isActive };
      this.admin.updateCategory(this.categoryId, body).subscribe({
        next: () => { this.toastr.success('Kategori güncellendi'); this.loading = false; this.router.navigate(['/categories']); },
        error: () => { this.toastr.error('Kategori güncellenemedi'); this.loading = false; }
      });
    }
  }
}