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
const THUMBNAIL_MAX_WIDTH = 200;
const THUMBNAIL_MAX_HEIGHT = 200;
const COMPRESSION_QUALITY = 0.85;

// ============================================================================
// Types
// ============================================================================

export interface UploadOptions {
  conversationId: string;
  file: File;
  onProgress?: (progress: UploadProgress) => void;
  signal?: AbortSignal;
}

export interface UploadResult {
  fileId: string;
  blobPath: string;
  thumbnailDataUrl?: string;
  metadata: FileMetadata;
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
 * Compress an image file to meet the max size requirement
 */
async function compressImage(file: File): Promise<File> {
  // If already under max size, return as-is
  if (file.size <= MAX_IMAGE_SIZE_BYTES) {
    return file;
  }

  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = (e) => {
      const img = new Image();

      img.onload = () => {
        const canvas = document.createElement('canvas');
        let { width, height } = img;

        // Calculate compression ratio based on file size
        const ratio = Math.sqrt(MAX_IMAGE_SIZE_BYTES / file.size);
        width = Math.floor(width * ratio);
        height = Math.floor(height * ratio);

        canvas.width = width;
        canvas.height = height;

        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('Failed to get canvas context'));
          return;
        }

        ctx.drawImage(img, 0, 0, width, height);

        canvas.toBlob(
          (blob) => {
            if (!blob) {
              reject(new Error('Failed to compress image'));
              return;
            }

            // If still too large, try with lower quality
            if (blob.size > MAX_IMAGE_SIZE_BYTES) {
              canvas.toBlob(
                (blob2) => {
                  if (!blob2) {
                    reject(new Error('Failed to compress image'));
                    return;
                  }
                  const compressedFile = new File([blob2], file.name, {
                    type: file.type,
                    lastModified: Date.now(),
                  });
                  resolve(compressedFile);
                },
                file.type,
                0.7 // Lower quality
              );
            } else {
              const compressedFile = new File([blob], file.name, {
                type: file.type,
                lastModified: Date.now(),
              });
              resolve(compressedFile);
            }
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
    const { conversationId, file, onProgress, signal } = options;

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
        fileToUpload = await compressImage(file);
        thumbnailDataUrl = await generateThumbnail(fileToUpload);
      } catch (error) {
        console.warn('Image compression/thumbnail generation failed:', error);
        // Continue with original file if compression fails
        fileToUpload = file;
      }
    }

    // Get presigned URL from API
    const presignedUrlRequest: PresignedUrlRequest = {
      conversationId,
      fileName: file.name,
      fileSize: fileToUpload.size,
      mimeType: fileToUpload.type,
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
      fileToUpload,
      onProgress,
      signal
    );

    // Get file metadata after successful upload
    const metadata = await apiClient.getFileMetadata(presignedUrlResponse.fileId);

    return {
      fileId: presignedUrlResponse.fileId,
      blobPath: presignedUrlResponse.blobPath,
      thumbnailDataUrl,
      metadata,
    };
  }

  /**
   * Cancel an upload (use AbortController with uploadFile)
   */
  static createAbortController(): AbortController {
    return new AbortController();
  }
}

// Export singleton instance
export const fileUploadService = new FileUploadService();
export default fileUploadService;
