import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../../../environments/environment';
import { TokenPayload } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class TokenService {
  private readonly tokenKey = environment.auth.tokenKey;
  private readonly refreshKey = environment.auth.refreshKey;
  private readonly storagePrefix = environment.storage.prefix;

  getAccessToken(): string | null {
    return localStorage.getItem(this.storagePrefix + this.tokenKey);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.storagePrefix + this.refreshKey);
  }

  setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(this.storagePrefix + this.tokenKey, accessToken);
    localStorage.setItem(this.storagePrefix + this.refreshKey, refreshToken);
  }

  clearTokens(): void {
    localStorage.removeItem(this.storagePrefix + this.tokenKey);
    localStorage.removeItem(this.storagePrefix + this.refreshKey);
  }

  isTokenValid(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;

    try {
      const payload = this.decodeToken(token);
      if (!payload || !payload.exp) return false;

      const expirationTime = payload.exp * 1000;
      const currentTime = Date.now();
      const bufferTime = environment.auth.refreshBeforeExpiry;

      return currentTime < (expirationTime - bufferTime);
    } catch {
      return false;
    }
  }

  isRefreshTokenValid(): boolean {
    const token = this.getRefreshToken();
    if (!token) return false;

    try {
      const payload = this.decodeToken(token);
      if (!payload || !payload.exp) return false;

      const expirationTime = payload.exp * 1000;
      const currentTime = Date.now();

      return currentTime < expirationTime;
    } catch {
      return false;
    }
  }

  decodeToken(token: string): TokenPayload | null {
    try {
      return jwtDecode<TokenPayload>(token);
    } catch {
      return null;
    }
  }

  getTokenPayload(): TokenPayload | null {
    const token = this.getAccessToken();
    return token ? this.decodeToken(token) : null;
  }

  getUserId(): string | null {
    const payload = this.getTokenPayload();
    return payload?.sub || null;
  }

  getUserEmail(): string | null {
    const payload = this.getTokenPayload();
    return payload?.email || null;
  }

  getUserPermissions(): string[] {
    const payload = this.getTokenPayload();
    return payload?.permissions || [];
  }

  getUserRoles(): string[] {
    const payload = this.getTokenPayload();
    return payload?.roles || [];
  }

  getTokenExpirationTime(): number | null {
    const payload = this.getTokenPayload();
    return payload?.exp ? payload.exp * 1000 : null;
  }

  shouldRefreshToken(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;

    const expirationTime = this.getTokenExpirationTime();
    if (!expirationTime) return false;

    const currentTime = Date.now();
    const bufferTime = environment.auth.refreshBeforeExpiry;

    return currentTime >= (expirationTime - bufferTime) && this.isRefreshTokenValid();
  }
}