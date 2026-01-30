import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { KeyBackupIntegrationService } from './keyBackupIntegration';
import { KeyManagementService } from './keyManagement';
import { CryptoKeyExchange } from './cryptoKeyExchange';
import type { KeyBackupApiClient } from './keyBackupApi';

describe('KeyBackupIntegrationService', () => {
  let integrationService: KeyBackupIntegrationService;
  let keyManagementService: KeyManagementService;
  let cryptoKeyExchange: CryptoKeyExchange;
  let apiClient: KeyBackupApiClient;
  const testPassword = 'test-password-12345';
  const testMasterPassword = 'master-password-12345';

  beforeEach(async () => {
    // Initialize services
    keyManagementService = new KeyManagementService();
    await keyManagementService.initialize();

    cryptoKeyExchange = new CryptoKeyExchange();
    await cryptoKeyExchange.initialize();

    // Set up master key
    const { masterKey } = await keyManagementService.deriveMasterKeyFromPassword(
      testMasterPassword
    );
    await keyManagementService.setMasterKey(masterKey);

    // Create mock API client
    apiClient = {
      storeKeyBackup: vi.fn(),
      getKeyBackup: vi.fn(),
      checkBackupExists: vi.fn(),
      deleteKeyBackup: vi.fn(),
      setAccessToken: vi.fn(),
      clearAccessToken: vi.fn(),
    } as unknown as KeyBackupApiClient;

    // Create integration service
    integrationService = new KeyBackupIntegrationService(
      keyManagementService,
      cryptoKeyExchange,
      apiClient
    );
  });

  afterEach(async () => {
    await keyManagementService.clearAll();
    await cryptoKeyExchange.clearAll();
    await keyManagementService.close();
    await cryptoKeyExchange.close();
    vi.clearAllMocks();
  });

  describe('Backup Creation and Upload', () => {
    it('should create and upload backup successfully', async () => {
      // Mock API response
      const mockBackupId = 'backup-123';
      vi.mocked(apiClient.storeKeyBackup).mockResolvedValueOnce({
        backupId: mockBackupId,
        createdAt: new Date().toISOString(),
        message: 'Success',
      });

      // Create some test keys
      const keyPair = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('test-key', keyPair, testMasterPassword);

      // Create and upload backup
      const backupId = await integrationService.createAndUploadBackup(testPassword);

      expect(backupId).toBe(mockBackupId);
      expect(apiClient.storeKeyBackup).toHaveBeenCalledOnce();

      // Verify the backup data structure
      const callArgs = vi.mocked(apiClient.storeKeyBackup).mock.calls[0][0];
      expect(callArgs).toHaveProperty('version');
      expect(callArgs).toHaveProperty('encryptedData');
      expect(callArgs).toHaveProperty('iv');
      expect(callArgs).toHaveProperty('salt');
    });

    it('should handle upload errors', async () => {
      vi.mocked(apiClient.storeKeyBackup).mockRejectedValueOnce(new Error('Network error'));

      await expect(integrationService.createAndUploadBackup(testPassword)).rejects.toThrow(
        'Backup failed: Network error'
      );
    });
  });

  describe('Backup Download and Restore', () => {
    it('should download and restore backup successfully', async () => {
      // Create and store keys first
      const keyPair = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('test-key', keyPair, testMasterPassword);

      const sharedSecret = crypto.getRandomValues(new Uint8Array(32));
      const conversationKey = await keyManagementService.deriveConversationKey(
        sharedSecret,
        'test-conversation'
      );
      await keyManagementService.storeConversationKey('test-conversation', conversationKey);

      // Create a local backup service to generate test data
      const { KeyBackupService } = await import('./keyBackup');
      const localBackupService = new KeyBackupService(keyManagementService, cryptoKeyExchange);
      const encryptedBackup = await localBackupService.createBackup(testPassword);

      // Mock API response with the real backup
      vi.mocked(apiClient.getKeyBackup).mockResolvedValueOnce(encryptedBackup);

      // Clear local storage
      await cryptoKeyExchange.clearAll();
      await keyManagementService.clearAll();

      // Download and restore
      await integrationService.downloadAndRestoreBackup(testPassword);

      // Verify keys are restored
      const keyIds = await cryptoKeyExchange.getAllKeyPairIds();
      expect(keyIds).toContain('test-key');

      const conversationIds = await keyManagementService.getAllConversationIds();
      expect(conversationIds).toContain('test-conversation');
    });

    it('should handle download errors', async () => {
      vi.mocked(apiClient.getKeyBackup).mockRejectedValueOnce(new Error('Not found'));

      await expect(integrationService.downloadAndRestoreBackup(testPassword)).rejects.toThrow(
        'Restore failed: Not found'
      );
    });

    it('should handle decryption errors with wrong password', async () => {
      // Create a local backup service to generate test data
      const { KeyBackupService } = await import('./keyBackup');
      const localBackupService = new KeyBackupService(keyManagementService, cryptoKeyExchange);
      const encryptedBackup = await localBackupService.createBackup(testPassword);

      // Mock API response
      vi.mocked(apiClient.getKeyBackup).mockResolvedValueOnce(encryptedBackup);

      // Try to restore with wrong password
      await expect(
        integrationService.downloadAndRestoreBackup('wrong-password-12345')
      ).rejects.toThrow('Restore failed');
    });
  });

  describe('Backup Existence Check', () => {
    it('should check if backup exists', async () => {
      vi.mocked(apiClient.checkBackupExists).mockResolvedValueOnce({
        exists: true,
        lastUpdatedAt: new Date().toISOString(),
      });

      const info = await integrationService.checkBackupExists();

      expect(info.exists).toBe(true);
      expect(info.lastUpdatedAt).toBeDefined();
      expect(apiClient.checkBackupExists).toHaveBeenCalledOnce();
    });

    it('should handle when backup does not exist', async () => {
      vi.mocked(apiClient.checkBackupExists).mockResolvedValueOnce({
        exists: false,
      });

      const info = await integrationService.checkBackupExists();

      expect(info.exists).toBe(false);
      expect(info.lastUpdatedAt).toBeUndefined();
    });
  });

  describe('Backup Deletion', () => {
    it('should delete backup successfully', async () => {
      vi.mocked(apiClient.deleteKeyBackup).mockResolvedValueOnce(undefined);

      await expect(integrationService.deleteBackup()).resolves.not.toThrow();
      expect(apiClient.deleteKeyBackup).toHaveBeenCalledOnce();
    });

    it('should handle deletion errors', async () => {
      vi.mocked(apiClient.deleteKeyBackup).mockRejectedValueOnce(new Error('Not found'));

      await expect(integrationService.deleteBackup()).rejects.toThrow(
        'Failed to delete backup: Not found'
      );
    });
  });

  describe('Complete Recovery Flow', () => {
    it('should recover keys on new device', async () => {
      // Create and store keys first
      const keyPair = cryptoKeyExchange.generateKeyPair();
      await cryptoKeyExchange.storeKeyPair('device-key', keyPair, testMasterPassword);

      // Create a local backup service to generate test data
      const { KeyBackupService } = await import('./keyBackup');
      const localBackupService = new KeyBackupService(keyManagementService, cryptoKeyExchange);
      const encryptedBackup = await localBackupService.createBackup(testPassword);

      // Mock API responses
      vi.mocked(apiClient.checkBackupExists).mockResolvedValueOnce({
        exists: true,
        lastUpdatedAt: new Date().toISOString(),
      });
      vi.mocked(apiClient.getKeyBackup).mockResolvedValueOnce(encryptedBackup);

      // Clear local storage (simulate new device)
      await cryptoKeyExchange.clearAll();
      await keyManagementService.clearAll();

      // Recover
      await integrationService.recoverOnNewDevice(testPassword);

      // Verify keys are restored
      const keyIds = await cryptoKeyExchange.getAllKeyPairIds();
      expect(keyIds).toContain('device-key');
    });

    it('should fail recovery when no backup exists', async () => {
      vi.mocked(apiClient.checkBackupExists).mockResolvedValueOnce({
        exists: false,
      });

      await expect(integrationService.recoverOnNewDevice(testPassword)).rejects.toThrow(
        'No backup found'
      );
    });

    it('should handle recovery errors', async () => {
      vi.mocked(apiClient.checkBackupExists).mockResolvedValueOnce({
        exists: true,
        lastUpdatedAt: new Date().toISOString(),
      });
      vi.mocked(apiClient.getKeyBackup).mockRejectedValueOnce(new Error('Server error'));

      await expect(integrationService.recoverOnNewDevice(testPassword)).rejects.toThrow(
        'Recovery failed: Restore failed: Server error'
      );
    });
  });
});
