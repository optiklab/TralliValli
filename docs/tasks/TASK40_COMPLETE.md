# Task 40: Key Management Service Implementation

## Overview

This document describes the implementation of a comprehensive key management service for TralliValli. The service provides per-conversation encryption keys with secure storage and rotation capabilities.

## Implementation Details

### Core Service: KeyManagementService

Location: `src/web/src/services/keyManagement.ts`

The `KeyManagementService` class provides complete key management functionality with the following capabilities:

#### 1. Master Key Management

**Purpose**: The master key is used to encrypt conversation keys at rest in IndexedDB.

```typescript
// Set master key directly (from existing source)
await keyService.setMasterKey(masterKey);

// Or derive from password
const { masterKey, salt } = await keyService.deriveMasterKeyFromPassword(
  'user-password',
  salt // optional, for re-derivation
);
await keyService.setMasterKey(masterKey);
```

**Features**:
- PBKDF2 key derivation from password (100,000 iterations)
- SHA-256 hash function
- 256-bit AES-GCM keys
- Deterministic derivation with same password and salt
- Secure salt generation (16 bytes)

**Security Notes**:
- Master key is stored in memory only, never persisted
- Service must be re-initialized after logout/restart
- Salt should be stored securely (separate from encrypted data)

#### 2. Conversation Key Derivation

**Purpose**: Derive unique encryption keys for each conversation from shared secrets.

```typescript
const sharedSecret = /* 32-byte shared secret from X25519 key exchange */;
const conversationKey = await keyService.deriveConversationKey(
  sharedSecret,
  conversationId,
  version // optional, defaults to 1
);
```

**Features**:
- HKDF-SHA256 key derivation function
- Context binding (conversation ID + version)
- Deterministic derivation (both parties get same key)
- Version support for key rotation
- 256-bit AES-GCM output keys

**Security Notes**:
- Shared secret must be 32 bytes (X25519 output)
- Each conversation gets a unique key even with same shared secret
- Different versions produce different keys

#### 3. Encrypted Key Storage

**Purpose**: Store conversation keys encrypted at rest in IndexedDB.

```typescript
// Store a conversation key
await keyService.storeConversationKey(
  conversationId,
  conversationKey,
  version // optional, defaults to 1
);

// Retrieve a conversation key
const key = await keyService.getConversationKey(conversationId);

// Get metadata without decryption
const info = await keyService.getConversationKeyInfo(conversationId);
```

**Storage Structure**:
```typescript
interface StoredConversationKey {
  conversationId: string;
  encryptedKey: string;      // Base64 encoded
  iv: string;                // Base64 encoded IV (12 bytes)
  tag: string;               // Base64 encoded auth tag (16 bytes)
  version: number;           // Current key version
  createdAt: string;         // ISO timestamp
  rotatedAt?: string;        // ISO timestamp of last rotation
}
```

**Security Notes**:
- Keys encrypted with AES-256-GCM using master key
- Random IV per encryption (12 bytes)
- Authentication tag ensures integrity (16 bytes)
- Stored in separate IndexedDB database
- Database: `TralliValli-KeyManagement`

#### 4. Key Rotation

**Purpose**: Rotate conversation keys when group membership changes or for periodic security.

```typescript
const newSharedSecret = /* new shared secret after membership change */;
const newVersion = await keyService.rotateConversationKey(
  conversationId,
  newSharedSecret,
  'member_added' // reason: 'member_added', 'member_removed', 'manual'
);
```

**Rotation Process**:
1. Get current key version (or 0 if none)
2. Derive new key with incremented version
3. Store new key (replaces old key)
4. Update rotation timestamp
5. Record rotation in history

**Rotation Reasons**:
- `member_added`: New member joined group conversation
- `member_removed`: Member left or was removed from group
- `manual`: Periodic rotation or security concern

**Rotation History**:
```typescript
const history = await keyService.getRotationHistory(conversationId);
// Returns array of KeyRotationRecord
```

#### 5. Additional Operations

