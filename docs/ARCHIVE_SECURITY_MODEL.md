# Archive Security Model

## Overview

The TralliValli archive system provides secure, readable exports of conversation messages while maintaining end-to-end encryption principles. This document describes the security architecture, key management, and threat model for the archive functionality.

## Architecture

### Key Hierarchy

The archive system uses a three-tier key hierarchy:

```
User Master Password
    ↓ (PBKDF2-SHA256, 100k iterations)
Master Key (32 bytes)
    ↓ (AES-256-GCM encryption)
Conversation Keys (32 bytes per conversation)
    ↓ (AES-256-GCM encryption)
Message Content (encrypted)
```

### Components

#### 1. ConversationKey Entity

Conversation keys are stored separately from archived messages in the `conversationKeys` MongoDB collection:

```csharp
public class ConversationKey
{
    public string Id { get; set; }              // MongoDB ObjectId
    public string ConversationId { get; set; }  // Reference to conversation
    public string EncryptedKey { get; set; }    // Base64-encoded encrypted key
    public string Iv { get; set; }              // Base64-encoded IV (12 bytes)
    public string Salt { get; set; }            // Base64-encoded PBKDF2 salt (16 bytes)
    public string Tag { get; set; }             // Base64-encoded auth tag (16 bytes)
    public int Version { get; set; }            // Key version for rotation
    public DateTime CreatedAt { get; set; }
    public DateTime? RotatedAt { get; set; }
}
```

**Key Properties:**
- Unique index on `conversationId` ensures one key per conversation
- Keys are encrypted with the user's master key (derived from password)
- Salt is stored with each key for master key derivation
- Authentication tags ensure integrity

#### 2. MessageEncryptionService

The `MessageEncryptionService` provides cryptographic operations:

```csharp
public interface IMessageEncryptionService
{
    // Derives master key from password using PBKDF2
    Task<byte[]> DeriveMasterKeyFromPasswordAsync(string password, string salt);
    
    // Decrypts conversation key using master key
    Task<byte[]> DecryptConversationKeyAsync(string encryptedKey, byte[] masterKey, string iv, string tag);
    
    // Decrypts message content using conversation key
    Task<string> DecryptMessageAsync(string encryptedContent, byte[] conversationKey, string iv, string tag);
}
```

**Cryptographic Details:**
- **Key Derivation:** PBKDF2-SHA256 with 100,000 iterations
- **Encryption:** AES-256-GCM (Authenticated Encryption with Associated Data)
- **Key Size:** 256 bits (32 bytes)
- **IV Size:** 96 bits (12 bytes, as recommended for GCM)
- **Tag Size:** 128 bits (16 bytes)

#### 3. ArchiveService

The `ArchiveService` orchestrates the decryption process during export:

```csharp
public Task<ExportResult> ExportConversationMessagesAsync(
    string conversationId,
    DateTime startDate,
    DateTime endDate,
    string? masterPassword = null,
    CancellationToken cancellationToken = default);
```

**Export Process:**

1. **Fetch Conversation:** Retrieve conversation metadata and participants
2. **Retrieve Messages:** Get messages within the specified date range
3. **Decrypt (if password provided):**
   - Fetch conversation key from `conversationKeys` collection
   - Derive master key from password using stored salt
   - Decrypt conversation key using master key
   - Decrypt each message using conversation key
4. **Fallback:** If decryption fails or no password provided:
   - Use plain `Content` field if available
   - Fall back to `EncryptedContent` as last resort
5. **Export:** Return structured data with decrypted messages

## Security Properties

### Confidentiality

✅ **Message Content**
- Messages are encrypted with conversation-specific keys
- Conversation keys are encrypted with user's master key
- Master key is derived from password (never stored)

✅ **Key Separation**
- Conversation keys stored separately from message archives
- Different MongoDB collections ensure separation
- Keys can be backed up/restored independently

✅ **Password Protection**
- Master password required for archival with decryption
- PBKDF2 with 100k iterations prevents brute-force attacks
- Salt ensures rainbow table attacks are ineffective

### Integrity

✅ **Authenticated Encryption**
- AES-GCM provides both confidentiality and integrity
- Authentication tags prevent tampering
- Modified ciphertexts are detected and rejected

✅ **Key Versioning**
- Version field supports key rotation
- History of rotations can be tracked
- Forward secrecy when keys are rotated

### Availability

✅ **Graceful Degradation**
- Export works without master password (returns encrypted content)
- Decryption failures don't prevent export
- Plain content fallback for legacy messages

✅ **Backward Compatibility**
- Supports messages with only plain `Content` field
- Handles both encrypted and non-encrypted conversations
- Optional decryption maintains compatibility

## Threat Model

### Protected Against

✅ **Database Compromise**
- Conversation keys are encrypted at rest
- Attacker with database access cannot decrypt messages without password

✅ **Archive Theft**
- Archived messages without conversation keys are unreadable
- Keys stored separately from archives

✅ **Credential Stuffing**
- PBKDF2 with high iteration count slows brute-force attacks
- Unique salt per conversation key prevents precomputation

