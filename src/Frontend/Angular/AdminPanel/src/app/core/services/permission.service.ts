import { Injectable, inject } from '@angular/core';
import { AuthService } from '../auth/services/auth.service';
import { Observable, map, BehaviorSubject, of, catchError } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../api/models/api.models';

export interface PermissionCheckResult {
  permission: string;
  allowed: boolean;
  reason?: string;
}

export interface UserPermissions {
  directPermissions: string[];
  rolePermissions: string[];
  allPermissions: string[];
  wildcardPermissions: string[];
}

@Injectable({
  providedIn: 'root'
})
export class PermissionService {
  private readonly authService = inject(AuthService);
  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiGateway}${environment.endpoints.auth}`;
  private cachedPermissions = new BehaviorSubject<UserPermissions | null>(null);
  public permissions$ = this.cachedPermissions.asObservable();

  hasPermission(permission: string): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      map(user => {
        if (!user) return false;

        // SuperAdmin has all permissions
        if (user.roles?.some(role => role.name === 'SuperAdmin')) return true;

        // Check if user has the specific permission
        return user.permissions?.includes(permission) || false;
      })
    );
  }

  hasAnyPermission(permissions: string[]): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      map(user => {
        if (!user) return false;

        // SuperAdmin has all permissions
        if (user.roles?.some(role => role.name === 'SuperAdmin')) return true;

        // Check if user has any of the permissions
        return permissions.some(permission =>
          user.permissions?.includes(permission) || false
        );
      })
    );
  }

  hasAllPermissions(permissions: string[]): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      map(user => {
        if (!user) return false;

        // SuperAdmin has all permissions
        if (user.roles?.some(role => role.name === 'SuperAdmin')) return true;

        // Check if user has all permissions
        return permissions.every(permission =>
          user.permissions?.includes(permission) || false
        );
      })
    );
  }

  hasRole(roleName: string): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      map(user => user?.roles?.some(role => role.name === roleName) || false)
    );
  }

  hasAnyRole(roleNames: string[]): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      map(user => {
        if (!user) return false;
        return roleNames.some(roleName =>
          user.roles?.some(role => role.name === roleName) || false
        );
      })
    );
  }

  // Synchronous versions for templates
  canAccess(permission: string): boolean {
    // Get current user directly from AuthService
    const user = this.authService.currentUserValue;

    if (!user) {
      return false;
    }

    // SuperAdmin has all permissions
    if (user.roles?.some((role: any) => role.name === 'SuperAdmin')) {
      return true;
    }

    // Check user permissions
    const hasPermission = user.permissions?.includes(permission) || false;

    return hasPermission;
  }

  canAccessAny(permissions: string[]): boolean {
    // Get current user directly from AuthService
    const user = this.authService.currentUserValue;

    if (!user) {
      return false;
    }

    // SuperAdmin has all permissions
    if (user.roles?.some((role: any) => role.name === 'SuperAdmin')) {
      return true;
    }

    // Check if user has any of the permissions
    const hasAnyPermission = permissions.some(permission =>
      user.permissions?.includes(permission) || false
    );

    return hasAnyPermission;
  }

  isInRole(roleName: string): boolean {
    const user = this.authService.currentUserValue;
    const isInRole = user?.roles?.some((role: any) => role.name === roleName) || false;

    return isInRole;
  }

  isInAnyRole(roleNames: string[]): boolean {
    const user = this.authService.currentUserValue;

    if (!user) {
      return false;
    }

    const isInAnyRole = roleNames.some(roleName =>
      user.roles?.some((role: any) => role.name === roleName) || false
    );

    return isInAnyRole;
  }

  /**
   * Check multiple permissions at once
   */
  checkPermissions(permissions: string[]): Observable<PermissionCheckResult[]> {
    return this.http.post<ApiResponse<PermissionCheckResult[]>>(
      `${this.apiUrl}/check-permissions`,
      { permissions }
    ).pipe(
      map(response => response.data || []),
      catchError(() => of(permissions.map(p => ({ permission: p, allowed: false, reason: 'Check failed' }))))
    );
  }

  /**
   * Load user permissions from server
   */
  loadUserPermissions(): Observable<UserPermissions> {
    return this.http.get<ApiResponse<UserPermissions>>(`${this.apiUrl}/user-permissions`).pipe(
      map(response => response.data!),
      map(permissions => {
        this.cachedPermissions.next(permissions);
        return permissions;
      }),
      catchError(() => {
        const emptyPermissions: UserPermissions = {
          directPermissions: [],
          rolePermissions: [],
          allPermissions: [],
          wildcardPermissions: []
        };
        this.cachedPermissions.next(emptyPermissions);
        return of(emptyPermissions);
      })
    );
  }

  /**
   * Check permission with wildcard support
   */
  private checkPermissionInCache(permission: string, cached: UserPermissions): boolean {
    if (cached.allPermissions.includes(permission)) {
      return true;
    }

    return cached.wildcardPermissions.some(wildcard =>
      this.matchesWildcard(permission, wildcard)
    );
  }

  /**
   * Match permission against wildcard pattern
   */
  private matchesWildcard(permission: string, wildcard: string): boolean {
    if (wildcard === '*.*.*' || wildcard === '*') {
      return true;
    }

    const permissionParts = permission.split('.');
    const wildcardParts = wildcard.split('.');

    if (wildcardParts.length !== permissionParts.length) {
      return false;
    }

    return wildcardParts.every((wildcardPart, index) =>
      wildcardPart === '*' || wildcardPart === permissionParts[index]
    );
  }

  /**
   * Check if user has wildcard permission with pattern matching
   */
  canAccessWithWildcard(permission: string): boolean {
    const user = this.authService.currentUserValue;
    if (!user) return false;

    if (user.roles?.some((role: any) => role.name === 'SuperAdmin')) {
      return true;
    }

    const cached = this.cachedPermissions.value;
    if (cached) {
      return this.checkPermissionInCache(permission, cached);
    }

    return this.canAccess(permission);
  }

  /**
   * Get current cached permissions
   */
  getCurrentPermissions(): UserPermissions | null {
    return this.cachedPermissions.value;
  }

  /**
   * Clear cached permissions
   */
  clearPermissions(): void {
    this.cachedPermissions.next(null);
  }

  /**
   * Initialize permissions on service start
   */
  initializePermissions(): void {
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.loadUserPermissions().subscribe();
      } else {
        this.clearPermissions();
      }
    });
  }
}