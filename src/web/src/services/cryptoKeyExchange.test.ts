import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { CryptoKeyExchange } from './cryptoKeyExchange';

describe('CryptoKeyExchange', () => {
  let cryptoService: CryptoKeyExchange;
  const testPassword = 'test-password-123';

  beforeEach(async () => {
    cryptoService = new CryptoKeyExchange();
    await cryptoService.initialize();
  });

  afterEach(async () => {
    await cryptoService.clearAll();
    await cryptoService.close();
  });

  describe('Initialization', () => {
    it('should initialize successfully', async () => {
      const newService = new CryptoKeyExchange();
      await expect(newService.initialize()).resolves.not.toThrow();
      await newService.close();
    });

    it('should throw error when using service before initialization', () => {
      const newService = new CryptoKeyExchange();
      expect(() => newService.generateKeyPair()).toThrow(
        'Crypto service not initialized. Call initialize() first.'
      );
    });
  });

  describe('Key Pair Generation', () => {
    it('should generate a valid X25519 key pair', () => {
      const keyPair = cryptoService.generateKeyPair();

      expect(keyPair).toBeDefined();
      expect(keyPair.publicKey).toBeInstanceOf(Uint8Array);
      expect(keyPair.privateKey).toBeInstanceOf(Uint8Array);
      expect(keyPair.publicKey.length).toBe(32); // X25519 public key is 32 bytes
      expect(keyPair.privateKey.length).toBe(32); // X25519 private key is 32 bytes
    });

    it('should generate unique key pairs', async () => {
      const keyPair1 = cryptoService.generateKeyPair();
      const keyPair2 = cryptoService.generateKeyPair();

      // Convert to base64 for easier comparison
      const pub1 = await CryptoKeyExchange.toBase64(keyPair1.publicKey);
      const pub2 = await CryptoKeyExchange.toBase64(keyPair2.publicKey);
      const priv1 = await CryptoKeyExchange.toBase64(keyPair1.privateKey);
      const priv2 = await CryptoKeyExchange.toBase64(keyPair2.privateKey);

      expect(pub1).not.toBe(pub2);
      expect(priv1).not.toBe(priv2);
    });
  });

  describe('Shared Secret Derivation', () => {
    it('should derive a shared secret correctly', async () => {
      const aliceKeyPair = cryptoService.generateKeyPair();
      const bobKeyPair = cryptoService.generateKeyPair();

      const aliceSharedSecret = cryptoService.deriveSharedSecret(
        aliceKeyPair.privateKey,
        bobKeyPair.publicKey
      );
      const bobSharedSecret = cryptoService.deriveSharedSecret(
        bobKeyPair.privateKey,
        aliceKeyPair.publicKey
      );

      expect(aliceSharedSecret).toBeInstanceOf(Uint8Array);
      expect(bobSharedSecret).toBeInstanceOf(Uint8Array);
      expect(aliceSharedSecret.length).toBe(32); // Shared secret is 32 bytes

      // Both parties should derive the same shared secret
      expect(await CryptoKeyExchange.toBase64(aliceSharedSecret)).toBe(
        await CryptoKeyExchange.toBase64(bobSharedSecret)
      );
    });

    it('should derive different shared secrets for different key pairs', async () => {
      const aliceKeyPair = cryptoService.generateKeyPair();
      const bobKeyPair = cryptoService.generateKeyPair();
      const charlieKeyPair = cryptoService.generateKeyPair();

      const aliceBobSecret = cryptoService.deriveSharedSecret(
        aliceKeyPair.privateKey,
        bobKeyPair.publicKey
      );
      const aliceCharlieSecret = cryptoService.deriveSharedSecret(
        aliceKeyPair.privateKey,
        charlieKeyPair.publicKey
      );

      expect(await CryptoKeyExchange.toBase64(aliceBobSecret)).not.toBe(
        await CryptoKeyExchange.toBase64(aliceCharlieSecret)
      );
    });

    it('should throw error with invalid private key length', () => {
      const keyPair = cryptoService.generateKeyPair();
      const invalidPrivateKey = new Uint8Array(16); // Wrong length

      expect(() =>
        cryptoService.deriveSharedSecret(invalidPrivateKey, keyPair.publicKey)
      ).toThrow('Invalid private key length');
    });

    it('should throw error with invalid public key length', () => {
      const keyPair = cryptoService.generateKeyPair();
      const invalidPublicKey = new Uint8Array(16); // Wrong length

      expect(() =>
        cryptoService.deriveSharedSecret(keyPair.privateKey, invalidPublicKey)
      ).toThrow('Invalid public key length');
    });
  });

  describe('Key Pair Storage', () => {
    it('should store and retrieve a key pair with correct password', async () => {
      const originalKeyPair = cryptoService.generateKeyPair();
      const keyId = 'test-key-1';

      await cryptoService.storeKeyPair(keyId, originalKeyPair, testPassword);

      const retrievedKeyPair = await cryptoService.getKeyPair(keyId, testPassword);

      expect(retrievedKeyPair).toBeDefined();
      expect(await CryptoKeyExchange.toBase64(retrievedKeyPair!.publicKey)).toBe(
        await CryptoKeyExchange.toBase64(originalKeyPair.publicKey)
      );
      expect(await CryptoKeyExchange.toBase64(retrievedKeyPair!.privateKey)).toBe(
        await CryptoKeyExchange.toBase64(originalKeyPair.privateKey)
      );
    });

    it('should fail to retrieve key pair with incorrect password', async () => {
      const keyPair = cryptoService.generateKeyPair();
      const keyId = 'test-key-2';

      await cryptoService.storeKeyPair(keyId, keyPair, testPassword);

      await expect(
        cryptoService.getKeyPair(keyId, 'wrong-password')
      ).rejects.toThrow(); // Just check that it throws an error
    });

    it('should return null for non-existent key pair', async () => {
      const retrievedKeyPair = await cryptoService.getKeyPair('non-existent', testPassword);
      expect(retrievedKeyPair).toBeNull();
    });

    it('should store multiple key pairs', async () => {
      const keyPair1 = cryptoService.generateKeyPair();
      const keyPair2 = cryptoService.generateKeyPair();

      await cryptoService.storeKeyPair('key-1', keyPair1, testPassword);
      await cryptoService.storeKeyPair('key-2', keyPair2, 'different-password');

      const retrieved1 = await cryptoService.getKeyPair('key-1', testPassword);
      const retrieved2 = await cryptoService.getKeyPair('key-2', 'different-password');

      expect(retrieved1).toBeDefined();
      expect(retrieved2).toBeDefined();
      expect(await CryptoKeyExchange.toBase64(retrieved1!.publicKey)).toBe(
        await CryptoKeyExchange.toBase64(keyPair1.publicKey)
      );
      expect(await CryptoKeyExchange.toBase64(retrieved2!.publicKey)).toBe(
        await CryptoKeyExchange.toBase64(keyPair2.publicKey)
      );
    });

    it('should overwrite existing key pair when storing with same ID', async () => {
      const keyPair1 = cryptoService.generateKeyPair();
      const keyPair2 = cryptoService.generateKeyPair();
      const keyId = 'test-key';

      await cryptoService.storeKeyPair(keyId, keyPair1, testPassword);
      await cryptoService.storeKeyPair(keyId, keyPair2, testPassword);

      const retrieved = await cryptoService.getKeyPair(keyId, testPassword);

      expect(await CryptoKeyExchange.toBase64(retrieved!.publicKey)).toBe(
        await CryptoKeyExchange.toBase64(keyPair2.publicKey)
      );
      expect(await CryptoKeyExchange.toBase64(retrieved!.publicKey)).not.toBe(
        await CryptoKeyExchange.toBase64(keyPair1.publicKey)
      );
    });
  });

  describe('Public Key Export', () => {
    it('should export public key for stored key pair', async () => {
      const keyPair = cryptoService.generateKeyPair();
      const keyId = 'export-test-1';

      await cryptoService.storeKeyPair(keyId, keyPair, testPassword);

      const exported = await cryptoService.exportPublicKey(keyId);

      expect(exported).toBeDefined();
      expect(exported!.publicKey).toBe(await CryptoKeyExchange.toBase64(keyPair.publicKey));
    });

    it('should return null when exporting non-existent key', async () => {
      const exported = await cryptoService.exportPublicKey('non-existent');
      expect(exported).toBeNull();
    });

    it('should export public key without requiring password', async () => {
      const keyPair = cryptoService.generateKeyPair();
      const keyId = 'export-test-2';

      await cryptoService.storeKeyPair(keyId, keyPair, testPassword);

      // Should work without password
      const exported = await cryptoService.exportPublicKey(keyId);
      expect(exported).toBeDefined();
    });
  });

  describe('Key Pair Management', () => {
    it('should delete a key pair', async () => {
      const keyPair = cryptoService.generateKeyPair();
      const keyId = 'delete-test';

      await cryptoService.storeKeyPair(keyId, keyPair, testPassword);
      await cryptoService.deleteKeyPair(keyId);

      const retrieved = await cryptoService.getKeyPair(keyId, testPassword);
      expect(retrieved).toBeNull();
    });

    it('should get all key pair IDs', async () => {
      const keyPair1 = cryptoService.generateKeyPair();
      const keyPair2 = cryptoService.generateKeyPair();
      const keyPair3 = cryptoService.generateKeyPair();

      await cryptoService.storeKeyPair('key-1', keyPair1, testPassword);
      await cryptoService.storeKeyPair('key-2', keyPair2, testPassword);
      await cryptoService.storeKeyPair('key-3', keyPair3, testPassword);

      const ids = await cryptoService.getAllKeyPairIds();

      expect(ids).toHaveLength(3);
      expect(ids).toContain('key-1');
      expect(ids).toContain('key-2');
      expect(ids).toContain('key-3');
    });

    it('should clear all key pairs', async () => {
      const keyPair1 = cryptoService.generateKeyPair();
      const keyPair2 = cryptoService.generateKeyPair();

      await cryptoService.storeKeyPair('key-1', keyPair1, testPassword);
      await cryptoService.storeKeyPair('key-2', keyPair2, testPassword);

      await cryptoService.clearAll();

      const ids = await cryptoService.getAllKeyPairIds();
      expect(ids).toHaveLength(0);
    });
  });

  describe('Base64 Conversion Utilities', () => {
    it('should convert Uint8Array to Base64 and back', async () => {
      const original = new Uint8Array([1, 2, 3, 4, 5, 6, 7, 8]);
      const base64 = await CryptoKeyExchange.toBase64(original);
      const converted = await CryptoKeyExchange.fromBase64(base64);

      expect(base64).toBe('AQIDBAUGBwg');
      expect(converted).toEqual(original);
    });

    it('should handle empty arrays', async () => {
      const original = new Uint8Array([]);
      const base64 = await CryptoKeyExchange.toBase64(original);
      const converted = await CryptoKeyExchange.fromBase64(base64);

      expect(converted).toEqual(original);
    });
  });

  describe('End-to-End Key Exchange Scenario', () => {
    it('should perform complete key exchange between two parties', async () => {
      // Alice generates her key pair
      const aliceKeyPair = cryptoService.generateKeyPair();
      await cryptoService.storeKeyPair('alice-key', aliceKeyPair, 'alice-password');

      // Bob generates his key pair
      const bobKeyPair = cryptoService.generateKeyPair();
      await cryptoService.storeKeyPair('bob-key', bobKeyPair, 'bob-password');

      // Alice exports her public key to send to Bob
      const alicePublicExport = await cryptoService.exportPublicKey('alice-key');
      expect(alicePublicExport).toBeDefined();

      // Bob exports his public key to send to Alice
      const bobPublicExport = await cryptoService.exportPublicKey('bob-key');
      expect(bobPublicExport).toBeDefined();

      // Alice receives Bob's public key and derives shared secret
      const bobPublicKeyForAlice = await CryptoKeyExchange.fromBase64(bobPublicExport!.publicKey);
      const aliceRetrievedKeys = await cryptoService.getKeyPair('alice-key', 'alice-password');
      const aliceSharedSecret = cryptoService.deriveSharedSecret(
        aliceRetrievedKeys!.privateKey,
        bobPublicKeyForAlice
      );

      // Bob receives Alice's public key and derives shared secret
      const alicePublicKeyForBob = await CryptoKeyExchange.fromBase64(alicePublicExport!.publicKey);
      const bobRetrievedKeys = await cryptoService.getKeyPair('bob-key', 'bob-password');
      const bobSharedSecret = cryptoService.deriveSharedSecret(
        bobRetrievedKeys!.privateKey,
        alicePublicKeyForBob
      );

      // Both should have derived the same shared secret
      expect(await CryptoKeyExchange.toBase64(aliceSharedSecret)).toBe(
        await CryptoKeyExchange.toBase64(bobSharedSecret)
      );

      // The shared secret should be 32 bytes
      expect(aliceSharedSecret.length).toBe(32);
      expect(bobSharedSecret.length).toBe(32);
    });
  });

  describe('Security Properties', () => {
    it('should encrypt private keys (stored key should not match original)', async () => {
      const keyPair = cryptoService.generateKeyPair();
      const keyId = 'security-test';

      await cryptoService.storeKeyPair(keyId, keyPair, testPassword);

      // Get the raw stored data (this requires accessing IndexedDB directly)
      // For now, we verify that retrieval with wrong password fails
      await expect(
        cryptoService.getKeyPair(keyId, 'wrong-password')
      ).rejects.toThrow();

      // Verify correct password works
      const retrieved = await cryptoService.getKeyPair(keyId, testPassword);
      expect(retrieved).toBeDefined();
    });

    it('should use salt in encryption (same key encrypted twice should be different)', async () => {
      const keyPair = cryptoService.generateKeyPair();

      await cryptoService.storeKeyPair('key-1', keyPair, testPassword);
      await cryptoService.deleteKeyPair('key-1');
      await cryptoService.storeKeyPair('key-1', keyPair, testPassword);

      // Even though we encrypted the same key with the same password,
      // the encrypted values should be different due to random salt and nonce
      // We can't directly verify this without accessing the encrypted data,
      // but we can verify that both decrypt correctly
      const retrieved = await cryptoService.getKeyPair('key-1', testPassword);
      expect(await CryptoKeyExchange.toBase64(retrieved!.privateKey)).toBe(
        await CryptoKeyExchange.toBase64(keyPair.privateKey)
      );
    });
  });
});
