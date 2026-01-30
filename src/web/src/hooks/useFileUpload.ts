/**
 * File Upload Helper Hook
 *
 * Provides a simple way to upload files with encryption support
 * in React components.
 */

import { useState, useCallback } from 'react';
import { fileUploadService, type UploadResult } from '@/services/fileUpload';
import { FileEncryptionService } from '@/services/fileEncryption';
import { KeyManagementService } from '@/services/keyManagement';
import type { UploadProgress } from '@/types/api';

// Create singleton instances for encryption
const keyManagementService = new KeyManagementService();
const fileEncryptionService = new FileEncryptionService(keyManagementService);

export interface UseFileUploadOptions {
  enableEncryption?: boolean;
  onProgress?: (progress: UploadProgress) => void;
  onError?: (error: Error) => void;
  onSuccess?: (result: UploadResult) => void;
}

export interface UseFileUploadReturn {
  uploadFile: (file: File, conversationId: string) => Promise<UploadResult | null>;
  isUploading: boolean;
  progress: UploadProgress | null;
  error: Error | null;
  result: UploadResult | null;
}

/**
 * Hook for uploading files with optional encryption support
 */
export function useFileUpload(options?: UseFileUploadOptions): UseFileUploadReturn {
  const [isUploading, setIsUploading] = useState(false);
  const [progress, setProgress] = useState<UploadProgress | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [result, setResult] = useState<UploadResult | null>(null);

  const uploadFile = useCallback(
    async (file: File, conversationId: string): Promise<UploadResult | null> => {
      setIsUploading(true);
      setProgress(null);
      setError(null);
      setResult(null);

      try {
        const uploadResult = await fileUploadService.uploadFile({
          conversationId,
          file,
          onProgress: (p) => {
            setProgress(p);
            options?.onProgress?.(p);
          },
          encryptionService: options?.enableEncryption ? fileEncryptionService : undefined,
        });

        setResult(uploadResult);
        options?.onSuccess?.(uploadResult);
        return uploadResult;
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Upload failed');
        setError(error);
        options?.onError?.(error);
        return null;
      } finally {
        setIsUploading(false);
      }
    },
    [options]
  );

  return {
    uploadFile,
    isUploading,
    progress,
    error,
    result,
  };
}

export default useFileUpload;
