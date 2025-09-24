import { Injectable, inject } from '@angular/core';
import { PermissionService } from '../../core/services/permission.service';

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly core = inject(PermissionService);

  // Wrapper API to maintain backward compatibility
  load(permissions: string[] | Set<string>): void {
    // No-op: core servis login sonrası kendi cache’ini yönetiyor
  }

  has(permission: string): boolean {
    return this.core.canAccessWithWildcard(permission);
  }

  hasAny(perms: string[]): boolean {
    return this.core.canAccessAny(perms);
  }

  hasAll(perms: string[]): boolean {
    return perms.every(p => this.core.canAccessWithWildcard(p));
  }

  get all(): string[] {
    const perms = this.core.getCurrentPermissions();
    if (!perms) { return []; }
    return perms.allPermissions?.length ? perms.allPermissions : [];
  }
}

