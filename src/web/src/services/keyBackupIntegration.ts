/**
 * Key Backup and Recovery Integration Service
 *
 * Provides a high-level interface for complete backup and recovery workflows,
 * integrating the local KeyBackupService with the server API.
 */

import { KeyBackupService } from './keyBackup';
import { KeyBackupApiClient } from './keyBackupApi';
import type { KeyManagementService } from './keyManagement';
import type { CryptoKeyExchange } from './cryptoKeyExchange';

export interface BackupInfo {
  exists: boolean;
  lastUpdatedAt?: string;
}

/**
 * Key Backup Integration Service
 *
 * Orchestrates the complete backup and recovery workflow:
 * 1. Local key collection and encryption
 * 2. Upload to server
 * 3. Download from server
 * 4. Decryption and restoration
 */
export class KeyBackupIntegrationService {
  private backupService: KeyBackupService;
  private apiClient: KeyBackupApiClient;

  constructor(
    keyManagementService: KeyManagementService,
    cryptoKeyExchange: CryptoKeyExchange,
    apiClient: KeyBackupApiClient
  ) {
    this.backupService = new KeyBackupService(keyManagementService, cryptoKeyExchange);
    this.apiClient = apiClient;
  }

  /**
   * Create and upload an encrypted backup to the server
   * @param password - Password for backup encryption
   * @returns Backup ID from server
   *
   * Workflow:
   * 1. Collect all keys from local storage
   * 2. Encrypt with password using PBKDF2/AES-256-GCM
   * 3. Upload encrypted blob to server
   */
  async createAndUploadBackup(password: string): Promise<string> {
    try {
      // Create encrypted backup locally
      const encryptedBackup = await this.backupService.createBackup(password);

      // Upload to server
      const response = await this.apiClient.storeKeyBackup(encryptedBackup);

      return response.backupId;
    } catch (error) {
      if (error instanceof Error) {
        throw new Error(`Backup failed: ${error.message}`);
      }
      throw new Error('Backup failed with unknown error');
    }
  }

  /**
   * Download and restore an encrypted backup from the server
   * @param password - Password for backup decryption
   *
   * Workflow:
   * 1. Download encrypted blob from server
   * 2. Decrypt with password
   * 3. Restore keys to local storage
   *
   * Security Note: Keys are restored with their original encryption.
   * Use the same password that was used to encrypt the private keys.
   */
  async downloadAndRestoreBackup(password: string): Promise<void> {
    try {
      // Download backup from server
      const encryptedBackup = await this.apiClient.getKeyBackup();

      // Restore locally
      await this.backupService.restoreBackup(encryptedBackup, password);
    } catch (error) {
      if (error instanceof Error) {
        throw new Error(`Restore failed: ${error.message}`);
      }
      throw new Error('Restore failed with unknown error');
    }
  }

  /**
   * Check if a backup exists on the server
   * @returns Backup information
   */
  async checkBackupExists(): Promise<BackupInfo> {
    try {
      return await this.apiClient.checkBackupExists();
    } catch (error) {
      if (error instanceof Error) {
        throw new Error(`Failed to check backup: ${error.message}`);
      }
      throw new Error('Failed to check backup with unknown error');
    }
  }

  /**
   * Delete the backup from the server
   */
  async deleteBackup(): Promise<void> {
    try {
      await this.apiClient.deleteKeyBackup();
    } catch (error) {
      if (error instanceof Error) {
        throw new Error(`Failed to delete backup: ${error.message}`);
      }
      throw new Error('Failed to delete backup with unknown error');
    }
  }

  /**
   * Complete recovery flow for new device/browser
   * @param password - Password used when the backup was created (not the master password for individual keys)
   *
   * This is the main entry point for recovering keys on a new device.
   * It downloads the backup from the server and restores all keys locally.
   *
   * Note: This password is for the backup encryption itself, created during createBackup().
   * The individual keys within the backup remain encrypted with their original passwords.
   */
  async recoverOnNewDevice(password: string): Promise<void> {
    try {
      // Check if backup exists
      const backupInfo = await this.checkBackupExists();
      if (!backupInfo.exists) {
        throw new Error('No backup found. Cannot recover keys.');
      }

      // Download and restore
      await this.downloadAndRestoreBackup(password);
    } catch (error) {
      if (error instanceof Error) {
        throw new Error(`Recovery failed: ${error.message}`);
      }
      throw new Error('Recovery failed with unknown error');
    }
  }
}
