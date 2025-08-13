import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { UserAdminService, RoleDto, PermissionDto, CreateRoleBody, UpdateRoleBody } from '../services/user-admin.service';
import { forkJoin, map, of, tap } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-role-form',
  templateUrl: './role-form.component.html',
  styleUrls: ['./role-form.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class RoleFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private admin = inject(UserAdminService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  roleId: string | null = null;
  loading = false;
  permissions: PermissionDto[] = [];

  form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    description: [''],
    isActive: [true],
    permissionIds: [[] as string[]]
  });

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id');
    if (this.roleId) {
      this.loadForEdit(this.roleId);
    } else {
      this.loadPermissions();
    }
  }

  private loadForEdit(id: string) {
    this.loading = true;
    const loadPerms$ = this.admin.listPermissions({ page: 1, pageSize: 1000 }).pipe(
      map(res => Array.isArray(res) ? res : res.data),
      tap(perms => this.permissions = perms)
    );
    const loadRole$ = this.admin.getRole(id);
    const loadRolePerms$ = this.admin.getRolePermissions(id).pipe(map(r => Array.isArray(r) ? r : r.data));

    forkJoin([loadPerms$, loadRole$, loadRolePerms$]).subscribe({
      next: ([_, role, rolePermKeys]) => {
        this.form.patchValue({
          name: role.name,
          description: role.description || '',
          isActive: role.isActive
        });
        const idSet = new Set<string>();
        for (const key of rolePermKeys || []) {
          const byId = this.permissions.find(p => p.id === key);
          if (byId) { idSet.add(byId.id); continue; }
          const byName = this.permissions.find(p => p.name === key);
          if (byName) { idSet.add(byName.id); }
        }
        this.form.patchValue({ permissionIds: Array.from(idSet) });
        this.loading = false;
      },
      error: () => {
        this.toastr.error('Rol bilgisi yüklenemedi');
        this.loading = false;
      }
    });
  }

  loadPermissions() {
    this.admin.listPermissions({ page: 1, pageSize: 1000 }).subscribe(res => {
      this.permissions = Array.isArray(res) ? res : res.data;
    });
  }

  // Seçili izin adlarını göstermek için yardımcı getter
  get selectedPermissionNames(): string[] {
    const ids = this.form.get('permissionIds')?.value as string[] || [];
    const byId: Record<string, string> = this.permissions.reduce((acc, p) => { acc[p.id] = p.name; return acc; }, {} as Record<string,string>);
    return ids.map(id => byId[id]).filter(Boolean);
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toastr.error('Lütfen formdaki hataları düzeltin.');
      return;
    }
    this.loading = true;
    const value = this.form.getRawValue();

    if (!this.roleId) {
      const body: CreateRoleBody = {
        name: value.name!,
        description: value.description || undefined,
        isActive: !!value.isActive,
        permissionIds: value.permissionIds || []
      };
      this.admin.createRole(body).subscribe({
        next: (created) => {
          // İzinleri ayrıca set etmek istersek:
          if ((value.permissionIds || []).length) {
            this.admin.updateRolePermissions(created.id, value.permissionIds!).subscribe(() =>{});
          }
          this.toastr.success('Rol oluşturuldu');
          this.loading = false;
          this.router.navigate(['/roles']);
        },
        error: () => {
          this.toastr.error('Rol oluşturulamadı');
          this.loading = false;
        }
      });
    } else {
      const body: UpdateRoleBody = {
        name: value.name!,
        description: value.description || undefined,
        isActive: !!value.isActive
      } as any;
      this.admin.updateRole(this.roleId, body).subscribe({
        next: () => {
          this.admin.updateRolePermissions(this.roleId!, value.permissionIds || []).subscribe(() =>{});
          this.toastr.success('Rol güncellendi');
          this.loading = false;
          this.router.navigate(['/roles']);
        },
        error: () => {
          this.toastr.error('Rol güncellenemedi');
          this.loading = false;
        }
      });
    }
  }
}