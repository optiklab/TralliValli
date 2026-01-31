# Test Data Factories

This directory contains factory classes for generating test entities with support for the builder pattern.

## Overview

The factories provide a fluent API for creating test data with sensible defaults, making tests more readable and maintainable. Each factory supports customization through method chaining (builder pattern).

## Available Factories

### UserFactory

Creates `User` entities with the following features:

**Default values:**
- Random unique email (e.g., `test.user.abc12345@example.com`)
- Display name: "Test User"
- Password hash: "hashed_password_123"
- Public key: "public_key_123"
- Role: "user"
- IsActive: true
- Devices: empty list

**Example usage:**

```csharp
using TraliVali.Tests.Data.Factories;

// Create a basic valid user with defaults
var user = UserFactory.BuildValid();

// Customize using builder pattern
var customUser = UserFactory.Create()
    .WithEmail("john.doe@example.com")
    .WithDisplayName("John Doe")
    .AsAdmin()
    .WithDevice("device-1", "iPhone 15", "mobile")
    .WithLastLoginAt(DateTime.UtcNow)
    .Build();

// Create an inactive user
var inactiveUser = UserFactory.Create()
    .WithEmail("inactive@example.com")
    .AsInactive()
    .Build();

// Create an invalid user for validation tests
var invalidUser = UserFactory.BuildInvalid();
```

**Available methods:**
- `WithId(string)` - Set user ID
- `WithEmail(string)` - Set email
- `WithDisplayName(string)` - Set display name
- `WithPasswordHash(string)` - Set password hash
- `WithPublicKey(string)` - Set public key
- `WithDevice(string, string, string)` - Add a device
- `WithDevices(List<Device>)` - Set devices list
- `WithCreatedAt(DateTime)` - Set creation timestamp
- `WithInvitedBy(string)` - Set inviter ID
- `WithLastLoginAt(DateTime)` - Set last login timestamp
- `WithRole(string)` - Set user role
- `AsAdmin()` - Set as admin user
- `AsActive()` - Set as active user
- `AsInactive()` - Set as inactive user
- `Build()` - Build and return the entity
- `BuildValid()` (static) - Quick valid user
- `BuildInvalid()` (static) - Quick invalid user

### ConversationFactory

Creates `Conversation` entities with the following features:

**Default values:**
- Type: "direct"
- Name: "Test Conversation"
- IsGroup: false
- Participants: empty list
- RecentMessages: empty list
- Metadata: empty dictionary

**Example usage:**

```csharp
using TraliVali.Tests.Data.Factories;

// Create a basic valid conversation
var conversation = ConversationFactory.BuildValid();

// Create a direct conversation between two users
var userId1 = ObjectId.GenerateNewId().ToString();
var userId2 = ObjectId.GenerateNewId().ToString();
var directChat = ConversationFactory.BuildDirectConversation(userId1, userId2);

// Create a group conversation
var groupChat = ConversationFactory.BuildGroupConversation(
    "Project Team",
    user1.Id, user2.Id, user3.Id
);

// Customize using builder pattern
var customConversation = ConversationFactory.Create()
    .AsGroup()
    .WithName("My Group Chat")
    .WithParticipant(user1.Id, "admin")
    .WithParticipant(user2.Id, "member")
    .WithLastMessageAt(DateTime.UtcNow)
    .WithMetadata("purpose", "team-discussion")
    .Build();

// Create an invalid conversation for validation tests
var invalidConversation = ConversationFactory.BuildInvalid();
```

**Available methods:**
- `WithId(string)` - Set conversation ID
- `WithType(string)` - Set conversation type
- `AsDirect()` - Set as direct message
- `AsGroup()` - Set as group conversation
- `AsChannel()` - Set as channel
- `WithName(string)` - Set conversation name
- `WithParticipant(string, string, DateTime?)` - Add a participant
- `WithParticipants(List<Participant>)` - Set participants list
- `WithAdminParticipant(string)` - Add participant as admin
- `WithRecentMessage(string)` - Add recent message ID
- `WithRecentMessages(List<string>)` - Set recent messages list
- `WithCreatedAt(DateTime)` - Set creation timestamp
- `WithLastMessageAt(DateTime)` - Set last message timestamp
- `WithMetadata(string, string)` - Add metadata entry
- `WithMetadata(Dictionary<string, string>)` - Set metadata dictionary
- `Build()` - Build and return the entity
- `BuildValid()` (static) - Quick valid conversation
- `BuildDirectConversation(string, string)` (static) - Direct conversation
- `BuildGroupConversation(string, params string[])` (static) - Group conversation
- `BuildInvalid()` (static) - Quick invalid conversation

### MessageFactory

Creates `Message` entities with the following features:

**Default values:**
- Type: "text"
- Content: "Test message content"
- EncryptedContent: ""
- IsDeleted: false
- ConversationId: auto-generated
- SenderId: auto-generated
- ReadBy: empty list
- Attachments: empty list

**Example usage:**

