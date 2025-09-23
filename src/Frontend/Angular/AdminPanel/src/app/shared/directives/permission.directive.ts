import { Directive, Input, TemplateRef, ViewContainerRef, inject, signal, effect } from '@angular/core';

interface Permission {
  resource: string;
  action: string;
}

// Basit izin servisi interface'i
interface PermissionService {
  hasPermission(permission: Permission | string): boolean;
  hasAnyPermission(permissions: (Permission | string)[]): boolean;
  hasAllPermissions(permissions: (Permission | string)[]): boolean;
}

// Basit izin servisi implementasyonu
class SimplePermissionService implements PermissionService {
  // Mock izinler - gerçek uygulamada backend'den gelecek
  private userPermissions: string[] = [
    'user:read',
    'user:create',
    'user:update',
    'user:delete',
    'role:read',
    'role:create',
    'group:read',
    'dashboard:view'
  ];

  hasPermission(permission: Permission | string): boolean {
    const permissionString = typeof permission === 'string'
      ? permission
      : `${permission.resource}:${permission.action}`;

    return this.userPermissions.includes(permissionString);
  }

  hasAnyPermission(permissions: (Permission | string)[]): boolean {
    return permissions.some(permission => this.hasPermission(permission));
  }

  hasAllPermissions(permissions: (Permission | string)[]): boolean {
    return permissions.every(permission => this.hasPermission(permission));
  }
}

@Directive({
  selector: '[appPermission]',
  standalone: true
})
export class PermissionDirective {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly permissionService = new SimplePermissionService();

  private readonly permission = signal<Permission | string | null>(null);
  private readonly permissions = signal<(Permission | string)[] | null>(null);
  private readonly requireAll = signal<boolean>(false);
  private readonly fallbackTemplate = signal<TemplateRef<unknown> | null>(null);

  @Input() set appPermission(value: Permission | string) {
    this.permission.set(value);
    this.permissions.set(null);
  }

  @Input() set appPermissionAny(value: (Permission | string)[]) {
    this.permissions.set(value);
    this.permission.set(null);
    this.requireAll.set(false);
  }

  @Input() set appPermissionAll(value: (Permission | string)[]) {
    this.permissions.set(value);
    this.permission.set(null);
    this.requireAll.set(true);
  }

  @Input() set appPermissionFallback(template: TemplateRef<unknown>) {
    this.fallbackTemplate.set(template);
  }

  constructor() {
    effect(() => {
      this.updateView();
    });
  }

  private updateView(): void {
    const hasAccess = this.checkPermissions();

    this.viewContainer.clear();

    if (hasAccess) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    } else if (this.fallbackTemplate()) {
      this.viewContainer.createEmbeddedView(this.fallbackTemplate()!);
    }
  }

  private checkPermissions(): boolean {
    const singlePermission = this.permission();
    const multiplePermissions = this.permissions();
    const requireAll = this.requireAll();

    if (singlePermission) {
      return this.permissionService.hasPermission(singlePermission);
    }

    if (multiplePermissions) {
      return requireAll
        ? this.permissionService.hasAllPermissions(multiplePermissions)
        : this.permissionService.hasAnyPermission(multiplePermissions);
    }

    return true; // Eğer hiç izin belirtilmemişse göster
  }
}