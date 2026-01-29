import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FileUpload } from './FileUpload';
import { fileUploadService } from '@/services';
import type { UploadResult } from '@/services';

// Mock the fileUploadService
vi.mock('@/services', () => ({
  fileUploadService: {
    uploadFile: vi.fn(),
  },
}));

describe('FileUpload Component', () => {
  const mockFile = new File(['test content'], 'test.txt', { type: 'text/plain' });
  const mockImageFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });

  const mockUploadResult: UploadResult = {
    fileId: 'file-123',
    blobPath: 'files/file-123.txt',
    metadata: {
      id: 'file-123',
      conversationId: 'conv-123',
      uploaderId: 'user-123',
      fileName: 'test.txt',
      mimeType: 'text/plain',
      size: 12,
      blobPath: 'files/file-123.txt',
      createdAt: new Date().toISOString(),
    },
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render file upload component with file info', () => {
    vi.mocked(fileUploadService.uploadFile).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(<FileUpload file={mockFile} conversationId="conv-123" />);

    expect(screen.getByText('test.txt')).toBeInTheDocument();
    expect(screen.getByText(/12 B/)).toBeInTheDocument();
  });

  it('should display progress bar during upload', async () => {
    vi.mocked(fileUploadService.uploadFile).mockImplementation(
      ({ onProgress }) =>
        new Promise((resolve) => {
          setTimeout(() => {
            onProgress?.({ loaded: 50, total: 100, percentage: 50 });
            setTimeout(() => {
              resolve(mockUploadResult);
            }, 100);
          }, 50);
        })
    );

    render(<FileUpload file={mockFile} conversationId="conv-123" />);

    // Progress bar should be visible
    const progressBar = screen.getByRole('progressbar');
    expect(progressBar).toBeInTheDocument();
  });

  it('should call onUploadComplete when upload succeeds', async () => {
    const onUploadComplete = vi.fn();

    vi.mocked(fileUploadService.uploadFile).mockResolvedValue(mockUploadResult);

    render(
      <FileUpload file={mockFile} conversationId="conv-123" onUploadComplete={onUploadComplete} />
    );

    await waitFor(() => {
      expect(onUploadComplete).toHaveBeenCalledWith(mockUploadResult);
    });

    expect(screen.getByText('Upload complete')).toBeInTheDocument();
  });

  it('should display error message when upload fails', async () => {
    const onUploadError = vi.fn();
    const error = new Error('Upload failed');

    vi.mocked(fileUploadService.uploadFile).mockRejectedValue(error);

    render(<FileUpload file={mockFile} conversationId="conv-123" onUploadError={onUploadError} />);

    await waitFor(() => {
      expect(onUploadError).toHaveBeenCalledWith(error);
    });

    expect(screen.getByText('Upload failed')).toBeInTheDocument();
  });

  it('should allow canceling upload', async () => {
    const onCancel = vi.fn();
    const user = userEvent.setup();

    vi.mocked(fileUploadService.uploadFile).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(<FileUpload file={mockFile} conversationId="conv-123" onCancel={onCancel} />);

    const cancelButton = screen.getByLabelText('Cancel upload');
    await user.click(cancelButton);

    expect(onCancel).toHaveBeenCalled();
  });

  it('should display thumbnail for image files', async () => {
    const mockResultWithThumbnail: UploadResult = {
      ...mockUploadResult,
      thumbnailDataUrl: 'data:image/jpeg;base64,mockthumbnail',
    };

    vi.mocked(fileUploadService.uploadFile).mockResolvedValue(mockResultWithThumbnail);

    render(<FileUpload file={mockImageFile} conversationId="conv-123" />);

    await waitFor(() => {
      const thumbnail = screen.getByAltText('test.jpg');
      expect(thumbnail).toBeInTheDocument();
      expect(thumbnail).toHaveAttribute('src', 'data:image/jpeg;base64,mockthumbnail');
    });
  });

  it('should format file sizes correctly', () => {
    const largeFile = new File([new ArrayBuffer(2 * 1024 * 1024)], 'large.pdf', {
      type: 'application/pdf',
    });

    vi.mocked(fileUploadService.uploadFile).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(<FileUpload file={largeFile} conversationId="conv-123" />);

    expect(screen.getByText(/2.0 MB/)).toBeInTheDocument();
  });

  it('should show file icon for non-image files', () => {
    vi.mocked(fileUploadService.uploadFile).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    const { container } = render(<FileUpload file={mockFile} conversationId="conv-123" />);

    // Should have file icon SVG
    const fileIcon = container.querySelector('svg');
    expect(fileIcon).toBeInTheDocument();
  });

  it('should abort upload on unmount', async () => {
    const abortSpy = vi.fn();

    // Create a real AbortController but spy on its abort method
    vi.mocked(fileUploadService.uploadFile).mockImplementation(
      ({ signal }) =>
        new Promise((resolve, reject) => {
          if (signal) {
            signal.addEventListener('abort', () => {
              abortSpy();
              reject(new Error('Upload cancelled'));
            });
          }
        })
    );

    const { unmount } = render(<FileUpload file={mockFile} conversationId="conv-123" />);

    unmount();

    await waitFor(() => {
      expect(abortSpy).toHaveBeenCalled();
    });
  });
});
