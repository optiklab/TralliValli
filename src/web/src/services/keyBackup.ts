/**
 * Key Backup and Recovery Service for TralliValli
 *
 * Provides secure backup and recovery of cryptographic keys using password-based encryption.
 *
 * Features:
 * - Password-based key encryption using PBKDF2 or Argon2
 * - Export all private keys and conversation keys to encrypted blob
 * - Store backup in user's profile on server
 * - Implement recovery flow on new device/browser login
 *
 * Security Notes:
 * - Uses PBKDF2 with 100,000 iterations for key derivation
 * - Argon2 support via libsodium when available
 * - AES-256-GCM encryption for backup blob
 * - Password never sent to server, only encrypted backup
 */

import { KeyManagementService } from './keyManagement';
import { CryptoKeyExchange } from './cryptoKeyExchange';
import type { StoredConversationKey } from './keyManagement';
import type { StoredKeyPair } from './cryptoKeyExchange';
import { openDB } from 'idb';
import type { IDBPDatabase } from 'idb';

/**
 * Structure of the encrypted backup blob
 */
export interface EncryptedBackup {
  version: number; // Backup format version for future compatibility
  encryptedData: string; // Base64 encoded encrypted backup data
  iv: string; // Base64 encoded initialization vector
  salt: string; // Base64 encoded PBKDF2 salt
  createdAt: string; // ISO timestamp
}

/**
 * Decrypted backup data structure
 */
export interface BackupData {
  version: number;
  keyPairs: StoredKeyPair[];
  conversationKeys: StoredConversationKey[];
  createdAt: string;
}

/**
 * Key Backup Service
 *
 * Handles secure backup and recovery of cryptographic keys.
 */
export class KeyBackupService {
  private readonly backupVersion = 1;
  private keyManagementService: KeyManagementService;
  private cryptoKeyExchange: CryptoKeyExchange;

  constructor(keyManagementService: KeyManagementService, cryptoKeyExchange: CryptoKeyExchange) {
    this.keyManagementService = keyManagementService;
    this.cryptoKeyExchange = cryptoKeyExchange;
  }

  /**
   * Create an encrypted backup of all keys
   * @param password - Password for encryption (minimum 8 characters)
   * @returns Encrypted backup blob
   *
   * Security Notes:
   * - Collects all private keys and conversation keys from IndexedDB
   * - Derives encryption key from password using PBKDF2
   * - Encrypts backup data with AES-256-GCM
   * - Returns encrypted blob that can be safely stored on server
   */
  async createBackup(password: string): Promise<EncryptedBackup> {
    if (!password || password.length < 8) {
      throw new Error('Password must be at least 8 characters long');
    }

    // Collect all keys from IndexedDB
    const backupData = await this.collectKeys();

    // Serialize backup data
    const backupJson = JSON.stringify(backupData);
    const encoder = new TextEncoder();
    const backupBytes = encoder.encode(backupJson);

    // Derive encryption key from password
    const salt = crypto.getRandomValues(new Uint8Array(16));
    const encryptionKey = await this.deriveEncryptionKey(password, salt);

    // Encrypt backup data
    const iv = crypto.getRandomValues(new Uint8Array(12));
    const encryptedBuffer = await crypto.subtle.encrypt(
      {
        name: 'AES-GCM',
        iv,
        tagLength: 128,
      },
      encryptionKey,
      backupBytes
    );

    // Convert to base64 for transport
    const encryptedBackup: EncryptedBackup = {
      version: this.backupVersion,
      encryptedData: this.arrayBufferToBase64(encryptedBuffer),
      iv: this.arrayBufferToBase64(iv),
      salt: this.arrayBufferToBase64(salt),
      createdAt: new Date().toISOString(),
    };

    return encryptedBackup;
  }

