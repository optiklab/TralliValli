/**
 * Token Storage Utility
 *
 * Provides secure storage for JWT access and refresh tokens.
 * Uses localStorage as httpOnly cookies require server-side implementation.
 *
 * Security considerations:
 * - Tokens are stored in localStorage (accessible to JavaScript)
 * - In production, consider httpOnly cookies set by the server for better security
 * - Always use HTTPS to prevent token interception
 */

const ACCESS_TOKEN_KEY = 'tralli_valli_access_token';
const REFRESH_TOKEN_KEY = 'tralli_valli_refresh_token';
const TOKEN_EXPIRY_KEY = 'tralli_valli_token_expiry';
const REFRESH_EXPIRY_KEY = 'tralli_valli_refresh_expiry';

export interface StoredTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: Date;
  refreshExpiresAt: Date;
}

/**
 * Store tokens securely in localStorage
 */
export function storeTokens(tokens: {
  accessToken: string;
  refreshToken: string;
  expiresAt: string | Date;
  refreshExpiresAt: string | Date;
}): void {
  try {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);

    const expiresAt =
      typeof tokens.expiresAt === 'string' ? new Date(tokens.expiresAt) : tokens.expiresAt;
    const refreshExpiresAt =
      typeof tokens.refreshExpiresAt === 'string'
        ? new Date(tokens.refreshExpiresAt)
        : tokens.refreshExpiresAt;

    localStorage.setItem(TOKEN_EXPIRY_KEY, expiresAt.toISOString());
    localStorage.setItem(REFRESH_EXPIRY_KEY, refreshExpiresAt.toISOString());
  } catch (error) {
    console.error('Failed to store tokens:', error);
    throw new Error('Failed to store authentication tokens');
  }
}

/**
 * Retrieve tokens from localStorage
 */
export function getTokens(): StoredTokens | null {
  try {
    const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    const expiresAt = localStorage.getItem(TOKEN_EXPIRY_KEY);
    const refreshExpiresAt = localStorage.getItem(REFRESH_EXPIRY_KEY);

    if (!accessToken || !refreshToken || !expiresAt || !refreshExpiresAt) {
      return null;
    }

    return {
      accessToken,
      refreshToken,
      expiresAt: new Date(expiresAt),
      refreshExpiresAt: new Date(refreshExpiresAt),
    };
  } catch (error) {
    console.error('Failed to retrieve tokens:', error);
    return null;
  }
}

/**
 * Get access token
 */
export function getAccessToken(): string | null {
  try {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  } catch (error) {
    console.error('Failed to get access token:', error);
    return null;
  }
}

/**
 * Get refresh token
 */
export function getRefreshToken(): string | null {
  try {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  } catch (error) {
    console.error('Failed to get refresh token:', error);
    return null;
  }
}

/**
 * Check if access token is expired
 */
export function isAccessTokenExpired(): boolean {
  try {
    const expiryStr = localStorage.getItem(TOKEN_EXPIRY_KEY);
    if (!expiryStr) {
      return true;
    }

    const expiry = new Date(expiryStr);
    // Add 60 second buffer to refresh before actual expiry
    const bufferMs = 60 * 1000;
    return Date.now() >= expiry.getTime() - bufferMs;
  } catch (error) {
    console.error('Failed to check token expiry:', error);
    return true;
  }
}

/**
 * Check if refresh token is expired
 */
export function isRefreshTokenExpired(): boolean {
  try {
    const expiryStr = localStorage.getItem(REFRESH_EXPIRY_KEY);
    if (!expiryStr) {
      return true;
    }

    const expiry = new Date(expiryStr);
    return Date.now() >= expiry.getTime();
  } catch (error) {
    console.error('Failed to check refresh token expiry:', error);
    return true;
  }
}

/**
 * Clear all stored tokens
 */
export function clearTokens(): void {
  try {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(TOKEN_EXPIRY_KEY);
    localStorage.removeItem(REFRESH_EXPIRY_KEY);
  } catch (error) {
    console.error('Failed to clear tokens:', error);
  }
}

/**
 * Check if user is authenticated (has valid tokens)
 */
export function isAuthenticated(): boolean {
  const tokens = getTokens();
  return tokens !== null && !isRefreshTokenExpired();
}
