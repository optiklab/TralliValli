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

  it('should open lightbox when thumbnail is clicked', async () => {
    const user = userEvent.setup();
    const thumbnail = 'data:image/jpeg;base64,mockthumbnail';

    render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} />);

    const img = screen.getByAltText('image.jpg');
    await user.click(img);

    // Should open lightbox instead of downloading
    expect(screen.getByRole('dialog', { name: 'Image lightbox' })).toBeInTheDocument();
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

  it('should open lightbox when image thumbnail is clicked', async () => {
    const user = userEvent.setup();
    const thumbnail = 'data:image/jpeg;base64,mockthumbnail';

    render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} />);

    const img = screen.getByAltText('image.jpg');
    await user.click(img);

    // Lightbox should be visible
    expect(screen.getByRole('dialog', { name: 'Image lightbox' })).toBeInTheDocument();
    expect(screen.getByLabelText('Close lightbox')).toBeInTheDocument();
  });

  it('should close lightbox when close button is clicked', async () => {
    const user = userEvent.setup();
    const thumbnail = 'data:image/jpeg;base64,mockthumbnail';

    render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} />);

    // Open lightbox
    const img = screen.getByAltText('image.jpg');
    await user.click(img);

    // Close lightbox
    const closeButton = screen.getByLabelText('Close lightbox');
    await user.click(closeButton);

    // Lightbox should be closed
    expect(screen.queryByRole('dialog', { name: 'Image lightbox' })).not.toBeInTheDocument();
  });

  it('should close lightbox when ESC key is pressed', async () => {
    const user = userEvent.setup();
    const thumbnail = 'data:image/jpeg;base64,mockthumbnail';

    render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} />);

    // Open lightbox
    const img = screen.getByAltText('image.jpg');
    await user.click(img);

    // Press ESC key
    await user.keyboard('{Escape}');

    // Lightbox should be closed
    expect(screen.queryByRole('dialog', { name: 'Image lightbox' })).not.toBeInTheDocument();
  });

  it('should close lightbox when clicking on backdrop', async () => {
    const user = userEvent.setup();
    const thumbnail = 'data:image/jpeg;base64,mockthumbnail';

    render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} />);

    // Open lightbox
    const img = screen.getByAltText('image.jpg');
    await user.click(img);

    // Click on backdrop
    const backdrop = screen.getByRole('dialog', { name: 'Image lightbox' });
    await user.click(backdrop);

    // Lightbox should be closed
    expect(screen.queryByRole('dialog', { name: 'Image lightbox' })).not.toBeInTheDocument();
  });

  it('should allow downloading from lightbox', async () => {
    const onDownload = vi.fn();
    const user = userEvent.setup();
    const thumbnail = 'data:image/jpeg;base64,mockthumbnail';

    render(<FileAttachment file={mockImageFile} thumbnail={thumbnail} onDownload={onDownload} />);

    // Open lightbox
    const img = screen.getByAltText('image.jpg');
    await user.click(img);

    // Find and click download button in lightbox (there are 2 download buttons now)
    const downloadButtons = screen.getAllByText('Download');
    const lightboxDownloadButton = downloadButtons[1]; // Second one is in lightbox
    await user.click(lightboxDownloadButton);

    expect(onDownload).toHaveBeenCalledWith(mockImageFile);
  });

  it('should not open lightbox for non-image files', async () => {
    const user = userEvent.setup();

    render(<FileAttachment file={mockFile} />);

    // Click on file icon area - should not open lightbox
    const fileIcon = screen.getByText('Download').closest('div')?.previousSibling;
    if (fileIcon) {
      await user.click(fileIcon as HTMLElement);
    }

    // Lightbox should not appear
    expect(screen.queryByRole('dialog', { name: 'Image lightbox' })).not.toBeInTheDocument();
  });
});
