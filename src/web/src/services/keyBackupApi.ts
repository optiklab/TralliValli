/**
 * API Client for Key Backup Operations
 *
 * Provides communication layer between client and server for key backup storage and retrieval.
 */

import type { EncryptedBackup } from './keyBackup';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

/**
 * API response types
 */
interface StoreKeyBackupResponse {
  backupId: string;
  createdAt: string;
  message: string;
}

interface GetKeyBackupResponse {
  backupId: string;
  version: number;
  encryptedData: string;
  iv: string;
  salt: string;
  createdAt: string;
  updatedAt: string;
}

interface KeyBackupExistsResponse {
  exists: boolean;
  lastUpdatedAt?: string;
}

/**
 * Key Backup API Client
 *
 * Handles HTTP communication with the server for key backup operations.
 */
export class KeyBackupApiClient {
  private accessToken: string | null = null;

  /**
   * Set the authentication token
   * @param token - JWT access token
   */
  setAccessToken(token: string): void {
    this.accessToken = token;
  }

  /**
   * Clear the authentication token
   */
  clearAccessToken(): void {
    this.accessToken = null;
  }

  /**
   * Get authorization headers
   */
  private getAuthHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (this.accessToken) {
      headers['Authorization'] = `Bearer ${this.accessToken}`;
    }

    return headers;
  }

  /**
   * Store an encrypted key backup on the server
   * @param backup - Encrypted backup blob
   * @returns Response with backup ID
   */
  async storeKeyBackup(backup: EncryptedBackup): Promise<StoreKeyBackupResponse> {
    const response = await fetch(`${API_BASE_URL}/key-backups`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({
        version: backup.version,
        encryptedData: backup.encryptedData,
        iv: backup.iv,
        salt: backup.salt,
      }),
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Unauthorized. Please log in again.');
      }
      const errorData = await response.json().catch(() => ({ message: 'Unknown error' }));
      throw new Error(errorData.message || `Failed to store backup: ${response.statusText}`);
    }

    return await response.json();
  }

  /**
   * Retrieve the encrypted key backup from the server
   * @returns Encrypted backup blob
   */
  async getKeyBackup(): Promise<EncryptedBackup> {
    const response = await fetch(`${API_BASE_URL}/key-backups`, {
      method: 'GET',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Unauthorized. Please log in again.');
      }
      if (response.status === 404) {
        throw new Error('No backup found. Please create a backup first.');
      }
      const errorData = await response.json().catch(() => ({ message: 'Unknown error' }));
      throw new Error(errorData.message || `Failed to retrieve backup: ${response.statusText}`);
    }

    const data: GetKeyBackupResponse = await response.json();

    // Convert API response to EncryptedBackup format
    return {
      version: data.version,
      encryptedData: data.encryptedData,
      iv: data.iv,
      salt: data.salt,
      createdAt: data.createdAt,
    };
  }

  /**
   * Check if a key backup exists for the authenticated user
   * @returns Whether a backup exists and when it was last updated
   */
  async checkBackupExists(): Promise<KeyBackupExistsResponse> {
    const response = await fetch(`${API_BASE_URL}/key-backups/exists`, {
      method: 'GET',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Unauthorized. Please log in again.');
      }
      const errorData = await response.json().catch(() => ({ message: 'Unknown error' }));
      throw new Error(errorData.message || `Failed to check backup: ${response.statusText}`);
    }

    return await response.json();
  }

  /**
   * Delete the key backup from the server
   */
  async deleteKeyBackup(): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/key-backups`, {
      method: 'DELETE',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Unauthorized. Please log in again.');
      }
      if (response.status === 404) {
        throw new Error('No backup found to delete.');
      }
      const errorData = await response.json().catch(() => ({ message: 'Unknown error' }));
      throw new Error(errorData.message || `Failed to delete backup: ${response.statusText}`);
    }
  }
}

/**
 * Singleton instance of the API client
 */
export const keyBackupApiClient = new KeyBackupApiClient();
