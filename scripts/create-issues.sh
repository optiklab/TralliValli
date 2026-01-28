#!/bin/bash

# Script to create GitHub issues for all 74 TralliValli implementation tasks
# Requires: GitHub CLI (gh) installed and authenticated
# Usage: ./create-issues.sh [--dry-run]

set -e

REPO="optiklab/TralliValli"
DRY_RUN=false

# Check for dry-run flag
if [ "$1" == "--dry-run" ]; then
    DRY_RUN=true
    echo "🔍 DRY RUN MODE - No issues will be created"
    echo ""
fi

# Check if gh CLI is installed
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI (gh) is not installed"
    echo "Install it from: https://cli.github.com/"
    exit 1
fi

# Check if authenticated
if ! gh auth status &> /dev/null; then
    echo "❌ GitHub CLI is not authenticated"
    echo "Run: gh auth login"
    exit 1
fi

echo "📋 Creating GitHub issues for TralliValli project"
echo "Repository: $REPO"
echo ""

# Function to create a milestone if it doesn't exist
create_milestone() {
    local title=$1
    local description=$2
    
    if [ "$DRY_RUN" = true ]; then
        echo "  Would create milestone: $title"
        return
    fi
    
    # Check if milestone exists, create if not
    if ! gh api "repos/$REPO/milestones" --jq '.[].title' 2>/dev/null | grep -q "^$title$"; then
        gh api "repos/$REPO/milestones" -f title="$title" -f description="$description" -f state="open" 2>/dev/null || true
    fi
}

# Create all required milestones
echo "🎯 Creating milestones..."

if [ "$DRY_RUN" = true ]; then
    echo "  (Dry run - milestones would be created)"
else
    create_milestone "Phase 1: Backend Foundation" "Core backend infrastructure: .NET solution, Docker, MongoDB, RabbitMQ, authentication"
    create_milestone "Phase 2: Real-Time Messaging" "SignalR hub, message processing, presence tracking, conversation management"
    create_milestone "Phase 3: Archival & Backup" "Message retention, archival workers, Azure Blob storage, backup system"
    create_milestone "Phase 4: Web Client" "React/TypeScript web application with all UI components"
    create_milestone "Phase 5: E2E Encryption" "End-to-end encryption with X25519 key exchange and AES-256-GCM"
    create_milestone "Phase 6: File Sharing" "File upload API, media processing, client-side file encryption"
    create_milestone "Phase 7: Azure Deployment" "Bicep templates, Docker production config, CI/CD pipelines"
    create_milestone "Phase 8: Testing" "Comprehensive test suite: unit, integration, E2E tests"
    create_milestone "Phase 9: Documentation" "API docs, architecture, security, user guide"
fi

echo "✅ Milestones ready"
echo ""

# Function to create a label if it doesn't exist
create_label() {
    local name=$1
    local color=$2
    local description=$3
    
    if [ "$DRY_RUN" = true ]; then
        echo "  Would create label: $name"
        return
    fi
    
    # Check if label exists, create if not
    if ! gh label list --repo "$REPO" --search "$name" --limit 1 | grep -q "^$name"; then
        gh label create "$name" --repo "$REPO" --color "$color" --description "$description" 2>/dev/null || true
    fi
}

# Create all required labels
echo "🏷️  Creating labels..."

if [ "$DRY_RUN" = true ]; then
    echo "  (Dry run - labels would be created)"
else
    # Backend labels
    create_label "backend" "0E8A16" "Backend development"
    create_label "frontend" "1D76DB" "Frontend development"
    create_label "infrastructure" "D4C5F9" "Infrastructure and DevOps"
    create_label "testing" "FBCA04" "Testing related"
    create_label "documentation" "0075CA" "Documentation"
    
    # Technology labels
    create_label "docker" "2496ED" "Docker related"
    create_label "mongodb" "47A248" "MongoDB database"
    create_label "rabbitmq" "FF6600" "RabbitMQ messaging"
    create_label "signalr" "512BD4" "SignalR real-time"
    create_label "redis" "DC382D" "Redis cache"
    create_label "azure" "0089D6" "Azure cloud"
    create_label "typescript" "3178C6" "TypeScript"
    create_label "react" "61DAFB" "React framework"
    
    # Feature labels
    create_label "setup" "C5DEF5" "Project setup"
    create_label "database" "006B75" "Database related"
    create_label "messaging" "7057FF" "Messaging features"
    create_label "email" "EA4AAA" "Email functionality"
    create_label "authentication" "B60205" "Authentication"
    create_label "jwt" "D93F0B" "JWT tokens"
    create_label "invites" "FBCA04" "Invite system"
    create_label "qr-code" "5319E7" "QR code features"
    create_label "registration" "0E8A16" "User registration"
    create_label "api" "1D76DB" "API endpoints"
    create_label "real-time" "7057FF" "Real-time features"
    create_label "workers" "F9D0C4" "Background workers"
    create_label "presence" "BFD4F2" "Presence tracking"
    create_label "conversations" "C2E0C6" "Conversations"
    create_label "messages" "D4C5F9" "Messages"
    create_label "notifications" "FBCA04" "Notifications"
    create_label "stub" "EDEDED" "Stub implementation"
    create_label "archival" "006B75" "Archival features"
    create_label "export" "0075CA" "Export functionality"
    create_label "blob-storage" "0089D6" "Blob storage"
    create_label "cleanup" "B60205" "Cleanup tasks"
    create_label "backup" "0E8A16" "Backup features"
    create_label "rotation" "D93F0B" "Rotation policies"
    create_label "admin" "B60205" "Admin features"
    create_label "service" "1D76DB" "Service layer"
    create_label "state-management" "7057FF" "State management"
    create_label "zustand" "433E38" "Zustand store"
    create_label "offline" "EDEDED" "Offline support"
    create_label "indexeddb" "F9D0C4" "IndexedDB storage"
    create_label "components" "61DAFB" "UI components"
    create_label "websocket" "010101" "WebSocket"
    create_label "http-client" "1D76DB" "HTTP client"
    create_label "file-upload" "0E8A16" "File upload"
    create_label "e2e" "FBCA04" "End-to-end tests"
    create_label "playwright" "2EAD33" "Playwright tests"
    create_label "encryption" "B60205" "Encryption"
    create_label "key-exchange" "D93F0B" "Key exchange"
    create_label "aes-gcm" "5319E7" "AES-GCM encryption"
    create_label "key-management" "006B75" "Key management"
    create_label "integration" "C2E0C6" "Integration"
    create_label "recovery" "FBCA04" "Recovery features"
    create_label "invite" "0E8A16" "Invite features"
    create_label "security" "B60205" "Security"
    create_label "files" "0075CA" "File handling"
    create_label "upload" "1D76DB" "Upload features"
    create_label "media" "7057FF" "Media handling"
    create_label "lifecycle" "D4C5F9" "Lifecycle policies"
    create_label "gallery" "61DAFB" "Gallery view"
    create_label "bicep" "0089D6" "Bicep templates"
    create_label "deployment" "0E8A16" "Deployment"
    create_label "production" "B60205" "Production"
    create_label "ssl" "006B75" "SSL certificates"
    create_label "letsencrypt" "2C3E50" "Let's Encrypt"
    create_label "devops" "F9D0C4" "DevOps"
    create_label "ci" "FBCA04" "Continuous Integration"
    create_label "cd" "0E8A16" "Continuous Deployment"
    create_label "github-actions" "2088FF" "GitHub Actions"
    create_label "monitoring" "7057FF" "Monitoring"
    create_label "observability" "D4C5F9" "Observability"
    create_label "development" "C5DEF5" "Development"
    create_label "unit-tests" "FBCA04" "Unit tests"
    create_label "integration-tests" "F9D0C4" "Integration tests"
    create_label "domain" "0E8A16" "Domain layer"
    create_label "services" "1D76DB" "Services"
    create_label "repository" "006B75" "Repository layer"
    create_label "vitest" "729B1B" "Vitest tests"
    create_label "hooks" "61DAFB" "React hooks"
    create_label "factories" "C2E0C6" "Test factories"
    create_label "test-data" "EDEDED" "Test data"
    create_label "openapi" "85EA2D" "OpenAPI spec"
    create_label "architecture" "7057FF" "Architecture"
    create_label "disaster-recovery" "B60205" "Disaster recovery"
    create_label "user-guide" "0075CA" "User guide"
    create_label "profile" "61DAFB" "User profile"
    create_label "settings" "C5DEF5" "Settings"
    create_label "composer" "1D76DB" "Message composer"
    create_label "entities" "0E8A16" "Domain entities"
