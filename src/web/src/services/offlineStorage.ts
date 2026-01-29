import { openDB } from 'idb';
import type { DBSchema, IDBPDatabase } from 'idb';
import type { ConversationResponse, MessageResponse } from '@/types/api';

/**
 * Database schema for IndexedDB
 */
interface OfflineDBSchema extends DBSchema {
  conversations: {
    key: string;
    value: ConversationResponse & { syncedAt?: string };
    indexes: { 'by-lastMessage': string };
  };
  messages: {
    key: string;
    value: MessageResponse & { syncedAt?: string };
    indexes: { 'by-conversation': string; 'by-createdAt': string };
  };
  outgoingQueue: {
    key: number;
    value: QueuedOutgoingMessage;
    indexes: { 'by-conversationId': string; 'by-timestamp': number };
  };
  syncMetadata: {
    key: string;
    value: SyncMetadata;
  };
}

/**
 * Queued outgoing message structure
 */
export interface QueuedOutgoingMessage {
  id?: number;
  conversationId: string;
  messageId: string;
  content: string;
  timestamp: number;
  retries: number;
  status: 'pending' | 'sending' | 'failed';
}

/**
 * Sync metadata for tracking last sync times
 */
export interface SyncMetadata {
  key: string;
  lastSyncAt: string;
  conversationId?: string;
}

/**
 * Options for conflict resolution
 */
export interface ConflictResolutionOptions {
  preferServer: boolean;
}

/**
 * OfflineStorage service for managing IndexedDB offline storage
 *
 * Features:
 * - Store conversations and messages locally
 * - Sync on reconnection
 * - Queue outgoing messages when offline
 * - Resolve conflicts using server timestamps
 */
export class OfflineStorage {
  private db: IDBPDatabase<OfflineDBSchema> | null = null;
  private readonly dbName = 'TralliValli';
  private readonly dbVersion = 1;

  /**
   * Initialize the database and create object stores
   */
  async initialize(): Promise<void> {
    this.db = await openDB<OfflineDBSchema>(this.dbName, this.dbVersion, {
      upgrade(db) {
        // Conversations store
        if (!db.objectStoreNames.contains('conversations')) {
          const conversationStore = db.createObjectStore('conversations', {
            keyPath: 'id',
          });
          conversationStore.createIndex('by-lastMessage', 'lastMessageAt');
        }

        // Messages store
        if (!db.objectStoreNames.contains('messages')) {
          const messageStore = db.createObjectStore('messages', {
            keyPath: 'id',
          });
          messageStore.createIndex('by-conversation', 'conversationId');
          messageStore.createIndex('by-createdAt', 'createdAt');
        }

        // Outgoing message queue
        if (!db.objectStoreNames.contains('outgoingQueue')) {
          const queueStore = db.createObjectStore('outgoingQueue', {
            keyPath: 'id',
            autoIncrement: true,
          });
          queueStore.createIndex('by-conversationId', 'conversationId');
          queueStore.createIndex('by-timestamp', 'timestamp');
        }

        // Sync metadata store
        if (!db.objectStoreNames.contains('syncMetadata')) {
          db.createObjectStore('syncMetadata', {
            keyPath: 'key',
          });
        }
      },
    });
  }

  /**
   * Close the database connection
   */
  async close(): Promise<void> {
    if (this.db) {
      this.db.close();
      this.db = null;
    }
  }

  /**
   * Ensure database is initialized
   */
  private ensureDB(): IDBPDatabase<OfflineDBSchema> {
    if (!this.db) {
      throw new Error('Database not initialized. Call initialize() first.');
    }
    return this.db;
  }

  // ============================================================================
  // Conversation Operations
  // ============================================================================

  /**
   * Store a conversation locally
   */
  async storeConversation(conversation: ConversationResponse): Promise<void> {
    const db = this.ensureDB();
    const conversationWithSync = {
      ...conversation,
      syncedAt: new Date().toISOString(),
    };
    await db.put('conversations', conversationWithSync);
  }

  /**
   * Store multiple conversations locally
   */
  async storeConversations(conversations: ConversationResponse[]): Promise<void> {
    const db = this.ensureDB();
    const tx = db.transaction('conversations', 'readwrite');
    const now = new Date().toISOString();

    await Promise.all([
      ...conversations.map((conv) => tx.store.put({ ...conv, syncedAt: now })),
      tx.done,
    ]);
  }

  /**
   * Get a conversation by ID
   */
  async getConversation(id: string): Promise<ConversationResponse | undefined> {
    const db = this.ensureDB();
    return db.get('conversations', id);
  }

  /**
   * Get all conversations
   */
  async getAllConversations(): Promise<ConversationResponse[]> {
    const db = this.ensureDB();
    return db.getAll('conversations');
  }

