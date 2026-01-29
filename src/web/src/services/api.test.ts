import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as tokenStorage from '@utils/tokenStorage';
import { apiClient } from './api';
import { ApiErrorResponse } from '@/types/api';

// Mock tokenStorage module
vi.mock('@utils/tokenStorage', () => ({
  getAccessToken: vi.fn(),
  getRefreshToken: vi.fn(),
  storeTokens: vi.fn(),
  clearTokens: vi.fn(),
  isAccessTokenExpired: vi.fn(),
  isRefreshTokenExpired: vi.fn(),
  isAuthenticated: vi.fn(),
}));

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

describe('API Client Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Default mock implementations
    vi.mocked(tokenStorage.getAccessToken).mockReturnValue('test-access-token');
    vi.mocked(tokenStorage.getRefreshToken).mockReturnValue('test-refresh-token');
    vi.mocked(tokenStorage.isAccessTokenExpired).mockReturnValue(false);
    vi.mocked(tokenStorage.isRefreshTokenExpired).mockReturnValue(false);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Request Interceptors', () => {
    it('should inject JWT token in Authorization header', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ message: 'success' }),
      } as Response);

      await apiClient.requestMagicLink({ email: 'test@example.com', deviceId: 'device-123' });

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer test-access-token',
          }),
        })
      );
    });

    it('should not inject token if not available', async () => {
      vi.mocked(tokenStorage.getAccessToken).mockReturnValue(null);

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ message: 'success' }),
      } as Response);

      await apiClient.requestMagicLink({ email: 'test@example.com', deviceId: 'device-123' });

      const callArgs = mockFetch.mock.calls[0][1] as RequestInit;
      const headers = callArgs.headers as Record<string, string>;
      expect(headers.Authorization).toBeUndefined();
    });

    it('should refresh token if expired before making request', async () => {
      vi.mocked(tokenStorage.isAccessTokenExpired).mockReturnValue(true);

      // Mock refresh token response
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            accessToken: 'new-access-token',
            refreshToken: 'new-refresh-token',
            expiresAt: new Date(Date.now() + 3600000).toISOString(),
            refreshExpiresAt: new Date(Date.now() + 7200000).toISOString(),
          }),
        } as Response)
        // Mock the actual request
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ message: 'success' }),
        } as Response);

      // Update mock to return new token after refresh
      vi.mocked(tokenStorage.getAccessToken).mockReturnValueOnce('test-access-token');
      vi.mocked(tokenStorage.getAccessToken).mockReturnValue('new-access-token');

      await apiClient.requestMagicLink({ email: 'test@example.com', deviceId: 'device-123' });

      // Should have called fetch twice: once for refresh, once for actual request
      expect(mockFetch).toHaveBeenCalledTimes(2);
      expect(tokenStorage.storeTokens).toHaveBeenCalled();
    });
  });

  describe('Error Handling', () => {
    it('should throw ApiErrorResponse on error', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        statusText: 'Bad Request',
        json: async () => ({
          message: 'Invalid request',
          statusCode: 400,
          errors: { email: ['Email is required'] },
        }),
      } as Response);

      try {
        await apiClient.requestMagicLink({ email: '', deviceId: 'device-123' });
        // If we reach here, the test should fail
        expect(true).toBe(false);
      } catch (error) {
        expect(error).toBeInstanceOf(ApiErrorResponse);
        const apiError = error as ApiErrorResponse;
        expect(apiError.statusCode).toBe(400);
        expect(apiError.message).toBe('Invalid request');
        expect(apiError.errors).toEqual({ email: ['Email is required'] });
      }
    });

    it('should handle non-JSON error responses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
        json: async () => {
          throw new Error('Not JSON');
        },
      } as Response);

      await expect(
        apiClient.requestMagicLink({ email: 'test@example.com', deviceId: 'device-123' })
      ).rejects.toThrow(ApiErrorResponse);
    });
  });

  describe('Auth Methods', () => {
    describe('requestMagicLink', () => {
      it('should request a magic link', async () => {
        const mockResponse = { message: 'Magic link sent' };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.requestMagicLink({
          email: 'test@example.com',
          deviceId: 'device-123',
        });

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/auth/request-magic-link'),
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ email: 'test@example.com', deviceId: 'device-123' }),
          })
        );
      });
    });

    describe('verifyMagicLink', () => {
      it('should verify magic link and store tokens', async () => {
        const mockResponse = {
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresAt: new Date().toISOString(),
          refreshExpiresAt: new Date().toISOString(),
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.verifyMagicLink({ token: 'magic-token' });

        expect(result).toEqual(mockResponse);
        expect(tokenStorage.storeTokens).toHaveBeenCalledWith(mockResponse);
      });
    });

    describe('refresh', () => {
      it('should refresh tokens and store new ones', async () => {
        const mockResponse = {
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          expiresAt: new Date().toISOString(),
          refreshExpiresAt: new Date().toISOString(),
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.refresh({ refreshToken: 'old-refresh-token' });

        expect(result).toEqual(mockResponse);
        expect(tokenStorage.storeTokens).toHaveBeenCalledWith(mockResponse);
      });
    });

    describe('logout', () => {
      it('should logout and clear tokens', async () => {
        const mockResponse = { message: 'Logged out successfully' };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.logout({ accessToken: 'test-token' });

        expect(result).toEqual(mockResponse);
        expect(tokenStorage.clearTokens).toHaveBeenCalled();
      });

      it('should clear tokens even if logout request fails', async () => {
        mockFetch.mockResolvedValueOnce({
          ok: false,
          status: 500,
          statusText: 'Internal Server Error',
          json: async () => ({}),
        } as Response);

        await expect(apiClient.logout({ accessToken: 'test-token' })).rejects.toThrow();
        expect(tokenStorage.clearTokens).toHaveBeenCalled();
      });
    });

    describe('register', () => {
      it('should register user and store tokens', async () => {
        const mockResponse = {
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresAt: new Date().toISOString(),
          refreshExpiresAt: new Date().toISOString(),
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.register({
          inviteToken: 'invite-token',
          email: 'test@example.com',
          displayName: 'Test User',
        });

        expect(result).toEqual(mockResponse);
        expect(tokenStorage.storeTokens).toHaveBeenCalledWith(mockResponse);
      });
    });

    describe('validateInvite', () => {
      it('should validate invite token', async () => {
        const mockResponse = {
          isValid: true,
          expiresAt: new Date().toISOString(),
          message: 'Invite is valid',
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.validateInvite('invite-token');

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/auth/invite/invite-token'),
          expect.objectContaining({ method: 'GET' })
        );
      });
    });
  });

  describe('Conversations Methods', () => {
    describe('listConversations', () => {
      it('should list conversations', async () => {
        const mockResponse = {
          conversations: [],
          totalCount: 0,
          page: 1,
          pageSize: 20,
          totalPages: 0,
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.listConversations();

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/conversations'),
          expect.objectContaining({ method: 'GET' })
        );
      });

      it('should list conversations with pagination', async () => {
        const mockResponse = {
          conversations: [],
          totalCount: 0,
          page: 2,
          pageSize: 10,
          totalPages: 0,
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        await apiClient.listConversations({ page: 2, pageSize: 10 });

        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('?page=2&pageSize=10'),
          expect.any(Object)
        );
      });
    });

    describe('createDirectConversation', () => {
      it('should create a direct conversation', async () => {
        const mockResponse = {
          id: 'conv-1',
          type: 'direct',
          name: 'Direct Chat',
          isGroup: false,
          participants: [],
          recentMessages: [],
          createdAt: new Date().toISOString(),
          metadata: {},
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.createDirectConversation({ otherUserId: 'user-2' });

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/conversations/direct'),
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ otherUserId: 'user-2' }),
          })
        );
      });
    });

    describe('createGroupConversation', () => {
      it('should create a group conversation', async () => {
        const mockResponse = {
          id: 'conv-1',
          type: 'group',
          name: 'Group Chat',
          isGroup: true,
          participants: [],
          recentMessages: [],
          createdAt: new Date().toISOString(),
          metadata: {},
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.createGroupConversation({
          name: 'Group Chat',
          memberUserIds: ['user-2', 'user-3'],
        });

        expect(result).toEqual(mockResponse);
      });
    });

    describe('getConversation', () => {
      it('should get a conversation by ID', async () => {
        const mockResponse = {
          id: 'conv-1',
          type: 'direct',
          name: 'Chat',
          isGroup: false,
          participants: [],
          recentMessages: [],
          createdAt: new Date().toISOString(),
          metadata: {},
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.getConversation('conv-1');

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/conversations/conv-1'),
          expect.objectContaining({ method: 'GET' })
        );
      });
    });

    describe('updateGroupMetadata', () => {
      it('should update group metadata', async () => {
        const mockResponse = {
          id: 'conv-1',
          type: 'group',
          name: 'Updated Group',
          isGroup: true,
          participants: [],
          recentMessages: [],
          createdAt: new Date().toISOString(),
          metadata: { key: 'value' },
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.updateGroupMetadata('conv-1', {
          name: 'Updated Group',
          metadata: { key: 'value' },
        });

        expect(result).toEqual(mockResponse);
      });
    });

    describe('addMember', () => {
      it('should add a member to a conversation', async () => {
        const mockResponse = {
          id: 'conv-1',
          type: 'group',
          name: 'Group',
          isGroup: true,
          participants: [],
          recentMessages: [],
          createdAt: new Date().toISOString(),
          metadata: {},
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.addMember('conv-1', { userId: 'user-4' });

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/conversations/conv-1/members'),
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ userId: 'user-4' }),
          })
        );
      });
    });

    describe('removeMember', () => {
      it('should remove a member from a conversation', async () => {
        const mockResponse = {
          id: 'conv-1',
          type: 'group',
          name: 'Group',
          isGroup: true,
          participants: [],
          recentMessages: [],
          createdAt: new Date().toISOString(),
          metadata: {},
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.removeMember('conv-1', 'user-4');

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/conversations/conv-1/members/user-4'),
          expect.objectContaining({ method: 'DELETE' })
        );
      });
    });
  });

  describe('Messages Methods', () => {
    describe('listMessages', () => {
      it('should list messages in a conversation', async () => {
        const mockResponse = {
          messages: [],
          hasMore: false,
          nextCursor: undefined,
          count: 0,
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.listMessages('conv-1');

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/conversations/conv-1/messages'),
          expect.objectContaining({ method: 'GET' })
        );
      });

      it('should list messages with pagination parameters', async () => {
        const mockResponse = {
          messages: [],
          hasMore: false,
          count: 0,
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        await apiClient.listMessages('conv-1', { limit: 50, before: '2024-01-01T00:00:00Z' });

        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/conversations/conv-1/messages'),
          expect.any(Object)
        );

        // Check that the URL contains the encoded parameters
        const callArgs = mockFetch.mock.calls[0][0] as string;
        expect(callArgs).toContain('limit=50');
        expect(callArgs).toContain('before=');
      });
    });

    describe('searchMessages', () => {
      it('should search messages in a conversation', async () => {
        const mockResponse = {
          messages: [],
          count: 0,
          query: 'search term',
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.searchMessages('conv-1', {
          query: 'search term',
          limit: 20,
        });

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/conversations/conv-1/messages/search'),
          expect.objectContaining({ method: 'GET' })
        );
      });
    });

    describe('deleteMessage', () => {
      it('should delete a message', async () => {
        const mockResponse = {
          id: 'msg-1',
          conversationId: 'conv-1',
          senderId: 'user-1',
          type: 'text',
          content: '',
          encryptedContent: '',
          createdAt: new Date().toISOString(),
          readBy: [],
          isDeleted: true,
          attachments: [],
        };

        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

        const result = await apiClient.deleteMessage('msg-1');

        expect(result).toEqual(mockResponse);
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/messages/msg-1'),
          expect.objectContaining({ method: 'DELETE' })
        );
      });
    });
  });

  describe('Token Refresh on 401', () => {
    it('should handle 401 response', async () => {
      // First request returns 401
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        statusText: 'Unauthorized',
        json: async () => ({ message: 'Token expired' }),
      } as Response);

      await expect(apiClient.listConversations()).rejects.toThrow();

      // Should have checked for refresh token when handling 401
      expect(tokenStorage.getRefreshToken).toHaveBeenCalled();
    });
  });
});