fi

echo "✅ Labels ready"
echo ""

# Function to create an issue
create_issue() {
    local number=$1
    local title=$2
    local body=$3
    local labels=$4
    local milestone=$5
    
    if [ "$DRY_RUN" = true ]; then
        echo "Would create: Task $number - $title"
        return
    fi
    
    echo "Creating: Task $number - $title"
    
    # Create the issue
    gh issue create \
        --repo "$REPO" \
        --title "Task $number: $title" \
        --body "$body" \
        --label "$labels" \
        --milestone "$milestone" || echo "  ⚠️  Failed to create issue $number"
    
    # Small delay to avoid rate limiting
    sleep 0.5
}

# Phase 1: Backend Foundation (Milestone 1)
echo "🔨 Phase 1: Backend Foundation"

create_issue 1 "Create .NET solution structure" \
"Initialize a .NET 8 solution with Clean Architecture: TraliVali.Api (ASP.NET Core Web API), TraliVali.Auth, TraliVali.Messaging, TraliVali.Workers, TraliVali.Domain, TraliVali.Infrastructure, TraliVali.Tests. 

Add project references following dependency rules:
- Domain has no dependencies
- Infrastructure references Domain
- Api references all

Include Serilog for structured logging. Document every public class and method with XML comments.

## Acceptance Criteria
- [ ] Solution builds successfully
- [ ] All project references are correct
- [ ] Serilog is configured
- [ ] All public APIs have XML documentation" \
"backend,infrastructure,setup" "Phase 1: Backend Foundation"

create_issue 2 "Create Docker Compose for local development" \
"Create docker-compose.yml with MongoDB (latest), RabbitMQ (3-management), and Redis (7-alpine) containers. 

Configure:
- Volumes for data persistence
- Ports for local debugging
- Health checks for all services
- .env.example with all required environment variables documented