  /**
   * Delete a conversation
   */
  async deleteConversation(id: string): Promise<void> {
    const db = this.ensureDB();
    await db.delete('conversations', id);
  }

  // ============================================================================
  // Message Operations
  // ============================================================================

  /**
   * Store a message locally
   */
  async storeMessage(message: MessageResponse): Promise<void> {
    const db = this.ensureDB();
    const messageWithSync = {
      ...message,
      syncedAt: new Date().toISOString(),
    };
    await db.put('messages', messageWithSync);
  }

  /**
   * Store multiple messages locally
   */
  async storeMessages(messages: MessageResponse[]): Promise<void> {
    const db = this.ensureDB();
    const tx = db.transaction('messages', 'readwrite');
    const now = new Date().toISOString();

    await Promise.all([...messages.map((msg) => tx.store.put({ ...msg, syncedAt: now })), tx.done]);
  }

  /**
   * Get messages for a conversation
   */
  async getMessagesByConversation(conversationId: string): Promise<MessageResponse[]> {
    const db = this.ensureDB();
    return db.getAllFromIndex('messages', 'by-conversation', conversationId);
  }

  /**
   * Get a message by ID
   */
  async getMessage(id: string): Promise<MessageResponse | undefined> {
    const db = this.ensureDB();
    return db.get('messages', id);
  }

  /**
   * Delete a message
   */
  async deleteMessage(id: string): Promise<void> {
    const db = this.ensureDB();
    await db.delete('messages', id);
  }

  /**
   * Delete all messages for a conversation
   */
  async deleteMessagesByConversation(conversationId: string): Promise<void> {
    const db = this.ensureDB();
    const messages = await this.getMessagesByConversation(conversationId);
    const tx = db.transaction('messages', 'readwrite');

    await Promise.all([...messages.map((msg) => tx.store.delete(msg.id)), tx.done]);
  }

  // ============================================================================
  // Outgoing Message Queue Operations
  // ============================================================================

  /**
   * Add a message to the outgoing queue
   */
  async queueOutgoingMessage(
    conversationId: string,
    messageId: string,
    content: string
  ): Promise<number> {
    const db = this.ensureDB();
    const queuedMessage: QueuedOutgoingMessage = {
      conversationId,
      messageId,
      content,
      timestamp: Date.now(),
      retries: 0,
      status: 'pending',
    };
    return db.add('outgoingQueue', queuedMessage);
  }

  /**
   * Get all pending outgoing messages
   */
  async getPendingOutgoingMessages(): Promise<QueuedOutgoingMessage[]> {
    const db = this.ensureDB();
    const allMessages = await db.getAll('outgoingQueue');
    return allMessages.filter((msg) => msg.status === 'pending');
  }

  /**
   * Get outgoing messages for a specific conversation
   */
  async getOutgoingMessagesByConversation(
    conversationId: string
  ): Promise<QueuedOutgoingMessage[]> {
    const db = this.ensureDB();
    return db.getAllFromIndex('outgoingQueue', 'by-conversationId', conversationId);
  }

  /**
   * Update the status of a queued message
   */
  async updateQueuedMessageStatus(
    id: number,
    status: 'pending' | 'sending' | 'failed',
    incrementRetry = false
  ): Promise<void> {
    const db = this.ensureDB();
    const message = await db.get('outgoingQueue', id);
    if (message) {
      message.status = status;
      if (incrementRetry) {
        message.retries += 1;
      }
      await db.put('outgoingQueue', message);
    }
  }

  /**
   * Remove a message from the outgoing queue
   */
  async removeFromQueue(id: number): Promise<void> {
    const db = this.ensureDB();
    await db.delete('outgoingQueue', id);
  }

  /**
   * Clear all messages from the outgoing queue
   */
  async clearOutgoingQueue(): Promise<void> {
    const db = this.ensureDB();
    await db.clear('outgoingQueue');
  }

  // ============================================================================
  // Sync Operations
  // ============================================================================

  /**
   * Get the last sync timestamp for a conversation
   */
  async getLastSyncTime(conversationId: string): Promise<string | null> {
    const db = this.ensureDB();
    const key = `conversation-${conversationId}`;
    const metadata = await db.get('syncMetadata', key);
    return metadata?.lastSyncAt || null;
  }

  /**
   * Update the last sync timestamp for a conversation
   */
  async updateLastSyncTime(conversationId: string, timestamp: string): Promise<void> {
    const db = this.ensureDB();
    const key = `conversation-${conversationId}`;
    const metadata: SyncMetadata = {
      key,
      lastSyncAt: timestamp,
      conversationId,
    };
    await db.put('syncMetadata', metadata);
  }

  /**
   * Get the global last sync timestamp
   */
  async getGlobalLastSyncTime(): Promise<string | null> {
    const db = this.ensureDB();
    const metadata = await db.get('syncMetadata', 'global');
    return metadata?.lastSyncAt || null;
  }

