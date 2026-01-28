using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Workers.Models;

namespace TraliVali.Workers;

/// <summary>
/// Configuration for MessageProcessorWorker
/// </summary>
public class MessageProcessorWorkerConfiguration
{
    /// <summary>
    /// Gets or sets the SignalR hub URL
    /// </summary>
    public string SignalRHubUrl { get; set; } = "http://localhost:5000/hubs/chat";

    /// <summary>
    /// Gets or sets the dead-letter queue name
    /// </summary>
    public string DeadLetterQueueName { get; set; } = "messages.process.deadletter";

    /// <summary>
    /// Gets or sets the maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}

/// <summary>
/// Background worker that processes messages from the message queue
/// </summary>
public class MessageProcessorWorker : BackgroundService
{
    private const string QueueName = "messages.process";
    private readonly IMessageConsumer _messageConsumer;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IMongoCollection<Message> _messagesCollection;
    private readonly IMongoCollection<Conversation> _conversationsCollection;
    private readonly ILogger<MessageProcessorWorker> _logger;
    private readonly MessageProcessorWorkerConfiguration _configuration;
    private HubConnection? _hubConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageProcessorWorker"/> class
    /// </summary>
    /// <param name="messageConsumer">The message consumer</param>
    /// <param name="messagePublisher">The message publisher</param>
    /// <param name="messagesCollection">The messages collection</param>
    /// <param name="conversationsCollection">The conversations collection</param>
    /// <param name="configuration">The worker configuration</param>
    /// <param name="logger">The logger instance</param>
    public MessageProcessorWorker(
        IMessageConsumer messageConsumer,
        IMessagePublisher messagePublisher,
        IMongoCollection<Message> messagesCollection,
        IMongoCollection<Conversation> conversationsCollection,
        MessageProcessorWorkerConfiguration configuration,
        ILogger<MessageProcessorWorker> logger)
    {
        _messageConsumer = messageConsumer ?? throw new ArgumentNullException(nameof(messageConsumer));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _messagesCollection = messagesCollection ?? throw new ArgumentNullException(nameof(messagesCollection));
        _conversationsCollection = conversationsCollection ?? throw new ArgumentNullException(nameof(conversationsCollection));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the worker
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageProcessorWorker starting...");

        try
        {
            // Initialize SignalR connection
            await InitializeSignalRConnectionAsync(stoppingToken);

            // Start consuming messages
            await _messageConsumer.StartConsumingAsync(QueueName, ProcessMessageAsync, stoppingToken);

            _logger.LogInformation("MessageProcessorWorker started successfully and consuming from {QueueName}", QueueName);

            // Keep the worker running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MessageProcessorWorker stopping due to cancellation request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in MessageProcessorWorker");
            throw;
        }
    }

