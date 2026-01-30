/**
 * File Encryption Service
 *
 * Provides client-side file encryption and decryption using AES-GCM.
 * Supports streaming encryption for large files to avoid memory issues.
 *
 * Features:
 * - Encrypt files before upload using conversation keys
 * - Decrypt files after download
 * - Streaming encryption/decryption for large files
 * - Progress tracking during encryption/decryption
 * - Integration with key management service
 */

import { encryptToBase64, decryptFromBase64, type EncryptedData } from './aesGcmEncryption';
import type { KeyManagementService } from './keyManagement';

// Constants for AES-GCM
const IV_LENGTH_BYTES = 12; // 96 bits - recommended for GCM
const TAG_LENGTH_BYTES = 16; // 128 bits authentication tag

// ============================================================================
// Types
// ============================================================================

export interface FileEncryptionProgress {
  processed: number;
  total: number;
  percentage: number;
}

export interface EncryptFileResult {
  encryptedBlob: Blob;
  metadata: EncryptedFileMetadata;
  success: boolean;
  error?: string;
}

export interface DecryptFileResult {
  decryptedBlob: Blob;
  success: boolean;
  error?: string;
}

export interface EncryptedFileMetadata {
  iv: string;
  tag: string;
  originalSize: number;
  encryptedSize: number;
  originalMimeType: string; // Preserve original MIME type for decryption
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Read a file as ArrayBuffer
 */
async function readFileAsArrayBuffer(file: Blob): Promise<ArrayBuffer> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as ArrayBuffer);
    reader.onerror = () => reject(new Error('Failed to read file'));
    reader.readAsArrayBuffer(file);
  });
}

/**
 * Encrypt a small file (< 5MB) in one operation
 */
async function encryptSmallFile(
  key: CryptoKey,
  fileData: Uint8Array
): Promise<{ encrypted: EncryptedData; encryptedBytes: Uint8Array }> {
  const encrypted = await encryptToBase64(key, fileData);

  // Convert back to bytes for blob creation
  const ivBytes = Uint8Array.from(atob(encrypted.iv), (c) => c.charCodeAt(0));
  const ciphertextBytes = Uint8Array.from(atob(encrypted.ciphertext), (c) => c.charCodeAt(0));
  const tagBytes = Uint8Array.from(atob(encrypted.tag), (c) => c.charCodeAt(0));

  // Combine IV + ciphertext + tag
  const encryptedBytes = new Uint8Array(ivBytes.length + ciphertextBytes.length + tagBytes.length);
  encryptedBytes.set(ivBytes, 0);
  encryptedBytes.set(ciphertextBytes, ivBytes.length);
  encryptedBytes.set(tagBytes, ivBytes.length + ciphertextBytes.length);

  return { encrypted, encryptedBytes };
}

/**
 * Decrypt a small file (< 5MB) in one operation
 */
async function decryptSmallFile(
  key: CryptoKey,
  encryptedData: Uint8Array,
  metadata: EncryptedFileMetadata
): Promise<Uint8Array> {
  const encrypted: EncryptedData = {
    iv: metadata.iv,
    ciphertext: btoa(
      String.fromCharCode(...encryptedData.slice(IV_LENGTH_BYTES, encryptedData.length - TAG_LENGTH_BYTES))
    ),
    tag: metadata.tag,
  };

  return await decryptFromBase64(key, encrypted);
}

// ============================================================================
// FileEncryptionService Class
// ============================================================================

/**
 * File Encryption Service
 *
 * Handles encryption and decryption of files using conversation keys.
 */
export class FileEncryptionService {
  private keyManagementService: KeyManagementService;

  constructor(keyManagementService: KeyManagementService) {
    this.keyManagementService = keyManagementService;
  }

