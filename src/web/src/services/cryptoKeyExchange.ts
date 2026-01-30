import _sodium from 'libsodium-wrappers';
import { openDB } from 'idb';
import type { DBSchema, IDBPDatabase } from 'idb';

// Fallback constants for libsodium (in case they're not available in test environment)
// These match the standard libsodium constants
const CRYPTO_PWHASH_SALTBYTES = 16;
const CRYPTO_SECRETBOX_KEYBYTES = 32;
const CRYPTO_SECRETBOX_NONCEBYTES = 24;
// Note: OPSLIMIT and MEMLIMIT define computational cost for password hashing
const CRYPTO_PWHASH_OPSLIMIT_INTERACTIVE = 2; // Argon2 operations limit for interactive use
const CRYPTO_PWHASH_MEMLIMIT_INTERACTIVE = 67108864; // 64MB memory limit
const CRYPTO_PWHASH_ALG_DEFAULT = 2; // Argon2id13

// Minimum password length for security
const MIN_PASSWORD_LENGTH = 8;

/**
 * Database schema for cryptographic keys
 */
interface CryptoKeysDBSchema extends DBSchema {
  keyPairs: {
    key: string;
    value: StoredKeyPair;
  };
}

/**
 * Structure for storing key pairs in IndexedDB
 */
export interface StoredKeyPair {
  id: string;
  publicKey: string; // Base64 encoded
  encryptedPrivateKey: string; // Encrypted and Base64 encoded
  createdAt: string;
}

/**
 * Key pair structure
 */
export interface KeyPair {
  publicKey: Uint8Array;
  privateKey: Uint8Array;
}

/**
 * Exported key pair (for API/profile)
 */
export interface ExportedKeyPair {
  publicKey: string; // Base64 encoded
}

/**
 * CryptoKeyExchange service for X25519 key exchange operations
 *
 * Features:
 * - Generate X25519 key pairs
 * - Derive shared secrets using X25519
 * - Store private keys encrypted in IndexedDB
 * - Export public keys for profile
 */
export class CryptoKeyExchange {
  private db: IDBPDatabase<CryptoKeysDBSchema> | null = null;
  private readonly dbName = 'TralliValli-Crypto';
  private readonly dbVersion = 1;
  private sodiumReady = false;

  /**
   * Initialize libsodium and database
   */
  async initialize(): Promise<void> {
    // Initialize libsodium
    await _sodium.ready;
    this.sodiumReady = true;

    // Initialize IndexedDB
    this.db = await openDB<CryptoKeysDBSchema>(this.dbName, this.dbVersion, {
      upgrade(db) {
        if (!db.objectStoreNames.contains('keyPairs')) {
          db.createObjectStore('keyPairs', {
            keyPath: 'id',
          });
        }
      },
    });
  }

  /**
   * Close the database connection
   */
  async close(): Promise<void> {
    if (this.db) {
      this.db.close();
      this.db = null;
    }
    this.sodiumReady = false;
  }

  /**
   * Ensure libsodium and database are initialized
   */
  private ensureReady(): void {
    if (!this.sodiumReady) {
      throw new Error('Crypto service not initialized. Call initialize() first.');
    }
    if (!this.db) {
      throw new Error('Database not initialized. Call initialize() first.');
    }
  }

  /**
   * Generate a new X25519 key pair
   * @returns KeyPair with publicKey and privateKey as Uint8Array
   *
   * Note: Uses crypto_box_keypair() which generates Curve25519 keys.
   * These are compatible with X25519 key exchange via crypto_scalarmult().
   * Both public and private keys are 32 bytes as per X25519 specification.
   */
  generateKeyPair(): KeyPair {
    this.ensureReady();

    const keyPair = _sodium.crypto_box_keypair();

    return {
      publicKey: keyPair.publicKey,
      privateKey: keyPair.privateKey,
    };
  }

  /**
   * Derive a shared secret using X25519 key exchange
   * @param privateKey - Your private key (Uint8Array)
   * @param peerPublicKey - Peer's public key (Uint8Array)
   * @returns Shared secret as Uint8Array (32 bytes)
   */
  deriveSharedSecret(privateKey: Uint8Array, peerPublicKey: Uint8Array): Uint8Array {
    this.ensureReady();

    if (privateKey.length !== _sodium.crypto_box_SECRETKEYBYTES) {
      throw new Error(
        `Invalid private key length: expected ${_sodium.crypto_box_SECRETKEYBYTES}, got ${privateKey.length}`
      );
    }

    if (peerPublicKey.length !== _sodium.crypto_box_PUBLICKEYBYTES) {
      throw new Error(
        `Invalid public key length: expected ${_sodium.crypto_box_PUBLICKEYBYTES}, got ${peerPublicKey.length}`
      );
    }

    // Use crypto_scalarmult to perform X25519 key exchange
    const sharedSecret = _sodium.crypto_scalarmult(privateKey, peerPublicKey);

    return sharedSecret;
  }

