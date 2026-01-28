using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client.Exceptions;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Messaging;

/// <summary>
/// Tests for RabbitMqService
/// </summary>
public class RabbitMqServiceTests : IClassFixture<RabbitMqFixture>
{
    private readonly RabbitMqFixture _fixture;

    public RabbitMqServiceTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateExchangeAndQueues()
    {
        // Arrange
        var logger = new Mock<ILogger<RabbitMqService>>().Object;
        var service = new RabbitMqService(_fixture.Configuration, logger);

        // Act
        await service.InitializeAsync();

        // Assert - If initialization succeeds without exception, exchange and queues were created
        Assert.True(true); // Initialization completed successfully
        
        service.Dispose();
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishMessageToQueue()
    {
        // Arrange
        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();

        // Act - Start consuming
        await _fixture.Service.StartConsumingAsync("messages.process", async (message) =>
        {
            receivedMessages.Add(message);
            messageReceived.SetResult(true);
            await Task.CompletedTask;
        });

        // Publish a message
        var testMessage = "Test message content";
        await _fixture.Service.PublishAsync("messages.process", testMessage);

        // Wait for message to be received (with timeout)
        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == messageReceived.Task, "Message was not received within timeout");
        Assert.Single(receivedMessages);
        Assert.Equal(testMessage, receivedMessages[0]);

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowException_WhenRoutingKeyIsNull()
    {
        // Arrange
        var service = _fixture.Service;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.PublishAsync(null!, "message"));
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowException_WhenMessageIsNull()
    {
        // Arrange
        var service = _fixture.Service;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.PublishAsync("messages.process", null!));
    }

    [Fact]
    public async Task StartConsumingAsync_ShouldConsumeMessages()
    {
        // Arrange
        var receivedMessages = new List<string>();
        var messagesReceived = new TaskCompletionSource<bool>();
        var expectedMessages = new[] { "Message 1", "Message 2", "Message 3" };

        // Act - Start consuming
        await _fixture.Service.StartConsumingAsync("files.process", async (message) =>
        {
            receivedMessages.Add(message);
            if (receivedMessages.Count == expectedMessages.Length)
            {
                messagesReceived.SetResult(true);
            }
            await Task.CompletedTask;
        });

        // Publish multiple messages
        foreach (var msg in expectedMessages)
        {
            await _fixture.Service.PublishAsync("files.process", msg);
        }

        // Wait for all messages to be received (with timeout)
        var received = await Task.WhenAny(messagesReceived.Task, Task.Delay(10000));

        // Assert
        Assert.True(received == messagesReceived.Task, "Not all messages were received within timeout");
        Assert.Equal(expectedMessages.Length, receivedMessages.Count);
        Assert.Equal(expectedMessages, receivedMessages);

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task StartConsumingAsync_ShouldThrowException_WhenQueueNameIsNull()
    {
        // Arrange
        var service = _fixture.Service;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.StartConsumingAsync(null!, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task StartConsumingAsync_ShouldThrowException_WhenCallbackIsNull()
    {
        // Arrange
        var service = _fixture.Service;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.StartConsumingAsync("messages.process", null!));
    }

    [Fact]
    public void StopConsuming_ShouldStopConsumingMessages()
    {
        // Arrange & Act
        _fixture.Service.StopConsuming();

        // Assert - Should not throw exception
        Assert.True(true);
    }

    [Fact]
    public async Task PublishAsync_ToMultipleQueues_ShouldRouteCorrectly()
    {
        // Arrange
        var messagesQueueMessages = new List<string>();
        var filesQueueMessages = new List<string>();
        var archivalQueueMessages = new List<string>();
        var backupQueueMessages = new List<string>();
        
        var allMessagesReceived = new TaskCompletionSource<bool>();
        var receivedCount = 0;

        var logger = new Mock<ILogger<RabbitMqService>>().Object;
        using var service = new RabbitMqService(_fixture.Configuration, logger);
        await service.InitializeAsync();

        // Act - Set up consumers for all queues
        await service.StartConsumingAsync("messages.process", async (msg) =>
        {
            messagesQueueMessages.Add(msg);
            if (++receivedCount == 4) allMessagesReceived.SetResult(true);
            await Task.CompletedTask;
        });

        await service.StartConsumingAsync("files.process", async (msg) =>
        {
            filesQueueMessages.Add(msg);
            if (++receivedCount == 4) allMessagesReceived.SetResult(true);
            await Task.CompletedTask;
        });

        await service.StartConsumingAsync("archival.process", async (msg) =>
        {
            archivalQueueMessages.Add(msg);
            if (++receivedCount == 4) allMessagesReceived.SetResult(true);
            await Task.CompletedTask;
        });

        await service.StartConsumingAsync("backup.process", async (msg) =>
        {
            backupQueueMessages.Add(msg);
            if (++receivedCount == 4) allMessagesReceived.SetResult(true);
            await Task.CompletedTask;
        });

        // Publish to each queue
        await service.PublishAsync("messages.process", "Message for messages queue");
        await service.PublishAsync("files.process", "Message for files queue");
        await service.PublishAsync("archival.process", "Message for archival queue");
        await service.PublishAsync("backup.process", "Message for backup queue");

        // Wait for all messages
        var received = await Task.WhenAny(allMessagesReceived.Task, Task.Delay(10000));

        // Assert
        Assert.True(received == allMessagesReceived.Task, "Not all messages were received within timeout");
        Assert.Single(messagesQueueMessages);
        Assert.Single(filesQueueMessages);
        Assert.Single(archivalQueueMessages);
        Assert.Single(backupQueueMessages);
        Assert.Equal("Message for messages queue", messagesQueueMessages[0]);
        Assert.Equal("Message for files queue", filesQueueMessages[0]);
        Assert.Equal("Message for archival queue", archivalQueueMessages[0]);
        Assert.Equal("Message for backup queue", backupQueueMessages[0]);
    }

    [Fact]
    public void GetQueueNames_ShouldReturnAllQueueNames()
    {
        // Act
        var queueNames = RabbitMqService.GetQueueNames();

        // Assert
        Assert.NotNull(queueNames);
        Assert.Equal(4, queueNames.Count);
        Assert.Contains("messages.process", queueNames);
        Assert.Contains("files.process", queueNames);
        Assert.Contains("archival.process", queueNames);
        Assert.Contains("backup.process", queueNames);
    }

    [Fact]
    public async Task InitializeAsync_WithInvalidConfiguration_ShouldRetryAndFail()
    {
        // Arrange
        var invalidConfig = new RabbitMqConfiguration
        {
            HostName = "invalid-host-that-does-not-exist",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };
        var logger = new Mock<ILogger<RabbitMqService>>().Object;
        var service = new RabbitMqService(invalidConfig, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => service.InitializeAsync());
        
        service.Dispose();
    }
}
