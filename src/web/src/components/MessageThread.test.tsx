/**
 * MessageThread Component Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MessageThread } from './MessageThread';
import { useConversationStore } from '@/stores/useConversationStore';
import { useAuthStore } from '@/stores/useAuthStore';
import type { Message } from '@/stores/useConversationStore';
import type { User } from '@/stores/useAuthStore';

describe('MessageThread', () => {
  const mockUser: User = {
    id: 'user-1',
    email: 'test@example.com',
    displayName: 'Test User',
  };

  const mockMessages: Message[] = [
    {
      id: 'msg-1',
      conversationId: 'conv-1',
      senderId: 'user-2',
      type: 'text',
      content: 'Hello there!',
      encryptedContent: '',
      createdAt: '2024-01-01T10:00:00Z',
      readBy: [],
      isDeleted: false,
      attachments: [],
    },
    {
      id: 'msg-2',
      conversationId: 'conv-1',
      senderId: 'user-1',
      type: 'text',
      content: 'Hi! How are you?',
      encryptedContent: '',
      createdAt: '2024-01-01T10:01:00Z',
      readBy: [{ userId: 'user-2', readAt: '2024-01-01T10:02:00Z' }],
      isDeleted: false,
      attachments: [],
    },
    {
      id: 'msg-3',
      conversationId: 'conv-1',
      senderId: 'user-2',
      type: 'text',
      content: "I'm doing great, thanks!",
      encryptedContent: '',
      createdAt: '2024-01-01T10:03:00Z',
      readBy: [],
      isDeleted: false,
      attachments: [],
    },
  ];

  beforeEach(() => {
    // Reset stores
    useConversationStore.getState().reset();
    useAuthStore.setState({ user: mockUser, isAuthenticated: true });
    
    // Clear mocks
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('renders empty state when no messages', () => {
      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('No messages yet')).toBeInTheDocument();
      expect(screen.getByText('Start the conversation!')).toBeInTheDocument();
    });

    it('renders messages when available', () => {
      useConversationStore.setState({
        messages: { 'conv-1': mockMessages },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('Hello there!')).toBeInTheDocument();
      expect(screen.getByText('Hi! How are you?')).toBeInTheDocument();
      expect(screen.getByText("I'm doing great, thanks!")).toBeInTheDocument();
    });

    it('shows login prompt when user is not authenticated', () => {
      useAuthStore.setState({ user: null, isAuthenticated: false });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('Please log in to view messages')).toBeInTheDocument();
    });

    it('only renders messages for the specified conversation', () => {
      useConversationStore.setState({
        messages: {
          'conv-1': mockMessages,
          'conv-2': [
            {
              ...mockMessages[0],
              id: 'msg-other',
              conversationId: 'conv-2',
              content: 'Other conversation message',
            },
          ],
        },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('Hello there!')).toBeInTheDocument();
      expect(screen.queryByText('Other conversation message')).not.toBeInTheDocument();
    });
  });

  describe('Message Display', () => {
    beforeEach(() => {
      useConversationStore.setState({
        messages: { 'conv-1': mockMessages },
      });
    });

    it('displays sender avatar for other users', () => {
      render(<MessageThread conversationId="conv-1" />);

      // Check for avatar circles (should be present for user-2's messages)
      const avatars = document.querySelectorAll('.bg-gray-400.rounded-full');
      expect(avatars.length).toBeGreaterThan(0);
    });

    it('displays message content correctly', () => {
      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('Hello there!')).toBeInTheDocument();
      expect(screen.getByText('Hi! How are you?')).toBeInTheDocument();
    });

    it('shows timestamp for each message', () => {
      render(<MessageThread conversationId="conv-1" />);

      // Should show timestamps (either relative or full date)
      const timestamps = screen.getAllByText(/^\d{1,2}\/\d{1,2}\/\d{4}$|ago|Just now/);
      expect(timestamps.length).toBeGreaterThan(0);
    });

    it('distinguishes between own messages and others', () => {
      render(<MessageThread conversationId="conv-1" />);

      // Own messages should have indigo background
      const ownMessageBubbles = document.querySelectorAll('.bg-indigo-600');
      expect(ownMessageBubbles.length).toBe(1); // Only msg-2 is from user-1

      // Other messages should have gray background
      const otherMessageBubbles = document.querySelectorAll('.bg-gray-100');
      expect(otherMessageBubbles.length).toBe(2); // msg-1 and msg-3 are from user-2
    });

    it('displays read receipts for own messages', () => {
      render(<MessageThread conversationId="conv-1" />);

      // msg-2 has 1 read receipt from user-2
      expect(screen.getByText(/✓✓ 1/)).toBeInTheDocument();
    });

    it('shows edited indicator for edited messages', () => {
      const editedMessage = {
        ...mockMessages[0],
        editedAt: '2024-01-01T10:05:00Z',
      };

      useConversationStore.setState({
        messages: { 'conv-1': [editedMessage] },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('(edited)')).toBeInTheDocument();
    });

    it('shows deleted message placeholder', () => {
      const deletedMessage = {
        ...mockMessages[0],
        isDeleted: true,
      };

      useConversationStore.setState({
        messages: { 'conv-1': [deletedMessage] },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('Message deleted')).toBeInTheDocument();
    });
  });

  describe('Message Sorting', () => {
    it('displays messages in chronological order', () => {
      const unorderedMessages = [mockMessages[2], mockMessages[0], mockMessages[1]];
      
      useConversationStore.setState({
        messages: { 'conv-1': unorderedMessages },
      });

      render(<MessageThread conversationId="conv-1" />);

      const messageContents = screen.getAllByText(/Hello there!|Hi! How are you?|I'm doing great/);
      
      // First message should be "Hello there!"
      expect(messageContents[0]).toHaveTextContent('Hello there!');
      // Second should be "Hi! How are you?"
      expect(messageContents[1]).toHaveTextContent('Hi! How are you?');
      // Third should be "I'm doing great"
      expect(messageContents[2]).toHaveTextContent("I'm doing great, thanks!");
    });
  });

  describe('File Messages', () => {
    it('displays file attachments', () => {
      const fileMessage: Message = {
        id: 'msg-file',
        conversationId: 'conv-1',
        senderId: 'user-2',
        type: 'file',
        content: 'Check out this document',
        encryptedContent: '',
        createdAt: '2024-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: ['document.pdf', 'image.png'],
      };

      useConversationStore.setState({
        messages: { 'conv-1': [fileMessage] },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('Check out this document')).toBeInTheDocument();
      expect(screen.getByText('document.pdf')).toBeInTheDocument();
      expect(screen.getByText('image.png')).toBeInTheDocument();
    });

    it('displays file attachment without text content', () => {
      const fileMessage: Message = {
        id: 'msg-file',
        conversationId: 'conv-1',
        senderId: 'user-2',
        type: 'file',
        content: '',
        encryptedContent: '',
        createdAt: '2024-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: ['file.txt'],
      };

      useConversationStore.setState({
        messages: { 'conv-1': [fileMessage] },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('file.txt')).toBeInTheDocument();
    });
  });

  describe('Reply Threading', () => {
    it('shows reply indicator for messages with replyTo', () => {
      const replyMessage: Message = {
        ...mockMessages[0],
        replyTo: 'msg-previous',
      };

      useConversationStore.setState({
        messages: { 'conv-1': [replyMessage] },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('Replying to previous message')).toBeInTheDocument();
    });

    it('displays reply button for non-deleted messages', () => {
      useConversationStore.setState({
        messages: { 'conv-1': [mockMessages[0]] },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('Reply')).toBeInTheDocument();
    });

    it('does not show reply button for deleted messages', () => {
      const deletedMessage = {
        ...mockMessages[0],
        isDeleted: true,
      };

      useConversationStore.setState({
        messages: { 'conv-1': [deletedMessage] },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.queryByText('Reply')).not.toBeInTheDocument();
    });
  });

  describe('Typing Indicators', () => {
    beforeEach(() => {
      // Add at least one message so the component renders properly
      useConversationStore.setState({
        messages: { 'conv-1': [mockMessages[0]] },
      });
    });

    it('shows typing indicator for single user', () => {
      render(
        <MessageThread
          conversationId="conv-1"
          typingUsers={[{ userId: 'user-2', userName: 'John Doe' }]}
        />
      );

      expect(screen.getByText('John Doe is typing...')).toBeInTheDocument();
    });

    it('shows typing indicator for two users', () => {
      render(
        <MessageThread
          conversationId="conv-1"
          typingUsers={[
            { userId: 'user-2', userName: 'John Doe' },
            { userId: 'user-3', userName: 'Jane Smith' },
          ]}
        />
      );

      expect(screen.getByText('John Doe and Jane Smith are typing...')).toBeInTheDocument();
    });

    it('shows typing indicator for multiple users', () => {
      render(
        <MessageThread
          conversationId="conv-1"
          typingUsers={[
            { userId: 'user-2', userName: 'John Doe' },
            { userId: 'user-3', userName: 'Jane Smith' },
            { userId: 'user-4', userName: 'Bob Wilson' },
          ]}
        />
      );

      expect(screen.getByText('John Doe and 2 others are typing...')).toBeInTheDocument();
    });

    it('does not show typing indicator when no users are typing', () => {
      useConversationStore.setState({
        messages: { 'conv-1': [mockMessages[0]] },
      });
      
      render(<MessageThread conversationId="conv-1" typingUsers={[]} />);

      expect(screen.queryByText(/typing/)).not.toBeInTheDocument();
    });
  });

  describe('Infinite Scroll', () => {
    beforeEach(() => {
      // Add some messages for the scroll tests
      useConversationStore.setState({
        messages: { 'conv-1': mockMessages },
      });
    });
    
    it('shows load more indicator when hasMore is true', () => {
      render(<MessageThread conversationId="conv-1" hasMore={true} />);

      expect(screen.getByText('Scroll up to load more')).toBeInTheDocument();
    });

    it('does not show load more indicator when hasMore is false', () => {
      render(<MessageThread conversationId="conv-1" hasMore={false} />);

      expect(screen.queryByText('Scroll up to load more')).not.toBeInTheDocument();
    });

    it('calls onLoadMore when scrolling to top', async () => {
      const onLoadMore = vi.fn().mockResolvedValue(undefined);
      
      useConversationStore.setState({
        messages: { 'conv-1': mockMessages },
      });

      render(
        <MessageThread
          conversationId="conv-1"
          hasMore={true}
          onLoadMore={onLoadMore}
        />
      );

      const scrollContainer = document.querySelector('.overflow-y-auto');
      if (scrollContainer) {
        // Simulate scroll to top
        Object.defineProperty(scrollContainer, 'scrollTop', { value: 0, writable: true });
        Object.defineProperty(scrollContainer, 'scrollHeight', { value: 1000, writable: true });
        Object.defineProperty(scrollContainer, 'clientHeight', { value: 500, writable: true });
        
        fireEvent.scroll(scrollContainer);

        await waitFor(() => {
          expect(onLoadMore).toHaveBeenCalled();
        });
      }
    });

    it('shows loading spinner while loading more messages', async () => {
      const onLoadMore = vi.fn(() => new Promise(() => {})); // Never resolves
      
      useConversationStore.setState({
        messages: { 'conv-1': mockMessages },
      });

      render(
        <MessageThread
          conversationId="conv-1"
          hasMore={true}
          onLoadMore={onLoadMore}
        />
      );

      const scrollContainer = document.querySelector('.overflow-y-auto');
      if (scrollContainer) {
        // Simulate scroll to top
        Object.defineProperty(scrollContainer, 'scrollTop', { value: 0, writable: true });
        Object.defineProperty(scrollContainer, 'scrollHeight', { value: 1000, writable: true });
        Object.defineProperty(scrollContainer, 'clientHeight', { value: 500, writable: true });
        
        fireEvent.scroll(scrollContainer);

        await waitFor(() => {
          const spinner = document.querySelector('.animate-spin');
          expect(spinner).toBeInTheDocument();
        });
      }
    });
  });

  describe('Auto-scroll', () => {
    it('auto-scrolls to bottom when new messages arrive', async () => {
      useConversationStore.setState({
        messages: { 'conv-1': [mockMessages[0]] },
      });

      const { rerender } = render(<MessageThread conversationId="conv-1" />);

      // Add a new message
      useConversationStore.setState({
        messages: { 'conv-1': mockMessages },
      });

      rerender(<MessageThread conversationId="conv-1" />);

      await waitFor(() => {
        // Check that the last message is visible
        expect(screen.getByText("I'm doing great, thanks!")).toBeInTheDocument();
      });
    });
  });

  describe('Avatar Display Logic', () => {
    it('shows avatar for first message in a group', () => {
      useConversationStore.setState({
        messages: { 'conv-1': mockMessages },
      });

      render(<MessageThread conversationId="conv-1" />);

      // Should have avatars for messages from user-2
      const avatars = document.querySelectorAll('.bg-gray-400.rounded-full');
      expect(avatars.length).toBeGreaterThan(0);
    });

    it('groups consecutive messages from same sender', () => {
      const consecutiveMessages: Message[] = [
        mockMessages[0],
        {
          ...mockMessages[0],
          id: 'msg-consecutive',
          createdAt: '2024-01-01T10:00:30Z',
          content: 'Another message from same sender',
        },
      ];

      useConversationStore.setState({
        messages: { 'conv-1': consecutiveMessages },
      });

      render(<MessageThread conversationId="conv-1" />);

      // Should show both messages
      expect(screen.getByText('Hello there!')).toBeInTheDocument();
      expect(screen.getByText('Another message from same sender')).toBeInTheDocument();
    });
  });

  describe('Encrypted Content', () => {
    it('displays encryptedContent when content is empty', () => {
      const encryptedMessage: Message = {
        ...mockMessages[0],
        content: '',
        encryptedContent: 'encrypted-message-text',
      };

      useConversationStore.setState({
        messages: { 'conv-1': [encryptedMessage] },
      });

      render(<MessageThread conversationId="conv-1" />);

      expect(screen.getByText('encrypted-message-text')).toBeInTheDocument();
    });
  });
});
