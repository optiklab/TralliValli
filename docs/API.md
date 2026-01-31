# TralliVali REST API Documentation

## Table of Contents
- [Overview](#overview)
- [Authentication](#authentication)
- [Rate Limiting](#rate-limiting)
- [Error Codes](#error-codes)
- [Endpoints](#endpoints)
  - [Authentication](#authentication-endpoints)
  - [Conversations](#conversations-endpoints)
  - [Messages](#messages-endpoints)
  - [Files](#files-endpoints)
  - [Key Backups](#key-backups-endpoints)
  - [Admin](#admin-endpoints)

## Overview

TralliVali is a self-hosted, invite-only messaging platform with end-to-end encryption. This API documentation provides comprehensive details about all available REST endpoints.

**Base URL**: `https://your-domain.com`

**API Version**: 1.0

**Content-Type**: `application/json`

## Authentication

TralliVali uses JWT (JSON Web Token) based authentication with RSA256 signing algorithm.

### Authentication Flow

1. **Request Magic Link**: User requests a magic link via email
2. **Verify Magic Link**: User clicks link and exchanges token for JWT tokens
3. **Access Protected Resources**: Use access token in Authorization header
4. **Refresh Token**: Use refresh token to obtain new access tokens

### Using Access Tokens

Include the access token in the `Authorization` header:

```bash
Authorization: Bearer <your-access-token>
```

### Token Expiration

- **Access Token**: 7 days (configurable)
- **Refresh Token**: 30 days (configurable)

### Protected Endpoints

All endpoints except the following require authentication:
- `POST /auth/request-magic-link`
- `POST /auth/verify-magic-link`
- `POST /auth/refresh`
- `GET /auth/invite/{token}`
- `POST /auth/register`

## Rate Limiting

**Note**: Rate limiting is currently not implemented but is planned for production deployment to prevent:
- Email spam attacks
- User enumeration via timing attacks
- API abuse

Recommended rate limits for production:
- **Magic Link Requests**: 5 requests per hour per IP address
- **API Calls**: 100 requests per minute per authenticated user
- **File Uploads**: 10 uploads per hour per user

## Error Codes

### HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 | OK - Request succeeded |
| 201 | Created - Resource created successfully |
| 204 | No Content - Request succeeded with no response body |
| 400 | Bad Request - Invalid request parameters |
| 401 | Unauthorized - Missing or invalid authentication |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource not found |
| 500 | Internal Server Error - Server error occurred |

### Error Response Format

```json
{
  "message": "Error description",
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

### Common Error Codes

| Error Message | Cause | Solution |
|---------------|-------|----------|
| "Invalid or expired magic link" | Magic link token is invalid/expired/used | Request a new magic link |
| "Invalid refresh token" | Refresh token is invalid/expired | Re-authenticate with magic link |
| "User ID claim not found" | JWT token missing userId claim | Re-authenticate |
| "Access denied" | User not participant in conversation | Verify conversation access |
| "Only admins can..." | User lacks admin role | Contact administrator |
| "File type '...' is not allowed" | Unsupported file type | Use allowed file types |
| "Invalid backup data" | Validation errors in backup | Check validation errors |

---

## Endpoints

## Authentication Endpoints

### 1. Request Magic Link

Request a passwordless authentication link to be sent via email.

**Endpoint**: `POST /auth/request-magic-link`

**Authentication**: Not required

**Request Body**:
```json
{
  "email": "user@example.com",
  "deviceId": "unique-device-identifier"
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | Yes | Valid email address |
| deviceId | string | Yes | Unique device identifier |

**Response**: `200 OK`
```json
{
  "message": "If the email exists in our system, a magic link has been sent."
}
```

**Error Responses**:
- `400 Bad Request` - Invalid email format or missing fields
- `500 Internal Server Error` - Email service error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/auth/request-magic-link \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "deviceId": "device-123"
  }'
```

---

### 2. Verify Magic Link

Verify a magic link token and receive JWT tokens for authentication.

**Endpoint**: `POST /auth/verify-magic-link`

**Authentication**: Not required

**Request Body**:
```json
{
  "token": "magic-link-token"
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| token | string | Yes | Magic link token from email |

**Response**: `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-string",
  "expiresAt": "2024-02-07T10:30:00Z",
  "refreshExpiresAt": "2024-03-01T10:30:00Z"
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| accessToken | string | JWT access token for API calls |
| refreshToken | string | Token to refresh access token |
| expiresAt | datetime | Access token expiration time (UTC) |
| refreshExpiresAt | datetime | Refresh token expiration time (UTC) |

**Error Responses**:
- `400 Bad Request` - Missing token
- `401 Unauthorized` - Invalid or expired magic link
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/auth/verify-magic-link \
  -H "Content-Type: application/json" \
  -d '{
    "token": "abc123xyz789"
  }'
```

---

### 3. Refresh Token

Obtain a new access token using a refresh token.

**Endpoint**: `POST /auth/refresh`

**Authentication**: Not required (uses refresh token)

**Request Body**:
```json
{
  "refreshToken": "refresh-token-string"
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| refreshToken | string | Yes | Valid refresh token |

**Response**: `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "new-refresh-token-string",
  "expiresAt": "2024-02-07T10:30:00Z",
  "refreshExpiresAt": "2024-03-01T10:30:00Z"
}
```

**Error Responses**:
- `400 Bad Request` - Missing refresh token
- `401 Unauthorized` - Invalid refresh token
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token"
  }'
```

---

### 4. Logout

Logout by blacklisting the current access token.

**Endpoint**: `POST /auth/logout`

**Authentication**: Not required (token provided in body)

**Request Body**:
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response**: `200 OK`
```json
{
  "message": "Logged out successfully."
}
```

**Error Responses**:
- `400 Bad Request` - Missing access token
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/auth/logout \
  -H "Content-Type: application/json" \
  -d '{
    "accessToken": "your-access-token"
  }'
```

---

### 5. Validate Invite

Validate an invite token before registration.

**Endpoint**: `GET /auth/invite/{token}`

**Authentication**: Not required

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| token | string | Yes | Invite token to validate |

**Response**: `200 OK`
```json
{
  "isValid": true,
  "expiresAt": "2024-02-15T10:30:00Z",
  "message": "Invite is valid."
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| isValid | boolean | Whether the invite is valid |
| expiresAt | datetime | Invite expiration time (UTC) |
| message | string | Status message |

**Error Responses**:
- `404 Not Found` - Invalid or expired invite token
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET https://your-domain.com/auth/invite/abc123xyz789
```

---

### 6. Register

Register a new user account using an invite token.

**Endpoint**: `POST /auth/register`

**Authentication**: Not required

**Request Body**:
```json
{
  "inviteToken": "invite-token-string",
  "email": "newuser@example.com",
  "displayName": "John Doe"
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| inviteToken | string | Yes | Valid invite token |
| email | string | Yes | Email address (will be normalized to lowercase) |
| displayName | string | Yes | Display name (will be trimmed) |

**Response**: `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-string",
  "expiresAt": "2024-02-07T10:30:00Z",
  "refreshExpiresAt": "2024-03-01T10:30:00Z"
}
```

**Error Responses**:
- `400 Bad Request` - Invalid invite token, email already exists, or validation errors
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "inviteToken": "abc123",
    "email": "newuser@example.com",
    "displayName": "John Doe"
  }'
```

---

## Conversations Endpoints

### 1. Get Conversations

Get the current user's conversations with pagination.

**Endpoint**: `GET /conversations`

**Authentication**: Required

**Query Parameters**:
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| page | integer | 1 | Page number (min: 1) |
| pageSize | integer | 20 | Results per page (min: 1, max: 100) |

**Response**: `200 OK`
```json
{
  "conversations": [
    {
      "id": "conv-123",
      "type": "direct",
      "name": "",
      "isGroup": false,
      "participants": [
        {
          "userId": "user-1",
          "role": "member",
          "joinedAt": "2024-01-01T00:00:00Z",
          "lastReadAt": "2024-01-31T10:30:00Z"
        }
      ],
      "recentMessages": [],
      "createdAt": "2024-01-01T00:00:00Z",
      "lastMessageAt": "2024-01-31T10:30:00Z",
      "metadata": {}
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET "https://your-domain.com/conversations?page=1&pageSize=20" \
  -H "Authorization: Bearer your-access-token"
```

---

### 2. Create Direct Conversation

Create a new direct (1-on-1) conversation.

**Endpoint**: `POST /conversations/direct`

**Authentication**: Required

**Request Body**:
```json
{
  "otherUserId": "user-456"
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| otherUserId | string | Yes | ID of the other user |

**Response**: `201 Created`
```json
{
  "id": "conv-789",
  "type": "direct",
  "name": "",
  "isGroup": false,
  "participants": [
    {
      "userId": "user-123",
      "role": "member",
      "joinedAt": "2024-01-31T10:30:00Z",
      "lastReadAt": null
    },
    {
      "userId": "user-456",
      "role": "member",
      "joinedAt": "2024-01-31T10:30:00Z",
      "lastReadAt": null
    }
  ],
  "recentMessages": [],
  "createdAt": "2024-01-31T10:30:00Z",
  "lastMessageAt": null,
  "metadata": {}
}
```

**Error Responses**:
- `400 Bad Request` - Cannot create conversation with self
- `404 Not Found` - Other user not found
- `401 Unauthorized` - Missing or invalid authentication
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/conversations/direct \
  -H "Authorization: Bearer your-access-token" \
  -H "Content-Type: application/json" \
  -d '{
    "otherUserId": "user-456"
  }'
```

---

### 3. Create Group Conversation

Create a new group conversation.

**Endpoint**: `POST /conversations/group`

**Authentication**: Required

**Request Body**:
```json
{
  "name": "Family Group",
  "memberUserIds": ["user-456", "user-789"]
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Group name |
| memberUserIds | string[] | Yes | Array of user IDs to include (creator added automatically) |

**Response**: `201 Created`
```json
{
  "id": "conv-999",
  "type": "group",
  "name": "Family Group",
  "isGroup": true,
  "participants": [
    {
      "userId": "user-123",
      "role": "admin",
      "joinedAt": "2024-01-31T10:30:00Z",
      "lastReadAt": null
    },
    {
      "userId": "user-456",
      "role": "member",
      "joinedAt": "2024-01-31T10:30:00Z",
      "lastReadAt": null
    }
  ],
  "recentMessages": [],
  "createdAt": "2024-01-31T10:30:00Z",
  "lastMessageAt": null,
  "metadata": {}
}
```

**Error Responses**:
- `400 Bad Request` - Invalid user ID or validation errors
- `401 Unauthorized` - Missing or invalid authentication
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/conversations/group \
  -H "Authorization: Bearer your-access-token" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Family Group",
    "memberUserIds": ["user-456", "user-789"]
  }'
```

---

### 4. Get Conversation

Get a specific conversation by ID.

**Endpoint**: `GET /conversations/{id}`

**Authentication**: Required

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | string | Yes | Conversation ID |

**Response**: `200 OK`
```json
{
  "id": "conv-123",
  "type": "direct",
  "name": "",
  "isGroup": false,
  "participants": [...],
  "recentMessages": [],
  "createdAt": "2024-01-01T00:00:00Z",
  "lastMessageAt": "2024-01-31T10:30:00Z",
  "metadata": {}
}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User not a participant
- `404 Not Found` - Conversation not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET https://your-domain.com/conversations/conv-123 \
  -H "Authorization: Bearer your-access-token"
```

---

### 5. Update Group Metadata

Update group conversation name or metadata (admin only).

**Endpoint**: `PUT /conversations/{id}`

**Authentication**: Required (Admin role)

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | string | Yes | Conversation ID |

**Request Body**:
```json
{
  "name": "Updated Group Name",
  "metadata": {
    "description": "Group description",
    "avatar": "avatar-url"
  }
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | No | New group name |
| metadata | object | No | Additional metadata to merge |

**Response**: `200 OK`
```json
{
  "id": "conv-123",
  "type": "group",
  "name": "Updated Group Name",
  "isGroup": true,
  "participants": [...],
  "recentMessages": [],
  "createdAt": "2024-01-01T00:00:00Z",
  "lastMessageAt": "2024-01-31T10:30:00Z",
  "metadata": {
    "description": "Group description",
    "avatar": "avatar-url"
  }
}
```

**Error Responses**:
- `400 Bad Request` - Only group conversations can be updated
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User not admin or not participant
- `404 Not Found` - Conversation not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X PUT https://your-domain.com/conversations/conv-123 \
  -H "Authorization: Bearer your-access-token" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Group Name"
  }'
```

---

### 6. Add Member to Group

Add a new member to a group conversation (admin only).

**Endpoint**: `POST /conversations/{id}/members`

**Authentication**: Required (Admin role)

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | string | Yes | Conversation ID |

**Request Body**:
```json
{
  "userId": "user-999",
  "role": "member"
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| userId | string | Yes | User ID to add |
| role | string | No | Must be "member" (default) |

**Response**: `200 OK`
```json
{
  "id": "conv-123",
  "type": "group",
  "name": "Family Group",
  "isGroup": true,
  "participants": [
    {
      "userId": "user-999",
      "role": "member",
      "joinedAt": "2024-01-31T10:30:00Z",
      "lastReadAt": null
    }
  ],
  "recentMessages": [],
  "createdAt": "2024-01-01T00:00:00Z",
  "lastMessageAt": null,
  "metadata": {}
}
```

**Error Responses**:
- `400 Bad Request` - Only group conversations or user already member
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User not admin
- `404 Not Found` - Conversation or user not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/conversations/conv-123/members \
  -H "Authorization: Bearer your-access-token" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-999"
  }'
```

---

### 7. Remove Member from Group

Remove a member from a group conversation (admin only or self-removal).

**Endpoint**: `DELETE /conversations/{id}/members/{userId}`

**Authentication**: Required (Admin role or self-removal)

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | string | Yes | Conversation ID |
| userId | string | Yes | User ID to remove |

**Response**: `200 OK`
```json
{
  "id": "conv-123",
  "type": "group",
  "name": "Family Group",
  "isGroup": true,
  "participants": [...],
  "recentMessages": [],
  "createdAt": "2024-01-01T00:00:00Z",
  "lastMessageAt": null,
  "metadata": {}
}
```

**Error Responses**:
- `400 Bad Request` - Only group conversations
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User not admin (unless self-removal)
- `404 Not Found` - Conversation or user not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X DELETE https://your-domain.com/conversations/conv-123/members/user-999 \
  -H "Authorization: Bearer your-access-token"
```

---

## Messages Endpoints

### 1. Get Messages

Get messages for a conversation with cursor-based pagination.

**Endpoint**: `GET /conversations/{conversationId}/messages`

**Authentication**: Required

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| conversationId | string | Yes | Conversation ID |

**Query Parameters**:
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| before | string | null | ISO 8601 timestamp cursor for pagination |
| limit | integer | 50 | Messages per page (min: 1, max: 100) |

**Response**: `200 OK`
```json
{
  "messages": [
    {
      "id": "msg-123",
      "conversationId": "conv-456",
      "senderId": "user-789",
      "type": "text",
      "content": "Hello World",
      "encryptedContent": "encrypted-base64-string",
      "replyTo": null,
      "createdAt": "2024-01-31T10:30:00Z",
      "readBy": [
        {
          "userId": "user-123",
          "readAt": "2024-01-31T10:31:00Z"
        }
      ],
      "editedAt": null,
      "isDeleted": false,
      "attachments": []
    }
  ],
  "hasMore": true,
  "nextCursor": "2024-01-31T10:29:00Z",
  "count": 50
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| messages | array | Array of message objects |
| hasMore | boolean | Whether more messages exist |
| nextCursor | datetime | Cursor for next page (use in 'before' param) |
| count | integer | Number of messages in this response |

**Error Responses**:
- `400 Bad Request` - Invalid cursor format
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User not a participant
- `404 Not Found` - Conversation not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET "https://your-domain.com/conversations/conv-456/messages?limit=50" \
  -H "Authorization: Bearer your-access-token"

# With pagination
curl -X GET "https://your-domain.com/conversations/conv-456/messages?before=2024-01-31T10:29:00Z&limit=50" \
  -H "Authorization: Bearer your-access-token"
```

---

### 2. Search Messages

Search messages in a conversation by content.

**Endpoint**: `GET /conversations/{conversationId}/messages/search`

**Authentication**: Required

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| conversationId | string | Yes | Conversation ID |

**Query Parameters**:
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| query | string | - | Search query (required, max 500 characters) |
| limit | integer | 50 | Max results (min: 1, max: 100) |

**Response**: `200 OK`
```json
{
  "messages": [
    {
      "id": "msg-123",
      "conversationId": "conv-456",
      "senderId": "user-789",
      "type": "text",
      "content": "Hello World",
      "encryptedContent": "encrypted-base64-string",
      "replyTo": null,
      "createdAt": "2024-01-31T10:30:00Z",
      "readBy": [],
      "editedAt": null,
      "isDeleted": false,
      "attachments": []
    }
  ],
  "count": 1,
  "query": "Hello"
}
```

**Error Responses**:
- `400 Bad Request` - Missing query or query too long
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User not a participant
- `404 Not Found` - Conversation not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET "https://your-domain.com/conversations/conv-456/messages/search?query=Hello&limit=50" \
  -H "Authorization: Bearer your-access-token"
```

---

### 3. Delete Message

Soft delete a message (sender only).

**Endpoint**: `DELETE /messages/{id}`

**Authentication**: Required

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | string | Yes | Message ID |

**Response**: `204 No Content`

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Only sender can delete or user not participant
- `404 Not Found` - Message or conversation not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X DELETE https://your-domain.com/messages/msg-123 \
  -H "Authorization: Bearer your-access-token"
```

---

## Files Endpoints

### 1. Generate Upload URL

Generate a presigned URL for direct file upload to Azure Blob Storage.

**Endpoint**: `POST /files/upload-url`

**Authentication**: Required

**Request Body**:
```json
{
  "conversationId": "conv-123",
  "fileName": "document.pdf",
  "mimeType": "application/pdf",
  "size": 1048576
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| conversationId | string | Yes | Target conversation ID |
| fileName | string | Yes | Original file name |
| mimeType | string | Yes | File MIME type (must be allowed) |
| size | integer | Yes | File size in bytes (max: 100MB) |

**Allowed MIME Types**:
- **Images**: `image/jpeg`, `image/png`, `image/gif`, `image/webp`, `image/bmp`
- **Documents**: `application/pdf`, `text/plain`, Word, Excel, PowerPoint
- **Archives**: `application/zip`, `application/x-7z-compressed`, `application/x-rar-compressed`
- **Video**: `video/mp4`, `video/mpeg`, `video/quicktime`, `video/x-msvideo`
- **Audio**: `audio/mpeg`, `audio/wav`, `audio/ogg`, `audio/mp4`

**Response**: `200 OK`
```json
{
  "fileId": "file-789",
  "uploadUrl": "https://storage.azure.com/...",
  "blobPath": "files/conv-123/file-789/document.pdf",
  "expiresAt": "2024-01-31T11:30:00Z"
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| fileId | string | Unique file identifier |
| uploadUrl | string | Presigned URL for upload (valid 1 hour) |
| blobPath | string | Blob storage path |
| expiresAt | datetime | Upload URL expiration time (UTC) |

**Error Responses**:
- `400 Bad Request` - Invalid file type or size
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User not a participant
- `404 Not Found` - Conversation not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/files/upload-url \
  -H "Authorization: Bearer your-access-token" \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "conv-123",
    "fileName": "document.pdf",
    "mimeType": "application/pdf",
    "size": 1048576
  }'
```

---

### 2. Complete Upload

Confirm that a file upload has been completed.

**Endpoint**: `POST /files/complete`

**Authentication**: Required

**Request Body**:
```json
{
  "fileId": "file-789"
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| fileId | string | Yes | File ID from upload URL request |

**Response**: `200 OK`
```json
{
  "fileId": "file-789",
  "message": "File upload completed successfully."
}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Only uploader can complete or user not participant
- `404 Not Found` - File or conversation not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/files/complete \
  -H "Authorization: Bearer your-access-token" \
  -H "Content-Type: application/json" \
  -d '{
    "fileId": "file-789"
  }'
```

---

### 3. Generate Download URL

Generate a presigned URL for file download.

**Endpoint**: `GET /files/{id}/download-url`

**Authentication**: Required

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | string | Yes | File ID |

**Response**: `200 OK`
```json
{
  "fileId": "file-789",
  "downloadUrl": "https://storage.azure.com/...",
  "fileName": "document.pdf",
  "expiresAt": "2024-01-31T11:30:00Z"
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| fileId | string | File identifier |
| downloadUrl | string | Presigned URL for download (valid 1 hour) |
| fileName | string | Original file name |
| expiresAt | datetime | Download URL expiration time (UTC) |

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User not a participant
- `404 Not Found` - File not found or deleted
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET https://your-domain.com/files/file-789/download-url \
  -H "Authorization: Bearer your-access-token"
```

---

### 4. Delete File

Soft delete a file (uploader only).

**Endpoint**: `DELETE /files/{id}`

**Authentication**: Required

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | string | Yes | File ID |

**Response**: `204 No Content`

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Only uploader can delete or user not participant
- `404 Not Found` - File or conversation not found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X DELETE https://your-domain.com/files/file-789 \
  -H "Authorization: Bearer your-access-token"
```

---

## Key Backups Endpoints

### 1. Store Key Backup

Store an encrypted key backup for the authenticated user.

**Endpoint**: `POST /key-backups`

**Authentication**: Required

**Request Body**:
```json
{
  "version": 1,
  "encryptedData": "base64-encrypted-keys",
  "iv": "base64-initialization-vector",
  "salt": "base64-salt"
}
```

**Request Schema**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| version | integer | Yes | Backup format version |
| encryptedData | string | Yes | Base64 encoded encrypted data |
| iv | string | Yes | Base64 encoded initialization vector |
| salt | string | Yes | Base64 encoded salt for PBKDF2 |

**Response**: `200 OK`
```json
{
  "backupId": "backup-123",
  "createdAt": "2024-01-31T10:30:00Z",
  "message": "Key backup stored successfully."
}
```

**Security Notes**:
- The server stores only encrypted data
- Never has access to user's password
- Encryption performed client-side using PBKDF2
- Each user can have only one active backup

**Error Responses**:
- `400 Bad Request` - Invalid backup data
- `401 Unauthorized` - Missing or invalid authentication
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/key-backups \
  -H "Authorization: Bearer your-access-token" \
  -H "Content-Type: application/json" \
  -d '{
    "version": 1,
    "encryptedData": "base64-encrypted-keys",
    "iv": "base64-initialization-vector",
    "salt": "base64-salt"
  }'
```

---

### 2. Get Key Backup

Retrieve the encrypted key backup for the authenticated user.

**Endpoint**: `GET /key-backups`

**Authentication**: Required

**Response**: `200 OK`
```json
{
  "backupId": "backup-123",
  "version": 1,
  "encryptedData": "base64-encrypted-keys",
  "iv": "base64-initialization-vector",
  "salt": "base64-salt",
  "createdAt": "2024-01-31T10:30:00Z",
  "updatedAt": "2024-01-31T10:30:00Z"
}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `404 Not Found` - No backup found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET https://your-domain.com/key-backups \
  -H "Authorization: Bearer your-access-token"
```

---

### 3. Check Backup Exists

Check if an encrypted key backup exists for the authenticated user.

**Endpoint**: `GET /key-backups/exists`

**Authentication**: Required

**Response**: `200 OK`
```json
{
  "exists": true,
  "lastUpdatedAt": "2024-01-31T10:30:00Z"
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| exists | boolean | Whether a backup exists |
| lastUpdatedAt | datetime | Last update time (null if no backup) |

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET https://your-domain.com/key-backups/exists \
  -H "Authorization: Bearer your-access-token"
```

---

### 4. Delete Key Backup

Delete the encrypted key backup for the authenticated user.

**Endpoint**: `DELETE /key-backups`

**Authentication**: Required

**Response**: `200 OK`
```json
{
  "message": "Key backup deleted successfully."
}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `404 Not Found` - No backup found
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X DELETE https://your-domain.com/key-backups \
  -H "Authorization: Bearer your-access-token"
```

---

## Admin Endpoints

All admin endpoints require the user to have the "admin" role.

### 1. Trigger Archival

Manually trigger message archival to blob storage.

**Endpoint**: `POST /admin/archival/trigger`

**Authentication**: Required (Admin role)

**Response**: `200 OK`
```json
{
  "message": "Archival completed successfully",
  "messagesArchived": 1000
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| message | string | Status message |
| messagesArchived | integer | Number of messages archived |

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User is not admin
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/admin/archival/trigger \
  -H "Authorization: Bearer your-admin-access-token"
```

---

### 2. Get Archival Stats

Get statistics about message archival.

**Endpoint**: `GET /admin/archival/stats`

**Authentication**: Required (Admin role)

**Response**: `200 OK`
```json
{
  "lastRunAt": "2024-01-31T10:30:00Z",
  "totalMessagesArchived": 50000,
  "totalStorageUsed": 104857600,
  "lastRunStatus": "Success"
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| lastRunAt | datetime | Last archival run time |
| totalMessagesArchived | integer | Total messages archived |
| totalStorageUsed | integer | Total storage used in bytes |
| lastRunStatus | string | Status of last run |

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User is not admin
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET https://your-domain.com/admin/archival/stats \
  -H "Authorization: Bearer your-admin-access-token"
```

---

### 3. Trigger Backup

Manually trigger a database backup.

**Endpoint**: `POST /admin/backup/trigger`

**Authentication**: Required (Admin role)

**Response**: `200 OK`
```json
{
  "backupId": "backup-456",
  "message": "Backup completed successfully",
  "status": "Completed"
}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User is not admin
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/admin/backup/trigger \
  -H "Authorization: Bearer your-admin-access-token"
```

---

### 4. List Backups

List all available database backups.

**Endpoint**: `GET /admin/backup/list`

**Authentication**: Required (Admin role)

**Response**: `200 OK`
```json
{
  "backups": [
    {
      "id": "backup-456",
      "createdAt": "2024-01-31T10:30:00Z",
      "filePath": "/backups/2024-01-31.bson",
      "size": 104857600,
      "type": "Full",
      "status": "Completed"
    }
  ]
}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User is not admin
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET https://your-domain.com/admin/backup/list \
  -H "Authorization: Bearer your-admin-access-token"
```

---

### 5. Restore Backup

Restore from a backup by date.

**Endpoint**: `POST /admin/backup/restore/{date}`

**Authentication**: Required (Admin role)

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| date | string | Yes | Backup date (yyyy-MM-dd format) |

**Response**: `200 OK`
```json
{
  "message": "Backup restored successfully",
  "success": true
}
```

**Error Responses**:
- `400 Bad Request` - Missing or invalid date
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User is not admin
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X POST https://your-domain.com/admin/backup/restore/2024-01-31 \
  -H "Authorization: Bearer your-admin-access-token"
```

---

### 6. List Archives

List all archive files in blob storage.

**Endpoint**: `GET /admin/archives`

**Authentication**: Required (Admin role)

**Response**: `200 OK`
```json
{
  "archives": [
    {
      "path": "archives/2024-01-31/messages.json",
      "downloadUrl": "/admin/archives/download?path=archives/2024-01-31/messages.json"
    }
  ]
}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - User is not admin
- `500 Internal Server Error` - Server error

**cURL Example**:
```bash
curl -X GET https://your-domain.com/admin/archives \
  -H "Authorization: Bearer your-admin-access-token"
```

---

## Additional Resources

### OpenAPI Specification

The complete OpenAPI 3.0 specification is available at:
- Development: `http://localhost:5000/swagger/v1/swagger.json`
- Production: `https://your-domain.com/swagger/v1/swagger.json`

### Interactive API Explorer

Swagger UI is available in development mode:
- Development: `http://localhost:5000/swagger`

### WebSocket (SignalR) Hub

For real-time messaging, connect to the SignalR hub:
- **Endpoint**: `/hubs/chat`
- **Protocol**: WebSocket
- **Authentication**: Include JWT token in query string: `?access_token=your-token`

### Support

For API support or questions:
- GitHub Issues: https://github.com/optiklab/TralliValli/issues
- Documentation: https://github.com/optiklab/TralliValli/tree/main/docs

---

*Last Updated: January 31, 2024*