```typescript
// List all conversation IDs
const ids = await keyService.getAllConversationIds();

// Delete a conversation key
await keyService.deleteConversationKey(conversationId);

// Clear all keys (use with caution)
await keyService.clearAll();

// Initialize and cleanup
await keyService.initialize();
await keyService.close();
```

## Database Schema

### IndexedDB: `TralliValli-KeyManagement` v1

**Object Stores**:

1. **conversationKeys**
   - Key Path: `conversationId`
   - Stores encrypted conversation keys
   - No indexes

2. **keyRotationHistory**
   - Key Path: `id` (auto-increment)
   - Stores rotation audit trail
   - Index: `by-conversation` on `conversationId`

## Integration Guide

### Basic Usage Flow

```typescript
import { KeyManagementService } from '@/services';

// 1. Initialize service
const keyService = new KeyManagementService();
await keyService.initialize();

// 2. Set up master key (once per session)
const { masterKey, salt } = await keyService.deriveMasterKeyFromPassword(
  userPassword
);
await keyService.setMasterKey(masterKey);
// Store salt securely for re-derivation

// 3. Derive and store conversation key
const sharedSecret = /* from X25519 key exchange */;
const conversationKey = await keyService.deriveConversationKey(
  sharedSecret,
  conversationId
);
await keyService.storeConversationKey(conversationId, conversationKey);

// 4. Retrieve conversation key for encryption/decryption
const key = await keyService.getConversationKey(conversationId);
// Use key with aesGcmEncryption service

// 5. Rotate key when needed
await keyService.rotateConversationKey(
  conversationId,
  newSharedSecret,
  'member_added'
);

// 6. Cleanup on logout
await keyService.clearAll();
await keyService.close();
```

### Integration with Existing Services

**With CryptoKeyExchange**:
```typescript
import { CryptoKeyExchange, KeyManagementService } from '@/services';

const cryptoService = new CryptoKeyExchange();
const keyService = new KeyManagementService();

await cryptoService.initialize();
await keyService.initialize();

// Derive shared secret
const myKeyPair = cryptoService.generateKeyPair();
const peerPublicKey = /* from API */;
const sharedSecret = cryptoService.deriveSharedSecret(
  myKeyPair.privateKey,
  peerPublicKey
);

// Derive and store conversation key
const conversationKey = await keyService.deriveConversationKey(
  sharedSecret,
  conversationId
);
await keyService.storeConversationKey(conversationId, conversationKey);
```

**With AES-GCM Encryption**:
```typescript
import { KeyManagementService, encrypt, decrypt } from '@/services';

const keyService = new KeyManagementService();
await keyService.initialize();
// ... set master key ...

// Get conversation key
const conversationKey = await keyService.getConversationKey(conversationId);

// Encrypt message
const encrypted = await encrypt(conversationKey, messageText);

// Decrypt message
const decrypted = await decrypt(
  conversationKey,
  encrypted.iv,
  encrypted.ciphertext,
  encrypted.tag
);
```

## Security Considerations

### Key Hierarchy

```
User Password
    ↓ (PBKDF2)
Master Key (in-memory only)
    ↓ (AES-GCM encryption)
Conversation Keys (in IndexedDB, encrypted)
    ↓ (AES-GCM encryption)
Message Content
```

### Security Best Practices

1. **Master Key**:
   - Never persisted to disk
   - Re-derived from password each session
   - Cleared on logout
   - Uses strong password hashing (PBKDF2, 100k iterations)

2. **Conversation Keys**:
   - Encrypted at rest with master key
   - Unique per conversation
   - Rotated when group membership changes
   - Derived deterministically for consistency

3. **Key Rotation**:
   - MUST rotate when member removed (forward secrecy)
   - SHOULD rotate when member added (best practice)
   - CAN rotate periodically for additional security
   - History maintained for audit trail

4. **Shared Secrets**:
   - Must be 32 bytes from X25519 key exchange
   - Should be unique per key exchange
   - Never reuse shared secrets

### Threat Model

**Protected Against**:
- ✅ Database compromise (keys encrypted at rest)
- ✅ Key reuse (unique per conversation)
- ✅ Removed member access (key rotation)
- ✅ Tampering (authentication tags)

