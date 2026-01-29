import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FileAttachment } from './FileAttachment';
import type { FileMetadata } from '@/types/api';

describe('FileAttachment Component', () => {
  const mockFile: FileMetadata = {
    id: 'file-123',
    conversationId: 'conv-123',
    uploaderId: 'user-123',
    fileName: 'document.pdf',
    mimeType: 'application/pdf',
    size: 1024 * 1024, // 1 MB
    blobPath: 'files/file-123.pdf',
    createdAt: new Date().toISOString(),
  };

  const mockImageFile: FileMetadata = {
    ...mockFile,
    id: 'file-456',
    fileName: 'image.jpg',
    mimeType: 'image/jpeg',
    blobPath: 'files/file-456.jpg',
  };

  it('should render file attachment with file info', () => {
    render(<FileAttachment file={mockFile} />);

    expect(screen.getByText('document.pdf')).toBeInTheDocument();
    expect(screen.getByText('1.0 MB')).toBeInTheDocument();
    expect(screen.getByText('Download')).toBeInTheDocument();
  });

  it('should display file icon for non-image files', () => {
    const { container } = render(<FileAttachment file={mockFile} />);

    // Should have file icon SVG
    const fileIcon = container.querySelector('svg');
    expect(fileIcon).toBeInTheDocument();
  });

  it('should display thumbnail for image files', () => {
    const thumbnail = 'data:image/jpeg;base64,mockthumbnail';

    render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} />);

    const img = screen.getByAltText('image.jpg');
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('src', thumbnail);
  });

  it('should call onDownload when download button is clicked', async () => {
    const onDownload = vi.fn();
    const user = userEvent.setup();

    render(<FileAttachment file={mockFile} onDownload={onDownload} />);

    const downloadButton = screen.getByText('Download');
    await user.click(downloadButton);

    expect(onDownload).toHaveBeenCalledWith(mockFile);
  });

  it('should call onDownload when thumbnail is clicked', async () => {
    const onDownload = vi.fn();
    const user = userEvent.setup();
    const thumbnail = 'data:image/jpeg;base64,mockthumbnail';

    render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} onDownload={onDownload} />);

    const img = screen.getByAltText('image.jpg');
    await user.click(img);

    expect(onDownload).toHaveBeenCalledWith(mockImageFile);
  });

  it('should format file sizes correctly', () => {
    const smallFile: FileMetadata = {
      ...mockFile,
      size: 512, // 512 bytes
    };

    const { rerender } = render(<FileAttachment file={smallFile} />);
    expect(screen.getByText('512 B')).toBeInTheDocument();

    const mediumFile: FileMetadata = {
      ...mockFile,
      size: 512 * 1024, // 512 KB
    };

    rerender(<FileAttachment file={mediumFile} />);
    expect(screen.getByText('512.0 KB')).toBeInTheDocument();

    const largeFile: FileMetadata = {
      ...mockFile,
      size: 5 * 1024 * 1024, // 5 MB
    };

    rerender(<FileAttachment file={largeFile} />);
    expect(screen.getByText('5.0 MB')).toBeInTheDocument();
  });

  it('should handle image load errors gracefully', async () => {
    const thumbnail = 'invalid-url';

    const { container } = render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} />);

    const img = screen.getByAltText('image.jpg');

    // Simulate image load error
    img.dispatchEvent(new Event('error'));

    // Should fallback to file icon
    const fileIcon = container.querySelector('svg');
    expect(fileIcon).toBeInTheDocument();
  });

  it('should truncate long file names', () => {
    const longNameFile: FileMetadata = {
      ...mockFile,
      fileName: 'very-long-file-name-that-should-be-truncated.pdf',
    };

    render(<FileAttachment file={longNameFile} />);

    const fileName = screen.getByText('very-long-file-name-that-should-be-truncated.pdf');
    expect(fileName).toHaveClass('truncate');
  });

  it('should have accessible title attribute for file name', () => {
    render(<FileAttachment file={mockFile} />);

    const fileName = screen.getByTitle('document.pdf');
    expect(fileName).toBeInTheDocument();
  });
});
