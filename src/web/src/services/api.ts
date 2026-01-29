/**
 * API Client Service
 *
 * Provides a centralized API client with interceptors for:
 * - JWT token injection
 * - Automatic token refresh
 * - Typed error handling
 *
 * All API methods return typed responses and throw ApiErrorResponse on errors.
 */

import {
  getAccessToken,
  getRefreshToken,
  storeTokens,
  clearTokens,
  isAccessTokenExpired,
  isRefreshTokenExpired,
} from '@utils/tokenStorage';
import {
  ApiErrorResponse,
  type ApiError,
  type AddMemberRequest,
  type ConversationResponse,
  type CreateDirectConversationRequest,
  type CreateGroupConversationRequest,
  type LogoutRequest,
  type LogoutResponse,
  type MessageResponse,
  type PaginatedConversationsResponse,
  type PaginatedMessagesResponse,
  type RefreshTokenRequest,
  type RefreshTokenResponse,
  type RegisterRequest,
  type RegisterResponse,
  type RequestMagicLinkRequest,
  type RequestMagicLinkResponse,
  type SearchMessagesRequest,
  type SearchMessagesResponse,
  type UpdateGroupMetadataRequest,
  type ValidateInviteResponse,
  type VerifyMagicLinkRequest,
  type VerifyMagicLinkResponse,
} from '@/types/api';

// ============================================================================
// Configuration
// ============================================================================

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

// ============================================================================
// Request Interceptor Types
// ============================================================================

type RequestInterceptor = (request: RequestInit) => Promise<RequestInit> | RequestInit;
type ResponseInterceptor = (response: Response) => Promise<Response> | Response;

// ============================================================================
// API Client Class
// ============================================================================

