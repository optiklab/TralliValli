/**
 * ConversationList Component Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ConversationList } from './ConversationList';
import { useConversationStore } from '@/stores/useConversationStore';
import { usePresenceStore } from '@/stores/usePresenceStore';
import { useAuthStore } from '@/stores/useAuthStore';
import type { Conversation, Message } from '@/stores/useConversationStore';
import type { User } from '@/stores/useAuthStore';

describe('ConversationList', () => {
  const mockUser: User = {
    id: 'user-1',
    email: 'test@example.com',
    displayName: 'Test User',
  };

  const mockConversations: Conversation[] = [
    {
      id: 'conv-1',
      type: 'direct',
      name: 'John Doe',
      isGroup: false,
      participants: [
        { userId: 'user-1', joinedAt: '2024-01-01T00:00:00Z', role: 'member' },
        { userId: 'user-2', joinedAt: '2024-01-01T00:00:00Z', role: 'member' },
      ],
      recentMessages: [],
      createdAt: '2024-01-01T00:00:00Z',
      lastMessageAt: '2024-01-01T12:00:00Z',
      metadata: {},
    },
    {
      id: 'conv-2',
      type: 'group',
      name: 'Team Chat',
      isGroup: true,
      participants: [
        { userId: 'user-1', joinedAt: '2024-01-01T00:00:00Z', role: 'member' },
        { userId: 'user-3', joinedAt: '2024-01-01T00:00:00Z', role: 'member' },
        { userId: 'user-4', joinedAt: '2024-01-01T00:00:00Z', role: 'member' },
      ],
      recentMessages: [],
      createdAt: '2024-01-01T00:00:00Z',
      lastMessageAt: '2024-01-01T10:00:00Z',
      metadata: {},
    },
    {
      id: 'conv-3',
      type: 'direct',
      name: 'Jane Smith',
      isGroup: false,
      participants: [
        { userId: 'user-1', joinedAt: '2024-01-01T00:00:00Z', role: 'member' },
        { userId: 'user-5', joinedAt: '2024-01-01T00:00:00Z', role: 'member' },
      ],
      recentMessages: [],
      createdAt: '2024-01-01T00:00:00Z',
      lastMessageAt: '2024-01-01T14:00:00Z',
      metadata: {},
    },
  ];

  const mockMessages: Record<string, Message[]> = {
    'conv-1': [
      {
        id: 'msg-1',
        conversationId: 'conv-1',
        senderId: 'user-2',
        type: 'text',
        content: 'Hello there!',
        encryptedContent: '',
        createdAt: '2024-01-01T12:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      },
    ],
    'conv-2': [
      {
        id: 'msg-2',
        conversationId: 'conv-2',
        senderId: 'user-3',
        type: 'text',
        content: 'Team meeting at 3pm',
        encryptedContent: '',
        createdAt: '2024-01-01T10:00:00Z',
        readBy: [{ userId: 'user-1', readAt: '2024-01-01T10:01:00Z' }],
        isDeleted: false,
        attachments: [],
      },
    ],
    'conv-3': [
      {
        id: 'msg-3',
        conversationId: 'conv-3',
        senderId: 'user-5',
        type: 'text',
        content: 'Can you review the document I sent?',
        encryptedContent: '',
        createdAt: '2024-01-01T14:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      },
    ],
  };

  beforeEach(() => {
    // Reset all stores
    useConversationStore.getState().reset();
    usePresenceStore.getState().reset();
    useAuthStore.setState({ user: mockUser, isAuthenticated: true });

    // Clear mocks
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('renders the search input', () => {
      render(<ConversationList />);
      expect(screen.getByPlaceholderText('Search conversations...')).toBeInTheDocument();
    });

    it('displays empty state when no conversations', () => {
      render(<ConversationList />);

      expect(screen.getByText('No conversations yet')).toBeInTheDocument();
      expect(screen.getByText('Start a conversation to get started')).toBeInTheDocument();
    });

    it('displays conversations when available', () => {
      useConversationStore.setState({ conversations: mockConversations });

      render(<ConversationList />);

      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.getByText('Team Chat')).toBeInTheDocument();
      expect(screen.getByText('Jane Smith')).toBeInTheDocument();
    });
  });

  describe('Sorting by lastMessageAt', () => {
    it('sorts conversations by lastMessageAt in descending order (most recent first)', () => {
      useConversationStore.setState({ conversations: mockConversations });

      render(<ConversationList />);

      const conversationElements = screen.getAllByRole('button');

      // conv-3 (14:00) should be first, conv-1 (12:00) second, conv-2 (10:00) third
      expect(conversationElements[0]).toHaveTextContent('Jane Smith');
      expect(conversationElements[1]).toHaveTextContent('John Doe');
      expect(conversationElements[2]).toHaveTextContent('Team Chat');
    });

    it('handles conversations without lastMessageAt', () => {
      const conversationsWithoutTime: Conversation[] = [
        { ...mockConversations[0], lastMessageAt: undefined },
        mockConversations[1],
      ];

      useConversationStore.setState({ conversations: conversationsWithoutTime });

      render(<ConversationList />);

      // Conversation with lastMessageAt should appear first
      const conversationElements = screen.getAllByRole('button');
      expect(conversationElements[0]).toHaveTextContent('Team Chat');
      expect(conversationElements[1]).toHaveTextContent('John Doe');
    });
  });

  describe('Display elements', () => {
    beforeEach(() => {
      useConversationStore.setState({
        conversations: mockConversations,
        messages: mockMessages,
      });
    });

    it('displays conversation name', () => {
      render(<ConversationList />);

      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.getByText('Team Chat')).toBeInTheDocument();
    });

    it('displays conversation avatar (first letter)', () => {
      render(<ConversationList />);

      const avatars = screen.getAllByText('J'); // John Doe and Jane Smith
      expect(avatars.length).toBe(2);
      expect(screen.getByText('T')).toBeInTheDocument(); // Team Chat
    });

    it('displays last message preview', () => {
      render(<ConversationList />);

      expect(screen.getByText('Hello there!')).toBeInTheDocument();
      expect(screen.getByText('Team meeting at 3pm')).toBeInTheDocument();
      expect(screen.getByText('Can you review the document I sent?')).toBeInTheDocument();
    });

    it('shows "No messages yet" when conversation has no messages', () => {
      useConversationStore.setState({
        conversations: [mockConversations[0]],
        messages: {},
      });

      render(<ConversationList />);

      expect(screen.getByText('No messages yet')).toBeInTheDocument();
    });

    it('truncates long message previews', () => {
      const longMessage =
        'This is a very long message that should be truncated to fit in the preview area without breaking the layout';

      useConversationStore.setState({
        conversations: [mockConversations[0]],
        messages: {
          'conv-1': [
            {
              ...mockMessages['conv-1'][0],
              content: longMessage,
            },
          ],
        },
      });

      render(<ConversationList />);

      expect(screen.queryByText(longMessage)).not.toBeInTheDocument();
      expect(
        screen.getByText(/This is a very long message that should be trunca.../)
      ).toBeInTheDocument();
    });

    it('shows "Message deleted" for deleted messages', () => {
      useConversationStore.setState({
        conversations: [mockConversations[0]],
        messages: {
          'conv-1': [
            {
              ...mockMessages['conv-1'][0],
              isDeleted: true,
            },
          ],
        },
      });

      render(<ConversationList />);

      expect(screen.getByText('Message deleted')).toBeInTheDocument();
    });
  });

  describe('Unread count badge', () => {
    it('displays unread count for conversations with unread messages', () => {
      useConversationStore.setState({
        conversations: mockConversations,
        messages: mockMessages,
      });

      render(<ConversationList />);

      // conv-1 has 1 unread message (from user-2)
      const unreadBadges = screen.getAllByText('1');
      expect(unreadBadges.length).toBeGreaterThan(0);
    });

    it('does not display badge when all messages are read', () => {
      useConversationStore.setState({
        conversations: [mockConversations[1]],
        messages: { 'conv-2': mockMessages['conv-2'] },
      });

      render(<ConversationList />);

      // conv-2 has message marked as read by user-1
      expect(screen.queryByText('1')).not.toBeInTheDocument();
    });

    it('does not count messages sent by current user as unread', () => {
      useConversationStore.setState({
        conversations: [mockConversations[0]],
        messages: {
          'conv-1': [
            {
              ...mockMessages['conv-1'][0],
              senderId: 'user-1', // Current user sent the message
            },
          ],
        },
      });

      render(<ConversationList />);

      // Should not show unread badge for own messages
      expect(screen.queryByText('1')).not.toBeInTheDocument();
    });
  });

  describe('Online indicator', () => {
    it('displays online indicator for direct chats when other user is online', () => {
      useConversationStore.setState({ conversations: [mockConversations[0]] });
      usePresenceStore.setState({
        onlineUsers: {
          'user-2': { userId: 'user-2', isOnline: true, lastSeen: null },
        },
      });

      render(<ConversationList />);

      // Check for the online indicator (green dot)
      const onlineIndicators = document.querySelectorAll('.bg-green-400');
      expect(onlineIndicators.length).toBe(1);
    });

    it('does not display online indicator when other user is offline', () => {
      useConversationStore.setState({ conversations: [mockConversations[0]] });
      usePresenceStore.setState({
        onlineUsers: {
          'user-2': { userId: 'user-2', isOnline: false, lastSeen: new Date() },
        },
      });

      render(<ConversationList />);

      const onlineIndicators = document.querySelectorAll('.bg-green-400');
      expect(onlineIndicators.length).toBe(0);
    });

    it('does not display online indicator for group chats', () => {
      useConversationStore.setState({ conversations: [mockConversations[1]] });
      usePresenceStore.setState({
        onlineUsers: {
          'user-3': { userId: 'user-3', isOnline: true, lastSeen: null },
          'user-4': { userId: 'user-4', isOnline: true, lastSeen: null },
        },
      });

      render(<ConversationList />);

      // Should not show online indicator for group chats
      const onlineIndicators = document.querySelectorAll('.bg-green-400');
      expect(onlineIndicators.length).toBe(0);
    });
  });

  describe('Search and filter', () => {
    beforeEach(() => {
      useConversationStore.setState({ conversations: mockConversations });
    });

    it('filters conversations based on search query', () => {
      render(<ConversationList />);

      const searchInput = screen.getByPlaceholderText('Search conversations...');
      fireEvent.change(searchInput, { target: { value: 'John' } });

      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.queryByText('Team Chat')).not.toBeInTheDocument();
      expect(screen.queryByText('Jane Smith')).not.toBeInTheDocument();
    });

    it('search is case-insensitive', () => {
      render(<ConversationList />);

      const searchInput = screen.getByPlaceholderText('Search conversations...');
      fireEvent.change(searchInput, { target: { value: 'team' } });

      expect(screen.getByText('Team Chat')).toBeInTheDocument();
      expect(screen.queryByText('John Doe')).not.toBeInTheDocument();
    });

    it('shows empty state with appropriate message when no results', () => {
      render(<ConversationList />);

      const searchInput = screen.getByPlaceholderText('Search conversations...');
      fireEvent.change(searchInput, { target: { value: 'NonExistent' } });

      expect(screen.getByText('No conversations found')).toBeInTheDocument();
      expect(screen.getByText('Try a different search term')).toBeInTheDocument();
    });

    it('shows all conversations when search is cleared', () => {
      render(<ConversationList />);

      const searchInput = screen.getByPlaceholderText('Search conversations...');

      // Filter
      fireEvent.change(searchInput, { target: { value: 'John' } });
      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.queryByText('Team Chat')).not.toBeInTheDocument();

      // Clear filter
      fireEvent.change(searchInput, { target: { value: '' } });
      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.getByText('Team Chat')).toBeInTheDocument();
      expect(screen.getByText('Jane Smith')).toBeInTheDocument();
    });
  });

  describe('Empty state', () => {
    it('handles empty state when no conversations exist', () => {
      render(<ConversationList />);

      expect(screen.getByText('No conversations yet')).toBeInTheDocument();
      expect(screen.getByText('Start a conversation to get started')).toBeInTheDocument();
    });

    it('handles empty state when search returns no results', () => {
      useConversationStore.setState({ conversations: mockConversations });

      render(<ConversationList />);

      const searchInput = screen.getByPlaceholderText('Search conversations...');
      fireEvent.change(searchInput, { target: { value: 'xyz123' } });

      expect(screen.getByText('No conversations found')).toBeInTheDocument();
      expect(screen.getByText('Try a different search term')).toBeInTheDocument();
    });
  });

  describe('Click to select conversation', () => {
    it('calls setActiveConversation when conversation is clicked', () => {
      useConversationStore.setState({ conversations: mockConversations });

      render(<ConversationList />);

      const conversationButton = screen.getByText('John Doe').closest('button');
      if (conversationButton) {
        fireEvent.click(conversationButton);
      }

      const activeId = useConversationStore.getState().activeConversationId;
      expect(activeId).toBe('conv-1');
    });

    it('calls onConversationSelect callback when provided', () => {
      const onConversationSelect = vi.fn();
      useConversationStore.setState({ conversations: mockConversations });

      render(<ConversationList onConversationSelect={onConversationSelect} />);

      const conversationButton = screen.getByText('Team Chat').closest('button');
      if (conversationButton) {
        fireEvent.click(conversationButton);
      }

      expect(onConversationSelect).toHaveBeenCalledWith('conv-2');
    });

    it('highlights the active conversation', () => {
      useConversationStore.setState({
        conversations: mockConversations,
        activeConversationId: 'conv-1',
      });

      render(<ConversationList />);

      const activeButton = screen.getByText('John Doe').closest('button');
      expect(activeButton).toHaveClass('bg-indigo-50');
    });
  });
});
