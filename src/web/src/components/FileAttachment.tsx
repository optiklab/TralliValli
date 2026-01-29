/**
 * FileAttachment Component
 *
 * Displays file attachments in messages with:
 * - Thumbnail preview for images
 * - Download option
 * - File metadata display
 */

import { useState } from 'react';
import type { FileMetadata } from '@/types/api';

export interface FileAttachmentProps {
  file: FileMetadata;
  thumbnail?: string;
  onDownload?: (file: FileMetadata) => void;
}

export function FileAttachment({ file, thumbnail, onDownload }: FileAttachmentProps) {
  const [imageError, setImageError] = useState(false);

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const handleDownload = () => {
    onDownload?.(file);
  };

  const isImage = file.mimeType.startsWith('image/');

  return (
    <div className="inline-flex items-start gap-3 p-3 bg-white rounded-lg border border-gray-200 max-w-sm">
      {/* Thumbnail or File Icon */}
      <div className="flex-shrink-0">
        {isImage && thumbnail && !imageError ? (
          <img
            src={thumbnail}
            alt={file.fileName}
            className="w-16 h-16 rounded object-cover cursor-pointer hover:opacity-80"
            onClick={handleDownload}
            onError={() => setImageError(true)}
          />
        ) : (
          <div className="w-16 h-16 bg-gray-100 rounded flex items-center justify-center">
            <svg
              className="w-8 h-8 text-gray-400"
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

      {/* File Info */}
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-gray-900 truncate" title={file.fileName}>
          {file.fileName}
        </p>
        <p className="text-xs text-gray-500 mt-0.5">{formatFileSize(file.size)}</p>

        {/* Download Button */}
        <button
          type="button"
          onClick={handleDownload}
          className="mt-2 inline-flex items-center text-xs font-medium text-indigo-600 hover:text-indigo-800"
        >
          <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
            />
          </svg>
          Download
        </button>
      </div>
    </div>
  );
}

export default FileAttachment;
