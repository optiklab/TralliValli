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

# Continue with remaining tasks...
echo ""
echo "✅ Created all Phase 1 issues"
echo ""
echo "ℹ️  NOTE: This script only creates Phase 1 issues as an example."
echo "   Modify the script to add remaining phases (2-9) with all 74 tasks."
echo "   See docs/PROJECT_ROADMAP.md for complete task details."
echo ""

if [ "$DRY_RUN" = true ]; then
    echo "🔍 This was a dry run. No issues were actually created."
    echo "   Run without --dry-run to create issues."
fi
