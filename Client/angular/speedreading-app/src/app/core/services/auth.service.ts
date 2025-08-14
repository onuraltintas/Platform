import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  LoginRequest, 
  RegisterRequest, 
  AuthResponse, 
  User, 
  ForgotPasswordRequest, 
  ResetPasswordRequest, 
  PasswordResetResponse 
} from '../../shared/models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/v1/auth`;
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadInitialUser();
  }

  private loadInitialUser(): void {
    const userStr = sessionStorage.getItem('user') ?? localStorage.getItem('user');
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        this.currentUserSubject.next(user);
      } catch {
        this.clearStoredUser();
      }
    }
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request, { 
      withCredentials: !!request.rememberMe 
    }).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.setTokensInternal(response.data.accessToken, response.data.refreshToken || null, !!request.rememberMe);
          this.storeUser(response.data.user, !!request.rememberMe);
          this.currentUserSubject.next(response.data.user);
        }
      })
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request);
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<PasswordResetResponse> {
    return this.http.post<PasswordResetResponse>(`${this.apiUrl}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<PasswordResetResponse> {
    return this.http.post<PasswordResetResponse>(`${this.apiUrl}/reset-password`, request);
  }

  logout(): Observable<any> {
    const refreshToken = this.getRefreshToken();
    
    this.clearStoredUser();
    this.removeTokens();
    this.currentUserSubject.next(null);

    const body = refreshToken ? { refreshToken } : null;
    return this.http.post<any>(`${this.apiUrl}/logout`, body, { withCredentials: true });
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, null, { 
      withCredentials: true 
    }).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.setTokensInternal(response.data.accessToken, response.data.refreshToken || null, true);
        }
      })
    );
  }

  getAccessToken(): string | null {
    return sessionStorage.getItem('accessToken') ?? localStorage.getItem('accessToken');
  }

  private getRefreshToken(): string | null {
    return sessionStorage.getItem('refreshToken') ?? localStorage.getItem('refreshToken');
  }

  isLoggedIn(): boolean {
    return !!this.getAccessToken();
  }

  getCurrentUser(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/user`, { withCredentials: true });
  }

  getCurrentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  clearLogoutFlag(): void {
    localStorage.removeItem('isLoggingOut');
  }

  googleLogin(): void {
    const authUrl = `https://accounts.google.com/o/oauth2/v2/auth?` +
                    `client_id=${environment.googleClientId}&` +
                    `redirect_uri=${encodeURIComponent(environment.backendGoogleRedirectUri)}&` +
                    `response_type=code&` +
                    `scope=openid%20profile%20email&` +
                    `access_type=offline&` +
                    `prompt=select_account&` +
                    `state=some_state_value`;

    console.log('Redirecting to Google OAuth URL:', authUrl);
    window.location.href = authUrl;
  }

  resendEmailConfirmation(email: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/resend-email-confirmation`, { email });
  }

  setTokens(accessToken: string, refreshToken: string | null, rememberMe: boolean): void {
    this.setTokensInternal(accessToken, refreshToken, rememberMe);
  }

  applyAuthenticatedUser(user: any, rememberMe: boolean): void {
    this.storeUser(user, rememberMe);
    this.currentUserSubject.next(user);
  }

  private setTokensInternal(accessToken: string, refreshToken: string | null, rememberMe: boolean): void {
    if (rememberMe) {
      localStorage.setItem('accessToken', accessToken);
      if (refreshToken) {
        localStorage.setItem('refreshToken', refreshToken);
      }
    } else {
      sessionStorage.setItem('accessToken', accessToken);
      if (refreshToken) {
        sessionStorage.setItem('refreshToken', refreshToken);
      }
    }
  }

  private removeTokens(): void {
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('refreshToken');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  private storeUser(user: User, rememberMe: boolean): void {
    const userStr = JSON.stringify(user);
    if (rememberMe) {
      localStorage.setItem('user', userStr);
      sessionStorage.removeItem('user');
    } else {
      sessionStorage.setItem('user', userStr);
      localStorage.removeItem('user');
    }
  }

  private clearStoredUser(): void {
    sessionStorage.removeItem('user');
    localStorage.removeItem('user');
  }
}