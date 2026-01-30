/**
 * AES-256-GCM Encryption Service using Web Crypto API
 *
 * Provides authenticated encryption with associated data (AEAD)
 * using AES-256 in GCM (Galois/Counter Mode) mode.
 *
 * Features:
 * - AES-256-GCM encryption and decryption
 * - Random IV generation per message
 * - Authentication tag included in output
 * - Graceful error handling
 */

/**
 * Result of encryption operation
 */
export interface EncryptionResult {
  iv: Uint8Array; // Initialization Vector (12 bytes for GCM)
  ciphertext: Uint8Array; // Encrypted data
  tag: Uint8Array; // Authentication tag (16 bytes)
}

/**
 * Encrypted data structure for storage/transmission
 */
export interface EncryptedData {
  iv: string; // Base64 encoded IV
  ciphertext: string; // Base64 encoded ciphertext (without tag)
  tag: string; // Base64 encoded authentication tag
}

// Constants for AES-GCM
const AES_ALGORITHM = 'AES-GCM';
const AES_KEY_LENGTH = 256; // bits
const IV_LENGTH = 12; // bytes (96 bits) - recommended for GCM
const TAG_LENGTH = 128; // bits (16 bytes)

/**
 * Generate a cryptographically secure random IV
 * @returns Random IV as Uint8Array (12 bytes)
 */
function generateIV(): Uint8Array {
  return crypto.getRandomValues(new Uint8Array(IV_LENGTH));
}

/**
 * Convert ArrayBuffer to Base64 string
 * @param buffer - ArrayBuffer to convert
 * @returns Base64 encoded string
 */
function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  const chars: string[] = [];
  for (let i = 0; i < bytes.length; i++) {
    chars.push(String.fromCharCode(bytes[i]));
  }
  return btoa(chars.join(''));
}

/**
 * Convert Base64 string to Uint8Array
 * @param base64 - Base64 encoded string
 * @returns Uint8Array
 */
