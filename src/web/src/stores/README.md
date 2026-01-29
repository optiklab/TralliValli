# Zustand State Stores

This directory contains the Zustand state management stores for TralliValli.

## Overview

Three main stores are implemented:

1. **useAuthStore** - Authentication state management
2. **useConversationStore** - Conversation and message management
3. **usePresenceStore** - User presence/online status management

## Stores

### useAuthStore

Manages authentication state including user data and JWT tokens. 

**Features:**
- Persists auth state to localStorage
- Stores user profile and tokens
- Provides login, logout, and token refresh methods

**Usage:**

```typescript
import { useAuthStore } from '@stores';

function LoginComponent() {
  const { login, logout, user, isAuthenticated } = useAuthStore();

  const handleLogin = async (email: string, password: string) => {
    const response = await api.login(email, password);
    login(
      { id: response.userId, email: response.email, displayName: response.name },
      {
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        expiresAt: response.expiresAt,
        refreshExpiresAt: response.refreshExpiresAt,
      }
    );
  };

  return (
    <div>
      {isAuthenticated ? (
        <div>
          <p>Welcome, {user?.displayName}!</p>
          <button onClick={logout}>Logout</button>
        </div>
      ) : (
        <button onClick={() => handleLogin('user@example.com', 'password')}>
          Login
        </button>
      )}
    </div>
  );
}
```

**State:**
- `user: User | null` - Current user profile
- `token: string | null` - Access token
- `refreshToken: string | null` - Refresh token
- `expiresAt: Date | null` - Access token expiry
- `refreshExpiresAt: Date | null` - Refresh token expiry
- `isAuthenticated: boolean` - Authentication status

**Actions:**
- `login(user, tokens)` - Authenticate user and store tokens
- `logout()` - Clear authentication state
- `refresh(tokens)` - Update tokens after refresh
- `setUser(user)` - Update user profile
- `initialize()` - Initialize from stored tokens

### useConversationStore

Manages conversations and messages.

**Features:**
- Stores conversations and their messages
- Tracks active conversation
- Handles message CRUD operations
- Syncs with SignalR for real-time updates

**Usage:**

```typescript
import { useConversationStore } from '@stores';

function ConversationList() {
  const { 
    conversations, 
    activeConversationId,
    setActiveConversation,
    loadConversations,
  } = useConversationStore();

  useEffect(() => {
    // Load conversations from API
    api.getConversations().then(loadConversations);
  }, [loadConversations]);

  return (
    <div>
      {conversations.map((conv) => (
        <div 
          key={conv.id}
          onClick={() => setActiveConversation(conv.id)}
          className={activeConversationId === conv.id ? 'active' : ''}
        >
          {conv.name}
        </div>
      ))}
    </div>
  );
}

function MessageList() {
  const { messages, activeConversationId, addMessage } = useConversationStore();
  const conversationMessages = activeConversationId 
    ? messages[activeConversationId] || []
    : [];

  return (
    <div>
      {conversationMessages.map((msg) => (
        <div key={msg.id}>{msg.content}</div>
      ))}
    </div>
  );
}
```

**State:**
- `conversations: Conversation[]` - List of conversations
- `activeConversationId: string | null` - Currently selected conversation
- `messages: Record<string, Message[]>` - Messages by conversation ID
- `loading: boolean` - Loading state
- `error: string | null` - Error message

**Actions:**
- `loadConversations(conversations)` - Load conversations
- `loadMessages(conversationId, messages)` - Load messages for a conversation
- `addMessage(conversationId, message)` - Add a new message
- `setActiveConversation(conversationId)` - Set active conversation
- `updateConversation(conversation)` - Update a conversation
- `removeConversation(conversationId)` - Remove a conversation
- `updateMessage(conversationId, messageId, updates)` - Update a message
- `deleteMessage(conversationId, messageId)` - Mark message as deleted
- `markMessageAsRead(conversationId, messageId, userId, readAt)` - Mark message as read
- `setLoading(loading)` - Set loading state
- `setError(error)` - Set error state
- `clearMessages(conversationId)` - Clear messages for a conversation
- `reset()` - Reset to initial state

### usePresenceStore

Manages user presence and online status.

