/**
 * FileUpload Component
 *
 * Displays file upload progress and provides controls for:
 * - Upload progress bar
 * - Cancellation
 * - Thumbnail preview for images
 * - File metadata display
 */

import { useState, useEffect } from 'react';
import { fileUploadService, type UploadOptions, type UploadResult } from '@/services';
import type { UploadProgress } from '@/types/api';

export interface FileUploadProps {
  file: File;
  conversationId: string;
  onUploadComplete?: (result: UploadResult) => void;
  onUploadError?: (error: Error) => void;
  onCancel?: () => void;
}

export function FileUpload({
  file,
  conversationId,
  onUploadComplete,
  onUploadError,
  onCancel,
}: FileUploadProps) {
  const [progress, setProgress] = useState<UploadProgress>({ loaded: 0, total: 0, percentage: 0 });
  const [isUploading, setIsUploading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [abortController] = useState(() => new AbortController());
  const [thumbnail, setThumbnail] = useState<string | null>(null);

  useEffect(() => {
    const uploadFile = async () => {
      try {
        const options: UploadOptions = {
          conversationId,
          file,
          onProgress: setProgress,
          signal: abortController.signal,
        };

        const result = await fileUploadService.uploadFile(options);

        // Set thumbnail if available
        if (result.thumbnailDataUrl) {
          setThumbnail(result.thumbnailDataUrl);
        }

        setIsUploading(false);
        onUploadComplete?.(result);
      } catch (err) {
        setIsUploading(false);
        const errorMessage = err instanceof Error ? err.message : 'Upload failed';
        setError(errorMessage);
        onUploadError?.(err instanceof Error ? err : new Error(errorMessage));
      }
    };

    uploadFile();

    // Cleanup on unmount
    return () => {
      if (isUploading) {
        abortController.abort();
      }
    };
  }, [file, conversationId, onUploadComplete, onUploadError, abortController, isUploading]);

  const handleCancel = () => {
    abortController.abort();
    setIsUploading(false);
    onCancel?.();
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const isImage = file.type.startsWith('image/');

  return (
    <div className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg border border-gray-200">
      {/* Thumbnail or File Icon */}
      <div className="flex-shrink-0">
        {isImage && thumbnail ? (
          <img src={thumbnail} alt={file.name} className="w-12 h-12 rounded object-cover" />
        ) : (
          <div className="w-12 h-12 bg-gray-300 rounded flex items-center justify-center">
            <svg
              className="w-6 h-6 text-gray-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"
              />
            </svg>
          </div>
        )}
      </div>

      {/* File Info and Progress */}
      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-gray-900 truncate">{file.name}</p>
            <p className="text-xs text-gray-500">
              {formatFileSize(file.size)}
              {isUploading && progress.total > 0 && (
                <span className="ml-2">
                  {formatFileSize(progress.loaded)} / {formatFileSize(progress.total)}
                </span>
              )}
            </p>
          </div>

          {/* Cancel Button */}
          {isUploading && (
            <button
              type="button"
              onClick={handleCancel}
              className="flex-shrink-0 text-gray-400 hover:text-gray-600"
              aria-label="Cancel upload"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          )}
        </div>

        {/* Progress Bar */}
        {isUploading && (
          <div className="mt-2">
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div
                className="bg-indigo-600 h-2 rounded-full transition-all duration-300"
                style={{ width: `${progress.percentage}%` }}
                role="progressbar"
                aria-valuenow={progress.percentage}
                aria-valuemin={0}
                aria-valuemax={100}
              />
            </div>
            <p className="text-xs text-gray-500 mt-1">{progress.percentage}% uploaded</p>
          </div>
        )}

        {/* Error Message */}
        {error && (
          <div className="mt-2 text-sm text-red-600" role="alert">
            {error}
          </div>
        )}

        {/* Success Message */}
        {!isUploading && !error && (
          <div className="mt-2 text-sm text-green-600 flex items-center">
            <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
            Upload complete
          </div>
        )}
      </div>
    </div>
  );
}

export default FileUpload;
