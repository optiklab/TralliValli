/**
 * MediaGallery Component
 *
 * Displays all files in a conversation as a grid with:
 * - Filter by type (images, documents, all)
 * - Lazy load thumbnails
 * - Click to view/download
 * - Infinite scroll for pagination
 */

import { useState, useRef, useEffect } from 'react';
import type { FileMetadata } from '@/types/api';
import { formatFileSize } from '@/utils/fileUtils';

export type FileFilter = 'all' | 'images' | 'documents';

export interface MediaGalleryProps {
  files: FileMetadata[];
  onFileClick?: (file: FileMetadata) => void;
  onDownload?: (file: FileMetadata) => void;
  onLoadMore?: () => Promise<void>;
  hasMore?: boolean;
  getThumbnailUrl?: (file: FileMetadata) => string | undefined;
  loading?: boolean;
}

interface MediaItemProps {
  file: FileMetadata;
  thumbnailUrl?: string;
  onFileClick?: (file: FileMetadata) => void;
  onDownload?: (file: FileMetadata) => void;
}

function MediaItem({ file, thumbnailUrl, onFileClick, onDownload }: MediaItemProps) {
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);
  const [isInView, setIsInView] = useState(false);
  const itemRef = useRef<HTMLDivElement>(null);

  const isImage = file.mimeType.startsWith('image/');

  // Intersection Observer for lazy loading
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setIsInView(true);
            observer.disconnect();
          }
        });
      },
      {
        rootMargin: '50px',
      }
    );

    if (itemRef.current) {
      observer.observe(itemRef.current);
    }

    return () => {
      observer.disconnect();
    };
  }, []);

  const handleClick = () => {
    onFileClick?.(file);
  };

  const handleDownload = (e: React.MouseEvent) => {
    e.stopPropagation();
    onDownload?.(file);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleClick();
    }
  };

  const renderThumbnail = () => {
    if (isImage && thumbnailUrl && isInView) {
      return (
        <div className="relative w-full h-full">
          {!imageLoaded && (
            <div className="absolute inset-0 flex items-center justify-center bg-gray-100">
              <div className="animate-pulse rounded-full h-8 w-8 bg-gray-300" />
            </div>
          )}
          <img
            src={thumbnailUrl}
            alt={file.fileName}
            className={`w-full h-full object-cover transition-opacity duration-200 ${
              imageLoaded ? 'opacity-100' : 'opacity-0'
            }`}
            onLoad={() => setImageLoaded(true)}
            onError={() => {
              setImageError(true);
              setImageLoaded(true);
            }}
          />
          {imageError && (
            <div className="absolute inset-0 flex items-center justify-center bg-gray-100">
              <svg
                className="w-12 h-12 text-gray-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
                />
              </svg>
            </div>
          )}
        </div>
      );
    }

    // Show document icon for non-images or when not in view
    return (
      <div className="flex flex-col items-center justify-center w-full h-full bg-gray-100">
        <svg
          className="w-12 h-12 text-gray-400 mb-2"
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
        <span className="text-xs text-gray-600 text-center px-2 truncate max-w-full">
          {file.fileName}
        </span>
      </div>
    );
  };

  return (
    <div
      ref={itemRef}
      role="button"
      tabIndex={0}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      className="relative aspect-square rounded-lg overflow-hidden border border-gray-200 hover:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer group transition-all bg-white"
      aria-label={`View ${file.fileName}`}
    >
      {renderThumbnail()}

      {/* Overlay with file info on hover */}
      <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-60 transition-all duration-200 flex flex-col justify-end p-3 opacity-0 group-hover:opacity-100">
        <div className="text-white">
          <p className="text-sm font-medium truncate" title={file.fileName}>
            {file.fileName}
          </p>
          <p className="text-xs opacity-90">{formatFileSize(file.size)}</p>
        </div>
        <button
          type="button"
          onClick={handleDownload}
          className="mt-2 inline-flex items-center justify-center px-3 py-1.5 bg-white text-gray-900 rounded text-xs font-medium hover:bg-gray-100 transition-colors"
          aria-label={`Download ${file.fileName}`}
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

export function MediaGallery({
  files,
  onFileClick,
  onDownload,
  onLoadMore,
  hasMore = false,
  getThumbnailUrl,
  loading = false,
}: MediaGalleryProps) {
  const [filter, setFilter] = useState<FileFilter>('all');
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const loadMoreTriggerRef = useRef<HTMLDivElement>(null);
  const isLoadingMoreRef = useRef(false);

  // Keep ref in sync with state
  useEffect(() => {
    isLoadingMoreRef.current = isLoadingMore;
  }, [isLoadingMore]);

  // Filter files based on selected type
  const filteredFiles = files.filter((file) => {
    if (filter === 'all') return true;
    if (filter === 'images') return file.mimeType.startsWith('image/');
    if (filter === 'documents') return !file.mimeType.startsWith('image/');
    return true;
  });

  // Intersection Observer for infinite scroll
  useEffect(() => {
    if (!hasMore || !onLoadMore || loading) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const target = entries[0];
        if (target.isIntersecting && !isLoadingMoreRef.current) {
          setIsLoadingMore(true);
          onLoadMore()
            .then(() => {
              setIsLoadingMore(false);
            })
            .catch((error) => {
              setIsLoadingMore(false);
              console.error('Failed to load more files:', error);
            });
        }
      },
      {
        root: scrollContainerRef.current,
        rootMargin: '100px',
      }
    );

    if (loadMoreTriggerRef.current) {
      observer.observe(loadMoreTriggerRef.current);
    }

    return () => {
      observer.disconnect();
    };
  }, [hasMore, onLoadMore, loading]);

  const getFilterLabel = (filterType: FileFilter): string => {
    switch (filterType) {
      case 'all':
        return 'All Files';
      case 'images':
        return 'Images';
      case 'documents':
        return 'Documents';
      default:
        return 'All Files';
    }
  };

  const getFilterCount = (filterType: FileFilter): number => {
    if (filterType === 'all') return files.length;
    if (filterType === 'images') return files.filter((f) => f.mimeType.startsWith('image/')).length;
    if (filterType === 'documents')
      return files.filter((f) => !f.mimeType.startsWith('image/')).length;
    return files.length;
  };

  return (
    <div className="flex flex-col h-full bg-white">
      {/* Header with filters */}
      <div className="flex-shrink-0 border-b border-gray-200 p-4">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Media Gallery</h2>
        <div className="flex gap-2" role="tablist" aria-label="File type filter">
          {(['all', 'images', 'documents'] as FileFilter[]).map((filterType) => (
            <button
              key={filterType}
              type="button"
              role="tab"
              aria-selected={filter === filterType}
              aria-controls="media-gallery-grid"
              onClick={() => setFilter(filterType)}
              className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                filter === filterType
                  ? 'bg-indigo-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              {getFilterLabel(filterType)} ({getFilterCount(filterType)})
            </button>
          ))}
        </div>
      </div>

      {/* Grid container */}
      <div
        ref={scrollContainerRef}
        className="flex-1 overflow-y-auto p-4"
        id="media-gallery-grid"
        role="tabpanel"
      >
        {/* Loading state */}
        {loading && filteredFiles.length === 0 && (
          <div className="flex items-center justify-center h-64">
            <div className="flex flex-col items-center gap-3">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600" />
              <p className="text-sm text-gray-500">Loading files...</p>
            </div>
          </div>
        )}

        {/* Empty state */}
        {!loading && filteredFiles.length === 0 && (
          <div className="flex flex-col items-center justify-center h-64 text-center">
            <svg
              className="w-16 h-16 text-gray-400 mb-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
              />
            </svg>
            <h3 className="text-lg font-medium text-gray-900 mb-1">
              No {filter === 'all' ? 'files' : filter === 'images' ? 'images' : 'documents'}
            </h3>
            <p className="text-sm text-gray-500">
              {filter === 'all'
                ? 'No files have been shared in this conversation yet.'
                : `No ${filter === 'images' ? 'images' : 'documents'} have been shared in this conversation yet.`}
            </p>
          </div>
        )}

        {/* Files grid */}
        {filteredFiles.length > 0 && (
          <div
            className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
            role="list"
          >
            {filteredFiles.map((file) => (
              <div key={file.id} role="listitem">
                <MediaItem
                  file={file}
                  thumbnailUrl={getThumbnailUrl?.(file)}
                  onFileClick={onFileClick}
                  onDownload={onDownload}
                />
              </div>
            ))}
          </div>
        )}

        {/* Infinite scroll trigger */}
        {hasMore && filteredFiles.length > 0 && (
          <div ref={loadMoreTriggerRef} className="flex justify-center py-8">
            {isLoadingMore && (
              <div className="flex flex-col items-center gap-2">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
                <p className="text-sm text-gray-500">Loading more files...</p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default MediaGallery;