## Acceptance Criteria
- [ ] \`docker-compose up\` starts all services
- [ ] All services pass health checks
- [ ] Data persists across restarts
- [ ] .env.example is complete and documented" \
"docker,infrastructure,setup" "Phase 1: Backend Foundation"

create_issue 3 "Implement MongoDB repository pattern" \
"In TraliVali.Infrastructure, create MongoDbContext and generic IRepository<T> interface with CRUD operations.

Implement repositories for:
- User
- Conversation
- Message
- Invite
- File
- Backup

Add MongoDB indexes:
- users.email (unique)
- messages.conversationId+createdAt
- conversations.participants.userId+lastMessageAt
- invites.token (unique with TTL)

Use TDD approach with xUnit tests using Testcontainers.

## Acceptance Criteria
- [ ] All repositories implement IRepository<T>
- [ ] All indexes are created on startup
- [ ] Tests use Testcontainers for MongoDB
- [ ] 100% test coverage on repository layer" \
"backend,database,mongodb,testing" "Phase 1: Backend Foundation"

create_issue 4 "Define domain entities" \
"In TraliVali.Domain, create entities with full XML documentation:

- **User**: id, email, displayName, passwordHash, publicKey, devices[], createdAt, invitedBy
- **Conversation**: id, type, participants[], recentMessages[50], lastMessageAt, metadata
- **Message**: id, conversationId, senderId, type, content, encryptedContent, replyTo, createdAt, readBy[]
- **Invite**: id, token, inviterId, expiresAt, usedBy, usedAt
- **File**: id, conversationId, uploaderId, fileName, mimeType, size, blobPath, thumbnailPath, createdAt

## Acceptance Criteria
- [ ] All entities defined with correct properties
- [ ] Full XML documentation on all types
- [ ] Validation logic included
- [ ] Unit tests for validation" \
"backend,domain,entities" "Phase 1: Backend Foundation"

create_issue 5 "Configure RabbitMQ infrastructure" \
"In TraliVali.Infrastructure, create RabbitMqService with topic exchange tralivali.messages.

Define queues:
- messages.process
- files.process
- archival.process
- backup.process

Implement:
- IMessagePublisher interface
- IMessageConsumer interface
- Connection resilience with Polly retry policies

Write unit tests with mocked RabbitMQ.

## Acceptance Criteria
- [ ] Exchange and queues created on startup
- [ ] Publishers can send messages
- [ ] Consumers can receive messages
- [ ] Retry policies work correctly
- [ ] Unit tests pass with mocked dependencies" \
"backend,messaging,rabbitmq" "Phase 1: Backend Foundation"

create_issue 6 "Integrate Azure Communication Services Email" \
"Create IEmailService interface and AzureCommunicationEmailService implementation.

Support email types:
- Magic-link authentication
- Invite notification
- Password reset

Use templated HTML emails stored as embedded resources.

Add configuration in appsettings.json:
- Connection string
- Sender address

Write unit tests with mocked Azure SDK.

## Acceptance Criteria
- [ ] Email service can send all email types
- [ ] Templates are properly formatted
- [ ] Configuration is validated on startup
- [ ] Unit tests cover all scenarios" \
"backend,email,azure" "Phase 1: Backend Foundation"

create_issue 7 "Build JWT authentication service" \
"In TraliVali.Auth, create IJwtService with methods:
- GenerateToken(User)
- ValidateToken(string)
- RefreshToken(string)

Configure JWT with:
- RS256 signing
- 7-day expiry
- Refresh token rotation

Add claims: userId, email, displayName, deviceId

Implement token blacklisting for logout using Redis.

Write comprehensive unit tests.

## Acceptance Criteria
- [ ] Tokens are generated with correct claims
- [ ] Token validation works correctly
- [ ] Refresh token rotation implemented
- [ ] Blacklisting prevents token reuse
- [ ] Unit tests cover all edge cases" \
"backend,authentication,jwt" "Phase 1: Backend Foundation"

create_issue 8 "Create magic-link authentication flow" \
"Implement AuthController with endpoints:
- POST /auth/request-magic-link (sends email)
- POST /auth/verify-magic-link (validates token, returns JWT)
- POST /auth/refresh (refresh JWT)
- POST /auth/logout (blacklist token)

Magic links:
- Expire in 15 minutes
- Single-use only
- Store pending magic links in Redis

Write integration tests.

## Acceptance Criteria
- [ ] Magic link emails are sent
- [ ] Links expire after 15 minutes
- [ ] Links are single-use
- [ ] JWT is returned on successful verification
- [ ] Integration tests pass" \
"backend,authentication,api" "Phase 1: Backend Foundation"

create_issue 9 "Build invite link and QR code service" \
"Create IInviteService with methods:
- GenerateInviteLink(inviterId, expiryHours)
- GenerateInviteQrCode(inviteLink)
- ValidateInvite(token)
- RedeemInvite(token, userId)

Use HMAC-SHA256 signed tokens.

Generate QR codes using QRCoder library.

Store invites in MongoDB with TTL.

Write unit tests covering expiry, redemption, and validation.

## Acceptance Criteria
- [ ] Invite links are generated and signed
- [ ] QR codes are generated correctly
- [ ] Invites expire at configured time
- [ ] Invites can only be used once
- [ ] Unit tests pass" \
"backend,invites,qr-code" "Phase 1: Backend Foundation"

create_issue 10 "Implement user registration flow" \
"Create POST /auth/register endpoint accepting:
- invite token
- email
- displayName

Flow:
1. Validate invite
2. Create user
3. Mark invite as used
4. Send welcome email
5. Return JWT

Add GET /auth/invite/{token} to validate invite before registration UI.

Write integration tests for complete flow.

## Acceptance Criteria
- [ ] Registration requires valid invite
- [ ] User is created in database
- [ ] Invite is marked as used
- [ ] Welcome email is sent
- [ ] JWT is returned
- [ ] Integration tests pass" \
"backend,authentication,registration,api" "Phase 1: Backend Foundation"

# Phase 2: Real-Time Messaging Backend (Milestone 2)
echo "🔨 Phase 2: Real-Time Messaging Backend"

create_issue 11 "Create SignalR ChatHub" \
"In TraliVali.Api, create ChatHub with strongly-typed interface IChatClient defining: ReceiveMessage, UserJoined, UserLeft, TypingIndicator, MessageRead, PresenceUpdate.

Implement hub methods:
- SendMessage
- JoinConversation
- LeaveConversation
- StartTyping
- StopTyping
- MarkAsRead

Require JWT authentication. Document all methods with XML comments.

## Acceptance Criteria
- [ ] IChatClient interface defined with all methods
- [ ] ChatHub implements all hub methods
- [ ] JWT authentication required
- [ ] All methods documented with XML comments
- [ ] Unit tests pass" \
"backend,signalr,real-time" "Phase 2: Real-Time Messaging"

create_issue 12 "Implement message processing worker" \
"In TraliVali.Workers, create MessageProcessorWorker consuming from messages.process queue.

Worker should:
- Validate message
- Encrypt content (placeholder for Phase 5)
- Persist to MongoDB
- Update conversation's recentMessages array
- Broadcast via SignalR to conversation participants

Add dead-letter queue for failed messages.

Write unit tests.

## Acceptance Criteria
- [ ] Worker consumes from messages.process queue
- [ ] Messages are validated and persisted
- [ ] Conversation recentMessages updated
- [ ] SignalR broadcast to participants
- [ ] Dead-letter queue configured
- [ ] Unit tests pass" \
"backend,workers,messaging" "Phase 2: Real-Time Messaging"

create_issue 13 "Build presence tracking system" \
"Create IPresenceService using Redis sorted sets to track online users with last-seen timestamps.

Implement methods:
- SetOnline(userId, connectionId)
- SetOffline(userId)
- GetOnlineUsers(userIds[])
- GetLastSeen(userId)

Update presence on SignalR connect/disconnect events. Broadcast presence changes to relevant users.

Write unit tests.

## Acceptance Criteria
- [ ] Redis sorted sets used for presence tracking
- [ ] All interface methods implemented
- [ ] Presence updates on SignalR events
- [ ] Presence changes broadcast to users
- [ ] Unit tests pass" \
"backend,presence,redis,signalr" "Phase 2: Real-Time Messaging"

create_issue 14 "Implement conversation service" \
"Create IConversationService with methods:
- CreateDirectConversation(userId1, userId2)
- CreateGroupConversation(name, creatorId, memberIds[])
- AddMember(conversationId, userId, role)
- RemoveMember(conversationId, userId)
- UpdateGroupMetadata(conversationId, name, avatar)
- GetUserConversations(userId)

Prevent duplicate direct conversations.

Write unit tests.

## Acceptance Criteria
- [ ] All interface methods implemented
- [ ] Duplicate direct conversations prevented
- [ ] Group conversations support multiple members
- [ ] Role-based member management
- [ ] Unit tests pass" \
"backend,conversations,service" "Phase 2: Real-Time Messaging"

create_issue 15 "Create conversation API endpoints" \
"Implement ConversationsController with endpoints:
- GET /conversations (list user's conversations with pagination)
- POST /conversations/direct (create 1-on-1)
- POST /conversations/group (create group)
- GET /conversations/{id} (get with recent messages)
- PUT /conversations/{id} (update group metadata)
- POST /conversations/{id}/members (add member)
- DELETE /conversations/{id}/members/{userId} (remove member)

Write integration tests.

## Acceptance Criteria
- [ ] All endpoints implemented
- [ ] Pagination works correctly
- [ ] Authorization enforced
- [ ] Integration tests pass" \
"backend,api,conversations" "Phase 2: Real-Time Messaging"

create_issue 16 "Implement message history API" \
"Create MessagesController with endpoints:
- GET /conversations/{id}/messages (paginated, cursor-based using message timestamp)
- GET /conversations/{id}/messages/search (full-text search)
- DELETE /messages/{id} (soft delete, mark as deleted)

Support loading 50 messages per page with before cursor.

Write integration tests.

## Acceptance Criteria
- [ ] Cursor-based pagination implemented
- [ ] Full-text search working
- [ ] Soft delete implemented
- [ ] 50 messages per page
- [ ] Integration tests pass" \
"backend,api,messages" "Phase 2: Real-Time Messaging"

create_issue 17 "Create notification stub service" \
"Implement INotificationService interface with methods:
- SendPushNotification(userId, title, body)
- SendBatchNotifications(userIds[], title, body)

Create NoOpNotificationService that logs notifications but takes no action.

Register as singleton.

Add configuration Notifications:Provider supporting None value.

Document for future implementation.

## Acceptance Criteria
- [ ] Interface defined
- [ ] NoOpNotificationService implemented
- [ ] Logging in place
- [ ] Configuration support
- [ ] Documentation complete" \
"backend,notifications,stub" "Phase 2: Real-Time Messaging"

echo ""
echo "✅ Created Phase 2 issues"
echo ""

# Phase 3: Message Retention, Archival & Backup (Milestone 3)
echo "🔨 Phase 3: Message Retention, Archival & Backup"

create_issue 18 "Create archival worker" \
"In TraliVali.Workers, create ArchivalWorker as BackgroundService running on configurable cron schedule (default: daily 2 AM).

- Query messages older than MessageRetention:RetentionDays (default: 365)
- Process in batches of 1000
- Log progress with Serilog
- Add circuit breaker for Azure Blob failures

## Acceptance Criteria
- [ ] BackgroundService runs on cron schedule
- [ ] Messages older than retention days queried
- [ ] Batch processing of 1000 messages
- [ ] Serilog logging in place
- [ ] Circuit breaker for Azure Blob failures" \
"backend,workers,archival" "Phase 3: Archival & Backup"

create_issue 19 "Implement archive export service" \
"Create IArchiveService with ExportConversationMessages(conversationId, startDate, endDate).

Export to JSON format with structure:
- exportedAt
- conversationId
- conversationName
- participants[]
- messagesCount
- messages[]

Decrypt messages before export (for readability). Include sender names and file references.

## Acceptance Criteria
- [ ] Export method implemented
- [ ] JSON format correct
- [ ] Messages decrypted for export
- [ ] Sender names included
- [ ] File references included" \
"backend,archival,export" "Phase 3: Archival & Backup"

create_issue 20 "Configure Azure Blob Storage for archives" \
"Create IAzureBlobService with methods:
- UploadArchive(stream, path)
- DownloadArchive(path)
- ListArchives(prefix)
- DeleteArchive(path)

Store archives at path: archives/{year}/{month}/messages_{conversationId}_{date}.json

Configure lifecycle policy:
- Cool tier after 90 days
- Archive tier after 180 days

Write tests using Azurite emulator.

## Acceptance Criteria
- [ ] All interface methods implemented
- [ ] Archive path structure correct
- [ ] Lifecycle policies configured
- [ ] Tests pass with Azurite" \
"backend,azure,blob-storage,archival" "Phase 3: Archival & Backup"

create_issue 21 "Implement message cleanup after archival" \
"After successful archive upload:
- Delete archived messages from MongoDB messages collection
- Update conversation's recentMessages array if affected
- Log deleted message count

Add configuration MessageRetention:DeleteAfterArchive (default: true).

Write unit tests verifying cleanup logic.

## Acceptance Criteria
- [ ] Messages deleted after archival
- [ ] recentMessages array updated
- [ ] Deletion count logged
- [ ] Configuration option works
- [ ] Unit tests pass" \
"backend,archival,cleanup" "Phase 3: Archival & Backup"

create_issue 22 "Create backup worker" \
"Create BackupWorker running daily at 3 AM.

Export MongoDB collections:
- users
- conversations
- messages
- invites
- files

Export to BSON format using mongodump or MongoDB driver export.

Compress with gzip.

Upload to Azure Blob at: backups/{date}/tralivali_{collection}.bson.gz

Retain backups for 30 days.

## Acceptance Criteria
- [ ] Worker runs daily at 3 AM
- [ ] All collections exported to BSON
- [ ] Files compressed with gzip
- [ ] Uploaded to correct path
- [ ] 30-day retention configured" \
"backend,workers,backup" "Phase 3: Archival & Backup"

create_issue 23 "Implement backup rotation" \
"After successful backup:
- Delete backups older than Backup:RetentionDays (default: 30)
- List blobs in backups/ container
- Parse dates from paths
- Delete expired backups
- Log retention actions

Write unit tests.

## Acceptance Criteria
- [ ] Old backups deleted automatically
- [ ] Configuration for retention days
- [ ] Logging of retention actions
- [ ] Unit tests pass" \
"backend,backup,rotation" "Phase 3: Archival & Backup"

create_issue 24 "Create admin archival/backup endpoints" \
"Implement AdminController (require admin role) with endpoints:
- POST /admin/archival/trigger (manual archival run)
- GET /admin/archival/stats (last run, messages archived, storage used)
- POST /admin/backup/trigger (manual backup)
- GET /admin/backup/list (list available backups)
- POST /admin/backup/restore/{date} (restore from backup)
- GET /admin/archives (list archive files with download URLs)

Write integration tests.

## Acceptance Criteria
- [ ] Admin role required for all endpoints
- [ ] All endpoints implemented
- [ ] Archival stats tracked
- [ ] Backup restore works
- [ ] Integration tests pass" \
"backend,api,admin,archival,backup" "Phase 3: Archival & Backup"

echo ""
echo "✅ Created Phase 3 issues"
echo ""

# Phase 4: Web Client (Milestone 4)
echo "🔨 Phase 4: Web Client"

create_issue 25 "Scaffold React web application" \
"Create Vite + React + TypeScript project in src/web.

Configure:
- Strict TypeScript
- ESLint with Airbnb config
- Prettier

Set up folder structure:
- components/
- hooks/
- services/
- stores/
- types/
- utils/

Add Tailwind CSS for styling.

Configure path aliases.

Add .env.example with VITE_API_URL, VITE_SIGNALR_URL.

## Acceptance Criteria
- [ ] Vite + React + TypeScript project created
- [ ] ESLint and Prettier configured
- [ ] Folder structure in place
- [ ] Tailwind CSS working
- [ ] Path aliases configured
- [ ] .env.example documented" \
"frontend,setup,react,typescript" "Phase 4: Web Client"

create_issue 26 "Create TypeScript API types" \
"Define interfaces matching backend entities:
- User
- Conversation
- Message
- Invite
- FileAttachment

Create API response types:
- AuthResponse
- ConversationListResponse
- MessageListResponse

Create request types:
- SendMessageRequest
- CreateGroupRequest
- RegisterRequest

Export from types/index.ts.

## Acceptance Criteria
- [ ] All entity interfaces defined
- [ ] Response types match backend
- [ ] Request types defined
- [ ] Types exported correctly" \
"frontend,typescript,types" "Phase 4: Web Client"

create_issue 27 "Implement SignalR client service" \
"Create SignalRService class managing WebSocket connection.

Implement:
- Auto-reconnection with exponential backoff
- Queue messages during disconnection
- Typed methods matching IChatClient: onReceiveMessage, onPresenceUpdate, onTypingIndicator
- Handle connection state events

Write unit tests with mocked SignalR.

## Acceptance Criteria
- [ ] WebSocket connection managed
- [ ] Auto-reconnection working
- [ ] Message queuing during disconnection
- [ ] Typed methods implemented
- [ ] Unit tests pass" \
"frontend,signalr,websocket" "Phase 4: Web Client"

create_issue 28 "Create API client service" \
"Implement ApiService using fetch with interceptors for JWT token injection and refresh.

Create methods for all API endpoints:
- auth.*
- conversations.*
- messages.*
- users.*
- files.*

Handle errors consistently with typed error responses.

Store JWT in httpOnly cookie or secure localStorage.

Write unit tests.

## Acceptance Criteria
- [ ] Fetch with interceptors
- [ ] JWT injection and refresh
- [ ] All endpoint methods created
- [ ] Typed error handling
- [ ] Unit tests pass" \
"frontend,api,http-client" "Phase 4: Web Client"

create_issue 29 "Build Zustand state stores" \
"Create stores:
- useAuthStore (user, token, login, logout, refresh)
- useConversationStore (conversations, activeConversation, messages, loadConversations, loadMessages, addMessage)
- usePresenceStore (onlineUsers, updatePresence)

Persist auth state to localStorage.

Sync with SignalR events.

Write unit tests.

## Acceptance Criteria
- [ ] All stores created
- [ ] Auth state persisted
- [ ] SignalR sync working
- [ ] Unit tests pass" \
"frontend,state-management,zustand" "Phase 4: Web Client"

create_issue 30 "Implement IndexedDB offline storage" \
"Using idb library, create OfflineStorage service:
- Store conversations and messages locally
- Sync on reconnection
- Queue outgoing messages when offline
- Resolve conflicts using server timestamps

Write unit tests.

## Acceptance Criteria
- [ ] IndexedDB storage working
- [ ] Sync on reconnection
- [ ] Offline message queue
- [ ] Conflict resolution
- [ ] Unit tests pass" \
"frontend,offline,indexeddb" "Phase 4: Web Client"

create_issue 31 "Create authentication UI components" \
"Build components:
- LoginPage (email input, request magic link)
- MagicLinkSent (check email message)
- VerifyMagicLink (handle callback, show loading)
- RegisterPage (invite link input, email, displayName form)
- QrScanner (camera-based QR code scanning)

Style with Tailwind.

Write Vitest component tests.

## Acceptance Criteria
- [ ] All auth components created
- [ ] Tailwind styling applied
- [ ] QR scanning works
- [ ] Component tests pass" \
"frontend,authentication,components" "Phase 4: Web Client"

create_issue 32 "Build conversation list component" \
"Create ConversationList showing user's conversations sorted by lastMessageAt.

Display:
- Conversation name/avatar
- Last message preview
- Unread count badge
- Online indicator for direct chats

Implement search/filter.

Handle empty state.

Click to select conversation.

Write component tests.

## Acceptance Criteria
- [ ] Conversations sorted by lastMessageAt
- [ ] All display elements present
- [ ] Search/filter working
- [ ] Empty state handled
- [ ] Component tests pass" \
"frontend,conversations,components" "Phase 4: Web Client"

create_issue 33 "Build message thread component" \
"Create MessageThread displaying messages with:
- Sender avatar/name
- Message content
- Timestamp
- Read receipts
- Reply threading

Support text and file messages.

Implement infinite scroll loading older messages.

Show typing indicators.

Auto-scroll to new messages.

Write component tests.

## Acceptance Criteria
- [ ] Messages display correctly
- [ ] Infinite scroll working
- [ ] Typing indicators shown
- [ ] Auto-scroll working
- [ ] Component tests pass" \
"frontend,messages,components" "Phase 4: Web Client"

create_issue 34 "Build message composer component" \
"Create MessageComposer with:
- Text input with Enter to send
- File attachment button
- Emoji picker (use emoji-picker-react)
- Reply preview when replying
- Typing indicator trigger (debounced)

Support paste images.

Disable when disconnected.

Write component tests.

## Acceptance Criteria
- [ ] Text input working
- [ ] File attachment button
- [ ] Emoji picker integrated
- [ ] Reply preview shown
- [ ] Typing indicator debounced
- [ ] Component tests pass" \
"frontend,composer,components" "Phase 4: Web Client"

create_issue 35 "Create user profile and settings components" \
"Build components:
- UserProfile (displayName, avatar, email, logout button)
- SettingsPage (notification preferences placeholder, theme toggle dark/light)
- InviteModal (generate invite link, display QR code, copy button, expiry selection)

Write component tests.

## Acceptance Criteria
- [ ] UserProfile component complete
- [ ] SettingsPage with theme toggle
- [ ] InviteModal with QR code
- [ ] Component tests pass" \
"frontend,profile,settings,components" "Phase 4: Web Client"

create_issue 36 "Implement file upload with progress" \
"Create FileUploadService using presigned URLs from API.

- Show upload progress bar
- Compress images client-side before upload (max 2MB)
- Generate thumbnails for preview
- Handle upload cancellation
- Display file attachments in messages with download option

Write unit tests.

## Acceptance Criteria
- [ ] Presigned URL upload working
- [ ] Progress bar displayed
- [ ] Image compression working
- [ ] Thumbnails generated
- [ ] Cancellation handled
- [ ] Unit tests pass" \
"frontend,file-upload,components" "Phase 4: Web Client"

create_issue 37 "Write Playwright E2E tests" \
"Create E2E test suite covering:
- Registration via invite link
- Login via magic link
- Create direct conversation
- Send text message
- Receive message in real-time
- Create group
- Add member to group
- Upload and send file
- Logout

Run against Docker Compose environment.

## Acceptance Criteria
- [ ] All E2E scenarios covered
- [ ] Tests run against Docker Compose
- [ ] CI integration configured
- [ ] Tests pass reliably" \
"frontend,testing,e2e,playwright" "Phase 4: Web Client"

echo ""
echo "✅ Created Phase 4 issues"
echo ""

# Phase 5: End-to-End Encryption (Milestone 5)
echo "🔨 Phase 5: End-to-End Encryption"

create_issue 38 "Implement X25519 key exchange in JavaScript" \
"Using libsodium-wrappers or tweetnacl, implement:
- generateKeyPair()
- deriveSharedSecret(privateKey, peerPublicKey)

Store private keys in IndexedDB (encrypted).

Export public keys for profile.

Write unit tests verifying key exchange.

## Acceptance Criteria
- [ ] Key pair generation working
- [ ] Shared secret derivation correct
- [ ] Private keys stored encrypted
- [ ] Public keys exportable
- [ ] Unit tests pass" \
"frontend,encryption,key-exchange" "Phase 5: E2E Encryption"

create_issue 39 "Implement AES-256-GCM encryption in JavaScript" \
"Using Web Crypto API, add functions:
- encrypt(key, plaintext)
- decrypt(key, ciphertext)

Use random IV per message.

Return {iv, ciphertext, tag}.

Handle decryption failures gracefully.

Write unit tests with test vectors.

## Acceptance Criteria
- [ ] Encrypt function working
- [ ] Decrypt function working
- [ ] Random IV per message
- [ ] Decryption failures handled
- [ ] Unit tests with test vectors pass" \
"frontend,encryption,aes-gcm" "Phase 5: E2E Encryption"

create_issue 40 "Create key management service" \
"Implement per-conversation symmetric key derivation from shared secrets.

Store conversation keys in IndexedDB (encrypted at rest with user's master key).

Implement key rotation when group members change.

Write unit tests.

## Acceptance Criteria
- [ ] Per-conversation keys derived
- [ ] Keys stored encrypted
- [ ] Key rotation working
- [ ] Unit tests pass" \
"frontend,encryption,key-management" "Phase 5: E2E Encryption"

create_issue 41 "Integrate encryption into message flow" \
"Modify MessageComposer to call encrypt before sending.

Modify MessageThread to call decrypt when displaying.

Handle decryption failures (show \"Unable to decrypt\" placeholder).

Store encrypted content in Message.encryptedContent, plaintext in local IndexedDB cache only.

## Acceptance Criteria
- [ ] Messages encrypted before sending
- [ ] Messages decrypted on display
- [ ] Decryption failures handled gracefully
- [ ] Only encrypted content sent to server
- [ ] Plaintext cached locally only" \
"frontend,encryption,integration" "Phase 5: E2E Encryption"

create_issue 42 "Build key backup and recovery" \
"Implement password-based key encryption using PBKDF2/Argon2 for key derivation.

Export all private keys and conversation keys to encrypted blob.

Store backup in user's profile on server.

Implement recovery flow on new device/browser login.

Write unit tests.

## Acceptance Criteria
- [ ] Password-based key encryption
- [ ] Key export to encrypted blob
- [ ] Backup stored on server
- [ ] Recovery flow working
- [ ] Unit tests pass" \
"frontend,encryption,backup,recovery" "Phase 5: E2E Encryption"

create_issue 43 "Implement key exchange during invite" \
"When generating invite QR code, include inviter's public key.

When accepting invite, generate keypair and derive shared secret.

Display key verification fingerprint (short hash) for manual verification.

Write integration tests.

## Acceptance Criteria
- [ ] Public key in invite QR
- [ ] Keypair generated on accept
- [ ] Shared secret derived
- [ ] Verification fingerprint displayed
- [ ] Integration tests pass" \
"frontend,encryption,invite,key-exchange" "Phase 5: E2E Encryption"

create_issue 44 "Ensure archive readability" \
"Modify ArchiveService to decrypt messages before export using conversation keys.

Requires user to provide master password for archival.

Store keys separately from archives.

Document archive security model.

## Acceptance Criteria
- [ ] Messages decrypted for archive
- [ ] Master password required
- [ ] Keys stored separately
- [ ] Security model documented" \
"backend,archival,encryption,security" "Phase 5: E2E Encryption"

echo ""
echo "✅ Created Phase 5 issues"
echo ""

# Phase 6: File Sharing & Media (Milestone 6)
echo "🔨 Phase 6: File Sharing & Media"

create_issue 45 "Create file upload API" \
"Implement FilesController with endpoints:
- POST /files/upload-url (generate presigned SAS URL for direct upload)
- POST /files/complete (confirm upload, create file record)
- GET /files/{id}/download-url (generate presigned download URL)
- DELETE /files/{id} (soft delete)

Validate file types and sizes.

Write integration tests.

## Acceptance Criteria
- [ ] Presigned upload URL generation
- [ ] Upload completion endpoint
- [ ] Download URL generation
- [ ] Soft delete working
- [ ] File validation
- [ ] Integration tests pass" \
"backend,api,files,upload" "Phase 6: File Sharing"

create_issue 46 "Implement file worker" \
"Create FileProcessorWorker consuming from files.process queue.

For images:
- Generate thumbnail (300px max dimension)
- Extract EXIF metadata
- Store dimensions

For videos:
- Extract first frame as thumbnail
- Store duration

Update file record with metadata.

Write unit tests.

## Acceptance Criteria
- [ ] Worker consumes from queue
- [ ] Image thumbnails generated
- [ ] EXIF metadata extracted
- [ ] Video thumbnails from first frame
- [ ] File records updated
- [ ] Unit tests pass" \
"backend,workers,files,media" "Phase 6: File Sharing"

create_issue 47 "Configure Azure Blob lifecycle policies" \
"Set up lifecycle management rules:
- Move blobs in files/ container to Cool tier after 30 days
- Move to Archive tier after 180 days

Configure in Bicep/ARM template.

Document retrieval process for archived files (rehydration).

## Acceptance Criteria
- [ ] Cool tier after 30 days
- [ ] Archive tier after 180 days
- [ ] Bicep template updated
- [ ] Rehydration documented" \
"infrastructure,azure,blob-storage,lifecycle" "Phase 6: File Sharing"

create_issue 48 "Build file attachment UI" \
"Create FileAttachment component displaying:
- Image thumbnails with lightbox view
- File icon with name/size for non-images
- Download button
- Upload progress during send

Support drag-and-drop upload.

Implement image compression before upload (max 2048px, 80% quality).

Write component tests.

## Acceptance Criteria
- [ ] Image thumbnails with lightbox
- [ ] File icons for non-images
- [ ] Download button working
- [ ] Drag-and-drop upload
- [ ] Image compression
- [ ] Component tests pass" \
"frontend,files,components" "Phase 6: File Sharing"

create_issue 49 "Create media gallery view" \
"Build MediaGallery component showing all files in a conversation as grid.

- Filter by type (images, documents, all)
- Lazy load thumbnails
- Click to view/download
- Implement infinite scroll

Write component tests.

## Acceptance Criteria
- [ ] Grid display of files
- [ ] Type filtering
- [ ] Lazy loading
- [ ] Click to view/download
- [ ] Infinite scroll
- [ ] Component tests pass" \
"frontend,media,gallery,components" "Phase 6: File Sharing"

create_issue 50 "Implement client-side file encryption" \
"Before upload, encrypt file using conversation key.

Store encrypted blob.

Decrypt on download before displaying.

Handle large files with streaming encryption.

Write unit tests.

## Acceptance Criteria
- [ ] Files encrypted before upload
- [ ] Encrypted blob stored
- [ ] Decryption on download
- [ ] Streaming encryption for large files
- [ ] Unit tests pass" \
"frontend,encryption,files" "Phase 6: File Sharing"

echo ""
echo "✅ Created Phase 6 issues"
echo ""

# Phase 7: Azure Deployment (Milestone 7)
echo "🔨 Phase 7: Azure Deployment"

create_issue 51 "Create Bicep infrastructure templates" \
"Write deploy/azure/main.bicep provisioning:
- Resource Group
- Container Apps Environment
- Container Apps (API)
- Container Instance (MongoDB)
- Storage Account (Blob)
- Azure Communication Services
- Container Registry
- Log Analytics Workspace

Parameterize for different environments (dev, prod).

Document all parameters.

## Acceptance Criteria
- [ ] All resources defined in Bicep
- [ ] Environment parameterization
- [ ] Documentation complete
- [ ] Deployment succeeds" \
"infrastructure,azure,bicep,deployment" "Phase 7: Azure Deployment"

create_issue 52 "Create production Docker Compose" \
"Write deploy/docker-compose.prod.yml with production-ready configurations:
- No exposed ports except reverse proxy
- Resource limits
- Health checks
- Restart policies

Create Dockerfile for API with multi-stage build (restore, build, publish, runtime).

Optimize for small image size.

## Acceptance Criteria
- [ ] Production Docker Compose created
- [ ] No unnecessary exposed ports
- [ ] Resource limits configured
- [ ] Health checks defined
- [ ] Multi-stage Dockerfile
- [ ] Optimized image size" \
"infrastructure,docker,production" "Phase 7: Azure Deployment"

create_issue 53 "Configure Let's Encrypt SSL" \
"Use Azure Container Apps managed certificates with custom domain.

Document DNS configuration (CNAME/A records).

Alternative: Add Caddy reverse proxy container with automatic Let's Encrypt.

Write step-by-step guide for domain setup.

## Acceptance Criteria
- [ ] SSL certificates configured
- [ ] DNS documentation complete
- [ ] Caddy alternative documented
- [ ] Step-by-step guide written" \
"infrastructure,ssl,letsencrypt,security" "Phase 7: Azure Deployment"

create_issue 54 "Create GitHub Actions CI pipeline" \
"Write .github/workflows/build-test.yml:
- Checkout
- Setup .NET 8
- Restore
- Build
- Run unit tests
- Run integration tests with Docker services
- Upload test results

Trigger on PR to main.

Add status checks requirement.

## Acceptance Criteria
- [ ] CI workflow created
- [ ] All tests run
- [ ] Docker services for integration tests
- [ ] Test results uploaded
- [ ] Status checks configured" \
"devops,ci,github-actions" "Phase 7: Azure Deployment"

create_issue 55 "Create GitHub Actions CD pipeline" \
"Write .github/workflows/deploy.yml:
- Checkout
- Build Docker image
- Push to Azure Container Registry
- Deploy to Azure Container Apps using Azure CLI

Use GitHub secrets for Azure credentials.

Trigger on push to main.

Add manual approval for production.

## Acceptance Criteria
- [ ] CD workflow created
- [ ] Docker image built and pushed
- [ ] Azure Container Apps deployment
- [ ] Secrets configured
- [ ] Manual approval for production" \
"devops,cd,github-actions,azure" "Phase 7: Azure Deployment"

create_issue 56 "Write deployment documentation" \
"Create docs/DEPLOYMENT.md with complete guide:
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

## Acceptance Criteria
- [ ] All deployment steps documented
- [ ] Screenshots/examples included
- [ ] Troubleshooting section complete
- [ ] Tested end-to-end" \
"documentation,deployment,azure" "Phase 7: Azure Deployment"

create_issue 57 "Configure Azure monitoring" \
"Set up Application Insights with .NET SDK.

Configure log streaming to Log Analytics.

Create alerts:
- Container restart
- High memory usage
- 5xx error rate > 1%
- SignalR connection failures

Add cost alert at \$30/month threshold.

Document monitoring dashboards.

## Acceptance Criteria
- [ ] Application Insights configured
- [ ] Log streaming working
- [ ] All alerts created
- [ ] Cost alert configured
- [ ] Dashboards documented" \
"infrastructure,monitoring,azure,observability" "Phase 7: Azure Deployment"

create_issue 58 "Create local development guide" \
"Write docs/DEVELOPMENT.md covering:
- Prerequisites (Docker, .NET 8, Node.js 20)
- Cloning repository
- Running Docker Compose
- Running API in debug mode
- Running web client in dev mode
- Running tests
- Code style guidelines
- PR process

## Acceptance Criteria
- [ ] All prerequisites listed
- [ ] Step-by-step instructions
- [ ] Debug mode instructions
- [ ] Test running documented
- [ ] Code style guidelines
- [ ] PR process documented" \
"documentation,development,setup" "Phase 7: Azure Deployment"

echo ""
echo "✅ Created Phase 7 issues"
echo ""

# Phase 8: Testing (Milestone 8)
echo "🔨 Phase 8: Testing"

create_issue 59 "Write domain entity unit tests" \
"Create xUnit tests for all domain entities:
- Validation logic
- Business rules
- Value object equality

Achieve 100% coverage on TraliVali.Domain.

Use descriptive test names following Given-When-Then pattern.

## Acceptance Criteria
- [ ] All entities tested
- [ ] Validation logic tested
- [ ] Business rules tested
- [ ] 100% coverage on Domain
- [ ] Given-When-Then naming" \
"testing,unit-tests,domain" "Phase 8: Testing"

create_issue 60 "Write service layer unit tests" \
"Create xUnit tests for all services in TraliVali.Auth, TraliVali.Messaging using Moq for dependencies.

Test:
- Success paths
- Error handling
- Edge cases

Target 90%+ coverage.

## Acceptance Criteria
- [ ] All services tested
- [ ] Moq used for dependencies
- [ ] Success paths tested
- [ ] Error handling tested
- [ ] 90%+ coverage achieved" \
"testing,unit-tests,services" "Phase 8: Testing"

create_issue 61 "Write repository integration tests" \
"Create xUnit tests using Testcontainers for MongoDB.

Test all repository methods with real database.

Verify indexes are created.

Test concurrent operations.

Clean up data between tests.

## Acceptance Criteria
- [ ] Testcontainers for MongoDB
- [ ] All repository methods tested
- [ ] Index creation verified
- [ ] Concurrent operations tested
- [ ] Data cleanup between tests" \
"testing,integration-tests,mongodb,repository" "Phase 8: Testing"

create_issue 62 "Write SignalR hub integration tests" \
"Create tests using Microsoft.AspNetCore.SignalR.Client.

Test:
- Message delivery between multiple clients
- Presence updates
- Reconnection scenarios
- Authorization

## Acceptance Criteria
- [ ] SignalR client used
- [ ] Multi-client message delivery tested
- [ ] Presence updates tested
- [ ] Reconnection tested
- [ ] Authorization tested" \
"testing,integration-tests,signalr" "Phase 8: Testing"

create_issue 63 "Write RabbitMQ integration tests" \
"Create tests using Testcontainers for RabbitMQ.

Test:
- Message publishing and consumption
- Dead-letter queue handling
- Worker processing end-to-end

## Acceptance Criteria
- [ ] Testcontainers for RabbitMQ
- [ ] Publishing tested
- [ ] Consumption tested
- [ ] Dead-letter queue tested
- [ ] Worker E2E tested" \
"testing,integration-tests,rabbitmq" "Phase 8: Testing"

create_issue 64 "Write Azure Blob integration tests" \
"Create tests using Azurite emulator.

Test:
- Upload
- Download
- Listing
- Deletion
- Lifecycle policy application (manual verification)
- Presigned URL generation

## Acceptance Criteria
- [ ] Azurite emulator used
- [ ] Upload tested
- [ ] Download tested
- [ ] Listing tested
- [ ] Deletion tested
- [ ] Presigned URLs tested" \
"testing,integration-tests,azure,blob-storage" "Phase 8: Testing"

create_issue 65 "Write frontend component tests" \
"Create Vitest tests for all React components.

Test:
- Rendering
- User interactions
- State updates

Mock API and SignalR services.

Use React Testing Library best practices.

## Acceptance Criteria
- [ ] All components tested
- [ ] Rendering tested
- [ ] Interactions tested
- [ ] State updates tested
- [ ] Mocks in place
- [ ] RTL best practices followed" \
"testing,frontend,vitest,components" "Phase 8: Testing"

create_issue 66 "Write frontend hook tests" \
"Create Vitest tests for all custom hooks.

Test:
- State management
- Side effects
- Error handling

Mock external dependencies.

## Acceptance Criteria
- [ ] All hooks tested
- [ ] State management tested
- [ ] Side effects tested
- [ ] Error handling tested
- [ ] Dependencies mocked" \
"testing,frontend,vitest,hooks" "Phase 8: Testing"

create_issue 67 "Write Playwright E2E test suite" \
"Create comprehensive E2E tests:
- Complete user journey from invite to messaging
- Group creation and management
- File upload and download
- Offline queue and sync
- Error handling and recovery

Run in CI with Docker Compose environment.

## Acceptance Criteria
- [ ] User journey tested
- [ ] Group management tested
- [ ] File operations tested
- [ ] Offline scenarios tested
- [ ] Error recovery tested
- [ ] CI integration working" \
"testing,e2e,playwright" "Phase 8: Testing"

create_issue 68 "Create test data generators" \
"Build factory classes for generating test entities:
- UserFactory
- ConversationFactory
- MessageFactory

Support customization via builder pattern.

Use in all test projects.

## Acceptance Criteria
- [ ] All factory classes created
- [ ] Builder pattern implemented
- [ ] Used across test projects
- [ ] Documentation complete" \
"testing,factories,test-data" "Phase 8: Testing"

echo ""
echo "✅ Created Phase 8 issues"
echo ""

# Phase 9: Documentation (Milestone 9)
echo "🔨 Phase 9: Documentation"

create_issue 69 "Write API documentation" \
"Create docs/API.md with complete REST API reference:
- All endpoints
- Request/response schemas
- Authentication requirements
- Error codes
- Rate limits

Include curl examples.

Generate OpenAPI spec from controllers.

## Acceptance Criteria
- [ ] All endpoints documented
- [ ] Request/response schemas
- [ ] Auth requirements
- [ ] Error codes listed
- [ ] Rate limits documented
- [ ] curl examples included
- [ ] OpenAPI spec generated" \
"documentation,api,openapi" "Phase 9: Documentation"

create_issue 70 "Write SignalR documentation" \
"Create docs/SIGNALR.md documenting:
- Hub URL
- Authentication
- All client methods
- All server methods
- Connection lifecycle
- Reconnection handling
- Message format examples

## Acceptance Criteria
- [ ] Hub URL documented
- [ ] Auth documented
- [ ] Client methods documented
- [ ] Server methods documented
- [ ] Lifecycle documented
- [ ] Reconnection documented
- [ ] Examples included" \
"documentation,signalr,real-time" "Phase 9: Documentation"

create_issue 71 "Write architecture documentation" \
"Create docs/ARCHITECTURE.md with:
- System overview diagram
- Component descriptions
- Data flow diagrams
- Technology choices rationale
- Security model
- Scalability considerations

## Acceptance Criteria
- [ ] System diagram included
- [ ] Components described
- [ ] Data flow diagrams
- [ ] Technology rationale
- [ ] Security model documented
- [ ] Scalability discussed" \
"documentation,architecture" "Phase 9: Documentation"

create_issue 72 "Write backup and restore guide" \
"Create docs/BACKUP-RESTORE.md documenting:
- Automatic backup schedule
- Backup file locations
- Manual backup trigger
- Restore procedure
- Point-in-time recovery
- Disaster recovery plan

## Acceptance Criteria
- [ ] Backup schedule documented
- [ ] File locations documented
- [ ] Manual trigger documented
- [ ] Restore procedure complete
- [ ] PITR documented
- [ ] DR plan included" \
"documentation,backup,disaster-recovery" "Phase 9: Documentation"

create_issue 73 "Write security documentation" \
"Create docs/SECURITY.md covering:
- Authentication flow
- JWT token lifecycle
- E2EE implementation
- Key management
- Data encryption at rest
- Network security
- Security best practices for deployment

## Acceptance Criteria
- [ ] Auth flow documented
- [ ] JWT lifecycle documented
- [ ] E2EE implementation documented
- [ ] Key management documented
- [ ] Encryption at rest documented
- [ ] Network security documented
- [ ] Best practices included" \
"documentation,security,encryption" "Phase 9: Documentation"

create_issue 74 "Write user guide" \
"Create docs/USER-GUIDE.md for end users:
- Registration process
- Sending messages
- Creating groups
- Sharing files
- QR code invitation
- Troubleshooting common issues

## Acceptance Criteria
- [ ] Registration documented
- [ ] Messaging documented
- [ ] Groups documented
- [ ] File sharing documented
- [ ] QR invitations documented
- [ ] Troubleshooting section" \
"documentation,user-guide" "Phase 9: Documentation"

echo ""
echo "✅ Created all Phase 9 issues"
echo ""

echo "🎉 All 74 issues created successfully!"
echo ""

if [ "$DRY_RUN" = true ]; then
    echo "🔍 This was a dry run. No issues were actually created."
    echo "   Run without --dry-run to create issues."
fi
