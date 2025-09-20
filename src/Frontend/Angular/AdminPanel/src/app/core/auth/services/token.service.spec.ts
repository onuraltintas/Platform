import { TestBed } from '@angular/core/testing';
import { TokenService } from './token.service';

describe('TokenService', () => {
  let service: TokenService;
  let localStorageMock: any;

  beforeEach(() => {
    localStorageMock = {
      getItem: jest.fn(),
      setItem: jest.fn(),
      removeItem: jest.fn(),
      clear: jest.fn()
    };

    Object.defineProperty(window, 'localStorage', {
      value: localStorageMock
    });

    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenService);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAccessToken', () => {
    it('should return access token from localStorage', () => {
      const mockToken = 'mock-access-token';
      localStorageMock.getItem.mockReturnValue(mockToken);

      const result = service.getAccessToken();

      expect(localStorageMock.getItem).toHaveBeenCalledWith('platformv1_access_token');
      expect(result).toBe(mockToken);
    });

    it('should return null when no token in localStorage', () => {
      localStorageMock.getItem.mockReturnValue(null);

      const result = service.getAccessToken();

      expect(result).toBeNull();
    });
  });

  describe('getRefreshToken', () => {
    it('should return refresh token from localStorage', () => {
      const mockToken = 'mock-refresh-token';
      localStorageMock.getItem.mockReturnValue(mockToken);

      const result = service.getRefreshToken();

      expect(localStorageMock.getItem).toHaveBeenCalledWith('refresh_token');
      expect(result).toBe(mockToken);
    });
  });

  describe('setTokens', () => {
    it('should store both access and refresh tokens', () => {
      const accessToken = 'access-token';
      const refreshToken = 'refresh-token';

      service.setTokens(accessToken, refreshToken);

      expect(localStorageMock.setItem).toHaveBeenCalledWith('access_token', accessToken);
      expect(localStorageMock.setItem).toHaveBeenCalledWith('refresh_token', refreshToken);
    });

    it('should store only access token when refresh token is not provided', () => {
      const accessToken = 'access-token';

      service.setTokens(accessToken);

      expect(localStorageMock.setItem).toHaveBeenCalledWith('access_token', accessToken);
      expect(localStorageMock.setItem).toHaveBeenCalledTimes(1);
    });
  });

  describe('removeTokens', () => {
    it('should remove both tokens from localStorage', () => {
      service.removeTokens();

      expect(localStorageMock.removeItem).toHaveBeenCalledWith('access_token');
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('refresh_token');
    });
  });

  describe('isTokenExpired', () => {
    it('should return true for expired token', () => {
      // Create a token that expired 1 hour ago
      const expiredTime = Math.floor(Date.now() / 1000) - 3600;
      const expiredToken = `header.${btoa(JSON.stringify({ exp: expiredTime }))}.signature`;

      const result = service.isTokenExpired(expiredToken);

      expect(result).toBe(true);
    });

    it('should return false for valid token', () => {
      // Create a token that expires 1 hour from now
      const futureTime = Math.floor(Date.now() / 1000) + 3600;
      const validToken = `header.${btoa(JSON.stringify({ exp: futureTime }))}.signature`;

      const result = service.isTokenExpired(validToken);

      expect(result).toBe(false);
    });

    it('should return true for invalid token format', () => {
      const invalidToken = 'invalid-token';

      const result = service.isTokenExpired(invalidToken);

      expect(result).toBe(true);
    });

    it('should return true for null token', () => {
      const result = service.isTokenExpired(null);

      expect(result).toBe(true);
    });
  });

  describe('getTokenPayload', () => {
    it('should return decoded payload for valid token', () => {
      const payload = { userId: '123', email: 'test@test.com' };
      const token = `header.${btoa(JSON.stringify(payload))}.signature`;

      const result = service.getTokenPayload(token);

      expect(result).toEqual(payload);
    });

    it('should return null for invalid token', () => {
      const invalidToken = 'invalid-token';

      const result = service.getTokenPayload(invalidToken);

      expect(result).toBeNull();
    });

    it('should return null for null token', () => {
      const result = service.getTokenPayload(null);

      expect(result).toBeNull();
    });
  });

  describe('hasValidAccessToken', () => {
    it('should return true when access token exists and is not expired', () => {
      const futureTime = Math.floor(Date.now() / 1000) + 3600;
      const validToken = `header.${btoa(JSON.stringify({ exp: futureTime }))}.signature`;
      localStorageMock.getItem.mockReturnValue(validToken);

      const result = service.hasValidAccessToken();

      expect(result).toBe(true);
    });

    it('should return false when access token is expired', () => {
      const expiredTime = Math.floor(Date.now() / 1000) - 3600;
      const expiredToken = `header.${btoa(JSON.stringify({ exp: expiredTime }))}.signature`;
      localStorageMock.getItem.mockReturnValue(expiredToken);

      const result = service.hasValidAccessToken();

      expect(result).toBe(false);
    });

    it('should return false when no access token exists', () => {
      localStorageMock.getItem.mockReturnValue(null);

      const result = service.hasValidAccessToken();

      expect(result).toBe(false);
    });
  });

  describe('getTimeToExpiry', () => {
    it('should return time to expiry in seconds', () => {
      const futureTime = Math.floor(Date.now() / 1000) + 3600; // 1 hour from now
      const token = `header.${btoa(JSON.stringify({ exp: futureTime }))}.signature`;

      const result = service.getTimeToExpiry(token);

      expect(result).toBeGreaterThan(3590); // Should be close to 3600 seconds
      expect(result).toBeLessThanOrEqual(3600);
    });

    it('should return 0 for expired token', () => {
      const expiredTime = Math.floor(Date.now() / 1000) - 3600;
      const expiredToken = `header.${btoa(JSON.stringify({ exp: expiredTime }))}.signature`;

      const result = service.getTimeToExpiry(expiredToken);

      expect(result).toBe(0);
    });

    it('should return 0 for invalid token', () => {
      const result = service.getTimeToExpiry('invalid-token');

      expect(result).toBe(0);
    });
  });
});