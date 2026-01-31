# TralliValli Architecture Documentation

## Table of Contents

- [System Overview](#system-overview)
- [System Architecture Diagram](#system-architecture-diagram)
- [Component Descriptions](#component-descriptions)
- [Data Flow Diagrams](#data-flow-diagrams)
- [Technology Choices and Rationale](#technology-choices-and-rationale)
- [Security Model](#security-model)
- [Scalability Considerations](#scalability-considerations)
- [Deployment Architecture](#deployment-architecture)

---

## System Overview

TralliValli is a self-hosted, invite-only messaging platform designed for family and friends with a focus on privacy and security. The system provides end-to-end encrypted messaging with real-time communication capabilities, file sharing, and comprehensive data management features.

### Core Capabilities

- **Real-time Messaging**: Instant message delivery via WebSocket connections with SignalR
- **End-to-End Encryption**: Client-side and server-side encryption ensuring message privacy
- **Magic Link Authentication**: Passwordless authentication via email with JWT tokens
- **File Sharing**: Secure file upload/download with encryption and presigned URLs
- **Invite System**: Self-hosted, invite-only platform with signed invitation tokens
- **Message Archival**: Automated archival and backup system for data retention
- **Multi-Device Support**: Users can access conversations from multiple devices

### Architecture Style

TralliValli follows a **layered architecture** pattern with clear separation of concerns:

- **Presentation Layer**: React/TypeScript SPA
- **API Layer**: ASP.NET Core REST API + SignalR Hub
- **Business Logic Layer**: Service classes in Auth, Messaging, Workers projects
- **Data Access Layer**: Repository pattern with MongoDB
- **Infrastructure Layer**: External services (RabbitMQ, Redis, Azure Blob Storage)

---

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         CLIENT APPLICATIONS                              │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                     React + TypeScript Web App                    │  │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐    │  │
│  │  │   Zustand  │ │  SignalR   │ │ libsodium  │ │   Axios    │    │  │
│  │  │   Store    │ │   Client   │ │  Crypto    │ │   HTTP     │    │  │
│  │  └────────────┘ └────────────┘ └────────────┘ └────────────┘    │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
                   HTTPS         WebSocket        HTTPS
                    │               │               │
┌───────────────────▼───────────────▼───────────────▼─────────────────────┐
│                           API GATEWAY LAYER                              │
│                                                                           │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                    TraliVali.Api (ASP.NET Core 8)                 │  │
│  │                                                                    │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │  │
│  │  │ REST API     │  │  SignalR Hub │  │  JWT Middleware      │   │  │
│  │  │ Controllers  │  │  /hubs/chat  │  │  Authentication      │   │  │
│  │  └──────────────┘  └──────────────┘  └──────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
                                    │
            ┌───────────────────────┼───────────────────────┐
            │                       │                       │
            ▼                       ▼                       ▼
┌─────────────────────┐ ┌─────────────────────┐ ┌─────────────────────┐
│  TraliVali.Auth     │ │ TraliVali.Messaging │ │  TraliVali.Workers  │
│                     │ │                     │ │                     │
│ • JWT Service       │ │ • Email Service     │ │ • Message Processor │
│ • Magic Link Auth   │ │ • Notification Mgr  │ │ • File Processor    │
│ • Encryption Svc    │ │ • Template Engine   │ │ • Archival Worker   │
│ • Token Blacklist   │ │ • ACS Integration   │ │ • Backup Worker     │
└─────────────────────┘ └─────────────────────┘ └─────────────────────┘
            │                       │                       │
            └───────────────────────┼───────────────────────┘
                                    │
                                    ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                      TraliVali.Infrastructure                             │
│                                                                           │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐      │
│  │   Repository     │  │   Messaging      │  │     Storage      │      │
│  │    Pattern       │  │   (RabbitMQ)     │  │  (Azure Blob)    │      │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘      │
└───────────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        │                           │                           │
        ▼                           ▼                           ▼
┌───────────────┐          ┌───────────────┐          ┌───────────────┐
│   MongoDB     │          │   RabbitMQ    │          │     Redis     │
│               │          │               │          │               │
│ • Users       │          │ • Message     │          │ • Cache       │
│ • Messages    │          │   Processing  │          │ • Sessions    │
│ • Convs       │          │ • File Queue  │          │ • Token       │
│ • Files       │          │ • Dead Letter │          │   Blacklist   │
│ • Invites     │          │   Queue       │          │               │
└───────────────┘          └───────────────┘          └───────────────┘
        │                                                      
        │                           ┌───────────────┐         
        └──────────────────────────►│ Azure Blob    │         
                                    │ Storage       │         
                                    │               │         
                                    │ • Backups     │         
                                    │ • Files       │         
                                    │ • Archives    │         
                                    └───────────────┘         
```

---

## Component Descriptions

### 1. TraliVali.Api (Presentation/API Layer)

**Purpose**: Entry point for all client requests, handles HTTP/HTTPS and WebSocket connections.

**Key Components**:

- **Controllers**: REST API endpoints for authentication, conversations, messages, files, admin operations
  - `AuthController`: Magic link request, verification, token refresh, logout
  - `ConversationsController`: CRUD operations for conversations and membership management
  - `MessagesController`: Message retrieval, search, deletion with pagination
  - `FilesController`: File upload/download URL generation and management
  - `KeyBackupController`: Encrypted key backup storage and retrieval
  - `AdminController`: Administrative operations (archival, backup, statistics)

- **SignalR Hub** (`/hubs/chat`): Real-time communication hub
  - Message broadcasting
  - Typing indicators
  - Read receipts
  - User presence
  - Conversation join/leave events

- **Middleware**:
  - JWT authentication middleware
  - Exception handling middleware
  - Request logging middleware
  - CORS configuration

**Technology**: ASP.NET Core 8, SignalR, Serilog

### 2. TraliVali.Domain (Domain Layer)

**Purpose**: Core business entities with no external dependencies, representing the domain model.

**Key Entities**:

- **User**: Email, display name, public key, connected devices, roles
- **Message**: Content (encrypted/plain), sender, conversation, timestamps, read status, reply references, attachments
- **Conversation**: Participants, type (direct/group), name, last message, metadata
- **File**: Blob reference, encryption metadata, uploader, size, MIME type
- **Invite**: Signed token, inviter, invitee email, expiration, used status
- **Backup**: Archive metadata, creation timestamp, size, Azure blob reference
- **ConversationKey**: Encrypted conversation keys for E2E encryption
- **UserKeyBackup**: Encrypted master key backups for recovery

**Design Pattern**: Domain-Driven Design (DDD) entities with encapsulation and validation

### 3. TraliVali.Infrastructure (Data Access Layer)

**Purpose**: Implements data access patterns and external service integrations.

**Key Components**:

- **Repository Pattern**:
  - `IRepository<T>`: Generic CRUD interface
  - Implementations for all domain entities
  - MongoDB-specific implementations with indexing

- **MongoDB Context**: 
  - Connection management
  - Index creation
  - Collection configuration
  - Query optimization

- **RabbitMQ Messaging Service**:
  - Topic exchange-based routing
  - Queue declarations with durability
  - Message publishing with retry logic
  - Consumer implementations

- **Storage Service**:
  - Azure Blob Storage client wrapper
  - Presigned URL generation
  - File lifecycle management
  - Backup archive storage

**Technology**: MongoDB.Driver, RabbitMQ.Client, Azure.Storage.Blobs

### 4. TraliVali.Auth (Authentication & Security Layer)

**Purpose**: Handles all authentication, authorization, and encryption concerns.

**Key Services**:

- **JwtService**: 
  - RSA256 token generation and validation
  - Token refresh mechanism
  - Claims management
  - Token expiration handling

- **MagicLinkService**:
  - Email-based authentication link generation
  - HMAC-signed secure tokens
  - Expiration validation
  - One-time use enforcement

- **MessageEncryptionService**:
  - AES-256-GCM encryption/decryption
  - PBKDF2 key derivation (100k iterations)
  - Conversation key management
  - Master key operations

- **TokenBlacklistService**:
  - Redis-based token revocation
  - Logout token tracking
  - TTL-based automatic cleanup

- **InviteService**:
  - HMAC-SHA256 signed invite tokens
  - Invite validation and verification
  - Expiration management

**Technology**: System.IdentityModel.Tokens.Jwt, StackExchange.Redis, System.Security.Cryptography

### 5. TraliVali.Messaging (Notification Layer)

**Purpose**: Manages email notifications and communication services.

**Key Components**:

- **EmailService**: 
  - Azure Communication Services integration
  - Template-based email generation
  - Retry logic for failed deliveries
  - Delivery status tracking

- **NotificationManager**:
  - Multi-channel notification routing
  - User preference management
  - Notification queue processing

- **Template Engine**:
  - HTML email templates for magic links
  - Liquid template processing
  - Localization support (future)

**Technology**: Azure.Communication.Email, RazorEngine/Liquid templates

### 6. TraliVali.Workers (Background Processing Layer)

**Purpose**: Asynchronous task processing for heavy operations and scheduled jobs.

**Key Workers**:

- **MessageProcessorWorker**:
  - Consumes `messages.process` RabbitMQ queue
  - Broadcasts messages to SignalR clients
  - Implements circuit breaker pattern for resilience
  - Dead-letter queue for failed messages

- **FileProcessorWorker**:
  - Handles file processing pipeline
  - Thumbnail generation (if needed)
  - Virus scanning integration (future)

- **ArchivalWorker**:
  - Scheduled message archival (cron: "0 2 * * *" - 2 AM daily)
  - Moves old messages (365+ days) to archive collection
  - Circuit breaker for fault tolerance
  - Configurable retention policies

- **BackupWorker**:
  - Scheduled database backups to Azure Blob Storage
  - Retention policy enforcement
  - Backup verification
  - Incremental backup support (future)

**Technology**: Microsoft.Extensions.Hosting.BackgroundService, Quartz.NET (scheduling)

### 7. Web Client (Frontend Layer)

**Purpose**: User interface for all TralliValli features with client-side encryption.

**Key Modules**:

- **State Management**:
  - Zustand stores for conversations, messages, auth, UI state
  - Optimistic updates for real-time feel
  - Persistence layer for offline support

- **Real-time Communication**:
  - SignalR client for WebSocket connections
  - Automatic reconnection with exponential backoff
  - Message queue for offline messages

- **Encryption**:
  - libsodium-wrappers for client-side crypto
  - Public/private key generation
  - Message encryption before sending
  - Local key storage (encrypted)

- **Components**:
  - Conversation list and detail views
  - Message composer with attachment support
  - File preview and download
  - User settings and profile management

**Technology**: React 19, TypeScript, Vite, Tailwind CSS, Zustand, libsodium-wrappers, @microsoft/signalr

---

## Data Flow Diagrams

### 1. User Authentication Flow (Magic Link)

```
┌──────────┐                                                    ┌──────────┐
│  Client  │                                                    │   API    │
└────┬─────┘                                                    └────┬─────┘
     │                                                               │
     │  1. POST /auth/request-magic-link                             │
     │    { email: "user@example.com" }                              │
     ├──────────────────────────────────────────────────────────────►│
     │                                                               │
     │                                     2. Generate HMAC token    │
     │                                     3. Create magic link URL  │
     │                                     4. Send email via ACS     │
     │                                                               │
     │  5. 200 OK { message: "Email sent" }                          │
     │◄──────────────────────────────────────────────────────────────┤
     │                                                               │
     │  6. User clicks link in email                                 │
     │     with token parameter                                      │
     │                                                               │
     │  7. GET /auth/verify-magic-link?token=...                     │
     ├──────────────────────────────────────────────────────────────►│
     │                                                               │
     │                                     8. Verify HMAC signature  │
     │                                     9. Check expiration       │
     │                                    10. Generate JWT tokens    │
     │                                    11. Save refresh token     │
     │                                                               │
     │ 12. 200 OK { accessToken, refreshToken, user }                │
     │◄──────────────────────────────────────────────────────────────┤
     │                                                               │
     │ 13. Store tokens in memory/storage                            │
     │                                                               │
     │ 14. All subsequent requests include:                          │
     │     Authorization: Bearer <accessToken>                       │
     ├──────────────────────────────────────────────────────────────►│
     │                                                               │
```

### 2. Message Send Flow (End-to-End Encrypted)

```
┌──────────┐        ┌──────────┐        ┌──────────┐        ┌──────────┐
│ Client A │        │   API    │        │ RabbitMQ │        │ Client B │
└────┬─────┘        └────┬─────┘        └────┬─────┘        └────┬─────┘
     │                   │                   │                   │
     │ 1. User types     │                   │                   │
     │    message        │                   │                   │
     │                   │                   │                   │
     │ 2. Encrypt with   │                   │                   │
     │    conversation   │                   │                   │
     │    key (client)   │                   │                   │
     │                   │                   │                   │
     │ 3. SignalR:       │                   │                   │
     │    SendMessage    │                   │                   │
     ├──────────────────►│                   │                   │
     │                   │                   │                   │
     │                   │ 4. Validate JWT   │                   │
     │                   │ 5. Save to MongoDB│                   │
     │                   │                   │                   │
     │                   │ 6. Publish to     │                   │
     │                   │    messages.process                   │
     │                   ├──────────────────►│                   │
     │                   │                   │                   │
     │                   │                   │ 7. Worker consumes│
     │                   │                   │    message        │
     │                   │                   │                   │
     │                   │ 8. Broadcast to   │                   │
     │                   │◄──────────────────┤                   │
     │                   │    SignalR clients│                   │
     │                   │                   │                   │
     │ 9. Receive via    │                   │                   │
     │    SignalR        │                   │                   │
     │◄──────────────────┤                   │                   │
     │                   │                   │                   │
     │                   │ 10. Broadcast to  │                   │
     │                   │     other clients │                   │
     │                   ├──────────────────────────────────────►│
     │                   │                   │                   │
     │                   │                   │ 11. Decrypt with  │
     │                   │                   │     conversation  │
     │                   │                   │     key (client)  │
     │                   │                   │                   │
     │                   │                   │ 12. Display       │
     │                   │                   │     message       │
     │                   │                   │                   │
```

### 3. File Upload Flow (Encrypted)

```
┌──────────┐        ┌──────────┐        ┌─────────────────┐
│  Client  │        │   API    │        │  Azure Blob     │
└────┬─────┘        └────┬─────┘        └────┬────────────┘
     │                   │                   │
     │ 1. Select file    │                   │
     │    to upload      │                   │
     │                   │                   │
     │ 2. Encrypt file   │                   │
     │    client-side    │                   │
     │    (optional)     │                   │
     │                   │                   │
     │ 3. POST /files/   │                   │
     │    generate-upload-url                │
     │    { fileName, size, mimeType }       │
     ├──────────────────►│                   │
     │                   │                   │
     │                   │ 4. Generate SAS   │
     │                   │    (Shared Access │
     │                   │     Signature)    │
     │                   ├──────────────────►│
     │                   │                   │
     │                   │ 5. Return presigned│
     │                   │    URL + metadata │
     │                   │◄──────────────────┤
     │                   │                   │
     │ 6. 200 OK         │                   │
     │    { uploadUrl,   │                   │
     │      fileId }     │                   │
     │◄──────────────────┤                   │
     │                   │                   │
     │ 7. PUT to         │                   │
     │    uploadUrl      │                   │
     │    (direct upload)│                   │
     ├──────────────────────────────────────►│
     │                   │                   │
     │                   │                   │ 8. Store blob
     │                   │                   │
     │ 9. 201 Created    │                   │
     │◄──────────────────────────────────────┤
     │                   │                   │
     │10. POST /files/   │                   │
     │    complete-upload│                   │
     │    { fileId }     │                   │
     ├──────────────────►│                   │
     │                   │                   │
     │                   │11. Update DB with │
     │                   │    file status    │
     │                   │                   │
     │12. 200 OK         │                   │
     │◄──────────────────┤                   │
     │                   │                   │
```

### 4. Message Archival Flow (Scheduled)

```
┌─────────────────┐        ┌──────────┐        ┌──────────────┐
│ Archival Worker │        │ MongoDB  │        │  Azure Blob  │
└────┬────────────┘        └────┬─────┘        └────┬─────────┘
     │                          │                   │
     │ 1. Cron trigger          │                   │
     │    (2 AM daily)          │                   │
     │                          │                   │
     │ 2. Query messages        │                   │
     │    older than 365 days   │                   │
     ├─────────────────────────►│                   │
     │                          │                   │
     │ 3. Batch of old messages │                   │
     │◄─────────────────────────┤                   │
     │                          │                   │
     │ 4. Generate archive      │                   │
     │    (JSON/CSV format)     │                   │
     │                          │                   │
     │ 5. Upload archive        │                   │
     │    to blob storage       │                   │
     ├─────────────────────────────────────────────►│
     │                          │                   │
     │                          │                   │ 6. Store blob
     │                          │                   │    with metadata
     │                          │                   │
     │ 7. Blob reference        │                   │
     │◄─────────────────────────────────────────────┤
     │                          │                   │
     │ 8. Delete archived       │                   │
     │    messages from main    │                   │
     │    collection            │                   │
     ├─────────────────────────►│                   │
     │                          │                   │
     │ 9. Move to archive       │                   │
     │    collection            │                   │
     │    (if needed)           │                   │
     │                          │                   │
     │10. Log completion        │                   │
     │                          │                   │
```

---

## Technology Choices and Rationale

### Backend Framework: .NET Core 8

**Choice**: ASP.NET Core 8 with C#

**Rationale**:
- **Performance**: .NET Core 8 offers exceptional performance with minimal memory footprint
- **Modern Features**: Record types, pattern matching, async/await, nullable reference types
- **Cross-platform**: Runs on Windows, Linux, macOS - essential for Docker deployment
- **Rich Ecosystem**: Extensive library support for cryptography, JWT, SignalR
- **Type Safety**: Strong typing reduces runtime errors
- **Long-term Support**: .NET 8 is an LTS release (3-year support cycle through November 2026)

### Database: MongoDB

**Choice**: MongoDB (NoSQL document database)

**Rationale**:
- **Schema Flexibility**: Conversations and messages have varying structures (attachments, replies, reactions)
- **JSON-native**: Natural fit for REST APIs and JavaScript clients
- **Horizontal Scalability**: Sharding support for future growth
- **Rich Query Language**: Aggregation pipelines for complex queries
- **Document Relations**: Embedded documents for messages within conversations reduce joins
- **Indexing**: Efficient indexing for common queries (conversation_id, timestamp, user_id)

**Alternatives Considered**:
- PostgreSQL: Excellent relational DB but less flexible schema and more complex for nested data
- Cassandra: Over-engineered for a self-hosted family/friends platform

### Message Queue: RabbitMQ

**Choice**: RabbitMQ with topic exchange

**Rationale**:
- **Reliability**: Message durability and acknowledgments prevent data loss
- **Decoupling**: Separates API from message processing, improving resilience
- **Routing Flexibility**: Topic exchanges allow complex routing patterns (messages.*, files.*)
- **Dead Letter Queues**: Failed messages routed for manual inspection
- **Management UI**: Built-in web UI for monitoring and debugging
- **Proven**: Battle-tested in production environments

**Alternatives Considered**:
- Azure Service Bus: Cloud-dependent, overkill for self-hosted scenarios
- Kafka: More complex, designed for event streaming rather than task queues

### Cache: Redis

**Choice**: Redis for caching and session management

**Rationale**:
- **Speed**: In-memory storage provides microsecond latency
- **Token Blacklist**: Perfect for logout token tracking with TTL
- **Session Storage**: Fast retrieval of user sessions
- **Pub/Sub**: Future support for real-time notifications
- **Data Structures**: Rich data types (sets, hashes, sorted sets)
- **Lightweight**: Minimal resource requirements

**Alternatives Considered**:
- Memcached: Simpler but lacks Redis' rich data structures and persistence
- In-memory .NET cache: Not shared across multiple API instances

### Storage: Azure Blob Storage

**Choice**: Azure Blob Storage for files and backups

**Rationale**:
- **Scalability**: Handles petabytes of data with no management overhead
- **Security**: Presigned URLs (SAS) for secure, temporary access
- **Cost-effective**: Tiered storage (Hot/Cool/Archive) for cost optimization
- **Geo-redundancy**: Built-in replication across regions
- **Integration**: Native .NET SDK with excellent documentation
- **Encryption**: Server-side encryption at rest included

**Alternatives Considered**:
- Local file system: Not scalable, no redundancy
- AWS S3: Comparable, but Azure chosen for ecosystem consistency
- MinIO: Self-hosted option, but adds operational complexity

### Frontend: React + TypeScript + Vite

**Choice**: React with TypeScript and Vite

**Rationale**:
- **React**: Most popular library, large ecosystem, stable, excellent for real-time UIs
- **TypeScript**: Type safety reduces bugs, improves maintainability, better IDE support
- **Vite**: Ultra-fast HMR (Hot Module Replacement), optimized builds, ESM-first
- **Modern**: Leverages latest web standards (ES modules, native imports)
- **Developer Experience**: Instant feedback loop improves productivity

**Alternatives Considered**:
- Angular: More opinionated, steeper learning curve
- Vue.js: Less ecosystem maturity for real-time/crypto libraries
- Next.js: Overkill for SPA, SSR not needed for authenticated app

### State Management: Zustand

**Choice**: Zustand for client state

**Rationale**:
- **Simplicity**: Minimal boilerplate compared to Redux
- **Performance**: Fine-grained subscriptions prevent unnecessary re-renders
- **TypeScript**: First-class TypeScript support
- **Small Bundle**: ~1KB gzipped
- **Flexibility**: Works with or without context, middleware support

**Alternatives Considered**:
- Redux: Over-engineered for this use case, too much boilerplate
- React Context: Performance issues with frequent updates
- Jotai/Recoil: Less mature, smaller ecosystems

### Real-time: SignalR

**Choice**: SignalR for WebSocket communication

**Rationale**:
- **Integration**: Native ASP.NET Core integration
- **Automatic Fallback**: Falls back to long-polling if WebSockets unavailable
- **Reconnection**: Automatic reconnection with exponential backoff
- **Typed Hubs**: Strong typing between client/server
- **Scalability**: Redis backplane for multi-server deployments (future)
- **Browser Support**: Works on all modern browsers

**Alternatives Considered**:
- Socket.io: Node.js-centric, requires separate server
- Raw WebSockets: Too low-level, manual reconnection logic
- Pusher/Ably: Third-party services, monthly costs, not self-hosted

### Encryption: libsodium (Client) + .NET Crypto (Server)

**Choice**: libsodium-wrappers (client), System.Security.Cryptography (server)

**Rationale**:
- **libsodium**: Industry-standard, audited, easy-to-use crypto library
  - Public-key cryptography (X25519, Ed25519)
  - Sealed boxes for encryption
  - Memory-safe by design
- **.NET Crypto**: Built-in, no dependencies, FIPS-compliant
  - AES-GCM for authenticated encryption
  - PBKDF2 for key derivation
  - RSA for JWT signing

**Alternatives Considered**:
- WebCrypto API: Browser-native but limited browser support for some operations
- CryptoJS: JavaScript-only, less secure than native libsodium

### Email: Azure Communication Services

**Choice**: Azure Communication Services Email

**Rationale**:
- **Reliability**: Enterprise-grade email delivery
- **Compliance**: GDPR compliant, SOC 2 certified
- **Deliverability**: High inbox placement rates
- **Integration**: Native .NET SDK
- **Cost**: Pay-as-you-go pricing, free tier available
- **No Infrastructure**: No SMTP server management

**Alternatives Considered**:
- SendGrid: Comparable, but Azure chosen for ecosystem consistency
- SES (AWS): Good option but cross-cloud complexity
- Self-hosted SMTP: Deliverability issues, spam folder risks

### Deployment: Azure Container Apps

**Choice**: Azure Container Apps for production deployment

**Rationale**:
- **Serverless Containers**: Kubernetes power without complexity
- **Auto-scaling**: Scale to zero, pay only for usage
- **Microservices**: Native support for multiple containers
- **Managed**: No infrastructure management (OS patching, networking)
- **Environment Variables**: Secret management built-in
- **Integration**: Direct access to Azure services (Blob, ACS, Redis)

**Alternatives Considered**:
- Azure App Service: Less flexible, not container-native
- Azure Kubernetes Service: Over-engineered, requires K8s expertise
- VM-based: Manual scaling, security patching burden

---

## Security Model

### 1. Authentication & Authorization

#### Magic Link Authentication

- **Flow**: User enters email → receives time-limited magic link → clicks to authenticate
- **Token**: HMAC-SHA256 signed with server secret
- **Expiration**: 15-minute validity window
- **One-time Use**: Tokens invalidated after successful authentication
- **Security Benefits**: 
  - No passwords to steal or brute-force
  - Mitigates phishing (tokens expire quickly)
  - Email serves as second factor

#### JWT Tokens

- **Algorithm**: RS256 (RSA with SHA-256)
- **Key Pair**: 2048-bit RSA public/private keys
- **Access Token**: 15-minute expiration
- **Refresh Token**: 7-day expiration, stored in MongoDB
- **Claims**: User ID, email, roles, device ID
- **Validation**: Signature verification + expiration check

#### Token Blacklist

- **Storage**: Redis with TTL matching token expiration
- **Purpose**: Invalidate tokens on logout
- **Implementation**: Middleware checks blacklist before processing requests
- **Automatic Cleanup**: Redis TTL removes expired entries

### 2. End-to-End Encryption

#### Encryption Architecture

```
User Master Password
    ↓ (PBKDF2-SHA256, 100k iterations)
Master Key (256-bit)
    ↓ (AES-256-GCM)
Conversation Key (256-bit per conversation)
    ↓ (AES-256-GCM)
Message Content
```

#### Message Encryption

- **Algorithm**: AES-256-GCM (Authenticated Encryption with Associated Data)
- **Key Size**: 256 bits (32 bytes)
- **IV Size**: 96 bits (12 bytes) - unique per message
- **Authentication Tag**: 128 bits (16 bytes) - prevents tampering
- **Process**:
  1. Client generates conversation key on first message
  2. Encrypt message with conversation key
  3. Send encrypted content + IV + tag to server
  4. Server stores encrypted message (cannot decrypt)
  5. Recipients fetch and decrypt using shared conversation key

#### Key Management

- **Conversation Keys**: 
  - Generated client-side (libsodium)
  - Encrypted with user's master key before backup
  - Stored in `conversationKeys` collection
  
- **Master Key**:
  - Derived from user password via PBKDF2 (100k iterations)
  - Never stored on server
  - Used only to encrypt/decrypt conversation keys
  
- **Key Backup**:
  - Encrypted master key backup stored in `userKeyBackups`
  - Enables multi-device access
  - Recovery mechanism for lost devices

#### File Encryption

- **Optional**: Files can be encrypted client-side before upload
- **Metadata**: IV and tag stored in `File` entity
- **Storage**: Encrypted blobs in Azure Storage
- **Access**: Presigned URLs with short expiration (15 minutes)

### 3. API Security

#### Transport Security

- **HTTPS Only**: TLS 1.3 enforced in production
- **Certificate Pinning**: Future enhancement for mobile apps
- **HSTS**: HTTP Strict Transport Security headers

#### Input Validation

- **Model Validation**: ASP.NET Core DataAnnotations
- **Sanitization**: HTML encoding to prevent XSS
- **Size Limits**: Request body size limits (10MB default)
- **Rate Limiting**: Future enhancement using middleware

#### CORS Policy

- **Origin Whitelist**: Only specified domains allowed
- **Credentials**: Credentials allowed for authenticated requests
- **Methods**: Restricted to necessary HTTP methods
- **Headers**: Limited to required headers

### 4. Data Security

#### Data at Rest

- **MongoDB**: Encryption at rest (optional, cloud-managed)
- **Azure Blobs**: Server-side encryption with Microsoft-managed keys
- **Backups**: Encrypted before upload to blob storage

#### Data in Transit

- **TLS 1.3**: All network communication encrypted
- **SignalR**: WebSocket connections over TLS
- **Internal**: RabbitMQ/Redis can use TLS (optional in self-hosted)

#### Sensitive Data Handling

- **Passwords**: Never stored (magic link auth)
- **Secrets**: Environment variables, not in code
- **Keys**: Encrypted when at rest
- **Logs**: PII redaction in production logs

### 5. Invite System Security

#### Signed Invites

- **Signature**: HMAC-SHA256 with server secret
- **Expiration**: 7-day validity
- **Single Use**: Marked as used after registration
- **Verification**: Server validates signature + expiration + usage status

#### Invite-Only Platform

- **Registration**: Requires valid invite token
- **No Public Signup**: Prevents spam and unauthorized access
- **Controlled Growth**: Inviter accountability

### 6. Threat Mitigation

#### Common Vulnerabilities

| Threat | Mitigation |
|--------|------------|
| **SQL Injection** | MongoDB uses parameterized queries, no raw string concatenation |
| **XSS (Cross-Site Scripting)** | React auto-escapes, HTML sanitization on inputs |
| **CSRF (Cross-Site Request Forgery)** | JWT tokens in headers (not cookies), SameSite attribute |
| **Man-in-the-Middle** | TLS 1.3 encryption, certificate validation |
| **Replay Attacks** | JWT expiration, one-time magic links, nonces |
| **Brute Force** | Rate limiting (future), magic link throttling |
| **Token Theft** | Short-lived access tokens, refresh token rotation |
| **Data Breach** | E2E encryption, encrypted at rest, minimal PII storage |

#### Zero Trust Principles

- **Verify All Requests**: JWT validation on every API call
- **Least Privilege**: Users can only access their conversations
- **Audit Logging**: Structured logs for security events
- **Defense in Depth**: Multiple security layers (auth, encryption, validation)

---

## Scalability Considerations

### 1. Horizontal Scalability

#### API Layer (Stateless)

- **Design**: Stateless REST API + SignalR hub
- **Scaling**: Add more API instances behind load balancer
- **Session Management**: JWT tokens (no server-side sessions)
- **Load Balancing**: Azure Load Balancer or Application Gateway
- **SignalR Backplane**: Redis backplane for multi-server SignalR

**Current Capacity**: Single instance handles ~1000 concurrent users

**Scaling Strategy**:
```
1-1000 users:     1 API instance
1000-5000 users:  2-3 API instances + Redis backplane
5000+ users:      Auto-scaling with Azure Container Apps (2-10 instances)
```

#### Workers (Queue-Based)

- **Design**: RabbitMQ consumers with competing consumers pattern
- **Scaling**: Add more worker instances (automatic work distribution)
- **Circuit Breaker**: Prevents cascading failures
- **Dead Letter Queue**: Failed messages for manual review

**Scaling Strategy**:
- 1 worker instance: ~500 messages/second
- 2-5 workers: ~2000 messages/second (linear scaling)

### 2. Vertical Scalability

#### MongoDB

- **Sharding**: Shard by conversation_id for horizontal scaling
- **Indexes**: Optimized indexes for common queries
- **Replication**: Replica set for high availability
- **Current**: Single instance (suitable for ~10k users, ~100M messages)

**Scaling Strategy**:
```
< 100M messages:    Single instance (16GB RAM)
100M-1B messages:   Replica set (3 nodes)
> 1B messages:      Sharded cluster (3+ shards)
```

#### Redis

- **Clustering**: Redis Cluster for horizontal scaling
- **Persistence**: RDB + AOF for durability
- **Current**: Single instance (suitable for ~10k concurrent users)

**Scaling Strategy**:
- < 10k users: Single instance (4GB RAM)
- 10k-50k users: Redis Cluster (3 master + 3 replica)

#### RabbitMQ

- **Clustering**: Federated queues for multi-region
- **Mirroring**: Queue mirroring for high availability
- **Current**: Single instance (suitable for ~5k messages/second)

**Scaling Strategy**:
- < 5k msg/sec: Single instance
- 5k-20k msg/sec: 3-node cluster with mirroring

### 3. Database Optimization

#### Indexing Strategy

```javascript
// Messages collection
{ "conversationId": 1, "createdAt": -1 }  // Most common query
{ "senderId": 1, "createdAt": -1 }        // User's sent messages
{ "content": "text" }                      // Full-text search

// Conversations collection
{ "participantIds": 1 }                    // User's conversations
{ "updatedAt": -1 }                        // Recent conversations

// Users collection
{ "email": 1 }                             // Unique index for auth
```

#### Query Optimization

- **Pagination**: Cursor-based pagination for large result sets
- **Projection**: Return only needed fields
- **Aggregation**: Pipeline optimization for complex queries
- **Caching**: Redis for frequently accessed data (user profiles, conversation metadata)

### 4. Caching Strategy

#### Multi-Layer Caching

```
Request
  ↓
[In-Memory Cache] - .NET Memory Cache (1-5 min TTL)
  ↓ (cache miss)
[Redis Cache] - Distributed cache (5-60 min TTL)
  ↓ (cache miss)
[MongoDB] - Source of truth
```

#### Cached Data

- **User Profiles**: 30-minute TTL
- **Conversation Metadata**: 5-minute TTL
- **Message Counts**: 1-minute TTL
- **File URLs**: 15-minute TTL (matches SAS expiration)

#### Cache Invalidation

- **Write-Through**: Update cache on data modification
- **Event-Based**: Invalidate on message sent/received
- **TTL-Based**: Automatic expiration for less critical data

### 5. File Storage Optimization

#### Azure Blob Storage

- **Tiering**: 
  - Hot: Recent files (< 30 days)
  - Cool: Older files (30-365 days)
  - Archive: Backups (> 365 days)

- **CDN**: Azure CDN for static file delivery (future)

- **Compression**: Gzip/Brotli for text files

#### Size Limits

- **Max File Size**: 100MB per file
- **Max Message Size**: 10MB
- **Throttling**: Rate limiting on upload endpoints (future)

### 6. Real-time Scalability

#### SignalR Scaling

- **Redis Backplane**: Share messages across API instances
- **Sticky Sessions**: Not required with Redis backplane
- **Connection Pooling**: Reuse connections

**Limits**:
- Single server: ~10k concurrent WebSocket connections
- With Redis backplane: ~100k+ connections (10 servers × 10k)

### 7. Monitoring & Auto-Scaling

#### Metrics

- **API**: Request rate, latency (p50, p95, p99), error rate
- **MongoDB**: Query latency, connection pool usage, disk I/O
- **RabbitMQ**: Queue depth, message rate, consumer lag
- **Redis**: Memory usage, hit/miss ratio, eviction rate
- **Workers**: Processing time, error rate, queue backlog

#### Auto-Scaling Triggers

- **CPU**: Scale out when > 70% for 5 minutes
- **Memory**: Scale out when > 80% for 5 minutes
- **Queue Depth**: Add workers when queue > 1000 messages
- **Response Time**: Scale out when p95 latency > 500ms

#### Azure Container Apps Scaling

```yaml
scale:
  minReplicas: 1
  maxReplicas: 10
  rules:
    - name: cpu-rule
      type: cpu
      metadata:
        type: Utilization
        value: "70"
    - name: http-rule
      type: http
      metadata:
        concurrentRequests: "100"
```

### 8. Future Enhancements

#### Performance Improvements

- **GraphQL**: Replace REST with GraphQL for optimized queries
- **gRPC**: Internal microservice communication (API ↔ Workers)
- **Message Batching**: Batch writes to MongoDB (10-100 messages)
- **Read Replicas**: MongoDB read replicas for query load distribution

#### Scalability Enhancements

- **Multi-Region**: Deploy in multiple Azure regions for global users
- **Event Sourcing**: Switch to event-sourced architecture for audit trails
- **CQRS**: Command Query Responsibility Segregation for read/write optimization
- **Message Streaming**: Replace RabbitMQ with Kafka for high-throughput scenarios

---

## Deployment Architecture

### 1. Local Development

```
Developer Machine
├── Docker Compose (Infrastructure)
│   ├── MongoDB:27017
│   ├── RabbitMQ:5672, 15672 (management)
│   └── Redis:6379
├── .NET API (localhost:5248)
└── React Web (localhost:5173)
```

**Setup**: `docker compose up -d && dotnet run && npm run dev`

### 2. Azure Production Deployment

```
                                    [Azure]
                                       │
                   ┌───────────────────┼───────────────────┐
                   │                   │                   │
                   ▼                   ▼                   ▼
       [Azure Container Apps]   [Azure Cosmos DB]  [Azure Blob Storage]
                   │              (MongoDB API)            │
       ┌───────────┼───────────┐                          │
       │           │           │                          │
       ▼           ▼           ▼                          ▼
  [API Pods]  [Worker Pods] [Web SPA]              [Files & Backups]
  (1-10)       (1-5)        (Static)                      │
       │           │           │                          │
       └───────────┼───────────┘                          │
                   │                                      │
                   ▼                                      │
          [Azure Cache for Redis]                         │
          [Azure Communication Svc]                       │
                   │                                      │
                   └──────────────────────────────────────┘
                              │
                              ▼
                      [Azure Monitor]
                      [Application Insights]
```

#### Components

| Component | Azure Service | Purpose |
|-----------|--------------|---------|
| **API** | Container Apps | REST API + SignalR Hub (auto-scaling) |
| **Workers** | Container Apps | Background jobs (message processing, archival) |
| **Web Client** | Static Web Apps | React SPA with CDN |
| **Database** | Cosmos DB (MongoDB API) | Primary data store with global distribution |
| **Cache** | Azure Cache for Redis | Token blacklist, session cache |
| **Storage** | Azure Blob Storage | Files, backups, archives |
| **Email** | Azure Communication Services | Magic link delivery |
| **Monitoring** | Application Insights | Logs, metrics, traces |
| **Secrets** | Azure Key Vault | Connection strings, API keys |

#### Networking

- **VNET Integration**: API and workers in private virtual network
- **Private Endpoints**: Database, Redis, Storage accessible only from VNET
- **Public Access**: Only API ingress and Static Web App
- **Firewall**: NSG rules restrict traffic to necessary ports

#### Environments

| Environment | Purpose | Auto-Scaling | Replica Count |
|-------------|---------|--------------|---------------|
| **Development** | Testing new features | No | 1 API, 1 Worker |
| **Staging** | Pre-production validation | No | 1 API, 1 Worker |
| **Production** | Live users | Yes | 2-10 API, 1-5 Workers |

### 3. Self-Hosted Deployment (Docker Compose)

For users who prefer complete control:

```
Self-Hosted Server (VPS/Dedicated)
├── Docker Compose Stack
│   ├── tralivali-api (ASP.NET Core)
│   ├── tralivali-worker (Background jobs)
│   ├── tralivali-web (Nginx + React SPA)
│   ├── mongodb (Database)
│   ├── rabbitmq (Message queue)
│   └── redis (Cache)
├── Volumes (Persistent data)
│   ├── mongodb_data
│   ├── rabbitmq_data
│   └── redis_data
└── Reverse Proxy (Nginx/Caddy)
    └── HTTPS with Let's Encrypt
```

**Requirements**:
- Linux server (Ubuntu 22.04 LTS recommended)
- Docker 24+ & Docker Compose v2
- 4 CPU cores, 8GB RAM minimum
- 100GB SSD storage
- Public IP + domain name for HTTPS

**Benefits**:
- Complete data ownership
- No cloud costs (except server)
- Customizable infrastructure

**Trade-offs**:
- Manual scaling
- Self-managed backups
- No auto-scaling
- Security patching responsibility

### 4. CI/CD Pipeline

#### GitHub Actions Workflow

```yaml
Trigger: Push to main branch
├── Build Stage
│   ├── Restore .NET dependencies
│   ├── Build solution
│   ├── Run unit tests
│   └── Build Docker images
├── Test Stage
│   ├── Run integration tests
│   └── Run E2E tests (Playwright)
├── Security Stage
│   ├── CodeQL analysis
│   ├── Dependency vulnerability scan
│   └── Container image scan
└── Deploy Stage
    ├── Push images to Azure Container Registry
    ├── Update Container Apps (API + Workers)
    └── Deploy Web SPA to Static Web Apps
```

#### Deployment Strategy

- **Blue-Green**: Zero-downtime deployments with traffic shifting
- **Canary**: 10% traffic to new version, monitor, then 100%
- **Rollback**: Automatic rollback on health check failures

### 5. High Availability

#### Redundancy

- **API**: Multi-instance with load balancing (active-active)
- **MongoDB**: 3-node replica set (1 primary, 2 secondaries)
- **Redis**: Clustered with failover (1 master, 2 replicas)
- **RabbitMQ**: 3-node cluster with mirrored queues
- **Storage**: Geo-redundant storage (GRS) in Azure

#### Disaster Recovery

- **RTO (Recovery Time Objective)**: < 4 hours
- **RPO (Recovery Point Objective)**: < 1 hour
- **Backups**: 
  - MongoDB: Daily automated backups (retained 30 days)
  - Files: Geo-redundant storage with soft delete
  - Config: Infrastructure as Code (ARM templates in Git)

#### Monitoring

- **Health Checks**: API /health endpoint every 10 seconds
- **Alerts**: 
  - API down > 2 minutes → Page on-call engineer
  - Error rate > 5% → Slack notification
  - Queue depth > 10k → Auto-scale workers
- **Dashboards**: Grafana + Application Insights for real-time metrics

---

## Conclusion

TralliValli's architecture is designed for **privacy, security, and scalability** while remaining **self-hostable** for users who want complete control. The layered architecture with clean separation of concerns enables:

- **Independent scaling** of API, workers, and frontend
- **Technology flexibility** (swap MongoDB for Postgres, Azure for AWS)
- **Maintainability** through clear boundaries and testable components
- **Security** with end-to-end encryption and defense-in-depth strategy

The system balances modern cloud-native practices with the simplicity needed for self-hosted deployments, making it suitable for both small family groups and larger communities.

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-31  
**Maintained by**: TralliValli Development Team
