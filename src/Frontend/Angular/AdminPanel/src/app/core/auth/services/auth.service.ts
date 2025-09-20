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
  GoogleLoginRequest
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

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.tokenService.isTokenValid());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor() {
    // Initialize authentication state
    this.initializeAuthState();
  }

  private initializeAuthState(): void {
    const user = this.loadUserFromStorage();
    const isValid = this.tokenService.isTokenValid();

    if (user && isValid) {
      this.currentUserSubject.next(user);
      this.isAuthenticatedSubject.next(true);
    } else if (!isValid) {
      this.clearAuthData();
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => this.handleLoginResponse(response)),
      catchError(error => this.handleAuthError(error))
    );
  }

  googleLogin(request: GoogleLoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/google-login`, request).pipe(
      tap(response => this.handleLoginResponse(response)),
      catchError(error => this.handleAuthError(error))
    );
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/register`, request).pipe(
      catchError(error => this.handleAuthError(error))
    );
  }

  refreshToken(): Observable<RefreshTokenResponse> {
    const refreshToken = this.tokenService.getRefreshToken();

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<RefreshTokenResponse>(
      `${this.apiUrl}/refresh`,
      { refreshToken }
    ).pipe(
      tap(response => {
        this.tokenService.setTokens(response.accessToken, response.refreshToken);
        this.isAuthenticatedSubject.next(true);
      }),
      catchError(error => {
        this.logout();
        return throwError(() => error);
      })
    );
  }

  logout(): void {
    const refreshToken = this.tokenService.getRefreshToken();

    if (refreshToken) {
      // Call logout endpoint (fire and forget)
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe({
        error: () => {} // Ignore errors
      });
    }

    this.clearAuthData();
    this.router.navigate(['/auth/login']);
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

  requestPasswordReset(email: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reset-password`, request);
  }

  verifyEmail(token: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/verify-email`, { token });
  }

  resendVerificationEmail(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/resend-verification`, {});
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

  private handleLoginResponse(response: LoginResponse): void {
    this.tokenService.setTokens(response.accessToken, response.refreshToken);

    // API response'dan gelen user data'sını düzenle
    const user = response.user;
    user.permissions = response.permissions || [];

    this.saveUserToStorage(user);
    this.savePermissionsToStorage(response.permissions || []);
    this.currentUserSubject.next(user);
    this.isAuthenticatedSubject.next(true);
  }

  private handleAuthError(error: any): Observable<never> {
    this.clearAuthData();
    return throwError(() => error);
  }

  private clearAuthData(): void {
    this.tokenService.clearTokens();
    localStorage.removeItem(this.userKey);
    localStorage.removeItem(this.permissionsKey);
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
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

  handleGoogleLoginSuccess(loginResponse: LoginResponse): void {
    this.handleLoginResponse(loginResponse);
  }
}