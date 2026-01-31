using System.Text.Json;
using Microsoft.Extensions.Logging;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Messaging;

/// <summary>
/// Integration tests for RabbitMQ dead-letter queue functionality using Testcontainers
/// </summary>
[Collection("Sequential")]
public class RabbitMqDeadLetterQueueTests : IClassFixture<RabbitMqFixture>
{
    private readonly RabbitMqFixture _fixture;

    public RabbitMqDeadLetterQueueTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeadLetterQueue_ShouldReceiveMessages()
    {
        // Arrange
        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var deadLetterQueueName = "messages.process.deadletter";

        // Act - Start consuming from dead-letter queue
        await _fixture.Service.StartConsumingAsync(deadLetterQueueName, async (message) =>
        {
            receivedMessages.Add(message);
            messageReceived.SetResult(true);
            await Task.CompletedTask;
        });

        // Publish a message directly to the dead-letter queue
        var deadLetterPayload = new
        {
            OriginalMessage = "Failed message content",
            Reason = "Test failure reason",
            FailedAt = DateTime.UtcNow
        };
        var deadLetterJson = JsonSerializer.Serialize(deadLetterPayload);
        await _fixture.Service.PublishAsync(deadLetterQueueName, deadLetterJson);

        // Wait for message to be received (with timeout)
        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == messageReceived.Task, "Dead-letter message was not received within timeout");
        Assert.Single(receivedMessages);
        
