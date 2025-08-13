import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { UserAdminService, PermissionDto, CreatePermissionBody, UpdatePermissionBody } from '../services/user-admin.service';

@Component({
  standalone: true,
  selector: 'app-permission-form',
  templateUrl: './permission-form.component.html',
  styleUrls: ['./permission-form.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class PermissionFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private admin = inject(UserAdminService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  permissionId: string | null = null;
  loading = false;

  form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    description: [''],
    group: [''],
    isActive: [true]
  });

  ngOnInit(): void {
    this.permissionId = this.route.snapshot.paramMap.get('id');
    if (this.permissionId) {
      this.load(this.permissionId);
    }
  }

  load(id: string) {
    this.loading = true;
    this.admin.getPermission(id).subscribe({
      next: (p) => {
        this.form.patchValue({
          name: p.name,
          description: p.description || '',
          group: p.group || '',
          isActive: p.isActive
        });
        this.loading = false;
      },
      error: () => { this.toastr.error('İzin yüklenemedi'); this.loading = false; }
    });
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); this.toastr.error('Lütfen formdaki hataları düzeltin.'); return; }
    this.loading = true;
    const v = this.form.getRawValue();

    if (!this.permissionId) {
      const body: CreatePermissionBody = { name: v.name!, description: v.description || undefined, group: v.group || undefined, isActive: !!v.isActive };
      this.admin.createPermission(body).subscribe({
        next: () => { this.toastr.success('İzin oluşturuldu'); this.loading = false; this.router.navigate(['/permissions']); },
        error: () => { this.toastr.error('İzin oluşturulamadı'); this.loading = false; }
      });
    } else {
      const body: UpdatePermissionBody = { name: v.name!, description: v.description || undefined, group: v.group || undefined, isActive: !!v.isActive };
      this.admin.updatePermission(this.permissionId, body).subscribe({
        next: () => { this.toastr.success('İzin güncellendi'); this.loading = false; this.router.navigate(['/permissions']); },
        error: () => { this.toastr.error('İzin güncellenemedi'); this.loading = false; }
      });
    }
  }
}