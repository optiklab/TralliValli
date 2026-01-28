# TralliValli Project Roadmap

This document outlines all 74 implementation tasks for building TralliValli, a self-hosted, invite-only messaging platform with end-to-end encryption.

## Overview

TralliValli is built with:
- **Backend**: .NET 8, ASP.NET Core Web API, SignalR
- **Message Queue**: RabbitMQ
- **Database**: MongoDB
- **Cache**: Redis
- **Storage**: Azure Blob Storage
- **Frontend**: React + TypeScript + Vite
- **Deployment**: Azure Container Apps

## Phase Summary

- **Phase 1**: Backend Foundation (10 tasks)
- **Phase 2**: Real-Time Messaging Backend (7 tasks)
- **Phase 3**: Message Retention, Archival & Backup (7 tasks)
- **Phase 4**: Web Client (13 tasks)
- **Phase 5**: End-to-End Encryption (7 tasks)
- **Phase 6**: File Sharing & Media (6 tasks)
- **Phase 7**: Azure Deployment (8 tasks)
- **Phase 8**: Testing (10 tasks)
- **Phase 9**: Documentation (6 tasks)

**Total: 74 Tasks**

---

## Phase 1: Backend Foundation

### Task 1: Create .NET solution structure

**Description**: Initialize a .NET 8 solution with Clean Architecture.

**Details**:
- Create TraliVali.Api (ASP.NET Core Web API)
- Create TraliVali.Auth (authentication library)
- Create TraliVali.Messaging (messaging services)
- Create TraliVali.Workers (background workers)
- Create TraliVali.Domain (domain entities, no dependencies)
- Create TraliVali.Infrastructure (repositories, external services)
- Create TraliVali.Tests (test project)
- Add project references following dependency rules:
  - Domain has no dependencies
  - Infrastructure references Domain
  - Api references all projects
- Include Serilog for structured logging
- Document every public class and method with XML comments

**Acceptance Criteria**:
- [ ] Solution builds successfully
- [ ] All project references are correct
- [ ] Serilog is configured
- [ ] All public APIs have XML documentation

**Labels**: backend, infrastructure, setup

---

### Task 2: Create Docker Compose for local development

**Description**: Create docker-compose.yml with required services for local development.

**Details**:
- MongoDB (latest) with authentication
- RabbitMQ (3-management) with management UI
- Redis (7-alpine) for caching
- Configure volumes for data persistence
- Expose ports for local debugging
- Add health checks for all services
- Create .env.example with all required environment variables documented

**Acceptance Criteria**:
- [ ] `docker-compose up` starts all services
- [ ] All services pass health checks
- [ ] Data persists across restarts
- [ ] .env.example is complete and documented

**Labels**: docker, infrastructure, setup

---

### Task 3: Implement MongoDB repository pattern

**Description**: Create repository pattern implementation for MongoDB.

**Details**:
- In TraliVali.Infrastructure, create MongoDbContext
- Create generic IRepository<T> interface with CRUD operations
- Implement repositories for:
  - User
  - Conversation
  - Message
  - Invite
  - File
  - Backup
- Add MongoDB indexes:
  - users.email (unique)
  - messages.conversationId + createdAt
  - conversations.participants.userId + lastMessageAt
  - invites.token (unique with TTL)
- Use TDD approach with xUnit tests using Testcontainers

**Acceptance Criteria**:
- [ ] All repositories implement IRepository<T>
- [ ] All indexes are created on startup
- [ ] Tests use Testcontainers for MongoDB
- [ ] 100% test coverage on repository layer

**Labels**: backend, database, mongodb, testing

---

### Task 4: Define domain entities

**Description**: Create all domain entities in TraliVali.Domain.

**Details**:
Create entities with full XML documentation:

- **User**: id, email, displayName, passwordHash, publicKey, devices[], createdAt, invitedBy
- **Conversation**: id, type, participants[], recentMessages[50], lastMessageAt, metadata
- **Message**: id, conversationId, senderId, type, content, encryptedContent, replyTo, createdAt, readBy[]
- **Invite**: id, token, inviterId, expiresAt, usedBy, usedAt
- **File**: id, conversationId, uploaderId, fileName, mimeType, size, blobPath, thumbnailPath, createdAt

**Acceptance Criteria**:
- [ ] All entities defined with correct properties
- [ ] Full XML documentation on all types
- [ ] Validation logic included
- [ ] Unit tests for validation

**Labels**: backend, domain, entities

---

### Task 5: Configure RabbitMQ infrastructure

**Description**: Set up RabbitMQ messaging infrastructure.

**Details**:
- In TraliVali.Infrastructure, create RabbitMqService
- Create topic exchange: tralivali.messages
- Define queues:
  - messages.process
  - files.process
  - archival.process
  - backup.process
- Implement IMessagePublisher interface
- Implement IMessageConsumer interface
- Add connection resilience with Polly retry policies
- Write unit tests with mocked RabbitMQ

**Acceptance Criteria**:
- [ ] Exchange and queues created on startup
- [ ] Publishers can send messages
- [ ] Consumers can receive messages
- [ ] Retry policies work correctly
- [ ] Unit tests pass with mocked dependencies

**Labels**: backend, messaging, rabbitmq

---

### Task 6: Integrate Azure Communication Services Email

**Description**: Implement email service using Azure Communication Services.

**Details**:
- Create IEmailService interface
- Implement AzureCommunicationEmailService
- Support email types:
  - Magic-link authentication
  - Invite notification
  - Password reset
- Use templated HTML emails stored as embedded resources
- Add configuration in appsettings.json:
  - Connection string
  - Sender address
- Write unit tests with mocked Azure SDK

**Acceptance Criteria**:
- [ ] Email service can send all email types
- [ ] Templates are properly formatted
- [ ] Configuration is validated on startup
- [ ] Unit tests cover all scenarios

**Labels**: backend, email, azure

---

### Task 7: Build JWT authentication service

**Description**: Implement JWT token service for authentication.

**Details**:
- In TraliVali.Auth, create IJwtService with:
  - GenerateToken(User)
  - ValidateToken(string)
  - RefreshToken(string)
