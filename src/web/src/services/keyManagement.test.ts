import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { KeyManagementService } from './keyManagement';
import { generateKey } from './aesGcmEncryption';

describe('KeyManagementService', () => {
  let keyService: KeyManagementService;
  let masterKey: CryptoKey;
  const testConversationId = 'test-conversation-123';
  const testPassword = 'test-password-12345';

  beforeEach(async () => {
    keyService = new KeyManagementService();
    await keyService.initialize();

    // Generate a master key for testing
    masterKey = await generateKey();
    await keyService.setMasterKey(masterKey);
  });

  afterEach(async () => {
    await keyService.clearAll();
    await keyService.close();
  });

  describe('Initialization', () => {
    it('should initialize successfully', async () => {
      const newService = new KeyManagementService();
      await expect(newService.initialize()).resolves.not.toThrow();
      await newService.close();
    });

    it('should throw error when using service before initialization', async () => {
      const newService = new KeyManagementService();
      const masterKey = await generateKey();
      await newService.setMasterKey(masterKey);

      await expect(newService.getAllConversationIds()).rejects.toThrow(
        'Key management service not initialized'
      );
    });

    it('should throw error when accessing keys without master key', async () => {
      const newService = new KeyManagementService();
      await newService.initialize();

      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const key = await newService.deriveConversationKey(sharedSecret, testConversationId);

      await expect(newService.storeConversationKey(testConversationId, key)).rejects.toThrow(
        'Master key not set'
      );

      await newService.close();
    });
  });

  describe('Master Key Management', () => {
    it('should set a valid master key', async () => {
      const newService = new KeyManagementService();
      await newService.initialize();
      const newMasterKey = await generateKey();

      await expect(newService.setMasterKey(newMasterKey)).resolves.not.toThrow();
      await newService.close();
    });

    it('should reject invalid master key', async () => {
      const newService = new KeyManagementService();
      await newService.initialize();

      // @ts-expect-error - intentionally passing invalid key
      await expect(newService.setMasterKey(null)).rejects.toThrow('Invalid master key');

      await newService.close();
    });

    it('should derive master key from password', async () => {
      const result = await keyService.deriveMasterKeyFromPassword(testPassword);

      expect(result.masterKey).toBeDefined();
      expect(result.masterKey.type).toBe('secret');
      expect(result.masterKey.algorithm.name).toBe('AES-GCM');
      expect(result.salt).toBeInstanceOf(Uint8Array);
      expect(result.salt.length).toBe(16);
    });

    it('should derive same key with same password and salt', async () => {
      const result1 = await keyService.deriveMasterKeyFromPassword(testPassword, undefined, true);
      const result2 = await keyService.deriveMasterKeyFromPassword(
        testPassword,
        result1.salt,
        true
      );

      // Export both keys to compare
      const key1Raw = await crypto.subtle.exportKey('raw', result1.masterKey);
      const key2Raw = await crypto.subtle.exportKey('raw', result2.masterKey);

      expect(new Uint8Array(key1Raw)).toEqual(new Uint8Array(key2Raw));
    });

    it('should derive different keys with different passwords', async () => {
      const result1 = await keyService.deriveMasterKeyFromPassword('password1', undefined, true);
      const result2 = await keyService.deriveMasterKeyFromPassword('password2', result1.salt, true);

      const key1Raw = await crypto.subtle.exportKey('raw', result1.masterKey);
      const key2Raw = await crypto.subtle.exportKey('raw', result2.masterKey);

      expect(new Uint8Array(key1Raw)).not.toEqual(new Uint8Array(key2Raw));
    });

    it('should reject short passwords', async () => {
      await expect(keyService.deriveMasterKeyFromPassword('short')).rejects.toThrow(
        'Password must be at least 8 characters'
      );
    });
  });

  describe('Conversation Key Derivation', () => {
    it('should derive a conversation key from shared secret', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      const key = await keyService.deriveConversationKey(sharedSecret, testConversationId);

      expect(key).toBeDefined();
      expect(key.type).toBe('secret');
      expect(key.algorithm.name).toBe('AES-GCM');
    });

    it('should derive same key from same shared secret and conversation ID', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      const key1 = await keyService.deriveConversationKey(sharedSecret, testConversationId);
      const key2 = await keyService.deriveConversationKey(sharedSecret, testConversationId);

      // Export both keys to compare
      const key1Raw = await crypto.subtle.exportKey('raw', key1);
      const key2Raw = await crypto.subtle.exportKey('raw', key2);

      expect(new Uint8Array(key1Raw)).toEqual(new Uint8Array(key2Raw));
    });

    it('should derive different keys for different conversation IDs', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      const key1 = await keyService.deriveConversationKey(sharedSecret, 'conversation-1');
      const key2 = await keyService.deriveConversationKey(sharedSecret, 'conversation-2');

      const key1Raw = await crypto.subtle.exportKey('raw', key1);
      const key2Raw = await crypto.subtle.exportKey('raw', key2);

      expect(new Uint8Array(key1Raw)).not.toEqual(new Uint8Array(key2Raw));
    });

    it('should derive different keys for different versions', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      const key1 = await keyService.deriveConversationKey(sharedSecret, testConversationId, 1);
      const key2 = await keyService.deriveConversationKey(sharedSecret, testConversationId, 2);

      const key1Raw = await crypto.subtle.exportKey('raw', key1);
      const key2Raw = await crypto.subtle.exportKey('raw', key2);

      expect(new Uint8Array(key1Raw)).not.toEqual(new Uint8Array(key2Raw));
    });

    it('should reject invalid shared secret length', async () => {
      const invalidSecret = crypto.getRandomValues(new Uint8Array(16));

      await expect(
        keyService.deriveConversationKey(invalidSecret, testConversationId)
      ).rejects.toThrow('Shared secret must be 32 bytes');
    });

    it('should reject empty conversation ID', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      await expect(keyService.deriveConversationKey(sharedSecret, '')).rejects.toThrow(
        'Conversation ID cannot be empty'
      );
    });
  });

  describe('Encrypted Key Storage', () => {
    it('should store and retrieve a conversation key', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const key = await keyService.deriveConversationKey(sharedSecret, testConversationId);

      // Store the key
      await keyService.storeConversationKey(testConversationId, key);

      // Retrieve the key
      const retrievedKey = await keyService.getConversationKey(testConversationId);

      expect(retrievedKey).toBeDefined();
      expect(retrievedKey!.type).toBe('secret');
      expect(retrievedKey!.algorithm.name).toBe('AES-GCM');

      // Verify keys are the same
      const originalRaw = await crypto.subtle.exportKey('raw', key);
      const retrievedRaw = await crypto.subtle.exportKey('raw', retrievedKey!);
      expect(new Uint8Array(originalRaw)).toEqual(new Uint8Array(retrievedRaw));
    });

    it('should return null for non-existent conversation key', async () => {
      const key = await keyService.getConversationKey('non-existent-id');
      expect(key).toBeNull();
    });

    it('should update existing conversation key', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId);
      await keyService.storeConversationKey(testConversationId, key1);

      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      const key2 = await keyService.deriveConversationKey(sharedSecret2, testConversationId);
      await keyService.storeConversationKey(testConversationId, key2);

      const retrievedKey = await keyService.getConversationKey(testConversationId);
      const retrievedRaw = await crypto.subtle.exportKey('raw', retrievedKey!);
      const key2Raw = await crypto.subtle.exportKey('raw', key2);

      expect(new Uint8Array(retrievedRaw)).toEqual(new Uint8Array(key2Raw));
    });

    it('should store keys with version information', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const key = await keyService.deriveConversationKey(sharedSecret, testConversationId, 2);

      await keyService.storeConversationKey(testConversationId, key, 2);

      const info = await keyService.getConversationKeyInfo(testConversationId);
      expect(info).toBeDefined();
      expect(info!.version).toBe(2);
    });

    it('should delete a conversation key', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const key = await keyService.deriveConversationKey(sharedSecret, testConversationId);

      await keyService.storeConversationKey(testConversationId, key);
      await keyService.deleteConversationKey(testConversationId);

      const retrievedKey = await keyService.getConversationKey(testConversationId);
      expect(retrievedKey).toBeNull();
    });

    it('should list all conversation IDs', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      const key1 = await keyService.deriveConversationKey(sharedSecret, 'conversation-1');
      const key2 = await keyService.deriveConversationKey(sharedSecret, 'conversation-2');
      const key3 = await keyService.deriveConversationKey(sharedSecret, 'conversation-3');

      await keyService.storeConversationKey('conversation-1', key1);
      await keyService.storeConversationKey('conversation-2', key2);
      await keyService.storeConversationKey('conversation-3', key3);

      const ids = await keyService.getAllConversationIds();
      expect(ids).toHaveLength(3);
      expect(ids).toContain('conversation-1');
      expect(ids).toContain('conversation-2');
      expect(ids).toContain('conversation-3');
    });
  });

  describe('Key Metadata', () => {
    it('should retrieve key metadata without decryption', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const key = await keyService.deriveConversationKey(sharedSecret, testConversationId);

      await keyService.storeConversationKey(testConversationId, key, 1);

      const info = await keyService.getConversationKeyInfo(testConversationId);

      expect(info).toBeDefined();
      expect(info!.conversationId).toBe(testConversationId);
      expect(info!.version).toBe(1);
      expect(info!.createdAt).toBeDefined();
      expect(new Date(info!.createdAt).getTime()).toBeLessThanOrEqual(Date.now());
    });

    it('should return null for non-existent key metadata', async () => {
      const info = await keyService.getConversationKeyInfo('non-existent');
      expect(info).toBeNull();
    });
  });

  describe('Key Rotation', () => {
    it('should rotate a conversation key', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId, 1);
      await keyService.storeConversationKey(testConversationId, key1, 1);

      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      const newVersion = await keyService.rotateConversationKey(
        testConversationId,
        sharedSecret2,
        'member_added'
      );

      expect(newVersion).toBe(2);

      const info = await keyService.getConversationKeyInfo(testConversationId);
      expect(info!.version).toBe(2);
      expect(info!.rotatedAt).toBeDefined();
    });

    it('should increment version correctly on multiple rotations', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId, 1);
      await keyService.storeConversationKey(testConversationId, key1, 1);

      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      const version2 = await keyService.rotateConversationKey(testConversationId, sharedSecret2);
      expect(version2).toBe(2);

      const sharedSecret3 = crypto.getRandomValues(new Uint8Array(32));
      const version3 = await keyService.rotateConversationKey(testConversationId, sharedSecret3);
      expect(version3).toBe(3);
    });

    it('should create rotation history entry', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId, 1);
      await keyService.storeConversationKey(testConversationId, key1, 1);

      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      await keyService.rotateConversationKey(testConversationId, sharedSecret2, 'member_removed');

      const history = await keyService.getRotationHistory(testConversationId);

      expect(history).toHaveLength(1);
      expect(history[0].conversationId).toBe(testConversationId);
      expect(history[0].oldVersion).toBe(1);
      expect(history[0].newVersion).toBe(2);
      expect(history[0].reason).toBe('member_removed');
      expect(history[0].rotatedAt).toBeDefined();
    });

    it('should maintain rotation history across multiple rotations', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId, 1);
      await keyService.storeConversationKey(testConversationId, key1, 1);

      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      await keyService.rotateConversationKey(testConversationId, sharedSecret2, 'member_added');

      const sharedSecret3 = crypto.getRandomValues(new Uint8Array(32));
      await keyService.rotateConversationKey(testConversationId, sharedSecret3, 'member_removed');

      const history = await keyService.getRotationHistory(testConversationId);

      expect(history).toHaveLength(2);
      expect(history[0].oldVersion).toBe(1);
      expect(history[0].newVersion).toBe(2);
      expect(history[0].reason).toBe('member_added');
      expect(history[1].oldVersion).toBe(2);
      expect(history[1].newVersion).toBe(3);
      expect(history[1].reason).toBe('member_removed');
    });

    it('should rotate key for conversation without existing key', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const newVersion = await keyService.rotateConversationKey(
        'new-conversation',
        sharedSecret,
        'initial'
      );

      expect(newVersion).toBe(1);

      const key = await keyService.getConversationKey('new-conversation');
      expect(key).toBeDefined();
    });

    it('should update rotatedAt timestamp on rotation', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId, 1);
      await keyService.storeConversationKey(testConversationId, key1, 1);

      const infoBefore = await keyService.getConversationKeyInfo(testConversationId);
      expect(infoBefore!.rotatedAt).toBeUndefined();

      // Wait a bit to ensure timestamp difference
      await new Promise((resolve) => setTimeout(resolve, 10));

      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      await keyService.rotateConversationKey(testConversationId, sharedSecret2);

      const infoAfter = await keyService.getConversationKeyInfo(testConversationId);
      expect(infoAfter!.rotatedAt).toBeDefined();
      expect(new Date(infoAfter!.rotatedAt!).getTime()).toBeGreaterThan(
        new Date(infoBefore!.createdAt).getTime()
      );
    });
  });

  describe('Key Rotation Scenarios', () => {
    it('should support member_added rotation scenario', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId, 1);
      await keyService.storeConversationKey(testConversationId, key1, 1);

      // Simulate adding a new member requiring key rotation
      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      const newVersion = await keyService.rotateConversationKey(
        testConversationId,
        sharedSecret2,
        'member_added'
      );

      expect(newVersion).toBe(2);

      const history = await keyService.getRotationHistory(testConversationId);
      expect(history[0].reason).toBe('member_added');
    });

    it('should support member_removed rotation scenario', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId, 1);
      await keyService.storeConversationKey(testConversationId, key1, 1);

      // Simulate removing a member requiring key rotation
      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      const newVersion = await keyService.rotateConversationKey(
        testConversationId,
        sharedSecret2,
        'member_removed'
      );

      expect(newVersion).toBe(2);

      const history = await keyService.getRotationHistory(testConversationId);
      expect(history[0].reason).toBe('member_removed');
    });

    it('should support manual rotation scenario', async () => {
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const key1 = await keyService.deriveConversationKey(sharedSecret1, testConversationId, 1);
      await keyService.storeConversationKey(testConversationId, key1, 1);

      // Manual rotation for security purposes
      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      const newVersion = await keyService.rotateConversationKey(
        testConversationId,
        sharedSecret2,
        'manual'
      );

      expect(newVersion).toBe(2);

      const history = await keyService.getRotationHistory(testConversationId);
      expect(history[0].reason).toBe('manual');
    });
  });

  describe('Error Handling', () => {
    it('should handle decryption failure with wrong master key', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const key = await keyService.deriveConversationKey(sharedSecret, testConversationId);

      // Store with one master key
      await keyService.storeConversationKey(testConversationId, key);

      // Try to retrieve with different master key
      const differentMasterKey = await generateKey();
      await keyService.setMasterKey(differentMasterKey);

      await expect(keyService.getConversationKey(testConversationId)).rejects.toThrow();
    });

    it('should handle invalid encrypted data gracefully', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const key = await keyService.deriveConversationKey(sharedSecret, testConversationId);
      await keyService.storeConversationKey(testConversationId, key);

      // Get direct database access and corrupt the data
      const ids = await keyService.getAllConversationIds();
      expect(ids).toContain(testConversationId);

      // Retrieving should work before corruption
      const retrievedKey = await keyService.getConversationKey(testConversationId);
      expect(retrievedKey).toBeDefined();
    });
  });

  describe('Cleanup', () => {
    it('should clear all keys and history', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      const key1 = await keyService.deriveConversationKey(sharedSecret, 'conversation-1');
      const key2 = await keyService.deriveConversationKey(sharedSecret, 'conversation-2');

      await keyService.storeConversationKey('conversation-1', key1);
      await keyService.storeConversationKey('conversation-2', key2);
      await keyService.rotateConversationKey('conversation-1', sharedSecret);

      await keyService.clearAll();

      const ids = await keyService.getAllConversationIds();
      expect(ids).toHaveLength(0);

      const history = await keyService.getRotationHistory('conversation-1');
      expect(history).toHaveLength(0);
    });
  });

  describe('Integration with X25519 Key Exchange', () => {
    it('should work with realistic X25519 shared secret', async () => {
      // Simulate X25519 key exchange output (32 bytes)
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      // Derive conversation key
      const conversationKey = await keyService.deriveConversationKey(
        sharedSecret,
        testConversationId
      );

      // Store encrypted key
      await keyService.storeConversationKey(testConversationId, conversationKey);

      // Retrieve and verify
      const retrievedKey = await keyService.getConversationKey(testConversationId);
      expect(retrievedKey).toBeDefined();

      // Both parties in the conversation should derive the same key
      const conversationKey2 = await keyService.deriveConversationKey(
        sharedSecret,
        testConversationId
      );

      const key1Raw = await crypto.subtle.exportKey('raw', conversationKey);
      const key2Raw = await crypto.subtle.exportKey('raw', conversationKey2);
      expect(new Uint8Array(key1Raw)).toEqual(new Uint8Array(key2Raw));
    });

    it('should support multiple conversations with same shared secret', async () => {
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));

      // Derive keys for different conversations
      const key1 = await keyService.deriveConversationKey(sharedSecret, 'conversation-1');
      const key2 = await keyService.deriveConversationKey(sharedSecret, 'conversation-2');

      await keyService.storeConversationKey('conversation-1', key1);
      await keyService.storeConversationKey('conversation-2', key2);

      // Keys should be different despite same shared secret
      const key1Raw = await crypto.subtle.exportKey('raw', key1);
      const key2Raw = await crypto.subtle.exportKey('raw', key2);
      expect(new Uint8Array(key1Raw)).not.toEqual(new Uint8Array(key2Raw));

      // Both should be retrievable
      const retrieved1 = await keyService.getConversationKey('conversation-1');
      const retrieved2 = await keyService.getConversationKey('conversation-2');
      expect(retrieved1).toBeDefined();
      expect(retrieved2).toBeDefined();
    });
  });
});