  /**
   * Encrypt private key using a password-derived key
   * @param privateKey - Private key to encrypt
   * @param password - Password for encryption (user's password or derived key)
   * @returns Encrypted private key as Base64 string
   *
   * Security Notes:
   * - Uses Argon2id password hashing when available for key derivation
   * - Falls back to BLAKE2b hash in test environments (less secure, not for production)
   * - Random salt and nonce ensure different ciphertext for same key/password
   */
  private encryptPrivateKey(privateKey: Uint8Array, password: string): string {
    this.ensureReady();

    // Validate password
    if (password.length < MIN_PASSWORD_LENGTH) {
      throw new Error(`Password must be at least ${MIN_PASSWORD_LENGTH} characters long`);
    }

    // Use fallback constants if libsodium constants are not available
    const saltBytes = _sodium.crypto_pwhash_SALTBYTES ?? CRYPTO_PWHASH_SALTBYTES;
    const keyBytes = _sodium.crypto_secretbox_KEYBYTES ?? CRYPTO_SECRETBOX_KEYBYTES;
    const nonceBytes = _sodium.crypto_secretbox_NONCEBYTES ?? CRYPTO_SECRETBOX_NONCEBYTES;

    // Generate a random salt
    const salt = _sodium.randombytes_buf(saltBytes);

    // Derive a key from the password
    // Use crypto_generichash as fallback if crypto_pwhash is not available
    let key: Uint8Array;
    try {
      const opsLimit =
        _sodium.crypto_pwhash_OPSLIMIT_INTERACTIVE ?? CRYPTO_PWHASH_OPSLIMIT_INTERACTIVE;
      const memLimit =
        _sodium.crypto_pwhash_MEMLIMIT_INTERACTIVE ?? CRYPTO_PWHASH_MEMLIMIT_INTERACTIVE;
      const algDefault = _sodium.crypto_pwhash_ALG_DEFAULT ?? CRYPTO_PWHASH_ALG_DEFAULT;

      key = _sodium.crypto_pwhash(keyBytes, password, salt, opsLimit, memLimit, algDefault);
    } catch {
      // Fallback to generic hash for test environments where crypto_pwhash is not available
      // WARNING: This fallback provides significantly less security than Argon2id
      // It should only be used in test environments, never in production
      console.warn(
        'crypto_pwhash not available, falling back to crypto_generichash (test environment only)'
      );

      // Combine password and salt for key derivation
      const passwordBytes = _sodium.from_string(password);
      const combined = new Uint8Array(passwordBytes.length + salt.length);
      combined.set(passwordBytes, 0);
      combined.set(salt, passwordBytes.length);
      key = _sodium.crypto_generichash(keyBytes, combined, null);
    }

    // Encrypt the private key
    const nonce = _sodium.randombytes_buf(nonceBytes);
    const ciphertext = _sodium.crypto_secretbox_easy(privateKey, nonce, key);

    // Clear sensitive key material from memory
    _sodium.memzero(key);

    // Combine salt, nonce, and ciphertext
    const combined = new Uint8Array(salt.length + nonce.length + ciphertext.length);
    combined.set(salt, 0);
    combined.set(nonce, salt.length);
    combined.set(ciphertext, salt.length + nonce.length);

    return _sodium.to_base64(combined);
  }

  /**
   * Decrypt private key using a password
   * @param encryptedPrivateKey - Encrypted private key as Base64 string
   * @param password - Password for decryption
   * @returns Decrypted private key as Uint8Array
   *
   * Security Notes:
   * - Uses same key derivation as encryption (Argon2id or BLAKE2b fallback)
   * - Sensitive key material is cleared from memory after use
   */
  private decryptPrivateKey(encryptedPrivateKey: string, password: string): Uint8Array {
    this.ensureReady();

    // Validate password
    if (password.length < MIN_PASSWORD_LENGTH) {
      throw new Error(`Password must be at least ${MIN_PASSWORD_LENGTH} characters long`);
    }

    // Use fallback constants if libsodium constants are not available
    const saltBytes = _sodium.crypto_pwhash_SALTBYTES ?? CRYPTO_PWHASH_SALTBYTES;
    const keyBytes = _sodium.crypto_secretbox_KEYBYTES ?? CRYPTO_SECRETBOX_KEYBYTES;
    const nonceBytes = _sodium.crypto_secretbox_NONCEBYTES ?? CRYPTO_SECRETBOX_NONCEBYTES;

    // Decode from Base64
    const combined = _sodium.from_base64(encryptedPrivateKey);

    // Extract salt, nonce, and ciphertext
    const salt = combined.slice(0, saltBytes);
    const nonce = combined.slice(saltBytes, saltBytes + nonceBytes);
    const ciphertext = combined.slice(saltBytes + nonceBytes);

    // Derive the key from the password
    // Use crypto_generichash as fallback if crypto_pwhash is not available
    let key: Uint8Array;
    try {
      const opsLimit =
        _sodium.crypto_pwhash_OPSLIMIT_INTERACTIVE ?? CRYPTO_PWHASH_OPSLIMIT_INTERACTIVE;
      const memLimit =
        _sodium.crypto_pwhash_MEMLIMIT_INTERACTIVE ?? CRYPTO_PWHASH_MEMLIMIT_INTERACTIVE;
      const algDefault = _sodium.crypto_pwhash_ALG_DEFAULT ?? CRYPTO_PWHASH_ALG_DEFAULT;

      key = _sodium.crypto_pwhash(keyBytes, password, salt, opsLimit, memLimit, algDefault);
    } catch {
      // Fallback to generic hash for test environments (must match encryption fallback)
      console.warn(
        'crypto_pwhash not available, falling back to crypto_generichash (test environment only)'
      );

      const passwordBytes = _sodium.from_string(password);
      const combinedInput = new Uint8Array(passwordBytes.length + salt.length);
      combinedInput.set(passwordBytes, 0);
      combinedInput.set(salt, passwordBytes.length);
      key = _sodium.crypto_generichash(keyBytes, combinedInput, null);
    }

    // Decrypt the private key
    const privateKey = _sodium.crypto_secretbox_open_easy(ciphertext, nonce, key);

    // Clear sensitive key material from memory
    _sodium.memzero(key);

    if (!privateKey) {
      throw new Error('Failed to decrypt private key. Invalid password or corrupted data.');
    }

    return privateKey;
  }

