import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { OfflineStorage } from './offlineStorage';
import type { ConversationResponse, MessageResponse } from '@/types/api';
import type { QueuedOutgoingMessage } from './offlineStorage';

describe('OfflineStorage', () => {
  let storage: OfflineStorage;

  beforeEach(async () => {
    storage = new OfflineStorage();
    await storage.initialize();
  });

  afterEach(async () => {
    await storage.clearAll();
    await storage.close();
  });

  describe('Database Initialization', () => {
    it('should initialize the database successfully', async () => {
      const newStorage = new OfflineStorage();
      await newStorage.initialize();
      const stats = await newStorage.getStats();

      expect(stats).toEqual({
        conversations: 0,
        messages: 0,
        queuedMessages: 0,
      });

      await newStorage.close();
    });

    it('should throw error when accessing database before initialization', async () => {
      const newStorage = new OfflineStorage();
      await expect(newStorage.getStats()).rejects.toThrow(
        'Database not initialized. Call initialize() first.'
      );
    });
  });

  describe('Conversation Operations', () => {
    const mockConversation: ConversationResponse = {
      id: 'conv-1',
      type: 'direct',
      name: 'Test Conversation',
      isGroup: false,
      participants: [],
      recentMessages: [],
      createdAt: '2026-01-01T00:00:00Z',
      metadata: {},
    };

    it('should store a conversation', async () => {
      await storage.storeConversation(mockConversation);

      const retrieved = await storage.getConversation('conv-1');
      expect(retrieved).toBeDefined();
      expect(retrieved?.id).toBe('conv-1');
      expect(retrieved?.name).toBe('Test Conversation');
    });

    it('should store multiple conversations', async () => {
      const conversations: ConversationResponse[] = [
        mockConversation,
        {
          ...mockConversation,
          id: 'conv-2',
          name: 'Second Conversation',
        },
        {
          ...mockConversation,
          id: 'conv-3',
          name: 'Third Conversation',
        },
      ];

      await storage.storeConversations(conversations);

      const allConversations = await storage.getAllConversations();
      expect(allConversations).toHaveLength(3);
      expect(allConversations.map((c) => c.id)).toEqual(['conv-1', 'conv-2', 'conv-3']);
    });

    it('should retrieve all conversations', async () => {
      await storage.storeConversation(mockConversation);
      await storage.storeConversation({ ...mockConversation, id: 'conv-2' });

      const conversations = await storage.getAllConversations();
      expect(conversations).toHaveLength(2);
    });

    it('should delete a conversation', async () => {
      await storage.storeConversation(mockConversation);
      await storage.deleteConversation('conv-1');

      const retrieved = await storage.getConversation('conv-1');
      expect(retrieved).toBeUndefined();
    });

    it('should update existing conversation when storing with same id', async () => {
      await storage.storeConversation(mockConversation);
      await storage.storeConversation({
        ...mockConversation,
        name: 'Updated Name',
      });

      const retrieved = await storage.getConversation('conv-1');
      expect(retrieved?.name).toBe('Updated Name');
    });
  });

  describe('Message Operations', () => {
    const mockMessage: MessageResponse = {
      id: 'msg-1',
      conversationId: 'conv-1',
      senderId: 'user-1',
      type: 'text',
      content: 'Test message',
      encryptedContent: '',
      createdAt: '2026-01-01T10:00:00Z',
      readBy: [],
      isDeleted: false,
      attachments: [],
    };

    it('should store a message', async () => {
      await storage.storeMessage(mockMessage);

      const retrieved = await storage.getMessage('msg-1');
      expect(retrieved).toBeDefined();
      expect(retrieved?.id).toBe('msg-1');
      expect(retrieved?.content).toBe('Test message');
    });

    it('should store multiple messages', async () => {
      const messages: MessageResponse[] = [
        mockMessage,
        { ...mockMessage, id: 'msg-2', content: 'Second message' },
        { ...mockMessage, id: 'msg-3', content: 'Third message' },
      ];

      await storage.storeMessages(messages);

      const conversationMessages = await storage.getMessagesByConversation('conv-1');
      expect(conversationMessages).toHaveLength(3);
    });

    it('should retrieve messages by conversation', async () => {
      await storage.storeMessage(mockMessage);
      await storage.storeMessage({
        ...mockMessage,
        id: 'msg-2',
        conversationId: 'conv-1',
      });
      await storage.storeMessage({
        ...mockMessage,
        id: 'msg-3',
        conversationId: 'conv-2',
      });

      const conv1Messages = await storage.getMessagesByConversation('conv-1');
      expect(conv1Messages).toHaveLength(2);
      expect(conv1Messages.map((m) => m.id)).toEqual(['msg-1', 'msg-2']);
    });

    it('should delete a message', async () => {
      await storage.storeMessage(mockMessage);
      await storage.deleteMessage('msg-1');

      const retrieved = await storage.getMessage('msg-1');
      expect(retrieved).toBeUndefined();
    });

    it('should delete all messages for a conversation', async () => {
      await storage.storeMessage(mockMessage);
      await storage.storeMessage({ ...mockMessage, id: 'msg-2' });
      await storage.storeMessage({
        ...mockMessage,
        id: 'msg-3',
        conversationId: 'conv-2',
      });

      await storage.deleteMessagesByConversation('conv-1');

      const conv1Messages = await storage.getMessagesByConversation('conv-1');
      const conv2Messages = await storage.getMessagesByConversation('conv-2');

      expect(conv1Messages).toHaveLength(0);
      expect(conv2Messages).toHaveLength(1);
    });

    it('should update existing message when storing with same id', async () => {
      await storage.storeMessage(mockMessage);
      await storage.storeMessage({
        ...mockMessage,
        content: 'Updated content',
        editedAt: '2026-01-01T11:00:00Z',
      });

      const retrieved = await storage.getMessage('msg-1');
      expect(retrieved?.content).toBe('Updated content');
      expect(retrieved?.editedAt).toBe('2026-01-01T11:00:00Z');
    });
  });

  describe('Outgoing Message Queue', () => {
    it('should queue an outgoing message', async () => {
      const queueId = await storage.queueOutgoingMessage('conv-1', 'msg-1', 'Hello');

      expect(queueId).toBeGreaterThan(0);

      const pending = await storage.getPendingOutgoingMessages();
      expect(pending).toHaveLength(1);
      expect(pending[0].conversationId).toBe('conv-1');
      expect(pending[0].messageId).toBe('msg-1');
      expect(pending[0].content).toBe('Hello');
      expect(pending[0].status).toBe('pending');
    });

    it('should retrieve pending outgoing messages', async () => {
      await storage.queueOutgoingMessage('conv-1', 'msg-1', 'Message 1');
      await storage.queueOutgoingMessage('conv-1', 'msg-2', 'Message 2');

      const id = await storage.queueOutgoingMessage('conv-1', 'msg-3', 'Message 3');
      await storage.updateQueuedMessageStatus(id, 'sending');

      const pending = await storage.getPendingOutgoingMessages();
      expect(pending).toHaveLength(2);
    });

    it('should retrieve outgoing messages by conversation', async () => {
      await storage.queueOutgoingMessage('conv-1', 'msg-1', 'Message 1');
      await storage.queueOutgoingMessage('conv-1', 'msg-2', 'Message 2');
      await storage.queueOutgoingMessage('conv-2', 'msg-3', 'Message 3');

      const conv1Messages = await storage.getOutgoingMessagesByConversation('conv-1');
      expect(conv1Messages).toHaveLength(2);
      expect(conv1Messages.map((m) => m.messageId)).toEqual(['msg-1', 'msg-2']);
    });

    it('should update queued message status', async () => {
      const queueId = await storage.queueOutgoingMessage('conv-1', 'msg-1', 'Hello');

      await storage.updateQueuedMessageStatus(queueId, 'sending');

      const pending = await storage.getPendingOutgoingMessages();
      expect(pending).toHaveLength(0);

      const allMessages = await storage.getOutgoingMessagesByConversation('conv-1');
      expect(allMessages[0].status).toBe('sending');
    });

    it('should increment retry count when updating status', async () => {
      const queueId = await storage.queueOutgoingMessage('conv-1', 'msg-1', 'Hello');

      await storage.updateQueuedMessageStatus(queueId, 'failed', true);

      const allMessages = await storage.getOutgoingMessagesByConversation('conv-1');
      expect(allMessages[0].retries).toBe(1);
      expect(allMessages[0].status).toBe('failed');
    });

    it('should remove a message from the queue', async () => {
      const queueId = await storage.queueOutgoingMessage('conv-1', 'msg-1', 'Hello');

      await storage.removeFromQueue(queueId);

      const pending = await storage.getPendingOutgoingMessages();
      expect(pending).toHaveLength(0);
    });

    it('should clear all messages from the queue', async () => {
      await storage.queueOutgoingMessage('conv-1', 'msg-1', 'Message 1');
      await storage.queueOutgoingMessage('conv-1', 'msg-2', 'Message 2');
      await storage.queueOutgoingMessage('conv-2', 'msg-3', 'Message 3');

      await storage.clearOutgoingQueue();

      const pending = await storage.getPendingOutgoingMessages();
      expect(pending).toHaveLength(0);
    });
  });

  describe('Sync Operations', () => {
    it('should store and retrieve last sync time for conversation', async () => {
      const timestamp = '2026-01-01T12:00:00Z';
      await storage.updateLastSyncTime('conv-1', timestamp);

      const retrieved = await storage.getLastSyncTime('conv-1');
      expect(retrieved).toBe(timestamp);
    });

    it('should return null for conversation with no sync time', async () => {
      const retrieved = await storage.getLastSyncTime('conv-unknown');
      expect(retrieved).toBeNull();
    });

    it('should store and retrieve global last sync time', async () => {
      const timestamp = '2026-01-01T12:00:00Z';
      await storage.updateGlobalLastSyncTime(timestamp);

      const retrieved = await storage.getGlobalLastSyncTime();
      expect(retrieved).toBe(timestamp);
    });

    it('should return null for global sync time when not set', async () => {
      const retrieved = await storage.getGlobalLastSyncTime();
      expect(retrieved).toBeNull();
    });

    it('should sync new conversations from server', async () => {
      const serverConversations: ConversationResponse[] = [
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
      ];

      const updatedIds = await storage.syncConversations(serverConversations);

      expect(updatedIds).toEqual(['conv-1']);

      const retrieved = await storage.getConversation('conv-1');
      expect(retrieved).toBeDefined();
      expect(retrieved?.name).toBe('Test Conversation');
    });

    it('should update existing conversations with server version when preferServer is true', async () => {
      const localConv: ConversationResponse = {
        id: 'conv-1',
        type: 'direct',
        name: 'Local Name',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      };

      await storage.storeConversation(localConv);

      const serverConv: ConversationResponse = {
        ...localConv,
        name: 'Server Name',
        lastMessageAt: '2026-01-01T12:00:00Z',
      };

      const updatedIds = await storage.syncConversations([serverConv], {
        preferServer: true,
      });

      expect(updatedIds).toEqual(['conv-1']);

      const retrieved = await storage.getConversation('conv-1');
      expect(retrieved?.name).toBe('Server Name');
    });

    it('should resolve conversation conflicts using lastMessageAt timestamp', async () => {
      const localConv: ConversationResponse = {
        id: 'conv-1',
        type: 'direct',
        name: 'Local Name',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        lastMessageAt: '2026-01-01T10:00:00Z',
        metadata: {},
      };

      await storage.storeConversation(localConv);

      const serverConv: ConversationResponse = {
        ...localConv,
        name: 'Server Name',
        lastMessageAt: '2026-01-01T12:00:00Z',
      };

      const updatedIds = await storage.syncConversations([serverConv], {
        preferServer: false,
      });

      expect(updatedIds).toEqual(['conv-1']);

      const retrieved = await storage.getConversation('conv-1');
      expect(retrieved?.name).toBe('Server Name');
    });

    it('should not update conversation when local is newer and preferServer is false', async () => {
      const localConv: ConversationResponse = {
        id: 'conv-1',
        type: 'direct',
        name: 'Local Name',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        lastMessageAt: '2026-01-01T14:00:00Z',
        metadata: {},
      };

      await storage.storeConversation(localConv);

      const serverConv: ConversationResponse = {
        ...localConv,
        name: 'Server Name',
        lastMessageAt: '2026-01-01T12:00:00Z',
      };

      const updatedIds = await storage.syncConversations([serverConv], {
        preferServer: false,
      });

      expect(updatedIds).toEqual([]);

      const retrieved = await storage.getConversation('conv-1');
      expect(retrieved?.name).toBe('Local Name');
    });

    it('should sync new messages from server', async () => {
      const serverMessages: MessageResponse[] = [
        {
          id: 'msg-1',
          conversationId: 'conv-1',
          senderId: 'user-1',
          type: 'text',
          content: 'Server message',
          encryptedContent: '',
          createdAt: '2026-01-01T10:00:00Z',
          readBy: [],
          isDeleted: false,
          attachments: [],
        },
      ];

      const updatedIds = await storage.syncMessages(serverMessages);

      expect(updatedIds).toEqual(['msg-1']);

      const retrieved = await storage.getMessage('msg-1');
      expect(retrieved).toBeDefined();
      expect(retrieved?.content).toBe('Server message');
    });

    it('should update existing messages with server version when preferServer is true', async () => {
      const localMsg: MessageResponse = {
        id: 'msg-1',
        conversationId: 'conv-1',
        senderId: 'user-1',
        type: 'text',
        content: 'Local content',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      await storage.storeMessage(localMsg);

      const serverMsg: MessageResponse = {
        ...localMsg,
        content: 'Server content',
      };

      const updatedIds = await storage.syncMessages([serverMsg], { preferServer: true });

      expect(updatedIds).toEqual(['msg-1']);

      const retrieved = await storage.getMessage('msg-1');
      expect(retrieved?.content).toBe('Server content');
    });

    it('should resolve message conflicts using editedAt timestamp', async () => {
      const localMsg: MessageResponse = {
        id: 'msg-1',
        conversationId: 'conv-1',
        senderId: 'user-1',
        type: 'text',
        content: 'Local content',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
        editedAt: '2026-01-01T11:00:00Z',
      };

      await storage.storeMessage(localMsg);

      const serverMsg: MessageResponse = {
        ...localMsg,
        content: 'Server content',
        editedAt: '2026-01-01T12:00:00Z',
      };

      const updatedIds = await storage.syncMessages([serverMsg], { preferServer: false });

      expect(updatedIds).toEqual(['msg-1']);

      const retrieved = await storage.getMessage('msg-1');
      expect(retrieved?.content).toBe('Server content');
    });

    it('should prefer edited server message over non-edited local message', async () => {
      const localMsg: MessageResponse = {
        id: 'msg-1',
        conversationId: 'conv-1',
        senderId: 'user-1',
        type: 'text',
        content: 'Local content',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      };

      await storage.storeMessage(localMsg);

      const serverMsg: MessageResponse = {
        ...localMsg,
        content: 'Server edited content',
        editedAt: '2026-01-01T11:00:00Z',
      };

      const updatedIds = await storage.syncMessages([serverMsg], { preferServer: false });

      expect(updatedIds).toEqual(['msg-1']);

      const retrieved = await storage.getMessage('msg-1');
      expect(retrieved?.content).toBe('Server edited content');
    });
  });

  describe('Utility Operations', () => {
    it('should clear all data from database', async () => {
      await storage.storeConversation({
        id: 'conv-1',
        type: 'direct',
        name: 'Test',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      });

      await storage.storeMessage({
        id: 'msg-1',
        conversationId: 'conv-1',
        senderId: 'user-1',
        type: 'text',
        content: 'Test',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      });

      await storage.queueOutgoingMessage('conv-1', 'msg-2', 'Queued message');

      await storage.clearAll();

      const stats = await storage.getStats();
      expect(stats).toEqual({
        conversations: 0,
        messages: 0,
        queuedMessages: 0,
      });
    });

    it('should return database statistics', async () => {
      await storage.storeConversation({
        id: 'conv-1',
        type: 'direct',
        name: 'Test',
        isGroup: false,
        participants: [],
        recentMessages: [],
        createdAt: '2026-01-01T00:00:00Z',
        metadata: {},
      });

      await storage.storeMessage({
        id: 'msg-1',
        conversationId: 'conv-1',
        senderId: 'user-1',
        type: 'text',
        content: 'Test',
        encryptedContent: '',
        createdAt: '2026-01-01T10:00:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      });

      await storage.storeMessage({
        id: 'msg-2',
        conversationId: 'conv-1',
        senderId: 'user-1',
        type: 'text',
        content: 'Test 2',
        encryptedContent: '',
        createdAt: '2026-01-01T10:01:00Z',
        readBy: [],
        isDeleted: false,
        attachments: [],
      });

      await storage.queueOutgoingMessage('conv-1', 'msg-3', 'Queued');

      const stats = await storage.getStats();
      expect(stats).toEqual({
        conversations: 1,
        messages: 2,
        queuedMessages: 1,
      });
    });
  });
});
