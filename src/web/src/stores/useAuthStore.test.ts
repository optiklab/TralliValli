import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as tokenStorage from '@utils/tokenStorage';
import { useAuthStore } from './useAuthStore';
import type { User } from './useAuthStore';

// Mock the tokenStorage module
vi.mock('@utils/tokenStorage', () => ({
  storeTokens: vi.fn(),
  clearTokens: vi.fn(),
  getTokens: vi.fn(),
}));

describe('useAuthStore', () => {
  beforeEach(() => {
    // Clear the store state before each test
    useAuthStore.setState({
      user: null,
      token: null,
      refreshToken: null,
      expiresAt: null,
      refreshExpiresAt: null,
      isAuthenticated: false,
    });

    // Clear all mocks
    vi.clearAllMocks();

    // Clear localStorage to reset persist middleware
    localStorage.clear();
  });

  describe('initial state', () => {
    it('should have initial state with no user and not authenticated', () => {
      const state = useAuthStore.getState();

      expect(state.user).toBeNull();
      expect(state.token).toBeNull();
      expect(state.refreshToken).toBeNull();
      expect(state.expiresAt).toBeNull();
      expect(state.refreshExpiresAt).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });
  });

  describe('login', () => {
    it('should set user and tokens on login', () => {
      const user: User = {
        id: 'user-123',
        email: 'test@example.com',
        displayName: 'Test User',
      };

      const tokens = {
        accessToken: 'access-token-123',
        refreshToken: 'refresh-token-123',
        expiresAt: '2026-12-31T23:59:59Z',
        refreshExpiresAt: '2027-12-31T23:59:59Z',
      };

      useAuthStore.getState().login(user, tokens);

      const state = useAuthStore.getState();

      expect(state.user).toEqual(user);
      expect(state.token).toBe('access-token-123');
      expect(state.refreshToken).toBe('refresh-token-123');
      expect(state.expiresAt).toBeInstanceOf(Date);
      expect(state.refreshExpiresAt).toBeInstanceOf(Date);
      expect(state.isAuthenticated).toBe(true);

      // Verify tokens were stored
      expect(tokenStorage.storeTokens).toHaveBeenCalledWith({
        accessToken: 'access-token-123',
        refreshToken: 'refresh-token-123',
        expiresAt: expect.any(Date),
        refreshExpiresAt: expect.any(Date),
      });
    });

    it('should handle Date objects for token expiry', () => {
      const user: User = {
        id: 'user-456',
        email: 'test2@example.com',
        displayName: 'Test User 2',
      };

      const expiresAt = new Date('2026-12-31T23:59:59Z');
      const refreshExpiresAt = new Date('2027-12-31T23:59:59Z');

      const tokens = {
        accessToken: 'access-token-456',
        refreshToken: 'refresh-token-456',
        expiresAt,
        refreshExpiresAt,
      };

      useAuthStore.getState().login(user, tokens);

      const state = useAuthStore.getState();

      expect(state.expiresAt).toEqual(expiresAt);
      expect(state.refreshExpiresAt).toEqual(refreshExpiresAt);
      expect(state.isAuthenticated).toBe(true);
    });
  });

  describe('logout', () => {
    it('should clear user and tokens on logout', () => {
      // First login
      const user: User = {
        id: 'user-789',
        email: 'test3@example.com',
        displayName: 'Test User 3',
      };

      const tokens = {
        accessToken: 'access-token-789',
        refreshToken: 'refresh-token-789',
        expiresAt: '2026-12-31T23:59:59Z',
        refreshExpiresAt: '2027-12-31T23:59:59Z',
      };

      useAuthStore.getState().login(user, tokens);

      // Then logout
      useAuthStore.getState().logout();

      const state = useAuthStore.getState();

      expect(state.user).toBeNull();
      expect(state.token).toBeNull();
      expect(state.refreshToken).toBeNull();
      expect(state.expiresAt).toBeNull();
      expect(state.refreshExpiresAt).toBeNull();
      expect(state.isAuthenticated).toBe(false);

      // Verify tokens were cleared
      expect(tokenStorage.clearTokens).toHaveBeenCalled();
    });
  });

  describe('refresh', () => {
    it('should update tokens on refresh', () => {
      // First login
      const user: User = {
        id: 'user-999',
        email: 'test4@example.com',
        displayName: 'Test User 4',
      };

      const initialTokens = {
        accessToken: 'old-access-token',
        refreshToken: 'old-refresh-token',
        expiresAt: '2026-06-30T23:59:59Z',
        refreshExpiresAt: '2027-06-30T23:59:59Z',
      };

      useAuthStore.getState().login(user, initialTokens);

      // Clear the mock to track only refresh call
      vi.clearAllMocks();

      // Then refresh
      const newTokens = {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        expiresAt: '2026-12-31T23:59:59Z',
        refreshExpiresAt: '2027-12-31T23:59:59Z',
      };

      useAuthStore.getState().refresh(newTokens);

      const state = useAuthStore.getState();

      expect(state.user).toEqual(user); // User should remain the same
      expect(state.token).toBe('new-access-token');
      expect(state.refreshToken).toBe('new-refresh-token');
      expect(state.isAuthenticated).toBe(true);

      // Verify new tokens were stored
      expect(tokenStorage.storeTokens).toHaveBeenCalledWith({
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        expiresAt: expect.any(Date),
        refreshExpiresAt: expect.any(Date),
      });
    });
  });

  describe('setUser', () => {
    it('should update user', () => {
      const user: User = {
        id: 'user-111',
        email: 'test5@example.com',
        displayName: 'Test User 5',
      };

      useAuthStore.getState().setUser(user);

      const state = useAuthStore.getState();

      expect(state.user).toEqual(user);
      // isAuthenticated should be false because no token
      expect(state.isAuthenticated).toBe(false);
    });

    it('should set isAuthenticated to true when user and token exist', () => {
      const user: User = {
        id: 'user-222',
        email: 'test6@example.com',
        displayName: 'Test User 6',
      };

      const tokens = {
        accessToken: 'access-token-222',
        refreshToken: 'refresh-token-222',
        expiresAt: '2026-12-31T23:59:59Z',
        refreshExpiresAt: '2027-12-31T23:59:59Z',
      };

      useAuthStore.getState().login(user, tokens);

      // Update user
      const updatedUser: User = {
        ...user,
        displayName: 'Updated User Name',
      };

      useAuthStore.getState().setUser(updatedUser);

      const state = useAuthStore.getState();

      expect(state.user).toEqual(updatedUser);
      expect(state.isAuthenticated).toBe(true); // Should remain true
    });
  });

  describe('initialize', () => {
    it('should initialize from stored tokens', () => {
      // First set a user in the store (simulating persisted state)
      const user: User = {
        id: 'user-stored',
        email: 'stored@example.com',
        displayName: 'Stored User',
      };

      useAuthStore.setState({ user });

      const storedTokens = {
        accessToken: 'stored-access-token',
        refreshToken: 'stored-refresh-token',
        expiresAt: new Date('2026-12-31T23:59:59Z'),
        refreshExpiresAt: new Date('2027-12-31T23:59:59Z'),
      };

      vi.mocked(tokenStorage.getTokens).mockReturnValue(storedTokens);

      useAuthStore.getState().initialize();

      const state = useAuthStore.getState();

      expect(state.token).toBe('stored-access-token');
      expect(state.refreshToken).toBe('stored-refresh-token');
      expect(state.expiresAt).toEqual(storedTokens.expiresAt);
      expect(state.refreshExpiresAt).toEqual(storedTokens.refreshExpiresAt);
      expect(state.isAuthenticated).toBe(true); // true because user exists
    });

    it('should not set isAuthenticated if user is missing', () => {
      // Don't set a user in the store
      const storedTokens = {
        accessToken: 'stored-access-token',
        refreshToken: 'stored-refresh-token',
        expiresAt: new Date('2026-12-31T23:59:59Z'),
        refreshExpiresAt: new Date('2027-12-31T23:59:59Z'),
      };

      vi.mocked(tokenStorage.getTokens).mockReturnValue(storedTokens);

      useAuthStore.getState().initialize();

      const state = useAuthStore.getState();

      expect(state.token).toBe('stored-access-token');
      expect(state.refreshToken).toBe('stored-refresh-token');
      expect(state.isAuthenticated).toBe(false); // false because no user
    });

    it('should handle no stored tokens', () => {
      vi.mocked(tokenStorage.getTokens).mockReturnValue(null);

      useAuthStore.getState().initialize();

      const state = useAuthStore.getState();

      expect(state.token).toBeNull();
      expect(state.refreshToken).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });
  });

  describe('persistence', () => {
    it('should persist state to localStorage', () => {
      const user: User = {
        id: 'user-persist',
        email: 'persist@example.com',
        displayName: 'Persist User',
      };

      const tokens = {
        accessToken: 'persist-access-token',
        refreshToken: 'persist-refresh-token',
        expiresAt: '2026-12-31T23:59:59Z',
        refreshExpiresAt: '2027-12-31T23:59:59Z',
      };

      useAuthStore.getState().login(user, tokens);

      // Check that localStorage was updated
      const storedData = localStorage.getItem('auth-storage');
      expect(storedData).toBeTruthy();

      if (storedData) {
        const parsed = JSON.parse(storedData);
        expect(parsed.state.user).toEqual(user);
        expect(parsed.state.token).toBe('persist-access-token');
        expect(parsed.state.isAuthenticated).toBe(true);
      }
    });
  });
});
