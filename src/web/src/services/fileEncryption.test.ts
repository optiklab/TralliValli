import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FileEncryptionService } from './fileEncryption';
import type { KeyManagementService } from './keyManagement';
import { generateKey } from './aesGcmEncryption';

// Mock KeyManagementService
const createMockKeyManagementService = (key?: CryptoKey): KeyManagementService => ({
  getConversationKey: vi.fn().mockResolvedValue(key),
  setConversationKey: vi.fn(),
  deleteConversationKey: vi.fn(),
  listConversationKeys: vi.fn(),
  hasConversationKey: vi.fn(),
});

describe('FileEncryptionService', () => {
  let encryptionKey: CryptoKey;

  beforeEach(async () => {
    // Generate a test encryption key
    encryptionKey = await generateKey();
  });

  describe('encryptFile', () => {
    it('should encrypt a small file successfully', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      const fileContent = 'Hello, World! This is a test file.';
      const file = new File([fileContent], 'test.txt', { type: 'text/plain' });

      const result = await service.encryptFile('conv-123', file);

      expect(result.success).toBe(true);
      expect(result.error).toBeUndefined();
      expect(result.encryptedBlob).toBeInstanceOf(Blob);
      expect(result.encryptedBlob.size).toBeGreaterThan(0);
      expect(result.metadata.iv).toBeTruthy();
      expect(result.metadata.tag).toBeTruthy();
      expect(result.metadata.originalSize).toBe(fileContent.length);
      expect(result.metadata.encryptedSize).toBeGreaterThan(0);
    });

    it('should encrypt a larger file successfully', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      // Create a 100KB file
      const fileContent = new Uint8Array(100 * 1024);
      for (let i = 0; i < fileContent.length; i++) {
        fileContent[i] = i % 256;
      }
      const file = new File([fileContent], 'large.bin', { type: 'application/octet-stream' });

      const result = await service.encryptFile('conv-123', file);

      expect(result.success).toBe(true);
      expect(result.error).toBeUndefined();
      expect(result.encryptedBlob).toBeInstanceOf(Blob);
      expect(result.metadata.originalSize).toBe(fileContent.length);
    });

    it('should track progress during encryption', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      const fileContent = 'Test file content';
      const file = new File([fileContent], 'test.txt', { type: 'text/plain' });

      const progressUpdates: Array<{ percentage: number }> = [];
      const onProgress = vi.fn((progress) => {
        progressUpdates.push({ percentage: progress.percentage });
      });

      const result = await service.encryptFile('conv-123', file, onProgress);

      expect(result.success).toBe(true);
      expect(onProgress).toHaveBeenCalled();
      expect(progressUpdates.length).toBeGreaterThan(0);
      expect(progressUpdates[progressUpdates.length - 1].percentage).toBe(100);
    });

    it('should return error when no encryption key is found', async () => {
      const mockKeyManagement = createMockKeyManagementService(undefined);
      const service = new FileEncryptionService(mockKeyManagement);

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });

      const result = await service.encryptFile('conv-123', file);

      expect(result.success).toBe(false);
      expect(result.error).toContain('No encryption key found');
    });

    it('should handle encryption errors gracefully', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      // Mock a key that will fail encryption
      const badKey = {} as CryptoKey;
      mockKeyManagement.getConversationKey = vi.fn().mockResolvedValue(badKey);

      const service = new FileEncryptionService(mockKeyManagement);

      const file = new File(['test'], 'test.txt', { type: 'text/plain' });

      const result = await service.encryptFile('conv-123', file);

      expect(result.success).toBe(false);
      expect(result.error).toBeTruthy();
    });
  });

  describe('decryptFile', () => {
    it('should decrypt a file successfully', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      const originalContent = 'Hello, World! This is a test file.';
      const originalFile = new File([originalContent], 'test.txt', { type: 'text/plain' });

      // First encrypt the file
      const encryptResult = await service.encryptFile('conv-123', originalFile);
      expect(encryptResult.success).toBe(true);

      // Then decrypt it
      const decryptResult = await service.decryptFile(
        'conv-123',
        encryptResult.encryptedBlob,
        encryptResult.metadata
      );

      expect(decryptResult.success).toBe(true);
      expect(decryptResult.error).toBeUndefined();
      expect(decryptResult.decryptedBlob).toBeInstanceOf(Blob);
      expect(decryptResult.decryptedBlob.size).toBe(originalContent.length);

      // Verify the decrypted content matches the original
      // Use FileReader since happy-dom doesn't support Blob.text()
      const decryptedText = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as string);
        reader.onerror = () => reject(new Error('Failed to read blob'));
        reader.readAsText(decryptResult.decryptedBlob);
      });
      expect(decryptedText).toBe(originalContent);
    });

    it('should decrypt a larger file successfully', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      // Create a 50KB file
      const originalContent = new Uint8Array(50 * 1024);
      for (let i = 0; i < originalContent.length; i++) {
        originalContent[i] = i % 256;
      }
      const originalFile = new File([originalContent], 'large.bin', {
        type: 'application/octet-stream',
      });

      // Encrypt the file
      const encryptResult = await service.encryptFile('conv-123', originalFile);
      expect(encryptResult.success).toBe(true);

      // Decrypt the file
      const decryptResult = await service.decryptFile(
        'conv-123',
        encryptResult.encryptedBlob,
        encryptResult.metadata
      );

      expect(decryptResult.success).toBe(true);
      expect(decryptResult.decryptedBlob.size).toBe(originalContent.length);

      // Verify the decrypted content matches the original
      // Use FileReader since happy-dom doesn't support Blob.arrayBuffer()
      const decryptedBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as ArrayBuffer);
        reader.onerror = () => reject(new Error('Failed to read blob'));
        reader.readAsArrayBuffer(decryptResult.decryptedBlob);
      });
      const decryptedContent = new Uint8Array(decryptedBuffer);
      expect(decryptedContent).toEqual(originalContent);
    });

    it('should track progress during decryption', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      const originalContent = 'Test file content for decryption';
      const originalFile = new File([originalContent], 'test.txt', { type: 'text/plain' });

      // Encrypt the file
      const encryptResult = await service.encryptFile('conv-123', originalFile);

      // Decrypt with progress tracking
      const progressUpdates: Array<{ percentage: number }> = [];
      const onProgress = vi.fn((progress) => {
        progressUpdates.push({ percentage: progress.percentage });
      });

      const decryptResult = await service.decryptFile(
        'conv-123',
        encryptResult.encryptedBlob,
        encryptResult.metadata,
        onProgress
      );

      expect(decryptResult.success).toBe(true);
      expect(onProgress).toHaveBeenCalled();
      expect(progressUpdates.length).toBeGreaterThan(0);
      expect(progressUpdates[progressUpdates.length - 1].percentage).toBe(100);
    });

    it('should return error when no decryption key is found', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      const originalFile = new File(['test'], 'test.txt', { type: 'text/plain' });
      const encryptResult = await service.encryptFile('conv-123', originalFile);

      // Use a different key management service with no key
      const mockKeyManagementNoKey = createMockKeyManagementService(undefined);
      const serviceNoKey = new FileEncryptionService(mockKeyManagementNoKey);

      const decryptResult = await serviceNoKey.decryptFile(
        'conv-123',
        encryptResult.encryptedBlob,
        encryptResult.metadata
      );

      expect(decryptResult.success).toBe(false);
      expect(decryptResult.error).toContain('No decryption key found');
    });

    it('should fail to decrypt with wrong key', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      const originalFile = new File(['test'], 'test.txt', { type: 'text/plain' });
      const encryptResult = await service.encryptFile('conv-123', originalFile);

      // Use a different key for decryption
      const wrongKey = await generateKey();
      const mockKeyManagementWrongKey = createMockKeyManagementService(wrongKey);
      const serviceWrongKey = new FileEncryptionService(mockKeyManagementWrongKey);

      const decryptResult = await serviceWrongKey.decryptFile(
        'conv-123',
        encryptResult.encryptedBlob,
        encryptResult.metadata
      );

      expect(decryptResult.success).toBe(false);
      expect(decryptResult.error).toBeTruthy();
    });

    it('should handle decryption errors gracefully', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      // Create invalid encrypted blob and metadata
      const invalidBlob = new Blob(['invalid encrypted data']);
      const invalidMetadata = {
        iv: 'invalid',
        tag: 'invalid',
        originalSize: 10,
        encryptedSize: 20,
      };

      const decryptResult = await service.decryptFile('conv-123', invalidBlob, invalidMetadata);

      expect(decryptResult.success).toBe(false);
      expect(decryptResult.error).toBeTruthy();
    });
  });

  describe('round-trip encryption and decryption', () => {
    it('should correctly encrypt and decrypt various file types', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      const testCases = [
        { content: 'Plain text file', type: 'text/plain', name: 'test.txt' },
        { content: '{"key": "value"}', type: 'application/json', name: 'data.json' },
        {
          content: new Uint8Array([1, 2, 3, 4, 5]),
          type: 'application/octet-stream',
          name: 'binary.bin',
        },
      ];

      for (const testCase of testCases) {
        const originalFile = new File([testCase.content], testCase.name, {
          type: testCase.type,
        });

        // Encrypt
        const encryptResult = await service.encryptFile('conv-123', originalFile);
        expect(encryptResult.success).toBe(true);

        // Decrypt
        const decryptResult = await service.decryptFile(
          'conv-123',
          encryptResult.encryptedBlob,
          encryptResult.metadata
        );
        expect(decryptResult.success).toBe(true);

        // Verify content
        if (testCase.content instanceof Uint8Array) {
          const decryptedBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result as ArrayBuffer);
            reader.onerror = () => reject(new Error('Failed to read blob'));
            reader.readAsArrayBuffer(decryptResult.decryptedBlob);
          });
          const decryptedContent = new Uint8Array(decryptedBuffer);
          expect(decryptedContent).toEqual(testCase.content);
        } else {
          const decryptedText = await new Promise<string>((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result as string);
            reader.onerror = () => reject(new Error('Failed to read blob'));
            reader.readAsText(decryptResult.decryptedBlob);
          });
          expect(decryptedText).toBe(testCase.content);
        }
      }
    });

    it('should produce different encrypted output for the same input', async () => {
      const mockKeyManagement = createMockKeyManagementService(encryptionKey);
      const service = new FileEncryptionService(mockKeyManagement);

      const content = 'Same content, different encryption';
      const file1 = new File([content], 'test1.txt', { type: 'text/plain' });
      const file2 = new File([content], 'test2.txt', { type: 'text/plain' });

      const result1 = await service.encryptFile('conv-123', file1);
      const result2 = await service.encryptFile('conv-123', file2);

      expect(result1.success).toBe(true);
      expect(result2.success).toBe(true);

      // IV should be different (random)
      expect(result1.metadata.iv).not.toBe(result2.metadata.iv);

      // But both should decrypt to the same content
      const decrypt1 = await service.decryptFile(
        'conv-123',
        result1.encryptedBlob,
        result1.metadata
      );
      const decrypt2 = await service.decryptFile(
        'conv-123',
        result2.encryptedBlob,
        result2.metadata
      );

      const text1 = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as string);
        reader.onerror = () => reject(new Error('Failed to read blob'));
        reader.readAsText(decrypt1.decryptedBlob);
      });
      const text2 = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as string);
        reader.onerror = () => reject(new Error('Failed to read blob'));
        reader.readAsText(decrypt2.decryptedBlob);
      });

      expect(text1).toBe(content);
      expect(text2).toBe(content);
    });
  });
});
