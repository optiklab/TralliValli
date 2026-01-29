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
