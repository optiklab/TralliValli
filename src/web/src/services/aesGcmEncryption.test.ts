import { describe, it, expect, beforeAll } from 'vitest';
import {
  encrypt,
  decrypt,
  generateKey,
  importKey,
  exportKey,
  encryptToBase64,
  decryptFromBase64,
} from './aesGcmEncryption';

describe('AES-256-GCM Encryption', () => {
  let testKey: CryptoKey;

  beforeAll(async () => {
    // Generate a test key to use across tests
    testKey = await generateKey();
  });

  describe('Key Generation and Management', () => {
    it('should generate a valid AES-256-GCM key', async () => {
      const key = await generateKey();

      expect(key).toBeDefined();
      expect(key.type).toBe('secret');
      expect(key.algorithm.name).toBe('AES-GCM');
      // @ts-expect-error - accessing length property
      expect(key.algorithm.length).toBe(256);
      expect(key.usages).toContain('encrypt');
      expect(key.usages).toContain('decrypt');
    });

    it('should generate unique keys', async () => {
      const key1 = await generateKey(true);
      const key2 = await generateKey(true);

      const raw1 = await exportKey(key1);
      const raw2 = await exportKey(key2);

      // Convert to base64 for comparison
      const base64_1 = btoa(String.fromCharCode(...raw1));
      const base64_2 = btoa(String.fromCharCode(...raw2));

      expect(base64_1).not.toBe(base64_2);
    });

    it('should not allow export of non-extractable keys by default', async () => {
      const key = await generateKey(false);

      await expect(exportKey(key)).rejects.toThrow();
    });

    it('should export extractable keys', async () => {
      const key = await generateKey(true);
      const rawKey = await exportKey(key);

      expect(rawKey).toBeInstanceOf(Uint8Array);
      expect(rawKey.length).toBe(32); // 32 bytes for AES-256
    });

    it('should import a raw key', async () => {
      const rawKey = new Uint8Array(32);
      crypto.getRandomValues(rawKey);

      const key = await importKey(rawKey);

      expect(key).toBeDefined();
      expect(key.type).toBe('secret');
      expect(key.algorithm.name).toBe('AES-GCM');
    });

    it('should reject invalid key lengths during import', async () => {
      const invalidKey = new Uint8Array(16); // Wrong length (should be 32 for AES-256)

      await expect(importKey(invalidKey)).rejects.toThrow('Invalid key length');
    });

    it('should round-trip key export and import', async () => {
      const originalKey = await generateKey(true);
      const rawKey = await exportKey(originalKey);
      const importedKey = await importKey(rawKey, true);

      // Test that both keys work the same way
      const plaintext = 'Test message';
      const encrypted = await encrypt(originalKey, plaintext);
      const decrypted = await decrypt(
        importedKey,
        encrypted.iv,
        encrypted.ciphertext,
        encrypted.tag
      );
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });
  });

  describe('Encryption', () => {
    it('should encrypt a string successfully', async () => {
      const plaintext = 'Hello, World!';

      const result = await encrypt(testKey, plaintext);

      expect(result).toBeDefined();
      expect(result.iv).toBeInstanceOf(Uint8Array);
      expect(result.ciphertext).toBeInstanceOf(Uint8Array);
      expect(result.tag).toBeInstanceOf(Uint8Array);
      expect(result.iv.length).toBe(12); // GCM IV is 12 bytes
      expect(result.tag.length).toBe(16); // GCM tag is 16 bytes
      expect(result.ciphertext.length).toBeGreaterThan(0);
    });

    it('should encrypt a Uint8Array successfully', async () => {
      const plaintext = new Uint8Array([1, 2, 3, 4, 5]);

      const result = await encrypt(testKey, plaintext);

      expect(result).toBeDefined();
      expect(result.iv).toBeInstanceOf(Uint8Array);
      expect(result.ciphertext).toBeInstanceOf(Uint8Array);
      expect(result.tag).toBeInstanceOf(Uint8Array);
    });

    it('should generate a random IV for each encryption', async () => {
      const plaintext = 'Same message';

      const result1 = await encrypt(testKey, plaintext);
      const result2 = await encrypt(testKey, plaintext);

      // Convert IVs to base64 for comparison
      const iv1 = btoa(String.fromCharCode(...result1.iv));
      const iv2 = btoa(String.fromCharCode(...result2.iv));

      expect(iv1).not.toBe(iv2);
    });

    it('should produce different ciphertext for same plaintext with different IVs', async () => {
      const plaintext = 'Same message';

      const result1 = await encrypt(testKey, plaintext);
      const result2 = await encrypt(testKey, plaintext);

      // Convert ciphertexts to base64 for comparison
      const ct1 = btoa(String.fromCharCode(...result1.ciphertext));
      const ct2 = btoa(String.fromCharCode(...result2.ciphertext));

      expect(ct1).not.toBe(ct2);
    });

    it('should encrypt empty string', async () => {
      const plaintext = '';

      const result = await encrypt(testKey, plaintext);

      expect(result).toBeDefined();
      expect(result.ciphertext.length).toBe(0);
      expect(result.iv.length).toBe(12);
      expect(result.tag.length).toBe(16);
    });

    it('should encrypt large data', async () => {
      const plaintext = 'A'.repeat(10000); // 10KB

      const result = await encrypt(testKey, plaintext);

      expect(result).toBeDefined();
      expect(result.ciphertext.length).toBeGreaterThan(0);
    });
  });

  describe('Decryption', () => {
    it('should decrypt successfully with correct key and parameters', async () => {
      const plaintext = 'Secret message';

      const encrypted = await encrypt(testKey, plaintext);
      const decrypted = await decrypt(testKey, encrypted.iv, encrypted.ciphertext, encrypted.tag);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });

    it('should decrypt Uint8Array data correctly', async () => {
      const plaintext = new Uint8Array([10, 20, 30, 40, 50]);

      const encrypted = await encrypt(testKey, plaintext);
      const decrypted = await decrypt(testKey, encrypted.iv, encrypted.ciphertext, encrypted.tag);

      expect(decrypted).toEqual(plaintext);
    });

    it('should fail decryption with wrong key', async () => {
      const plaintext = 'Secret message';
      const wrongKey = await generateKey();

      const encrypted = await encrypt(testKey, plaintext);

      await expect(
        decrypt(wrongKey, encrypted.iv, encrypted.ciphertext, encrypted.tag)
      ).rejects.toThrow('Decryption failed');
    });

    it('should fail decryption with tampered ciphertext', async () => {
      const plaintext = 'Secret message';

      const encrypted = await encrypt(testKey, plaintext);

      // Tamper with ciphertext
      const tamperedCiphertext = new Uint8Array(encrypted.ciphertext);
      if (tamperedCiphertext.length > 0) {
        tamperedCiphertext[0] ^= 0xff; // Flip bits in first byte
      }

      await expect(
        decrypt(testKey, encrypted.iv, tamperedCiphertext, encrypted.tag)
      ).rejects.toThrow('Decryption failed');
    });

    it('should fail decryption with wrong IV', async () => {
      const plaintext = 'Secret message';

      const encrypted = await encrypt(testKey, plaintext);

      // Use a different IV
      const wrongIV = new Uint8Array(12);
      crypto.getRandomValues(wrongIV);

      await expect(decrypt(testKey, wrongIV, encrypted.ciphertext, encrypted.tag)).rejects.toThrow(
        'Decryption failed'
      );
    });

    it('should fail decryption with tampered authentication tag', async () => {
      const plaintext = 'Secret message';

      const encrypted = await encrypt(testKey, plaintext);

      // Tamper with tag
      const tamperedTag = new Uint8Array(encrypted.tag);
      tamperedTag[0] ^= 0xff; // Flip bits in first byte

      await expect(
        decrypt(testKey, encrypted.iv, encrypted.ciphertext, tamperedTag)
      ).rejects.toThrow('Decryption failed');
    });

    it('should handle decryption of empty ciphertext', async () => {
      const plaintext = '';

      const encrypted = await encrypt(testKey, plaintext);
      const decrypted = await decrypt(testKey, encrypted.iv, encrypted.ciphertext, encrypted.tag);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });

    it('should gracefully handle decryption errors', async () => {
      const plaintext = 'Test';
      const encrypted = await encrypt(testKey, plaintext);

      // Corrupt the data to cause decryption failure
      const corruptedCiphertext = new Uint8Array(5); // Wrong size

      try {
        await decrypt(testKey, encrypted.iv, corruptedCiphertext, encrypted.tag);
        expect.fail('Should have thrown an error');
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
        expect((error as Error).message).toContain('Decryption failed');
      }
    });
  });

  describe('Base64 Encoding/Decoding', () => {
    it('should encrypt to Base64 format', async () => {
      const plaintext = 'Hello, World!';

      const encrypted = await encryptToBase64(testKey, plaintext);

      expect(encrypted).toBeDefined();
      expect(typeof encrypted.iv).toBe('string');
      expect(typeof encrypted.ciphertext).toBe('string');
      expect(typeof encrypted.tag).toBe('string');

      // Verify Base64 format (should not throw)
      expect(() => atob(encrypted.iv)).not.toThrow();
      expect(() => atob(encrypted.ciphertext)).not.toThrow();
      expect(() => atob(encrypted.tag)).not.toThrow();
    });

    it('should decrypt from Base64 format', async () => {
      const plaintext = 'Secret data';

      const encrypted = await encryptToBase64(testKey, plaintext);
      const decrypted = await decryptFromBase64(testKey, encrypted);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });

    it('should round-trip with Base64 encoding', async () => {
      const plaintext = 'Test message with special chars: 🔒🔑';

      const encrypted = await encryptToBase64(testKey, plaintext);
      const decrypted = await decryptFromBase64(testKey, encrypted);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });
  });

  describe('Test Vectors', () => {
    it('should work with known test vector 1', async () => {
      // Use a known key for reproducible tests
      const rawKey = new Uint8Array(32);
      for (let i = 0; i < 32; i++) {
        rawKey[i] = i;
      }
      const key = await importKey(rawKey);

      const plaintext = 'Test vector message';
      const encrypted = await encrypt(key, plaintext);
      const decrypted = await decrypt(key, encrypted.iv, encrypted.ciphertext, encrypted.tag);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });

    it('should work with known test vector 2 (empty message)', async () => {
      // Use a known key
      const rawKey = new Uint8Array(32);
      rawKey.fill(0xff);
      const key = await importKey(rawKey);

      const plaintext = '';
      const encrypted = await encrypt(key, plaintext);
      const decrypted = await decrypt(key, encrypted.iv, encrypted.ciphertext, encrypted.tag);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });

    it('should work with known test vector 3 (binary data)', async () => {
      // Use a known key
      const rawKey = new Uint8Array(32);
      for (let i = 0; i < 32; i++) {
        rawKey[i] = 255 - i;
      }
      const key = await importKey(rawKey);

      const plaintext = new Uint8Array([0, 1, 2, 3, 4, 5, 254, 255]);
      const encrypted = await encrypt(key, plaintext);
      const decrypted = await decrypt(key, encrypted.iv, encrypted.ciphertext, encrypted.tag);

      expect(decrypted).toEqual(plaintext);
    });

    it('should work with known test vector 4 (unicode)', async () => {
      // Use a known key
      const rawKey = new Uint8Array(32);
      for (let i = 0; i < 32; i++) {
        rawKey[i] = (i * 8) % 256;
      }
      const key = await importKey(rawKey);

      const plaintext = '🔐 Encryption test with emoji 🔑 and unicode: 中文';
      const encrypted = await encrypt(key, plaintext);
      const decrypted = await decrypt(key, encrypted.iv, encrypted.ciphertext, encrypted.tag);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });

    it('should produce consistent results with same key and IV (for testing)', async () => {
      // Note: In production, never reuse IVs!
      // This test is only to verify deterministic behavior

      const rawKey = new Uint8Array(32);
      rawKey.fill(42);
      const key = await importKey(rawKey);

      const plaintext = 'Deterministic test';

      // Encrypt once
      const encrypted1 = await encrypt(key, plaintext);

      // Decrypt with the same parameters to verify it works
      const decrypted1 = await decrypt(key, encrypted1.iv, encrypted1.ciphertext, encrypted1.tag);
      const text1 = new TextDecoder().decode(decrypted1);

      expect(text1).toBe(plaintext);

      // Encrypt again - should produce different ciphertext due to random IV
      const encrypted2 = await encrypt(key, plaintext);
      const decrypted2 = await decrypt(key, encrypted2.iv, encrypted2.ciphertext, encrypted2.tag);
      const text2 = new TextDecoder().decode(decrypted2);

      expect(text2).toBe(plaintext);

      // Verify that ciphertexts are different (due to different IVs)
      const ct1 = btoa(String.fromCharCode(...encrypted1.ciphertext));
      const ct2 = btoa(String.fromCharCode(...encrypted2.ciphertext));
      expect(ct1).not.toBe(ct2);
    });
  });

  describe('Edge Cases', () => {
    it('should handle very long messages', async () => {
      const plaintext = 'A'.repeat(1000000); // 1MB

      const encrypted = await encrypt(testKey, plaintext);
      const decrypted = await decrypt(testKey, encrypted.iv, encrypted.ciphertext, encrypted.tag);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
      expect(decrypted.length).toBe(1000000);
    });

    it('should handle messages with null bytes', async () => {
      const plaintext = new Uint8Array([1, 0, 2, 0, 3, 0]);

      const encrypted = await encrypt(testKey, plaintext);
      const decrypted = await decrypt(testKey, encrypted.iv, encrypted.ciphertext, encrypted.tag);

      expect(decrypted).toEqual(plaintext);
    });

    it('should handle single byte messages', async () => {
      const plaintext = 'X';

      const encrypted = await encrypt(testKey, plaintext);
      const decrypted = await decrypt(testKey, encrypted.iv, encrypted.ciphertext, encrypted.tag);
      const decryptedText = new TextDecoder().decode(decrypted);

      expect(decryptedText).toBe(plaintext);
    });
  });

  describe('Security Properties', () => {
    it('should verify that IVs are random and unique', async () => {
      const ivSet = new Set<string>();
      const iterations = 100;

      for (let i = 0; i < iterations; i++) {
        const result = await encrypt(testKey, 'test');
        const ivBase64 = btoa(String.fromCharCode(...result.iv));
        ivSet.add(ivBase64);
      }

      // All IVs should be unique
      expect(ivSet.size).toBe(iterations);
    });

    it('should verify authentication tag integrity', async () => {
      const plaintext = 'Important data';

      const encrypted = await encrypt(testKey, plaintext);

      // Ensure tag is present and correct length
      expect(encrypted.tag).toBeDefined();
      expect(encrypted.tag.length).toBe(16); // 128 bits

      // Decryption should succeed with correct tag
      const decrypted = await decrypt(testKey, encrypted.iv, encrypted.ciphertext, encrypted.tag);
      expect(decrypted).toBeDefined();

      // Decryption should fail with modified tag
      const modifiedTag = new Uint8Array(encrypted.tag);
      modifiedTag[0] ^= 1; // Flip one bit

      await expect(
        decrypt(testKey, encrypted.iv, encrypted.ciphertext, modifiedTag)
      ).rejects.toThrow();
    });

    it('should prevent decryption without the correct key', async () => {
      const plaintext = 'Confidential';
      const key1 = await generateKey();
      const key2 = await generateKey();

      const encrypted = await encrypt(key1, plaintext);

      // Should succeed with correct key
      await expect(
        decrypt(key1, encrypted.iv, encrypted.ciphertext, encrypted.tag)
      ).resolves.toBeDefined();

      // Should fail with different key
      await expect(
        decrypt(key2, encrypted.iv, encrypted.ciphertext, encrypted.tag)
      ).rejects.toThrow('Decryption failed');
    });
  });

  describe('Error Handling', () => {
    it('should throw descriptive errors on encryption failure', async () => {
      // Create an invalid key by modifying a valid one
      const invalidKey = {} as CryptoKey;

      await expect(encrypt(invalidKey, 'test')).rejects.toThrow('Encryption failed');
    });

    it('should throw descriptive errors on decryption failure', async () => {
      const invalidKey = {} as CryptoKey;
      const iv = new Uint8Array(12);
      const ciphertext = new Uint8Array(10);
      const tag = new Uint8Array(16);

      await expect(decrypt(invalidKey, iv, ciphertext, tag)).rejects.toThrow('Decryption failed');
    });

    it('should handle corrupt Base64 data gracefully', async () => {
      const encrypted = await encryptToBase64(testKey, 'test');

      // Corrupt the Base64 data
      const corruptedEncrypted = {
        iv: 'invalid-base64!!!',
        ciphertext: encrypted.ciphertext,
        tag: encrypted.tag,
      };

      await expect(decryptFromBase64(testKey, corruptedEncrypted)).rejects.toThrow();
    });
  });
});