- Configure JWT with:
  - RS256 signing
  - 7-day expiry
  - Refresh token rotation
- Add claims: userId, email, displayName, deviceId
- Implement token blacklisting for logout using Redis
- Write comprehensive unit tests

**Acceptance Criteria**:
- [ ] Tokens are generated with correct claims
- [ ] Token validation works correctly
- [ ] Refresh token rotation implemented
- [ ] Blacklisting prevents token reuse
- [ ] Unit tests cover all edge cases

**Labels**: backend, authentication, jwt

---

### Task 8: Create magic-link authentication flow

**Description**: Implement passwordless authentication using magic links.

**Details**:
- Implement AuthController with endpoints:
  - POST /auth/request-magic-link (sends email)
  - POST /auth/verify-magic-link (validates token, returns JWT)
  - POST /auth/refresh (refresh JWT)
  - POST /auth/logout (blacklist token)
- Magic links expire in 15 minutes
- Single-use tokens
- Store pending magic links in Redis
- Write integration tests

**Acceptance Criteria**:
- [ ] Magic link emails are sent
- [ ] Links expire after 15 minutes
- [ ] Links are single-use
- [ ] JWT is returned on successful verification
- [ ] Integration tests pass

**Labels**: backend, authentication, api

---

### Task 9: Build invite link and QR code service

**Description**: Implement invitation system with links and QR codes.

**Details**:
- Create IInviteService with:
  - GenerateInviteLink(inviterId, expiryHours)
  - GenerateInviteQrCode(inviteLink)
  - ValidateInvite(token)
  - RedeemInvite(token, userId)
- Use HMAC-SHA256 signed tokens
- Generate QR codes using QRCoder library
- Store invites in MongoDB with TTL
- Write unit tests covering expiry, redemption, validation

**Acceptance Criteria**:
- [ ] Invite links are generated and signed
- [ ] QR codes are generated correctly
- [ ] Invites expire at configured time
- [ ] Invites can only be used once
- [ ] Unit tests pass

**Labels**: backend, invites, qr-code

---

### Task 10: Implement user registration flow

**Description**: Create user registration endpoint and flow.

**Details**:
- Create POST /auth/register endpoint accepting:
  - invite token
  - email
  - displayName
- Validate invite
- Create user
- Mark invite as used
- Send welcome email
- Return JWT
- Add GET /auth/invite/{token} to validate invite before registration UI
- Write integration tests for complete flow

**Acceptance Criteria**:
- [ ] Registration requires valid invite
- [ ] User is created in database
- [ ] Invite is marked as used
- [ ] Welcome email is sent
- [ ] JWT is returned
- [ ] Integration tests pass

**Labels**: backend, authentication, registration, api

---

## Phase 2: Real-Time Messaging Backend

### Task 11: Create SignalR ChatHub

**Description**: Implement SignalR hub for real-time messaging.

**Details**:
- In TraliVali.Api, create ChatHub
- Define strongly-typed interface IChatClient with:
  - ReceiveMessage
  - UserJoined
  - UserLeft
  - TypingIndicator
  - MessageRead
  - PresenceUpdate
- Implement hub methods:
  - SendMessage
  - JoinConversation
  - LeaveConversation
  - StartTyping
  - StopTyping
  - MarkAsRead
- Require JWT authentication
- Document all methods with XML comments

**Acceptance Criteria**:
- [ ] Hub is registered in startup
- [ ] All client methods are defined
- [ ] All hub methods work correctly
- [ ] Authentication is required
- [ ] Full XML documentation

**Labels**: backend, signalr, real-time

---

### Task 12: Implement message processing worker

**Description**: Create background worker for processing messages.

**Details**:
- In TraliVali.Workers, create MessageProcessorWorker
- Consume from messages.process queue
- Worker should:
  - Validate message
  - Encrypt content (placeholder for Phase 5)
  - Persist to MongoDB
  - Update conversation's recentMessages array
  - Broadcast via SignalR to conversation participants
- Add dead-letter queue for failed messages
- Write unit tests

**Acceptance Criteria**:
- [ ] Worker consumes messages from queue
- [ ] Messages are validated
- [ ] Messages are persisted to database
- [ ] SignalR broadcasts work
- [ ] Failed messages go to DLQ
- [ ] Unit tests pass

**Labels**: backend, workers, messaging

---

### Task 13: Build presence tracking system

**Description**: Implement user presence tracking using Redis.

**Details**:
- Create IPresenceService using Redis sorted sets
- Track online users with last-seen timestamps
- Implement methods:
  - SetOnline(userId, connectionId)
  - SetOffline(userId)
  - GetOnlineUsers(userIds[])
  - GetLastSeen(userId)
- Update presence on SignalR connect/disconnect events
- Broadcast presence changes to relevant users
- Write unit tests

**Acceptance Criteria**:
- [ ] Presence is tracked in Redis
- [ ] Online/offline updates work
- [ ] Last-seen timestamps are accurate
- [ ] Broadcasts work correctly
- [ ] Unit tests pass

**Labels**: backend, presence, redis, signalr

---

### Task 14: Implement conversation service

**Description**: Create service for managing conversations.

**Details**:
- Create IConversationService with methods:
  - CreateDirectConversation(userId1, userId2)
  - CreateGroupConversation(name, creatorId, memberIds[])
  - AddMember(conversationId, userId, role)
  - RemoveMember(conversationId, userId)
  - UpdateGroupMetadata(conversationId, name, avatar)
  - GetUserConversations(userId)
- Prevent duplicate direct conversations
- Write unit tests

**Acceptance Criteria**:
- [ ] Direct conversations are created correctly
- [ ] Group conversations support multiple members
- [ ] Duplicate direct conversations are prevented
- [ ] Members can be added/removed
- [ ] Metadata updates work
- [ ] Unit tests pass

**Labels**: backend, conversations, service

---

### Task 15: Create conversation API endpoints

**Description**: Implement REST API endpoints for conversations.

