# MessageThread Component

The `MessageThread` component displays a list of messages in a conversation with support for infinite scrolling, typing indicators, and auto-scrolling to new messages.

## Features

- ✅ Display messages with sender avatar, name, content, and timestamp
- ✅ Show read receipts indicating who has read each message
- ✅ Support for reply threading with visual indicators
- ✅ Handle both text and file messages
- ✅ Infinite scroll for loading older messages
- ✅ Typing indicators showing who is currently typing
- ✅ Auto-scroll to new messages
- ✅ Accessibility support with ARIA labels

## Usage

```tsx
import { MessageThread } from '@/components';

function ChatView() {
  const [typingUsers, setTypingUsers] = useState<Array<{ userId: string; userName: string }>>([]);
  const [hasMore, setHasMore] = useState(true);

  const handleLoadMore = async (cursor?: string) => {
    // Fetch older messages from API
    const response = await api.getMessages(conversationId, { cursor, limit: 30 });
    
    // Add messages to store
    conversationStore.loadMessages(conversationId, response.messages);
    
    // Update hasMore flag
    setHasMore(response.hasMore);
  };

  return (
    <MessageThread
      conversationId="conversation-123"
      onLoadMore={handleLoadMore}
      hasMore={hasMore}
      typingUsers={typingUsers}
    />
  );
}
```

## Props

### `MessageThreadProps`

| Prop | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `conversationId` | `string` | ✅ Yes | - | The ID of the conversation to display |
| `onLoadMore` | `(cursor?: string) => Promise<void>` | ❌ No | - | Callback when user scrolls to load older messages |
| `hasMore` | `boolean` | ❌ No | `false` | Whether there are more messages to load |
| `typingUsers` | `Array<{ userId: string; userName: string }>` | ❌ No | `[]` | List of users currently typing |

## Integration with SignalR

The component works seamlessly with SignalR for real-time updates:

```tsx
import { MessageThread } from '@/components';
import { useSignalR } from '@/hooks';

function ChatView({ conversationId }: { conversationId: string }) {
  const [typingUsers, setTypingUsers] = useState<Array<{ userId: string; userName: string }>>([]);
  
  useEffect(() => {
    // Subscribe to typing indicator events
    const handleTyping = (convId: string, userId: string, userName: string, isTyping: boolean) => {
      if (convId !== conversationId) return;
      
      setTypingUsers(prev => {
        if (isTyping) {
          // Add user to typing list if not already there
          if (!prev.find(u => u.userId === userId)) {
            return [...prev, { userId, userName }];
          }
          return prev;
        } else {
          // Remove user from typing list
          return prev.filter(u => u.userId !== userId);
        }
      });
    };
    
    signalRService.on('onTypingIndicator', handleTyping);
    
    return () => {
      signalRService.off('onTypingIndicator', handleTyping);
    };
  }, [conversationId]);

  return (
    <MessageThread
      conversationId={conversationId}
      typingUsers={typingUsers}
      hasMore={true}
      onLoadMore={handleLoadMore}
    />
  );
}
```

## Message Types

The component supports various message types:

### Text Messages
```tsx
{
  id: 'msg-1',
  conversationId: 'conv-1',
  senderId: 'user-123',
  type: 'text',
  content: 'Hello, world!',
  createdAt: '2024-01-01T10:00:00Z',
  readBy: [],
  isDeleted: false,
  attachments: []
}
```

### File Messages
```tsx
{
  id: 'msg-2',
  conversationId: 'conv-1',
  senderId: 'user-123',
  type: 'file',
  content: 'Check out this document',
  createdAt: '2024-01-01T10:01:00Z',
  readBy: [],
  isDeleted: false,
  attachments: ['document.pdf', 'image.png']
}
```

### Reply Messages
```tsx
{
  id: 'msg-3',
  conversationId: 'conv-1',
  senderId: 'user-456',
  type: 'text',
  content: 'Thanks!',
  replyTo: 'msg-1',
  createdAt: '2024-01-01T10:02:00Z',
  readBy: [],
  isDeleted: false,
  attachments: []
}
```

## Styling

The component uses Tailwind CSS for styling and follows the application's design system:

- Own messages: `bg-indigo-600 text-white`
- Other messages: `bg-gray-100 text-gray-900`
- Avatars: Circular with first letter of sender name
- Read receipts: Double checkmark (✓✓) with count
- Typing indicators: Animated dots with user names

## Accessibility

The component includes comprehensive accessibility features:

- ARIA labels for screen readers
- Keyboard navigation support
- Screen reader announcements for typing indicators
- Alternative text for icons and images
- Semantic HTML structure

## Testing

The component has 29 comprehensive tests covering:

- Message display and rendering
- Infinite scroll functionality
- Typing indicators
- Auto-scroll behavior
- File attachments
- Reply threading
- Empty states
- Accessibility features

Run tests with:

```bash
npm test -- MessageThread
```

## Performance Considerations

- Messages are memoized to prevent unnecessary re-renders
- Scroll position is maintained when loading older messages
- Auto-scroll is disabled when user scrolls up manually
- Typing indicators are debounced to reduce updates

## Future Enhancements

Potential improvements for future iterations:

- [ ] Message reactions (emoji)
- [ ] Message editing inline
- [ ] Drag and drop file uploads
- [ ] Image/video previews
- [ ] Voice message support
- [ ] Message search and filtering
- [ ] Jump to date functionality
- [ ] Unread message indicator
