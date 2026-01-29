/**
 * Auth Store using Zustand
 *
 * Manages authentication state including user data and tokens.
 * Persists auth state to localStorage for session persistence.
 */

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { storeTokens, clearTokens, getTokens } from '@utils/tokenStorage';

export interface User {
  id: string;
  email: string;
  displayName: string;
}

export interface AuthState {
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  expiresAt: Date | null;
  refreshExpiresAt: Date | null;
  isAuthenticated: boolean;
}

export interface AuthActions {
  login: (
    user: User,
    tokens: {
      accessToken: string;
      refreshToken: string;
      expiresAt: string | Date;
      refreshExpiresAt: string | Date;
    }
  ) => void;
  logout: () => void;
  refresh: (tokens: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string | Date;
    refreshExpiresAt: string | Date;
  }) => void;
  setUser: (user: User | null) => void;
  initialize: () => void;
}

export type AuthStore = AuthState & AuthActions;

const initialState: AuthState = {
  user: null,
  token: null,
  refreshToken: null,
  expiresAt: null,
  refreshExpiresAt: null,
  isAuthenticated: false,
};

export const useAuthStore = create<AuthStore>()(
  persist(
    (set) => ({
      ...initialState,

      login: (user, tokens) => {
        const expiresAt =
          typeof tokens.expiresAt === 'string' ? new Date(tokens.expiresAt) : tokens.expiresAt;
        const refreshExpiresAt =
          typeof tokens.refreshExpiresAt === 'string'
            ? new Date(tokens.refreshExpiresAt)
            : tokens.refreshExpiresAt;

        // Store tokens in localStorage via tokenStorage utility
        // This is needed for the API client which doesn't use Zustand
        storeTokens({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          expiresAt,
          refreshExpiresAt,
        });

        set({
          user,
          token: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          expiresAt,
          refreshExpiresAt,
          isAuthenticated: true,
        });
      },

      logout: () => {
        // Clear tokens from tokenStorage (used by API client)
        clearTokens();
        set({
          ...initialState,
        });
      },

      refresh: (tokens) => {
        const expiresAt =
          typeof tokens.expiresAt === 'string' ? new Date(tokens.expiresAt) : tokens.expiresAt;
        const refreshExpiresAt =
          typeof tokens.refreshExpiresAt === 'string'
            ? new Date(tokens.refreshExpiresAt)
            : tokens.refreshExpiresAt;

        // Store tokens in localStorage via tokenStorage utility
        // This is needed for the API client which doesn't use Zustand
        storeTokens({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          expiresAt,
          refreshExpiresAt,
        });

        set((state) => ({
          token: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          expiresAt,
          refreshExpiresAt,
          isAuthenticated: state.user !== null,
        }));
      },

      setUser: (user) => {
        set((state) => ({
          user,
          isAuthenticated: user !== null && state.token !== null,
        }));
      },

      initialize: () => {
        const tokens = getTokens();
        if (tokens) {
          set((state) => ({
            token: tokens.accessToken,
            refreshToken: tokens.refreshToken,
            expiresAt: tokens.expiresAt,
            refreshExpiresAt: tokens.refreshExpiresAt,
            // Only set authenticated if we also have a user from persisted state
            isAuthenticated: state.user !== null,
          }));
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        token: state.token,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        refreshExpiresAt: state.refreshExpiresAt,
        isAuthenticated: state.isAuthenticated,
      }),
      // Custom storage to handle Date serialization
      storage: {
        getItem: (name) => {
          const str = localStorage.getItem(name);
          if (!str) return null;
          const { state } = JSON.parse(str);
          // Convert date strings back to Date objects
          return {
            state: {
              ...state,
              expiresAt: state.expiresAt ? new Date(state.expiresAt) : null,
              refreshExpiresAt: state.refreshExpiresAt ? new Date(state.refreshExpiresAt) : null,
            },
          };
        },
        setItem: (name, value) => {
          localStorage.setItem(name, JSON.stringify(value));
        },
        removeItem: (name) => {
          localStorage.removeItem(name);
        },
      },
    }
  )
);
