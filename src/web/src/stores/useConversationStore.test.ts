import { describe, it, expect, beforeEach } from 'vitest';
import { useConversationStore } from './useConversationStore';
import type { Conversation, Message } from './useConversationStore';

describe('useConversationStore', () => {
  beforeEach(() => {
    // Reset the store before each test
    useConversationStore.getState().reset();
  });

  describe('initial state', () => {
    it('should have empty initial state', () => {
      const state = useConversationStore.getState();

      expect(state.conversations).toEqual([]);
      expect(state.activeConversationId).toBeNull();
      expect(state.messages).toEqual({});
      expect(state.loading).toBe(false);
      expect(state.error).toBeNull();
    });
  });

  describe('loadConversations', () => {
    it('should load conversations', () => {
      const conversations: Conversation[] = [
        {
          id: 'conv-1',
          type: 'direct',
          name: 'Test Conversation',
          isGroup: false,
          participants: [],
          recentMessages: [],
          createdAt: '2026-01-01T00:00:00Z',
          metadata: {},
        },
        {
          id: 'conv-2',
          type: 'group',
          name: 'Group Chat',
          isGroup: true,
          participants: [],
          recentMessages: [],
          createdAt: '2026-01-02T00:00:00Z',
          metadata: {},
        },
      ];

      useConversationStore.getState().loadConversations(conversations);

      const state = useConversationStore.getState();
      expect(state.conversations).toEqual(conversations);
      expect(state.error).toBeNull();
    });
  });

  describe('loadMessages', () => {
    it('should load messages for a conversation', () => {
      const conversationId = 'conv-1';
      const messages: Message[] = [
        {
          id: 'msg-1',
          conversationId,
          senderId: 'user-1',
          type: 'text',
          content: 'Hello',
          encryptedContent: '',
          createdAt: '2026-01-01T10:00:00Z',
          readBy: [],
          isDeleted: false,
          attachments: [],
        },
        {
          id: 'msg-2',
          conversationId,
          senderId: 'user-2',
          type: 'text',
          content: 'Hi there',
          encryptedContent: '',
          createdAt: '2026-01-01T10:01:00Z',
          readBy: [],
          isDeleted: false,
          attachments: [],
        },
      ];

      useConversationStore.getState().loadMessages(conversationId, messages);

      const state = useConversationStore.getState();
      expect(state.messages[conversationId]).toEqual(messages);
      expect(state.error).toBeNull();
    });
  });

  describe('addMessage', () => {
    it('should add a new message to a conversation', () => {
      const conversationId = 'conv-1';
      const message: Message = {
        id: 'msg-1',
        conversationId,
        senderId: 'user-1',
        type: 'text',
        content: 'New message',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      useConversationStore.getState().addMessage(conversationId, message);

      const state = useConversationStore.getState();
      expect(state.messages[conversationId]).toHaveLength(1);
      expect(state.messages[conversationId][0]).toEqual(message);
    });

    it('should not add duplicate messages', () => {
      const conversationId = 'conv-1';
      const message: Message = {
        id: 'msg-1',
        conversationId,
        senderId: 'user-1',
        type: 'text',
        content: 'Message',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      useConversationStore.getState().addMessage(conversationId, message);
      useConversationStore.getState().addMessage(conversationId, message);

      const state = useConversationStore.getState();
      expect(state.messages[conversationId]).toHaveLength(1);
    });

    it('should update conversation lastMessageAt when adding message', () => {
      const conversationId = 'conv-1';
      const conversation: Conversation = {
        id: conversationId,
        type: 'direct',
        name: 'Test',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      };

      useConversationStore.getState().loadConversations([conversation]);

      const message: Message = {
        id: 'msg-1',
        conversationId,
        senderId: 'user-1',
        type: 'text',
        content: 'Message',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      useConversationStore.getState().addMessage(conversationId, message);

      const state = useConversationStore.getState();
      const updatedConv = state.conversations.find((c) => c.id === conversationId);
      expect(updatedConv?.lastMessageAt).toBe('2026-01-01T10:00:00Z');
    });
  });

  describe('setActiveConversation', () => {
    it('should set active conversation', () => {
      const conversationId = 'conv-1';

      useConversationStore.getState().setActiveConversation(conversationId);

      const state = useConversationStore.getState();
      expect(state.activeConversationId).toBe(conversationId);
    });

    it('should clear active conversation', () => {
      useConversationStore.getState().setActiveConversation('conv-1');
      useConversationStore.getState().setActiveConversation(null);

      const state = useConversationStore.getState();
      expect(state.activeConversationId).toBeNull();
    });
  });

  describe('updateConversation', () => {
    it('should update existing conversation', () => {
      const conversation: Conversation = {
        id: 'conv-1',
        type: 'direct',
        name: 'Original Name',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      };

      useConversationStore.getState().loadConversations([conversation]);

      const updatedConversation: Conversation = {
        ...conversation,
        name: 'Updated Name',
      };

      useConversationStore.getState().updateConversation(updatedConversation);

      const state = useConversationStore.getState();
      expect(state.conversations[0].name).toBe('Updated Name');
    });

    it('should add new conversation if it does not exist', () => {
      const conversation: Conversation = {
        id: 'conv-1',
        type: 'direct',
        name: 'New Conversation',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      };

      useConversationStore.getState().updateConversation(conversation);

      const state = useConversationStore.getState();
      expect(state.conversations).toHaveLength(1);
      expect(state.conversations[0]).toEqual(conversation);
    });
  });

  describe('removeConversation', () => {
    it('should remove conversation and its messages', () => {
      const conversationId = 'conv-1';
      const conversation: Conversation = {
        id: conversationId,
        type: 'direct',
        name: 'Test',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      };

      const messages: Message[] = [
        {
          id: 'msg-1',
          conversationId,
          senderId: 'user-1',
          type: 'text',
          content: 'Message',
          encryptedContent: '',
          createdAt: '2026-01-01T10:00:00Z',
          readBy: [],
          isDeleted: false,
          attachments: [],
        },
      ];

      useConversationStore.getState().loadConversations([conversation]);
      useConversationStore.getState().loadMessages(conversationId, messages);

      useConversationStore.getState().removeConversation(conversationId);

      const state = useConversationStore.getState();
      expect(state.conversations).toHaveLength(0);
      expect(state.messages[conversationId]).toBeUndefined();
    });

    it('should clear active conversation if removed', () => {
      const conversationId = 'conv-1';
      const conversation: Conversation = {
        id: conversationId,
        type: 'direct',
        name: 'Test',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      };

      useConversationStore.getState().loadConversations([conversation]);
      useConversationStore.getState().setActiveConversation(conversationId);
      useConversationStore.getState().removeConversation(conversationId);

      const state = useConversationStore.getState();
      expect(state.activeConversationId).toBeNull();
    });
  });

  describe('updateMessage', () => {
    it('should update a message', () => {
      const conversationId = 'conv-1';
      const message: Message = {
        id: 'msg-1',
        conversationId,
        senderId: 'user-1',
        type: 'text',
        content: 'Original content',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      useConversationStore.getState().loadMessages(conversationId, [message]);

      useConversationStore.getState().updateMessage(conversationId, 'msg-1', {
        content: 'Updated content',
        editedAt: '2026-01-01T11:00:00Z',
      });

      const state = useConversationStore.getState();
      const updatedMessage = state.messages[conversationId][0];
      expect(updatedMessage.content).toBe('Updated content');
      expect(updatedMessage.editedAt).toBe('2026-01-01T11:00:00Z');
    });
  });

  describe('deleteMessage', () => {
    it('should mark message as deleted', () => {
      const conversationId = 'conv-1';
      const message: Message = {
        id: 'msg-1',
        conversationId,
        senderId: 'user-1',
        type: 'text',
        content: 'Message to delete',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      useConversationStore.getState().loadMessages(conversationId, [message]);
      useConversationStore.getState().deleteMessage(conversationId, 'msg-1');

      const state = useConversationStore.getState();
      expect(state.messages[conversationId][0].isDeleted).toBe(true);
    });
  });

  describe('markMessageAsRead', () => {
    it('should mark message as read by user', () => {
      const conversationId = 'conv-1';
      const message: Message = {
        id: 'msg-1',
        conversationId,
        senderId: 'user-1',
        type: 'text',
        content: 'Message',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      useConversationStore.getState().loadMessages(conversationId, [message]);

      const readAt = new Date('2026-01-01T10:05:00Z');
      useConversationStore.getState().markMessageAsRead(conversationId, 'msg-1', 'user-2', readAt);

      const state = useConversationStore.getState();
      const updatedMessage = state.messages[conversationId][0];
      expect(updatedMessage.readBy).toHaveLength(1);
      expect(updatedMessage.readBy[0].userId).toBe('user-2');
      expect(updatedMessage.readBy[0].readAt).toBe(readAt.toISOString());
    });

    it('should not duplicate read status for same user', () => {
      const conversationId = 'conv-1';
      const message: Message = {
        id: 'msg-1',
        conversationId,
        senderId: 'user-1',
        type: 'text',
        content: 'Message',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      useConversationStore.getState().loadMessages(conversationId, [message]);

      const readAt1 = new Date('2026-01-01T10:05:00Z');
      const readAt2 = new Date('2026-01-01T10:10:00Z');

      useConversationStore.getState().markMessageAsRead(conversationId, 'msg-1', 'user-2', readAt1);
      useConversationStore.getState().markMessageAsRead(conversationId, 'msg-1', 'user-2', readAt2);

      const state = useConversationStore.getState();
      expect(state.messages[conversationId][0].readBy).toHaveLength(1);
    });
  });

  describe('loading and error states', () => {
    it('should set loading state', () => {
      useConversationStore.getState().setLoading(true);
      expect(useConversationStore.getState().loading).toBe(true);

      useConversationStore.getState().setLoading(false);
      expect(useConversationStore.getState().loading).toBe(false);
    });

    it('should set error state', () => {
      const error = 'Something went wrong';
      useConversationStore.getState().setError(error);
      expect(useConversationStore.getState().error).toBe(error);

      useConversationStore.getState().setError(null);
      expect(useConversationStore.getState().error).toBeNull();
    });
  });

  describe('clearMessages', () => {
    it('should clear messages for a conversation', () => {
      const conversationId = 'conv-1';
      const messages: Message[] = [
        {
          id: 'msg-1',
          conversationId,
          senderId: 'user-1',
          type: 'text',
          content: 'Message',
          encryptedContent: '',
          createdAt: '2026-01-01T10:00:00Z',
          readBy: [],
          isDeleted: false,
          attachments: [],
        },
      ];

      useConversationStore.getState().loadMessages(conversationId, messages);
      expect(useConversationStore.getState().messages[conversationId]).toBeDefined();

      useConversationStore.getState().clearMessages(conversationId);
      expect(useConversationStore.getState().messages[conversationId]).toBeUndefined();
    });
  });

  describe('reset', () => {
    it('should reset store to initial state', () => {
      // Set up some state
      const conversation: Conversation = {
        id: 'conv-1',
        type: 'direct',
        name: 'Test',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      };

      useConversationStore.getState().loadConversations([conversation]);
      useConversationStore.getState().setActiveConversation('conv-1');
      useConversationStore.getState().setLoading(true);
      useConversationStore.getState().setError('Error');

      // Reset
      useConversationStore.getState().reset();

      const state = useConversationStore.getState();
      expect(state.conversations).toEqual([]);
      expect(state.activeConversationId).toBeNull();
      expect(state.messages).toEqual({});
      expect(state.loading).toBe(false);
      expect(state.error).toBeNull();
    });
  });
});