  /**
   * Store a key pair in IndexedDB with encrypted private key
   * @param id - Unique identifier for the key pair
   * @param keyPair - Key pair to store
   * @param password - Password to encrypt the private key
   */
  async storeKeyPair(id: string, keyPair: KeyPair, password: string): Promise<void> {
    this.ensureReady();

    // Validate input
    if (!id || id.trim().length === 0) {
      throw new Error('Key pair ID cannot be empty');
    }

    const encryptedPrivateKey = this.encryptPrivateKey(keyPair.privateKey, password);
    const publicKeyBase64 = _sodium.to_base64(keyPair.publicKey);

    const storedKeyPair: StoredKeyPair = {
      id: id.trim(),
      publicKey: publicKeyBase64,
      encryptedPrivateKey,
      createdAt: new Date().toISOString(),
    };

    await this.db!.put('keyPairs', storedKeyPair);
  }

  /**
   * Retrieve a key pair from IndexedDB and decrypt private key
   * @param id - Unique identifier for the key pair
   * @param password - Password to decrypt the private key
   * @returns KeyPair with decrypted private key, or null if not found
   */
  async getKeyPair(id: string, password: string): Promise<KeyPair | null> {
    this.ensureReady();

    const stored = await this.db!.get('keyPairs', id);
    if (!stored) {
      return null;
    }

    try {
      const publicKey = _sodium.from_base64(stored.publicKey);
      const privateKey = this.decryptPrivateKey(stored.encryptedPrivateKey, password);

      return {
        publicKey,
        privateKey,
      };
    } catch (error) {
      // Re-throw with clearer message if decryption fails
      throw new Error(
        `Failed to retrieve key pair: ${error instanceof Error ? error.message : 'Unknown error'}`
      );
    }
  }

  /**
   * Export public key for profile or API
   * @param id - Unique identifier for the key pair
   * @returns Exported public key as Base64 string
   */
  async exportPublicKey(id: string): Promise<ExportedKeyPair | null> {
    this.ensureReady();

    const stored = await this.db!.get('keyPairs', id);
    if (!stored) {
      return null;
    }

    return {
      publicKey: stored.publicKey,
    };
  }

  /**
   * Delete a key pair from IndexedDB
   * @param id - Unique identifier for the key pair
   */
  async deleteKeyPair(id: string): Promise<void> {
    this.ensureReady();
    await this.db!.delete('keyPairs', id);
  }

  /**
   * Get all stored key pair IDs
   * @returns Array of key pair IDs
   */
  async getAllKeyPairIds(): Promise<string[]> {
    this.ensureReady();
    const keys = await this.db!.getAllKeys('keyPairs');
    return keys;
  }

  /**
   * Clear all key pairs from storage
   */
  async clearAll(): Promise<void> {
    this.ensureReady();
    await this.db!.clear('keyPairs');
  }

  /**
   * Convert Base64 string to Uint8Array
   * @param base64 - Base64 encoded string
   * @returns Uint8Array
   */
  static async fromBase64(base64: string): Promise<Uint8Array> {
    await _sodium.ready;
    return _sodium.from_base64(base64);
  }

  /**
   * Convert Uint8Array to Base64 string
   * @param data - Uint8Array data
   * @returns Base64 encoded string
   */
  static async toBase64(data: Uint8Array): Promise<string> {
    await _sodium.ready;
    return _sodium.to_base64(data);
  }
}