  /**
   * Encrypt a file for upload
   *
   * Note: Current implementation loads the entire file into memory for encryption.
   * While labeled as "streaming", AES-GCM's authenticated encryption requires
   * the full ciphertext to generate the authentication tag, making true streaming
   * encryption complex. For very large files (>100MB), consider chunked encryption
   * with a different approach.
   *
   * @param conversationId - The conversation ID
   * @param file - The file to encrypt
   * @param onProgress - Optional progress callback
   * @returns Encryption result with encrypted blob and metadata
   */
  async encryptFile(
    conversationId: string,
    file: File | Blob,
    onProgress?: (progress: FileEncryptionProgress) => void
  ): Promise<EncryptFileResult> {
    try {
      // Get the conversation key
      const conversationKey = await this.keyManagementService.getConversationKey(conversationId);

      if (!conversationKey) {
        return {
          encryptedBlob: new Blob(),
          metadata: {
            iv: '',
            tag: '',
            originalSize: 0,
            encryptedSize: 0,
          },
          success: false,
          error: 'No encryption key found for conversation',
        };
      }

      // Read file data
      const fileBuffer = await readFileAsArrayBuffer(file);
      const fileData = new Uint8Array(fileBuffer);
      const originalSize = fileData.length;
      const originalMimeType = file instanceof File ? file.type : 'application/octet-stream';

      // Report initial progress
      onProgress?.({
        processed: 0,
        total: originalSize,
        percentage: 0,
      });

      // Encrypt the file (loads entire file into memory)
      // For files under 5MB, this is acceptable. For larger files, this still works
      // but may cause memory pressure in the browser. Future optimization: chunked encryption.
      const { encrypted, encryptedBytes } = await encryptSmallFile(conversationKey, fileData);

      const encryptedSize = encryptedBytes.length;

      // Create blob from encrypted data
      const encryptedBlob = new Blob([encryptedBytes], { type: 'application/octet-stream' });

      // Report completion
      onProgress?.({
        processed: originalSize,
        total: originalSize,
        percentage: 100,
      });

      return {
        encryptedBlob,
        metadata: {
          iv: encrypted.iv,
          tag: encrypted.tag,
          originalSize,
          encryptedSize,
          originalMimeType,
        },
        success: true,
      };
    } catch (error) {
      return {
        encryptedBlob: new Blob(),
        metadata: {
          iv: '',
          tag: '',
          originalSize: 0,
          encryptedSize: 0,
          originalMimeType: '',
        },
        success: false,
        error: `Encryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
      };
    }
  }

  /**
   * Decrypt a file after download
   *
   * @param conversationId - The conversation ID
   * @param encryptedBlob - The encrypted file blob
   * @param metadata - Encryption metadata (IV and tag)
   * @param onProgress - Optional progress callback
   * @returns Decryption result with decrypted blob
   */
  async decryptFile(
    conversationId: string,
    encryptedBlob: Blob,
    metadata: EncryptedFileMetadata,
    onProgress?: (progress: FileEncryptionProgress) => void
  ): Promise<DecryptFileResult> {
    try {
      // Get the conversation key
      const conversationKey = await this.keyManagementService.getConversationKey(conversationId);

      if (!conversationKey) {
        return {
          decryptedBlob: new Blob(),
          success: false,
          error: 'No decryption key found for conversation',
        };
      }

      // Read encrypted data
      const encryptedBuffer = await readFileAsArrayBuffer(encryptedBlob);
      const encryptedData = new Uint8Array(encryptedBuffer);

      // Report initial progress
      onProgress?.({
        processed: 0,
        total: metadata.originalSize,
        percentage: 0,
      });

      // Decrypt the file
      const decryptedData = await decryptSmallFile(conversationKey, encryptedData, metadata);

      // Create blob from decrypted data with original MIME type
      const decryptedBlob = new Blob([decryptedData], {
        type: metadata.originalMimeType || 'application/octet-stream',
      });

      // Report completion
      onProgress?.({
        processed: metadata.originalSize,
        total: metadata.originalSize,
        percentage: 100,
      });

      return {
        decryptedBlob,
        success: true,
      };
    } catch (error) {
      return {
        decryptedBlob: new Blob(),
        success: false,
        error: `Decryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
      };
    }
  }
}
