# SignalR Documentation

This document describes the real-time communication capabilities of TraliVali using SignalR, including connection setup, authentication, available methods, and lifecycle management.

## Table of Contents

- [Hub URL](#hub-url)
- [Authentication](#authentication)
- [Client Methods](#client-methods)
- [Server Methods](#server-methods)
- [Connection Lifecycle](#connection-lifecycle)
- [Reconnection Handling](#reconnection-handling)
- [Message Format Examples](#message-format-examples)
- [TypeScript Client Usage](#typescript-client-usage)

## Hub URL

The SignalR ChatHub is available at:

```
/hubs/chat
```

**Full URL Example:**
```
https://your-domain.com/hubs/chat
ws://your-domain.com/hubs/chat
```

The hub automatically handles both WebSocket and long-polling transport mechanisms based on client capabilities.

## Authentication

The ChatHub requires JWT authentication using the `[Authorize]` attribute. Authentication can be provided in two ways:

### 1. Query String Parameter (Recommended for WebSocket)

For WebSocket connections, which cannot use custom headers, pass the JWT token as a query parameter:

```
/hubs/chat?access_token=YOUR_JWT_TOKEN
```

### 2. Authorization Header

For HTTP-based transports, use the standard Authorization header:

```
Authorization: Bearer YOUR_JWT_TOKEN
```

### JWT Token Requirements

The JWT token must include the following claims:
- **`userId`**: The unique identifier of the user
- **`displayName`**: The display name of the user

These claims are extracted by the hub to identify users in conversations and presence updates.

### Security Considerations

- Tokens passed via query string may appear in logs
- This is a standard practice for SignalR WebSocket authentication
- Ensure tokens are short-lived and properly validated
- Use HTTPS in production to protect tokens in transit

## Client Methods

Client methods are invoked by the server to push updates to connected clients. These methods are defined in the `IChatClient` interface.

### ReceiveMessage

Called when a new message is received in a conversation.

**Signature:**
```csharp
Task ReceiveMessage(string conversationId, string messageId, string senderId, string senderName, string content, DateTime timestamp)
```

**Parameters:**
- `conversationId`: The ID of the conversation
- `messageId`: The unique ID of the message
- `senderId`: The ID of the user who sent the message
- `senderName`: The display name of the sender
- `content`: The message content (encrypted)
- `timestamp`: UTC timestamp when the message was sent

**Example:**
```typescript
connection.on('ReceiveMessage', (conversationId, messageId, senderId, senderName, content, timestamp) => {
  console.log(`Message from ${senderName}: ${content}`);
});
```

### UserJoined

Called when a user joins a conversation.

**Signature:**
```csharp
Task UserJoined(string conversationId, string userId, string userName)
```

**Parameters:**
- `conversationId`: The ID of the conversation
- `userId`: The ID of the user who joined
- `userName`: The display name of the user who joined

**Example:**
```typescript
connection.on('UserJoined', (conversationId, userId, userName) => {
  console.log(`${userName} joined conversation ${conversationId}`);
});
```

### UserLeft

Called when a user leaves a conversation.

**Signature:**
```csharp
Task UserLeft(string conversationId, string userId, string userName)
```

**Parameters:**
- `conversationId`: The ID of the conversation
- `userId`: The ID of the user who left
- `userName`: The display name of the user who left

**Example:**
```typescript
connection.on('UserLeft', (conversationId, userId, userName) => {
  console.log(`${userName} left conversation ${conversationId}`);
});
```

### TypingIndicator

Called when a user starts or stops typing in a conversation.

**Signature:**
```csharp
Task TypingIndicator(string conversationId, string userId, string userName, bool isTyping)
```

**Parameters:**
- `conversationId`: The ID of the conversation
- `userId`: The ID of the user typing
- `userName`: The display name of the user typing
- `isTyping`: `true` if the user started typing, `false` if they stopped

**Example:**
```typescript
connection.on('TypingIndicator', (conversationId, userId, userName, isTyping) => {
  if (isTyping) {
    console.log(`${userName} is typing...`);
  } else {
    console.log(`${userName} stopped typing`);
  }
});
```

### MessageRead

Called when a user has read a message.

**Signature:**
```csharp
Task MessageRead(string conversationId, string messageId, string userId)
```

**Parameters:**
- `conversationId`: The ID of the conversation
- `messageId`: The ID of the message that was read
- `userId`: The ID of the user who read the message

**Example:**
```typescript
connection.on('MessageRead', (conversationId, messageId, userId) => {
  console.log(`User ${userId} read message ${messageId}`);
});
```

### PresenceUpdate

Called when a user's online/offline status changes.

**Signature:**
```csharp
Task PresenceUpdate(string userId, bool isOnline, DateTime? lastSeen)
```

**Parameters:**
- `userId`: The ID of the user
- `isOnline`: `true` if the user is online, `false` if offline
- `lastSeen`: UTC timestamp of when the user was last seen (only provided when `isOnline` is `false`)

**Example:**
```typescript
connection.on('PresenceUpdate', (userId, isOnline, lastSeen) => {
  if (isOnline) {
    console.log(`User ${userId} is now online`);
  } else {
    console.log(`User ${userId} was last seen at ${lastSeen}`);
  }
});
```

## Server Methods

Server methods are invoked by clients to send data or trigger actions on the server. These methods are implemented in the `ChatHub` class.

### SendMessage

Sends a message to all users in a conversation.

**Signature:**
```csharp
Task SendMessage(string conversationId, string messageId, string content)
```

**Parameters:**
- `conversationId`: The ID of the conversation (required)
- `messageId`: The unique ID of the message (required)
- `content`: The message content (required)

**Behavior:**
- Broadcasts the message to all members of the conversation group
- Automatically includes the sender's `userId` and `displayName` from JWT claims
- Includes a UTC timestamp

**Example:**
```typescript
await connection.invoke('SendMessage', 'conv123', 'msg456', 'Hello, World!');
```

**Validation:**
- Throws `ArgumentException` if any parameter is null or whitespace

### JoinConversation

Joins a conversation group to receive real-time updates.

**Signature:**
```csharp
Task JoinConversation(string conversationId)
```

**Parameters:**
- `conversationId`: The ID of the conversation to join (required)

**Behavior:**
- Adds the connection to the specified conversation group
- Notifies all group members that the user joined

**Example:**
```typescript
await connection.invoke('JoinConversation', 'conv123');
```

**Validation:**
- Throws `ArgumentException` if `conversationId` is null or whitespace

**Note:** Users must join a conversation before they can receive messages from it.

### LeaveConversation

Leaves a conversation group.

**Signature:**
```csharp
Task LeaveConversation(string conversationId)
```

**Parameters:**
- `conversationId`: The ID of the conversation to leave (required)

**Behavior:**
- Removes the connection from the specified conversation group
- Notifies remaining group members that the user left

**Example:**
```typescript
await connection.invoke('LeaveConversation', 'conv123');
```

**Validation:**
- Throws `ArgumentException` if `conversationId` is null or whitespace

### StartTyping

Notifies other users in a conversation that the current user is typing.

**Signature:**
```csharp
Task StartTyping(string conversationId)
```

**Parameters:**
- `conversationId`: The ID of the conversation (required)

**Behavior:**
- Sends typing indicator to other users in the conversation (not to the caller)
- Uses `Clients.OthersInGroup()` to exclude the sender

**Example:**
```typescript
await connection.invoke('StartTyping', 'conv123');
```

**Validation:**
- Throws `ArgumentException` if `conversationId` is null or whitespace

**Best Practice:** Call `StopTyping` after a timeout or when the user stops typing.

### StopTyping

Notifies other users in a conversation that the current user stopped typing.

**Signature:**
```csharp
Task StopTyping(string conversationId)
```

**Parameters:**
- `conversationId`: The ID of the conversation (required)

**Behavior:**
- Sends typing stopped indicator to other users in the conversation
- Uses `Clients.OthersInGroup()` to exclude the sender

**Example:**
```typescript
await connection.invoke('StopTyping', 'conv123');
```

**Validation:**
- Throws `ArgumentException` if `conversationId` is null or whitespace

### MarkAsRead

Marks a message as read by the current user.

**Signature:**
```csharp
Task MarkAsRead(string conversationId, string messageId)
```

**Parameters:**
- `conversationId`: The ID of the conversation (required)
- `messageId`: The ID of the message to mark as read (required)

**Behavior:**
- Notifies all conversation members that the user read the message
- Useful for implementing read receipts

**Example:**
```typescript
await connection.invoke('MarkAsRead', 'conv123', 'msg456');
```

**Validation:**
- Throws `ArgumentException` if any parameter is null or whitespace

## Connection Lifecycle

The ChatHub manages user presence and connection state through lifecycle events.

### OnConnectedAsync

Triggered when a client successfully connects to the hub.

**Automatic Actions:**
1. Extracts `userId` from JWT claims
2. Marks the user as online in the presence service
3. Stores the connection ID for the user
4. Broadcasts `PresenceUpdate` to all clients with `isOnline=true`

**Multi-Connection Support:**
- Users can have multiple simultaneous connections
- The presence service tracks all connection IDs per user
- User appears online as long as at least one connection is active

**Example Flow:**
```
Client connects → JWT validated → userId extracted 
→ Presence service updated → All clients notified
```

### OnDisconnectedAsync

Triggered when a client disconnects from the hub.

**Automatic Actions:**
1. Extracts `userId` from JWT claims
2. Removes the specific connection ID from presence service
3. Checks if the user has other active connections
4. If no other connections exist:
   - Records the last-seen timestamp
   - Broadcasts `PresenceUpdate` to all clients with `isOnline=false` and `lastSeen` timestamp
5. If other connections exist:
   - No broadcast is sent (user still online)

**Exception Handling:**
- The `exception` parameter contains disconnect reason (if any)
- Logged for debugging purposes

**Example Flow:**
```
Client disconnects → Connection removed → Check remaining connections
→ If last connection: Set last-seen and broadcast offline status
→ If other connections remain: No broadcast
```

### Connection States

The client-side connection can be in one of the following states:

- **Disconnected**: Not connected to the server
- **Connecting**: Attempting to establish a connection
- **Connected**: Successfully connected and ready to communicate
- **Reconnecting**: Connection lost, attempting to reconnect
- **Disconnecting**: Gracefully closing the connection

## Reconnection Handling

The SignalR connection includes robust reconnection capabilities to handle network interruptions.

### Automatic Reconnection

The server-side hub is configured with automatic reconnection using `withAutomaticReconnect()`.

### Client-Side Reconnection Strategy

The TypeScript client implements exponential backoff with the following configuration:

**Default Settings:**
- **Initial Retry Delay**: 1000ms (1 second)
- **Maximum Retry Delay**: 30000ms (30 seconds)
- **Maximum Reconnect Attempts**: 10
- **Backoff Algorithm**: Exponential (delay doubles each attempt)

**Retry Delay Calculation:**
```typescript
delay = Math.min(initialDelay * Math.pow(2, attemptCount), maxDelay)
```

**Example Retry Sequence:**
```
Attempt 1: 1 second
Attempt 2: 2 seconds
Attempt 3: 4 seconds
Attempt 4: 8 seconds
Attempt 5: 16 seconds
Attempt 6+: 30 seconds (capped)
```

### Message Queuing During Disconnection

When the connection is lost, the client automatically queues outgoing messages:

1. **Queuing**: Messages sent while disconnected are stored in memory
2. **Persistence**: Each queued message includes a timestamp
3. **Replay**: Upon reconnection, queued messages are sent in order
4. **Failure Handling**: If a queued message fails to send, it's re-queued

**Example:**
```typescript
// This will queue automatically if disconnected
await signalRService.sendMessage('conv123', 'msg456', 'Hello!');
// Message is sent immediately when connection is restored
```

### Reconnection Events

The client provides hooks for monitoring connection state:

```typescript
connection.onreconnecting((error) => {
  console.log('Connection lost. Reconnecting...', error);
});

connection.onreconnected((connectionId) => {
  console.log('Reconnected with connection ID:', connectionId);
  // Queued messages are automatically sent
});

connection.onclose((error) => {
  console.log('Connection closed', error);
  // Maximum retry attempts reached or manually closed
});
```

### State Change Handler

Monitor all state transitions:

```typescript
const unsubscribe = signalRService.onStateChange((state) => {
  console.log('Connection state changed:', state);
  // Possible states: Disconnected, Connecting, Connected, Reconnecting, Disconnecting
});

// Later, to stop monitoring
unsubscribe();
```

### Best Practices for Reconnection

1. **UI Feedback**: Show connection status to users
2. **Retry Limits**: Don't retry indefinitely (max 10 attempts by default)
3. **Message Queuing**: Use the built-in queue for seamless UX
4. **State Monitoring**: Handle state changes in your application
5. **Manual Reconnection**: Allow users to manually trigger reconnection if automatic attempts fail

## Message Format Examples

### 1. Sending a Message

**Client to Server:**
```typescript
await connection.invoke('SendMessage', 'conv-001', 'msg-abc123', 'Hello, World!');
```

**Server to All Clients in Group:**
```json
{
  "method": "ReceiveMessage",
  "arguments": [
    "conv-001",
    "msg-abc123",
    "user-456",
    "John Doe",
    "Hello, World!",
    "2024-01-15T14:30:00.000Z"
  ]
}
```

### 2. Joining a Conversation

**Client to Server:**
```typescript
await connection.invoke('JoinConversation', 'conv-001');
```

**Server to All Clients in Group:**
```json
{
  "method": "UserJoined",
  "arguments": [
    "conv-001",
    "user-456",
    "John Doe"
  ]
}
```

### 3. Typing Indicator

**Client to Server (Start):**
```typescript
await connection.invoke('StartTyping', 'conv-001');
```

**Server to Other Clients in Group:**
```json
{
  "method": "TypingIndicator",
  "arguments": [
    "conv-001",
    "user-456",
    "John Doe",
    true
  ]
}
```

**Client to Server (Stop):**
```typescript
await connection.invoke('StopTyping', 'conv-001');
```

**Server to Other Clients in Group:**
```json
{
  "method": "TypingIndicator",
  "arguments": [
    "conv-001",
    "user-456",
    "John Doe",
    false
  ]
}
```

### 4. Marking Message as Read

**Client to Server:**
```typescript
await connection.invoke('MarkAsRead', 'conv-001', 'msg-abc123');
```

**Server to All Clients in Group:**
```json
{
  "method": "MessageRead",
  "arguments": [
    "conv-001",
    "msg-abc123",
    "user-456"
  ]
}
```

### 5. Presence Updates

**User Connects:**
```json
{
  "method": "PresenceUpdate",
  "arguments": [
    "user-456",
    true,
    null
  ]
}
```

**User Disconnects (Last Connection):**
```json
{
  "method": "PresenceUpdate",
  "arguments": [
    "user-456",
    false,
    "2024-01-15T14:35:00.000Z"
  ]
}
```

### 6. Leaving a Conversation

**Client to Server:**
```typescript
await connection.invoke('LeaveConversation', 'conv-001');
```

**Server to Remaining Clients in Group:**
```json
{
  "method": "UserLeft",
  "arguments": [
    "conv-001",
    "user-456",
    "John Doe"
  ]
}
```

## TypeScript Client Usage

### Basic Setup

```typescript
import { SignalRService } from './services/signalr';

// Initialize the service
const signalRService = new SignalRService({
  url: 'https://your-domain.com/hubs/chat',
  accessTokenFactory: () => getAuthToken(), // Your JWT token
  automaticReconnect: true,
  maxReconnectAttempts: 10,
  logLevel: LogLevel.Information
});

// Register event handlers
signalRService.on({
  onReceiveMessage: (conversationId, messageId, senderId, senderName, content, timestamp) => {
    console.log(`Message from ${senderName}: ${content}`);
  },
  onPresenceUpdate: (userId, isOnline, lastSeen) => {
    console.log(`User ${userId} is ${isOnline ? 'online' : 'offline'}`);
  },
  onTypingIndicator: (conversationId, userId, userName, isTyping) => {
    console.log(`${userName} is ${isTyping ? 'typing' : 'not typing'}`);
  }
});

// Start the connection
await signalRService.start();

// Join a conversation
await signalRService.joinConversation('conv-001');

// Send a message
await signalRService.sendMessage('conv-001', 'msg-123', 'Hello!');
```

### Connection State Monitoring

```typescript
// Monitor connection state changes
const unsubscribe = signalRService.onStateChange((state) => {
  switch (state) {
    case ConnectionState.Connected:
      console.log('Connected to SignalR');
      break;
    case ConnectionState.Reconnecting:
      console.log('Connection lost, attempting to reconnect...');
      break;
    case ConnectionState.Disconnected:
      console.log('Disconnected from SignalR');
      break;
  }
});

// Check current state
if (signalRService.isConnected()) {
  console.log('Connection is active');
}
```

### Cleanup

```typescript
// When done, stop the connection
await signalRService.stop();
```

## Error Handling

### Server-Side Errors

All server methods validate input parameters and throw `ArgumentException` for invalid input:

```csharp
if (string.IsNullOrWhiteSpace(conversationId))
    throw new ArgumentException("Conversation ID is required", nameof(conversationId));
```

### Client-Side Error Handling

```typescript
try {
  await signalRService.sendMessage('conv-001', 'msg-123', 'Hello!');
} catch (error) {
  console.error('Failed to send message:', error);
  // Message will be queued if connection is lost
  // Otherwise, this is a validation or server error
}
```

### Authentication Errors

If JWT validation fails:
- Connection attempt will fail with authentication error
- Check JWT token validity and claims
- Ensure `userId` and `displayName` claims are present

### Missing Claims Error

If user claims are not found:
```
InvalidOperationException: "User ID claim not found. Authentication may have failed."
```

Ensure your JWT contains:
- `userId` claim
- `displayName` claim

## Performance Considerations

### Scaling Considerations

1. **Presence Broadcasting**: Currently broadcasts presence to all clients
   - Consider targeted updates to users with shared conversations at scale
   - Documented in code TODOs for future optimization

2. **Authorization**: Currently no per-conversation authorization checks
   - Implement participant verification before allowing operations
   - Documented in code TODOs for future security enhancement

3. **Connection Limits**: Monitor concurrent connections
   - Use SignalR backplane (Redis, Azure SignalR Service) for horizontal scaling

### Best Practices

1. **Join Before Send**: Always join a conversation before sending messages
2. **Leave on Navigate**: Leave conversations when navigating away
3. **Throttle Typing**: Implement client-side throttling for typing indicators
4. **Connection Pooling**: Reuse single connection across application
5. **Message Size**: Keep message content reasonable (handle large files separately)

## Additional Resources

- [ASP.NET Core SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [SignalR TypeScript Client](https://docs.microsoft.com/en-us/javascript/api/@microsoft/signalr/)
- [JWT Authentication with SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz)