function base64ToUint8Array(base64: string): Uint8Array {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

/**
 * Encrypt plaintext using AES-256-GCM
 *
 * @param key - CryptoKey for encryption (must be AES-256-GCM key)
 * @param plaintext - Data to encrypt (string or Uint8Array)
 * @returns Promise resolving to encryption result with IV, ciphertext, and tag
 *
 * @example
 * ```typescript
 * const key = await generateKey();
 * const result = await encrypt(key, "Hello, World!");
 * console.log(result); // { iv: Uint8Array, ciphertext: Uint8Array, tag: Uint8Array }
 * ```
 */
export async function encrypt(
  key: CryptoKey,
  plaintext: string | Uint8Array
): Promise<EncryptionResult> {
  try {
    // Validate key
    if (!key || key.type !== 'secret' || key.algorithm.name !== 'AES-GCM') {
      throw new Error('Invalid key: must be an AES-GCM secret key');
    }
    if (!key.usages.includes('encrypt')) {
      throw new Error('Invalid key: must have encrypt usage');
    }

    // Convert string to Uint8Array if needed
    const plaintextBytes =
      typeof plaintext === 'string' ? new TextEncoder().encode(plaintext) : plaintext;

    // Generate random IV for this message
    const iv = generateIV();

    // Encrypt the data
    // GCM mode automatically generates and appends the authentication tag
    const ciphertextWithTag = await crypto.subtle.encrypt(
      {
        name: AES_ALGORITHM,
        // @ts-ignore - TypeScript 5.x strict mode has issues with Uint8Array<ArrayBufferLike> vs BufferSource
        iv,
        tagLength: TAG_LENGTH,
      },
      key,
      // @ts-ignore - TypeScript 5.x strict mode has issues with Uint8Array<ArrayBufferLike> vs BufferSource
      plaintextBytes
    );

    // In GCM mode, the authentication tag is appended to the ciphertext
    // We need to split them for the return value
    const ciphertextWithTagArray = new Uint8Array(ciphertextWithTag);
    const tagLengthBytes = TAG_LENGTH / 8; // Convert bits to bytes

    // Extract ciphertext and tag
    const ciphertext = ciphertextWithTagArray.slice(0, -tagLengthBytes);
    const tag = ciphertextWithTagArray.slice(-tagLengthBytes);

    return {
      iv,
      ciphertext,
      tag,
    };
  } catch (error) {
    throw new Error(
      `Encryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`
    );
  }
}

/**
 * Decrypt ciphertext using AES-256-GCM
 *
 * @param key - CryptoKey for decryption (must be AES-256-GCM key)
 * @param iv - Initialization vector used during encryption
 * @param ciphertext - Encrypted data (without authentication tag)
 * @param tag - Authentication tag (optional if included in ciphertext)
 * @returns Promise resolving to decrypted plaintext as Uint8Array
 *
 * @throws Error if decryption fails (wrong key, corrupted data, or tampered ciphertext)
 *
 * @example
 * ```typescript
 * const key = await generateKey();
 * const encrypted = await encrypt(key, "Hello, World!");
 * const decrypted = await decrypt(key, encrypted.iv, encrypted.ciphertext, encrypted.tag);
 * const text = new TextDecoder().decode(decrypted);
 * console.log(text); // "Hello, World!"
 * ```
 */
export async function decrypt(
  key: CryptoKey,
  iv: Uint8Array,
  ciphertext: Uint8Array,
  tag?: Uint8Array
): Promise<Uint8Array> {
  try {
    // Validate key
    if (!key || key.type !== 'secret' || key.algorithm.name !== 'AES-GCM') {
      throw new Error('Invalid key: must be an AES-GCM secret key');
    }
    if (!key.usages.includes('decrypt')) {
      throw new Error('Invalid key: must have decrypt usage');
    }

    // Validate IV length
    if (iv.length !== IV_LENGTH) {
      throw new Error(`Invalid IV length: expected ${IV_LENGTH} bytes, got ${iv.length}`);
    }

    // Validate tag length if provided
    if (tag && tag.length !== TAG_LENGTH / 8) {
      throw new Error(`Invalid tag length: expected ${TAG_LENGTH / 8} bytes, got ${tag.length}`);
    }

    // Combine ciphertext and tag for GCM decryption
    // Web Crypto API expects them together
    let ciphertextWithTag: Uint8Array;

    if (tag) {
      // If tag is provided separately, combine them
      ciphertextWithTag = new Uint8Array(ciphertext.length + tag.length);
      ciphertextWithTag.set(ciphertext, 0);
      ciphertextWithTag.set(tag, ciphertext.length);
    } else {
      // If tag is already included in ciphertext, use as is
      ciphertextWithTag = ciphertext;
    }

    // Decrypt the data
    // GCM mode automatically verifies the authentication tag
    const plaintextBuffer = await crypto.subtle.decrypt(
      {
        name: AES_ALGORITHM,
        // @ts-ignore - TypeScript 5.x strict mode has issues with Uint8Array<ArrayBufferLike> vs BufferSource
        iv,
        tagLength: TAG_LENGTH,
      },
      key,
      // @ts-ignore - TypeScript 5.x strict mode has issues with Uint8Array<ArrayBufferLike> vs BufferSource
      ciphertextWithTag
    );

    return new Uint8Array(plaintextBuffer);
  } catch (error) {
    // Gracefully handle decryption failures
    // This can happen due to:
    // - Wrong decryption key
    // - Corrupted ciphertext
    // - Tampered data (authentication failure)
    // - Invalid IV
    if (error instanceof Error) {
      // Check for specific crypto errors
      if (error.name === 'OperationError') {
        throw new Error('Decryption failed: Authentication tag verification failed or invalid key');
      }
    }
    throw new Error(
      `Decryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`
    );
  }
}

/**
 * Generate a new AES-256-GCM encryption key
 *
 * @param extractable - Whether the key can be exported (default: false)
 * @returns Promise resolving to a new CryptoKey
 *
 * @example
 * ```typescript
 * const key = await generateKey();
 * // Use key for encryption/decryption
 * ```
 */
export async function generateKey(extractable = false): Promise<CryptoKey> {
  try {
    return await crypto.subtle.generateKey(
      {
        name: AES_ALGORITHM,
        length: AES_KEY_LENGTH,
      },
      extractable,
      ['encrypt', 'decrypt']
    );
  } catch (error) {
    throw new Error(
      `Key generation failed: ${error instanceof Error ? error.message : 'Unknown error'}`
    );
  }
}

/**
 * Import a raw key for AES-256-GCM encryption
 *
 * @param rawKey - Raw key bytes (must be 32 bytes for AES-256)
 * @param extractable - Whether the key can be exported (default: false)
 * @returns Promise resolving to a CryptoKey
 *
 * @example
 * ```typescript
 * const rawKey = new Uint8Array(32); // 32 bytes for AES-256
 * crypto.getRandomValues(rawKey);
 * const key = await importKey(rawKey);
 * ```
 */
export async function importKey(rawKey: Uint8Array, extractable = false): Promise<CryptoKey> {
  try {
    if (rawKey.length !== 32) {
      throw new Error(`Invalid key length: expected 32 bytes for AES-256, got ${rawKey.length}`);
    }

    // @ts-ignore - TypeScript 5.x strict mode has issues with Uint8Array<ArrayBufferLike> vs BufferSource
    return await crypto.subtle.importKey(
      'raw',
      // @ts-ignore - TypeScript 5.x strict mode has issues with Uint8Array<ArrayBufferLike> vs BufferSource
      rawKey,
      {
        name: AES_ALGORITHM,
        length: AES_KEY_LENGTH,
      },
      extractable,
      ['encrypt', 'decrypt']
    );
  } catch (error) {
    throw new Error(
      `Key import failed: ${error instanceof Error ? error.message : 'Unknown error'}`
    );
  }
}

/**
 * Export a CryptoKey to raw bytes
 *
 * @param key - CryptoKey to export (must be extractable)
 * @returns Promise resolving to raw key bytes
 *
 * @example
 * ```typescript
 * const key = await generateKey(true); // extractable = true
 * const rawKey = await exportKey(key);
 * // Store rawKey securely
 * ```
 */
export async function exportKey(key: CryptoKey): Promise<Uint8Array> {
  try {
    const exported = await crypto.subtle.exportKey('raw', key);
    return new Uint8Array(exported);
  } catch (error) {
    throw new Error(
      `Key export failed: ${error instanceof Error ? error.message : 'Unknown error'}`
    );
  }
}

/**
 * Encrypt plaintext and return Base64-encoded result
 *
 * @param key - CryptoKey for encryption
 * @param plaintext - Data to encrypt
 * @returns Promise resolving to Base64-encoded encryption result
 *
 * @example
 * ```typescript
 * const key = await generateKey();
 * const encrypted = await encryptToBase64(key, "Hello, World!");
 * // Store encrypted.iv and encrypted.ciphertext
 * ```
 */
export async function encryptToBase64(
  key: CryptoKey,
  plaintext: string | Uint8Array
): Promise<EncryptedData> {
  const result = await encrypt(key, plaintext);
  return {
    iv: arrayBufferToBase64(result.iv.buffer as ArrayBuffer),
    ciphertext: arrayBufferToBase64(result.ciphertext.buffer as ArrayBuffer),
    tag: arrayBufferToBase64(result.tag.buffer as ArrayBuffer),
  };
}

/**
 * Decrypt Base64-encoded ciphertext
 *
 * @param key - CryptoKey for decryption
 * @param encrypted - Base64-encoded encryption result
 * @returns Promise resolving to decrypted plaintext as Uint8Array
 *
 * @example
 * ```typescript
 * const key = await generateKey();
 * const encrypted = await encryptToBase64(key, "Hello, World!");
 * const decrypted = await decryptFromBase64(key, encrypted);
 * const text = new TextDecoder().decode(decrypted);
 * console.log(text); // "Hello, World!"
 * ```
 */
export async function decryptFromBase64(
  key: CryptoKey,
  encrypted: EncryptedData
): Promise<Uint8Array> {
  const iv = base64ToUint8Array(encrypted.iv);
  const ciphertext = base64ToUint8Array(encrypted.ciphertext);
  const tag = base64ToUint8Array(encrypted.tag);

  return await decrypt(key, iv, ciphertext, tag);
}
