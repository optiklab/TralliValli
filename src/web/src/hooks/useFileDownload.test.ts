import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import type { EncryptedFileMetadata, DownloadProgress } from '@/services';

// Create mock functions
const mockDownloadFile = vi.fn();

// Mock the services before importing the hook
vi.mock('@/services/fileDownload', () => ({
  FileDownloadService: class {
    downloadFile = mockDownloadFile;
  },
}));

vi.mock('@/services/fileEncryption', () => ({
  FileEncryptionService: class {},
}));

vi.mock('@/services/keyManagement', () => ({
  KeyManagementService: class {},
}));

describe('useFileDownload', () => {
  beforeEach(() => {
    mockDownloadFile.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('initial state', () => {
    it('should have correct initial state', async () => {
      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      expect(result.current.isDownloading).toBe(false);
      expect(result.current.progress).toBeNull();
      expect(result.current.error).toBeNull();
      expect(typeof result.current.downloadFile).toBe('function');
    });
  });

  describe('state management', () => {
    it('should set isDownloading to true during download', async () => {
      mockDownloadFile.mockImplementation(() => new Promise((resolve) => setTimeout(resolve, 100)));

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      const downloadPromise = result.current.downloadFile('file-1', 'conv-1');

      // Should be downloading immediately
      await waitFor(() => {
        expect(result.current.isDownloading).toBe(true);
      });

      await downloadPromise;

      // Should be done after completion
      await waitFor(() => {
        expect(result.current.isDownloading).toBe(false);
      });
    });

    it('should update progress during download', async () => {
      mockDownloadFile.mockImplementation(async ({ onProgress }) => {
        const progress: DownloadProgress = {
          loaded: 50,
          total: 100,
          percentage: 50,
        };
        onProgress(progress);
      });

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      await result.current.downloadFile('file-1', 'conv-1');

      await waitFor(() => {
        expect(result.current.progress).toEqual({
          loaded: 50,
          total: 100,
          percentage: 50,
        });
      });
    });

    it('should reset progress and error on new download', async () => {
      // First download with error
      mockDownloadFile.mockRejectedValueOnce(new Error('Failed'));

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      await result.current.downloadFile('file-1', 'conv-1').catch(() => {});

      await waitFor(() => {
        expect(result.current.error).toBeTruthy();
      });

      // Second download should reset error
      mockDownloadFile.mockResolvedValueOnce(undefined);

      await result.current.downloadFile('file-2', 'conv-2');

      await waitFor(() => {
        expect(result.current.error).toBeNull();
        expect(result.current.progress).toBeNull();
      });
    });
  });

  describe('side effects', () => {
    it('should call onProgress callback', async () => {
      const onProgress = vi.fn();

      mockDownloadFile.mockImplementation(async ({ onProgress: progressCb }) => {
        const progress: DownloadProgress = {
          loaded: 75,
          total: 100,
          percentage: 75,
        };
        progressCb(progress);
      });

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload({ onProgress }));

      await result.current.downloadFile('file-1', 'conv-1');

      await waitFor(() => {
        expect(onProgress).toHaveBeenCalledWith({
          loaded: 75,
          total: 100,
          percentage: 75,
        });
      });
    });

    it('should call onSuccess callback on successful download', async () => {
      const onSuccess = vi.fn();
      mockDownloadFile.mockResolvedValue(undefined);

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload({ onSuccess }));

      await result.current.downloadFile('file-1', 'conv-1');

      await waitFor(() => {
        expect(onSuccess).toHaveBeenCalled();
      });
    });

    it('should call onError callback on download failure', async () => {
      const onError = vi.fn();
      const testError = new Error('Download failed');
      mockDownloadFile.mockRejectedValue(testError);

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload({ onError }));

      await result.current.downloadFile('file-1', 'conv-1');

      await waitFor(() => {
        expect(onError).toHaveBeenCalledWith(testError);
      });
    });

    it('should pass encryption metadata to download service', async () => {
      mockDownloadFile.mockResolvedValue(undefined);

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      const encryptionMetadata: EncryptedFileMetadata = {
        encryptionKey: 'test-key',
        iv: 'test-iv',
        algorithm: 'AES-GCM',
      };

      await result.current.downloadFile('file-1', 'conv-1', encryptionMetadata);

      await waitFor(() => {
        expect(mockDownloadFile).toHaveBeenCalledWith(
          expect.objectContaining({
            fileId: 'file-1',
            conversationId: 'conv-1',
            encryptionMetadata,
          })
        );
      });
    });
  });

  describe('error handling', () => {
    it('should set error state on download failure', async () => {
      const testError = new Error('Network error');
      mockDownloadFile.mockRejectedValue(testError);

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      await result.current.downloadFile('file-1', 'conv-1');

      await waitFor(() => {
        expect(result.current.error).toEqual(testError);
      });
    });

    it('should convert non-Error exceptions to Error objects', async () => {
      mockDownloadFile.mockRejectedValue('String error');

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      await result.current.downloadFile('file-1', 'conv-1');

      await waitFor(() => {
        expect(result.current.error).toBeInstanceOf(Error);
        expect(result.current.error?.message).toBe('Download failed');
      });
    });

    it('should set isDownloading to false after error', async () => {
      mockDownloadFile.mockRejectedValue(new Error('Failed'));

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      await result.current.downloadFile('file-1', 'conv-1');

      await waitFor(() => {
        expect(result.current.isDownloading).toBe(false);
      });
    });
  });

  describe('dependencies mocked', () => {
    it('should use mocked FileDownloadService', async () => {
      mockDownloadFile.mockResolvedValue(undefined);

      const { useFileDownload } = await import('./useFileDownload');
      const { result } = renderHook(() => useFileDownload());

      await result.current.downloadFile('file-1', 'conv-1');

      await waitFor(() => {
        expect(mockDownloadFile).toHaveBeenCalled();
      });
    });
  });
});
