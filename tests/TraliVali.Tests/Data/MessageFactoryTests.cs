using MongoDB.Bson;
using TraliVali.Domain.Entities;
using TraliVali.Tests.Data.Factories;

namespace TraliVali.Tests.Data;

/// <summary>
/// Tests for MessageFactory to ensure it generates valid test data
/// </summary>
public class MessageFactoryTests
{
    [Fact]
    public void BuildValid_ShouldCreateValidMessage()
    {
        // Act
        var message = MessageFactory.BuildValid();

        // Assert
        Assert.NotNull(message);
        Assert.NotEmpty(message.ConversationId);
        Assert.NotEmpty(message.SenderId);
        Assert.NotEmpty(message.Type);
        Assert.NotEmpty(message.Content);
        
        var errors = message.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void BuildInvalid_ShouldCreateInvalidMessage()
    {
        // Act
        var message = MessageFactory.BuildInvalid();

        // Assert
        var errors = message.Validate();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AsText_ShouldSetTypeToText()
    {
        // Act
        var message = MessageFactory.Create()
            .AsText()
            .Build();

        // Assert
        Assert.Equal("text", message.Type);
    }

    [Fact]
    public void AsImage_ShouldSetTypeToImage()
    {
        // Act
        var message = MessageFactory.Create()
            .AsImage()
            .Build();

        // Assert
        Assert.Equal("image", message.Type);
    }

    [Fact]
    public void AsFile_ShouldSetTypeToFile()
    {
        // Act
        var message = MessageFactory.Create()
            .AsFile()
            .Build();

        // Assert
        Assert.Equal("file", message.Type);
    }

    [Fact]
    public void AsSystem_ShouldSetTypeToSystem()
    {
        // Act
        var message = MessageFactory.Create()
            .AsSystem()
            .Build();

        // Assert
        Assert.Equal("system", message.Type);
    }

    [Fact]
    public void WithContent_ShouldSetContent()
    {
        // Arrange
        var expectedContent = "Custom message content";

        // Act
        var message = MessageFactory.Create()
            .WithContent(expectedContent)
            .Build();

        // Assert
        Assert.Equal(expectedContent, message.Content);
    }

    [Fact]
    public void WithEncryptedContent_ShouldSetEncryptedContent()
    {
        // Arrange
        var expectedEncrypted = "base64_encrypted_content";

        // Act
        var message = MessageFactory.Create()
            .WithEncryptedContent(expectedEncrypted)
            .Build();

        // Assert
        Assert.Equal(expectedEncrypted, message.EncryptedContent);
    }

    [Fact]
    public void AsReplyTo_ShouldSetReplyTo()
    {
        // Arrange
        var originalMessageId = ObjectId.GenerateNewId().ToString();

        // Act
        var message = MessageFactory.Create()
            .AsReplyTo(originalMessageId)
            .Build();

        // Assert
        Assert.Equal(originalMessageId, message.ReplyTo);
    }

    [Fact]
    public void WithReadBy_ShouldAddReadStatus()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId().ToString();

        // Act
        var message = MessageFactory.Create()
            .WithReadBy(userId)
            .Build();

        // Assert
        Assert.Single(message.ReadBy);
        Assert.Equal(userId, message.ReadBy[0].UserId);
    }

    [Fact]
    public void WithReadBy_MultipleCalls_ShouldAddMultipleReadStatuses()
    {
        // Arrange
        var userId1 = ObjectId.GenerateNewId().ToString();
        var userId2 = ObjectId.GenerateNewId().ToString();

        // Act
        var message = MessageFactory.Create()
            .WithReadBy(userId1)
            .WithReadBy(userId2)
            .Build();

        // Assert
        Assert.Equal(2, message.ReadBy.Count);
        Assert.Contains(message.ReadBy, r => r.UserId == userId1);
        Assert.Contains(message.ReadBy, r => r.UserId == userId2);
    }

    [Fact]
    public void AsEdited_ShouldSetEditedAt()
    {
        // Act
        var message = MessageFactory.Create()
            .AsEdited()
            .Build();

        // Assert
        Assert.NotNull(message.EditedAt);
    }

    [Fact]
    public void AsDeleted_ShouldSetIsDeletedTrue()
    {
        // Act
        var message = MessageFactory.Create()
            .AsDeleted()
            .Build();

        // Assert
        Assert.True(message.IsDeleted);
    }

    [Fact]
    public void WithAttachment_ShouldAddAttachment()
    {
        // Arrange
        var attachmentUrl = "https://example.com/file.pdf";

        // Act
        var message = MessageFactory.Create()
            .WithAttachment(attachmentUrl)
            .Build();

        // Assert
        Assert.Single(message.Attachments);
        Assert.Equal(attachmentUrl, message.Attachments[0]);
    }

    [Fact]
    public void BuildTextMessage_ShouldCreateTextMessage()
    {
        // Arrange
        var conversationId = ObjectId.GenerateNewId().ToString();
        var senderId = ObjectId.GenerateNewId().ToString();
        var content = "Hello, World!";

        // Act
        var message = MessageFactory.BuildTextMessage(conversationId, senderId, content);

        // Assert
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(senderId, message.SenderId);
        Assert.Equal(content, message.Content);
        Assert.Equal("text", message.Type);
    }

    [Fact]
    public void BuildEncryptedMessage_ShouldCreateEncryptedMessage()
    {
        // Arrange
        var conversationId = ObjectId.GenerateNewId().ToString();
        var senderId = ObjectId.GenerateNewId().ToString();
        var encryptedContent = "base64_encrypted_content";

        // Act
        var message = MessageFactory.BuildEncryptedMessage(conversationId, senderId, encryptedContent);

        // Assert
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(senderId, message.SenderId);
        Assert.Equal(encryptedContent, message.EncryptedContent);
        Assert.Equal("text", message.Type);
    }

    [Fact]
    public void WithConversationId_ShouldSetConversationId()
    {
        // Arrange
        var conversationId = ObjectId.GenerateNewId().ToString();

        // Act
        var message = MessageFactory.Create()
            .WithConversationId(conversationId)
            .Build();

        // Assert
        Assert.Equal(conversationId, message.ConversationId);
    }

    [Fact]
    public void WithSenderId_ShouldSetSenderId()
    {
        // Arrange
        var senderId = ObjectId.GenerateNewId().ToString();

        // Act
        var message = MessageFactory.Create()
            .WithSenderId(senderId)
            .Build();

        // Assert
        Assert.Equal(senderId, message.SenderId);
    }

    [Fact]
    public void BuilderPattern_ShouldAllowChaining()
    {
        // Arrange
        var conversationId = ObjectId.GenerateNewId().ToString();
        var senderId = ObjectId.GenerateNewId().ToString();
        var userId = ObjectId.GenerateNewId().ToString();

        // Act
        var message = MessageFactory.Create()
            .WithConversationId(conversationId)
            .WithSenderId(senderId)
            .WithContent("Chained message")
            .AsText()
            .WithReadBy(userId)
            .WithAttachment("https://example.com/file.pdf")
            .AsEdited()
            .Build();

        // Assert
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(senderId, message.SenderId);
        Assert.Equal("Chained message", message.Content);
        Assert.Equal("text", message.Type);
        Assert.Single(message.ReadBy);
        Assert.Single(message.Attachments);
        Assert.NotNull(message.EditedAt);
    }
}
