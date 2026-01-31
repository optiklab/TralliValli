using System.Text.Json;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Tests.Infrastructure;
using TraliVali.Workers.Models;

namespace TraliVali.Tests.Workers;

/// <summary>
/// Integration tests for RabbitMQ worker end-to-end scenarios
/// These tests verify worker queue processing with Testcontainers
/// </summary>
public class WorkerRabbitMqIntegrationTests : IClassFixture<RabbitMqFixture>
{
    private readonly RabbitMqFixture _fixture;

    public WorkerRabbitMqIntegrationTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MessageQueue_ShouldSupportWorkerConsumptionPattern()
    {
        // Arrange
        var messagesReceived = new List<MessageQueuePayload>();
        var messagesLock = new object();
        var allMessagesReceived = new TaskCompletionSource<bool>();
        var expectedCount = 3;

        // Simulate worker consumption pattern
        await _fixture.Service.StartConsumingAsync("messages.process", async (messageJson) =>
        {
            var payload = JsonSerializer.Deserialize<MessageQueuePayload>(messageJson);
            if (payload != null)
            {
                lock (messagesLock)
                {
                    messagesReceived.Add(payload);
                    if (messagesReceived.Count == expectedCount)
                    {
                        allMessagesReceived.TrySetResult(true);
                    }
                }
            }
            await Task.CompletedTask;
        });

        // Act - Publish multiple messages as a worker would receive them
        var messages = new[]
        {
            new MessageQueuePayload
            {
                ConversationId = "507f1f77bcf86cd799439011",
                SenderId = "507f1f77bcf86cd799439012",
                SenderName = "Alice",
                Type = "text",
                Content = "Message 1",
                Attachments = new List<string>()
            },
            new MessageQueuePayload
            {
                ConversationId = "507f1f77bcf86cd799439011",
                SenderId = "507f1f77bcf86cd799439013",
                SenderName = "Bob",
                Type = "text",
                Content = "Message 2",
                Attachments = new List<string>()
            },
            new MessageQueuePayload
            {
                ConversationId = "507f1f77bcf86cd799439011",
                SenderId = "507f1f77bcf86cd799439014",
                SenderName = "Charlie",
                Type = "text",
                Content = "Message 3",
                Attachments = new List<string>()
            }
        };

        foreach (var message in messages)
        {
            var json = JsonSerializer.Serialize(message);
            await _fixture.Service.PublishAsync("messages.process", json);
        }

        // Wait for all messages
        var received = await Task.WhenAny(allMessagesReceived.Task, Task.Delay(10000));

        // Assert
        Assert.True(received == allMessagesReceived.Task, "Not all messages were received");
        Assert.Equal(expectedCount, messagesReceived.Count);
        Assert.Equal("Alice", messagesReceived[0].SenderName);
        Assert.Equal("Bob", messagesReceived[1].SenderName);
        Assert.Equal("Charlie", messagesReceived[2].SenderName);

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task DeadLetterQueue_ShouldHandleWorkerFailureScenario()
    {
        // Arrange
        var failedMessages = new List<string>();
        var failedLock = new object();
        var failureReceived = new TaskCompletionSource<bool>();

        // Simulate dead-letter queue consumer (monitoring failed messages)
        await _fixture.Service.StartConsumingAsync("messages.process.deadletter", async (messageJson) =>
        {
            lock (failedLock)
            {
                if (failedMessages.Count == 0)
                {
                    failedMessages.Add(messageJson);
                    failureReceived.TrySetResult(true);
                }
            }
            await Task.CompletedTask;
        });

        // Act - Simulate a worker sending a failed message to DLQ
        var failedPayload = new
        {
            OriginalMessage = JsonSerializer.Serialize(new MessageQueuePayload
            {
                ConversationId = "invalid",
                SenderId = "invalid",
                SenderName = "Test",
                Type = "text",
                Content = "Failed message",
                Attachments = new List<string>()
            }),
            Reason = "Simulated worker processing failure",
            FailedAt = DateTime.UtcNow,
            AttemptNumber = 3
        };

        var dlqJson = JsonSerializer.Serialize(failedPayload);
        await _fixture.Service.PublishAsync("messages.process.deadletter", dlqJson);

        // Wait for failure notification
        var received = await Task.WhenAny(failureReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == failureReceived.Task, "Failed message not received in DLQ");
        Assert.Single(failedMessages);

        var dlqMessage = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(failedMessages[0]);
        Assert.NotNull(dlqMessage);
        Assert.Contains("Reason", dlqMessage.Keys);
        Assert.Contains("OriginalMessage", dlqMessage.Keys);

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task WorkerQueues_ShouldSupportMultipleQueueTypes()
    {
        // Arrange
        var messageQueueReceived = new TaskCompletionSource<bool>();
        var fileQueueReceived = new TaskCompletionSource<bool>();
        var archivalQueueReceived = new TaskCompletionSource<bool>();

        // Set up consumers for different worker queues
        await _fixture.Service.StartConsumingAsync("messages.process", async (msg) =>
        {
            messageQueueReceived.TrySetResult(true);
            await Task.CompletedTask;
        });

        await _fixture.Service.StartConsumingAsync("files.process", async (msg) =>
        {
            fileQueueReceived.TrySetResult(true);
            await Task.CompletedTask;
        });

        await _fixture.Service.StartConsumingAsync("archival.process", async (msg) =>
        {
            archivalQueueReceived.TrySetResult(true);
            await Task.CompletedTask;
        });

        // Act - Publish to different queues
        await _fixture.Service.PublishAsync("messages.process", "{\"type\":\"message\"}");
        await _fixture.Service.PublishAsync("files.process", "{\"type\":\"file\"}");
        await _fixture.Service.PublishAsync("archival.process", "{\"type\":\"archival\"}");

        // Wait for all
        var allReceived = await Task.WhenAll(
            Task.WhenAny(messageQueueReceived.Task, Task.Delay(5000)),
            Task.WhenAny(fileQueueReceived.Task, Task.Delay(5000)),
            Task.WhenAny(archivalQueueReceived.Task, Task.Delay(5000))
        );

        // Assert
        Assert.True(messageQueueReceived.Task.IsCompleted, "Message queue not processed");
        Assert.True(fileQueueReceived.Task.IsCompleted, "File queue not processed");
        Assert.True(archivalQueueReceived.Task.IsCompleted, "Archival queue not processed");

        // Cleanup
        _fixture.Service.StopConsuming();
    }

    [Fact]
    public async Task WorkerQueue_ShouldMaintainMessageOrder()
    {
        // Arrange
        var receivedMessages = new List<string>();
        var messagesLock = new object();
        var allReceived = new TaskCompletionSource<bool>();
        var expectedMessages = new[] { "First", "Second", "Third", "Fourth", "Fifth" };

        await _fixture.Service.StartConsumingAsync("backup.process", async (msg) =>
        {
            lock (messagesLock)
            {
                receivedMessages.Add(msg);
                if (receivedMessages.Count == expectedMessages.Length)
                {
                    allReceived.TrySetResult(true);
                }
            }
            await Task.CompletedTask;
        });

        // Act - Publish messages in sequence
        foreach (var message in expectedMessages)
        {
            await _fixture.Service.PublishAsync("backup.process", message);
        }

        var received = await Task.WhenAny(allReceived.Task, Task.Delay(10000));

        // Assert
        Assert.True(received == allReceived.Task, "Not all messages received");
        Assert.Equal(expectedMessages.Length, receivedMessages.Count);
        
        // While RabbitMQ doesn't guarantee strict ordering, in a single-consumer scenario
        // with our test setup, messages should typically arrive in order
        for (int i = 0; i < expectedMessages.Length; i++)
        {
            Assert.Equal(expectedMessages[i], receivedMessages[i]);
        }

        // Cleanup
        _fixture.Service.StopConsuming();
    }
}