**Details**:
- Implement ConversationsController with:
  - GET /conversations (list user's conversations with pagination)
  - POST /conversations/direct (create 1-on-1)
  - POST /conversations/group (create group)
  - GET /conversations/{id} (get with recent messages)
  - PUT /conversations/{id} (update group metadata)
  - POST /conversations/{id}/members (add member)
  - DELETE /conversations/{id}/members/{userId} (remove member)
- Write integration tests

**Acceptance Criteria**:
- [ ] All endpoints work correctly
- [ ] Pagination works
- [ ] Authorization is enforced
- [ ] Validation works
- [ ] Integration tests pass

**Labels**: backend, api, conversations

---

### Task 16: Implement message history API

**Description**: Create API endpoints for message history.

**Details**:
- Create MessagesController with:
  - GET /conversations/{id}/messages (paginated, cursor-based)
  - GET /conversations/{id}/messages/search (full-text search)
  - DELETE /messages/{id} (soft delete)
- Support loading 50 messages per page
- Use cursor-based pagination with message timestamp
- Write integration tests

**Acceptance Criteria**:
- [ ] Pagination works correctly
- [ ] Cursor-based loading implemented
- [ ] Search functionality works
- [ ] Soft delete works
- [ ] Integration tests pass

**Labels**: backend, api, messages

---

### Task 17: Create notification stub service

**Description**: Create placeholder notification service for future implementation.

**Details**:
- Implement INotificationService interface with:
  - SendPushNotification(userId, title, body)
  - SendBatchNotifications(userIds[], title, body)
- Create NoOpNotificationService that logs notifications
- Register as singleton
- Add configuration: Notifications:Provider supporting "None" value
- Document for future implementation

**Acceptance Criteria**:
- [ ] Interface is defined
- [ ] NoOp implementation logs correctly
- [ ] Service is registered
- [ ] Configuration exists
- [ ] Documentation explains future use

**Labels**: backend, notifications, stub

---

## Phase 3: Message Retention, Archival & Backup

### Task 18: Create archival worker

**Description**: Implement background worker for archiving old messages.

**Details**:
- In TraliVali.Workers, create ArchivalWorker as BackgroundService
- Run on configurable cron schedule (default: daily 2 AM)
- Query messages older than MessageRetention:RetentionDays (default: 365)
- Process in batches of 1000
- Log progress with Serilog
- Add circuit breaker for Azure Blob failures

**Acceptance Criteria**:
- [ ] Worker runs on schedule
- [ ] Old messages are queried correctly
- [ ] Batch processing works
- [ ] Circuit breaker prevents cascading failures
- [ ] Progress is logged

**Labels**: backend, workers, archival

---

### Task 19: Implement archive export service

**Description**: Create service for exporting conversation archives.

**Details**:
- Create IArchiveService with:
  - ExportConversationMessages(conversationId, startDate, endDate)
- Export to JSON format with structure:
  - exportedAt
  - conversationId
  - conversationName
  - participants[]
  - messagesCount
  - messages[]
- Decrypt messages before export (for readability)
- Include sender names and file references

**Acceptance Criteria**:
- [ ] Archives are exported to JSON
- [ ] Format is correct
- [ ] Messages are decrypted
- [ ] All metadata is included
- [ ] Unit tests pass

**Labels**: backend, archival, service

---

### Task 20: Configure Azure Blob Storage for archives

**Description**: Implement Azure Blob Storage service for archives.

**Details**:
- Create IAzureBlobService with methods:
  - UploadArchive(stream, path)
  - DownloadArchive(path)
  - ListArchives(prefix)
  - DeleteArchive(path)
- Store archives at: archives/{year}/{month}/messages_{conversationId}_{date}.json
- Configure lifecycle policy:
  - Cool tier after 90 days
  - Archive tier after 180 days
- Write tests using Azurite emulator

**Acceptance Criteria**:
- [ ] Blobs can be uploaded/downloaded
- [ ] Path structure is correct
- [ ] Lifecycle policies are configured
- [ ] Tests use Azurite
- [ ] Integration tests pass

**Labels**: backend, azure, storage, archival

---

### Task 21: Implement message cleanup after archival

**Description**: Delete messages from MongoDB after successful archival.

**Details**:
- After successful archive upload, delete archived messages from MongoDB
- Update conversation's recentMessages array if affected
- Log deleted message count
- Add configuration: MessageRetention:DeleteAfterArchive (default: true)
- Write unit tests verifying cleanup logic

**Acceptance Criteria**:
- [ ] Messages are deleted after archival
- [ ] Recent messages are updated
- [ ] Deletion is logged
- [ ] Configuration controls behavior
- [ ] Unit tests pass

**Labels**: backend, archival, cleanup

---

### Task 22: Create backup worker

**Description**: Implement automated backup worker for database.

**Details**:
- Create BackupWorker running daily at 3 AM
- Export MongoDB collections to BSON:
  - users
  - conversations
  - messages
  - invites
  - files
- Compress with gzip
- Upload to Azure Blob: backups/{date}/tralivali_{collection}.bson.gz
- Retain backups for 30 days

**Acceptance Criteria**:
- [ ] Worker runs on schedule
- [ ] All collections are exported
- [ ] Files are compressed
- [ ] Backups are uploaded
- [ ] Integration tests pass

**Labels**: backend, workers, backup

---

### Task 23: Implement backup rotation

**Description**: Implement automatic deletion of old backups.

**Details**:
- After successful backup, delete backups older than Backup:RetentionDays (default: 30)
- List blobs in backups/ container
- Parse dates from paths
- Delete expired backups
- Log retention actions
- Write unit tests

**Acceptance Criteria**:
- [ ] Old backups are identified
- [ ] Old backups are deleted
- [ ] Retention period is configurable
- [ ] Actions are logged
- [ ] Unit tests pass

**Labels**: backend, backup, retention

---

### Task 24: Create admin archival/backup endpoints

**Description**: Create admin API endpoints for manual archival and backup operations.

**Details**:
- Implement AdminController (require admin role) with:
  - POST /admin/archival/trigger (manual archival run)
  - GET /admin/archival/stats (last run, messages archived, storage used)
  - POST /admin/backup/trigger (manual backup)
  - GET /admin/backup/list (list available backups)
  - POST /admin/backup/restore/{date} (restore from backup)
  - GET /admin/archives (list archive files with download URLs)
- Write integration tests

**Acceptance Criteria**:
- [ ] All endpoints require admin role
- [ ] Manual operations work correctly
- [ ] Stats are accurate
- [ ] Restore functionality works
- [ ] Integration tests pass

**Labels**: backend, api, admin, backup, archival

---

## Phase 4: Web Client

### Task 25: Scaffold React web application

**Description**: Create React web application with TypeScript and Vite.

**Details**:
- Create Vite + React + TypeScript project in src/web
- Configure strict TypeScript
- Add ESLint with Airbnb config
- Add Prettier
- Set up folder structure:
  - components/
  - hooks/
  - services/
  - stores/
  - types/
  - utils/
- Add Tailwind CSS for styling
- Configure path aliases
- Add .env.example with VITE_API_URL, VITE_SIGNALR_URL

**Acceptance Criteria**:
- [ ] Project builds successfully
- [ ] TypeScript is configured strictly
- [ ] ESLint and Prettier work
- [ ] Folder structure is correct
- [ ] Tailwind CSS is installed
- [ ] .env.example exists

**Labels**: frontend, setup, react, typescript

---

### Task 26: Create TypeScript API types

**Description**: Define TypeScript types matching backend entities.

**Details**:
Define interfaces:
- User, Conversation, Message, Invite, FileAttachment
- API response types: AuthResponse, ConversationListResponse, MessageListResponse
- Request types: SendMessageRequest, CreateGroupRequest, RegisterRequest
- Export from types/index.ts

**Acceptance Criteria**:
- [ ] All types match backend entities
- [ ] Types are properly exported
- [ ] TypeScript compilation succeeds
- [ ] Types are documented

**Labels**: frontend, typescript, types

---

### Task 27: Implement SignalR client service

**Description**: Create SignalR client service for WebSocket communication.

**Details**:
- Create SignalRService class managing WebSocket connection
- Implement auto-reconnection with exponential backoff
- Queue messages during disconnection
- Provide typed methods matching IChatClient:
  - onReceiveMessage
  - onPresenceUpdate
  - onTypingIndicator
- Handle connection state events
- Write unit tests with mocked SignalR

**Acceptance Criteria**:
- [ ] Connection management works
- [ ] Auto-reconnection works
- [ ] Message queueing works
- [ ] All events are handled
- [ ] Unit tests pass

**Labels**: frontend, signalr, websocket

---

### Task 28: Create API client service

**Description**: Implement HTTP API client service.

**Details**:
- Implement ApiService using fetch
- Add interceptors for JWT token injection and refresh
- Create methods for all API endpoints:
  - auth.*
  - conversations.*
  - messages.*
  - users.*
  - files.*
- Handle errors consistently with typed error responses
- Store JWT in httpOnly cookie or secure localStorage
- Write unit tests

**Acceptance Criteria**:
- [ ] All API methods are implemented
- [ ] Token injection works
- [ ] Token refresh works
- [ ] Error handling is consistent
- [ ] Unit tests pass

**Labels**: frontend, api, http

---

### Task 29: Build Zustand state stores

**Description**: Create Zustand stores for state management.

**Details**:
Create stores:
- useAuthStore (user, token, login, logout, refresh)
- useConversationStore (conversations, activeConversation, messages, loadConversations, loadMessages, addMessage)
- usePresenceStore (onlineUsers, updatePresence)
- Persist auth state to localStorage
- Sync with SignalR events
- Write unit tests

**Acceptance Criteria**:
- [ ] All stores are implemented
- [ ] State persistence works
- [ ] SignalR sync works
- [ ] Unit tests pass

**Labels**: frontend, state-management, zustand

---

### Task 30: Implement IndexedDB offline storage

**Description**: Create offline storage using IndexedDB.

**Details**:
- Using idb library, create OfflineStorage service
- Store conversations and messages locally
- Sync on reconnection
- Queue outgoing messages when offline
- Resolve conflicts using server timestamps
- Write unit tests

**Acceptance Criteria**:
- [ ] Data is stored in IndexedDB
- [ ] Sync works on reconnection
- [ ] Offline queueing works
- [ ] Conflicts are resolved
- [ ] Unit tests pass

**Labels**: frontend, offline, indexeddb

---

### Task 31: Create authentication UI components

**Description**: Build UI components for authentication flow.

**Details**:
Build components:
- LoginPage (email input, request magic link)
- MagicLinkSent (check email message)
- VerifyMagicLink (handle callback, show loading)
- RegisterPage (invite link input, email, displayName form)
- QrScanner (camera-based QR code scanning)
- Style with Tailwind
- Write Vitest component tests

**Acceptance Criteria**:
- [ ] All components render correctly
- [ ] Authentication flow works
- [ ] QR scanner works
- [ ] Styling is consistent
- [ ] Component tests pass

**Labels**: frontend, ui, authentication

---

### Task 32: Build conversation list component

**Description**: Create component for displaying conversations list.

**Details**:
- Create ConversationList showing user's conversations
- Sort by lastMessageAt
- Display:
  - Conversation name/avatar
  - Last message preview
  - Unread count badge
  - Online indicator for direct chats
- Implement search/filter
- Handle empty state
- Click to select conversation
- Write component tests

**Acceptance Criteria**:
- [ ] List displays correctly
- [ ] Sorting works
- [ ] Search/filter works
- [ ] Empty state shows
- [ ] Selection works
- [ ] Component tests pass

**Labels**: frontend, ui, conversations

---

### Task 33: Build message thread component

**Description**: Create component for displaying message thread.

**Details**:
- Create MessageThread displaying messages with:
  - Sender avatar/name
  - Message content
  - Timestamp
  - Read receipts
  - Reply threading
- Support text and file messages
- Implement infinite scroll loading older messages
- Show typing indicators
- Auto-scroll to new messages
- Write component tests

**Acceptance Criteria**:
- [ ] Messages display correctly
- [ ] Infinite scroll works
- [ ] Typing indicators show
- [ ] Auto-scroll works
- [ ] Component tests pass

**Labels**: frontend, ui, messages

---

### Task 34: Build message composer component

**Description**: Create component for composing messages.

**Details**:
- Create MessageComposer with:
  - Text input with Enter to send
  - File attachment button
  - Emoji picker (use emoji-picker-react)
  - Reply preview when replying
  - Typing indicator trigger (debounced)
- Support paste images
- Disable when disconnected
- Write component tests

**Acceptance Criteria**:
- [ ] Text input works
- [ ] File attachment works
- [ ] Emoji picker works
- [ ] Reply feature works
- [ ] Typing indicator triggers
- [ ] Component tests pass

**Labels**: frontend, ui, messages

---

### Task 35: Create user profile and settings components

**Description**: Build user profile and settings UI.

**Details**:
Build components:
- UserProfile (displayName, avatar, email, logout button)
- SettingsPage (notification preferences placeholder, theme toggle dark/light)
- InviteModal (generate invite link, display QR code, copy button, expiry selection)
- Write component tests

**Acceptance Criteria**:
- [ ] Profile displays correctly
- [ ] Settings work
- [ ] Invite modal generates links/QR
- [ ] Theme toggle works
- [ ] Component tests pass

**Labels**: frontend, ui, settings, profile

---

### Task 36: Implement file upload with progress

**Description**: Create file upload service with progress tracking.

**Details**:
- Create FileUploadService using presigned URLs from API
- Show upload progress bar
- Compress images client-side before upload (max 2MB)
- Generate thumbnails for preview
- Handle upload cancellation
- Display file attachments in messages with download option
- Write unit tests

**Acceptance Criteria**:
- [ ] Files upload successfully
- [ ] Progress is shown
- [ ] Images are compressed
- [ ] Thumbnails are generated
- [ ] Cancellation works
- [ ] Unit tests pass

**Labels**: frontend, files, upload

---

### Task 37: Write Playwright E2E tests

**Description**: Create end-to-end test suite using Playwright.

**Details**:
Create E2E test suite covering:
- Registration via invite link
- Login via magic link
- Create direct conversation
- Send text message
- Receive message in real-time
- Create group
- Add member to group
- Upload and send file
- Logout
- Run against Docker Compose environment

**Acceptance Criteria**:
- [ ] All test scenarios pass
- [ ] Tests run against local environment
- [ ] Tests are stable and reliable
- [ ] CI integration ready

**Labels**: frontend, testing, e2e, playwright

---

## Phase 5: End-to-End Encryption

### Task 38: Implement X25519 key exchange in JavaScript

**Description**: Implement key exchange using X25519.

**Details**:
- Using libsodium-wrappers or tweetnacl
- Implement:
  - generateKeyPair()
  - deriveSharedSecret(privateKey, peerPublicKey)
- Store private keys in IndexedDB (encrypted)
- Export public keys for profile
- Write unit tests verifying key exchange

**Acceptance Criteria**:
- [ ] Key pairs are generated
- [ ] Shared secrets are derived
- [ ] Keys are stored securely
- [ ] Unit tests pass with test vectors

**Labels**: frontend, encryption, e2ee

---

### Task 39: Implement AES-256-GCM encryption in JavaScript

**Description**: Implement message encryption using AES-256-GCM.

**Details**:
- Using Web Crypto API
- Add functions:
  - encrypt(key, plaintext)
  - decrypt(key, ciphertext)
- Use random IV per message
- Return {iv, ciphertext, tag}
- Handle decryption failures gracefully
- Write unit tests with test vectors

**Acceptance Criteria**:
- [ ] Encryption works correctly
- [ ] Decryption works correctly
- [ ] IVs are unique
- [ ] Errors are handled
- [ ] Unit tests pass

**Labels**: frontend, encryption, e2ee

---

### Task 40: Create key management service

**Description**: Implement key management for conversations.

**Details**:
- Implement per-conversation symmetric key derivation
- Store conversation keys in IndexedDB (encrypted at rest)
- Implement key rotation when group members change
- Write unit tests

**Acceptance Criteria**:
- [ ] Conversation keys are derived
- [ ] Keys are stored securely
- [ ] Key rotation works
- [ ] Unit tests pass

**Labels**: frontend, encryption, e2ee, key-management

---

### Task 41: Integrate encryption into message flow

**Description**: Add encryption to message sending/receiving.

**Details**:
- Modify MessageComposer to call encrypt before sending
- Modify MessageThread to call decrypt when displaying
- Handle decryption failures (show "Unable to decrypt" placeholder)
- Store encrypted content in Message.encryptedContent
- Store plaintext in local IndexedDB cache only

**Acceptance Criteria**:
- [ ] Messages are encrypted before sending
- [ ] Messages are decrypted on display
- [ ] Failures are handled gracefully
- [ ] Local cache works
- [ ] Integration tests pass

**Labels**: frontend, encryption, e2ee, messages

---

### Task 42: Build key backup and recovery

**Description**: Implement key backup and recovery system.

**Details**:
- Implement password-based key encryption using PBKDF2/Argon2
- Export all private keys and conversation keys to encrypted blob
- Store backup in user's profile on server
- Implement recovery flow on new device/browser login
- Write unit tests

**Acceptance Criteria**:
- [ ] Keys are backed up
- [ ] Backups are encrypted
- [ ] Recovery flow works
- [ ] Unit tests pass

**Labels**: frontend, encryption, e2ee, backup

---

### Task 43: Implement key exchange during invite

**Description**: Include public key in invite flow.

**Details**:
- When generating invite QR code, include inviter's public key
- When accepting invite, generate keypair and derive shared secret
- Display key verification fingerprint (short hash) for manual verification
- Write integration tests

**Acceptance Criteria**:
- [ ] Public key is in invite
- [ ] Key exchange happens on accept
- [ ] Fingerprint is displayed
- [ ] Integration tests pass

**Labels**: frontend, encryption, e2ee, invites

---

### Task 44: Ensure archive readability

**Description**: Make archived messages readable without encryption.

**Details**:
- Modify ArchiveService to decrypt messages before export
- Require user to provide master password for archival
- Store keys separately from archives
- Document archive security model

**Acceptance Criteria**:
- [ ] Archives contain decrypted messages
- [ ] Master password is required
- [ ] Security model is documented
- [ ] Integration tests pass

**Labels**: backend, archival, encryption

---

## Phase 6: File Sharing & Media

### Task 45: Create file upload API

**Description**: Implement file upload API endpoints.

**Details**:
- Implement FilesController with:
  - POST /files/upload-url (generate presigned SAS URL)
  - POST /files/complete (confirm upload, create file record)
  - GET /files/{id}/download-url (generate presigned download URL)
  - DELETE /files/{id} (soft delete)
- Validate file types and sizes
- Write integration tests

**Acceptance Criteria**:
- [ ] Presigned URLs are generated
- [ ] File records are created
- [ ] Download URLs work
- [ ] Validation works
- [ ] Integration tests pass

**Labels**: backend, api, files

---

### Task 46: Implement file worker

**Description**: Create background worker for file processing.

**Details**:
- Create FileProcessorWorker consuming from files.process queue
- For images:
  - Generate thumbnail (300px max dimension)
  - Extract EXIF metadata
  - Store dimensions
- For videos:
  - Extract first frame as thumbnail
  - Store duration
- Update file record with metadata
- Write unit tests

**Acceptance Criteria**:
- [ ] Images are processed
- [ ] Videos are processed
- [ ] Thumbnails are generated
- [ ] Metadata is extracted
- [ ] Unit tests pass

**Labels**: backend, workers, files, media

---

### Task 47: Configure Azure Blob lifecycle policies

**Description**: Set up blob storage lifecycle management.

**Details**:
- Set up lifecycle management rules:
  - Move blobs in files/ container to Cool tier after 30 days
  - Move to Archive tier after 180 days
- Configure in Bicep/ARM template
- Document retrieval process for archived files (rehydration)

**Acceptance Criteria**:
- [ ] Lifecycle policies are configured
- [ ] Policies are in IaC templates
- [ ] Rehydration process is documented

**Labels**: backend, azure, storage, lifecycle

---

### Task 48: Build file attachment UI

**Description**: Create UI components for file attachments.

**Details**:
- Create FileAttachment component displaying:
  - Image thumbnails with lightbox view
  - File icon with name/size for non-images
  - Download button
  - Upload progress during send
- Support drag-and-drop upload
- Implement image compression before upload (max 2048px, 80% quality)
- Write component tests

**Acceptance Criteria**:
- [ ] Attachments display correctly
- [ ] Lightbox works for images
- [ ] Drag-and-drop works
- [ ] Compression works
- [ ] Component tests pass

**Labels**: frontend, ui, files

---

### Task 49: Create media gallery view

**Description**: Build media gallery component.

**Details**:
- Build MediaGallery component showing all files in conversation
- Display as grid
- Filter by type (images, documents, all)
- Lazy load thumbnails
- Click to view/download
- Implement infinite scroll
- Write component tests

**Acceptance Criteria**:
- [ ] Gallery displays files
- [ ] Filtering works
- [ ] Lazy loading works
- [ ] Infinite scroll works
- [ ] Component tests pass

**Labels**: frontend, ui, files, media

---

### Task 50: Implement client-side file encryption

**Description**: Add encryption to file uploads/downloads.

**Details**:
- Before upload, encrypt file using conversation key
- Store encrypted blob
- Decrypt on download before displaying
- Handle large files with streaming encryption
- Write unit tests

**Acceptance Criteria**:
- [ ] Files are encrypted before upload
- [ ] Files are decrypted on download
- [ ] Streaming works for large files
- [ ] Unit tests pass

**Labels**: frontend, files, encryption, e2ee

---

## Phase 7: Azure Deployment

### Task 51: Create Bicep infrastructure templates

**Description**: Write infrastructure as code using Bicep.

**Details**:
Write deploy/azure/main.bicep provisioning:
- Resource Group
- Container Apps Environment
- Container Apps (API)
- Container Instance (MongoDB)
- Storage Account (Blob)
- Azure Communication Services
- Container Registry
- Log Analytics Workspace
- Parameterize for different environments (dev, prod)
- Document all parameters

**Acceptance Criteria**:
- [ ] Template deploys successfully
- [ ] All resources are created
- [ ] Parameters are documented
- [ ] Multiple environments supported

**Labels**: infrastructure, azure, bicep, deployment

---

### Task 52: Create production Docker Compose

**Description**: Create production-ready Docker Compose configuration.

**Details**:
- Write deploy/docker-compose.prod.yml with:
  - No exposed ports except reverse proxy
  - Resource limits
  - Health checks
  - Restart policies
- Create Dockerfile for API with multi-stage build:
  - restore
  - build
  - publish
  - runtime
- Optimize for small image size

**Acceptance Criteria**:
- [ ] Production compose works
- [ ] Security is hardened
- [ ] Resource limits set
- [ ] Dockerfile is optimized

**Labels**: docker, deployment, production

---

### Task 53: Configure Let's Encrypt SSL

**Description**: Set up SSL certificates for HTTPS.

**Details**:
- Use Azure Container Apps managed certificates with custom domain
- Document DNS configuration (CNAME/A records)
- Alternative: Add Caddy reverse proxy container with automatic Let's Encrypt
- Write step-by-step guide for domain setup

**Acceptance Criteria**:
- [ ] SSL certificates work
- [ ] DNS configuration documented
- [ ] Setup guide is complete
- [ ] HTTPS is enforced

**Labels**: infrastructure, ssl, security, deployment

---

### Task 54: Create GitHub Actions CI pipeline

**Description**: Create continuous integration pipeline.

**Details**:
Write .github/workflows/build-test.yml:
- Checkout code
- Setup .NET 8
- Restore dependencies
- Build solution
- Run unit tests
- Run integration tests with Docker services
- Upload test results
- Trigger on PR to main
- Add status checks requirement

**Acceptance Criteria**:
- [ ] Pipeline runs on PR
- [ ] All tests run
- [ ] Results are uploaded
- [ ] Status checks work

**Labels**: ci, github-actions, testing

---

### Task 55: Create GitHub Actions CD pipeline

**Description**: Create continuous deployment pipeline.

**Details**:
Write .github/workflows/deploy.yml:
- Checkout code
- Build Docker image
- Push to Azure Container Registry
- Deploy to Azure Container Apps using Azure CLI
- Use GitHub secrets for Azure credentials
- Trigger on push to main
- Add manual approval for production

**Acceptance Criteria**:
- [ ] Pipeline deploys automatically
- [ ] Docker images are pushed
- [ ] Azure deployment works
- [ ] Manual approval works

**Labels**: cd, github-actions, deployment

---

### Task 56: Write deployment documentation

**Description**: Create comprehensive deployment guide.

**Details**:
Create docs/DEPLOYMENT.md with:
- Azure account setup
- Subscription selection
- Resource group creation
- Running Bicep deployment
- DNS configuration
- SSL certificate setup
- Environment variables configuration
- MongoDB initialization
- First admin user creation
- Verifying deployment
- Troubleshooting guide

**Acceptance Criteria**:
- [ ] Guide is complete
- [ ] All steps are documented
- [ ] Troubleshooting included
- [ ] Examples provided

**Labels**: documentation, deployment

---

### Task 57: Configure Azure monitoring

**Description**: Set up monitoring and alerting.

**Details**:
- Set up Application Insights with .NET SDK
- Configure log streaming to Log Analytics
- Create alerts:
  - Container restart
  - High memory usage
  - 5xx error rate > 1%
  - SignalR connection failures
- Add cost alert at $30/month threshold
- Document monitoring dashboards

**Acceptance Criteria**:
- [ ] Application Insights configured
- [ ] Logs stream to Log Analytics
- [ ] Alerts are created
- [ ] Cost alert works
- [ ] Dashboards documented

**Labels**: infrastructure, monitoring, azure

---

### Task 58: Create local development guide

**Description**: Write guide for local development setup.

**Details**:
Create docs/DEVELOPMENT.md covering:
- Prerequisites (Docker, .NET 8, Node.js 20)
- Cloning repository
- Running Docker Compose
- Running API in debug mode
- Running web client in dev mode
- Running tests
- Code style guidelines
- PR process

**Acceptance Criteria**:
- [ ] Guide is complete
- [ ] Prerequisites listed
- [ ] All steps documented
- [ ] Examples provided

**Labels**: documentation, development

---

## Phase 8: Testing

### Task 59: Write domain entity unit tests

**Description**: Create comprehensive unit tests for domain entities.

**Details**:
- Create xUnit tests for all domain entities
- Test validation logic
- Test business rules
- Test value object equality
- Achieve 100% coverage on TraliVali.Domain
- Use descriptive test names following Given-When-Then pattern

**Acceptance Criteria**:
- [ ] All entities have tests
- [ ] 100% code coverage
- [ ] Test names are descriptive
- [ ] All tests pass

**Labels**: backend, testing, unit-tests, domain

---

### Task 60: Write service layer unit tests

**Description**: Create unit tests for service layer.

**Details**:
- Create xUnit tests for all services in:
  - TraliVali.Auth
  - TraliVali.Messaging
- Use Moq for dependencies
- Test success paths
- Test error handling
- Test edge cases
- Target 90%+ coverage

**Acceptance Criteria**:
- [ ] All services have tests
- [ ] 90%+ code coverage
- [ ] Mocking is used correctly
- [ ] All tests pass

**Labels**: backend, testing, unit-tests, services

---

### Task 61: Write repository integration tests

**Description**: Create integration tests for repositories.

**Details**:
- Create xUnit tests using Testcontainers for MongoDB
- Test all repository methods with real database
- Verify indexes are created
- Test concurrent operations
- Clean up data between tests

**Acceptance Criteria**:
- [ ] All repositories have tests
- [ ] Testcontainers used
- [ ] Indexes verified
- [ ] Concurrency tested
- [ ] All tests pass

**Labels**: backend, testing, integration-tests, mongodb

---

### Task 62: Write SignalR hub integration tests

**Description**: Create integration tests for SignalR hub.

**Details**:
- Create tests using Microsoft.AspNetCore.SignalR.Client
- Test message delivery between multiple clients
- Test presence updates
- Test reconnection scenarios
- Test authorization

**Acceptance Criteria**:
- [ ] Hub has comprehensive tests
- [ ] Multi-client scenarios work
- [ ] Reconnection tested
- [ ] Authorization tested
- [ ] All tests pass

**Labels**: backend, testing, integration-tests, signalr

---

### Task 63: Write RabbitMQ integration tests

**Description**: Create integration tests for RabbitMQ.

**Details**:
- Create tests using Testcontainers for RabbitMQ
- Test message publishing and consumption
- Test dead-letter queue handling
- Test worker processing end-to-end

**Acceptance Criteria**:
- [ ] Publishing tested
- [ ] Consumption tested
- [ ] DLQ tested
- [ ] End-to-end tested
- [ ] All tests pass

**Labels**: backend, testing, integration-tests, rabbitmq

---

### Task 64: Write Azure Blob integration tests

**Description**: Create integration tests for Azure Blob Storage.

**Details**:
- Create tests using Azurite emulator
- Test upload, download, listing, deletion
- Test lifecycle policy application (manual verification)
- Test presigned URL generation

**Acceptance Criteria**:
- [ ] Upload/download tested
- [ ] Listing tested
- [ ] Deletion tested
- [ ] Presigned URLs tested
- [ ] All tests pass

**Labels**: backend, testing, integration-tests, azure

---

### Task 65: Write frontend component tests

**Description**: Create tests for React components.

**Details**:
- Create Vitest tests for all React components
- Test rendering
- Test user interactions
- Test state updates
- Mock API and SignalR services
- Use React Testing Library best practices

**Acceptance Criteria**:
- [ ] All components have tests
- [ ] Interactions tested
- [ ] Mocking used correctly
- [ ] Best practices followed
- [ ] All tests pass

**Labels**: frontend, testing, component-tests

---

### Task 66: Write frontend hook tests

**Description**: Create tests for custom React hooks.

**Details**:
- Create Vitest tests for all custom hooks
- Test state management
- Test side effects
- Test error handling
- Mock external dependencies

**Acceptance Criteria**:
- [ ] All hooks have tests
- [ ] State management tested
- [ ] Side effects tested
- [ ] Error handling tested
- [ ] All tests pass

**Labels**: frontend, testing, hook-tests

---

### Task 67: Write Playwright E2E test suite

**Description**: Create comprehensive end-to-end test suite.

**Details**:
Create comprehensive E2E tests covering:
- Complete user journey from invite to messaging
- Group creation and management
- File upload and download
- Offline queue and sync
- Error handling and recovery
- Run in CI with Docker Compose environment

**Acceptance Criteria**:
- [ ] All user journeys tested
- [ ] Edge cases covered
- [ ] CI integration works
- [ ] Tests are stable
- [ ] All tests pass

**Labels**: testing, e2e, playwright

---

### Task 68: Create test data generators

**Description**: Build factory classes for test data generation.

**Details**:
- Build factory classes for generating test entities:
  - UserFactory
  - ConversationFactory
  - MessageFactory
- Support customization via builder pattern
- Use in all test projects

**Acceptance Criteria**:
- [ ] All factories implemented
- [ ] Builder pattern used
- [ ] Used across test projects
- [ ] Documentation provided

**Labels**: testing, test-data, factories

---

## Phase 9: Documentation

### Task 69: Write API documentation

**Description**: Create comprehensive REST API documentation.

**Details**:
Create docs/API.md with:
- Complete REST API reference
- All endpoints
- Request/response schemas
- Authentication requirements
- Error codes
- Rate limits
- Include curl examples
- Generate OpenAPI spec from controllers

**Acceptance Criteria**:
- [ ] All endpoints documented
- [ ] Examples provided
- [ ] OpenAPI spec generated
- [ ] Error codes listed

**Labels**: documentation, api

---

### Task 70: Write SignalR documentation

**Description**: Create SignalR documentation.

**Details**:
Create docs/SIGNALR.md documenting:
- Hub URL
- Authentication
- All client methods
- All server methods
- Connection lifecycle
- Reconnection handling
- Message format examples

**Acceptance Criteria**:
- [ ] All methods documented
- [ ] Examples provided
- [ ] Lifecycle explained
- [ ] Formats documented

**Labels**: documentation, signalr

---

### Task 71: Write architecture documentation

**Description**: Create architecture documentation.

**Details**:
Create docs/ARCHITECTURE.md with:
- System overview diagram
- Component descriptions
- Data flow diagrams
- Technology choices rationale
- Security model
- Scalability considerations

**Acceptance Criteria**:
- [ ] Diagrams included
- [ ] Components described
- [ ] Rationale explained
- [ ] Security covered

**Labels**: documentation, architecture

---

### Task 72: Write backup and restore guide

**Description**: Create backup and restore documentation.

**Details**:
Create docs/BACKUP-RESTORE.md documenting:
- Automatic backup schedule
- Backup file locations
- Manual backup trigger
- Restore procedure
- Point-in-time recovery
- Disaster recovery plan

**Acceptance Criteria**:
- [ ] Schedule documented
- [ ] Procedures complete
- [ ] Recovery plan included
- [ ] Examples provided

**Labels**: documentation, backup

---

### Task 73: Write security documentation

**Description**: Create security documentation.

**Details**:
Create docs/SECURITY.md covering:
- Authentication flow
- JWT token lifecycle
- E2EE implementation
- Key management
- Data encryption at rest
- Network security
- Security best practices for deployment

**Acceptance Criteria**:
- [ ] All security aspects covered
- [ ] Flows documented
- [ ] Best practices included
- [ ] Examples provided

**Labels**: documentation, security

---

### Task 74: Write user guide

**Description**: Create end-user documentation.

**Details**:
Create docs/USER-GUIDE.md for end users:
- Registration process
- Sending messages
- Creating groups
- Sharing files
- QR code invitation
- Troubleshooting common issues

**Acceptance Criteria**:
- [ ] All features explained
- [ ] Screenshots included
- [ ] Troubleshooting section complete
- [ ] Clear and concise

**Labels**: documentation, user-guide

---

## Implementation Notes

### Dependencies Between Phases

- Phase 1 must be completed before Phase 2
- Phase 2 must be completed before Phase 3
- Phase 4 can start after Phase 1 Task 1-10
- Phase 5 depends on Phase 4 Tasks 25-30
- Phase 6 depends on Phase 2 and Phase 4
- Phase 7 can start after Phase 1-3 are complete
- Phase 8 should run in parallel with implementation
- Phase 9 should be completed last

### Recommended Task Order

1. Start with Phase 1 (Backend Foundation)
2. Begin Phase 4 (Web Client scaffolding) in parallel
3. Complete Phase 2 (Real-Time Messaging)
4. Complete Phase 4 (Web Client UI)
5. Implement Phase 5 (E2EE)
6. Add Phase 3 (Archival & Backup)
7. Add Phase 6 (File Sharing)
8. Deploy with Phase 7 (Azure Deployment)
9. Ensure quality with Phase 8 (Testing)
10. Finish with Phase 9 (Documentation)

### Labels

The following labels should be created in GitHub:
- backend
- frontend
- infrastructure
- setup
- docker
- database
- mongodb
- redis
- rabbitmq
- authentication
- jwt
- api
- signalr
- real-time
- workers
- messaging
- presence
- conversations
- notifications
- archival
- backup
- azure
- storage
- react
- typescript
- websocket
- http
- state-management
- offline
- ui
- files
- media
- encryption
- e2ee
- key-management
- invites
- qr-code
- registration
- email
- bicep
- deployment
- production
- ssl
- security
- ci
- cd
- github-actions
- monitoring
- testing
- unit-tests
- integration-tests
- component-tests
- hook-tests
- e2e
- playwright
- domain
- entities
- services
- test-data
- factories
- documentation
- architecture
- user-guide

---

## Getting Started

To create GitHub issues from this roadmap:

1. Use the provided script: `scripts/create-issues.sh`
2. Or manually create issues from individual task files in `docs/tasks/`
3. Or use the GitHub CLI with labels and milestones

Each task should be tracked as a separate GitHub issue with appropriate labels and linked to a project board for tracking progress.