class ApiClient {
  private baseUrl: string;
  private requestInterceptors: RequestInterceptor[] = [];
  private responseInterceptors: ResponseInterceptor[] = [];
  private isRefreshing = false;
  private refreshPromise: Promise<void> | null = null;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
    this.setupDefaultInterceptors();
  }

  /**
   * Setup default interceptors for token injection and refresh
   */
  private setupDefaultInterceptors(): void {
    // Request interceptor: Inject JWT token
    this.addRequestInterceptor(async (request) => {
      const accessToken = getAccessToken();

      if (accessToken) {
        // Check if token is expired and refresh if needed
        if (isAccessTokenExpired()) {
          await this.handleTokenRefresh();
          // Get the new token after refresh
          const newAccessToken = getAccessToken();
          if (newAccessToken) {
            request.headers = {
              ...request.headers,
              Authorization: `Bearer ${newAccessToken}`,
            };
          }
        } else {
          request.headers = {
            ...request.headers,
            Authorization: `Bearer ${accessToken}`,
          };
        }
      }

      return request;
    });

    // Response interceptor: Handle 401 Unauthorized
    this.addResponseInterceptor(async (response) => {
      if (response.status === 401) {
        // Try to refresh token
        const refreshToken = getRefreshToken();
        if (refreshToken && !isRefreshTokenExpired()) {
          await this.handleTokenRefresh();
          // Retry the original request with new token
          // Note: This is a simplified version, in production you may want to
          // implement a more sophisticated retry mechanism
        }
      }
      return response;
    });
  }

  /**
   * Handle token refresh with deduplication
   */
  private async handleTokenRefresh(): Promise<void> {
    // If already refreshing, wait for that promise
    if (this.isRefreshing && this.refreshPromise) {
      return this.refreshPromise;
    }

    const refreshToken = getRefreshToken();
    if (!refreshToken || isRefreshTokenExpired()) {
      clearTokens();
      throw new ApiErrorResponse(401, 'Refresh token expired or missing');
    }

    this.isRefreshing = true;
    this.refreshPromise = this.refreshTokens(refreshToken)
      .then((response) => {
        storeTokens(response);
      })
      .catch((error) => {
        clearTokens();
        throw error;
      })
      .finally(() => {
        this.isRefreshing = false;
        this.refreshPromise = null;
      });

    return this.refreshPromise;
  }

  /**
   * Refresh tokens without using interceptors (to avoid infinite loop)
   */
  private async refreshTokens(refreshToken: string): Promise<RefreshTokenResponse> {
    const response = await fetch(`${this.baseUrl}/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      const error = await this.parseError(response);
      throw error;
    }

    return response.json();
  }

  /**
   * Add a request interceptor
   */
  addRequestInterceptor(interceptor: RequestInterceptor): void {
    this.requestInterceptors.push(interceptor);
  }

  /**
   * Add a response interceptor
   */
  addResponseInterceptor(interceptor: ResponseInterceptor): void {
    this.responseInterceptors.push(interceptor);
  }

  /**
   * Apply all request interceptors
   */
  private async applyRequestInterceptors(request: RequestInit): Promise<RequestInit> {
    let modifiedRequest = request;
    for (const interceptor of this.requestInterceptors) {
      modifiedRequest = await interceptor(modifiedRequest);
    }
    return modifiedRequest;
  }

  /**
   * Apply all response interceptors
   */
  private async applyResponseInterceptors(response: Response): Promise<Response> {
    let modifiedResponse = response;
    for (const interceptor of this.responseInterceptors) {
      modifiedResponse = await interceptor(modifiedResponse);
    }
    return modifiedResponse;
  }

  /**
   * Parse error response into ApiErrorResponse
   */
  private async parseError(response: Response): Promise<ApiErrorResponse> {
    try {
      const errorData: ApiError = await response.json();
      return new ApiErrorResponse(
        response.status,
        errorData.message || response.statusText,
        errorData.errors,
        errorData.traceId
      );
    } catch {
      return new ApiErrorResponse(response.status, response.statusText);
    }
  }

  /**
   * Make a request with interceptors
   */
  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    // Setup default headers
    const defaultHeaders: HeadersInit = {
      'Content-Type': 'application/json',
    };

    const requestOptions: RequestInit = {
      ...options,
      headers: {
        ...defaultHeaders,
        ...options.headers,
      },
    };

    // Apply request interceptors
    const modifiedRequest = await this.applyRequestInterceptors(requestOptions);

    // Make the request
    let response = await fetch(`${this.baseUrl}${endpoint}`, modifiedRequest);

    // Apply response interceptors
    response = await this.applyResponseInterceptors(response);

    // Handle errors
    if (!response.ok) {
      const error = await this.parseError(response);
      throw error;
    }

    // Parse and return response
    // Handle 204 No Content
    if (response.status === 204) {
      return {} as T;
    }

    return response.json();
  }

  // ============================================================================
  // Auth Methods
  // ============================================================================

  /**
   * Request a magic link to be sent to the specified email
   */
  async requestMagicLink(data: RequestMagicLinkRequest): Promise<RequestMagicLinkResponse> {
    return this.request<RequestMagicLinkResponse>('/auth/request-magic-link', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  /**
   * Verify a magic link token and get JWT tokens
   */
  async verifyMagicLink(data: VerifyMagicLinkRequest): Promise<VerifyMagicLinkResponse> {
    const response = await this.request<VerifyMagicLinkResponse>('/auth/verify-magic-link', {
      method: 'POST',
      body: JSON.stringify(data),
    });

    // Store tokens
    storeTokens(response);

    return response;
  }

  /**
   * Refresh JWT tokens
   */
  async refresh(data: RefreshTokenRequest): Promise<RefreshTokenResponse> {
    const response = await this.request<RefreshTokenResponse>('/auth/refresh', {
      method: 'POST',
      body: JSON.stringify(data),
    });

    // Store new tokens
    storeTokens(response);

    return response;
  }

  /**
   * Logout and invalidate tokens
   */
  async logout(data: LogoutRequest): Promise<LogoutResponse> {
    try {
      const response = await this.request<LogoutResponse>('/auth/logout', {
        method: 'POST',
        body: JSON.stringify(data),
      });

      return response;
    } finally {
      // Always clear local tokens
      clearTokens();
    }
  }

  /**
   * Register a new user with an invite token
   */
  async register(data: RegisterRequest): Promise<RegisterResponse> {
    const response = await this.request<RegisterResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    });

    // Store tokens
    storeTokens(response);

    return response;
  }

  /**
   * Validate an invite token
   */
  async validateInvite(token: string): Promise<ValidateInviteResponse> {
    return this.request<ValidateInviteResponse>(`/auth/invite/${token}`, {
      method: 'GET',
    });
  }

  // ============================================================================
  // Conversations Methods
  // ============================================================================

  /**
   * List conversations for the authenticated user
   */
  async listConversations(params?: {
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedConversationsResponse> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());

    const query = queryParams.toString();
    return this.request<PaginatedConversationsResponse>(
      `/conversations${query ? `?${query}` : ''}`,
      { method: 'GET' }
    );
  }

  /**
   * Create a direct conversation
   */
  async createDirectConversation(
    data: CreateDirectConversationRequest
  ): Promise<ConversationResponse> {
    return this.request<ConversationResponse>('/conversations/direct', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  /**
   * Create a group conversation
   */
  async createGroupConversation(
    data: CreateGroupConversationRequest
  ): Promise<ConversationResponse> {
    return this.request<ConversationResponse>('/conversations/group', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  /**
   * Get a conversation by ID
   */
  async getConversation(id: string): Promise<ConversationResponse> {
    return this.request<ConversationResponse>(`/conversations/${id}`, {
      method: 'GET',
    });
  }

  /**
   * Update group metadata
   */
  async updateGroupMetadata(
    id: string,
    data: UpdateGroupMetadataRequest
  ): Promise<ConversationResponse> {
    return this.request<ConversationResponse>(`/conversations/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  /**
   * Add a member to a conversation
   */
  async addMember(id: string, data: AddMemberRequest): Promise<ConversationResponse> {
    return this.request<ConversationResponse>(`/conversations/${id}/members`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  /**
   * Remove a member from a conversation
   */
  async removeMember(id: string, userId: string): Promise<ConversationResponse> {
    return this.request<ConversationResponse>(`/conversations/${id}/members/${userId}`, {
      method: 'DELETE',
    });
  }

  // ============================================================================
  // Messages Methods
  // ============================================================================

  /**
   * List messages in a conversation
   */
  async listMessages(
    conversationId: string,
    params?: {
      limit?: number;
      before?: string;
    }
  ): Promise<PaginatedMessagesResponse> {
    const queryParams = new URLSearchParams();
    if (params?.limit) queryParams.append('limit', params.limit.toString());
    if (params?.before) queryParams.append('before', params.before);

    const query = queryParams.toString();
    return this.request<PaginatedMessagesResponse>(
      `/conversations/${conversationId}/messages${query ? `?${query}` : ''}`,
      { method: 'GET' }
    );
  }

  /**
   * Search messages in a conversation
   */
  async searchMessages(
    conversationId: string,
    data: SearchMessagesRequest
  ): Promise<SearchMessagesResponse> {
    const queryParams = new URLSearchParams({
      query: data.query,
      ...(data.limit && { limit: data.limit.toString() }),
    });

    return this.request<SearchMessagesResponse>(
      `/conversations/${conversationId}/messages/search?${queryParams.toString()}`,
      { method: 'GET' }
    );
  }

  /**
   * Delete a message
   */
  async deleteMessage(messageId: string): Promise<MessageResponse> {
    return this.request<MessageResponse>(`/messages/${messageId}`, {
      method: 'DELETE',
    });
  }

  // ============================================================================
  // Users Methods (Placeholder)
  // ============================================================================
  // Note: No user endpoints found in the API controllers yet
  // Add methods here when user endpoints are implemented

  // ============================================================================
  // Files Methods (Placeholder)
  // ============================================================================
  // Note: No file endpoints found in the API controllers yet
  // Add methods here when file endpoints are implemented
}

// ============================================================================
// Export Singleton Instance
// ============================================================================

export const apiClient = new ApiClient();
export default apiClient;
