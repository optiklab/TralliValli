/**
 * Tests for MessageEncryptionService
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MessageEncryptionService } from './messageEncryption';
import { KeyManagementService } from './keyManagement';
import { generateKey } from './aesGcmEncryption';

describe('MessageEncryptionService', () => {
  let encryptionService: MessageEncryptionService;
  let keyManagementService: KeyManagementService;
  let conversationKey: CryptoKey;

  beforeEach(async () => {
    // Initialize key management service
    keyManagementService = new KeyManagementService();
    await keyManagementService.initialize();

    // Generate and set master key (extractable for tests)
    const masterKey = await generateKey(true);
    await keyManagementService.setMasterKey(masterKey);

    // Generate and store a conversation key (extractable for storage)
    conversationKey = await generateKey(true);
    await keyManagementService.storeConversationKey('test-conversation', conversationKey);

    // Create encryption service
    encryptionService = new MessageEncryptionService(keyManagementService);
  });

  describe('encryptMessage', () => {
    it('should encrypt a message successfully', async () => {
      const result = await encryptionService.encryptMessage('test-conversation', 'Hello, World!');

      expect(result.success).toBe(true);
      expect(result.encryptedContent).toBeTruthy();
      expect(result.error).toBeUndefined();

      // Verify encrypted content is valid JSON
      const encrypted = JSON.parse(result.encryptedContent);
      expect(encrypted.iv).toBeTruthy();
      expect(encrypted.ciphertext).toBeTruthy();
      expect(encrypted.tag).toBeTruthy();
    });

    it('should return error when conversation key not found', async () => {
      const result = await encryptionService.encryptMessage(
        'nonexistent-conversation',
        'Hello, World!'
      );

      expect(result.success).toBe(false);
      expect(result.encryptedContent).toBe('');
      expect(result.error).toContain('No encryption key found');
    });

    it('should handle empty content', async () => {
      const result = await encryptionService.encryptMessage('test-conversation', '');

      expect(result.success).toBe(true);
      expect(result.encryptedContent).toBeTruthy();
    });

    it('should handle unicode content', async () => {
      const unicodeText = 'Hello 👋 世界 🌍';
      const result = await encryptionService.encryptMessage('test-conversation', unicodeText);

      expect(result.success).toBe(true);
      expect(result.encryptedContent).toBeTruthy();
    });
  });

  describe('decryptMessage', () => {
    it('should decrypt an encrypted message successfully', async () => {
      const originalMessage = 'Hello, World!';

      // Encrypt
      const encryptResult = await encryptionService.encryptMessage(
        'test-conversation',
        originalMessage
      );
      expect(encryptResult.success).toBe(true);

      // Decrypt
      const decryptResult = await encryptionService.decryptMessage(
        'test-conversation',
        encryptResult.encryptedContent
      );

      expect(decryptResult.success).toBe(true);
      expect(decryptResult.content).toBe(originalMessage);
      expect(decryptResult.error).toBeUndefined();
    });

    it('should handle empty encrypted content', async () => {
      const result = await encryptionService.decryptMessage('test-conversation', '');

      expect(result.success).toBe(false);
      expect(result.content).toBe('');
      expect(result.error).toContain('No encrypted content provided');
    });

    it('should return error when conversation key not found', async () => {
      const result = await encryptionService.decryptMessage(
        'nonexistent-conversation',
        '{"iv":"test","ciphertext":"test","tag":"test"}'
      );

      expect(result.success).toBe(false);
      expect(result.content).toBe('');
      expect(result.error).toContain('No decryption key found');
    });

    it('should return error for invalid JSON', async () => {
      const result = await encryptionService.decryptMessage(
        'test-conversation',
        'invalid json'
      );

      expect(result.success).toBe(false);
      expect(result.content).toBe('');
      expect(result.error).toContain('Invalid encrypted content format');
    });

    it('should return error for incomplete encrypted data', async () => {
      const result = await encryptionService.decryptMessage(
        'test-conversation',
        '{"iv":"test"}'
      );

      expect(result.success).toBe(false);
      expect(result.content).toBe('');
      expect(result.error).toContain('Incomplete encrypted data');
    });

    it('should handle unicode content correctly', async () => {
      const unicodeText = 'Hello 👋 世界 🌍';

      // Encrypt
      const encryptResult = await encryptionService.encryptMessage(
        'test-conversation',
        unicodeText
      );

      // Decrypt
      const decryptResult = await encryptionService.decryptMessage(
        'test-conversation',
        encryptResult.encryptedContent
      );

      expect(decryptResult.success).toBe(true);
      expect(decryptResult.content).toBe(unicodeText);
    });

    it('should fail when decrypting with wrong key', async () => {
      // Encrypt with first conversation
      const encryptResult = await encryptionService.encryptMessage(
        'test-conversation',
        'Secret message'
      );

      // Store a different key for another conversation (extractable)
      const otherKey = await generateKey(true);
      await keyManagementService.storeConversationKey('other-conversation', otherKey);

      // Try to decrypt with wrong conversation key
      const decryptResult = await encryptionService.decryptMessage(
        'other-conversation',
        encryptResult.encryptedContent
      );

      expect(decryptResult.success).toBe(false);
      expect(decryptResult.error).toContain('Decryption failed');
    });
  });

  describe('encryptMessageOrFallback', () => {
    it('should return encrypted content on success', async () => {
      const result = await encryptionService.encryptMessageOrFallback(
        'test-conversation',
        'Hello, World!'
      );

      expect(result.success).toBe(true);
      expect(result.encryptedContent).toBeTruthy();
    });

    it('should return plaintext when encryption fails', async () => {
      const plaintext = 'Hello, World!';
      const result = await encryptionService.encryptMessageOrFallback(
        'nonexistent-conversation',
        plaintext
      );

      expect(result.success).toBe(false);
      expect(result.encryptedContent).toBe(plaintext);
      expect(result.error).toBeTruthy();
    });
  });

  describe('decryptMessageOrPlaceholder', () => {
    it('should return decrypted content on success', async () => {
      const originalMessage = 'Hello, World!';
      const encryptResult = await encryptionService.encryptMessage(
        'test-conversation',
        originalMessage
      );

      const decrypted = await encryptionService.decryptMessageOrPlaceholder(
        'test-conversation',
        encryptResult.encryptedContent
      );

      expect(decrypted).toBe(originalMessage);
    });

    it('should return placeholder when decryption fails', async () => {
      const decrypted = await encryptionService.decryptMessageOrPlaceholder(
        'test-conversation',
        'invalid content'
      );

      expect(decrypted).toBe('[Unable to decrypt message]');
    });

    it('should return fallback plaintext when provided and no encrypted content', async () => {
      const fallback = 'Fallback message';
      const decrypted = await encryptionService.decryptMessageOrPlaceholder(
        'test-conversation',
        '',
        fallback
      );

      expect(decrypted).toBe(fallback);
    });

    it('should return decryption failed placeholder when no encrypted content and no fallback', async () => {
      const decrypted = await encryptionService.decryptMessageOrPlaceholder(
        'test-conversation',
        ''
      );

      expect(decrypted).toBe('[Unable to decrypt message]');
    });
  });

  describe('end-to-end encryption flow', () => {
    it('should handle complete encrypt-decrypt cycle', async () => {
      const messages = [
        'Simple message',
        'Message with\nmultiple\nlines',
        'Message with emoji 🎉',
        'Message with special chars: !@#$%^&*()',
        'Very long message: ' + 'a'.repeat(1000),
      ];

      for (const msg of messages) {
        const encrypted = await encryptionService.encryptMessage('test-conversation', msg);
        expect(encrypted.success).toBe(true);

        const decrypted = await encryptionService.decryptMessage(
          'test-conversation',
          encrypted.encryptedContent
        );
        expect(decrypted.success).toBe(true);
        expect(decrypted.content).toBe(msg);
      }
    });
  });
});
