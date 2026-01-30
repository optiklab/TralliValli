import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { KeyBackupService } from './keyBackup';
import { KeyManagementService } from './keyManagement';
import { CryptoKeyExchange } from './cryptoKeyExchange';

describe('KeyBackupService', () => {
  let backupService: KeyBackupService;
  let keyManagementService: KeyManagementService;
  let cryptoKeyExchange: CryptoKeyExchange;
  const testPassword = 'backup-password-12345';
  const testMasterPassword = 'master-password-12345';

  beforeEach(async () => {
    // Initialize services
    keyManagementService = new KeyManagementService();
    await keyManagementService.initialize();

    cryptoKeyExchange = new CryptoKeyExchange();
    await cryptoKeyExchange.initialize();

    backupService = new KeyBackupService(keyManagementService, cryptoKeyExchange);

    // Set up master key for key management service
    const { masterKey } = await keyManagementService.deriveMasterKeyFromPassword(
      testMasterPassword
    );
    await keyManagementService.setMasterKey(masterKey);
  });

  afterEach(async () => {
    // Clean up
    await keyManagementService.clearAll();
    await cryptoKeyExchange.clearAll();
    await keyManagementService.close();
    await cryptoKeyExchange.close();
  });

  describe('Backup Creation', () => {
    it('should create an encrypted backup', async () => {
      const backup = await backupService.createBackup(testPassword);

      expect(backup).toBeDefined();
      expect(backup.version).toBe(1);
      expect(backup.encryptedData).toBeDefined();
      expect(backup.iv).toBeDefined();
      expect(backup.salt).toBeDefined();
      expect(backup.createdAt).toBeDefined();

      // Verify base64 encoding
      expect(backup.encryptedData.length).toBeGreaterThan(0);
      expect(backup.iv.length).toBeGreaterThan(0);
      expect(backup.salt.length).toBeGreaterThan(0);
    });

    it('should reject password shorter than 8 characters', async () => {
      await expect(backupService.createBackup('short')).rejects.toThrow(
        'Password must be at least 8 characters long'
      );
    });

    it('should create different encrypted data with different passwords', async () => {
      const backup1 = await backupService.createBackup('password-one-12345');
      const backup2 = await backupService.createBackup('password-two-12345');

      expect(backup1.encryptedData).not.toBe(backup2.encryptedData);
      expect(backup1.iv).not.toBe(backup2.iv);
      expect(backup1.salt).not.toBe(backup2.salt);
    });

    it('should backup existing key pairs', async () => {
      // Create a test key pair
      const keyPair = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('test-key-1', keyPair, testMasterPassword);

      const backup = await backupService.createBackup(testPassword);

      expect(backup.encryptedData).toBeDefined();
      expect(backup.encryptedData.length).toBeGreaterThan(0);
    });

    it('should backup existing conversation keys', async () => {
      // Create a test conversation key
      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const conversationKey = await keyManagementService.deriveConversationKey(
        sharedSecret,
        'test-conversation-1'
      );
      await keyManagementService.storeConversationKey('test-conversation-1', conversationKey);

      const backup = await backupService.createBackup(testPassword);

      expect(backup.encryptedData).toBeDefined();
      expect(backup.encryptedData.length).toBeGreaterThan(0);
    });

    it('should backup multiple keys', async () => {
      // Create multiple key pairs
      const keyPair1 = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('test-key-1', keyPair1, testMasterPassword);

      const keyPair2 = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('test-key-2', keyPair2, testMasterPassword);

      // Create multiple conversation keys
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const conversationKey1 = await keyManagementService.deriveConversationKey(
        sharedSecret1,
        'test-conversation-1'
      );
      await keyManagementService.storeConversationKey('test-conversation-1', conversationKey1);

      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      const conversationKey2 = await keyManagementService.deriveConversationKey(
        sharedSecret2,
        'test-conversation-2'
      );
      await keyManagementService.storeConversationKey('test-conversation-2', conversationKey2);

      const backup = await backupService.createBackup(testPassword);

      expect(backup.encryptedData).toBeDefined();
      expect(backup.encryptedData.length).toBeGreaterThan(0);
    });
  });

  describe('Backup Restoration', () => {
    it('should restore keys from backup', async () => {
      // Create keys
      const keyPair = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('test-key-1', keyPair, testMasterPassword);

      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const conversationKey = await keyManagementService.deriveConversationKey(
        sharedSecret,
        'test-conversation-1'
      );
      await keyManagementService.storeConversationKey('test-conversation-1', conversationKey);

      // Create backup
      const backup = await backupService.createBackup(testPassword);

      // Clear all keys
      await cryptoKeyExchange.clearAll();
      await keyManagementService.clearAll();

      // Restore from backup
      await backupService.restoreBackup(backup, testPassword);

      // Verify keys are restored in storage (without decrypting them yet)
      const restoredKeyPairIds = await cryptoKeyExchange.getAllKeyPairIds();
      expect(restoredKeyPairIds).toContain('test-key-1');

      const restoredConversationIds = await keyManagementService.getAllConversationIds();
      expect(restoredConversationIds).toContain('test-conversation-1');
    });

    it('should reject password shorter than 8 characters for restoration', async () => {
      const backup = await backupService.createBackup(testPassword);

      await expect(backupService.restoreBackup(backup, 'short')).rejects.toThrow(
        'Password must be at least 8 characters long'
      );
    });

    it('should fail with incorrect password', async () => {
      const backup = await backupService.createBackup(testPassword);

      await expect(backupService.restoreBackup(backup, 'wrong-password-12345')).rejects.toThrow(
        'Failed to decrypt backup'
      );
    });

    it('should fail with unsupported backup version', async () => {
      const backup = await backupService.createBackup(testPassword);
      backup.version = 999; // Invalid version

      await expect(backupService.restoreBackup(backup, testPassword)).rejects.toThrow(
        'Unsupported backup version'
      );
    });

    it('should fail with corrupted backup data', async () => {
      const backup = await backupService.createBackup(testPassword);
      backup.encryptedData = 'Y29ycnVwdGVkLWRhdGE='; // Valid base64 but invalid ciphertext

      await expect(backupService.restoreBackup(backup, testPassword)).rejects.toThrow(
        'Failed to decrypt backup'
      );
    });
  });

  describe('End-to-End Backup and Recovery', () => {
    it('should successfully backup and restore multiple keys', async () => {
      // Create multiple key pairs
      const keyPair1 = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('key-1', keyPair1, testMasterPassword);

      const keyPair2 = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('key-2', keyPair2, testMasterPassword);

      // Create multiple conversation keys
      const sharedSecret1 = crypto.getRandomValues(new Uint8Array(32));
      const conversationKey1 = await keyManagementService.deriveConversationKey(
        sharedSecret1,
        'conversation-1'
      );
      await keyManagementService.storeConversationKey('conversation-1', conversationKey1);

      const sharedSecret2 = crypto.getRandomValues(new Uint8Array(32));
      const conversationKey2 = await keyManagementService.deriveConversationKey(
        sharedSecret2,
        'conversation-2'
      );
      await keyManagementService.storeConversationKey('conversation-2', conversationKey2);

      const sharedSecret3 = crypto.getRandomValues(new Uint8Array(32));
      const conversationKey3 = await keyManagementService.deriveConversationKey(
        sharedSecret3,
        'conversation-3'
      );
      await keyManagementService.storeConversationKey('conversation-3', conversationKey3);

      // Create backup
      const backup = await backupService.createBackup(testPassword);

      // Verify backup metadata
      expect(backup.version).toBe(1);
      expect(new Date(backup.createdAt).getTime()).toBeLessThanOrEqual(Date.now());

      // Clear all keys (simulate new device)
      await cryptoKeyExchange.clearAll();
      await keyManagementService.clearAll();

      // Verify keys are gone
      expect(await cryptoKeyExchange.getAllKeyPairIds()).toHaveLength(0);
      expect(await keyManagementService.getAllConversationIds()).toHaveLength(0);

      // Restore from backup
      await backupService.restoreBackup(backup, testPassword);

      // Verify all keys are restored in storage
      const restoredKeyPairIds = await cryptoKeyExchange.getAllKeyPairIds();
      expect(restoredKeyPairIds).toHaveLength(2);
      expect(restoredKeyPairIds).toContain('key-1');
      expect(restoredKeyPairIds).toContain('key-2');

      const restoredConversationIds = await keyManagementService.getAllConversationIds();
      expect(restoredConversationIds).toHaveLength(3);
      expect(restoredConversationIds).toContain('conversation-1');
      expect(restoredConversationIds).toContain('conversation-2');
      expect(restoredConversationIds).toContain('conversation-3');
    });

    it('should handle empty backup (no keys)', async () => {
      // Create backup with no keys
      const backup = await backupService.createBackup(testPassword);

      // Restore backup
      await backupService.restoreBackup(backup, testPassword);

      // Verify no keys are present
      expect(await cryptoKeyExchange.getAllKeyPairIds()).toHaveLength(0);
      expect(await keyManagementService.getAllConversationIds()).toHaveLength(0);
    });
  });

  describe('Security', () => {
    it('should use unique salt for each backup', async () => {
      const backup1 = await backupService.createBackup(testPassword);
      const backup2 = await backupService.createBackup(testPassword);

      expect(backup1.salt).not.toBe(backup2.salt);
    });

    it('should use unique IV for each backup', async () => {
      const backup1 = await backupService.createBackup(testPassword);
      const backup2 = await backupService.createBackup(testPassword);

      expect(backup1.iv).not.toBe(backup2.iv);
    });

    it('should produce different ciphertext with same data and different passwords', async () => {
      // Create some keys
      const keyPair = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('test-key', keyPair, testMasterPassword);

      const backup1 = await backupService.createBackup('password-one-12345');
      const backup2 = await backupService.createBackup('password-two-12345');

      expect(backup1.encryptedData).not.toBe(backup2.encryptedData);
    });
  });
});
