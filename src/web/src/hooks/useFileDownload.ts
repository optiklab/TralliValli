/**
 * File Download Helper Hook
 *
 * Provides a simple way to download files with encryption support
 * in React components.
 */

import { useState, useCallback } from 'react';
import { FileDownloadService } from '@/services/fileDownload';
import { FileEncryptionService } from '@/services/fileEncryption';
import { KeyManagementService } from '@/services/keyManagement';
import type { EncryptedFileMetadata, DownloadProgress } from '@/services';

// Create singleton instances for the download service
const keyManagementService = new KeyManagementService();
const fileEncryptionService = new FileEncryptionService(keyManagementService);
const fileDownloadService = new FileDownloadService(fileEncryptionService);

export interface UseFileDownloadOptions {
  onProgress?: (progress: DownloadProgress) => void;
  onError?: (error: Error) => void;
  onSuccess?: () => void;
}

export interface UseFileDownloadReturn {
  downloadFile: (
    fileId: string,
    conversationId: string,
    encryptionMetadata?: EncryptedFileMetadata
  ) => Promise<void>;
  isDownloading: boolean;
  progress: DownloadProgress | null;
  error: Error | null;
}

/**
 * Hook for downloading files with encryption support
 */
export function useFileDownload(options?: UseFileDownloadOptions): UseFileDownloadReturn {
  const [isDownloading, setIsDownloading] = useState(false);
  const [progress, setProgress] = useState<DownloadProgress | null>(null);
  const [error, setError] = useState<Error | null>(null);

  const downloadFile = useCallback(
    async (fileId: string, conversationId: string, encryptionMetadata?: EncryptedFileMetadata) => {
      setIsDownloading(true);
      setProgress(null);
      setError(null);

      try {
        await fileDownloadService.downloadFile({
          fileId,
          conversationId,
          encryptionMetadata,
          onProgress: (p) => {
            setProgress(p);
            options?.onProgress?.(p);
          },
        });
        options?.onSuccess?.();
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Download failed');
        setError(error);
        options?.onError?.(error);
      } finally {
        setIsDownloading(false);
      }
    },
    [options]
  );

  return {
    downloadFile,
    isDownloading,
    progress,
    error,
  };
}

export default useFileDownload;
