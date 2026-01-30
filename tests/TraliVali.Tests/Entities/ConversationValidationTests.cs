using TraliVali.Domain.Entities;

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
        var conversation = new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = "507f1f77bcf86cd799439011" }
            },
            RecentMessages = new List<string> { "msg1", "msg2" }
        };

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEmptyType_WhenValidating_ThenReturnsTypeRequiredError()
    {
        // Arrange
        var conversation = new Conversation
        {
            Type = "",
            Participants = new List<Participant>
            {
                new Participant { UserId = "507f1f77bcf86cd799439011" }
            }
        };

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Contains("Type is required", errors);
    }

    [Fact]
    public void GivenEmptyParticipants_WhenValidating_ThenReturnsParticipantsRequiredError()
    {
        // Arrange
        var conversation = new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>()
        };

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Contains("At least one participant is required", errors);
    }

    [Fact]
    public void GivenRecentMessagesExceeds50_WhenValidating_ThenReturnsRecentMessagesTooManyError()
    {
        // Arrange
        var conversation = new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = "507f1f77bcf86cd799439011" }
            },
            RecentMessages = Enumerable.Range(1, 51).Select(i => $"msg{i}").ToList()
        };

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Contains("RecentMessages cannot exceed 50 items", errors);
    }

    [Fact]
    public void GivenRecentMessagesExactly50_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var conversation = new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = "507f1f77bcf86cd799439011" }
            },
            RecentMessages = Enumerable.Range(1, 50).Select(i => $"msg{i}").ToList()
        };

        // Act
        var errors = conversation.Validate();

        // Assert
        Assert.Empty(errors);
    }
}
