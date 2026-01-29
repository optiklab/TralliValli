import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import {
  storeTokens,
  getTokens,
  getAccessToken,
  getRefreshToken,
  isAccessTokenExpired,
  isRefreshTokenExpired,
  clearTokens,
  isAuthenticated,
} from './tokenStorage';

describe('Token Storage Utility', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('storeTokens', () => {
    it('should store tokens with string dates', () => {
      const tokens = {
        accessToken: 'access-token-123',
        refreshToken: 'refresh-token-456',
        expiresAt: new Date('2024-12-31T23:59:59Z').toISOString(),
        refreshExpiresAt: new Date('2025-01-31T23:59:59Z').toISOString(),
      };

      storeTokens(tokens);

      expect(localStorage.getItem('tralli_valli_access_token')).toBe('access-token-123');
      expect(localStorage.getItem('tralli_valli_refresh_token')).toBe('refresh-token-456');
      expect(localStorage.getItem('tralli_valli_token_expiry')).toBe(
        '2024-12-31T23:59:59.000Z'
      );
      expect(localStorage.getItem('tralli_valli_refresh_expiry')).toBe(
        '2025-01-31T23:59:59.000Z'
      );
    });

    it('should store tokens with Date objects', () => {
      const tokens = {
        accessToken: 'access-token-123',
        refreshToken: 'refresh-token-456',
        expiresAt: new Date('2024-12-31T23:59:59Z'),
        refreshExpiresAt: new Date('2025-01-31T23:59:59Z'),
      };

      storeTokens(tokens);

      expect(localStorage.getItem('tralli_valli_access_token')).toBe('access-token-123');
      expect(localStorage.getItem('tralli_valli_refresh_token')).toBe('refresh-token-456');
    });
  });

  describe('getTokens', () => {
    it('should retrieve stored tokens', () => {
      const tokens = {
        accessToken: 'access-token-123',
        refreshToken: 'refresh-token-456',
        expiresAt: new Date('2024-12-31T23:59:59Z').toISOString(),
        refreshExpiresAt: new Date('2025-01-31T23:59:59Z').toISOString(),
      };

      storeTokens(tokens);
      const retrieved = getTokens();

      expect(retrieved).not.toBeNull();
      expect(retrieved?.accessToken).toBe('access-token-123');
      expect(retrieved?.refreshToken).toBe('refresh-token-456');
      expect(retrieved?.expiresAt).toBeInstanceOf(Date);
      expect(retrieved?.refreshExpiresAt).toBeInstanceOf(Date);
    });

    it('should return null if tokens are not stored', () => {
      const retrieved = getTokens();
      expect(retrieved).toBeNull();
    });

    it('should return null if any token is missing', () => {
      localStorage.setItem('tralli_valli_access_token', 'access-token');
      // Missing other tokens
      
      const retrieved = getTokens();
      expect(retrieved).toBeNull();
    });

    it('should return null if retrieval fails', () => {
      const mockGetItem = vi.spyOn(Storage.prototype, 'getItem');
      mockGetItem.mockImplementation(() => {
        throw new Error('Storage error');
      });

      const retrieved = getTokens();
      expect(retrieved).toBeNull();

      mockGetItem.mockRestore();
    });
  });

  describe('getAccessToken', () => {
    it('should retrieve access token', () => {
      localStorage.setItem('tralli_valli_access_token', 'access-token-123');
      
      const token = getAccessToken();
      expect(token).toBe('access-token-123');
    });

    it('should return null if not stored', () => {
      const token = getAccessToken();
      expect(token).toBeNull();
    });

    it('should return null if retrieval fails', () => {
      const mockGetItem = vi.spyOn(Storage.prototype, 'getItem');
      mockGetItem.mockImplementation(() => {
        throw new Error('Storage error');
      });

      const token = getAccessToken();
      expect(token).toBeNull();

      mockGetItem.mockRestore();
    });
  });

  describe('getRefreshToken', () => {
    it('should retrieve refresh token', () => {
      localStorage.setItem('tralli_valli_refresh_token', 'refresh-token-456');
      
      const token = getRefreshToken();
      expect(token).toBe('refresh-token-456');
    });

    it('should return null if not stored', () => {
      const token = getRefreshToken();
      expect(token).toBeNull();
    });

    it('should return null if retrieval fails', () => {
      const mockGetItem = vi.spyOn(Storage.prototype, 'getItem');
      mockGetItem.mockImplementation(() => {
        throw new Error('Storage error');
      });

      const token = getRefreshToken();
      expect(token).toBeNull();

      mockGetItem.mockRestore();
    });
  });

  describe('isAccessTokenExpired', () => {
    it('should return false if token is not expired', () => {
      const futureDate = new Date(Date.now() + 3600000); // 1 hour from now
      localStorage.setItem('tralli_valli_token_expiry', futureDate.toISOString());

      const expired = isAccessTokenExpired();
      expect(expired).toBe(false);
    });

    it('should return true if token is expired', () => {
      const pastDate = new Date(Date.now() - 3600000); // 1 hour ago
      localStorage.setItem('tralli_valli_token_expiry', pastDate.toISOString());

      const expired = isAccessTokenExpired();
      expect(expired).toBe(true);
    });

    it('should return true if expiry is within 60 second buffer', () => {
      const nearExpiry = new Date(Date.now() + 30000); // 30 seconds from now
      localStorage.setItem('tralli_valli_token_expiry', nearExpiry.toISOString());

      const expired = isAccessTokenExpired();
      expect(expired).toBe(true);
    });

    it('should return true if expiry is not stored', () => {
      const expired = isAccessTokenExpired();
      expect(expired).toBe(true);
    });

    it('should return true if check fails', () => {
      const mockGetItem = vi.spyOn(Storage.prototype, 'getItem');
      mockGetItem.mockImplementation(() => {
        throw new Error('Storage error');
      });

      const expired = isAccessTokenExpired();
      expect(expired).toBe(true);

      mockGetItem.mockRestore();
    });
  });

  describe('isRefreshTokenExpired', () => {
    it('should return false if refresh token is not expired', () => {
      const futureDate = new Date(Date.now() + 7200000); // 2 hours from now
      localStorage.setItem('tralli_valli_refresh_expiry', futureDate.toISOString());

      const expired = isRefreshTokenExpired();
      expect(expired).toBe(false);
    });

    it('should return true if refresh token is expired', () => {
      const pastDate = new Date(Date.now() - 3600000); // 1 hour ago
      localStorage.setItem('tralli_valli_refresh_expiry', pastDate.toISOString());

      const expired = isRefreshTokenExpired();
      expect(expired).toBe(true);
    });

    it('should return true if expiry is not stored', () => {
      const expired = isRefreshTokenExpired();
      expect(expired).toBe(true);
    });

    it('should return true if check fails', () => {
      const mockGetItem = vi.spyOn(Storage.prototype, 'getItem');
      mockGetItem.mockImplementation(() => {
        throw new Error('Storage error');
      });

      const expired = isRefreshTokenExpired();
      expect(expired).toBe(true);

      mockGetItem.mockRestore();
    });
  });

  describe('clearTokens', () => {
    it('should clear all tokens', () => {
      const tokens = {
        accessToken: 'access-token-123',
        refreshToken: 'refresh-token-456',
        expiresAt: new Date().toISOString(),
        refreshExpiresAt: new Date().toISOString(),
      };

      storeTokens(tokens);
      clearTokens();

      expect(localStorage.getItem('tralli_valli_access_token')).toBeNull();
      expect(localStorage.getItem('tralli_valli_refresh_token')).toBeNull();
      expect(localStorage.getItem('tralli_valli_token_expiry')).toBeNull();
      expect(localStorage.getItem('tralli_valli_refresh_expiry')).toBeNull();
    });

    it('should not throw error if clear fails', () => {
      const mockRemoveItem = vi.spyOn(Storage.prototype, 'removeItem');
      mockRemoveItem.mockImplementation(() => {
        throw new Error('Storage error');
      });

      expect(() => clearTokens()).not.toThrow();

      mockRemoveItem.mockRestore();
    });
  });

  describe('isAuthenticated', () => {
    it('should return true if tokens are valid', () => {
      const tokens = {
        accessToken: 'access-token-123',
        refreshToken: 'refresh-token-456',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        refreshExpiresAt: new Date(Date.now() + 7200000).toISOString(),
      };

      storeTokens(tokens);

      const authenticated = isAuthenticated();
      expect(authenticated).toBe(true);
    });

    it('should return false if tokens are not stored', () => {
      const authenticated = isAuthenticated();
      expect(authenticated).toBe(false);
    });

    it('should return false if refresh token is expired', () => {
      const tokens = {
        accessToken: 'access-token-123',
        refreshToken: 'refresh-token-456',
        expiresAt: new Date(Date.now() - 3600000).toISOString(),
        refreshExpiresAt: new Date(Date.now() - 1000).toISOString(),
      };

      storeTokens(tokens);

      const authenticated = isAuthenticated();
      expect(authenticated).toBe(false);
    });
  });
});
