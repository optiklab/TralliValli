# Services Documentation

This directory contains service layer implementations for interacting with backend services.

## Table of Contents

1. [SignalR Client Service](#signalr-client-service)
2. [API Client Service](#api-client-service)

---

# SignalR Client Service

A robust SignalR client service for managing WebSocket connections with auto-reconnection, message queuing, and typed event handlers.

## Features

- **WebSocket Connection Management**: Handles connection lifecycle (connecting, connected, disconnecting, disconnected)
- **Auto-Reconnection**: Automatically reconnects with exponential backoff when connection is lost
- **Message Queuing**: Queues messages during disconnection and processes them when connection is restored
- **Typed Methods**: Strongly-typed methods matching the IChatClient interface
- **Connection State Events**: Subscribe to connection state changes for UI updates
- **Configurable Options**: Customize reconnection behavior, retry delays, and logging

## Installation

The SignalR client package is already installed:

```bash
npm install @microsoft/signalr
```

## Usage

### Basic Setup

```typescript
import { SignalRService, ConnectionState } from '@services';

// Create service instance
const signalRService = new SignalRService({
  url: 'http://localhost:5000/hubs/chat',
  accessTokenFactory: () => getAuthToken(), // Your auth token provider
});

// Start the connection
await signalRService.start();
```

### Advanced Configuration

```typescript
const signalRService = new SignalRService({
  url: 'http://localhost:5000/hubs/chat',
  accessTokenFactory: () => getAuthToken(),
  automaticReconnect: true,         // Enable auto-reconnection (default: true)
  maxReconnectAttempts: 10,         // Max reconnection attempts (default: 10)
  initialRetryDelayMs: 1000,        // Initial retry delay in ms (default: 1000)
  maxRetryDelayMs: 30000,           // Max retry delay in ms (default: 30000)
  logLevel: LogLevel.Information,   // Logging level (default: Information)
});
```

### Handling Events

Register event handlers to receive real-time updates:

```typescript
signalRService.on({
  onReceiveMessage: (conversationId, messageId, senderId, senderName, content, timestamp) => {
    console.log(`${senderName}: ${content}`);
    // Update UI with new message
  },

  onUserJoined: (conversationId, userId, userName) => {
    console.log(`${userName} joined the conversation`);
  },

  onUserLeft: (conversationId, userId, userName) => {
    console.log(`${userName} left the conversation`);
  },

  onTypingIndicator: (conversationId, userId, userName, isTyping) => {
    if (isTyping) {
      console.log(`${userName} is typing...`);
    }
  },

  onMessageRead: (conversationId, messageId, userId) => {
    console.log(`Message ${messageId} was read`);
  },

  onPresenceUpdate: (userId, isOnline, lastSeen) => {
    console.log(`User ${userId} is ${isOnline ? 'online' : 'offline'}`);
  },
});
```

### Monitoring Connection State

```typescript
// Subscribe to connection state changes
const unsubscribe = signalRService.onStateChange((state) => {
  switch (state) {
    case ConnectionState.Connecting:
      console.log('Connecting to server...');
      break;
    case ConnectionState.Connected:
      console.log('Connected!');
      break;
    case ConnectionState.Reconnecting:
      console.log('Reconnecting...');
      break;
    case ConnectionState.Disconnected:
      console.log('Disconnected');
      break;
  }
});

// Check current connection state
if (signalRService.isConnected()) {
  console.log('Service is connected');
}

// Unsubscribe when no longer needed
unsubscribe();
```

### Sending Messages

```typescript
// Send a message
await signalRService.sendMessage('conversation-123', 'message-456', 'Hello, world!');

// Join a conversation
await signalRService.joinConversation('conversation-123');

// Leave a conversation
await signalRService.leaveConversation('conversation-123');

// Typing indicators
await signalRService.startTyping('conversation-123');
await signalRService.stopTyping('conversation-123');

// Mark message as read
await signalRService.markAsRead('conversation-123', 'message-456');
```

### Message Queuing

Messages sent while disconnected are automatically queued and sent when the connection is restored:

```typescript
// These messages will be queued if connection is lost
await signalRService.sendMessage('conv-1', 'msg-1', 'Message 1');
await signalRService.sendMessage('conv-1', 'msg-2', 'Message 2');

// When connection is restored, queued messages are automatically sent
```

### Stopping the Connection

```typescript
// Stop the connection
await signalRService.stop();
```

## API Reference

### Constructor Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `url` | `string` | (required) | SignalR hub URL |
| `accessTokenFactory` | `() => string \| Promise<string>` | `() => ''` | Function that returns auth token |
| `automaticReconnect` | `boolean` | `true` | Enable automatic reconnection |
| `maxReconnectAttempts` | `number` | `10` | Maximum reconnection attempts |
| `initialRetryDelayMs` | `number` | `1000` | Initial retry delay in milliseconds |
| `maxRetryDelayMs` | `number` | `30000` | Maximum retry delay in milliseconds |
| `logLevel` | `LogLevel` | `Information` | SignalR logging level |

### Methods

#### Connection Management

- `start(): Promise<void>` - Start the SignalR connection
- `stop(): Promise<void>` - Stop the SignalR connection
- `getState(): ConnectionState` - Get current connection state
- `isConnected(): boolean` - Check if connection is active

#### Event Registration

- `on(handlers: ChatClientHandlers): void` - Register event handlers
- `onStateChange(handler: ConnectionStateHandler): () => void` - Subscribe to state changes (returns unsubscribe function)

#### Hub Methods

- `sendMessage(conversationId: string, messageId: string, content: string): Promise<void>`
- `joinConversation(conversationId: string): Promise<void>`
- `leaveConversation(conversationId: string): Promise<void>`
- `startTyping(conversationId: string): Promise<void>`
- `stopTyping(conversationId: string): Promise<void>`
- `markAsRead(conversationId: string, messageId: string): Promise<void>`

### Event Handlers

All event handlers are optional:

```typescript
interface ChatClientHandlers {
  onReceiveMessage?: (conversationId: string, messageId: string, senderId: string, 
                      senderName: string, content: string, timestamp: Date) => void;
  onUserJoined?: (conversationId: string, userId: string, userName: string) => void;
  onUserLeft?: (conversationId: string, userId: string, userName: string) => void;
  onTypingIndicator?: (conversationId: string, userId: string, userName: string, 
                       isTyping: boolean) => void;
  onMessageRead?: (conversationId: string, messageId: string, userId: string) => void;
  onPresenceUpdate?: (userId: string, isOnline: boolean, lastSeen: Date | null) => void;
}
```

### Connection States

```typescript
const ConnectionState = {
  Disconnected: 'Disconnected',
  Connecting: 'Connecting',
  Connected: 'Connected',
  Reconnecting: 'Reconnecting',
  Disconnecting: 'Disconnecting',
} as const;
```

## Testing

The service includes comprehensive unit tests covering:

- Connection management
- Auto-reconnection with exponential backoff
- Message queuing and processing
- Typed methods
- Event handlers
- Connection state events

Run tests:

```bash
npm test
```

## Error Handling

The service handles common error scenarios:

- **Connection Failures**: Automatically retries with exponential backoff
- **Invoke Failures**: Re-queues messages if they fail to send
- **Max Retries**: Stops reconnection attempts after max retries reached

## Best Practices

1. **Token Management**: Provide a fresh token in `accessTokenFactory` to avoid expired tokens
2. **Event Handlers**: Register event handlers before calling `start()` to avoid missing early events
3. **Cleanup**: Call `stop()` when component unmounts to prevent memory leaks
4. **State Monitoring**: Use `onStateChange` to update UI based on connection state
5. **Error Handling**: Handle promise rejections when calling hub methods

## Example: React Hook

```typescript
import { useEffect, useState } from 'react';
import { SignalRService, ConnectionState } from '@services';

export function useSignalR(hubUrl: string) {
  const [service] = useState(() => new SignalRService({ url: hubUrl }));
  const [connectionState, setConnectionState] = useState(ConnectionState.Disconnected);

  useEffect(() => {
    const unsubscribe = service.onStateChange(setConnectionState);

    service.start().catch(console.error);

    return () => {
      unsubscribe();
      service.stop();
    };
  }, [service]);

  return { service, connectionState, isConnected: service.isConnected() };
}
```

## License

Part of the TralliValli project.

---

# API Client Service

A centralized HTTP API client with interceptors for JWT token injection, automatic token refresh, and typed error handling.

## Features

- **Fetch-based HTTP Client**: Modern fetch API with typed responses
- **JWT Token Management**: Automatic injection and refresh
- **Request Interceptors**: Modify requests before they're sent
- **Response Interceptors**: Handle responses globally
- **Typed Error Handling**: Custom `ApiErrorResponse` class
- **Token Refresh Deduplication**: Prevents multiple simultaneous refresh requests
- **Secure Token Storage**: LocalStorage with expiry checking

## Quick Start

```typescript
import { apiClient } from '@services/api';
import { ApiErrorResponse } from '@types/api';

// Make authenticated requests
try {
  const conversations = await apiClient.listConversations();
  console.log('Conversations:', conversations);
} catch (error) {
  if (error instanceof ApiErrorResponse) {
    console.error('API Error:', error.statusCode, error.message);
  }
}
```

## Authentication

### Request Magic Link

```typescript
const response = await apiClient.requestMagicLink({
  email: 'user@example.com',
  deviceId: 'device-123',
});
```

### Verify Magic Link

```typescript
const response = await apiClient.verifyMagicLink({
  token: 'magic-link-token',
});
// Tokens are automatically stored
```

### Logout

```typescript
import { getAccessToken } from '@utils/tokenStorage';

const accessToken = getAccessToken();
if (accessToken) {
  await apiClient.logout({ accessToken });
}
// Tokens are automatically cleared
```

## Conversations API

```typescript
// List conversations
const response = await apiClient.listConversations({ page: 1, pageSize: 20 });

// Create direct conversation
const conversation = await apiClient.createDirectConversation({
  otherUserId: 'user-456',
});

// Create group conversation
const group = await apiClient.createGroupConversation({
  name: 'Project Team',
  memberUserIds: ['user-456', 'user-789'],
});

// Get conversation
const conv = await apiClient.getConversation('conversation-123');

// Update group metadata
const updated = await apiClient.updateGroupMetadata('conversation-123', {
  name: 'New Name',
  metadata: { key: 'value' },
});

// Add member
const withMember = await apiClient.addMember('conversation-123', {
  userId: 'user-999',
  role: 'member',
});

// Remove member
const removed = await apiClient.removeMember('conversation-123', 'user-999');
```

## Messages API

```typescript
// List messages
const response = await apiClient.listMessages('conversation-123', {
  limit: 50,
  before: '2024-01-01T00:00:00Z', // Optional cursor
});

// Search messages
const results = await apiClient.searchMessages('conversation-123', {
  query: 'search term',
  limit: 20,
});

// Delete message
const deleted = await apiClient.deleteMessage('message-456');
```

## Error Handling

All API methods throw `ApiErrorResponse` on errors:

```typescript
import { ApiErrorResponse } from '@types/api';

try {
  await apiClient.someMethod();
} catch (error) {
  if (error instanceof ApiErrorResponse) {
    console.error('Status:', error.statusCode);
    console.error('Message:', error.message);
    console.error('Field errors:', error.errors);
    console.error('Trace ID:', error.traceId);
    
    // Handle specific status codes
    if (error.statusCode === 401) {
      // Redirect to login
    } else if (error.statusCode === 403) {
      // Show permission denied
    } else if (error.statusCode === 404) {
      // Show not found
    }
  }
}
```

## Token Storage

Token storage is handled automatically, but utilities are available:

```typescript
import {
  getAccessToken,
  getRefreshToken,
  isAuthenticated,
  isAccessTokenExpired,
  clearTokens,
} from '@utils/tokenStorage';

// Check authentication
if (isAuthenticated()) {
  console.log('User is logged in');
}

// Get tokens
const accessToken = getAccessToken();
const refreshToken = getRefreshToken();

// Check expiry
if (isAccessTokenExpired()) {
  console.log('Token needs refresh');
}

// Clear tokens
clearTokens();
```

## Custom Interceptors

Extend the API client with custom interceptors:

```typescript
import { apiClient } from '@services/api';

// Add custom request header
apiClient.addRequestInterceptor((request) => {
  request.headers = {
    ...request.headers,
    'X-Custom-Header': 'value',
  };
  return request;
});

// Log all responses
apiClient.addResponseInterceptor(async (response) => {
  console.log('Response:', response.status, response.url);
  return response;
});
```

## Automatic Token Refresh

The API client automatically refreshes tokens:

- Tokens are refreshed 60 seconds before expiry
- Refresh requests are deduplicated
- Failed refresh clears tokens and requires re-authentication

Manual refresh (usually not needed):

```typescript
import { getRefreshToken } from '@utils/tokenStorage';

const refreshToken = getRefreshToken();
if (refreshToken) {
  await apiClient.refresh({ refreshToken });
}
```

## Security Considerations

⚠️ **Important Security Notes:**

1. **XSS Vulnerability**: Tokens in localStorage are accessible to JavaScript
   - Implement Content Security Policy (CSP) headers
   - Sanitize all user inputs
   - Avoid third-party scripts when possible

2. **HTTPS Required**: Always use HTTPS in production
   - Prevents token interception
   - Required for secure authentication

3. **HttpOnly Cookies (Recommended for Production)**:
   - More secure than localStorage
   - Requires backend implementation
   - Not accessible to JavaScript

4. **Never expose tokens**:
   - Don't log tokens
   - Don't include in URLs
   - Don't send over insecure connections

## API Reference

### Authentication Methods

- `requestMagicLink(data: RequestMagicLinkRequest): Promise<RequestMagicLinkResponse>`
- `verifyMagicLink(data: VerifyMagicLinkRequest): Promise<VerifyMagicLinkResponse>`
- `refresh(data: RefreshTokenRequest): Promise<RefreshTokenResponse>`
- `logout(data: LogoutRequest): Promise<LogoutResponse>`
- `register(data: RegisterRequest): Promise<RegisterResponse>`
- `validateInvite(token: string): Promise<ValidateInviteResponse>`

### Conversation Methods

- `listConversations(params?: { page?: number; pageSize?: number }): Promise<PaginatedConversationsResponse>`
- `createDirectConversation(data: CreateDirectConversationRequest): Promise<ConversationResponse>`
- `createGroupConversation(data: CreateGroupConversationRequest): Promise<ConversationResponse>`
- `getConversation(id: string): Promise<ConversationResponse>`
- `updateGroupMetadata(id: string, data: UpdateGroupMetadataRequest): Promise<ConversationResponse>`
- `addMember(id: string, data: AddMemberRequest): Promise<ConversationResponse>`
- `removeMember(id: string, userId: string): Promise<ConversationResponse>`

### Message Methods

- `listMessages(conversationId: string, params?: { limit?: number; before?: string }): Promise<PaginatedMessagesResponse>`
- `searchMessages(conversationId: string, data: SearchMessagesRequest): Promise<SearchMessagesResponse>`
- `deleteMessage(messageId: string): Promise<MessageResponse>`

## Testing

Comprehensive tests included:

- Token storage: 26 tests
- API client: 25 tests
- All edge cases covered

Run tests:

```bash
npm test
```

## Example: React Hook

```typescript
import { useState, useEffect } from 'react';
import { apiClient } from '@services/api';
import { isAuthenticated } from '@utils/tokenStorage';

export function useConversations() {
  const [conversations, setConversations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    async function fetchConversations() {
      if (!isAuthenticated()) {
        setLoading(false);
        return;
      }

      try {
        const response = await apiClient.listConversations();
        setConversations(response.conversations);
      } catch (err) {
        setError(err);
      } finally {
        setLoading(false);
      }
    }

    fetchConversations();
  }, []);

  return { conversations, loading, error };
}
```

## License

Part of the TralliValli project.

---

# Key Management Service

A comprehensive key management service for per-conversation encryption with secure storage and rotation capabilities.

## Features

- **Per-conversation Key Derivation**: Derive unique encryption keys from shared secrets
- **HKDF-based Key Derivation**: Industry-standard key derivation (HKDF-SHA256)
- **Encrypted Storage**: Store conversation keys encrypted at rest in IndexedDB
- **Master Key Protection**: Conversation keys encrypted with user's master key
- **Key Rotation**: Full support for key rotation when group membership changes
- **Rotation History**: Audit trail of all key rotations
- **PBKDF2 Password Derivation**: Secure master key derivation from passwords

## Quick Start

```typescript
import { KeyManagementService } from '@/services';

// 1. Initialize service
const keyService = new KeyManagementService();
await keyService.initialize();

// 2. Set up master key
const { masterKey, salt } = await keyService.deriveMasterKeyFromPassword('user-password');
await keyService.setMasterKey(masterKey);

// 3. Derive conversation key from shared secret (X25519)
const sharedSecret = new Uint8Array(32); // From X25519 key exchange
const conversationKey = await keyService.deriveConversationKey(
  sharedSecret,
  'conversation-123'
);

// 4. Store the key (encrypted at rest)
await keyService.storeConversationKey('conversation-123', conversationKey);

// 5. Retrieve the key later
const key = await keyService.getConversationKey('conversation-123');
```

## Documentation

Complete implementation guide: `/TASK40_COMPLETE.md`

Comprehensive test suite with 37 tests - all passing ✅

## License

Part of the TralliValli project.
