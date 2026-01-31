using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Testcontainers.MongoDb;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Tests.Infrastructure;
using TraliVali.Workers;
using TraliVali.Workers.Models;

namespace TraliVali.Tests.Workers;

/// <summary>
/// End-to-end integration tests for MessageProcessorWorker using Testcontainers
/// </summary>
public class MessageProcessorWorkerIntegrationTests : IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private readonly RabbitMqFixture _rabbitMqFixture;
    private MongoDbContainer? _mongoContainer;
    private IMongoDatabase? _database;
    private IMongoCollection<Message>? _messagesCollection;
    private IMongoCollection<Conversation>? _conversationsCollection;
    
    public MessageProcessorWorkerIntegrationTests(RabbitMqFixture rabbitMqFixture)
    {
        _rabbitMqFixture = rabbitMqFixture;
    }

    public async Task InitializeAsync()
    {
        // Set up MongoDB container
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();
        
        await _mongoContainer.StartAsync();
        
        var client = new MongoClient(_mongoContainer.GetConnectionString());
        _database = client.GetDatabase("tralivali_test");
        _messagesCollection = _database.GetCollection<Message>("messages");
        _conversationsCollection = _database.GetCollection<Conversation>("conversations");
    }

    public async Task DisposeAsync()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.StopAsync();
            await _mongoContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task Worker_ShouldProcessMessageEndToEnd()
    {
        // Arrange
        var conversationId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var senderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        // Create a conversation first
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = senderId, JoinedAt = DateTime.UtcNow },
                new Participant { UserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), JoinedAt = DateTime.UtcNow }
            },
            CreatedAt = DateTime.UtcNow,
            RecentMessages = new List<string>()
        };
        await _conversationsCollection!.InsertOneAsync(conversation);

        var mockHubContext = new Mock<IHubContext<TestHub, ITestClient>>();
        var mockClients = new Mock<IHubClients<ITestClient>>();
        var mockClientProxy = new Mock<ITestClient>();
        var messageReceivedBySignalR = new TaskCompletionSource<bool>();

        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClientProxy.Setup(c => c.ReceiveMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .Callback(() => messageReceivedBySignalR.TrySetResult(true))
            .Returns(Task.CompletedTask);

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<RabbitMqService>();
        var messageConsumer = new RabbitMqService(_rabbitMqFixture.Configuration, logger);
        var messagePublisher = new RabbitMqService(_rabbitMqFixture.Configuration, logger);
        await messageConsumer.InitializeAsync();
        await messagePublisher.InitializeAsync();

        var workerLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<MessageProcessorWorker<TestHub, ITestClient>>();

        var configuration = new MessageProcessorWorkerConfiguration
        {
            DeadLetterQueueName = "messages.process.deadletter",
            MaxRetryAttempts = 3
        };

        var worker = new MessageProcessorWorker<TestHub, ITestClient>(
            messageConsumer,
            messagePublisher,
            _messagesCollection!,
            _conversationsCollection!,
            mockHubContext.Object,
            configuration,
            workerLogger);

        // Act - Start the worker
        using var cts = new CancellationTokenSource();
        var workerTask = worker.StartAsync(cts.Token);

        // Give worker time to start consuming
        await Task.Delay(2000);

        // Publish a message
        var payload = new MessageQueuePayload
        {
            ConversationId = conversationId,
            SenderId = senderId,
            SenderName = "John Doe",
            Type = "text",
            Content = "Hello from integration test!",
            Attachments = new List<string>()
        };

        var messageJson = JsonSerializer.Serialize(payload);
        await messagePublisher.PublishAsync("messages.process", messageJson);

        // Wait for processing (with timeout)
        var signalRReceived = await Task.WhenAny(messageReceivedBySignalR.Task, Task.Delay(10000));

        // Stop the worker
        await worker.StopAsync(CancellationToken.None);
        cts.Cancel();

        // Assert
        Assert.True(signalRReceived == messageReceivedBySignalR.Task, "SignalR did not receive the message");

        // Verify message was persisted to MongoDB
        var persistedMessages = await _messagesCollection!
            .Find(m => m.ConversationId == conversationId)
            .ToListAsync();

        Assert.Single(persistedMessages);
        Assert.Equal(senderId, persistedMessages[0].SenderId);
        Assert.Equal("text", persistedMessages[0].Type);
        Assert.Equal("Hello from integration test!", persistedMessages[0].Content);

        // Verify conversation was updated
        var updatedConversation = await _conversationsCollection!
            .Find(c => c.Id == conversationId)
            .FirstOrDefaultAsync();

        Assert.NotNull(updatedConversation);
        Assert.Single(updatedConversation.RecentMessages);
        Assert.Equal(persistedMessages[0].Id, updatedConversation.RecentMessages[0]);

        // Verify SignalR was called with correct parameters
        mockClientProxy.Verify(c => c.ReceiveMessage(
            conversationId,
            It.IsAny<string>(),
            senderId,
            "John Doe",
            "Hello from integration test!",
            It.IsAny<DateTime>()), Times.Once);

        // Cleanup
        messageConsumer.Dispose();
        messagePublisher.Dispose();
    }

    [Fact]
    public async Task Worker_ShouldSendInvalidMessageToDeadLetterQueue()
    {
        // Arrange
        var deadLetterMessages = new List<string>();
        var deadLetterLock = new object();
        var deadLetterReceived = new TaskCompletionSource<bool>();

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<RabbitMqService>();
        
        var messageConsumer = new RabbitMqService(_rabbitMqFixture.Configuration, logger);
        var messagePublisher = new RabbitMqService(_rabbitMqFixture.Configuration, logger);
        var deadLetterConsumer = new RabbitMqService(_rabbitMqFixture.Configuration, logger);
        
        await messageConsumer.InitializeAsync();
        await messagePublisher.InitializeAsync();
        await deadLetterConsumer.InitializeAsync();

        // Start consuming from dead-letter queue
        await deadLetterConsumer.StartConsumingAsync("messages.process.deadletter", async (message) =>
        {
            lock (deadLetterLock)
            {
                if (deadLetterMessages.Count == 0)
                {
                    deadLetterMessages.Add(message);
                    deadLetterReceived.TrySetResult(true);
                }
            }
            await Task.CompletedTask;
        });

        var mockHubContext = new Mock<IHubContext<TestHub, ITestClient>>();
        var workerLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<MessageProcessorWorker<TestHub, ITestClient>>();

        var configuration = new MessageProcessorWorkerConfiguration
        {
            DeadLetterQueueName = "messages.process.deadletter",
            MaxRetryAttempts = 3
        };

        var worker = new MessageProcessorWorker<TestHub, ITestClient>(
            messageConsumer,
            messagePublisher,
            _messagesCollection!,
            _conversationsCollection!,
            mockHubContext.Object,
            configuration,
            workerLogger);

        // Act - Start the worker
        using var cts = new CancellationTokenSource();
        var workerTask = worker.StartAsync(cts.Token);

        // Give worker time to start
        await Task.Delay(2000);

        // Publish an invalid message (missing required fields)
        var invalidPayload = new MessageQueuePayload
        {
            ConversationId = "",  // Invalid: empty conversation ID
            SenderId = "",  // Invalid: empty sender ID
            SenderName = "Test User",
            Type = "text",
            Content = "This should go to dead-letter queue",
            Attachments = new List<string>()
        };

        var messageJson = JsonSerializer.Serialize(invalidPayload);
        await messagePublisher.PublishAsync("messages.process", messageJson);

        // Wait for dead-letter message
        var dlReceived = await Task.WhenAny(deadLetterReceived.Task, Task.Delay(10000));

        // Stop the worker
        await worker.StopAsync(CancellationToken.None);
        cts.Cancel();

        // Assert
        Assert.True(dlReceived == deadLetterReceived.Task, "Dead-letter message was not received");
        Assert.Single(deadLetterMessages);

        var deadLetterPayload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(deadLetterMessages[0]);
        Assert.NotNull(deadLetterPayload);
        Assert.True(deadLetterPayload.ContainsKey("OriginalMessage"));
        Assert.True(deadLetterPayload.ContainsKey("Reason"));
        // The reason contains validation errors or processing failures
        var reason = deadLetterPayload["Reason"].GetString();
        Assert.Contains("failed", reason, StringComparison.OrdinalIgnoreCase);

        // Cleanup
        deadLetterConsumer.Dispose();
        messageConsumer.Dispose();
        messagePublisher.Dispose();
    }
}
