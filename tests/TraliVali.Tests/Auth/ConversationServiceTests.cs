using MongoDB.Driver;
using Testcontainers.MongoDb;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Tests.Auth;

/// <summary>
/// Tests for ConversationService
/// </summary>
public class ConversationServiceTests : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;
    private MongoDbContext? _mongoContext;
    private ConversationService? _conversationService;

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder().Build();
        await _mongoContainer.StartAsync();

        var connectionString = _mongoContainer.GetConnectionString();
        _mongoContext = new MongoDbContext(connectionString, "test_tralivali");
        await _mongoContext.CreateIndexesAsync();

        _conversationService = new ConversationService(_mongoContext.Conversations);
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
    public async Task CreateDirectConversationAsync_ShouldCreateNewConversation()
    {
        // Arrange
        var userId1 = "507f1f77bcf86cd799439011";
        var userId2 = "507f1f77bcf86cd799439012";

        // Act
        var result = await _conversationService!.CreateDirectConversationAsync(userId1, userId2);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("direct", result.Type);
        Assert.False(result.IsGroup);
        Assert.Equal(2, result.Participants.Count);
        Assert.Contains(result.Participants, p => p.UserId == userId1);
        Assert.Contains(result.Participants, p => p.UserId == userId2);
        Assert.All(result.Participants, p => Assert.Equal("member", p.Role));
    }

    [Fact]
    public async Task CreateDirectConversationAsync_ShouldReturnExistingConversation_WhenDuplicateIsAttempted()
    {
        // Arrange
        var userId1 = "507f1f77bcf86cd799439011";
        var userId2 = "507f1f77bcf86cd799439012";

        // Act - Create first conversation
        var firstConversation = await _conversationService!.CreateDirectConversationAsync(userId1, userId2);

        // Act - Attempt to create duplicate
        var secondConversation = await _conversationService.CreateDirectConversationAsync(userId1, userId2);

        // Assert
        Assert.NotNull(firstConversation);
        Assert.NotNull(secondConversation);
        Assert.Equal(firstConversation.Id, secondConversation.Id);

        // Verify only one conversation exists in database
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.IsGroup, false),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId1),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId2)
        );
        var conversations = await _mongoContext!.Conversations.Find(filter).ToListAsync();
        Assert.Single(conversations);
    }

    [Fact]
    public async Task CreateDirectConversationAsync_ShouldReturnExistingConversation_WhenUsersAreReversed()
    {
        // Arrange
        var userId1 = "507f1f77bcf86cd799439011";
        var userId2 = "507f1f77bcf86cd799439012";

        // Act - Create conversation with userId1, userId2
        var firstConversation = await _conversationService!.CreateDirectConversationAsync(userId1, userId2);

        // Act - Attempt to create with reversed users userId2, userId1
        var secondConversation = await _conversationService.CreateDirectConversationAsync(userId2, userId1);

        // Assert
        Assert.NotNull(firstConversation);
        Assert.NotNull(secondConversation);
        Assert.Equal(firstConversation.Id, secondConversation.Id);
    }

    [Fact]
    public async Task CreateDirectConversationAsync_ShouldThrowException_WhenUserId1IsEmpty()
    {
        // Arrange
        var userId2 = "507f1f77bcf86cd799439012";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.CreateDirectConversationAsync("", userId2));
    }

    [Fact]
    public async Task CreateDirectConversationAsync_ShouldThrowException_WhenUserId2IsEmpty()
    {
        // Arrange
        var userId1 = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.CreateDirectConversationAsync(userId1, ""));
    }

    [Fact]
    public async Task CreateDirectConversationAsync_ShouldThrowException_WhenUsersAreSame()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.CreateDirectConversationAsync(userId, userId));
    }

    [Fact]
    public async Task CreateGroupConversationAsync_ShouldCreateGroupWithMultipleMembers()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012", "507f1f77bcf86cd799439013" };

        // Act
        var result = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("group", result.Type);
        Assert.True(result.IsGroup);
        Assert.Equal(name, result.Name);
        Assert.Equal(3, result.Participants.Count); // Creator + 2 members
        
        // Check creator has admin role
        var creator = result.Participants.FirstOrDefault(p => p.UserId == creatorId);
        Assert.NotNull(creator);
        Assert.Equal("admin", creator.Role);
        
        // Check members have member role
        Assert.All(result.Participants.Where(p => p.UserId != creatorId), 
            p => Assert.Equal("member", p.Role));
    }

    [Fact]
    public async Task CreateGroupConversationAsync_ShouldNotDuplicateCreatorInMembers()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { creatorId, "507f1f77bcf86cd799439012" }; // Creator included in members

        // Act
        var result = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Participants.Count); // Should only have creator once + 1 other member
        Assert.Single(result.Participants.Where(p => p.UserId == creatorId));
    }

    [Fact]
    public async Task CreateGroupConversationAsync_ShouldThrowException_WhenNameIsEmpty()
    {
        // Arrange
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.CreateGroupConversationAsync("", creatorId, memberIds));
    }

    [Fact]
    public async Task CreateGroupConversationAsync_ShouldThrowException_WhenCreatorIdIsEmpty()
    {
        // Arrange
        var name = "Test Group";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.CreateGroupConversationAsync(name, "", memberIds));
    }

    [Fact]
    public async Task CreateGroupConversationAsync_ShouldThrowException_WhenMemberIdsIsNull()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.CreateGroupConversationAsync(name, creatorId, null!));
    }

    [Fact]
    public async Task CreateGroupConversationAsync_ShouldThrowException_WhenMemberIdsIsEmpty()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.CreateGroupConversationAsync(name, creatorId, Array.Empty<string>()));
    }

    [Fact]
    public async Task AddMemberAsync_ShouldAddMemberToConversation()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);
        var newMemberId = "507f1f77bcf86cd799439013";

        // Act
        var result = await _conversationService.AddMemberAsync(conversation.Id, newMemberId);

        // Assert
        Assert.True(result);

        // Verify member was added
        var updatedConversation = await _mongoContext!.Conversations.Find(c => c.Id == conversation.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedConversation);
        Assert.Equal(3, updatedConversation.Participants.Count);
        Assert.Contains(updatedConversation.Participants, p => p.UserId == newMemberId);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldAddMemberWithSpecifiedRole()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);
        var newMemberId = "507f1f77bcf86cd799439013";

        // Act
        var result = await _conversationService.AddMemberAsync(conversation.Id, newMemberId, "admin");

        // Assert
        Assert.True(result);

        // Verify member was added with admin role
        var updatedConversation = await _mongoContext!.Conversations.Find(c => c.Id == conversation.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedConversation);
        var addedMember = updatedConversation.Participants.FirstOrDefault(p => p.UserId == newMemberId);
        Assert.NotNull(addedMember);
        Assert.Equal("admin", addedMember.Role);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldReturnFalse_WhenMemberAlreadyExists()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);

        // Act - Try to add existing member
        var result = await _conversationService.AddMemberAsync(conversation.Id, memberIds[0]);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldReturnFalse_WhenConversationDoesNotExist()
    {
        // Arrange
        var nonExistentId = "507f1f77bcf86cd799439999";
        var userId = "507f1f77bcf86cd799439011";

        // Act
        var result = await _conversationService!.AddMemberAsync(nonExistentId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.AddMemberAsync("", userId));
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Arrange
        var conversationId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.AddMemberAsync(conversationId, ""));
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrowException_WhenRoleIsEmpty()
    {
        // Arrange
        var conversationId = "507f1f77bcf86cd799439011";
        var userId = "507f1f77bcf86cd799439012";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.AddMemberAsync(conversationId, userId, ""));
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldRemoveMemberFromConversation()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012", "507f1f77bcf86cd799439013" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);

        // Act
        var result = await _conversationService.RemoveMemberAsync(conversation.Id, memberIds[0]);

        // Assert
        Assert.True(result);

        // Verify member was removed
        var updatedConversation = await _mongoContext!.Conversations.Find(c => c.Id == conversation.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedConversation);
        Assert.Equal(2, updatedConversation.Participants.Count); // Creator + 1 remaining member
        Assert.DoesNotContain(updatedConversation.Participants, p => p.UserId == memberIds[0]);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldReturnFalse_WhenMemberDoesNotExist()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);
        var nonMemberId = "507f1f77bcf86cd799439999";

        // Act
        var result = await _conversationService.RemoveMemberAsync(conversation.Id, nonMemberId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldReturnFalse_WhenConversationDoesNotExist()
    {
        // Arrange
        var nonExistentId = "507f1f77bcf86cd799439999";
        var userId = "507f1f77bcf86cd799439011";

        // Act
        var result = await _conversationService!.RemoveMemberAsync(nonExistentId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.RemoveMemberAsync("", userId));
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Arrange
        var conversationId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.RemoveMemberAsync(conversationId, ""));
    }

    [Fact]
    public async Task UpdateGroupMetadataAsync_ShouldUpdateName()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);
        var newName = "Updated Group Name";

        // Act
        var result = await _conversationService.UpdateGroupMetadataAsync(conversation.Id, name: newName);

        // Assert
        Assert.True(result);

        // Verify name was updated
        var updatedConversation = await _mongoContext!.Conversations.Find(c => c.Id == conversation.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedConversation);
        Assert.Equal(newName, updatedConversation.Name);
    }

    [Fact]
    public async Task UpdateGroupMetadataAsync_ShouldUpdateAvatar()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);
        var avatarUrl = "https://example.com/avatar.png";

        // Act
        var result = await _conversationService.UpdateGroupMetadataAsync(conversation.Id, avatar: avatarUrl);

        // Assert
        Assert.True(result);

        // Verify avatar was updated
        var updatedConversation = await _mongoContext!.Conversations.Find(c => c.Id == conversation.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedConversation);
        Assert.True(updatedConversation.Metadata.ContainsKey("avatar"));
        Assert.Equal(avatarUrl, updatedConversation.Metadata["avatar"]);
    }

    [Fact]
    public async Task UpdateGroupMetadataAsync_ShouldUpdateBothNameAndAvatar()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);
        var newName = "Updated Group Name";
        var avatarUrl = "https://example.com/avatar.png";

        // Act
        var result = await _conversationService.UpdateGroupMetadataAsync(conversation.Id, newName, avatarUrl);

        // Assert
        Assert.True(result);

        // Verify both were updated
        var updatedConversation = await _mongoContext!.Conversations.Find(c => c.Id == conversation.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedConversation);
        Assert.Equal(newName, updatedConversation.Name);
        Assert.True(updatedConversation.Metadata.ContainsKey("avatar"));
        Assert.Equal(avatarUrl, updatedConversation.Metadata["avatar"]);
    }

    [Fact]
    public async Task UpdateGroupMetadataAsync_ShouldReturnFalse_WhenNothingToUpdate()
    {
        // Arrange
        var name = "Test Group";
        var creatorId = "507f1f77bcf86cd799439011";
        var memberIds = new[] { "507f1f77bcf86cd799439012" };
        var conversation = await _conversationService!.CreateGroupConversationAsync(name, creatorId, memberIds);

        // Act - Call with no updates
        var result = await _conversationService.UpdateGroupMetadataAsync(conversation.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateGroupMetadataAsync_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.UpdateGroupMetadataAsync("", "New Name"));
    }

    [Fact]
    public async Task UpdateGroupMetadataAsync_ShouldReturnFalse_WhenConversationDoesNotExist()
    {
        // Arrange
        var nonExistentId = "507f1f77bcf86cd799439999";
        var newName = "Updated Name";

        // Act
        var result = await _conversationService!.UpdateGroupMetadataAsync(nonExistentId, newName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserConversationsAsync_ShouldReturnAllUserConversations()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var otherUserId1 = "507f1f77bcf86cd799439012";
        var otherUserId2 = "507f1f77bcf86cd799439013";

        // Create a direct conversation
        await _conversationService!.CreateDirectConversationAsync(userId, otherUserId1);

        // Create a group conversation
        await _conversationService.CreateGroupConversationAsync("Test Group", userId, new[] { otherUserId2 });

        // Create another direct conversation
        await _conversationService.CreateDirectConversationAsync(userId, otherUserId2);

        // Act
        var result = await _conversationService.GetUserConversationsAsync(userId);

        // Assert
        Assert.NotNull(result);
        var conversations = result.ToList();
        Assert.Equal(3, conversations.Count);
        Assert.All(conversations, c => Assert.Contains(c.Participants, p => p.UserId == userId));
    }

    [Fact]
    public async Task GetUserConversationsAsync_ShouldReturnEmpty_WhenUserHasNoConversations()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";

        // Act
        var result = await _conversationService!.GetUserConversationsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserConversationsAsync_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _conversationService!.GetUserConversationsAsync(""));
    }

    [Fact]
    public async Task GetUserConversationsAsync_ShouldNotReturnOtherUsersConversations()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var otherUserId1 = "507f1f77bcf86cd799439012";
        var otherUserId2 = "507f1f77bcf86cd799439013";

        // Create conversation for the test user
        await _conversationService!.CreateDirectConversationAsync(userId, otherUserId1);

        // Create conversation between other users (not including test user)
        await _conversationService.CreateDirectConversationAsync(otherUserId1, otherUserId2);

        // Act
        var result = await _conversationService.GetUserConversationsAsync(userId);

        // Assert
        Assert.NotNull(result);
        var conversations = result.ToList();
        Assert.Single(conversations);
        Assert.Contains(conversations[0].Participants, p => p.UserId == userId);
    }
}
