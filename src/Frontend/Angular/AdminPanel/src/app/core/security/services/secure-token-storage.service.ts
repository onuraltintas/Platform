import { Injectable, inject } from '@angular/core';
import { ITokenStorage, TokenMetadata, StorageType } from '../interfaces/storage-security.interface';
import { SecureStorageService } from './secure-storage.service';
import { EncryptionService } from './encryption.service';
import { environment } from '../../../../environments/environment';

/**
 * Secure token storage service
 * Implements enterprise-grade token management with encryption and security controls
 */
@Injectable({
  providedIn: 'root'
})
export class SecureTokenStorageService implements ITokenStorage {
  private readonly secureStorage = inject(SecureStorageService);
  private readonly encryptionService = inject(EncryptionService);

  private readonly TOKEN_KEYS = {
    ACCESS_TOKEN: 'auth_access_token',
    REFRESH_TOKEN: 'auth_refresh_token',
    ACCESS_METADATA: 'auth_access_metadata',
    REFRESH_METADATA: 'auth_refresh_metadata'
  };

  private readonly DEFAULT_SECURITY_LEVEL = 'critical';

  /**
   * Store access token securely
   */
  async setAccessToken(token: string, expiresIn?: number): Promise<boolean> {
    try {
      const expirationTime = expiresIn ? Date.now() + (expiresIn * 1000) : undefined;

      // Create token metadata
      const metadata: TokenMetadata = {
        type: 'access',
        createdAt: Date.now(),
        expiresAt: expirationTime || (Date.now() + environment.auth.tokenExpiry),
        hash: await this.encryptionService.generateIntegrityHash(token),
        securityLevel: this.DEFAULT_SECURITY_LEVEL
      };

      // Store token and metadata separately for enhanced security
      const tokenStored = this.secureStorage.setItem(
        this.TOKEN_KEYS.ACCESS_TOKEN,
        token,
        {
          encrypt: true,
          expiry: expirationTime ? expirationTime - Date.now() : environment.auth.tokenExpiry
        }
      );

      const metadataStored = this.secureStorage.setItem(
        this.TOKEN_KEYS.ACCESS_METADATA,
        metadata,
        {
          encrypt: false, // Metadata doesn't need encryption
          expiry: expirationTime ? expirationTime - Date.now() : environment.auth.tokenExpiry
        }
      );

      return tokenStored && metadataStored;
    } catch (error) {
      console.error('Failed to store access token:', error);
      return false;
    }
  }

  /**
   * Store refresh token securely
   */
  async setRefreshToken(token: string, expiresIn?: number): Promise<boolean> {
    try {
      const expirationTime = expiresIn ? Date.now() + (expiresIn * 1000) : undefined;

      // Create token metadata
      const metadata: TokenMetadata = {
        type: 'refresh',
        createdAt: Date.now(),
        expiresAt: expirationTime || (Date.now() + environment.auth.refreshExpiry),
        hash: await this.encryptionService.generateIntegrityHash(token),
        securityLevel: this.DEFAULT_SECURITY_LEVEL
      };

      // Store token and metadata
      const tokenStored = this.secureStorage.setItem(
        this.TOKEN_KEYS.REFRESH_TOKEN,
        token,
        {
          encrypt: true,
          expiry: expirationTime ? expirationTime - Date.now() : environment.auth.refreshExpiry
        }
      );

      const metadataStored = this.secureStorage.setItem(
        this.TOKEN_KEYS.REFRESH_METADATA,
        metadata,
        {
          encrypt: false,
          expiry: expirationTime ? expirationTime - Date.now() : environment.auth.refreshExpiry
        }
      );

      return tokenStored && metadataStored;
    } catch (error) {
      console.error('Failed to store refresh token:', error);
      return false;
    }
  }

