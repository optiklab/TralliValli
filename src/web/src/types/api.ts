// ============================================================================
// API Types and Models
// ============================================================================

// ============================================================================
// Auth Models
// ============================================================================

export interface RequestMagicLinkRequest {
  email: string;
  deviceId: string;
}

export interface RequestMagicLinkResponse {
  message: string;
}

export interface VerifyMagicLinkRequest {
  token: string;
}

export interface VerifyMagicLinkResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  refreshExpiresAt: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  refreshExpiresAt: string;
}

export interface LogoutRequest {
  accessToken: string;
}

export interface LogoutResponse {
  message: string;
}

export interface RegisterRequest {
  inviteToken: string;
  email: string;
  displayName: string;
}

export interface RegisterResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  refreshExpiresAt: string;
}

export interface ValidateInviteResponse {
  isValid: boolean;
  expiresAt?: string;
  message?: string;
}

// ============================================================================
// Conversation Models
// ============================================================================

export interface CreateDirectConversationRequest {
  otherUserId: string;
}

export interface CreateGroupConversationRequest {
  name: string;
  memberUserIds: string[];
}

export interface UpdateGroupMetadataRequest {
  name?: string;
  metadata?: Record<string, string>;
}

export interface AddMemberRequest {
  userId: string;
  role?: string;
}

export interface ParticipantResponse {
  userId: string;
  joinedAt: string;
  lastReadAt?: string;
  role: string;
}

export interface ConversationResponse {
  id: string;
  type: string;
  name: string;
  isGroup: boolean;
  participants: ParticipantResponse[];
  recentMessages: string[];
  createdAt: string;
  lastMessageAt?: string;
  metadata: Record<string, string>;
}

export interface PaginatedConversationsResponse {
  conversations: ConversationResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ============================================================================
// Message Models
// ============================================================================

export interface MessageReadStatusResponse {
  userId: string;
  readAt: string;
}

export interface MessageResponse {
  id: string;
  conversationId: string;
  senderId: string;
  type: string;
  content: string;
  encryptedContent: string;
  replyTo?: string;
  createdAt: string;
  readBy: MessageReadStatusResponse[];
  editedAt?: string;
  isDeleted: boolean;
  attachments: string[];
}

export interface PaginatedMessagesResponse {
  messages: MessageResponse[];
  hasMore: boolean;
  nextCursor?: string;
  count: number;
}

export interface SearchMessagesRequest {
  query: string;
  limit?: number;
}

export interface SearchMessagesResponse {
  messages: MessageResponse[];
  count: number;
  query: string;
}

// ============================================================================
// Error Models
// ============================================================================

export interface ApiError {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export class ApiErrorResponse extends Error {
  statusCode: number;
  errors?: Record<string, string[]>;
  traceId?: string;

  constructor(
    statusCode: number,
    message: string,
    errors?: Record<string, string[]>,
    traceId?: string
  ) {
    super(message);
    this.name = 'ApiErrorResponse';
    this.statusCode = statusCode;
    this.errors = errors;
    this.traceId = traceId;
  }
}

// ============================================================================
// File Upload Models
// ============================================================================

export interface PresignedUrlRequest {
  fileName: string;
  fileSize: number;
  mimeType: string;
  conversationId: string;
}

export interface PresignedUrlResponse {
  uploadUrl: string;
  fileId: string;
  blobPath: string;
  expiresAt: string;
}

export interface FileMetadata {
  id: string;
  conversationId: string;
  uploaderId: string;
  fileName: string;
  mimeType: string;
  size: number;
  blobPath: string;
  thumbnailPath?: string;
  createdAt: string;
}

export interface UploadProgress {
  loaded: number;
  total: number;
  percentage: number;
}
