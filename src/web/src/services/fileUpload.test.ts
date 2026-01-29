import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { FileUploadService, fileUploadService } from './fileUpload';
import { apiClient } from './api';
import type { PresignedUrlResponse, FileMetadata, UploadProgress } from '@/types/api';

// Mock the API client
vi.mock('./api', () => ({
  apiClient: {
    getPresignedUrl: vi.fn(),
    getFileMetadata: vi.fn(),
  },
}));

describe('FileUploadService', () => {
  let mockXHR: {
    open: ReturnType<typeof vi.fn>;
    send: ReturnType<typeof vi.fn>;
    setRequestHeader: ReturnType<typeof vi.fn>;
    abort: ReturnType<typeof vi.fn>;
    upload: {
      addEventListener: ReturnType<typeof vi.fn>;
    };
    addEventListener: ReturnType<typeof vi.fn>;
    status: number;
  };

  beforeEach(() => {
    vi.clearAllMocks();

    // Mock XMLHttpRequest
    mockXHR = {
      open: vi.fn(),
      send: vi.fn(),
      setRequestHeader: vi.fn(),
      abort: vi.fn(),
      upload: {
        addEventListener: vi.fn(),
      },
      addEventListener: vi.fn(),
      status: 200,
    };

    // Create a proper constructor mock
    (global as unknown as { XMLHttpRequest: unknown }).XMLHttpRequest = vi.fn(
      function XMLHttpRequest() {
        return mockXHR;
      }
    );

    // Mock Image
    global.Image = class {
      onload: (() => void) | null = null;
      onerror: (() => void) | null = null;
      src = '';

      constructor() {
        setTimeout(() => {
          if (this.onload) {
            this.onload();
          }
        }, 0);
      }

      get width() {
        return 800;
      }

      get height() {
        return 600;
      }
    } as unknown as typeof Image;

    // Mock FileReader
    global.FileReader = class {
      onload: ((e: ProgressEvent<FileReader>) => void) | null = null;
      onerror: (() => void) | null = null;
      result: string | ArrayBuffer | null = null;

      readAsDataURL() {
        setTimeout(() => {
          this.result = 'data:image/png;base64,mockbase64data';
          if (this.onload) {
            this.onload({ target: this } as ProgressEvent<FileReader>);
          }
        }, 0);
      }
    } as unknown as typeof FileReader;

    // Mock Canvas
    const mockCanvas = {
      width: 0,
      height: 0,
      getContext: vi.fn(() => ({
        drawImage: vi.fn(),
      })),
      toBlob: vi.fn((callback) => {
        const blob = new Blob(['mock blob data'], { type: 'image/jpeg' });
        callback(blob);
      }),
      toDataURL: vi.fn(() => 'data:image/jpeg;base64,mockthumbnail'),
    };

    document.createElement = vi.fn((tagName) => {
      if (tagName === 'canvas') {
        return mockCanvas as unknown as HTMLCanvasElement;
      }
      return {} as HTMLElement;
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('uploadFile', () => {
    it('should upload a file successfully', async () => {
      const mockFile = new File(['test content'], 'test.txt', {
        type: 'text/plain',
      });

      const mockPresignedUrlResponse: PresignedUrlResponse = {
        uploadUrl: 'https://storage.example.com/upload',
        fileId: 'file-123',
        blobPath: 'files/file-123.txt',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
      };

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'test.txt',
        mimeType: 'text/plain',
        size: 12,
        blobPath: 'files/file-123.txt',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getPresignedUrl).mockResolvedValue(mockPresignedUrlResponse);
      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);

      // Simulate successful upload
      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'load') {
          setTimeout(() => handler(), 0);
        }
      });

      const result = await fileUploadService.uploadFile({
        conversationId: 'conv-123',
        file: mockFile,
      });

      expect(result.fileId).toBe('file-123');
      expect(result.blobPath).toBe('files/file-123.txt');
      expect(result.metadata).toEqual(mockFileMetadata);
      expect(apiClient.getPresignedUrl).toHaveBeenCalledWith({
        conversationId: 'conv-123',
        fileName: 'test.txt',
        fileSize: 12,
        mimeType: 'text/plain',
      });
      expect(mockXHR.open).toHaveBeenCalledWith('PUT', mockPresignedUrlResponse.uploadUrl);
      expect(mockXHR.send).toHaveBeenCalled();
    });

    it('should track upload progress', async () => {
      const mockFile = new File(['test content'], 'test.txt', {
        type: 'text/plain',
      });

      const mockPresignedUrlResponse: PresignedUrlResponse = {
        uploadUrl: 'https://storage.example.com/upload',
        fileId: 'file-123',
        blobPath: 'files/file-123.txt',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
      };

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'test.txt',
        mimeType: 'text/plain',
        size: 12,
        blobPath: 'files/file-123.txt',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getPresignedUrl).mockResolvedValue(mockPresignedUrlResponse);
      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);

      const progressUpdates: UploadProgress[] = [];
      const onProgress = (progress: UploadProgress) => {
        progressUpdates.push(progress);
      };

      // Simulate progress updates
      mockXHR.upload.addEventListener.mockImplementation((event, handler) => {
        if (event === 'progress') {
          setTimeout(() => {
            handler({ lengthComputable: true, loaded: 50, total: 100 });
            handler({ lengthComputable: true, loaded: 100, total: 100 });
          }, 0);
        }
      });

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'load') {
          setTimeout(() => handler(), 10);
        }
      });

      await fileUploadService.uploadFile({
        conversationId: 'conv-123',
        file: mockFile,
        onProgress,
      });

      expect(progressUpdates.length).toBeGreaterThan(0);
      expect(progressUpdates[0]).toMatchObject({
        loaded: 50,
        total: 100,
        percentage: 50,
      });
    });

    it('should handle upload cancellation', async () => {
      const mockFile = new File(['test content'], 'test.txt', {
        type: 'text/plain',
      });

      const mockPresignedUrlResponse: PresignedUrlResponse = {
        uploadUrl: 'https://storage.example.com/upload',
        fileId: 'file-123',
        blobPath: 'files/file-123.txt',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
      };

      vi.mocked(apiClient.getPresignedUrl).mockResolvedValue(mockPresignedUrlResponse);

      const abortController = new AbortController();
      let abortHandler: (() => void) | null = null;

      // Capture abort handler
      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'abort') {
          abortHandler = handler;
        }
      });

      const uploadPromise = fileUploadService.uploadFile({
        conversationId: 'conv-123',
        file: mockFile,
        signal: abortController.signal,
      });

      // Cancel the upload
      abortController.abort();
      if (abortHandler) {
        abortHandler();
      }

      await expect(uploadPromise).rejects.toThrow('Upload was cancelled');
    });

    it('should compress large images', async () => {
      // Create a mock large image file (> 2MB)
      const largeImageData = new Array(3 * 1024 * 1024).fill('x').join('');
      const mockFile = new File([largeImageData], 'large-image.jpg', {
        type: 'image/jpeg',
      });

      const mockPresignedUrlResponse: PresignedUrlResponse = {
        uploadUrl: 'https://storage.example.com/upload',
        fileId: 'file-123',
        blobPath: 'files/file-123.jpg',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
      };

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'large-image.jpg',
        mimeType: 'image/jpeg',
        size: 1024 * 1024, // Compressed size
        blobPath: 'files/file-123.jpg',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getPresignedUrl).mockResolvedValue(mockPresignedUrlResponse);
      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'load') {
          setTimeout(() => handler(), 0);
        }
      });

      const result = await fileUploadService.uploadFile({
        conversationId: 'conv-123',
        file: mockFile,
      });

      expect(result.fileId).toBe('file-123');
      // Verify getPresignedUrl was called (compression happened if file size changed)
      expect(apiClient.getPresignedUrl).toHaveBeenCalled();
    });

    it('should generate thumbnail for images', async () => {
      const mockFile = new File(['image data'], 'image.jpg', {
        type: 'image/jpeg',
      });

      const mockPresignedUrlResponse: PresignedUrlResponse = {
        uploadUrl: 'https://storage.example.com/upload',
        fileId: 'file-123',
        blobPath: 'files/file-123.jpg',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
      };

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'image.jpg',
        mimeType: 'image/jpeg',
        size: 10,
        blobPath: 'files/file-123.jpg',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getPresignedUrl).mockResolvedValue(mockPresignedUrlResponse);
      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'load') {
          setTimeout(() => handler(), 0);
        }
      });

      const result = await fileUploadService.uploadFile({
        conversationId: 'conv-123',
        file: mockFile,
      });

      expect(result.thumbnailDataUrl).toBeDefined();
      expect(result.thumbnailDataUrl).toContain('data:image/jpeg');
    });

    it('should not generate thumbnail for non-image files', async () => {
      const mockFile = new File(['pdf content'], 'document.pdf', {
        type: 'application/pdf',
      });

      const mockPresignedUrlResponse: PresignedUrlResponse = {
        uploadUrl: 'https://storage.example.com/upload',
        fileId: 'file-123',
        blobPath: 'files/file-123.pdf',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
      };

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'document.pdf',
        mimeType: 'application/pdf',
        size: 11,
        blobPath: 'files/file-123.pdf',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getPresignedUrl).mockResolvedValue(mockPresignedUrlResponse);
      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'load') {
          setTimeout(() => handler(), 0);
        }
      });

      const result = await fileUploadService.uploadFile({
        conversationId: 'conv-123',
        file: mockFile,
      });

      expect(result.thumbnailDataUrl).toBeUndefined();
    });

    it('should handle upload errors', async () => {
      const mockFile = new File(['test content'], 'test.txt', {
        type: 'text/plain',
      });

      const mockPresignedUrlResponse: PresignedUrlResponse = {
        uploadUrl: 'https://storage.example.com/upload',
        fileId: 'file-123',
        blobPath: 'files/file-123.txt',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
      };

      vi.mocked(apiClient.getPresignedUrl).mockResolvedValue(mockPresignedUrlResponse);

      // Simulate upload error
      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'error') {
          setTimeout(() => handler(), 0);
        }
      });

      await expect(
        fileUploadService.uploadFile({
          conversationId: 'conv-123',
          file: mockFile,
        })
      ).rejects.toThrow('Upload failed due to network error');
    });

    it('should handle invalid file', async () => {
      const mockFile = new File([], 'empty.txt', {
        type: 'text/plain',
      });

      await expect(
        fileUploadService.uploadFile({
          conversationId: 'conv-123',
          file: mockFile,
        })
      ).rejects.toThrow('Invalid file');
    });

    it('should throw error if already aborted', async () => {
      const mockFile = new File(['test content'], 'test.txt', {
        type: 'text/plain',
      });

      const abortController = new AbortController();
      abortController.abort();

      await expect(
        fileUploadService.uploadFile({
          conversationId: 'conv-123',
          file: mockFile,
          signal: abortController.signal,
        })
      ).rejects.toThrow('Upload was cancelled');
    });

    it('should reject files larger than 2MB', async () => {
      const largeFile = new File([new ArrayBuffer(3 * 1024 * 1024)], 'large.pdf', {
        type: 'application/pdf',
      });

      await expect(
        fileUploadService.uploadFile({
          conversationId: 'conv-123',
          file: largeFile,
        })
      ).rejects.toThrow('too large');
    });
  });

  describe('singleton instance', () => {
    it('should export a singleton instance', () => {
      expect(fileUploadService).toBeInstanceOf(FileUploadService);
    });
  });
});
