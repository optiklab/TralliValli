/**
 * Key Management Service for TralliValli
 *
 * Provides per-conversation key management with encryption at rest.
 *
 * Features:
 * - Per-conversation symmetric key derivation from shared secrets
 * - Encrypted storage in IndexedDB using user's master key
 * - Key rotation when group members change
 * - Secure key derivation using HKDF
 */

import { openDB } from 'idb';
import type { DBSchema, IDBPDatabase } from 'idb';
import { importKey as importAESKey, exportKey as exportAESKey } from './aesGcmEncryption';

/**
 * Database schema for conversation keys
 */
interface KeyManagementDBSchema extends DBSchema {
  conversationKeys: {
    key: string; // conversationId
    value: StoredConversationKey;
  };
  keyRotationHistory: {
    key: number;
    value: KeyRotationRecord;
    indexes: { 'by-conversation': string };
  };
}

/**
 * Stored conversation key structure
 */
export interface StoredConversationKey {
  conversationId: string;
  encryptedKey: string; // Base64 encoded encrypted key
  iv: string; // Base64 encoded IV for encryption
  tag: string; // Base64 encoded authentication tag
  version: number; // Key version for rotation
  createdAt: string;
  rotatedAt?: string;
}

/**
 * Key rotation record
 */
export interface KeyRotationRecord {
  id?: number;
  conversationId: string;
  oldVersion: number;
  newVersion: number;
  rotatedAt: string;
  reason: string; // e.g., "member_added", "member_removed", "manual"
}

/**
 * Conversation key metadata
 */
export interface ConversationKeyInfo {
  conversationId: string;
  version: number;
  createdAt: string;
  rotatedAt?: string;
}

/**
 * Key Management Service
 *
 * Handles per-conversation encryption keys with secure storage and rotation.
 */
export class KeyManagementService {
  private db: IDBPDatabase<KeyManagementDBSchema> | null = null;
  private readonly dbName = 'TralliValli-KeyManagement';
  private readonly dbVersion = 1;
  private masterKey: CryptoKey | null = null;

