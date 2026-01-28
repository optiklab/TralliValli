using MongoDB.Driver;
using TraliVali.Domain.Entities;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for MongoDB indexes
/// </summary>
public class MongoDbIndexTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;

    public MongoDbIndexTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateIndexesAsync_ShouldCreateUserEmailIndex()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var indexes = await _fixture.Context.Users.Indexes.List().ToListAsync();

        // Assert
        Assert.NotNull(indexes);
        // Should have at least the _id index and email index
        Assert.True(indexes.Count >= 2);
        
        // Check for email index
        var emailIndex = indexes.FirstOrDefault(i => 
            i.Contains("name") && i["name"].AsString.Contains("email"));
        Assert.NotNull(emailIndex);
    }

    [Fact]
    public async Task CreateIndexesAsync_ShouldCreateMessageConversationIdAndCreatedAtIndex()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var indexes = await _fixture.Context.Messages.Indexes.List().ToListAsync();

        // Assert
        Assert.NotNull(indexes);
        // Should have at least the _id index and conversationId+createdAt compound index
        Assert.True(indexes.Count >= 2);

        // Check for conversationId index
        var conversationIndex = indexes.FirstOrDefault(i => 
            i.Contains("name") && i["name"].AsString.Contains("conversationId"));
        Assert.NotNull(conversationIndex);
    }

    [Fact]
    public async Task CreateIndexesAsync_ShouldCreateConversationParticipantIndex()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var indexes = await _fixture.Context.Conversations.Indexes.List().ToListAsync();

        // Assert
        Assert.NotNull(indexes);
        // Should have at least the _id index and participants index
        Assert.True(indexes.Count >= 2);

        // Check for participants.userId index
        var participantIndex = indexes.FirstOrDefault(i => 
            i.Contains("name") && i["name"].AsString.Contains("participants"));
        Assert.NotNull(participantIndex);
    }

    [Fact]
    public async Task CreateIndexesAsync_ShouldCreateInviteTokenIndex()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var indexes = await _fixture.Context.Invites.Indexes.List().ToListAsync();

        // Assert
        Assert.NotNull(indexes);
        // Should have at least _id index, token unique index, and TTL index
        Assert.True(indexes.Count >= 3);

        // Check for token index
        var tokenIndex = indexes.FirstOrDefault(i => 
            i.Contains("name") && i["name"].AsString.Contains("token"));
        Assert.NotNull(tokenIndex);
    }

    [Fact]
    public async Task CreateIndexesAsync_ShouldCreateInviteTtlIndex()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var indexes = await _fixture.Context.Invites.Indexes.List().ToListAsync();

        // Assert
        Assert.NotNull(indexes);
        
        // Check for TTL index on expiresAt
        var ttlIndex = indexes.FirstOrDefault(i => 
            i.Contains("name") && i["name"].AsString.Contains("expiresAt"));
        Assert.NotNull(ttlIndex);
        
        // Verify it's a TTL index (has expireAfterSeconds)
        if (ttlIndex != null && ttlIndex.Contains("expireAfterSeconds"))
        {
            Assert.True(ttlIndex["expireAfterSeconds"].IsInt32 || ttlIndex["expireAfterSeconds"].IsInt64);
        }
    }

    [Fact]
    public async Task UserEmailIndex_ShouldBeUnique()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user1 = new User
        {
            Email = "duplicate@example.com",
            DisplayName = "User 1"
        };
        var user2 = new User
        {
            Email = "duplicate@example.com",
            DisplayName = "User 2"
        };

        // Act & Assert
        await _fixture.Context.Users.InsertOneAsync(user1);
        
        // Should throw exception for duplicate email
        await Assert.ThrowsAsync<MongoWriteException>(async () =>
        {
            await _fixture.Context.Users.InsertOneAsync(user2);
        });
    }

    [Fact]
    public async Task InviteTokenIndex_ShouldBeUnique()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var token = Guid.NewGuid().ToString();
        var invite1 = new Invite
        {
            Token = token,
            Email = "user1@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        var invite2 = new Invite
        {
            Token = token,
            Email = "user2@example.com",
            InviterId = "507f1f77bcf86cd799439012",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act & Assert
        await _fixture.Context.Invites.InsertOneAsync(invite1);
        
        // Should throw exception for duplicate token
        await Assert.ThrowsAsync<MongoWriteException>(async () =>
        {
            await _fixture.Context.Invites.InsertOneAsync(invite2);
        });
    }
}