  /**
   * Get access token securely
   */
  async getAccessToken(): Promise<string | null> {
    try {
      // Get token and metadata
      const token = this.secureStorage.getItem(this.TOKEN_KEYS.ACCESS_TOKEN) as string;
      const metadata = this.secureStorage.getItem(this.TOKEN_KEYS.ACCESS_METADATA) as TokenMetadata;

      if (!token || !metadata) {
        return null;
      }

      // Validate token expiration
      if (metadata.expiresAt && Date.now() > metadata.expiresAt) {
        await this.removeAccessToken();
        return null;
      }

      // Validate token integrity
      const currentHash = await this.encryptionService.generateIntegrityHash(token);
      if (currentHash !== metadata.hash) {
        console.warn('Access token integrity check failed');
        await this.removeAccessToken();
        return null;
      }

      return token;
    } catch (error) {
      console.error('Failed to retrieve access token:', error);
      return null;
    }
  }

  /**
   * Get refresh token securely
   */
  async getRefreshToken(): Promise<string | null> {
    try {
      // Get token and metadata
      const token = this.secureStorage.getItem(this.TOKEN_KEYS.REFRESH_TOKEN) as string;
      const metadata = this.secureStorage.getItem(this.TOKEN_KEYS.REFRESH_METADATA) as TokenMetadata;

      if (!token || !metadata) {
        return null;
      }

      // Validate token expiration
      if (metadata.expiresAt && Date.now() > metadata.expiresAt) {
        await this.removeRefreshToken();
        return null;
      }

      // Validate token integrity
      const currentHash = await this.encryptionService.generateIntegrityHash(token);
      if (currentHash !== metadata.hash) {
        console.warn('Refresh token integrity check failed');
        await this.removeRefreshToken();
        return null;
      }

      return token;
    } catch (error) {
      console.error('Failed to retrieve refresh token:', error);
      return null;
    }
  }

  /**
   * Clear all tokens
   */
  async clearTokens(): Promise<boolean> {
    try {
      const results = await Promise.all([
        this.removeAccessToken(),
        this.removeRefreshToken()
      ]);

      return results.every(result => result);
    } catch (error) {
      console.error('Failed to clear tokens:', error);
      return false;
    }
  }

  /**
   * Check if tokens are valid and not expired
   */
  async areTokensValid(): Promise<boolean> {
    try {
      const [accessToken, refreshToken] = await Promise.all([
        this.getAccessToken(),
        this.getRefreshToken()
      ]);

      return accessToken !== null || refreshToken !== null;
    } catch (error) {
      console.error('Failed to validate tokens:', error);
      return false;
    }
  }

  /**
   * Get token expiration information
   */
  async getTokenExpiration(): Promise<{ accessToken?: number; refreshToken?: number }> {
    try {
      const accessMetadata = this.secureStorage.getItem(this.TOKEN_KEYS.ACCESS_METADATA) as TokenMetadata;
      const refreshMetadata = this.secureStorage.getItem(this.TOKEN_KEYS.REFRESH_METADATA) as TokenMetadata;

      return {
        accessToken: accessMetadata?.expiresAt,
        refreshToken: refreshMetadata?.expiresAt
      };
    } catch (error) {
      console.error('Failed to get token expiration:', error);
      return {};
    }
  }

  /**
   * Check if access token is close to expiration
   */
  async shouldRefreshAccessToken(): Promise<boolean> {
    try {
      const metadata = this.secureStorage.getItem(this.TOKEN_KEYS.ACCESS_METADATA) as TokenMetadata;

      if (!metadata || !metadata.expiresAt) {
        return true; // Refresh if no metadata or expiration
      }

      const timeUntilExpiry = metadata.expiresAt - Date.now();
      return timeUntilExpiry <= environment.auth.refreshBeforeExpiry;
    } catch (error) {
      console.error('Failed to check token refresh requirement:', error);
      return true;
    }
  }

  /**
   * Check if refresh token is valid
   */
  async isRefreshTokenValid(): Promise<boolean> {
    try {
      const metadata = this.secureStorage.getItem(this.TOKEN_KEYS.REFRESH_METADATA) as TokenMetadata;

      if (!metadata || !metadata.expiresAt) {
        return false;
      }

      return Date.now() < metadata.expiresAt;
    } catch (error) {
      console.error('Failed to validate refresh token:', error);
      return false;
    }
  }

