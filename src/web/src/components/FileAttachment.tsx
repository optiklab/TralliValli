/**
 * FileAttachment Component
 *
 * Displays file attachments in messages with:
 * - Thumbnail preview for images
 * - Lightbox view for full-size images
 * - Download option
 * - File metadata display
 */

import { useState, useEffect, useRef } from 'react';
import type { FileMetadata } from '@/types/api';
import { formatFileSize } from '@/utils/fileUtils';

export interface FileAttachmentProps {
  file: FileMetadata;
  thumbnail?: string;
  onDownload?: (file: FileMetadata) => void;
}

export function FileAttachment({ file, thumbnail, onDownload }: FileAttachmentProps) {
  const [imageError, setImageError] = useState(false);
  const [showLightbox, setShowLightbox] = useState(false);
  const lightboxRef = useRef<HTMLDivElement>(null);

  const handleDownload = () => {
    onDownload?.(file);
  };

  const handleThumbnailClick = () => {
    if (isImage && thumbnail && !imageError) {
      setShowLightbox(true);
    } else {
      handleDownload();
    }
  };

  const handleThumbnailKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleThumbnailClick();
    }
  };

  const closeLightbox = () => {
    setShowLightbox(false);
  };

  // Handle ESC key to close lightbox
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && showLightbox) {
        closeLightbox();
      }
    };

    if (showLightbox) {
      document.addEventListener('keydown', handleEscape);
      // Prevent body scroll when lightbox is open
      document.body.style.overflow = 'hidden';
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = 'unset';
    };
  }, [showLightbox]);

  const isImage = file.mimeType.startsWith('image/');

  return (
    <>
      <div className="inline-flex items-start gap-3 p-3 bg-white rounded-lg border border-gray-200 max-w-sm">
        {/* Thumbnail or File Icon */}
        <div className="flex-shrink-0">
          {isImage && thumbnail && !imageError ? (
            <div
              role="button"
              tabIndex={0}
              onClick={handleThumbnailClick}
              onKeyDown={handleThumbnailKeyDown}
              className="cursor-pointer hover:opacity-80 focus:outline-none focus:ring-2 focus:ring-indigo-500 rounded"
              aria-label={`View ${file.fileName}`}
            >
              <img
                src={thumbnail}
                alt={file.fileName}
                className="w-16 h-16 rounded object-cover"
                onError={() => setImageError(true)}
              />
            </div>
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

      {/* Lightbox Modal */}
      {showLightbox && isImage && thumbnail && (
        <div
          ref={lightboxRef}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-90"
          onClick={closeLightbox}
          role="dialog"
          aria-modal="true"
          aria-label="Image lightbox"
        >
          {/* Close button */}
          <button
            type="button"
            onClick={closeLightbox}
            className="absolute top-4 right-4 text-white hover:text-gray-300 focus:outline-none focus:ring-2 focus:ring-white rounded-full p-2"
            aria-label="Close lightbox"
          >
            <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>

          {/* Image */}
          <div
            className="max-w-7xl max-h-screen p-4"
            onClick={(e) => e.stopPropagation()}
            role="presentation"
          >
            <img
              src={thumbnail}
              alt={file.fileName}
              className="max-w-full max-h-screen object-contain"
            />
          </div>

          {/* Download button in lightbox */}
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              handleDownload();
            }}
            className="absolute bottom-4 right-4 bg-white text-gray-900 px-4 py-2 rounded-lg hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-white flex items-center gap-2"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
      )}
    </>
  );
}

export default FileAttachment;
