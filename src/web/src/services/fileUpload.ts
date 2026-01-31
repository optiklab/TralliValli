/**
 * File Upload Service
 *
 * Provides file upload functionality with:
 * - Presigned URL upload to Azure Blob Storage
 * - Image compression (max 2MB)
 * - Thumbnail generation for preview
 * - Upload progress tracking
 * - Upload cancellation support
 */

import { apiClient } from './api';
import { FileEncryptionService, type EncryptedFileMetadata } from './fileEncryption';
import type {
  PresignedUrlRequest,
  PresignedUrlResponse,
  UploadProgress,
  FileMetadata,
} from '@/types/api';

// ============================================================================
// Constants
// ============================================================================

const MAX_IMAGE_SIZE_BYTES = 2 * 1024 * 1024; // 2MB
const MAX_IMAGE_DIMENSION = 2048; // Max width or height in pixels
const THUMBNAIL_MAX_WIDTH = 200;
const THUMBNAIL_MAX_HEIGHT = 200;
const COMPRESSION_QUALITY = 0.8; // 80% quality

// ============================================================================
// Types
// ============================================================================

export interface UploadOptions {
  conversationId: string;
  file: File;
  onProgress?: (progress: UploadProgress) => void;
  signal?: AbortSignal;
  encryptionService?: FileEncryptionService;
}

export interface UploadResult {
  fileId: string;
  blobPath: string;
  thumbnailDataUrl?: string;
  metadata: FileMetadata;
  encryptionMetadata?: EncryptedFileMetadata;
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Check if a file is an image
 */
function isImage(file: File): boolean {
  return file.type.startsWith('image/');
}

/**
 * Check if an image needs compression based on dimensions
 */
async function needsCompression(file: File): Promise<boolean> {
  if (!isImage(file)) {
    return false;
  }

  return new Promise((resolve) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const img = new Image();
      img.onload = () => {
        resolve(img.width > MAX_IMAGE_DIMENSION || img.height > MAX_IMAGE_DIMENSION);
      };
      img.onerror = () => resolve(false);
      img.src = e.target?.result as string;
    };
    reader.onerror = () => resolve(false);
    reader.readAsDataURL(file);
  });
}

/**
 * Compress an image file to meet the max size and dimension requirements
 */
async function compressImage(file: File): Promise<File> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = (e) => {
      const img = new Image();

      img.onload = () => {
        const canvas = document.createElement('canvas');
        let { width, height } = img;

        // Scale down to max dimension if needed
        if (width > MAX_IMAGE_DIMENSION || height > MAX_IMAGE_DIMENSION) {
          if (width > height) {
            height = Math.floor((height * MAX_IMAGE_DIMENSION) / width);
            width = MAX_IMAGE_DIMENSION;
          } else {
            width = Math.floor((width * MAX_IMAGE_DIMENSION) / height);
            height = MAX_IMAGE_DIMENSION;
          }
        }

        canvas.width = width;
        canvas.height = height;

        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('Failed to get canvas context'));
          return;
        }

        ctx.drawImage(img, 0, 0, width, height);

        // Convert to blob with compression
        canvas.toBlob(
          (blob) => {
            if (!blob) {
              reject(new Error('Failed to compress image'));
              return;
            }

            const compressedFile = new File([blob], file.name, {
              type: file.type,
              lastModified: Date.now(),
            });
            resolve(compressedFile);
          },
          file.type,
          COMPRESSION_QUALITY
        );
      };

      img.onerror = () => {
        reject(new Error('Failed to load image'));
      };

      img.src = e.target?.result as string;
    };

    reader.onerror = () => {
      reject(new Error('Failed to read file'));
    };

    reader.readAsDataURL(file);
  });
}

/**
 * Generate a thumbnail for an image
 */
async function generateThumbnail(file: File): Promise<string | undefined> {
  if (!isImage(file)) {
    return undefined;
  }

  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = (e) => {
      const img = new Image();

      img.onload = () => {
        const canvas = document.createElement('canvas');

        // Calculate dimensions to maintain aspect ratio
        let { width, height } = img;
        if (width > height) {
          if (width > THUMBNAIL_MAX_WIDTH) {
            height = (height * THUMBNAIL_MAX_WIDTH) / width;
            width = THUMBNAIL_MAX_WIDTH;
          }
        } else {
          if (height > THUMBNAIL_MAX_HEIGHT) {
            width = (width * THUMBNAIL_MAX_HEIGHT) / height;
            height = THUMBNAIL_MAX_HEIGHT;
          }
        }

        canvas.width = width;
        canvas.height = height;

        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('Failed to get canvas context'));
          return;
        }

        ctx.drawImage(img, 0, 0, width, height);

        try {
          const thumbnailDataUrl = canvas.toDataURL('image/jpeg', 0.8);
          resolve(thumbnailDataUrl);
        } catch (error) {
          reject(error);
        }
      };

      img.onerror = () => {
        reject(new Error('Failed to load image for thumbnail'));
      };

      img.src = e.target?.result as string;
    };

    reader.onerror = () => {
      reject(new Error('Failed to read file for thumbnail'));
    };

    reader.readAsDataURL(file);
  });
}

