using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for Message entity validation following Given-When-Then pattern
/// </summary>
public class MessageValidationTests
{
    [Fact]
    public void GivenValidMessage_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var message = new Message
        {
            ConversationId = "507f1f77bcf86cd799439011",
            SenderId = "507f1f77bcf86cd799439012",
            Type = "text",
            Content = "Hello, world!"
        };

        // Act
        var errors = message.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEncryptedContentProvided_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var message = new Message
        {
            ConversationId = "507f1f77bcf86cd799439011",
            SenderId = "507f1f77bcf86cd799439012",
            Type = "text",
            EncryptedContent = "encrypted_message_123"
        };

        // Act
        var errors = message.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEmptyConversationId_WhenValidating_ThenReturnsConversationIdRequiredError()
    {
        // Arrange
        var message = new Message
        {
            ConversationId = "",
            SenderId = "507f1f77bcf86cd799439012",
            Type = "text",
            Content = "Hello"
        };

        // Act
        var errors = message.Validate();

        // Assert
        Assert.Contains("ConversationId is required", errors);
    }

    [Fact]
    public void GivenEmptySenderId_WhenValidating_ThenReturnsSenderIdRequiredError()
    {
        // Arrange
        var message = new Message
        {
            ConversationId = "507f1f77bcf86cd799439011",
            SenderId = "",
            Type = "text",
            Content = "Hello"
        };

        // Act
        var errors = message.Validate();

        // Assert
        Assert.Contains("SenderId is required", errors);
    }

    [Fact]
    public void GivenEmptyType_WhenValidating_ThenReturnsTypeRequiredError()
    {
        // Arrange
        var message = new Message
        {
            ConversationId = "507f1f77bcf86cd799439011",
            SenderId = "507f1f77bcf86cd799439012",
            Type = "",
            Content = "Hello"
        };

        // Act
        var errors = message.Validate();

        // Assert
        Assert.Contains("Type is required", errors);
    }

    [Fact]
    public void GivenBothContentAndEncryptedContentEmpty_WhenValidating_ThenReturnsContentRequiredError()
    {
        // Arrange
        var message = new Message
        {
            ConversationId = "507f1f77bcf86cd799439011",
            SenderId = "507f1f77bcf86cd799439012",
            Type = "text",
            Content = "",
            EncryptedContent = ""
        };

        // Act
        var errors = message.Validate();

        // Assert
        Assert.Contains("Either Content or EncryptedContent is required", errors);
    }
}