  /**
   * Get token storage statistics
   */
  async getTokenStats(): Promise<{
    hasAccessToken: boolean;
    hasRefreshToken: boolean;
    accessTokenAge?: number;
    refreshTokenAge?: number;
    accessTokenExpiresIn?: number;
    refreshTokenExpiresIn?: number;
  }> {
    try {
      const accessMetadata = this.secureStorage.getItem(this.TOKEN_KEYS.ACCESS_METADATA) as TokenMetadata;
      const refreshMetadata = this.secureStorage.getItem(this.TOKEN_KEYS.REFRESH_METADATA) as TokenMetadata;

      const now = Date.now();

      return {
        hasAccessToken: accessMetadata !== null,
        hasRefreshToken: refreshMetadata !== null,
        accessTokenAge: accessMetadata ? now - accessMetadata.createdAt : undefined,
        refreshTokenAge: refreshMetadata ? now - refreshMetadata.createdAt : undefined,
        accessTokenExpiresIn: accessMetadata?.expiresAt ? Math.max(0, accessMetadata.expiresAt - now) : undefined,
        refreshTokenExpiresIn: refreshMetadata?.expiresAt ? Math.max(0, refreshMetadata.expiresAt - now) : undefined
      };
    } catch (error) {
      console.error('Failed to get token stats:', error);
      return {
        hasAccessToken: false,
        hasRefreshToken: false
      };
    }
  }

  // Implement ISecureStorage interface methods by delegating to secureStorage

  async setItem<T>(key: string, value: T, options?: any): Promise<boolean> {
    return Promise.resolve(this.secureStorage.setItem(key, value, options));
  }

  async getItem<T>(key: string): Promise<T | null> {
    return Promise.resolve(this.secureStorage.getItem(key) as T | null);
  }

  async removeItem(key: string): Promise<boolean> {
    return Promise.resolve(this.secureStorage.removeItem(key));
  }

  async clear(): Promise<boolean> {
    return this.clearTokens();
  }

  async hasItem(key: string): Promise<boolean> {
    return Promise.resolve(this.secureStorage.hasItem(key));
  }

  async getMetrics(): Promise<any> {
    // Return default metrics since SecureStorageService doesn't expose them publicly
    return {
      operations: 0,
      errors: 0,
      cacheHits: 0,
      size: 0
    };
  }

  async validateIntegrity(): Promise<boolean> {
    // Simple integrity check - verify that key tokens exist
    try {
      const accessToken = this.secureStorage.getItem(this.TOKEN_KEYS.ACCESS_TOKEN);
      const refreshToken = this.secureStorage.getItem(this.TOKEN_KEYS.REFRESH_TOKEN);
      return accessToken !== null || refreshToken !== null;
    } catch {
      return false;
    }
  }

  getAvailableStorageTypes(): StorageType[] {
    // Return available storage types
    return ['localStorage', 'sessionStorage', 'memory'];
  }

  /**
   * Remove access token and its metadata
   */
  private async removeAccessToken(): Promise<boolean> {
    const accessTokenResult = this.secureStorage.removeItem(this.TOKEN_KEYS.ACCESS_TOKEN);
    const metadataResult = this.secureStorage.removeItem(this.TOKEN_KEYS.ACCESS_METADATA);

    return accessTokenResult && metadataResult;
  }

  /**
   * Remove refresh token and its metadata
   */
  private async removeRefreshToken(): Promise<boolean> {
    const refreshTokenResult = this.secureStorage.removeItem(this.TOKEN_KEYS.REFRESH_TOKEN);
    const metadataResult = this.secureStorage.removeItem(this.TOKEN_KEYS.REFRESH_METADATA);

    return refreshTokenResult && metadataResult;
  }
}