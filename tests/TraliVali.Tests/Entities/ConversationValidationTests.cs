using MongoDB.Bson;
using TraliVali.Domain.Entities;
using TraliVali.Tests.Data.Factories;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for Conversation entity validation following Given-When-Then pattern
/// </summary>
public class ConversationValidationTests
{
    [Fact]
    public void GivenValidConversation_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var conversation = ConversationFactory.BuildValid();

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEmptyType_WhenValidating_ThenReturnsTypeRequiredError()
    {
        // Arrange
        var conversation = ConversationFactory.Create()
            .WithType("")
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .Build();

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Contains("Type is required", errors);
    }

    [Fact]
    public void GivenEmptyParticipants_WhenValidating_ThenReturnsParticipantsRequiredError()
    {
        // Arrange
        var conversation = ConversationFactory.BuildInvalid();

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Contains("At least one participant is required", errors);
    }

    [Fact]
    public void GivenRecentMessagesExceeds50_WhenValidating_ThenReturnsRecentMessagesTooManyError()
    {
        // Arrange
        var recentMessages = Enumerable.Range(1, 51).Select(i => $"msg{i}").ToList();
        var conversation = ConversationFactory.Create()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .WithRecentMessages(recentMessages)
            .Build();

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Contains("RecentMessages cannot exceed 50 items", errors);
    }

    [Fact]
    public void GivenRecentMessagesExactly50_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var recentMessages = Enumerable.Range(1, 50).Select(i => $"msg{i}").ToList();
        var conversation = ConversationFactory.Create()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .WithRecentMessages(recentMessages)
            .Build();

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenGroupConversation_WhenIsGroupIsTrue_ThenIsGroupPropertyIsCorrect()
    {
        // Arrange & Act
        var userId1 = ObjectId.GenerateNewId().ToString();
        var userId2 = ObjectId.GenerateNewId().ToString();
        var conversation = ConversationFactory.Create()
            .AsGroup()
            .WithName("My Group")
            .WithParticipant(userId1)
            .WithParticipant(userId2)
            .Build();

        // Assert
        Assert.True(conversation.IsGroup);
        Assert.Equal("My Group", conversation.Name);
    }

    [Fact]
    public void GivenDirectConversation_WhenIsGroupIsFalse_ThenIsGroupPropertyIsCorrect()
    {
        // Arrange & Act
        var conversation = ConversationFactory.Create()
            .AsDirect()
            .WithParticipant(ObjectId.GenerateNewId().ToString())
            .Build();

        // Assert
        Assert.False(conversation.IsGroup);
    }
}
