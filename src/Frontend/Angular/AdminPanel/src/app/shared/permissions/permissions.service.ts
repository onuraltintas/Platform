import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly permissionsSignal = signal<Set<string>>(new Set());

  load(permissions: string[] | Set<string>): void {
    const set = Array.isArray(permissions) ? new Set(permissions) : new Set(permissions);
    this.permissionsSignal.set(set);
  }

  has(permission: string): boolean {
    return this.permissionsSignal().has(permission);
  }

  hasAny(perms: string[]): boolean {
    const set = this.permissionsSignal();
    return perms.some(p => set.has(p));
  }

  hasAll(perms: string[]): boolean {
    const set = this.permissionsSignal();
    return perms.every(p => set.has(p));
  }

  get all(): string[] {
    return Array.from(this.permissionsSignal());
  }
}

