import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FileDownloadService } from './fileDownload';
import { FileEncryptionService } from './fileEncryption';
import { apiClient } from './api';
import type { KeyManagementService } from './keyManagement';
import { generateKey } from './aesGcmEncryption';
import type { FileMetadata } from '@/types/api';

// Mock API client
vi.mock('./api', () => ({
  apiClient: {
    getFileMetadata: vi.fn(),
    request: vi.fn(),
  },
}));

// Mock KeyManagementService
const createMockKeyManagementService = (key?: CryptoKey): KeyManagementService => ({
  getConversationKey: vi.fn().mockResolvedValue(key),
  setConversationKey: vi.fn(),
  deleteConversationKey: vi.fn(),
  listConversationKeys: vi.fn(),
  hasConversationKey: vi.fn(),
});

describe('FileDownloadService', () => {
  let mockXHR: {
    open: ReturnType<typeof vi.fn>;
    send: ReturnType<typeof vi.fn>;
    addEventListener: ReturnType<typeof vi.fn>;
    abort: ReturnType<typeof vi.fn>;
    status: number;
    responseType: string;
    response: Blob | null;
  };

  let encryptionKey: CryptoKey;

  beforeEach(async () => {
    vi.clearAllMocks();

    // Generate a test encryption key
    encryptionKey = await generateKey();

    // Mock XMLHttpRequest
    mockXHR = {
      open: vi.fn(),
      send: vi.fn(),
      addEventListener: vi.fn(),
      abort: vi.fn(),
      status: 200,
      responseType: '',
      response: null,
    };

    (global as unknown as { XMLHttpRequest: unknown }).XMLHttpRequest = vi.fn(
      function XMLHttpRequest() {
        return mockXHR;
      }
    );

    // Mock URL.createObjectURL and URL.revokeObjectURL
    global.URL.createObjectURL = vi.fn(() => 'blob:mock-url');
    global.URL.revokeObjectURL = vi.fn();

    // Mock document.createElement for download link
    const mockLink = {
      href: '',
      download: '',
      click: vi.fn(),
    };

    const originalCreateElement = document.createElement.bind(document);
    document.createElement = vi.fn((tagName: string) => {
      if (tagName === 'a') {
        return mockLink as unknown as HTMLAnchorElement;
      }
      return originalCreateElement(tagName);
    });

    // Mock document.body.appendChild and removeChild
    document.body.appendChild = vi.fn();
    document.body.removeChild = vi.fn();
  });

  describe('downloadFile', () => {
    it('should download and decrypt a file successfully', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const encryptionService = new FileEncryptionService(mockKeyManagement);
      const downloadService = new FileDownloadService(encryptionService);

      // Create and encrypt a test file
      const originalContent = 'Test file content for download';
      const originalFile = new File([originalContent], 'test.txt', { type: 'text/plain' });
      const encryptResult = await encryptionService.encryptFile('conv-123', originalFile);

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'test.txt',
        mimeType: 'text/plain',
        size: originalContent.length,
        blobPath: 'files/file-123.txt',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);
      vi.mocked(apiClient.request).mockResolvedValue({
        downloadUrl: 'https://storage.example.com/download',
        fileName: 'test.txt',
      });

      // Mock XHR to return the encrypted blob
      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'load') {
          mockXHR.response = encryptResult.encryptedBlob;
          setTimeout(() => handler(), 0);
        }
      });

      await downloadService.downloadFile({
        fileId: 'file-123',
        conversationId: 'conv-123',
        encryptionMetadata: encryptResult.metadata,
      });

      expect(apiClient.getFileMetadata).toHaveBeenCalledWith('file-123');
      expect(apiClient.request).toHaveBeenCalledWith('/files/file-123/download-url', {
        method: 'GET',
      });
      expect(mockXHR.open).toHaveBeenCalledWith('GET', 'https://storage.example.com/download');
      expect(mockXHR.send).toHaveBeenCalled();
      expect(document.createElement).toHaveBeenCalledWith('a');
    });

    it('should track download progress', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const encryptionService = new FileEncryptionService(mockKeyManagement);
      const downloadService = new FileDownloadService(encryptionService);

      const originalContent = 'Test file content';
      const originalFile = new File([originalContent], 'test.txt', { type: 'text/plain' });
      const encryptResult = await encryptionService.encryptFile('conv-123', originalFile);

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'test.txt',
        mimeType: 'text/plain',
        size: originalContent.length,
        blobPath: 'files/file-123.txt',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);
      vi.mocked(apiClient.request).mockResolvedValue({
        downloadUrl: 'https://storage.example.com/download',
        fileName: 'test.txt',
      });

      const progressUpdates: Array<{ percentage: number }> = [];
      const onProgress = vi.fn((progress) => {
        progressUpdates.push({ percentage: progress.percentage });
      });

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'progress') {
          setTimeout(() => {
            handler({ lengthComputable: true, loaded: 50, total: 100 });
            handler({ lengthComputable: true, loaded: 100, total: 100 });
          }, 0);
        } else if (event === 'load') {
          mockXHR.response = encryptResult.encryptedBlob;
          setTimeout(() => handler(), 10);
        }
      });

      await downloadService.downloadFile({
        fileId: 'file-123',
        conversationId: 'conv-123',
        encryptionMetadata: encryptResult.metadata,
        onProgress,
      });

      expect(onProgress).toHaveBeenCalled();
      expect(progressUpdates.length).toBeGreaterThan(0);
    });

    it('should handle download cancellation', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const encryptionService = new FileEncryptionService(mockKeyManagement);
      const downloadService = new FileDownloadService(encryptionService);

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'test.txt',
        mimeType: 'text/plain',
        size: 100,
        blobPath: 'files/file-123.txt',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);
      vi.mocked(apiClient.request).mockResolvedValue({
        downloadUrl: 'https://storage.example.com/download',
        fileName: 'test.txt',
      });

      const abortController = new AbortController();
      let abortHandler: (() => void) | null = null;

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'abort') {
          abortHandler = handler;
        }
      });

      const downloadPromise = downloadService.downloadFile({
        fileId: 'file-123',
        conversationId: 'conv-123',
        signal: abortController.signal,
      });

      // Cancel the download
      abortController.abort();
      if (abortHandler) {
        abortHandler();
      }

      await expect(downloadPromise).rejects.toThrow('Download was cancelled');
    });

    it('should handle download without encryption metadata', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const encryptionService = new FileEncryptionService(mockKeyManagement);
      const downloadService = new FileDownloadService(encryptionService);

      const fileContent = 'Unencrypted file content';
      const fileBlob = new Blob([fileContent], { type: 'text/plain' });

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'test.txt',
        mimeType: 'text/plain',
        size: fileContent.length,
        blobPath: 'files/file-123.txt',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);
      vi.mocked(apiClient.request).mockResolvedValue({
        downloadUrl: 'https://storage.example.com/download',
        fileName: 'test.txt',
      });

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'load') {
          mockXHR.response = fileBlob;
          setTimeout(() => handler(), 0);
        }
      });

      await downloadService.downloadFile({
        fileId: 'file-123',
        conversationId: 'conv-123',
        // No encryption metadata - should download as-is
      });

      expect(apiClient.getFileMetadata).toHaveBeenCalledWith('file-123');
      expect(document.createElement).toHaveBeenCalledWith('a');
    });

    it('should handle download errors', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const encryptionService = new FileEncryptionService(mockKeyManagement);
      const downloadService = new FileDownloadService(encryptionService);

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'test.txt',
        mimeType: 'text/plain',
        size: 100,
        blobPath: 'files/file-123.txt',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);
      vi.mocked(apiClient.request).mockResolvedValue({
        downloadUrl: 'https://storage.example.com/download',
        fileName: 'test.txt',
      });

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'error') {
          setTimeout(() => handler(), 0);
        }
      });

      await expect(
        downloadService.downloadFile({
          fileId: 'file-123',
          conversationId: 'conv-123',
        })
      ).rejects.toThrow('Download failed due to network error');
    });

    it('should handle decryption errors', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const encryptionService = new FileEncryptionService(mockKeyManagement);
      const downloadService = new FileDownloadService(encryptionService);

      const fileContent = 'Invalid encrypted data';
      const fileBlob = new Blob([fileContent], { type: 'application/octet-stream' });

      const mockFileMetadata: FileMetadata = {
        id: 'file-123',
        conversationId: 'conv-123',
        uploaderId: 'user-123',
        fileName: 'test.txt',
        mimeType: 'text/plain',
        size: fileContent.length,
        blobPath: 'files/file-123.txt',
        createdAt: new Date().toISOString(),
      };

      vi.mocked(apiClient.getFileMetadata).mockResolvedValue(mockFileMetadata);
      vi.mocked(apiClient.request).mockResolvedValue({
        downloadUrl: 'https://storage.example.com/download',
        fileName: 'test.txt',
      });

      mockXHR.addEventListener.mockImplementation((event, handler) => {
        if (event === 'load') {
          mockXHR.response = fileBlob;
          setTimeout(() => handler(), 0);
        }
      });

      // Provide invalid encryption metadata
      const invalidMetadata = {
        iv: 'invalid',
        tag: 'invalid',
        originalSize: 10,
        encryptedSize: 20,
      };

      await expect(
        downloadService.downloadFile({
          fileId: 'file-123',
          conversationId: 'conv-123',
          encryptionMetadata: invalidMetadata,
        })
      ).rejects.toThrow();
    });

    it('should throw error if already aborted', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const encryptionService = new FileEncryptionService(mockKeyManagement);
      const downloadService = new FileDownloadService(encryptionService);

      const abortController = new AbortController();
      abortController.abort();

      await expect(
        downloadService.downloadFile({
          fileId: 'file-123',
          conversationId: 'conv-123',
          signal: abortController.signal,
        })
      ).rejects.toThrow('Download was cancelled');
    });
  });
});