/**
 * Upload file to a presigned URL with progress tracking
 */
async function uploadToPresignedUrl(
  url: string,
  file: File,
  onProgress?: (progress: UploadProgress) => void,
  signal?: AbortSignal
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();

    // Setup progress tracking
    xhr.upload.addEventListener('progress', (e) => {
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
        resolve();
      } else {
        reject(new Error(`Upload failed with status ${xhr.status}`));
      }
    });

    // Handle errors
    xhr.addEventListener('error', () => {
      reject(new Error('Upload failed due to network error'));
    });

    xhr.addEventListener('abort', () => {
      reject(new Error('Upload was cancelled'));
    });

    // Setup cancellation
    if (signal) {
      signal.addEventListener('abort', () => {
        xhr.abort();
      });
    }

    // Start upload
    xhr.open('PUT', url);
    xhr.setRequestHeader('Content-Type', file.type);
    xhr.setRequestHeader('x-ms-blob-type', 'BlockBlob');
    xhr.send(file);
  });
}

// ============================================================================
// FileUploadService Class
// ============================================================================

export class FileUploadService {
  /**
   * Upload a file with progress tracking and optional compression
   */
  async uploadFile(options: UploadOptions): Promise<UploadResult> {
    const { conversationId, file, onProgress, signal, encryptionService } = options;

    // Validate file
    if (!file || file.size === 0) {
      throw new Error('Invalid file');
    }

    // Check if aborted before starting
    if (signal?.aborted) {
      throw new Error('Upload was cancelled');
    }

    let fileToUpload = file;
    let thumbnailDataUrl: string | undefined;

    // Compress image if needed
    if (isImage(file)) {
      try {
        // Only compress if dimensions exceed max
        const shouldCompress = await needsCompression(file);
        if (shouldCompress) {
          fileToUpload = await compressImage(file);
        }

        thumbnailDataUrl = await generateThumbnail(fileToUpload);

        // Validate file size (even if dimensions don't require compression, size might still be too large)
        if (fileToUpload.size > MAX_IMAGE_SIZE_BYTES) {
          throw new Error(
            `Image is too large (${(fileToUpload.size / (1024 * 1024)).toFixed(1)}MB). Maximum size is 2MB. Try reducing image quality or dimensions.`
          );
        }
      } catch (error) {
        // If compression fails or image is still too large, throw error
        if (error instanceof Error && error.message.includes('too large')) {
          throw error;
        }
        throw new Error('Failed to process image for upload');
      }
    } else {
      // Validate non-image file size
      if (file.size > MAX_IMAGE_SIZE_BYTES) {
        throw new Error(
          `File is too large (${(file.size / (1024 * 1024)).toFixed(1)}MB). Maximum size is 2MB.`
        );
      }
    }

    // Encrypt file if encryption service is provided
    let encryptionMetadata: EncryptedFileMetadata | undefined;
    let finalFileToUpload: File | Blob = fileToUpload;

    if (encryptionService) {
      const encryptResult = await encryptionService.encryptFile(
        conversationId,
        fileToUpload,
        (progress) => {
          // Report encryption progress as part of overall upload (first 20% of progress)
          onProgress?.({
            loaded: progress.processed,
            total: progress.total * 5, // Total work is 5x encryption (20% encryption, 80% upload)
            percentage: Math.round((progress.percentage * 20) / 100),
          });
        }
      );

      if (!encryptResult.success) {
        throw new Error(encryptResult.error || 'Failed to encrypt file');
      }

      finalFileToUpload = encryptResult.encryptedBlob;
      encryptionMetadata = encryptResult.metadata;
    }

    // Get presigned URL from API
    const presignedUrlRequest: PresignedUrlRequest = {
      conversationId,
      fileName: file.name,
      fileSize: finalFileToUpload.size,
      mimeType: encryptionService ? 'application/octet-stream' : fileToUpload.type,
    };

    const presignedUrlResponse: PresignedUrlResponse =
      await apiClient.getPresignedUrl(presignedUrlRequest);

    // Check if aborted after getting presigned URL
    if (signal?.aborted) {
      throw new Error('Upload was cancelled');
    }

    // Upload to presigned URL with progress tracking
    await uploadToPresignedUrl(
      presignedUrlResponse.uploadUrl,
      finalFileToUpload,
      (progress) => {
        // If encrypted, adjust progress to account for encryption step (20-100%)
        if (encryptionService) {
          const uploadTotalWork = progress.total * 5; // Total work is 5x upload
          onProgress?.({
            loaded: progress.loaded,
            total: uploadTotalWork,
            percentage: 20 + Math.round((progress.percentage * 80) / 100),
          });
        } else {
          onProgress?.(progress);
        }
      },
      signal
    );

    // Get file metadata after successful upload
    const metadata = await apiClient.getFileMetadata(presignedUrlResponse.fileId);

    return {
      fileId: presignedUrlResponse.fileId,
      blobPath: presignedUrlResponse.blobPath,
      thumbnailDataUrl,
      metadata,
      encryptionMetadata,
    };
  }
}

// Export singleton instance
export const fileUploadService = new FileUploadService();
export default fileUploadService;