```csharp
using TraliVali.Tests.Data.Factories;

// Create a basic valid message
var message = MessageFactory.BuildValid();

// Create a text message in a conversation
var textMessage = MessageFactory.BuildTextMessage(
    conversationId: conversation.Id,
    senderId: user.Id,
    content: "Hello, World!"
);

// Create an encrypted message
var encryptedMsg = MessageFactory.BuildEncryptedMessage(
    conversationId: conversation.Id,
    senderId: user.Id,
    encryptedContent: "base64_encrypted_content_here"
);

// Customize using builder pattern
var customMessage = MessageFactory.Create()
    .WithConversationId(conversation.Id)
    .WithSenderId(user.Id)
    .WithContent("This is a reply")
    .AsReplyTo(previousMessage.Id)
    .WithReadBy(user1.Id)
    .WithReadBy(user2.Id)
    .WithAttachment("https://example.com/file.pdf")
    .Build();

// Create a system message
var systemMsg = MessageFactory.Create()
    .AsSystem()
    .WithContent("User joined the conversation")
    .Build();

// Create an edited message
var editedMsg = MessageFactory.Create()
    .WithContent("Edited content")
    .AsEdited()
    .Build();

// Create an invalid message for validation tests
var invalidMessage = MessageFactory.BuildInvalid();
```

**Available methods:**
- `WithId(string)` - Set message ID
- `WithConversationId(string)` - Set conversation ID
- `WithSenderId(string)` - Set sender ID
- `WithType(string)` - Set message type
- `AsText()` - Set as text message
- `AsImage()` - Set as image message
- `AsFile()` - Set as file message
- `AsSystem()` - Set as system message
- `WithContent(string)` - Set message content
- `WithEncryptedContent(string)` - Set encrypted content
- `WithBothContents(string, string)` - Set both contents
- `AsReplyTo(string)` - Set as reply to another message
- `WithCreatedAt(DateTime)` - Set creation timestamp
- `WithReadBy(string, DateTime?)` - Add read status for user
- `WithReadByList(List<MessageReadStatus>)` - Set read by list
- `AsEdited(DateTime?)` - Mark as edited
- `AsDeleted()` - Mark as deleted
- `WithAttachment(string)` - Add attachment
- `WithAttachments(List<string>)` - Set attachments list
- `Build()` - Build and return the entity
- `BuildValid()` (static) - Quick valid message
- `BuildTextMessage(string, string, string)` (static) - Text message
- `BuildEncryptedMessage(string, string, string)` (static) - Encrypted message
- `BuildInvalid()` (static) - Quick invalid message

## Design Patterns

### Builder Pattern

All factories implement the builder pattern for flexible object construction:

1. **Fluent Interface**: Methods return `this` to enable method chaining
2. **Sensible Defaults**: Each factory provides reasonable default values
3. **Immutable Build**: The `Build()` method creates a new instance
4. **Type Safety**: Strong typing ensures compile-time validation

### Factory Method Pattern

Static convenience methods provide quick access to common configurations:

- `BuildValid()` - Creates a valid entity with defaults
- `BuildInvalid()` - Creates an invalid entity for validation testing
- Type-specific methods (e.g., `BuildDirectConversation`, `BuildTextMessage`)

## Best Practices

### 1. Use Static Methods for Simple Cases

```csharp
// Good - for simple, common cases
var user = UserFactory.BuildValid();
var message = MessageFactory.BuildTextMessage(convId, senderId, "Hello");
```

### 2. Use Builder Pattern for Custom Requirements

```csharp
// Good - for complex, specific cases
var user = UserFactory.Create()
    .WithEmail("specific@example.com")
    .AsAdmin()
    .WithDevice("device-1", "iPhone")
    .Build();
```

### 3. Keep Test Data Meaningful

```csharp
// Good - meaningful test data
var user = UserFactory.Create()
    .WithEmail("john.doe@company.com")
    .WithDisplayName("John Doe")
    .Build();

// Avoid - meaningless data
var user = UserFactory.Create()
    .WithEmail("a@b.c")
    .WithDisplayName("x")
    .Build();
```

### 4. Use Factories in Arrange Phase

```csharp
[Fact]
public async Task AddAsync_ShouldAddUser()
{
    // Arrange - Use factory
    await _fixture.CleanupAsync();
    var user = UserFactory.Create()
        .WithEmail("test@example.com")
        .Build();

    // Act
    var result = await _repository.AddAsync(user);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("test@example.com", result.Email);
}
```

### 5. Combine Factories for Related Entities

```csharp
// Create a complete test scenario
var user1 = UserFactory.BuildValid();
var user2 = UserFactory.BuildValid();
var conversation = ConversationFactory.BuildDirectConversation(user1.Id, user2.Id);
var message = MessageFactory.BuildTextMessage(conversation.Id, user1.Id, "Hello!");
```

## Benefits

1. **Consistency**: All tests use the same default values
2. **Readability**: Clear, expressive test setup
3. **Maintainability**: Changes to defaults in one place
4. **Flexibility**: Easy customization when needed
5. **Reduced Boilerplate**: Less repetitive code in tests
6. **Type Safety**: Compile-time checking

## Migration Guide

### Before (Manual Creation)

```csharp
var user = new User
{
    Email = "test@example.com",
    DisplayName = "Test User",
    PasswordHash = "hashed_password_123",
    PublicKey = "public_key_123",
    IsActive = true,
    Role = "user"
};
```

### After (Using Factory)

```csharp
var user = UserFactory.Create()
    .WithEmail("test@example.com")
    .Build();
```

Or even simpler for basic cases:

```csharp
var user = UserFactory.BuildValid();
```

## Testing the Factories

The factories themselves are tested to ensure they:
- Generate valid entities by default
- Support all customization options
- Handle edge cases correctly
- Follow naming conventions

See the test files in the parent directory for examples of factory usage.
