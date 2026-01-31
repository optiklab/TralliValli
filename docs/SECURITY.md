# TralliValli Security Documentation

## Table of Contents

- [Overview](#overview)
- [Authentication Flow](#authentication-flow)
- [JWT Token Lifecycle](#jwt-token-lifecycle)
- [End-to-End Encryption (E2EE)](#end-to-end-encryption-e2ee)
- [Key Management](#key-management)
- [Data Encryption at Rest](#data-encryption-at-rest)
- [Network Security](#network-security)
- [Security Best Practices for Deployment](#security-best-practices-for-deployment)
- [Security Compliance](#security-compliance)
- [Reporting Security Issues](#reporting-security-issues)

---

## Overview

TralliValli implements a comprehensive security model that combines server-side authentication with client-side end-to-end encryption. This dual-layer approach ensures:

1. **Server Security**: JWT-based authentication and authorization
2. **Client Security**: AES-256-GCM end-to-end encryption for message content
3. **Zero-Knowledge Architecture**: Server cannot decrypt message content

This document describes the security architecture, implementations, and deployment best practices for the TralliValli messaging platform.

---

## Authentication Flow

TralliValli uses a passwordless authentication system based on magic links and JWT tokens.

### Magic Link Authentication

**Location**: `src/TraliVali.Api/Controllers/AuthController.cs`

#### Authentication Process

1. **Request Magic Link**
   ```
   POST /api/auth/request-magic-link
   Body: { "email": "user@example.com" }
   ```
   - System generates a cryptographically secure token using `RandomNumberGenerator.GetBytes()`
   - Token stored in Redis with 15-minute expiration
   - Magic link sent to user's email address
   - Format: `https://app.example.com/verify?token={token}`

2. **Verify Magic Link**
   ```
   POST /api/auth/verify-magic-link
   Body: { "token": "secure-token" }
   ```
   - System validates token against Redis store
   - On success, creates or retrieves user account
   - Issues JWT access and refresh tokens
   - Token removed from Redis after use (single-use only)

3. **Active Session**
   - User receives JWT tokens for subsequent API requests
   - Frontend stores tokens securely (localStorage with expiry management)
   - All API requests authenticated via Bearer token

#### Security Features

- **No Password Storage**: System never stores user passwords
- **Time-Limited Tokens**: Magic links expire after 15 minutes
- **Single-Use Tokens**: Each magic link can only be used once
- **Cryptographically Secure**: Uses .NET's `RandomNumberGenerator` for token generation
- **Email Verification**: Validates email ownership as part of authentication

#### Frontend Implementation

**Location**: 
- `src/web/src/components/LoginPage.tsx` - Login interface
- `src/web/src/components/VerifyMagicLink.tsx` - Magic link verification

### User Registration Flow

1. User requests magic link with email
2. System checks if user exists
3. If new user: creates account with unique ID
4. If existing user: retrieves account
5. JWT tokens issued for authenticated session

---

## JWT Token Lifecycle

TralliValli uses JSON Web Tokens (JWT) with RSA-256 asymmetric signing for secure, stateless authentication.

### Token Generation

**Location**: `src/TraliVali.Auth/JwtService.cs`

#### Token Configuration

- **Algorithm**: RSA-256 (asymmetric cryptography)
- **Private Key**: RSA private key from environment variable `JWT_PRIVATE_KEY`
- **Public Key**: RSA public key from environment variable `JWT_PUBLIC_KEY`
- **Access Token Expiry**: 7 days (configurable via `Jwt:ExpirationDays`)
- **Refresh Token Expiry**: 30 days (configurable via `Jwt:RefreshTokenExpirationDays`)

#### Token Structure

Access tokens include the following claims:
```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "jti": "unique-token-id",
  "exp": 1234567890,
  "iss": "TralliValli",
  "aud": "TralliValli"
}
```

### Token Validation

**Location**: `src/TraliVali.Api/Program.cs` (lines 78-100)

#### Validation Parameters

The system validates all of the following:
- **Signature**: Verified using RSA public key
- **Issuer**: Must match configured issuer ("TralliValli")
- **Audience**: Must match configured audience ("TralliValli")
- **Expiration**: Token must not be expired
- **Not Before**: Token must be valid for current time

#### Token Refresh Flow

1. **Request Token Refresh**
   ```
   POST /api/auth/refresh
   Body: { "refreshToken": "valid-refresh-token" }
   ```
   - System validates refresh token
   - Checks token is not blacklisted
   - Issues new access token and refresh token
   - Old refresh token blacklisted to prevent reuse

2. **Automatic Refresh** (Frontend)
   - **Location**: `src/web/src/utils/tokenStorage.ts`
   - Frontend monitors token expiry
   - Automatically refreshes tokens 60 seconds before expiration
   - Uses API interceptor for seamless token refresh

### Token Blacklisting

**Location**: `src/TraliVali.Auth/TokenBlacklistService.cs`

#### Implementation Details

- **Storage**: Redis-based blacklist for performance
- **Hash Function**: SHA256 hash of tokens to reduce storage size
- **Expiration**: Blacklist entries expire after token's natural expiration
- **Use Cases**:
  - User logout (blacklists current tokens)
  - Token refresh (blacklists old refresh token)
  - Account suspension (blacklists all user tokens)

#### Logout Process

```
POST /api/auth/logout
Authorization: Bearer {access-token}
Body: { "refreshToken": "current-refresh-token" }
```
- Both access and refresh tokens added to blacklist
- Tokens remain blacklisted until natural expiration
- User must re-authenticate to obtain new tokens

### Token Security Best Practices

1. **Key Management**
   - RSA keys generated offline, never in application code
   - Private key secured in environment variables or Azure Key Vault
   - Public key can be distributed (used only for verification)
   - Keys rotated periodically (recommended: annually)

2. **Token Storage**
   - Frontend: localStorage with expiry tracking
   - Never log token values
   - Clear tokens on logout
   - Production recommendation: Use httpOnly cookies for enhanced security

3. **Token Validation**
   - Validate on every request
   - Check blacklist before accepting token
   - Reject tokens with invalid claims
   - Implement rate limiting (see Network Security section)

---

## End-to-End Encryption (E2EE)

TralliValli implements client-side end-to-end encryption, ensuring that message content is encrypted before transmission and can only be decrypted by intended recipients.

### Encryption Architecture

**Location**: `src/web/src/services/` (frontend encryption suite)

#### Core Components

1. **aesGcmEncryption.ts** - Core AES-256-GCM encryption engine
2. **messageEncryption.ts** - Message-specific encryption/decryption
3. **keyManagement.ts** - Per-conversation key management
4. **cryptoKeyExchange.ts** - Key exchange protocol
5. **fileEncryption.ts** - File and attachment encryption

### Encryption Algorithm

**Cipher**: AES-256-GCM (Advanced Encryption Standard with Galois/Counter Mode)

#### Algorithm Properties

- **Key Size**: 256 bits (32 bytes)
- **IV Size**: 96 bits (12 bytes) - randomly generated per message
- **Authentication Tag**: 128 bits (16 bytes) - ensures message integrity
- **Mode**: GCM (provides both confidentiality and authenticity)

#### Why AES-256-GCM?

- **AEAD**: Authenticated Encryption with Associated Data
- **Performance**: Hardware-accelerated on modern CPUs
- **Security**: NIST recommended, FIPS 140-2 compliant
- **Integrity**: Built-in authentication prevents tampering
- **Standards**: Compliant with NIST SP 800-38D

### Message Encryption Process

#### Encryption Flow

1. **Message Creation**
   - User composes message in client application
   - Message content remains in plaintext in client memory

2. **Key Selection**
   - System retrieves or generates conversation-specific key
   - Each conversation has unique symmetric encryption key

3. **Encryption**
   ```typescript
   // Simplified encryption flow
   const iv = crypto.getRandomValues(new Uint8Array(12)); // Random IV
   const ciphertext = await crypto.subtle.encrypt(
     { name: 'AES-GCM', iv: iv, tagLength: 128 },
     conversationKey,
     messageBytes
   );
   ```

4. **Transmission**
   - Encrypted message sent to server
   - Format: `base64(iv):base64(tag):base64(ciphertext)`
   - Server stores encrypted message without ability to decrypt

5. **Recipient Decryption**
   - Recipient retrieves encrypted message
   - Loads conversation key from secure storage
   - Decrypts message locally in browser
   - Plaintext never transmitted over network

#### Decryption Flow

```typescript
// Simplified decryption flow
const plaintext = await crypto.subtle.decrypt(
  { name: 'AES-GCM', iv: iv, tagLength: 128 },
  conversationKey,
  ciphertext
);
```

### File Encryption

**Location**: `src/web/src/services/fileEncryption.ts`

Files and attachments follow the same encryption model:
- Files encrypted client-side before upload
- Same AES-256-GCM algorithm used
- Encrypted files stored on server (Azure Blob Storage)
- Decryption occurs client-side upon download
- Supports all file types and sizes

### E2EE Security Properties

#### Zero-Knowledge Architecture

- **Server Cannot Decrypt**: Server lacks conversation keys
- **Database Compromise**: Even if database is breached, messages remain encrypted
- **Transport Security**: Messages encrypted before HTTPS layer
- **Forward Secrecy**: Implemented through key rotation

#### Attack Surface Mitigation

1. **Man-in-the-Middle**: Prevented by key exchange protocol + HTTPS
2. **Server Compromise**: Messages remain encrypted at rest
3. **Database Breach**: Keys stored separately, encrypted with user password
4. **Replay Attacks**: Prevented by message IDs and timestamps
5. **Message Tampering**: Prevented by GCM authentication tags

### Limitations and Considerations

1. **Client-Side Security**: E2EE security depends on client device security
2. **Metadata**: Server can observe conversation participants and timing (not content)
3. **Browser Security**: Users must trust their web browser
4. **Password Recovery**: Lost master passwords cannot be recovered (by design)
5. **Multi-Device**: Requires key synchronization across devices

---

## Key Management

TralliValli implements a hierarchical key management system with three tiers of encryption keys.

### Key Hierarchy

**Location**: `src/web/src/services/keyManagement.ts`, `src/TraliVali.Auth/BackupService.cs`

```
User Master Password
    ↓ (PBKDF2-SHA256, 100,000 iterations)
Master Key (32 bytes)
    ↓ (AES-256-GCM encryption)
Conversation Keys (32 bytes per conversation)
    ↓ (AES-256-GCM encryption)
Message Content (encrypted)
```

### Tier 1: Master Password & Master Key

#### Master Password
- User-selected password (minimum 8 characters recommended)
- Never transmitted to server
- Stored only in user's memory/password manager
- Used solely for key derivation

#### Master Key Derivation

**Algorithm**: PBKDF2-SHA256

```typescript
const masterKey = await crypto.subtle.deriveKey(
  {
    name: 'PBKDF2',
    salt: salt,        // 16 bytes, randomly generated
    iterations: 100000, // NIST SP 800-132 compliant
    hash: 'SHA-256'
  },
  passwordKey,
  { name: 'AES-GCM', length: 256 },
  false,
  ['encrypt', 'decrypt']
);
```

**Security Parameters**:
- **Iterations**: 100,000 (compliant with NIST SP 800-132)
- **Salt**: 16 bytes, unique per user, stored with encrypted keys
- **Output**: 256-bit AES key
- **Protection**: Resistant to brute-force attacks

### Tier 2: Conversation Keys

#### Key Generation

Each conversation has a unique 256-bit symmetric key:

```typescript
const conversationKey = await crypto.subtle.generateKey(
  { name: 'AES-GCM', length: 256 },
  true,
  ['encrypt', 'decrypt']
);
```

#### Key Storage

**Database**: MongoDB `conversationKeys` collection

```csharp
public class ConversationKey
{
    public string Id { get; set; }              // MongoDB ObjectId
    public string ConversationId { get; set; }  // Reference to conversation
    public string EncryptedKey { get; set; }    // Encrypted with master key
    public string Iv { get; set; }              // 12 bytes for encryption
    public string Salt { get; set; }            // 16 bytes for PBKDF2
    public string Tag { get; set; }             // 16 bytes authentication
    public int Version { get; set; }            // Key rotation support
    public DateTime CreatedAt { get; set; }
    public DateTime? RotatedAt { get; set; }
}
```

**Key Properties**:
- Unique index on `conversationId`
- Encrypted with user's master key
- Stored separately from message content
- Supports versioning for key rotation

#### Key Encryption Format

```
Format: base64(iv):base64(tag):base64(ciphertext)
Example: "r3KjNm8pQWxY...==:vT9kLp2M...==:xH4nPq7W...=="
```

### Tier 3: Message Content

Messages encrypted with conversation keys:
- Each message uses unique IV (never reused)
- Format: `base64(iv):base64(tag):base64(ciphertext)`
- Stored in MongoDB `encryptedMessages` collection
- Indexed by `conversationId` and `createdAt`

### Key Rotation

**Purpose**: Limit exposure from potential key compromise

#### Rotation Triggers

1. **Scheduled Rotation**: Recommended every 90 days
2. **Security Event**: Suspected compromise
3. **User Request**: Manual rotation
4. **Membership Change**: When user leaves conversation (optional)

#### Rotation Process

1. Generate new conversation key (version N+1)
2. Encrypt new key with master key
3. Store new key with incremented version
4. New messages encrypted with new key
5. Old messages remain encrypted with old key (no re-encryption)
6. Old key marked as rotated with `RotatedAt` timestamp

### Key Backup and Recovery

**Location**: 
- Backend: `src/TraliVali.Auth/BackupService.cs`
- Frontend: `src/web/src/services/keyBackup.ts`

#### Backup Features

1. **Encrypted Key Backup**
   - All conversation keys exported as encrypted bundle
   - Bundle encrypted with master password
   - Stored in secure location (Azure Blob Storage)

2. **Recovery Process**
   - User enters master password
   - System derives master key
   - Decrypts conversation keys
   - Restores keys to IndexedDB

3. **Multi-Device Sync**
   - Same master password across devices
   - Keys synchronized via encrypted backup
   - Each device derives master key locally

### Key Security Best Practices

1. **Master Password**
   - Use strong, unique password
   - Store in password manager
   - Never share with others
   - Change periodically (annually recommended)

2. **Key Storage**
   - Frontend: IndexedDB (browser secure storage)
   - Backend: Encrypted in MongoDB
   - Never log keys
   - Clear keys on logout

3. **Key Rotation**
   - Rotate keys after security events
   - Regular rotation schedule (90 days)
   - Monitor key age and usage

4. **Backup**
   - Regular key backups
   - Store backup securely (encrypted)
   - Test recovery process
   - Multiple backup locations

---

## Data Encryption at Rest

TralliValli implements multiple layers of encryption for data at rest, ensuring protection even in the event of database compromise.

### Database Encryption

**Database**: MongoDB (configurable storage backend)

#### Encrypted Collections

1. **encryptedMessages**
   - Contains encrypted message content
   - Schema:
     ```csharp
     {
       "id": "ObjectId",
       "conversationId": "string",
       "encryptedContent": "string",  // base64(iv):base64(tag):base64(ciphertext)
       "senderId": "string",
       "createdAt": "DateTime"
     }
     ```
   - Index: `conversationId` + `createdAt` for efficient queries
   - Content encrypted with conversation key (client-side)

2. **conversationKeys**
   - Contains encrypted conversation keys
   - Keys encrypted with user's master key
   - Stored separately from message content
   - See [Key Management](#key-management) section for schema

#### Encryption Format

All encrypted fields use consistent format:
```
base64(iv):base64(tag):base64(ciphertext)
```

**Components**:
- **IV (Initialization Vector)**: 12 bytes, random, unique per encryption
- **Tag (Authentication Tag)**: 16 bytes, ensures data integrity
- **Ciphertext**: Variable length, actual encrypted data

### Storage Architecture

#### Separation of Concerns

1. **Message Content**: Encrypted, stored in `encryptedMessages`
2. **Conversation Keys**: Encrypted separately, stored in `conversationKeys`
3. **User Metadata**: Unencrypted (usernames, email, timestamps)
4. **Group Membership**: Unencrypted (required for access control)

**Security Benefit**: Even with database access, attacker needs:
- Encrypted messages (in database)
- Encrypted conversation keys (in database)
- User's master password (not in database)

### File Storage Encryption

**Backend**: Azure Blob Storage (configurable)

#### File Encryption

1. **Client-Side Encryption**
   - Files encrypted in browser before upload
   - Uses same AES-256-GCM algorithm as messages
   - Encrypted with conversation key

2. **Upload Process**
   ```
   [File] → [Encrypt Client-Side] → [Upload Encrypted] → [Azure Blob Storage]
   ```

3. **Download Process**
   ```
   [Azure Blob Storage] → [Download Encrypted] → [Decrypt Client-Side] → [File]
   ```

4. **Storage Configuration**
   - Azure Blob Storage (recommended for production)
   - Private container access only
   - Encrypted blobs with conversation-specific keys
   - See: `docs/AZURE_BLOB_STORAGE_CONFIGURATION.md`

### Backup Encryption

**Location**: `src/TraliVali.Auth/BackupService.cs`

#### Backup Types

1. **Key Backups**
   - All conversation keys bundled
   - Encrypted with master password
   - Stored in Azure Blob Storage
   - Format: JSON with encrypted key bundle

2. **Message Archives**
   - Export includes encrypted messages
   - Optional decryption with master password
   - Archive can be stored separately from keys
   - See: `docs/ARCHIVE_SECURITY_MODEL.md`

#### Archive Security Model

Detailed documentation available in `docs/ARCHIVE_SECURITY_MODEL.md`:
- Comprehensive threat model analysis
- Key hierarchy implementation
- Compliance with security standards
- Recovery procedures
- Graceful degradation for lost passwords

### MongoDB Security Configuration

#### Production Recommendations

1. **Enable MongoDB Encryption at Rest**
   ```yaml
   security:
     enableEncryption: true
     encryptionKeyFile: /path/to/keyfile
   ```

2. **Enable Authentication**
   ```yaml
   security:
     authorization: enabled
   ```

3. **Network Isolation**
   - MongoDB accessible only from application servers
   - Use VPN or private network
   - Firewall rules restricting access

4. **Backup Encryption**
   - Encrypt MongoDB backups
   - Store in secure location
   - Test restore procedures

### Data at Rest Security Best Practices

1. **Layered Security**
   - Application-level encryption (E2EE)
   - Database-level encryption (MongoDB)
   - Storage-level encryption (Azure Storage)
   - Operating system encryption (BitLocker/LUKS)

2. **Key Management**
   - Separate key storage from data
   - Use hardware security modules (HSM) for production
   - Regular key rotation
   - Secure key backup procedures

3. **Access Control**
   - Principle of least privilege
   - Role-based access control
   - Audit logging of data access
   - Regular access reviews

4. **Monitoring**
   - Monitor database access patterns
   - Alert on unusual queries
   - Log encryption operations
   - Regular security audits

---

## Network Security

TralliValli implements multiple layers of network security to protect data in transit and prevent unauthorized access.

### Transport Layer Security (TLS/HTTPS)

**Configuration**: `src/TraliVali.Api/Program.cs`

#### HTTPS Configuration

```csharp
// Development
ASPNETCORE_URLS=https://+:5001;http://+:5000
ASPNETCORE_HTTPS_PORT=5001

// Production
ASPNETCORE_URLS=https://+:443;http://+:80
```

#### Certificate Management

**See**: `docs/SSL_CONFIGURATION.md` for comprehensive guide

**Options**:
1. **Azure Container Apps Managed Certificates** (Recommended)
   - Automatic certificate provisioning
   - Automatic renewal
   - Free Let's Encrypt certificates

2. **Caddy Reverse Proxy**
   - Automatic Let's Encrypt integration
   - Zero-configuration HTTPS

3. **Manual Certbot + Nginx**
   - Full control over certificates
   - Manual renewal process

#### TLS Best Practices

1. **Use TLS 1.2 or Higher**
   ```csharp
   builder.Services.Configure<KestrelServerOptions>(options =>
   {
       options.ConfigureHttpsDefaults(https =>
       {
           https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
       });
   });
   ```

2. **Strong Cipher Suites**
   - Disable weak ciphers (RC4, DES, 3DES)
   - Prefer AEAD ciphers (AES-GCM, ChaCha20-Poly1305)
   - Forward secrecy with ECDHE

3. **HSTS (HTTP Strict Transport Security)**
   ```csharp
   app.UseHsts();
   ```

### API Security

**Location**: `src/TraliVali.Api/Controllers/`

#### Authentication Middleware

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new RsaSecurityKey(rsaPublic)
        };
    });
```

#### Authorization

All protected endpoints require JWT authentication:
```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    // Protected endpoints
}
```

### CORS (Cross-Origin Resource Sharing)

**Configuration**: SignalR CORS policy

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRCorsPolicy", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

**Production Configuration**:
- Restrict origins to specific domains
- Never use `AllowAnyOrigin()` with `AllowCredentials()`
- Validate origin headers

### Rate Limiting

**Status**: ⚠️ TODO (noted in `AuthController.cs:56-59`)

#### Recommended Implementation

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

#### Rate Limiting Strategy

1. **Authentication Endpoints**
   - Magic link requests: 5 per 15 minutes per email
   - Login attempts: 10 per 15 minutes per IP
   - Token refresh: 20 per hour per user

2. **API Endpoints**
   - General API: 1000 requests per hour per user
   - Message sending: 100 per minute per user
   - File uploads: 10 per minute per user

3. **WebSocket Connections**
   - SignalR connections: 5 concurrent per user
   - Message rate: 60 messages per minute

### SignalR Security

**Location**: `src/TraliVali.Api/Hubs/ChatHub.cs`

#### Real-Time Messaging Security

1. **Authentication Required**
   ```csharp
   [Authorize]
   public class ChatHub : Hub
   {
       // All hub methods require authentication
   }
   ```

2. **Connection Authorization**
   - JWT token validated before connection established
   - User identity verified from token claims
   - Connections rejected for invalid/expired tokens

3. **Message Authorization**
   - Verify sender is member of conversation
   - Validate conversation access permissions
   - Enforce group membership rules

4. **Rate Limiting**
   - Limit messages per connection
   - Disconnect abusive clients
   - Monitor connection patterns

### Security Headers

#### Recommended Headers

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    
    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self'; " +
        "connect-src 'self' wss://yourdomain.com;"
    );
    
    await next();
});
```

### Network Security Best Practices

1. **Use HTTPS Everywhere**
   - All traffic over TLS 1.2+
   - Redirect HTTP to HTTPS
   - Enable HSTS

2. **Implement Rate Limiting**
   - Protect against DoS attacks
   - Limit API abuse
   - Monitor rate limit violations

3. **Security Headers**
   - CSP to prevent XSS
   - X-Frame-Options to prevent clickjacking
   - HSTS for transport security

4. **API Security**
   - JWT authentication on all endpoints
   - Input validation and sanitization
   - Output encoding

5. **Monitoring and Logging**
   - Log all authentication events
   - Monitor suspicious activity
   - Alert on security events

---

## Security Best Practices for Deployment

This section provides comprehensive security guidance for deploying TralliValli in production environments.

### Pre-Deployment Checklist

#### Environment Configuration

1. **Secrets Management**
   - [ ] Never commit secrets to source control
   - [ ] Use Azure Key Vault or similar for production secrets
   - [ ] Rotate secrets regularly (quarterly recommended)
   - [ ] Use different secrets for each environment

2. **JWT Configuration**
   - [ ] Generate strong RSA-2048 or RSA-4096 key pairs
   - [ ] Store private key in Key Vault
   - [ ] Configure appropriate token expiration times
   - [ ] Set up token blacklist with Redis

3. **Database Security**
   - [ ] Enable MongoDB authentication
   - [ ] Use strong database passwords (20+ characters)
   - [ ] Enable encryption at rest
   - [ ] Configure network isolation
   - [ ] Set up automated backups
   - [ ] Test backup restoration

4. **HTTPS/TLS**
   - [ ] Configure valid SSL/TLS certificates
   - [ ] Enable HSTS
   - [ ] Disable TLS 1.0 and 1.1
   - [ ] Configure strong cipher suites
   - [ ] Set up automatic certificate renewal

#### Application Security

5. **Rate Limiting**
   - [ ] Implement rate limiting on all endpoints
   - [ ] Configure appropriate limits per endpoint type
   - [ ] Set up monitoring for rate limit violations

6. **CORS Configuration**
   - [ ] Restrict CORS to specific domains
   - [ ] Never use wildcards in production
   - [ ] Validate origin headers

7. **Security Headers**
   - [ ] Configure Content Security Policy
   - [ ] Enable X-Content-Type-Options
   - [ ] Set X-Frame-Options to DENY
   - [ ] Configure Referrer-Policy

8. **Logging and Monitoring**
   - [ ] Enable structured logging (Serilog)
   - [ ] Log authentication events
   - [ ] Monitor failed login attempts
   - [ ] Set up alerts for security events
   - [ ] Configure log retention policies

### Azure Deployment Security

**See**: `docs/DEPLOYMENT.md` for complete deployment guide

#### Container Apps Security

1. **Networking**
   ```bash
   # Deploy in private virtual network
   az containerapp env create \
     --name $ENVIRONMENT \
     --resource-group $RESOURCE_GROUP \
     --location $LOCATION \
     --internal-only true
   ```

2. **Managed Identity**
   ```bash
   # Enable managed identity for Key Vault access
   az containerapp identity assign \
     --name $APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --system-assigned
   ```

3. **Secrets Management**
   ```bash
   # Reference Key Vault secrets
   az containerapp secret set \
     --name $APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --secrets "jwt-private-key=keyvaultref:$KEY_VAULT_URL/secrets/jwt-private-key"
   ```

#### Azure Key Vault Configuration

1. **Create Key Vault**
   ```bash
   az keyvault create \
     --name $KEY_VAULT_NAME \
     --resource-group $RESOURCE_GROUP \
     --location $LOCATION \
     --enable-rbac-authorization true
   ```

2. **Store Secrets**
   ```bash
   # JWT Private Key
   az keyvault secret set \
     --vault-name $KEY_VAULT_NAME \
     --name jwt-private-key \
     --value "$JWT_PRIVATE_KEY"
   
   # MongoDB Connection String
   az keyvault secret set \
     --vault-name $KEY_VAULT_NAME \
     --name mongodb-connection \
     --value "$MONGODB_CONNECTION"
   ```

3. **Grant Access**
   ```bash
   # Grant Container App access to Key Vault
   az keyvault set-policy \
     --name $KEY_VAULT_NAME \
     --object-id $MANAGED_IDENTITY_ID \
     --secret-permissions get list
   ```

### Self-Hosted Deployment Security

#### Docker Compose Security

1. **Network Isolation**
   ```yaml
   # docker-compose.yml
   services:
     api:
       networks:
         - internal
     
     mongodb:
       networks:
         - internal
     
   networks:
     internal:
       driver: bridge
       internal: true
   ```

2. **Secrets Management**
   ```yaml
   # Use Docker secrets
   services:
     api:
       secrets:
         - jwt_private_key
         - mongodb_connection
   
   secrets:
     jwt_private_key:
       file: ./secrets/jwt_private_key.txt
     mongodb_connection:
       file: ./secrets/mongodb_connection.txt
   ```

3. **Resource Limits**
   ```yaml
   services:
     api:
       deploy:
         resources:
           limits:
             cpus: '2'
             memory: 2G
           reservations:
             cpus: '0.5'
             memory: 512M
   ```

#### Nginx Reverse Proxy

```nginx
# /etc/nginx/sites-available/tralivali

# Rate limiting zones
limit_req_zone $binary_remote_addr zone=login:10m rate=5r/m;
limit_req_zone $binary_remote_addr zone=api:10m rate=100r/m;

server {
    listen 443 ssl http2;
    server_name yourdomain.com;
    
    # SSL Configuration
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # Security Headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Content-Security-Policy "default-src 'self';" always;
    
    # Rate limiting
    location /api/auth/ {
        limit_req zone=login burst=3 nodelay;
        proxy_pass http://localhost:5000;
    }
    
    location /api/ {
        limit_req zone=api burst=20 nodelay;
        proxy_pass http://localhost:5000;
    }
    
    # WebSocket support
    location /hub/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
}
```

### Monitoring and Incident Response

#### Security Monitoring

1. **Application Logging**
   ```csharp
   // Log security events
   _logger.LogWarning("Failed login attempt for {Email} from {IP}", 
       email, httpContext.Connection.RemoteIpAddress);
   
   _logger.LogInformation("User {UserId} logged in from {IP}",
       userId, httpContext.Connection.RemoteIpAddress);
   ```

2. **Azure Monitor** (for Azure deployments)
   - Configure Application Insights
   - Set up alerts for failed authentications
   - Monitor rate limit violations
   - Track API response times

3. **Log Analysis**
   - Centralize logs (Azure Log Analytics, ELK stack)
   - Create dashboards for security metrics
   - Set up automated anomaly detection

#### Incident Response Plan

1. **Security Incident Detection**
   - Failed login spike (>100 in 5 minutes)
   - Unusual API access patterns
   - Database connection anomalies
   - Certificate expiration warnings

2. **Response Procedures**
   - [ ] Identify scope of incident
   - [ ] Isolate affected systems
   - [ ] Preserve evidence (logs, database snapshots)
   - [ ] Notify stakeholders
   - [ ] Implement fix/mitigation
   - [ ] Test fix in staging
   - [ ] Deploy to production
   - [ ] Monitor for recurrence
   - [ ] Post-incident review

3. **Contact Information**
   - Maintain security contact list
   - Document escalation procedures
   - Keep contact information current

### Security Maintenance

#### Regular Security Tasks

**Daily**:
- Monitor logs for suspicious activity
- Review failed authentication attempts
- Check system resource usage

**Weekly**:
- Review new security vulnerabilities (CVEs)
- Check certificate expiration dates
- Verify backup completion

**Monthly**:
- Review access permissions
- Audit user accounts
- Update dependencies
- Security testing (penetration testing)

**Quarterly**:
- Rotate secrets and keys
- Review and update firewall rules
- Security training for team
- Review incident response plan

**Annually**:
- Comprehensive security audit
- Rotate JWT signing keys
- Review and update security policies
- Third-party security assessment

### Dependency Management

1. **Keep Dependencies Updated**
   ```bash
   # .NET dependencies
   dotnet list package --outdated
   dotnet add package [PackageName] --version [Version]
   
   # NPM dependencies (frontend)
   npm audit
   npm update
   ```

2. **Security Scanning**
   - Enable GitHub Dependabot
   - Configure automated security updates
   - Review and apply security patches promptly

3. **Vulnerability Monitoring**
   - Subscribe to security advisories
   - Monitor CVE databases
   - Set up automated scanning (Snyk, WhiteSource)

### Backup and Disaster Recovery

#### Backup Strategy

1. **Database Backups**
   ```bash
   # Automated MongoDB backups
   mongodump --uri="mongodb://..." --out=/backups/$(date +%Y%m%d)
   ```
   - Frequency: Daily
   - Retention: 30 days
   - Encryption: Required
   - Storage: Off-site (Azure Blob Storage)

2. **Key Backups**
   - Backup JWT keys securely
   - Store in multiple locations
   - Encrypt with strong passphrase
   - Document recovery procedures

3. **Configuration Backups**
   - Version control all configuration
   - Backup environment variables
   - Document infrastructure as code

#### Disaster Recovery

1. **Recovery Time Objective (RTO)**: < 4 hours
2. **Recovery Point Objective (RPO)**: < 1 hour
3. **Recovery Procedures**:
   - Document step-by-step recovery
   - Test recovery quarterly
   - Maintain offline copies of procedures

### Compliance and Auditing

#### Compliance Requirements

- **GDPR**: User data privacy and right to be forgotten
- **HIPAA**: Healthcare data protection (if applicable)
- **SOC 2**: Security and availability controls
- **NIST**: Cryptographic algorithm compliance

#### Audit Logging

1. **Events to Log**
   - User authentication (success/failure)
   - User authorization changes
   - Data access and modifications
   - Configuration changes
   - Security events

2. **Log Retention**
   - Production logs: 90 days online, 1 year archived
   - Security logs: 1 year online, 7 years archived
   - Comply with regulatory requirements

3. **Log Protection**
   - Write-only log access for applications
   - Encrypted log storage
   - Tamper-evident logging
   - Regular log integrity checks

---

## Security Compliance

TralliValli is designed to comply with modern security standards and cryptographic best practices.

### Cryptographic Standards

#### NIST Compliance

1. **NIST SP 800-132** (Password-Based Key Derivation)
   - PBKDF2-SHA256 with 100,000 iterations
   - Compliant with NIST recommendations for password-based KDFs

2. **NIST SP 800-38D** (GCM Mode)
   - AES-256-GCM for authenticated encryption
   - 96-bit IV, 128-bit authentication tag
   - Compliant with NIST recommendations

3. **FIPS 140-2** (Cryptographic Module)
   - AES-256: FIPS 140-2 validated algorithm
   - SHA-256: FIPS 140-2 validated algorithm
   - RSA-2048: FIPS 140-2 validated algorithm

#### Algorithm Selection

| Purpose | Algorithm | Key Size | Standard |
|---------|-----------|----------|----------|
| Message Encryption | AES-GCM | 256 bits | NIST SP 800-38D |
| Key Derivation | PBKDF2-SHA256 | 256 bits | NIST SP 800-132 |
| JWT Signing | RSA | 2048+ bits | FIPS 186-4 |
| Hashing | SHA-256 | 256 bits | FIPS 180-4 |
| Random Generation | CSPRNG | N/A | NIST SP 800-90A |

### Data Protection Regulations

#### GDPR Compliance

1. **Data Minimization**
   - Only necessary user data collected (email, username)
   - No tracking or analytics without consent

2. **Right to Access**
   - Users can export all their data
   - Archive functionality provides data portability

3. **Right to Erasure**
   - Account deletion removes user data
   - Messages deleted from database
   - Conversation keys removed

4. **Data Security**
   - End-to-end encryption for messages
   - Encryption at rest for database
   - TLS for data in transit

5. **Data Breach Notification**
   - Incident response procedures
   - Notification within 72 hours
   - Contact: [security email]

#### Privacy by Design

- **Zero-Knowledge Architecture**: Server cannot decrypt messages
- **Local Key Derivation**: Master passwords never transmitted
- **Encrypted Backups**: All backups encrypted with user keys
- **Minimal Metadata**: Only necessary metadata stored

### Security Certifications

For production deployments requiring certifications:

1. **SOC 2 Type II**
   - Security controls
   - Availability monitoring
   - Confidentiality measures
   - Regular audits

2. **ISO 27001**
   - Information security management
   - Risk assessment procedures
   - Security controls implementation

3. **PCI DSS** (if handling payments)
   - Not currently applicable
   - Required if payment processing added

---

## Reporting Security Issues

We take security seriously and appreciate responsible disclosure of security vulnerabilities.

### Reporting Process

1. **Do Not** open public GitHub issues for security vulnerabilities
2. **Do** email security issues to: [security contact email - to be configured]
3. **Include**:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

### What to Expect

- **Acknowledgment**: Within 24 hours
- **Initial Assessment**: Within 72 hours
- **Status Update**: Weekly until resolved
- **Fix Timeline**: Based on severity
  - Critical: 7 days
  - High: 30 days
  - Medium: 90 days
  - Low: Next release

### Security Disclosure Policy

1. **Coordinated Disclosure**
   - We will work with you to understand and fix the issue
   - We request 90 days before public disclosure
   - We will credit you for the discovery (if desired)

2. **Bug Bounty**
   - Currently not offered
   - May be implemented in the future

### Security Contact

- **Email**: [To be configured]
- **PGP Key**: [To be configured]
- **Response Time**: Within 24 hours

---

## Additional Security Resources

### Documentation References

- [Archive Security Model](./ARCHIVE_SECURITY_MODEL.md) - Detailed E2EE architecture
- [SSL Configuration](./SSL_CONFIGURATION.md) - TLS/HTTPS setup guide
- [Deployment Guide](./DEPLOYMENT.md) - Complete deployment instructions
- [Development Guide](./DEVELOPMENT.md) - Development environment setup

### External Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [NIST Cryptographic Standards](https://csrc.nist.gov/publications)
- [Azure Security Best Practices](https://docs.microsoft.com/en-us/azure/security/)
- [MongoDB Security Checklist](https://docs.mongodb.com/manual/administration/security-checklist/)

### Security Tools

- **Static Analysis**: SonarQube, ESLint Security
- **Dependency Scanning**: Dependabot, Snyk
- **Penetration Testing**: OWASP ZAP, Burp Suite
- **Monitoring**: Azure Monitor, Serilog, Application Insights

---

## Revision History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2026-01-31 | Initial security documentation | Copilot |

---

## License

This documentation is part of the TralliValli project and is subject to the same license as the project.

For questions or clarifications about this security documentation, please contact the development team.