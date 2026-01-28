using System.Text.Json;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Workers;
using TraliVali.Workers.Models;

namespace TraliVali.Tests.Workers;

/// <summary>
/// Tests for MessageProcessorWorker
/// Note: Full integration tests for message processing require SignalR hub connectivity.
/// These tests focus on constructor validation and basic structure verification.
/// </summary>
public class MessageProcessorWorkerTests : IDisposable
{
    private readonly Mock<IMessageConsumer> _mockConsumer;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly Mock<ILogger<MessageProcessorWorker>> _mockLogger;
    private readonly MessageProcessorWorkerConfiguration _configuration;
    private readonly Mock<IMongoCollection<Message>> _mockMessageCollection;
    private readonly Mock<IMongoCollection<Conversation>> _mockConversationCollection;

    public MessageProcessorWorkerTests()
    {
        _mockConsumer = new Mock<IMessageConsumer>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _mockLogger = new Mock<ILogger<MessageProcessorWorker>>();
        
        // Create mock collections
        _mockMessageCollection = new Mock<IMongoCollection<Message>>();
        _mockConversationCollection = new Mock<IMongoCollection<Conversation>>();

        _configuration = new MessageProcessorWorkerConfiguration
        {
            SignalRHubUrl = "http://localhost:5000/hubs/chat",
            DeadLetterQueueName = "messages.process.deadletter",
            MaxRetryAttempts = 3
        };
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConsumerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageProcessorWorker(
            null!,
            _mockPublisher.Object,
            _mockMessageCollection.Object,
            _mockConversationCollection.Object,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPublisherIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageProcessorWorker(
            _mockConsumer.Object,
            null!,
            _mockMessageCollection.Object,
            _mockConversationCollection.Object,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMessagesCollectionIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            null!,
            _mockConversationCollection.Object,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConversationsCollectionIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            _mockMessageCollection.Object,
            null!,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            _mockMessageCollection.Object,
            _mockConversationCollection.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            _mockMessageCollection.Object,
            _mockConversationCollection.Object,
            _configuration,
            null!));
    }

    [Fact]
    public void Constructor_ShouldCreateWorkerSuccessfully_WhenAllParametersAreValid()
    {
        // Act
        var worker = new MessageProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            _mockMessageCollection.Object,
            _mockConversationCollection.Object,
            _configuration,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void MessageQueuePayload_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var payload = new MessageQueuePayload
        {
            ConversationId = "507f1f77bcf86cd799439011",
            SenderId = "507f1f77bcf86cd799439012",
            SenderName = "John Doe",
            Type = "text",
            Content = "Hello, World!",
            Attachments = new List<string> { "file1.txt", "file2.jpg" }
        };

        // Act
        var json = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<MessageQueuePayload>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(payload.ConversationId, deserialized.ConversationId);
        Assert.Equal(payload.SenderId, deserialized.SenderId);
        Assert.Equal(payload.SenderName, deserialized.SenderName);
        Assert.Equal(payload.Type, deserialized.Type);
        Assert.Equal(payload.Content, deserialized.Content);
        Assert.Equal(2, deserialized.Attachments.Count);
    }

    [Fact]
    public void MessageProcessorWorkerConfiguration_ShouldHaveDefaultValues()
    {
        // Act
        var config = new MessageProcessorWorkerConfiguration();

        // Assert
        Assert.Equal("http://localhost:5000/hubs/chat", config.SignalRHubUrl);
        Assert.Equal("messages.process.deadletter", config.DeadLetterQueueName);
        Assert.Equal(3, config.MaxRetryAttempts);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