**Features:**
- Tracks which users are online
- Stores last seen timestamps
- Syncs with SignalR for real-time presence updates

**Usage:**

```typescript
import { usePresenceStore } from '@stores';

function UserStatus({ userId }: { userId: string }) {
  const { onlineUsers } = usePresenceStore();
  const presence = onlineUsers[userId];

  if (!presence) {
    return <span>Unknown</span>;
  }

  return (
    <div>
      <span className={presence.isOnline ? 'online' : 'offline'}>
        {presence.isOnline ? 'Online' : 'Offline'}
      </span>
      {presence.lastSeen && !presence.isOnline && (
        <span>Last seen: {presence.lastSeen.toLocaleString()}</span>
      )}
    </div>
  );
}
```

**State:**
- `onlineUsers: Record<string, UserPresence>` - User presence by user ID

**Actions:**
- `updatePresence(userId, isOnline, lastSeen)` - Update user presence
- `setUserOnline(userId)` - Mark user as online
- `setUserOffline(userId, lastSeen)` - Mark user as offline
- `removeUser(userId)` - Remove user from tracking
- `bulkUpdatePresence(updates)` - Update multiple users at once
- `reset()` - Reset to initial state

## SignalR Integration

The `useSignalRStoreIntegration` hook connects SignalR events to the stores:

```typescript
import { useSignalRStoreIntegration } from '@hooks/useSignalRStoreIntegration';
import { SignalRService } from '@services/signalr';

function App() {
  const signalRService = useMemo(
    () => new SignalRService({
      url: import.meta.env.VITE_SIGNALR_URL,
      accessTokenFactory: () => useAuthStore.getState().token || '',
    }),
    []
  );

  // Connect SignalR events to stores
  useSignalRStoreIntegration({
    signalRService,
    enabled: true,
  });

  useEffect(() => {
    signalRService.start();
    return () => {
      signalRService.stop();
    };
  }, [signalRService]);

  return <YourApp />;
}
```

**Events synchronized:**
- `onReceiveMessage` → Adds message to conversation store
- `onUserJoined` → Sets user as online in presence store
- `onUserLeft` → Sets user as offline in presence store
- `onMessageRead` → Updates message read status
- `onPresenceUpdate` → Updates user presence

## Testing

All stores have comprehensive unit tests:

```bash
npm test
```

Test files:
- `useAuthStore.test.ts` - Auth store tests
- `useConversationStore.test.ts` - Conversation store tests
- `usePresenceStore.test.ts` - Presence store tests

## Persistence

The auth store uses Zustand's `persist` middleware to save state to localStorage:

- **Storage key:** `auth-storage`
- **Persisted fields:** user, token, refreshToken, expiresAt, refreshExpiresAt, isAuthenticated
- **Hydration:** Automatic on app load

## Best Practices

1. **Use selectors for performance:**
   ```typescript
   // Good - only re-renders when user changes
   const user = useAuthStore((state) => state.user);
   
   // Less optimal - re-renders on any auth state change
   const { user } = useAuthStore();
   ```

2. **Initialize auth on app load:**
   ```typescript
   useEffect(() => {
     useAuthStore.getState().initialize();
   }, []);
   ```

3. **Clear stores on logout:**
   ```typescript
   const handleLogout = () => {
     useAuthStore.getState().logout();
     useConversationStore.getState().reset();
     usePresenceStore.getState().reset();
   };
   ```

4. **Use TypeScript for type safety:**
   All stores are fully typed with TypeScript interfaces.

## Architecture

```
┌─────────────────┐
│   Components    │
└────────┬────────┘
         │
         │ (use hooks)
         │
┌────────▼────────┐
│  Zustand Stores │
│  - Auth         │
│  - Conversation │
│  - Presence     │
└────────┬────────┘
         │
         │ (sync)
         │
┌────────▼────────┐
│  SignalR Events │
└─────────────────┘
```

## Future Enhancements

Potential improvements for the stores:

1. **Optimistic updates** - Update UI before API confirmation
2. **Offline support** - Queue actions when offline
3. **Message pagination** - Load messages in chunks
4. **Typing indicators** - Add typing state to conversation store
5. **Read receipts** - Track who read which messages
6. **Notifications** - Badge counts for unread messages
