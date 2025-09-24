import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { tap, catchError, map } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import {
  User,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RefreshTokenResponse,
  ChangePasswordRequest,
  ResetPasswordRequest,
  GoogleLoginRequest,
  Role
} from '../models/auth.models';
import { TokenService } from './token.service';
import { ApiResponse } from '../../api/models/api.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenService = inject(TokenService);

  private readonly apiUrl = `${environment.apiGateway}${environment.endpoints.auth}`;
  private readonly userKey = environment.storage.prefix + environment.auth.userKey;
  private readonly permissionsKey = environment.storage.prefix + environment.auth.permissionsKey;

  private currentUserSubject = new BehaviorSubject<User | null>(this.loadUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor() {
    // Initialize authentication state asynchronously
    this.initializeAuthState();
  }

  private async initializeAuthState(): Promise<void> {
    try {
      const user = this.loadUserFromStorage();
      const isValid = await this.tokenService.isTokenValid();

      if (user && isValid) {
        this.currentUserSubject.next(user);
        this.isAuthenticatedSubject.next(true);
      } else if (!isValid) {
        await this.clearAuthData();
      }
    } catch (error) {
      console.error('Failed to initialize auth state:', error);
      await this.clearAuthData();
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(async response => await this.handleLoginResponse(response)),
      catchError(error => this.handleAuthError(error))
    );
  }

  googleLogin(request: GoogleLoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/google-login`, request).pipe(
      tap(async response => await this.handleLoginResponse(response)),
      catchError(error => this.handleAuthError(error))
    );
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/register`, request).pipe(
      catchError(error => this.handleAuthError(error))
    );
  }

  refreshToken(): Observable<RefreshTokenResponse> {
    return new Observable(observer => {
      this.tokenService.getRefreshToken().then(refreshToken => {
        if (!refreshToken) {
          observer.error(new Error('No refresh token available'));
          return;
        }

        this.http.post<RefreshTokenResponse>(
          `${this.apiUrl}/refresh`,
          { refreshToken }
        ).pipe(
          tap(async response => {
            const success = await this.tokenService.setTokens(response.accessToken, response.refreshToken);
            if (success) {
              this.isAuthenticatedSubject.next(true);
              observer.next(response);
              observer.complete();
            } else {
              observer.error(new Error('Failed to store tokens securely'));
            }
          }),
          catchError(error => {
            this.logout();
            observer.error(error);
            return throwError(() => error);
          })
        ).subscribe();
      }).catch(error => {
        observer.error(error);
      });
    });
  }

  logout(): void {
    // Use async logout for secure token handling
    this.performLogout().catch(error => {
      console.error('Logout error:', error);
      // Even if logout fails, clear local data and redirect
      this.router.navigate(['/auth/login']);
    });
  }

  private async performLogout(): Promise<void> {
    try {
      const refreshToken = await this.tokenService.getRefreshToken();

      if (refreshToken) {
        // Call logout endpoint (fire and forget)
        this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe({
          error: () => {} // Ignore errors
        });
      }

      await this.clearAuthData();
      this.router.navigate(['/auth/login']);
    } catch (error) {
      console.error('Logout process failed:', error);
      throw error;
    }
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<ApiResponse<User>>(`${this.apiUrl}/me`).pipe(
      map(response => response.data!),
      tap(user => {
        this.saveUserToStorage(user);
        this.currentUserSubject.next(user);
      })
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/change-password`, request);
  }


  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reset-password`, request);
  }

  verifyEmail(token: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/verify-email`, { token });
  }

  resendVerificationEmail(email?: string): Observable<void> {
    const body = email ? { email } : {};
    return this.http.post<void>(`${this.apiUrl}/resend-verification`, body);
  }

  hasPermission(permission: string): Observable<boolean> {
    const permissions = this.loadPermissionsFromStorage();
    if (permissions.includes(permission)) {
      return of(true);
    }

    // Check with backend for real-time permission check
    return this.http.get<ApiResponse<boolean>>(
      `${this.apiUrl}/check-permission/${permission}`
    ).pipe(
      map(response => response.data || false),
      catchError(() => of(false))
    );
  }

  hasRole(role: string): Observable<boolean> {
    const user = this.currentUserSubject.value;
    if (!user) return of(false);

    return of(user.roles.some(r => r.name === role));
  }

  hasAnyPermission(permissions: string[]): Observable<boolean> {
    const userPermissions = this.loadPermissionsFromStorage();
    return of(permissions.some(p => userPermissions.includes(p)));
  }

  hasAllPermissions(permissions: string[]): Observable<boolean> {
    const userPermissions = this.loadPermissionsFromStorage();
    return of(permissions.every(p => userPermissions.includes(p)));
  }

  private async handleLoginResponse(response: LoginResponse): Promise<void> {
    try {
      // Use consistent camelCase property names
      const accessToken = response.accessToken;
      const refreshToken = response.refreshToken;
      const expiresIn = response.expiresIn;
      const user = response.user;
      const permissions = response.permissions || [];
      const rolesFromResponse = response.roles || [];

      if (!accessToken || !refreshToken || !user) {
        throw new Error('Invalid login response format');
      }

      // Store tokens securely
      const success = await this.tokenService.setTokens(
        accessToken,
        refreshToken,
        expiresIn
      );

      if (!success) {
        throw new Error('Failed to store tokens securely');
      }

      // API response'dan gelen user data'sını düzenle
      user.permissions = permissions;
      // Map roles (string[]) to Role[] expected by UI
      const mappedRoles: Role[] = rolesFromResponse.map((roleName: string) => ({
        id: roleName,
        name: roleName,
        description: '',
        isSystemRole: false,
        permissions: [],
        createdAt: new Date(),
        updatedAt: new Date()
      }));
      (user as User).roles = mappedRoles;

      this.saveUserToStorage(user);
      this.savePermissionsToStorage(permissions);
      this.currentUserSubject.next(user);
      this.isAuthenticatedSubject.next(true);
    } catch (error) {
      console.error('Failed to handle login response:', error);
      throw error;
    }
  }

  private handleAuthError(error: unknown): Observable<never> {
    this.clearAuthData();
    return throwError(() => error);
  }

  private async clearAuthData(): Promise<void> {
    try {
      await this.tokenService.clearTokens();
      localStorage.removeItem(this.userKey);
      localStorage.removeItem(this.permissionsKey);
      this.currentUserSubject.next(null);
      this.isAuthenticatedSubject.next(false);
    } catch (error) {
      console.error('Failed to clear auth data:', error);
      // Force clear even if secure storage fails
      localStorage.removeItem(this.userKey);
      localStorage.removeItem(this.permissionsKey);
      this.currentUserSubject.next(null);
      this.isAuthenticatedSubject.next(false);
    }
  }

  private saveUserToStorage(user: User): void {
    localStorage.setItem(this.userKey, JSON.stringify(user));
  }

  private loadUserFromStorage(): User | null {
    const userData = localStorage.getItem(this.userKey);
    return userData ? JSON.parse(userData) : null;
  }

  private savePermissionsToStorage(permissions: string[]): void {
    localStorage.setItem(this.permissionsKey, JSON.stringify(permissions));
  }

  private loadPermissionsFromStorage(): string[] {
    const permissionsData = localStorage.getItem(this.permissionsKey);
    return permissionsData ? JSON.parse(permissionsData) : [];
  }

  get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  /**
   * Get authentication status asynchronously (more reliable)
   */
  async getAuthenticationStatus(): Promise<boolean> {
    try {
      return await this.tokenService.isTokenValid();
    } catch {
      return false;
    }
  }

  /**
   * Request password reset email
   */
  requestPasswordReset(email: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/forgot-password`, { email }).pipe(
      map(response => response),
      catchError(error => {
        console.error('Password reset request failed:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Get comprehensive authentication information
   */
  async getAuthInfo(): Promise<{
    isAuthenticated: boolean;
    tokenStats: unknown;
    user: User | null;
  }> {
    try {
      const [isAuthenticated, tokenStats] = await Promise.all([
        this.getAuthenticationStatus(),
        this.tokenService.getTokenStats()
      ]);

      return {
        isAuthenticated,
        tokenStats,
        user: this.currentUserValue
      };
    } catch (error) {
      console.error('Failed to get auth info:', error);
      return {
        isAuthenticated: false,
        tokenStats: { hasAccessToken: false, hasRefreshToken: false },
        user: null
      };
    }
  }

  async handleGoogleLoginSuccess(loginResponse: LoginResponse): Promise<void> {
    await this.handleLoginResponse(loginResponse);
  }
}