/**
 * File Download Service
 *
 * Provides file download functionality with:
 * - Fetch encrypted files from Azure Blob Storage
 * - Decrypt files before downloading
 * - Download progress tracking
 * - Trigger browser download
 */

import { apiClient } from './api';
import { FileEncryptionService, type EncryptedFileMetadata } from './fileEncryption';
import type { FileMetadata } from '@/types/api';

// ============================================================================
// Types
// ============================================================================

export interface DownloadProgress {
  loaded: number;
  total: number;
  percentage: number;
}

export interface DownloadOptions {
  fileId: string;
  conversationId: string;
  encryptionMetadata?: EncryptedFileMetadata;
  onProgress?: (progress: DownloadProgress) => void;
  signal?: AbortSignal;
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Download a file from a URL with progress tracking
 */
async function downloadFromUrl(
  url: string,
  onProgress?: (progress: DownloadProgress) => void,
  signal?: AbortSignal
): Promise<Blob> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();

    // Setup progress tracking
    xhr.addEventListener('progress', (e) => {
      if (e.lengthComputable && onProgress) {
        onProgress({
          loaded: e.loaded,
          total: e.total,
          percentage: Math.round((e.loaded / e.total) * 100),
        });
      }
    });

    // Handle completion
    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve(xhr.response as Blob);
      } else {
        reject(new Error(`Download failed with status ${xhr.status}`));
      }
    });

    // Handle errors
    xhr.addEventListener('error', () => {
      reject(new Error('Download failed due to network error'));
    });

    xhr.addEventListener('abort', () => {
      reject(new Error('Download was cancelled'));
    });

    // Setup cancellation
    if (signal) {
      signal.addEventListener('abort', () => {
        xhr.abort();
      });
    }

    // Start download
    xhr.open('GET', url);
    xhr.responseType = 'blob';
    xhr.send();
  });
}

/**
 * Trigger browser download of a blob
 */
function triggerBrowserDownload(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  // Clean up the object URL after a short delay
  setTimeout(() => {
    URL.revokeObjectURL(url);
  }, 100);
}

// ============================================================================
// FileDownloadService Class
// ============================================================================

export class FileDownloadService {
  private encryptionService: FileEncryptionService;

  constructor(encryptionService: FileEncryptionService) {
    this.encryptionService = encryptionService;
  }

  /**
   * Download and decrypt a file
   *
   * @param options - Download options
   * @returns Promise that resolves when download is complete
   */
  async downloadFile(options: DownloadOptions): Promise<void> {
    const { fileId, conversationId, encryptionMetadata, onProgress, signal } = options;

    // Check if aborted before starting
    if (signal?.aborted) {
      throw new Error('Download was cancelled');
    }

    // Get file metadata
    const metadata: FileMetadata = await apiClient.getFileMetadata(fileId);

    // Get download URL from API
    const downloadUrlResponse = await apiClient.request<{ downloadUrl: string; fileName: string }>(
      `/files/${fileId}/download-url`,
      {
        method: 'GET',
      }
    );

    // Check if aborted after getting download URL
    if (signal?.aborted) {
      throw new Error('Download was cancelled');
    }

    // Download encrypted file with progress tracking (50% of total progress)
    const encryptedBlob = await downloadFromUrl(
      downloadUrlResponse.downloadUrl,
      (progress) => {
        onProgress?.({
          loaded: progress.loaded,
          total: progress.total * 2, // Account for decryption step
          percentage: Math.round(progress.percentage / 2),
        });
      },
      signal
    );

    // If encryption metadata is provided, decrypt the file
    if (encryptionMetadata) {
      // Decrypt with progress tracking (remaining 50% of total progress)
      const decryptResult = await this.encryptionService.decryptFile(
        conversationId,
        encryptedBlob,
        encryptionMetadata,
        (progress) => {
          onProgress?.({
            loaded: progress.processed,
            total: progress.total * 2, // Account for download step
            percentage: 50 + Math.round(progress.percentage / 2),
          });
        }
      );

      if (!decryptResult.success) {
        throw new Error(decryptResult.error || 'Failed to decrypt file');
      }

      // Trigger download of decrypted file
      triggerBrowserDownload(decryptResult.decryptedBlob, metadata.fileName);
    } else {
      // No encryption metadata, download as-is (for backward compatibility)
      triggerBrowserDownload(encryptedBlob, metadata.fileName);
    }

    // Report completion
    onProgress?.({
      loaded: metadata.size,
      total: metadata.size,
      percentage: 100,
    });
  }
}

// Export singleton instance (will be created with proper dependencies elsewhere)
export default FileDownloadService;
