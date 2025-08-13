import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap, BehaviorSubject } from 'rxjs';
import { DOCUMENT } from '@angular/common';
import { LoginRequest, AuthResponse, ForgotPasswordRequest, ResetPasswordRequest, PasswordResetResponse, RegisterRequest, EmailConfirmationResponse, ConfirmEmailRequest } from '../models/auth.models';
import { environment } from '../../environments/environment';
import { UserProfileDto } from '../models/user.models';
import { UsersService } from '../services/users.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // API Gateway v1 altında kimlik rotası
  private apiUrl = `${environment.apiUrl}/v1/auth`;
  private currentUserSubject = new BehaviorSubject<any | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    @Inject(DOCUMENT) private document: Document,
    private usersService: UsersService
  ) {
    this.loadInitialUser();
  }

  private loadInitialUser(): void {
    const userStr = sessionStorage.getItem('user') ?? localStorage.getItem('user');
    if (userStr) {
      try {
        const parsedUser = JSON.parse(userStr);
        this.currentUserSubject.next(parsedUser);
        // Kullanıcı zaten varsa, avatar ve claim'ler ile zenginleştir
        this.enrichUserWithProfile(parsedUser);
        this.enrichUserWithClaims();
      } catch (e) {
        sessionStorage.removeItem('user');
        localStorage.removeItem('user');
        this.currentUserSubject.next(null);
      }
    } else {
      // Access token yoksa ve backend tarafında remember-me çerezi varsa cookie tabanlı yenilemeyi dene
      const access = this.getAccessToken();
      if (!access) {
        this.tryCookieBasedRefresh();
      }
    }
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request, { withCredentials: !!request.rememberMe }).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.setTokens(response.data.accessToken, response.data.refreshToken, !!request.rememberMe);
          this.storeUser(response.data.user, !!request.rememberMe);
          this.currentUserSubject.next(response.data.user);
          // Girişten sonra profil ve claim'leri uygula
          this.enrichUserWithProfile(response.data.user);
          this.enrichUserWithClaims();
        }
      })
    );
  }

  googleLogin(): void {
    try {
      const authUrl = `https://accounts.google.com/o/oauth2/v2/auth?` +
                      `client_id=${environment.googleClientId}&` +
                      `redirect_uri=${encodeURIComponent(environment.backendGoogleRedirectUri)}&` +
                      `response_type=code&` +
                      `scope=openid%20profile%20email&` +
                      `access_type=offline&` +
                      `prompt=select_account&` +
                      `state=some_state_value`;

      console.log('Redirecting to Google OAuth URL:', authUrl);

      // Force immediate redirect
      this.document.location.href = authUrl;

    } catch (error) {
      console.error('Error during Google login redirect:', error);
    }
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<PasswordResetResponse> {
    return this.http.post<PasswordResetResponse>(`${this.apiUrl}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<PasswordResetResponse> {
    return this.http.post<PasswordResetResponse>(`${this.apiUrl}/reset-password`, request);
  }

  setTokens(accessToken: string, refreshToken: string | null, rememberMe: boolean): void {
    if (rememberMe) {
      localStorage.setItem('accessToken', accessToken);
    } else {
      sessionStorage.setItem('accessToken', accessToken);
    }
    if (refreshToken) {
      if (rememberMe) {
        // HttpOnly cookie kullanılacağı için refresh token'ı depolamaya gerek yok
      } else {
        sessionStorage.setItem('refreshToken', refreshToken);
      }
    }
  }

  logout(): Observable<any> {
    // Set logout flag before clearing anything
    localStorage.setItem('isLoggingOut', 'true');
    
    const refreshToken = this.getRefreshToken();

    // Immediately clear local session to update UI
    this.removeTokens();
    sessionStorage.removeItem('user');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);

    const body = refreshToken ? { refreshToken } : null;
    return this.http.post<any>(`${this.apiUrl}/logout`, body, { withCredentials: true });
  }

  isCurrentlyLoggingOut(): boolean {
    return localStorage.getItem('isLoggingOut') === 'true';
  }

  clearLogoutFlag(): void {
    localStorage.removeItem('isLoggingOut');
  }

  private removeTokens(): void {
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('refreshToken');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  getAccessToken(): string | null {
    return sessionStorage.getItem('accessToken') ?? localStorage.getItem('accessToken');
  }

  updateCurrentUser(profileData: Partial<UserProfileDto>): void {
    const currentUser = this.currentUserSubject.getValue();
    if (currentUser) {
      const updatedUser = { ...currentUser, ...profileData };
      this.currentUserSubject.next(updatedUser);
      if (sessionStorage.getItem('user')) {
        sessionStorage.setItem('user', JSON.stringify(updatedUser));
      } else {
        localStorage.setItem('user', JSON.stringify(updatedUser));
      }
    }
  }

  /**
   * Başarılı kimlik doğrulama sonrası kullanıcıyı yayınlar ve uygun depoda saklar.
   */
  public applyAuthenticatedUser(user: any, rememberMe: boolean): void {
    this.storeUser(user, rememberMe);
    this.currentUserSubject.next(user);
    this.enrichUserWithProfile(user);
  }

  /**
   * Kullanıcının profilini çekerek (özellikle avatar alanını) mevcut kullanıcı verisine uygular.
   */
  private enrichUserWithProfile(user: any): void {
    try {
      const userId: string | undefined = user?.id || user?.userId;
      if (!userId) return;
      this.usersService.getProfile(userId).subscribe({
        next: (profile: UserProfileDto) => {
          if (profile?.avatar) {
            this.updateCurrentUser({ avatar: profile.avatar });
          }
        },
        error: () => {
          // Sessiz geç: profil yoksa veya hata varsa avatar güncellenmez
        }
      });
    } catch {
      // Sessiz geç
    }
  }

  private tryCookieBasedRefresh(): void {
    this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, null, { withCredentials: true }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.setTokens(response.data.accessToken, response.data.refreshToken, true);
          this.storeUser(response.data.user, true);
          this.currentUserSubject.next(response.data.user);
          this.enrichUserWithProfile(response.data.user);
          this.enrichUserWithClaims();
        }
      },
      error: () => {
        // Sessiz geç
      }
    });
  }

  private storeUser(user: any, rememberMe: boolean): void {
    const payload = JSON.stringify(user);
    if (rememberMe) {
      localStorage.setItem('user', payload);
      sessionStorage.removeItem('user');
    } else {
      sessionStorage.setItem('user', payload);
      localStorage.removeItem('user');
    }
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem('refreshToken') ?? localStorage.getItem('refreshToken');
  }

  isLoggedIn(): boolean {
    return !!this.getAccessToken();
  }

  getCurrentUser(): Observable<any> {
    // Extract user info from JWT token instead of API call
    const accessToken = this.getAccessToken();

    if (!accessToken) {
      return of({ success: false, message: 'No access token found' });
    }

    try {
      // Decode JWT token
      const payload = this.decodeJWT(accessToken);

      const userData = {
        id: payload.uid || payload.sub,
        userName: payload.username || payload.email,
        email: payload.email,
        firstName: payload.given_name || 'Google',
        lastName: payload.surname || 'User',
        fullName: `${payload.given_name || 'Google'} ${payload.surname || 'User'}`,
        phoneNumber: payload.phone_number || null,
        isActive: payload.is_active === 'true' || true,
        isEmailConfirmed: payload.email_confirmed === 'true' || true,
        roles: this.extractArrayClaim(payload, 'role') || ['User'],
        categories: this.extractArrayClaim(payload, 'category') || [],
        permissions: this.extractArrayClaim(payload, 'permission') || []
      };

      return of({
        success: true,
        data: userData,
        message: 'User info retrieved from token'
      });
    } catch (error) {
      console.error('Error decoding JWT token:', error);
      return of({ success: false, message: 'Failed to decode token' });
    }
  }

  private decodeJWT(token: string): any {
    try {
      const payload = token.split('.')[1];
      const decoded = JSON.parse(atob(payload));
      return decoded;
    } catch (error) {
      throw new Error('Invalid JWT token');
    }
  }

  private extractArrayClaim(payload: any, claimName: string): string[] {
    const value = payload[claimName];
    if (Array.isArray(value)) {
      return value;
    } else if (typeof value === 'string') {
      return [value];
    }
    return [];
  }

  private getClaimsFromAccessToken(): { roles: string[]; permissions: string[]; categories: string[] } | null {
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      const payload = this.decodeJWT(token);
      return {
        roles: this.extractArrayClaim(payload, 'role') || [],
        permissions: this.extractArrayClaim(payload, 'permission') || [],
        categories: this.extractArrayClaim(payload, 'category') || []
      };
    } catch {
      return null;
    }
  }

  private enrichUserWithClaims(): void {
    const current = this.currentUserSubject.getValue();
    const claims = this.getClaimsFromAccessToken();
    if (!current || !claims) return;
    const merged = {
      ...current,
      roles: (current.roles && current.roles.length > 0) ? current.roles : claims.roles,
      permissions: (current.permissions && current.permissions.length > 0) ? current.permissions : claims.permissions,
      categories: (current.categories && current.categories.length > 0) ? current.categories : claims.categories,
    };
    this.currentUserSubject.next(merged);
    if (sessionStorage.getItem('user')) {
      sessionStorage.setItem('user', JSON.stringify(merged));
    } else if (localStorage.getItem('user')) {
      localStorage.setItem('user', JSON.stringify(merged));
    }
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request);
  }

  confirmEmail(request: ConfirmEmailRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/confirm-email`, request);
  }

  resendEmailConfirmation(email: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/resend-email-confirmation`, { email });
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, null, { withCredentials: true }).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.setTokens(response.data.accessToken, response.data.refreshToken, true);
          this.enrichUserWithClaims();
        }
      })
    );
  }

  logoutAll(): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/logout-all`, {});
  }
}