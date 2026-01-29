using MongoDB.Driver;
using Testcontainers.MongoDb;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Tests.Auth;

/// <summary>
/// Tests for ArchiveService
/// </summary>
public class ArchiveServiceTests : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;
    private MongoDbContext? _mongoContext;
    private ArchiveService? _archiveService;

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder().Build();
        await _mongoContainer.StartAsync();

        var connectionString = _mongoContainer.GetConnectionString();
        _mongoContext = new MongoDbContext(connectionString, "test_tralivali");
        await _mongoContext.CreateIndexesAsync();

        _archiveService = new ArchiveService(
            _mongoContext.Conversations,
            _mongoContext.Messages,
            _mongoContext.Users);
    }

    public async Task DisposeAsync()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.StopAsync();
            await _mongoContainer.DisposeAsync();
        }
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConversationsIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ArchiveService(null!, _mongoContext!.Messages, _mongoContext.Users));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMessagesIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ArchiveService(_mongoContext!.Conversations, null!, _mongoContext.Users));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenUsersIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ArchiveService(_mongoContext!.Conversations, _mongoContext.Messages, null!));
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldThrowArgumentException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _archiveService!.ExportConversationMessagesAsync("", startDate, endDate));
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldThrowArgumentException_WhenStartDateIsAfterEndDate()
    {
        // Arrange
        var conversationId = "507f1f77bcf86cd799439011";
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-7);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _archiveService!.ExportConversationMessagesAsync(conversationId, startDate, endDate));
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldThrowInvalidOperationException_WhenConversationNotFound()
    {
        // Arrange
        var conversationId = "507f1f77bcf86cd799439011";
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _archiveService!.ExportConversationMessagesAsync(conversationId, startDate, endDate));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldReturnEmptyMessagesWhenNoMessagesInRange()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "User One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User Two");
        var conversation = await CreateTestConversationAsync("Test Conversation", new[] { user1.Id, user2.Id });

        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(-5);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(conversation.Id, result.ConversationId);
        Assert.Equal(conversation.Name, result.ConversationName);
        Assert.Equal(0, result.MessagesCount);
        Assert.Empty(result.Messages);
        Assert.Equal(2, result.Participants.Count);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldReturnMessagesWithinDateRange()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "User One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User Two");
        var conversation = await CreateTestConversationAsync("Test Conversation", new[] { user1.Id, user2.Id });

        var baseTime = DateTime.UtcNow.AddDays(-10);
        
        // Create messages at different times
        var message1 = await CreateTestMessageAsync(conversation.Id, user1.Id, "Message 1", baseTime.AddDays(1));
        var message2 = await CreateTestMessageAsync(conversation.Id, user2.Id, "Message 2", baseTime.AddDays(3));
        var message3 = await CreateTestMessageAsync(conversation.Id, user1.Id, "Message 3", baseTime.AddDays(5));
        var message4 = await CreateTestMessageAsync(conversation.Id, user2.Id, "Message 4", baseTime.AddDays(7));

        // Set date range to include only messages 2 and 3
        var startDate = baseTime.AddDays(2);
        var endDate = baseTime.AddDays(6);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(conversation.Id, result.ConversationId);
        Assert.Equal(2, result.MessagesCount);
        Assert.Equal(2, result.Messages.Count);

        // Verify messages are sorted by creation date
        Assert.Equal(message2.Id, result.Messages[0].MessageId);
        Assert.Equal(message3.Id, result.Messages[1].MessageId);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldIncludeSenderNames()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "Alice Smith");
        var user2 = await CreateTestUserAsync("user2@test.com", "Bob Jones");
        var conversation = await CreateTestConversationAsync("Test Conversation", new[] { user1.Id, user2.Id });

        var baseTime = DateTime.UtcNow.AddDays(-5);
        await CreateTestMessageAsync(conversation.Id, user1.Id, "Hello from Alice", baseTime);
        await CreateTestMessageAsync(conversation.Id, user2.Id, "Hello from Bob", baseTime.AddHours(1));

        var startDate = baseTime.AddDays(-1);
        var endDate = baseTime.AddDays(1);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.MessagesCount);
        Assert.Equal("Alice Smith", result.Messages[0].SenderName);
        Assert.Equal("Bob Jones", result.Messages[1].SenderName);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldIncludeFileReferences()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "User One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User Two");
        var conversation = await CreateTestConversationAsync("Test Conversation", new[] { user1.Id, user2.Id });

        var baseTime = DateTime.UtcNow.AddDays(-5);
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = user1.Id,
            Type = "file",
            Content = "Document shared",
            CreatedAt = baseTime,
            Attachments = new List<string> { "file1.pdf", "file2.docx", "image1.jpg" }
        };
        await _mongoContext!.Messages.InsertOneAsync(message);

        var startDate = baseTime.AddDays(-1);
        var endDate = baseTime.AddDays(1);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal(3, result.Messages[0].Attachments.Count);
        Assert.Contains("file1.pdf", result.Messages[0].Attachments);
        Assert.Contains("file2.docx", result.Messages[0].Attachments);
        Assert.Contains("image1.jpg", result.Messages[0].Attachments);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldIncludeParticipantInformation()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("alice@test.com", "Alice Smith");
        var user2 = await CreateTestUserAsync("bob@test.com", "Bob Jones");
        var user3 = await CreateTestUserAsync("charlie@test.com", "Charlie Brown");
        
        var conversation = new Conversation
        {
            Name = "Team Chat",
            Type = "group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = user1.Id, Role = "admin" },
                new Participant { UserId = user2.Id, Role = "member" },
                new Participant { UserId = user3.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        };
        await _mongoContext!.Conversations.InsertOneAsync(conversation);

        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Participants.Count);
        
        var alice = result.Participants.First(p => p.UserId == user1.Id);
        Assert.Equal("Alice Smith", alice.DisplayName);
        Assert.Equal("alice@test.com", alice.Email);
        Assert.Equal("admin", alice.Role);

        var bob = result.Participants.First(p => p.UserId == user2.Id);
        Assert.Equal("Bob Jones", bob.DisplayName);
        Assert.Equal("bob@test.com", bob.Email);
        Assert.Equal("member", bob.Role);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldExcludeDeletedMessages()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "User One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User Two");
        var conversation = await CreateTestConversationAsync("Test Conversation", new[] { user1.Id, user2.Id });

        var baseTime = DateTime.UtcNow.AddDays(-5);
        
        await CreateTestMessageAsync(conversation.Id, user1.Id, "Active Message", baseTime);
        
        var deletedMessage = new Message
        {
            ConversationId = conversation.Id,
            SenderId = user2.Id,
            Type = "text",
            Content = "Deleted Message",
            CreatedAt = baseTime.AddHours(1),
            IsDeleted = true
        };
        await _mongoContext!.Messages.InsertOneAsync(deletedMessage);

        var startDate = baseTime.AddDays(-1);
        var endDate = baseTime.AddDays(1);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal("Active Message", result.Messages[0].Content);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldHandleEncryptedContent()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "User One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User Two");
        var conversation = await CreateTestConversationAsync("Test Conversation", new[] { user1.Id, user2.Id });

        var baseTime = DateTime.UtcNow.AddDays(-5);
        
        // Message with encrypted content (in future, this would be decrypted)
        var encryptedMessage = new Message
        {
            ConversationId = conversation.Id,
            SenderId = user1.Id,
            Type = "text",
            Content = string.Empty,
            EncryptedContent = "encrypted_data_here",
            CreatedAt = baseTime
        };
        await _mongoContext!.Messages.InsertOneAsync(encryptedMessage);

        var startDate = baseTime.AddDays(-1);
        var endDate = baseTime.AddDays(1);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        // Currently returns encrypted content as-is (will be decrypted in Phase 5)
        Assert.Equal("encrypted_data_here", result.Messages[0].Content);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldIncludeExportMetadata()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "User One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User Two");
        var conversation = await CreateTestConversationAsync("Important Conversation", new[] { user1.Id, user2.Id });

        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        var beforeExport = DateTime.UtcNow;

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        var afterExport = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ExportedAt >= beforeExport && result.ExportedAt <= afterExport);
        Assert.Equal("Important Conversation", result.ConversationName);
        Assert.Equal(conversation.Id, result.ConversationId);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldHandleUnknownUsers()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "User One");
        var conversation = await CreateTestConversationAsync("Test Conversation", new[] { user1.Id });

        var baseTime = DateTime.UtcNow.AddDays(-5);
        
        // Create message from unknown user (simulating deleted user or data inconsistency)
        var unknownUserId = "507f1f77bcf86cd799439099";
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = unknownUserId,
            Type = "text",
            Content = "Message from unknown user",
            CreatedAt = baseTime
        };
        await _mongoContext!.Messages.InsertOneAsync(message);

        var startDate = baseTime.AddDays(-1);
        var endDate = baseTime.AddDays(1);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal("Unknown User", result.Messages[0].SenderName);
    }

    [Fact]
    public async Task ExportConversationMessagesAsync_ShouldProduceValidJsonStructure()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("alice@test.com", "Alice Smith");
        var user2 = await CreateTestUserAsync("bob@test.com", "Bob Jones");
        var conversation = await CreateTestConversationAsync("Project Discussion", new[] { user1.Id, user2.Id });

        var baseTime = DateTime.UtcNow.AddDays(-5);
        
        var message1 = new Message
        {
            ConversationId = conversation.Id,
            SenderId = user1.Id,
            Type = "text",
            Content = "Hello, how are you?",
            CreatedAt = baseTime,
            Attachments = new List<string> { "doc1.pdf" }
        };
        await _mongoContext!.Messages.InsertOneAsync(message1);

        var message2 = new Message
        {
            ConversationId = conversation.Id,
            SenderId = user2.Id,
            Type = "text",
            Content = "I am doing well, thanks!",
            CreatedAt = baseTime.AddMinutes(5),
            EditedAt = baseTime.AddMinutes(10)
        };
        await _mongoContext.Messages.InsertOneAsync(message2);

        var startDate = baseTime.AddDays(-1);
        var endDate = baseTime.AddDays(1);

        // Act
        var result = await _archiveService!.ExportConversationMessagesAsync(
            conversation.Id,
            startDate,
            endDate);

        // Serialize to JSON to verify structure
        var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Assert - Verify JSON contains all required fields
        Assert.Contains("\"exportedAt\":", json);
        Assert.Contains("\"conversationId\":", json);
        Assert.Contains("\"conversationName\": \"Project Discussion\"", json);
        Assert.Contains("\"participants\":", json);
        Assert.Contains("\"messagesCount\": 2", json);
        Assert.Contains("\"messages\":", json);
        
        // Verify participant structure
        Assert.Contains("\"userId\":", json);
        Assert.Contains("\"displayName\": \"Alice Smith\"", json);
        Assert.Contains("\"email\": \"alice@test.com\"", json);
        Assert.Contains("\"role\": \"member\"", json);
        
        // Verify message structure
        Assert.Contains("\"messageId\":", json);
        Assert.Contains("\"senderId\":", json);
        Assert.Contains("\"senderName\": \"Alice Smith\"", json);
        Assert.Contains("\"senderName\": \"Bob Jones\"", json);
        Assert.Contains("\"type\": \"text\"", json);
        Assert.Contains("\"content\": \"Hello, how are you?\"", json);
        Assert.Contains("\"content\": \"I am doing well, thanks!\"", json);
        Assert.Contains("\"createdAt\":", json);
        Assert.Contains("\"attachments\": [", json);
        Assert.Contains("\"doc1.pdf\"", json);
        
        // Verify editedAt is included for message2
        Assert.Contains("\"editedAt\":", json);
    }

    // Helper methods

    private async Task<User> CreateTestUserAsync(string email, string displayName)
    {
        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = "hash",
            PublicKey = "key",
            CreatedAt = DateTime.UtcNow
        };
        await _mongoContext!.Users.InsertOneAsync(user);
        return user;
    }

    private async Task<Conversation> CreateTestConversationAsync(string name, string[] participantIds)
    {
        var conversation = new Conversation
        {
            Name = name,
            Type = "group",
            IsGroup = true,
            Participants = participantIds.Select(id => new Participant
            {
                UserId = id,
                Role = "member",
                JoinedAt = DateTime.UtcNow
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
        await _mongoContext!.Conversations.InsertOneAsync(conversation);
        return conversation;
    }

    private async Task<Message> CreateTestMessageAsync(string conversationId, string senderId, string content, DateTime createdAt)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Type = "text",
            Content = content,
            CreatedAt = createdAt,
            IsDeleted = false
        };
        await _mongoContext!.Messages.InsertOneAsync(message);
        return message;
    }
}
