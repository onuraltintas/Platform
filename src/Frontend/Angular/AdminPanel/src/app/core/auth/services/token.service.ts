import { Injectable, inject } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../../../environments/environment';
import { TokenPayload } from '../models/auth.models';
import { SecureTokenStorageService } from '../../security/services/secure-token-storage.service';

/**
 * Secure Token Service
 * Refactored to use enterprise-grade secure storage with encryption
 */
@Injectable({
  providedIn: 'root'
})
export class TokenService {
  private readonly secureTokenStorage = inject(SecureTokenStorageService);

  /**
   * Get access token securely
   */
  getAccessToken(): Promise<string | null> {
    return this.secureTokenStorage.getAccessToken();
  }

  /**
   * Get refresh token securely
   */
  getRefreshToken(): Promise<string | null> {
    return this.secureTokenStorage.getRefreshToken();
  }

  /**
   * Store tokens securely with encryption
   */
  async setTokens(accessToken: string, refreshToken: string, expiresIn?: number): Promise<boolean> {
    try {
      const [accessResult, refreshResult] = await Promise.all([
        this.secureTokenStorage.setAccessToken(accessToken, expiresIn),
        this.secureTokenStorage.setRefreshToken(refreshToken)
      ]);

      return accessResult && refreshResult;
    } catch (error) {
      console.error('Failed to store tokens securely:', error);
      return false;
    }
  }

  /**
   * Clear all tokens securely
   */
  async clearTokens(): Promise<boolean> {
    try {
      return await this.secureTokenStorage.clearTokens();
    } catch (error) {
      console.error('Failed to clear tokens securely:', error);
      return false;
    }
  }

  /**
   * Check if access token is valid and not expired
   */
  async isTokenValid(): Promise<boolean> {
    try {
      const token = await this.getAccessToken();
      if (!token) return false;

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

  /**
   * Check if refresh token is valid and not expired
   */
  async isRefreshTokenValid(): Promise<boolean> {
    try {
      return await this.secureTokenStorage.isRefreshTokenValid();
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

  /**
   * Get token payload from access token
   */
  async getTokenPayload(): Promise<TokenPayload | null> {
    try {
      const token = await this.getAccessToken();
      return token ? this.decodeToken(token) : null;
    } catch {
      return null;
    }
  }

  /**
   * Get user ID from token
   */
  async getUserId(): Promise<string | null> {
    try {
      const payload = await this.getTokenPayload();
      return payload?.sub || null;
    } catch {
      return null;
    }
  }

  /**
   * Get user email from token
   */
  async getUserEmail(): Promise<string | null> {
    try {
      const payload = await this.getTokenPayload();
      return payload?.email || null;
    } catch {
      return null;
    }
  }

  /**
   * Get user permissions from token
   */
  async getUserPermissions(): Promise<string[]> {
    try {
      const payload = await this.getTokenPayload();
      return payload?.permissions || [];
    } catch {
      return [];
    }
  }

  /**
   * Get user roles from token
   */
  async getUserRoles(): Promise<string[]> {
    try {
      const payload = await this.getTokenPayload();
      return payload?.roles || [];
    } catch {
      return [];
    }
  }

  /**
   * Get token expiration time
   */
  async getTokenExpirationTime(): Promise<number | null> {
    try {
      const payload = await this.getTokenPayload();
      return payload?.exp ? payload.exp * 1000 : null;
    } catch {
      return null;
    }
  }

  /**
   * Check if token should be refreshed
   */
  async shouldRefreshToken(): Promise<boolean> {
    try {
      return await this.secureTokenStorage.shouldRefreshAccessToken();
    } catch {
      return false;
    }
  }

  /**
   * Get comprehensive token statistics
   */
  async getTokenStats(): Promise<Record<string, unknown>> {
    try {
      return await this.secureTokenStorage.getTokenStats();
    } catch (error) {
      console.error('Failed to get token stats:', error);
      return {
        hasAccessToken: false,
        hasRefreshToken: false
      };
    }
  }

  /**
   * Validate token storage integrity
   */
  async validateTokenIntegrity(): Promise<boolean> {
    try {
      return await this.secureTokenStorage.validateIntegrity();
    } catch {
      return false;
    }
  }
}