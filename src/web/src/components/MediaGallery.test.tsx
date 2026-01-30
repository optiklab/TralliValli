import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MediaGallery } from './MediaGallery';
import type { FileMetadata } from '@/types/api';

// Mock IntersectionObserver
class MockIntersectionObserver {
  constructor(
    private callback: IntersectionObserverCallback,
    private options?: IntersectionObserverInit
  ) {}

  observe(target: Element) {
    // Trigger callback immediately for testing
    this.callback(
      [
        {
          isIntersecting: true,
          target,
          intersectionRatio: 1,
          boundingClientRect: {} as DOMRectReadOnly,
          intersectionRect: {} as DOMRectReadOnly,
          rootBounds: null,
          time: Date.now(),
        },
      ],
      this as unknown as IntersectionObserver
    );
  }

  unobserve() {}
  disconnect() {}
  takeRecords() {
    return [];
  }
}

global.IntersectionObserver = MockIntersectionObserver as unknown as typeof IntersectionObserver;

describe('MediaGallery Component', () => {
  const mockImageFile: FileMetadata = {
    id: 'file-1',
    conversationId: 'conv-123',
    uploaderId: 'user-123',
    fileName: 'image.jpg',
    mimeType: 'image/jpeg',
    size: 2048 * 1024, // 2 MB
    blobPath: 'files/image.jpg',
    createdAt: new Date().toISOString(),
  };

  const mockDocumentFile: FileMetadata = {
    id: 'file-2',
    conversationId: 'conv-123',
    uploaderId: 'user-123',
    fileName: 'document.pdf',
    mimeType: 'application/pdf',
    size: 1024 * 1024, // 1 MB
    blobPath: 'files/document.pdf',
    createdAt: new Date().toISOString(),
  };

  const mockFiles: FileMetadata[] = [mockImageFile, mockDocumentFile];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Grid display', () => {
    it('should render files in a grid layout', () => {
      render(<MediaGallery files={mockFiles} />);

      expect(screen.getByRole('list')).toBeInTheDocument();
      expect(screen.getAllByRole('listitem')).toHaveLength(2);
    });

    it('should display file count in filter buttons', () => {
      render(<MediaGallery files={mockFiles} />);

      expect(screen.getByText(/All Files \(2\)/)).toBeInTheDocument();
      expect(screen.getByText(/Images \(1\)/)).toBeInTheDocument();
      expect(screen.getByText(/Documents \(1\)/)).toBeInTheDocument();
    });

    it('should render empty state when no files are available', () => {
      render(<MediaGallery files={[]} />);

      expect(screen.getByRole('heading', { name: /No files/ })).toBeInTheDocument();
      expect(screen.getByText(/No files have been shared/)).toBeInTheDocument();
    });
  });

  describe('Type filtering', () => {
    it('should filter by images when Images filter is selected', async () => {
      const user = userEvent.setup();
      render(<MediaGallery files={mockFiles} />);

      const imagesButton = screen.getByRole('tab', { name: /Images/ });
      await user.click(imagesButton);

      expect(screen.getAllByRole('listitem')).toHaveLength(1);
      expect(screen.getByLabelText(/View image.jpg/)).toBeInTheDocument();
    });

    it('should filter by documents when Documents filter is selected', async () => {
      const user = userEvent.setup();
      render(<MediaGallery files={mockFiles} />);

      const documentsButton = screen.getByRole('tab', { name: /Documents/ });
      await user.click(documentsButton);

      expect(screen.getAllByRole('listitem')).toHaveLength(1);
      expect(screen.getByLabelText(/View document.pdf/)).toBeInTheDocument();
    });

    it('should show all files when All Files filter is selected', async () => {
      const user = userEvent.setup();
      render(<MediaGallery files={mockFiles} />);

      // First click Documents
      await user.click(screen.getByRole('tab', { name: /Documents/ }));
      expect(screen.getAllByRole('listitem')).toHaveLength(1);

      // Then click All Files
      await user.click(screen.getByRole('tab', { name: /All Files/ }));
      expect(screen.getAllByRole('listitem')).toHaveLength(2);
    });

    it('should display correct active filter state', async () => {
      const user = userEvent.setup();
      render(<MediaGallery files={mockFiles} />);

      const allButton = screen.getByRole('tab', { name: /All Files/ });
      const imagesButton = screen.getByRole('tab', { name: /Images/ });

      // Initially All Files should be selected
      expect(allButton).toHaveClass('bg-indigo-600');

      // After clicking Images, it should be selected
      await user.click(imagesButton);
      expect(imagesButton).toHaveClass('bg-indigo-600');
      expect(allButton).not.toHaveClass('bg-indigo-600');
    });

    it('should show empty state when filter has no matching files', async () => {
      const user = userEvent.setup();
      const imageOnlyFiles = [mockImageFile];
      render(<MediaGallery files={imageOnlyFiles} />);

      await user.click(screen.getByRole('tab', { name: /Documents/ }));

      expect(screen.getByRole('heading', { name: /No documents/ })).toBeInTheDocument();
    });
  });

  describe('Lazy loading', () => {
    it('should render image thumbnails when available', () => {
      const getThumbnailUrl = (file: FileMetadata) => {
        if (file.mimeType.startsWith('image/')) {
          return `https://example.com/thumbnails/${file.id}`;
        }
        return undefined;
      };

      const { container } = render(
        <MediaGallery files={[mockImageFile]} getThumbnailUrl={getThumbnailUrl} />
      );

      const img = container.querySelector('img');
      expect(img).toBeInTheDocument();
      expect(img).toHaveAttribute('src', `https://example.com/thumbnails/${mockImageFile.id}`);
    });

    it('should show loading placeholder before image loads', () => {
      const getThumbnailUrl = () => 'https://example.com/thumbnail.jpg';
      const { container } = render(
        <MediaGallery files={[mockImageFile]} getThumbnailUrl={getThumbnailUrl} />
      );

      // Check for loading animation
      const loadingElement = container.querySelector('.animate-pulse');
      expect(loadingElement).toBeInTheDocument();
    });

    it('should show document icon for non-image files', () => {
      const { container } = render(<MediaGallery files={[mockDocumentFile]} />);

      // Should show document icon
      const svg = container.querySelector('svg');
      expect(svg).toBeInTheDocument();

      // Should show file name (multiple instances exist - one in main view, one in hover overlay)
      expect(screen.getAllByText('document.pdf').length).toBeGreaterThan(0);
    });
  });

  describe('Click to view/download', () => {
    it('should call onFileClick when file is clicked', async () => {
      const user = userEvent.setup();
      const onFileClick = vi.fn();

      render(<MediaGallery files={mockFiles} onFileClick={onFileClick} />);

      const fileButton = screen.getByLabelText(/View image.jpg/);
      await user.click(fileButton);

      expect(onFileClick).toHaveBeenCalledWith(mockImageFile);
    });

    it('should call onDownload when download button is clicked', async () => {
      const user = userEvent.setup();
      const onDownload = vi.fn();

      render(<MediaGallery files={mockFiles} onDownload={onDownload} />);

      const fileButton = screen.getByLabelText(/View image.jpg/);

      // Hover to show download button
      await user.hover(fileButton);

      const downloadButton = screen.getByLabelText(/Download image.jpg/);
      await user.click(downloadButton);

      expect(onDownload).toHaveBeenCalledWith(mockImageFile);
    });

    it('should support keyboard navigation for file selection', async () => {
      const user = userEvent.setup();
      const onFileClick = vi.fn();

      render(<MediaGallery files={mockFiles} onFileClick={onFileClick} />);

      const fileButton = screen.getByLabelText(/View image.jpg/);
      fileButton.focus();
      await user.keyboard('{Enter}');

      expect(onFileClick).toHaveBeenCalledWith(mockImageFile);
    });

    it('should support space key for file selection', async () => {
      const user = userEvent.setup();
      const onFileClick = vi.fn();

      render(<MediaGallery files={mockFiles} onFileClick={onFileClick} />);

      const fileButton = screen.getByLabelText(/View image.jpg/);
      fileButton.focus();
      await user.keyboard('{ }');

      expect(onFileClick).toHaveBeenCalledWith(mockImageFile);
    });
  });

  describe('Infinite scroll', () => {
    it('should call onLoadMore when scroll trigger is visible', async () => {
      const onLoadMore = vi.fn().mockResolvedValue(undefined);

      render(<MediaGallery files={mockFiles} hasMore={true} onLoadMore={onLoadMore} />);

      // Wait for IntersectionObserver to trigger
      await waitFor(() => {
        expect(onLoadMore).toHaveBeenCalled();
      });
    });

    it('should show loading indicator while loading more files', async () => {
      const onLoadMore = vi
        .fn()
        .mockImplementation(() => new Promise((resolve) => setTimeout(resolve, 100)));

      render(<MediaGallery files={mockFiles} hasMore={true} onLoadMore={onLoadMore} />);

      await waitFor(() => {
        expect(screen.getByText(/Loading more files/)).toBeInTheDocument();
      });
    });

    it('should not call onLoadMore when hasMore is false', () => {
      const onLoadMore = vi.fn();

      render(<MediaGallery files={mockFiles} hasMore={false} onLoadMore={onLoadMore} />);

      expect(onLoadMore).not.toHaveBeenCalled();
    });

    it('should not call onLoadMore when loading is true', () => {
      const onLoadMore = vi.fn();

      render(
        <MediaGallery files={mockFiles} hasMore={true} loading={true} onLoadMore={onLoadMore} />
      );

      expect(onLoadMore).not.toHaveBeenCalled();
    });

    it('should handle load more errors gracefully', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
      const onLoadMore = vi.fn().mockRejectedValue(new Error('Load failed'));

      render(<MediaGallery files={mockFiles} hasMore={true} onLoadMore={onLoadMore} />);

      await waitFor(() => {
        expect(consoleError).toHaveBeenCalledWith('Failed to load more files:', expect.any(Error));
      });

      consoleError.mockRestore();
    });
  });

  describe('Loading states', () => {
    it('should show loading spinner when loading and no files', () => {
      render(<MediaGallery files={[]} loading={true} />);

      expect(screen.getByText(/Loading files/)).toBeInTheDocument();
    });

    it('should not show loading spinner when loading with existing files', () => {
      render(<MediaGallery files={mockFiles} loading={true} />);

      expect(screen.queryByText(/Loading files/)).not.toBeInTheDocument();
      expect(screen.getAllByRole('listitem')).toHaveLength(2);
    });
  });

  describe('File information display', () => {
    it('should display file name on hover', async () => {
      const user = userEvent.setup();
      render(<MediaGallery files={mockFiles} />);

      const fileButton = screen.getByLabelText(/View image.jpg/);
      await user.hover(fileButton);

      // File name appears in both the main view and the hover overlay
      expect(screen.getAllByText('image.jpg').length).toBeGreaterThan(0);
    });

    it('should display file size on hover', async () => {
      const user = userEvent.setup();
      render(<MediaGallery files={mockFiles} />);

      const fileButton = screen.getByLabelText(/View image.jpg/);
      await user.hover(fileButton);

      expect(screen.getByText('2.0 MB')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels', () => {
      render(<MediaGallery files={mockFiles} />);

      expect(screen.getByRole('tablist', { name: /File type filter/ })).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /All Files/ })).toBeInTheDocument();
      expect(screen.getByRole('tabpanel')).toBeInTheDocument();
    });

    it('should mark active filter tab as selected', async () => {
      const user = userEvent.setup();
      render(<MediaGallery files={mockFiles} />);

      const allTab = screen.getByRole('tab', { name: /All Files/ });
      const imagesTab = screen.getByRole('tab', { name: /Images/ });

      expect(allTab).toHaveAttribute('aria-selected', 'true');
      expect(imagesTab).toHaveAttribute('aria-selected', 'false');

      await user.click(imagesTab);

      expect(imagesTab).toHaveAttribute('aria-selected', 'true');
      expect(allTab).toHaveAttribute('aria-selected', 'false');
    });

    it('should provide accessible labels for file items', () => {
      render(<MediaGallery files={mockFiles} />);

      expect(screen.getByLabelText(/View image.jpg/)).toBeInTheDocument();
      expect(screen.getByLabelText(/View document.pdf/)).toBeInTheDocument();
    });
  });

  describe('Multiple file types', () => {
    it('should handle mixed file types correctly', () => {
      const mixedFiles: FileMetadata[] = [
        mockImageFile,
        { ...mockImageFile, id: 'file-3', fileName: 'photo.png', mimeType: 'image/png' },
        mockDocumentFile,
        {
          ...mockDocumentFile,
          id: 'file-4',
          fileName: 'sheet.xlsx',
          mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        },
      ];

      render(<MediaGallery files={mixedFiles} />);

      expect(screen.getByText(/All Files \(4\)/)).toBeInTheDocument();
      expect(screen.getByText(/Images \(2\)/)).toBeInTheDocument();
      expect(screen.getByText(/Documents \(2\)/)).toBeInTheDocument();
    });
  });

  describe('Edge cases', () => {
    it('should handle empty file list gracefully', () => {
      render(<MediaGallery files={[]} />);

      expect(screen.getByText(/No files have been shared/)).toBeInTheDocument();
    });

    it('should handle undefined callback props gracefully', async () => {
      const user = userEvent.setup();
      render(<MediaGallery files={mockFiles} />);

      const fileButton = screen.getByLabelText(/View image.jpg/);

      // Should not throw error when clicking without callbacks
      await user.click(fileButton);
    });

    it('should handle files with very long names', () => {
      const longNameFile: FileMetadata = {
        ...mockImageFile,
        fileName: 'very-long-file-name-that-should-be-truncated-in-the-ui-display.jpg',
      };

      render(<MediaGallery files={[longNameFile]} />);

      expect(
        screen.getByLabelText(/View very-long-file-name-that-should-be-truncated/)
      ).toBeInTheDocument();
    });
  });
});