  /**
   * Update the global last sync timestamp
   */
  async updateGlobalLastSyncTime(timestamp: string): Promise<void> {
    const db = this.ensureDB();
    const metadata: SyncMetadata = {
      key: 'global',
      lastSyncAt: timestamp,
    };
    await db.put('syncMetadata', metadata);
  }

  /**
   * Sync conversations from server with conflict resolution
   * Returns list of conversation IDs that were updated
   */
  async syncConversations(
    serverConversations: ConversationResponse[],
    options: ConflictResolutionOptions = { preferServer: true }
  ): Promise<string[]> {
    const db = this.ensureDB();
    const updatedIds: string[] = [];

    for (const serverConv of serverConversations) {
      const localConv = await db.get('conversations', serverConv.id);

      if (!localConv) {
        // New conversation from server
        await this.storeConversation(serverConv);
        updatedIds.push(serverConv.id);
      } else {
        // Conflict resolution using server timestamps
        const shouldUpdate = this.shouldUpdateConversation(localConv, serverConv, options);

        if (shouldUpdate) {
          await this.storeConversation(serverConv);
          updatedIds.push(serverConv.id);
        }
      }
    }

    return updatedIds;
  }

  /**
   * Sync messages from server with conflict resolution
   * Returns list of message IDs that were updated
   */
  async syncMessages(
    serverMessages: MessageResponse[],
    options: ConflictResolutionOptions = { preferServer: true }
  ): Promise<string[]> {
    const db = this.ensureDB();
    const updatedIds: string[] = [];

    for (const serverMsg of serverMessages) {
      const localMsg = await db.get('messages', serverMsg.id);

      if (!localMsg) {
        // New message from server
        await this.storeMessage(serverMsg);
        updatedIds.push(serverMsg.id);
      } else {
        // Conflict resolution using server timestamps
        const shouldUpdate = this.shouldUpdateMessage(localMsg, serverMsg, options);

        if (shouldUpdate) {
          await this.storeMessage(serverMsg);
          updatedIds.push(serverMsg.id);
        }
      }
    }

    return updatedIds;
  }

  /**
   * Determine if local conversation should be updated with server version
   * Uses server timestamps for conflict resolution
   */
  private shouldUpdateConversation(
    local: ConversationResponse,
    server: ConversationResponse,
    options: ConflictResolutionOptions
  ): boolean {
    // Always prefer server if option is set
    if (options.preferServer) {
      return true;
    }

    // Compare lastMessageAt timestamps
    const localTime = local.lastMessageAt ? new Date(local.lastMessageAt).getTime() : 0;
    const serverTime = server.lastMessageAt ? new Date(server.lastMessageAt).getTime() : 0;

    // Server version is newer
    if (serverTime > localTime) {
      return true;
    }

    // If timestamps are equal, don't update
    if (serverTime === localTime) {
      return false;
    }

    // Compare createdAt as fallback
    const localCreated = new Date(local.createdAt).getTime();
    const serverCreated = new Date(server.createdAt).getTime();

    return serverCreated > localCreated;
  }

  /**
   * Determine if local message should be updated with server version
   * Uses server timestamps for conflict resolution
   */
  private shouldUpdateMessage(
    local: MessageResponse,
    server: MessageResponse,
    options: ConflictResolutionOptions
  ): boolean {
    // Always prefer server if option is set
    if (options.preferServer) {
      return true;
    }

    // If server message is edited, prefer it
    if (server.editedAt && !local.editedAt) {
      return true;
    }

    // Compare edit timestamps if both are edited
    if (server.editedAt && local.editedAt) {
      const localEditTime = new Date(local.editedAt).getTime();
      const serverEditTime = new Date(server.editedAt).getTime();
      return serverEditTime > localEditTime;
    }

    // Compare creation timestamps
    const localTime = new Date(local.createdAt).getTime();
    const serverTime = new Date(server.createdAt).getTime();

    return serverTime >= localTime;
  }

  // ============================================================================
  // Utility Operations
  // ============================================================================

  /**
   * Clear all data from the database
   */
  async clearAll(): Promise<void> {
    const db = this.ensureDB();
    await Promise.all([
      db.clear('conversations'),
      db.clear('messages'),
      db.clear('outgoingQueue'),
      db.clear('syncMetadata'),
    ]);
  }

  /**
   * Get database statistics
   */
  async getStats(): Promise<{
    conversations: number;
    messages: number;
    queuedMessages: number;
  }> {
    const db = this.ensureDB();
    const [conversations, messages, queuedMessages] = await Promise.all([
      db.count('conversations'),
      db.count('messages'),
      db.count('outgoingQueue'),
    ]);

    return {
      conversations,
      messages,
      queuedMessages,
    };
  }
}