**Not Protected Against**:
- ❌ Memory dumps while keys in use
- ❌ Compromised master password
- ❌ Side-channel attacks
- ❌ Malicious code injection

## Testing

### Test Coverage

Location: `src/web/src/services/keyManagement.test.ts`

**37 test cases covering**:
- ✅ Initialization and lifecycle
- ✅ Master key management
- ✅ Password-based key derivation
- ✅ Conversation key derivation (HKDF)
- ✅ Encrypted storage and retrieval
- ✅ Key metadata operations
- ✅ Key rotation mechanics
- ✅ Rotation history tracking
- ✅ Multiple rotation scenarios
- ✅ Error handling
- ✅ Integration with X25519
- ✅ Cleanup operations

### Running Tests

```bash
# Run key management tests only
npm test -- keyManagement.test.ts

# Run all tests
npm test
```

**Test Results**: All 37 tests passing ✅

## API Reference

### KeyManagementService

#### Methods

- `initialize(): Promise<void>` - Initialize service and database
- `close(): Promise<void>` - Close database connection
- `setMasterKey(masterKey: CryptoKey): Promise<void>` - Set master key
- `deriveMasterKeyFromPassword(password: string, salt?: Uint8Array, extractable?: boolean): Promise<{masterKey: CryptoKey, salt: Uint8Array}>` - Derive master key from password
- `deriveConversationKey(sharedSecret: Uint8Array, conversationId: string, version?: number): Promise<CryptoKey>` - Derive conversation key
- `storeConversationKey(conversationId: string, conversationKey: CryptoKey, version?: number): Promise<void>` - Store encrypted key
- `getConversationKey(conversationId: string): Promise<CryptoKey | null>` - Retrieve decrypted key
- `getConversationKeyInfo(conversationId: string): Promise<ConversationKeyInfo | null>` - Get key metadata
- `rotateConversationKey(conversationId: string, newSharedSecret: Uint8Array, reason?: string): Promise<number>` - Rotate key
- `getRotationHistory(conversationId: string): Promise<KeyRotationRecord[]>` - Get rotation history
- `deleteConversationKey(conversationId: string): Promise<void>` - Delete key
- `getAllConversationIds(): Promise<string[]>` - List all conversation IDs
- `clearAll(): Promise<void>` - Clear all data

### Types

```typescript
interface StoredConversationKey {
  conversationId: string;
  encryptedKey: string;
  iv: string;
  tag: string;
  version: number;
  createdAt: string;
  rotatedAt?: string;
}

interface KeyRotationRecord {
  id?: number;
  conversationId: string;
  oldVersion: number;
  newVersion: number;
  rotatedAt: string;
  reason: string;
}

interface ConversationKeyInfo {
  conversationId: string;
  version: number;
  createdAt: string;
  rotatedAt?: string;
}
```

## Acceptance Criteria Status

- ✅ **Per-conversation keys derived** - Using HKDF-SHA256 from shared secrets
- ✅ **Keys stored encrypted** - AES-256-GCM encryption at rest in IndexedDB
- ✅ **Key rotation working** - Full rotation mechanism with history tracking
- ✅ **Unit tests pass** - All 37 tests passing with comprehensive coverage

## Future Enhancements

Potential improvements for future iterations:

1. **Key Backup/Recovery**:
   - Encrypted key export/import
   - Recovery mechanism for lost master key

2. **Multi-Device Support**:
   - Sync conversation keys across devices
   - Device-specific encryption

3. **Key Lifecycle Management**:
   - Automatic periodic rotation
   - Key expiration policies
   - Old key archival

4. **Performance Optimizations**:
   - Key caching in memory
   - Batch operations
   - Lazy loading

5. **Enhanced Security**:
   - Hardware security module integration
   - Biometric authentication
   - Key stretching optimizations

## Conclusion

The KeyManagementService provides a complete, secure solution for managing per-conversation encryption keys in TralliValli. It integrates seamlessly with existing crypto services and follows security best practices for key derivation, storage, and rotation.
