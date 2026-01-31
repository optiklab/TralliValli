using MongoDB.Bson;
using TraliVali.Domain.Entities;
using TraliVali.Tests.Data.Factories;

namespace TraliVali.Tests.Data;

/// <summary>
/// Tests for ConversationFactory to ensure it generates valid test data
/// </summary>
public class ConversationFactoryTests
{
    [Fact]
    public void BuildValid_ShouldCreateValidConversation()
    {
        // Act
        var conversation = ConversationFactory.BuildValid();

        // Assert
        Assert.NotNull(conversation);
        Assert.NotEmpty(conversation.Type);
        Assert.NotEmpty(conversation.Participants);
        
        var errors = conversation.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void BuildInvalid_ShouldCreateInvalidConversation()
    {
        // Act
        var conversation = ConversationFactory.BuildInvalid();

        // Assert
        var errors = conversation.Validate();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AsDirect_ShouldSetTypeAndIsGroup()
    {
        // Act
        var conversation = ConversationFactory.Create()
            .AsDirect()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .Build();

        // Assert
        Assert.Equal("direct", conversation.Type);
        Assert.False(conversation.IsGroup);
    }

    [Fact]
    public void AsGroup_ShouldSetTypeAndIsGroup()
    {
        // Act
        var conversation = ConversationFactory.Create()
            .AsGroup()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .Build();

        // Assert
        Assert.Equal("group", conversation.Type);
        Assert.True(conversation.IsGroup);
    }

    [Fact]
    public void AsChannel_ShouldSetTypeAndIsGroup()
    {
        // Act
        var conversation = ConversationFactory.Create()
            .AsChannel()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .Build();

        // Assert
        Assert.Equal("channel", conversation.Type);
        Assert.True(conversation.IsGroup);
    }

    [Fact]
    public void WithName_ShouldSetName()
    {
        // Arrange
        var expectedName = "Test Group";

        // Act
        var conversation = ConversationFactory.Create()
            .WithName(expectedName)
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .Build();

        // Assert
        Assert.Equal(expectedName, conversation.Name);
    }

    [Fact]
    public void WithParticipant_ShouldAddParticipant()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId().ToString();

        // Act
        var conversation = ConversationFactory.Create()
            .WithParticipant(userId)
            .Build();

        // Assert
        Assert.Single(conversation.Participants);
        Assert.Equal(userId, conversation.Participants[0].UserId);
        Assert.Equal("member", conversation.Participants[0].Role);
    }

    [Fact]
    public void WithAdminParticipant_ShouldAddParticipantAsAdmin()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId().ToString();

        // Act
        var conversation = ConversationFactory.Create()
            .WithAdminParticipant(userId)
            .Build();

        // Assert
        Assert.Single(conversation.Participants);
        Assert.Equal(userId, conversation.Participants[0].UserId);
        Assert.Equal("admin", conversation.Participants[0].Role);
    }

    [Fact]
    public void WithRecentMessage_ShouldAddMessageId()
    {
        // Arrange
        var messageId = ObjectId.GenerateNewId().ToString();

        // Act
        var conversation = ConversationFactory.Create()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .WithRecentMessage(messageId)
            .Build();

        // Assert
        Assert.Single(conversation.RecentMessages);
        Assert.Equal(messageId, conversation.RecentMessages[0]);
    }

    [Fact]
    public void WithMetadata_ShouldAddMetadataEntry()
    {
        // Act
        var conversation = ConversationFactory.Create()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", "value2")
            .Build();

        // Assert
        Assert.Equal(2, conversation.Metadata.Count);
        Assert.Equal("value1", conversation.Metadata["key1"]);
        Assert.Equal("value2", conversation.Metadata["key2"]);
    }

    [Fact]
    public void WithLastMessageAt_ShouldSetLastMessageAt()
    {
        // Arrange
        var lastMessage = DateTime.UtcNow.AddHours(-1);

        // Act
        var conversation = ConversationFactory.Create()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .WithLastMessageAt(lastMessage)
            .Build();

        // Assert
        Assert.Equal(lastMessage, conversation.LastMessageAt);
    }

    [Fact]
    public void BuildDirectConversation_ShouldCreateDirectConversationWithTwoParticipants()
    {
        // Arrange
        var userId1 = ObjectId.GenerateNewId().ToString();
        var userId2 = ObjectId.GenerateNewId().ToString();

        // Act
        var conversation = ConversationFactory.BuildDirectConversation(userId1, userId2);

        // Assert
        Assert.Equal("direct", conversation.Type);
        Assert.False(conversation.IsGroup);
        Assert.Equal(2, conversation.Participants.Count);
        Assert.Contains(conversation.Participants, p => p.UserId == userId1);
        Assert.Contains(conversation.Participants, p => p.UserId == userId2);
    }

    [Fact]
    public void BuildGroupConversation_ShouldCreateGroupConversationWithMultipleParticipants()
    {
        // Arrange
        var userId1 = ObjectId.GenerateNewId().ToString();
        var userId2 = ObjectId.GenerateNewId().ToString();
        var userId3 = ObjectId.GenerateNewId().ToString();

        // Act
        var conversation = ConversationFactory.BuildGroupConversation(
            "Test Group",
            userId1, userId2, userId3
        );

        // Assert
        Assert.Equal("group", conversation.Type);
        Assert.True(conversation.IsGroup);
        Assert.Equal("Test Group", conversation.Name);
        Assert.Equal(3, conversation.Participants.Count);
    }

    [Fact]
    public void BuilderPattern_ShouldAllowChaining()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId().ToString();

        // Act
        var conversation = ConversationFactory.Create()
            .AsGroup()
            .WithName("Chained Group")
            .WithParticipant(userId, "admin")
            .WithMetadata("purpose", "testing")
            .WithLastMessageAt(DateTime.UtcNow)
            .Build();

        // Assert
        Assert.Equal("group", conversation.Type);
        Assert.True(conversation.IsGroup);
        Assert.Equal("Chained Group", conversation.Name);
        Assert.Single(conversation.Participants);
        Assert.Equal("admin", conversation.Participants[0].Role);
        Assert.Single(conversation.Metadata);
        Assert.NotNull(conversation.LastMessageAt);
    }
}