✅ **Man-in-the-Middle (Archive Process)**
- Authentication tags prevent tampering during decryption
- Invalid ciphertexts are rejected

### Not Protected Against

❌ **Password Compromise**
- If master password is compromised, all conversation keys can be decrypted
- Mitigation: Strong password policy, password manager usage

❌ **Client-Side Attacks**
- Keys exist in memory during decryption
- Malicious code on server could intercept keys
- Mitigation: Trusted server environment, regular security audits

❌ **Side-Channel Attacks**
- Timing attacks on cryptographic operations
- Memory dumps during decryption
- Mitigation: Use timing-safe comparison, secure memory handling

❌ **Metadata Leakage**
- Conversation participants, timestamps, message counts visible
- Mitigation: This is by design for operational needs

## Best Practices

### For Developers

1. **Never Store Master Password**
   - Derive master key, use immediately, discard
   - Never persist master key or password to disk

2. **Use Separate Storage**
   - Keep conversation keys in separate collection
   - Consider separate database for additional isolation

3. **Validate All Inputs**
   - Check Base64 encoding before decryption
   - Validate key sizes and parameters
   - Handle exceptions gracefully

4. **Audit Access**
   - Log all archive export attempts
   - Record which conversations were decrypted
   - Monitor for suspicious patterns

### For Users

1. **Strong Master Password**
   - Use unique, strong password for archives
   - Consider passphrase (e.g., 6+ random words)
   - Use password manager

2. **Secure Password Entry**
   - Only enter password when creating archives
   - Use HTTPS for web interface
   - Clear clipboard after use

3. **Key Backup**
   - Export conversation keys separately from archives
   - Store keys in secure location (password manager, hardware token)
   - Test recovery process periodically

4. **Archive Storage**
   - Store archives separately from keys
   - Encrypt storage media (full-disk encryption)
   - Implement secure deletion when no longer needed

## Implementation Notes

### Message Encrypted Content Format

Messages store encrypted content in the `EncryptedContent` field using the format:

```
base64(iv):base64(tag):base64(ciphertext)
```

Components:
- **IV (96 bits):** Initialization vector for AES-GCM
- **Tag (128 bits):** Authentication tag for integrity
- **Ciphertext:** Encrypted message content

### Key Rotation

Key rotation is supported through the `Version` field:

```csharp
public class ConversationKey
{
    public int Version { get; set; }        // Current version
    public DateTime? RotatedAt { get; set; } // Last rotation timestamp
}
```

**Rotation Process:**
1. Generate new conversation key (increment version)
2. Re-encrypt old messages with new key (optional)
3. Store new key with incremented version
4. Update `RotatedAt` timestamp

**When to Rotate:**
- Member removed from group (forward secrecy)
- Member added to group (best practice)
- Periodic rotation (e.g., every 90 days)
- Suspected key compromise

### Database Indexes

Required indexes for optimal performance:

```javascript
// Conversation keys: unique index on conversationId
db.conversationKeys.createIndex({ "conversationId": 1 }, { unique: true });

// Messages: compound index for efficient queries
db.messages.createIndex({ "conversationId": 1, "createdAt": -1 });
```

## Future Enhancements

### Short-Term (Phase 5)

1. **Client-Side Archive Creation**
   - Move decryption to client for better security
   - Server never sees master password or decrypted keys

2. **Key Rotation Automation**
   - Automatic rotation on membership changes
   - Background re-encryption of old messages

3. **Enhanced Audit Logging**
   - Detailed logs of all decryption operations
   - Alerts for suspicious activity

### Long-Term (Phase 6+)

1. **Hardware Security Module (HSM) Integration**
   - Store master keys in HSM
   - Cryptographic operations in secure enclave

2. **Multi-Factor Authentication for Archives**
   - Require 2FA for archive creation
   - Hardware token for key access

3. **Post-Quantum Cryptography**
   - Migrate to quantum-resistant algorithms
   - Hybrid classical/post-quantum encryption

4. **Zero-Knowledge Proof Archives**
   - Prove archive integrity without decryption
   - Selective disclosure of specific messages

## Compliance

### GDPR Considerations

- ✅ **Right to Erasure:** Keys can be deleted independently
- ✅ **Data Portability:** Structured JSON export format
- ✅ **Data Minimization:** Only necessary fields exported
- ✅ **Encryption at Rest:** All keys encrypted in database

### Industry Standards

- ✅ **NIST SP 800-132:** PBKDF2 configuration compliant
- ✅ **NIST SP 800-38D:** AES-GCM usage compliant
- ✅ **FIPS 140-2:** Uses approved algorithms
- ⚠️ **FIPS 140-2 Level 2+:** Requires HSM integration (future)

## Conclusion

The TralliValli archive security model provides strong protection for exported messages through:

1. **Layered Encryption:** Password → Master Key → Conversation Keys → Messages
2. **Key Separation:** Keys stored separately from archives
3. **Authenticated Encryption:** AES-GCM prevents tampering
4. **Graceful Degradation:** Works with or without decryption

The system is designed to be secure by default while maintaining usability and backward compatibility. Regular security audits and adherence to best practices are essential for maintaining the security posture.

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-30  
**Maintained By:** TralliValli Security Team