        var deserializedMessage = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(receivedMessages[0]);
        Assert.NotNull(deserializedMessage);
        Assert.True(deserializedMessage.ContainsKey("OriginalMessage"));
        Assert.True(deserializedMessage.ContainsKey("Reason"));
        Assert.Equal("Failed message content", deserializedMessage["OriginalMessage"].GetString());
        Assert.Equal("Test failure reason", deserializedMessage["Reason"].GetString());

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task DeadLetterQueue_ShouldHandleMultipleFailedMessages()
    {
        // Arrange
        var receivedMessages = new List<string>();
        var allMessagesReceived = new TaskCompletionSource<bool>();
        var deadLetterQueueName = "messages.process.deadletter";
        var expectedMessageCount = 3;

        // Act - Start consuming from dead-letter queue
        await _fixture.Service.StartConsumingAsync(deadLetterQueueName, async (message) =>
        {
            receivedMessages.Add(message);
            if (receivedMessages.Count == expectedMessageCount)
            {
                allMessagesReceived.SetResult(true);
            }
            await Task.CompletedTask;
        });

        // Publish multiple failed messages
        for (int i = 0; i < expectedMessageCount; i++)
        {
            var deadLetterPayload = new
            {
                OriginalMessage = $"Failed message {i + 1}",
                Reason = $"Failure reason {i + 1}",
                FailedAt = DateTime.UtcNow,
                RetryCount = i
            };
            var deadLetterJson = JsonSerializer.Serialize(deadLetterPayload);
            await _fixture.Service.PublishAsync(deadLetterQueueName, deadLetterJson);
        }

        // Wait for all messages to be received (with timeout)
        var received = await Task.WhenAny(allMessagesReceived.Task, Task.Delay(10000));

        // Assert
        Assert.True(received == allMessagesReceived.Task, "Not all dead-letter messages were received within timeout");
        Assert.Equal(expectedMessageCount, receivedMessages.Count);

        // Verify each message
        for (int i = 0; i < expectedMessageCount; i++)
        {
            var deserializedMessage = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(receivedMessages[i]);
            Assert.NotNull(deserializedMessage);
            Assert.Contains($"Failed message {i + 1}", deserializedMessage["OriginalMessage"].GetString());
        }

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task DeadLetterQueue_ShouldPersistMessagesWhenNoConsumer()
    {
        // Arrange
        var deadLetterQueueName = "messages.process.deadletter";
        var testMessage = new
        {
            OriginalMessage = "Message without consumer",
            Reason = "Testing persistence",
            FailedAt = DateTime.UtcNow
        };

        // Act - Publish to dead-letter queue without any consumer
        var messageJson = JsonSerializer.Serialize(testMessage);
        await _fixture.Service.PublishAsync(deadLetterQueueName, messageJson);

        // Give some time for the message to persist in the queue
        await Task.Delay(500);

        // Now start consuming
        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        
        await _fixture.Service.StartConsumingAsync(deadLetterQueueName, async (message) =>
        {
            receivedMessages.Add(message);
            messageReceived.SetResult(true);
            await Task.CompletedTask;
        });

        // Wait for the persisted message to be received
        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == messageReceived.Task, "Persisted dead-letter message was not received");
        Assert.Single(receivedMessages);
        Assert.Contains("Message without consumer", receivedMessages[0]);

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task DeadLetterQueue_ShouldPreserveMessageContent()
    {
        // Arrange
        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var deadLetterQueueName = "messages.process.deadletter";

        var complexPayload = new
        {
            OriginalMessage = new
            {
                ConversationId = "507f1f77bcf86cd799439011",
                SenderId = "507f1f77bcf86cd799439012",
                SenderName = "John Doe",
                Type = "text",
                Content = "This is a test message with special characters: !@#$%^&*()",
                Attachments = new List<string> { "file1.pdf", "image.png" }
            },
            Reason = "Validation failed: Missing required field",
            FailedAt = DateTime.UtcNow,
            ErrorDetails = new
            {
                Code = "VALIDATION_ERROR",
                Field = "ConversationId"
            }
        };

        // Act
        await _fixture.Service.StartConsumingAsync(deadLetterQueueName, async (message) =>
        {
            receivedMessages.Add(message);
            messageReceived.SetResult(true);
            await Task.CompletedTask;
        });

        var messageJson = JsonSerializer.Serialize(complexPayload);
        await _fixture.Service.PublishAsync(deadLetterQueueName, messageJson);

        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == messageReceived.Task, "Message was not received");
        Assert.Single(receivedMessages);

        var deserializedMessage = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(receivedMessages[0]);
        Assert.NotNull(deserializedMessage);
        
        // Verify complex nested structure is preserved
        var originalMessage = deserializedMessage["OriginalMessage"];
        Assert.Equal("507f1f77bcf86cd799439011", originalMessage.GetProperty("ConversationId").GetString());
        Assert.Equal("John Doe", originalMessage.GetProperty("SenderName").GetString());
        Assert.Contains("special characters", originalMessage.GetProperty("Content").GetString());
        
        var errorDetails = deserializedMessage["ErrorDetails"];
        Assert.Equal("VALIDATION_ERROR", errorDetails.GetProperty("Code").GetString());

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task DeadLetterQueue_ShouldSupportConcurrentConsumption()
    {
        // Arrange
        var receivedMessages = new List<string>();
        var messagesLock = new object();
        var allMessagesReceived = new TaskCompletionSource<bool>();
        var deadLetterQueueName = "messages.process.deadletter";
        var messageCount = 10;

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<RabbitMqService>();
        using var service = new RabbitMqService(_fixture.Configuration, logger);
        await service.InitializeAsync();

        // Act - Start consuming
        await service.StartConsumingAsync(deadLetterQueueName, async (message) =>
        {
            lock (messagesLock)
            {
                receivedMessages.Add(message);
                if (receivedMessages.Count == messageCount)
                {
                    allMessagesReceived.SetResult(true);
                }
            }
            await Task.CompletedTask;
        });

        // Publish messages concurrently
        var publishTasks = Enumerable.Range(1, messageCount).Select(i =>
        {
            var payload = new
            {
                OriginalMessage = $"Concurrent message {i}",
                Reason = "Concurrent processing test",
                FailedAt = DateTime.UtcNow
            };
            return service.PublishAsync(deadLetterQueueName, JsonSerializer.Serialize(payload));
        });

        await Task.WhenAll(publishTasks);

        // Wait for all messages
        var received = await Task.WhenAny(allMessagesReceived.Task, Task.Delay(10000));

        // Assert
        Assert.True(received == allMessagesReceived.Task, "Not all messages received in time");
        Assert.Equal(messageCount, receivedMessages.Count);
        
        // Verify all messages are unique
        var uniqueMessages = receivedMessages.Select(m => 
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(m);
            return parsed!["OriginalMessage"].GetString();
        }).Distinct().ToList();
        
        Assert.Equal(messageCount, uniqueMessages.Count);
    }
}
