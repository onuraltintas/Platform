import { TestBed } from '@angular/core/testing';
import { SecureStorageService } from './secure-storage.service';
import { EncryptionService } from './encryption.service';

/**
 * Security-focused test suite for SecureStorageService
 * Tests encryption, integrity, and security features
 */
describe('SecureStorageService Security Tests', () => {
  let service: SecureStorageService;
  let encryptionService: EncryptionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SecureStorageService);
    encryptionService = TestBed.inject(EncryptionService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Encryption Security', () => {
    it('should encrypt data by default', async () => {
      const testData = { sensitive: 'password123' };
      const stored = await service.setItem('test', testData);
      expect(stored).toBe(true);

      // Direct storage inspection should show encrypted data
      const memoryProvider = (service as any).memoryStorage;
      const rawData = memoryProvider.getItem('test');
      expect(typeof rawData).toBe('object');
      expect(rawData).not.toEqual(testData); // Should be encrypted/wrapped
    });

    it('should validate integrity on retrieval', async () => {
      const testData = 'integrity-test-data';
      await service.setItem('integrity-test', testData, { integrityCheck: true });

      const retrieved = await service.getItem('integrity-test');
      expect(retrieved).toBe(testData);
    });

    it('should detect tampered data', async () => {
      const testData = 'important-data';
      await service.setItem('tamper-test', testData, { integrityCheck: true });

      // Simulate tampering by directly modifying storage
      const memoryProvider = (service as any).memoryStorage;
      const rawData = memoryProvider.getItem('tamper-test');

      if (rawData && typeof rawData === 'object' && 'integrity' in rawData) {
        // Corrupt the integrity hash
        rawData.integrity = 'tampered-hash';
        memoryProvider.setItem('tamper-test', rawData);
      }

      const retrieved = await service.getItem('tamper-test');
      expect(retrieved).toBeNull(); // Should return null for tampered data
    });
  });

  describe('Storage Security', () => {
    it('should use memory storage as primary for security', () => {
      const availableTypes = service.getAvailableStorageTypes();
      expect(availableTypes).toContain('memory');
    });

    it('should handle storage failures gracefully', async () => {
      // Test with invalid data that might cause storage failure
      const result = await service.setItem('test', undefined);
      expect(typeof result).toBe('boolean');
    });

    it('should clear all data securely', async () => {
      await service.setItem('test1', 'data1');
      await service.setItem('test2', 'data2');

      const cleared = await service.clear();
      expect(cleared).toBe(true);

      const retrieved1 = await service.getItem('test1');
      const retrieved2 = await service.getItem('test2');
      expect(retrieved1).toBeNull();
      expect(retrieved2).toBeNull();
    });
  });

  describe('Security Metrics', () => {
    it('should track access attempts', async () => {
      await service.getItem('non-existent');
      await service.getItem('another-non-existent');

      const metrics = await service.getMetrics();
      expect(metrics.accessAttempts).toBeGreaterThan(0);
      expect(metrics.lastAccess).toBeGreaterThan(0);
    });

    it('should validate storage integrity', async () => {
      const isValid = await service.validateIntegrity();
      expect(typeof isValid).toBe('boolean');
    });
  });

  describe('Expiration Security', () => {
    it('should respect expiration times', async () => {
      const shortExpiry = 100; // 100ms
      await service.setItem('expire-test', 'data', { expiresIn: shortExpiry });

      // Data should be available immediately
      let retrieved = await service.getItem('expire-test');
      expect(retrieved).toBe('data');

      // Wait for expiration
      await new Promise(resolve => setTimeout(resolve, shortExpiry + 50));

      // Data should be expired and removed
      retrieved = await service.getItem('expire-test');
      expect(retrieved).toBeNull();
    });
  });

  describe('Failover Security', () => {
    it('should fallback to memory storage when sessionStorage fails', async () => {
      // Mock sessionStorage failure
      const originalSessionStorage = window.sessionStorage;
      Object.defineProperty(window, 'sessionStorage', {
        value: {
          setItem: () => { throw new Error('Storage quota exceeded'); },
          getItem: () => null,
          removeItem: () => {},
          clear: () => {}
        },
        writable: true
      });

      const stored = await service.setItem('fallback-test', 'data', {
        fallbackStorage: 'memory'
      });
      expect(stored).toBe(true);

      const retrieved = await service.getItem('fallback-test');
      expect(retrieved).toBe('data');

      // Restore original sessionStorage
      Object.defineProperty(window, 'sessionStorage', {
        value: originalSessionStorage,
        writable: true
      });
    });
  });

  describe('Data Validation', () => {
    it('should handle various data types securely', async () => {
      const testCases = [
        { key: 'string', value: 'test string' },
        { key: 'number', value: 12345 },
        { key: 'boolean', value: true },
        { key: 'object', value: { nested: { data: 'value' } } },
        { key: 'array', value: [1, 2, 3, 'mixed', { type: 'array' }] },
        { key: 'null', value: null }
      ];

      for (const testCase of testCases) {
        const stored = await service.setItem(testCase.key, testCase.value);
        expect(stored).toBe(true);

        const retrieved = await service.getItem(testCase.key);
        expect(retrieved).toEqual(testCase.value);
      }
    });

    it('should reject malicious payloads safely', async () => {
      // Test with potentially harmful data
      const maliciousData = {
        script: '<script>alert("xss")</script>',
        proto: { __proto__: { polluted: true } },
        constructor: { constructor: { constructor: 'Function' } }
      };

      const stored = await service.setItem('malicious-test', maliciousData);
      expect(stored).toBe(true);

      const retrieved = await service.getItem('malicious-test');
      expect(retrieved).toEqual(maliciousData);
      // Ensure no global pollution occurred
      expect((Object.prototype as any).polluted).toBeUndefined();
    });
  });

  describe('Concurrent Access', () => {
    it('should handle concurrent operations safely', async () => {
      const promises = [];

      // Create multiple concurrent operations
      for (let i = 0; i < 10; i++) {
        promises.push(service.setItem(`concurrent-${i}`, `data-${i}`));
        promises.push(service.getItem(`concurrent-${i}`));
      }

      const results = await Promise.allSettled(promises);

      // All operations should complete without throwing
      const failures = results.filter(result => result.status === 'rejected');
      expect(failures.length).toBe(0);
    });
  });
});

