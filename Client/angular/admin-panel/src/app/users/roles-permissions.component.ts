import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserAdminService, RoleDto, PermissionDto } from '../services/user-admin.service';

@Component({
  standalone: true,
  selector: 'app-roles-permissions',
  templateUrl: './roles-permissions.component.html',
  styleUrls: ['./roles-permissions.component.scss'],
  imports: [CommonModule, FormsModule]
})
export class RolesPermissionsComponent {
  private admin = inject(UserAdminService);

  roles = signal<RoleDto[]>([]);
  permissions = signal<PermissionDto[]>([]);
  selectedRoleId = signal<string | null>(null);
  assigned = signal<Set<string>>(new Set()); // permission names
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loading.set(true);
    this.admin.listRoles({ page: 1, pageSize: 500 }).subscribe({
      next: (r) => { this.roles.set(((r as any).data ?? r) as RoleDto[]); },
      error: (e) => this.error.set(e?.message ?? 'Roller alınamadı'),
      complete: () => this.loading.set(false)
    });
    this.admin.listPermissions({ page: 1, pageSize: 2000 }).subscribe({
      next: (p) => { this.permissions.set(((p as any).data ?? p) as PermissionDto[]); },
      error: (e) => this.error.set(e?.message ?? 'İzinler alınamadı')
    });
  }

  onRoleChange() {
    const roleId = this.selectedRoleId();
    this.assigned.set(new Set());
    if (!roleId) return;
    this.admin.getRolePermissions(roleId).subscribe({
      next: (res) => {
        const perms = ((res as any).data ?? res) as string[];
        this.assigned.set(new Set(perms));
      }
    });
  }

  isChecked(permissionName: string): boolean {
    return this.assigned().has(permissionName);
  }

  togglePermission(permission: PermissionDto, ev: Event) {
    const roleId = this.selectedRoleId();
    if (!roleId) return;
    const checked = (ev.target as HTMLInputElement).checked;
    if (checked) {
      this.admin.assignPermissionToRole(roleId, permission.id).subscribe({
        next: () => {
          const set = new Set(this.assigned());
          set.add(permission.name);
          this.assigned.set(set);
        },
        error: (e) => this.error.set(e?.message ?? 'İzin atama hatası')
      });
    } else {
      this.admin.removePermissionFromRole(roleId, permission.id).subscribe({
        next: () => {
          const set = new Set(this.assigned());
          set.delete(permission.name);
          this.assigned.set(set);
        },
        error: (e) => this.error.set(e?.message ?? 'İzin kaldırma hatası')
      });
    }
  }
}

