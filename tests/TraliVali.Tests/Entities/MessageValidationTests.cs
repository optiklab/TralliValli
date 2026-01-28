using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for Message entity validation
/// </summary>
public class MessageValidationTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenMessageIsValid()
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
    public void Validate_ShouldReturnNoErrors_WhenEncryptedContentIsProvided()
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
    public void Validate_ShouldReturnError_WhenConversationIdIsEmpty()
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
    public void Validate_ShouldReturnError_WhenSenderIdIsEmpty()
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
    public void Validate_ShouldReturnError_WhenTypeIsEmpty()
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
    public void Validate_ShouldReturnError_WhenBothContentAndEncryptedContentAreEmpty()
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