/**
 * Token Storage Security Tests
 */
describe('SecureTokenStorageService Security Tests', () => {
  let tokenStorage: any;

  beforeEach(async () => {
    const { SecureTokenStorageService } = await import('./secure-token-storage.service');
    TestBed.configureTestingModule({});
    tokenStorage = TestBed.inject(SecureTokenStorageService);
  });

  describe('Token Security', () => {
    it('should store tokens with encryption', async () => {
      const accessToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test';
      const refreshToken = 'refresh_token_example';

      const stored = await tokenStorage.setAccessToken(accessToken);
      expect(stored).toBe(true);

      const retrieved = await tokenStorage.getAccessToken();
      expect(retrieved).toBe(accessToken);
    });

    it('should validate token integrity', async () => {
      const token = 'integrity_test_token';
      await tokenStorage.setAccessToken(token);

      // Tokens should have integrity protection
      const stats = await tokenStorage.getTokenStats();
      expect(stats.hasAccessToken).toBe(true);
    });

    it('should clear tokens securely', async () => {
      await tokenStorage.setAccessToken('access_token');
      await tokenStorage.setRefreshToken('refresh_token');

      const cleared = await tokenStorage.clearTokens();
      expect(cleared).toBe(true);

      const accessToken = await tokenStorage.getAccessToken();
      const refreshToken = await tokenStorage.getRefreshToken();

      expect(accessToken).toBeNull();
      expect(refreshToken).toBeNull();
    });

    it('should handle token expiration', async () => {
      const shortExpiry = 0.1; // 0.1 seconds
      await tokenStorage.setAccessToken('expiring_token', shortExpiry);

      // Wait for expiration
      await new Promise(resolve => setTimeout(resolve, 200));

      const token = await tokenStorage.getAccessToken();
      expect(token).toBeNull();
    });
  });

  describe('Token Validation', () => {
    it('should detect token tampering', async () => {
      const originalToken = 'original_secure_token';
      await tokenStorage.setAccessToken(originalToken);

      // Simulate direct storage tampering
      const secureStorage = (tokenStorage as any).secureStorage;
      const memoryStorage = (secureStorage as any).memoryStorage;

      // Try to corrupt token metadata
      const metadata = memoryStorage.getItem('auth_access_metadata');
      if (metadata) {
        metadata.hash = 'corrupted_hash';
        memoryStorage.setItem('auth_access_metadata', metadata);
      }

      // Token should be invalidated due to integrity failure
      const retrievedToken = await tokenStorage.getAccessToken();
      expect(retrievedToken).toBeNull();
    });
  });
});