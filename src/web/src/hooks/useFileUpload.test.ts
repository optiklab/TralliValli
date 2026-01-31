import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import type { UploadResult, UploadProgress } from '@/types/api';

// Create mock function
const mockUploadFile = vi.fn();

// Mock the services before importing the hook
vi.mock('@/services/fileUpload', () => ({
  fileUploadService: {
    uploadFile: mockUploadFile,
  },
}));

vi.mock('@/services/fileEncryption', () => ({
  FileEncryptionService: class {},
}));

vi.mock('@/services/keyManagement', () => ({
  KeyManagementService: class {},
}));

describe('useFileUpload', () => {
  beforeEach(() => {
    mockUploadFile.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('initial state', () => {
    it('should have correct initial state', async () => {
      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      expect(result.current.isUploading).toBe(false);
      expect(result.current.progress).toBeNull();
      expect(result.current.error).toBeNull();
      expect(result.current.result).toBeNull();
      expect(typeof result.current.uploadFile).toBe('function');
    });
  });

  describe('state management', () => {
    it('should set isUploading to true during upload', async () => {
      mockUploadFile.mockImplementation(
        () => new Promise((resolve) => setTimeout(() => resolve({ fileId: 'file-1' }), 100))
      );

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      const uploadPromise = result.current.uploadFile(file, 'conv-1');

      // Should be uploading immediately
      await waitFor(() => {
        expect(result.current.isUploading).toBe(true);
      });

      await uploadPromise;

      // Should be done after completion
      await waitFor(() => {
        expect(result.current.isUploading).toBe(false);
      });
    });

    it('should update progress during upload', async () => {
      mockUploadFile.mockImplementation(async ({ onProgress }) => {
        const progress: UploadProgress = {
          loaded: 60,
          total: 100,
          percentage: 60,
        };
        onProgress(progress);
        return { fileId: 'file-1', fileName: 'test.txt', fileSize: 100 };
      });

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(result.current.progress).toEqual({
          loaded: 60,
          total: 100,
          percentage: 60,
        });
      });
    });

    it('should store upload result on success', async () => {
      const uploadResult: UploadResult = {
        fileId: 'file-1',
        fileName: 'test.txt',
        fileSize: 1024,
      };
      mockUploadFile.mockResolvedValue(uploadResult);

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(result.current.result).toEqual(uploadResult);
      });
    });

    it('should reset state on new upload', async () => {
      // First upload with error
      mockUploadFile.mockRejectedValueOnce(new Error('Failed'));

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file1 = new File(['test1'], 'test1.txt', { type: 'text/plain' });
      await result.current.uploadFile(file1, 'conv-1');

      await waitFor(() => {
        expect(result.current.error).toBeTruthy();
      });

      // Second upload should reset error and result
      mockUploadFile.mockResolvedValueOnce({ fileId: 'file-2' });

      const file2 = new File(['test2'], 'test2.txt', { type: 'text/plain' });
      await result.current.uploadFile(file2, 'conv-2');

      await waitFor(() => {
        expect(result.current.error).toBeNull();
        expect(result.current.progress).toBeNull();
      });
    });
  });

  describe('side effects', () => {
    it('should call onProgress callback', async () => {
      const onProgress = vi.fn();

      mockUploadFile.mockImplementation(async ({ onProgress: progressCb }) => {
        const progress: UploadProgress = {
          loaded: 80,
          total: 100,
          percentage: 80,
        };
        progressCb(progress);
        return { fileId: 'file-1' };
      });

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload({ onProgress }));

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(onProgress).toHaveBeenCalledWith({
          loaded: 80,
          total: 100,
          percentage: 80,
        });
      });
    });

    it('should call onSuccess callback on successful upload', async () => {
      const onSuccess = vi.fn();
      const uploadResult: UploadResult = {
        fileId: 'file-1',
        fileName: 'test.txt',
        fileSize: 1024,
      };
      mockUploadFile.mockResolvedValue(uploadResult);

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload({ onSuccess }));

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(onSuccess).toHaveBeenCalledWith(uploadResult);
      });
    });

    it('should call onError callback on upload failure', async () => {
      const onError = vi.fn();
      const testError = new Error('Upload failed');
      mockUploadFile.mockRejectedValue(testError);

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload({ onError }));

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(onError).toHaveBeenCalledWith(testError);
      });
    });

    it('should pass encryption service when enabled', async () => {
      mockUploadFile.mockResolvedValue({ fileId: 'file-1' });

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload({ enableEncryption: true }));

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(mockUploadFile).toHaveBeenCalledWith(
          expect.objectContaining({
            conversationId: 'conv-1',
            file,
            encryptionService: expect.any(Object),
          })
        );
      });
    });

    it('should not pass encryption service when disabled', async () => {
      mockUploadFile.mockResolvedValue({ fileId: 'file-1' });

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload({ enableEncryption: false }));

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(mockUploadFile).toHaveBeenCalledWith(
          expect.objectContaining({
            conversationId: 'conv-1',
            file,
            encryptionService: undefined,
          })
        );
      });
    });
  });

  describe('error handling', () => {
    it('should set error state on upload failure', async () => {
      const testError = new Error('Network error');
      mockUploadFile.mockRejectedValue(testError);

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(result.current.error).toEqual(testError);
      });
    });

    it('should convert non-Error exceptions to Error objects', async () => {
      mockUploadFile.mockRejectedValue('String error');

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(result.current.error).toBeInstanceOf(Error);
        expect(result.current.error?.message).toBe('Upload failed');
      });
    });

    it('should return null on upload failure', async () => {
      mockUploadFile.mockRejectedValue(new Error('Failed'));

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      const uploadResult = await result.current.uploadFile(file, 'conv-1');

      expect(uploadResult).toBeNull();
    });

    it('should set isUploading to false after error', async () => {
      mockUploadFile.mockRejectedValue(new Error('Failed'));

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(result.current.isUploading).toBe(false);
      });
    });
  });

  describe('dependencies mocked', () => {
    it('should use mocked fileUploadService', async () => {
      mockUploadFile.mockResolvedValue({ fileId: 'file-1' });

      const { useFileUpload } = await import('./useFileUpload');
      const { result } = renderHook(() => useFileUpload());

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      await result.current.uploadFile(file, 'conv-1');

      await waitFor(() => {
        expect(mockUploadFile).toHaveBeenCalled();
      });
    });
  });
});