    /// <summary>
    /// Initializes the SignalR connection
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task InitializeSignalRConnectionAsync(CancellationToken cancellationToken)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_configuration.SignalRHubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning(error, "SignalR connection closed. Attempting to reconnect...");
            await Task.Delay(5000, cancellationToken);
            try
            {
                await _hubConnection.StartAsync(cancellationToken);
                _logger.LogInformation("SignalR connection re-established");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect to SignalR");
            }
        };

        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("SignalR connection established to {HubUrl}", _configuration.SignalRHubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub at {HubUrl}", _configuration.SignalRHubUrl);
            throw;
        }
    }

    /// <summary>
    /// Processes a message from the queue
    /// </summary>
    /// <param name="messageJson">The message JSON payload</param>
    private async Task ProcessMessageAsync(string messageJson)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount < _configuration.MaxRetryAttempts)
        {
            try
            {
                _logger.LogDebug("Processing message: {MessageJson}", messageJson);

                // Deserialize the message payload
                var payload = JsonSerializer.Deserialize<MessageQueuePayload>(messageJson);
                if (payload == null)
                {
                    _logger.LogError("Failed to deserialize message payload");
                    await SendToDeadLetterQueueAsync(messageJson, "Failed to deserialize payload");
                    return;
                }

                // Create message entity
                var message = new Message
                {
                    ConversationId = payload.ConversationId,
                    SenderId = payload.SenderId,
                    Type = payload.Type,
                    Content = payload.Content,
                    EncryptedContent = payload.EncryptedContent ?? string.Empty,
                    ReplyTo = payload.ReplyTo,
                    Attachments = payload.Attachments,
                    CreatedAt = DateTime.UtcNow
                };

                // Validate message
                var validationErrors = message.Validate();
                if (validationErrors.Count > 0)
                {
                    _logger.LogError("Message validation failed: {Errors}", string.Join(", ", validationErrors));
                    await SendToDeadLetterQueueAsync(messageJson, $"Validation failed: {string.Join(", ", validationErrors)}");
                    return;
                }

                // TODO: Phase 5 - Implement encryption
                // For now, just log that encryption would happen here
                _logger.LogDebug("Encryption placeholder - would encrypt content here in Phase 5");

                // Persist to MongoDB
                await _messagesCollection.InsertOneAsync(message);
                _logger.LogInformation("Message {MessageId} persisted to MongoDB", message.Id);

                // Update conversation's recentMessages array
                await UpdateConversationRecentMessagesAsync(message.ConversationId, message.Id);

                // Broadcast via SignalR to conversation participants
                await BroadcastMessageAsync(message, payload.SenderName);

                _logger.LogInformation("Successfully processed message {MessageId} for conversation {ConversationId}", 
                    message.Id, message.ConversationId);

                return; // Success - exit the retry loop
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                _logger.LogWarning(ex, "Error processing message (attempt {RetryCount}/{MaxRetries})", 
                    retryCount, _configuration.MaxRetryAttempts);

                if (retryCount < _configuration.MaxRetryAttempts)
                {
                    // Wait before retrying (exponential backoff)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                }
            }
        }

        // If we get here, all retries failed
        _logger.LogError(lastException, "Failed to process message after {MaxRetries} attempts", _configuration.MaxRetryAttempts);
        await SendToDeadLetterQueueAsync(messageJson, $"Processing failed after {_configuration.MaxRetryAttempts} attempts: {lastException?.Message}");
    }

    /// <summary>
    /// Updates the conversation's recentMessages array
    /// </summary>
    /// <param name="conversationId">The conversation identifier</param>
    /// <param name="messageId">The message identifier</param>
    private async Task UpdateConversationRecentMessagesAsync(string conversationId, string messageId)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, conversationId);
        
        // Get the current conversation to check recentMessages
        var conversation = await _conversationsCollection.Find(filter).FirstOrDefaultAsync();
        if (conversation == null)
        {
            _logger.LogWarning("Conversation {ConversationId} not found", conversationId);
            return;
        }

        // Add the new message to the beginning of recentMessages and keep only the 50 most recent
        var recentMessages = new List<string> { messageId };
        recentMessages.AddRange(conversation.RecentMessages.Take(49));

        var update = Builders<Conversation>.Update
            .Set(c => c.RecentMessages, recentMessages)
            .Set(c => c.LastMessageAt, DateTime.UtcNow);

        var result = await _conversationsCollection.UpdateOneAsync(filter, update);
        
        if (result.ModifiedCount > 0)
        {
            _logger.LogDebug("Updated recentMessages for conversation {ConversationId}", conversationId);
        }
        else
        {
            _logger.LogWarning("Failed to update recentMessages for conversation {ConversationId}", conversationId);
        }
    }

    /// <summary>
    /// Broadcasts the message to conversation participants via SignalR
    /// </summary>
    /// <param name="message">The message to broadcast</param>
    /// <param name="senderName">The sender's display name</param>
    private async Task BroadcastMessageAsync(Message message, string senderName)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("SignalR connection is not active, cannot broadcast message {MessageId}", message.Id);
            return;
        }

        try
        {
            // Get the conversation to find participants
            var conversation = await _conversationsCollection
                .Find(c => c.Id == message.ConversationId)
                .FirstOrDefaultAsync();

            if (conversation == null)
            {
                _logger.LogWarning("Conversation {ConversationId} not found for message broadcast", message.ConversationId);
                return;
            }

            // Broadcast to all participants in the conversation group
            await _hubConnection.InvokeAsync("SendMessage",
                message.ConversationId,
                message.Id,
                message.Content);

            _logger.LogDebug("Broadcasted message {MessageId} to conversation {ConversationId} participants", 
                message.Id, message.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast message {MessageId} via SignalR", message.Id);
            // Don't throw - the message is already persisted, so this is not a critical failure
        }
    }

    /// <summary>
    /// Sends a failed message to the dead-letter queue
    /// </summary>
    /// <param name="messageJson">The original message JSON</param>
    /// <param name="reason">The reason for failure</param>
    private async Task SendToDeadLetterQueueAsync(string messageJson, string reason)
    {
        try
        {
            var deadLetterPayload = new
            {
                OriginalMessage = messageJson,
                Reason = reason,
                FailedAt = DateTime.UtcNow
            };

            var deadLetterJson = JsonSerializer.Serialize(deadLetterPayload);
            await _messagePublisher.PublishAsync(_configuration.DeadLetterQueueName, deadLetterJson);
            
            _logger.LogWarning("Sent message to dead-letter queue: {Reason}", reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to dead-letter queue");
        }
    }

    /// <summary>
    /// Disposes the worker resources
    /// </summary>
    public override void Dispose()
    {
        _messageConsumer.StopConsuming();
        
        if (_hubConnection != null)
        {
            _hubConnection.StopAsync().GetAwaiter().GetResult();
            _hubConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose();
    }
}