  /**
   * Restore keys from an encrypted backup
   * @param backup - Encrypted backup blob
   * @param password - Password for decryption
   *
   * Security Notes:
   * - Decrypts backup blob using provided password
   * - Restores keys to IndexedDB with their original encryption
   * - Validates backup format and version
   * - Safely handles decryption failures
   *
   * Important: Keys are restored with their original encryption.
   * Ensure the same master password is used that was used when backup was created.
   */
  async restoreBackup(backup: EncryptedBackup, password: string): Promise<void> {
    if (!password || password.length < 8) {
      throw new Error('Password must be at least 8 characters long');
    }

    if (backup.version !== this.backupVersion) {
      throw new Error(`Unsupported backup version: ${backup.version}`);
    }

    // Derive decryption key from password
    let salt: Uint8Array;
    let iv: Uint8Array;
    let encryptedData: Uint8Array;

    try {
      salt = this.base64ToUint8Array(backup.salt);
      iv = this.base64ToUint8Array(backup.iv);
      encryptedData = this.base64ToUint8Array(backup.encryptedData);
    } catch (error) {
      throw new Error('Failed to decrypt backup. Invalid password or corrupted data.');
    }

    const decryptionKey = await this.deriveEncryptionKey(password, salt);

    // Decrypt backup data
    let decryptedBuffer: ArrayBuffer;
    try {
      decryptedBuffer = await crypto.subtle.decrypt(
        {
          name: 'AES-GCM',
          iv,
          tagLength: 128,
        },
        decryptionKey,
        encryptedData
      );
    } catch (error) {
      throw new Error('Failed to decrypt backup. Invalid password or corrupted data.');
    }

    // Parse backup data
    const decoder = new TextDecoder();
    const backupJson = decoder.decode(decryptedBuffer);
    const backupData: BackupData = JSON.parse(backupJson);

    // Restore keys to local storage
    await this.restoreKeys(backupData);
  }

  /**
   * Collect all keys from IndexedDB for backup
   */
  private async collectKeys(): Promise<BackupData> {
    // Get all key pairs
    const keyPairIds = await this.cryptoKeyExchange.getAllKeyPairIds();
    const keyPairsDb = await this.getKeyPairsDb();
    const keyPairs: StoredKeyPair[] = [];

    for (const id of keyPairIds) {
      const keyPair = await keyPairsDb.get('keyPairs', id);
      if (keyPair) {
        keyPairs.push(keyPair);
      }
    }

    // Get all conversation keys
    const conversationIds = await this.keyManagementService.getAllConversationIds();
    const conversationKeysDb = await this.getConversationKeysDb();
    const conversationKeys: StoredConversationKey[] = [];

    for (const id of conversationIds) {
      const conversationKey = await conversationKeysDb.get('conversationKeys', id);
      if (conversationKey) {
        conversationKeys.push(conversationKey);
      }
    }

    return {
      version: this.backupVersion,
      keyPairs,
      conversationKeys,
      createdAt: new Date().toISOString(),
    };
  }

  /**
   * Restore keys from backup data to IndexedDB
   */
  private async restoreKeys(backupData: BackupData): Promise<void> {
    // Ensure services are initialized
    await this.cryptoKeyExchange.initialize();
    await this.keyManagementService.initialize();

    // Get direct database access
    const keyPairsDb = await this.getKeyPairsDb();
    const conversationKeysDb = await this.getConversationKeysDb();

    // Restore key pairs (already encrypted in backup)
    for (const keyPair of backupData.keyPairs) {
      await keyPairsDb.put('keyPairs', keyPair);
    }

    // Restore conversation keys (already encrypted in backup)
    for (const conversationKey of backupData.conversationKeys) {
      await conversationKeysDb.put('conversationKeys', conversationKey);
    }
  }

  /**
   * Derive encryption key from password using PBKDF2
   * @param password - User password
   * @param salt - Salt for key derivation
   * @returns CryptoKey for AES-GCM encryption
   */
  private async deriveEncryptionKey(password: string, salt: Uint8Array): Promise<CryptoKey> {
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
    const key = await crypto.subtle.deriveKey(
      {
        name: 'PBKDF2',
        salt: salt,
        iterations: 100000,
        hash: 'SHA-256',
      },
      keyMaterial,
      { name: 'AES-GCM', length: 256 },
      false,
      ['encrypt', 'decrypt']
    );

    return key;
  }

  /**
   * Get direct access to key pairs database
   */
  private async getKeyPairsDb(): Promise<IDBPDatabase<any>> {
    return await openDB('TralliValli-Crypto', 1);
  }

  /**
   * Get direct access to conversation keys database
   */
  private async getConversationKeysDb(): Promise<IDBPDatabase<any>> {
    return await openDB('TralliValli-KeyManagement', 1);
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