  /**
   * Initialize the service and database
   */
  async initialize(): Promise<void> {
    this.db = await openDB<KeyManagementDBSchema>(this.dbName, this.dbVersion, {
      upgrade(db) {
        // Conversation keys store
        if (!db.objectStoreNames.contains('conversationKeys')) {
          db.createObjectStore('conversationKeys', {
            keyPath: 'conversationId',
          });
        }

        // Key rotation history store
        if (!db.objectStoreNames.contains('keyRotationHistory')) {
          const rotationStore = db.createObjectStore('keyRotationHistory', {
            keyPath: 'id',
            autoIncrement: true,
          });
          rotationStore.createIndex('by-conversation', 'conversationId');
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
    this.masterKey = null;
  }

  /**
   * Ensure database is initialized
   */
  private ensureDB(): IDBPDatabase<KeyManagementDBSchema> {
    if (!this.db) {
      throw new Error('Key management service not initialized. Call initialize() first.');
    }
    return this.db;
  }

  /**
   * Ensure master key is set
   */
  private ensureMasterKey(): CryptoKey {
    if (!this.masterKey) {
      throw new Error('Master key not set. Call setMasterKey() first.');
    }
    return this.masterKey;
  }

  /**
   * Set the master key for encrypting conversation keys
   * @param masterKey - CryptoKey to use for encrypting conversation keys at rest
   *
   * Note: The master key should be derived from the user's password or
   * another secure source. It must be an AES-256-GCM key.
   */
  async setMasterKey(masterKey: CryptoKey): Promise<void> {
    if (!masterKey || masterKey.type !== 'secret' || masterKey.algorithm.name !== 'AES-GCM') {
      throw new Error('Invalid master key: must be an AES-GCM secret key');
    }
    this.masterKey = masterKey;
  }

  /**
   * Derive a master key from a password
   * @param password - User's password
   * @param salt - Optional salt (will be generated if not provided)
   * @param extractable - Whether the key should be extractable (default: false)
   * @returns Master key and salt used for derivation
   */
  async deriveMasterKeyFromPassword(
    password: string,
    salt?: Uint8Array,
    extractable: boolean = false
  ): Promise<{ masterKey: CryptoKey; salt: Uint8Array }> {
    if (!password || password.length < 8) {
      throw new Error('Password must be at least 8 characters long');
    }

    // Generate salt if not provided
    const keySalt = salt || crypto.getRandomValues(new Uint8Array(16));

    // Encode password
    const encoder = new TextEncoder();
    const passwordData = encoder.encode(password);

    // Import password as key material
    const keyMaterial = await crypto.subtle.importKey(
      'raw',
      passwordData,
      { name: 'PBKDF2' },
      false,
      ['deriveBits', 'deriveKey']
    );

    // Derive AES-GCM key using PBKDF2
    const masterKey = await crypto.subtle.deriveKey(
      {
        name: 'PBKDF2',
        salt: keySalt,
        iterations: 100000,
        hash: 'SHA-256',
      },
      keyMaterial,
      { name: 'AES-GCM', length: 256 },
      extractable,
      ['encrypt', 'decrypt']
    );

    return { masterKey, salt: keySalt };
  }

  /**
   * Derive a conversation key from a shared secret
   * @param sharedSecret - Shared secret derived from X25519 key exchange (32 bytes)
   * @param conversationId - Unique conversation identifier
   * @param version - Key version (for rotation)
   * @returns Derived conversation key as CryptoKey
   *
   * Uses HKDF-SHA256 to derive a conversation-specific key from the shared secret.
   * The conversation ID and version are used as context to ensure unique keys.
   */
  async deriveConversationKey(
    sharedSecret: Uint8Array,
    conversationId: string,
    version: number = 1
  ): Promise<CryptoKey> {
    if (sharedSecret.length !== 32) {
      throw new Error('Shared secret must be 32 bytes');
    }

    if (!conversationId || conversationId.trim().length === 0) {
      throw new Error('Conversation ID cannot be empty');
    }

    // Import shared secret as key material
    const keyMaterial = await crypto.subtle.importKey(
      'raw',
      sharedSecret,
      { name: 'HKDF' },
      false,
      ['deriveBits', 'deriveKey']
    );

    // Create info string combining conversation ID and version
    const encoder = new TextEncoder();
    const info = encoder.encode(`conversation:${conversationId}:v${version}`);

    // Use a fixed salt for deterministic derivation
    // This ensures both parties derive the same key
    const salt = encoder.encode('TralliValli-ConversationKey');

    // Derive conversation key using HKDF
    const conversationKey = await crypto.subtle.deriveKey(
      {
        name: 'HKDF',
        hash: 'SHA-256',
        salt: salt,
        info: info,
      },
      keyMaterial,
      { name: 'AES-GCM', length: 256 },
      true, // extractable so we can store it
      ['encrypt', 'decrypt']
    );

    return conversationKey;
  }

  /**
   * Encrypt a conversation key with the master key
   * @param conversationKey - CryptoKey to encrypt
   * @param masterKey - Master key for encryption
   * @returns Encrypted key data
   */
  private async encryptConversationKey(
    conversationKey: CryptoKey,
    masterKey: CryptoKey
  ): Promise<{ encryptedKey: string; iv: string; tag: string }> {
    // Export conversation key to raw bytes
    const rawKey = await exportAESKey(conversationKey);

    // Generate IV
    const iv = crypto.getRandomValues(new Uint8Array(12));

    // Encrypt the key
    const encryptedBuffer = await crypto.subtle.encrypt(
      {
        name: 'AES-GCM',
        iv: iv,
        tagLength: 128,
      },
      masterKey,
      rawKey
    );

    // Split ciphertext and tag
    const encryptedArray = new Uint8Array(encryptedBuffer);
    const ciphertext = encryptedArray.slice(0, -16);
    const tag = encryptedArray.slice(-16);

    // Convert to base64
    return {
      encryptedKey: this.arrayBufferToBase64(ciphertext),
      iv: this.arrayBufferToBase64(iv),
      tag: this.arrayBufferToBase64(tag),
    };
  }

  /**
   * Decrypt a conversation key with the master key
   * @param encryptedData - Encrypted key data
   * @param masterKey - Master key for decryption
   * @returns Decrypted conversation key
   */
  private async decryptConversationKey(
    encryptedData: { encryptedKey: string; iv: string; tag: string },
    masterKey: CryptoKey
  ): Promise<CryptoKey> {
    // Convert from base64
    const ciphertext = this.base64ToUint8Array(encryptedData.encryptedKey);
    const iv = this.base64ToUint8Array(encryptedData.iv);
    const tag = this.base64ToUint8Array(encryptedData.tag);

    // Combine ciphertext and tag
    const combined = new Uint8Array(ciphertext.length + tag.length);
    combined.set(ciphertext, 0);
    combined.set(tag, ciphertext.length);

    // Decrypt
    const decryptedBuffer = await crypto.subtle.decrypt(
      {
        name: 'AES-GCM',
        iv: iv,
        tagLength: 128,
      },
      masterKey,
      combined
    );

    // Import as CryptoKey
    return await importAESKey(new Uint8Array(decryptedBuffer), true);
  }

  /**
   * Store a conversation key encrypted with the master key
   * @param conversationId - Unique conversation identifier
   * @param conversationKey - Conversation key to store
   * @param version - Key version (default: 1)
   */
  async storeConversationKey(
    conversationId: string,
    conversationKey: CryptoKey,
    version: number = 1
  ): Promise<void> {
    const db = this.ensureDB();
    const masterKey = this.ensureMasterKey();

    // Encrypt the conversation key
    const encrypted = await this.encryptConversationKey(conversationKey, masterKey);

    const storedKey: StoredConversationKey = {
      conversationId,
      encryptedKey: encrypted.encryptedKey,
      iv: encrypted.iv,
      tag: encrypted.tag,
      version,
      createdAt: new Date().toISOString(),
    };

    await db.put('conversationKeys', storedKey);
  }

  /**
   * Retrieve a conversation key
   * @param conversationId - Unique conversation identifier
   * @returns Decrypted conversation key or null if not found
   */
  async getConversationKey(conversationId: string): Promise<CryptoKey | null> {
    const db = this.ensureDB();
    const masterKey = this.ensureMasterKey();

    const stored = await db.get('conversationKeys', conversationId);
    if (!stored) {
      return null;
    }

    return await this.decryptConversationKey(
      {
        encryptedKey: stored.encryptedKey,
        iv: stored.iv,
        tag: stored.tag,
      },
      masterKey
    );
  }

  /**
   * Get conversation key metadata without decrypting
   * @param conversationId - Unique conversation identifier
   * @returns Key metadata or null if not found
   */
  async getConversationKeyInfo(conversationId: string): Promise<ConversationKeyInfo | null> {
    const db = this.ensureDB();

    const stored = await db.get('conversationKeys', conversationId);
    if (!stored) {
      return null;
    }

    return {
      conversationId: stored.conversationId,
      version: stored.version,
      createdAt: stored.createdAt,
      rotatedAt: stored.rotatedAt,
    };
  }

  /**
   * Rotate a conversation key
   * @param conversationId - Unique conversation identifier
   * @param newSharedSecret - New shared secret for the rotated key
   * @param reason - Reason for rotation (e.g., "member_added", "member_removed")
   * @returns New key version
   *
   * Key rotation is necessary when:
   * - A member is added to a group conversation
   * - A member is removed from a group conversation
   * - Security breach is suspected
   * - Periodic rotation for security best practices
   */
  async rotateConversationKey(
    conversationId: string,
    newSharedSecret: Uint8Array,
    reason: string = 'manual'
  ): Promise<number> {
    const db = this.ensureDB();

    // Get current key info
    const currentInfo = await this.getConversationKeyInfo(conversationId);
    const oldVersion = currentInfo?.version || 0;
    const newVersion = oldVersion + 1;

    // Derive new conversation key
    const newKey = await this.deriveConversationKey(newSharedSecret, conversationId, newVersion);

    // Store new key
    await this.storeConversationKey(conversationId, newKey, newVersion);

    // Update rotation timestamp
    const stored = await db.get('conversationKeys', conversationId);
    if (stored) {
      stored.rotatedAt = new Date().toISOString();
      await db.put('conversationKeys', stored);
    }

    // Record rotation in history
    const rotationRecord: KeyRotationRecord = {
      conversationId,
      oldVersion,
      newVersion,
      rotatedAt: new Date().toISOString(),
      reason,
    };
    await db.add('keyRotationHistory', rotationRecord);

    return newVersion;
  }

  /**
   * Get key rotation history for a conversation
   * @param conversationId - Unique conversation identifier
   * @returns Array of rotation records
   */
  async getRotationHistory(conversationId: string): Promise<KeyRotationRecord[]> {
    const db = this.ensureDB();
    return await db.getAllFromIndex('keyRotationHistory', 'by-conversation', conversationId);
  }

  /**
   * Delete a conversation key
   * @param conversationId - Unique conversation identifier
   */
  async deleteConversationKey(conversationId: string): Promise<void> {
    const db = this.ensureDB();
    await db.delete('conversationKeys', conversationId);
  }

  /**
   * Get all stored conversation IDs
   * @returns Array of conversation IDs
   */
  async getAllConversationIds(): Promise<string[]> {
    const db = this.ensureDB();
    const keys = await db.getAllKeys('conversationKeys');
    return keys;
  }

  /**
   * Clear all stored keys (use with caution)
   */
  async clearAll(): Promise<void> {
    const db = this.ensureDB();
    await Promise.all([
      db.clear('conversationKeys'),
      db.clear('keyRotationHistory'),
    ]);
  }

  /**
   * Convert ArrayBuffer to Base64 string
   */
  private arrayBufferToBase64(buffer: ArrayBuffer | Uint8Array): string {
    const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
    const chars: string[] = [];
    for (let i = 0; i < bytes.length; i++) {
      chars.push(String.fromCharCode(bytes[i]));
    }
    return btoa(chars.join(''));
  }

  /**
   * Convert Base64 string to Uint8Array
   */
  private base64ToUint8Array(base64: string): Uint8Array {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
  }
}
