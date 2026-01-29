using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;
using TraliVali.Domain.Entities;

namespace TraliVali.Workers;

/// <summary>
/// Background worker that archives old messages to Azure Blob Storage
/// </summary>
public class ArchivalWorker : BackgroundService
{
    private readonly IMongoCollection<Message> _messagesCollection;
    private readonly IMongoCollection<Conversation> _conversationsCollection;
    private readonly ILogger<ArchivalWorker> _logger;
    private readonly ArchivalWorkerConfiguration _configuration;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private BlobContainerClient? _containerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchivalWorker"/> class
    /// </summary>
    /// <param name="messagesCollection">The messages collection</param>
    /// <param name="conversationsCollection">The conversations collection</param>
    /// <param name="configuration">The worker configuration</param>
    /// <param name="logger">The logger instance</param>
    public ArchivalWorker(
        IMongoCollection<Message> messagesCollection,
        IMongoCollection<Conversation> conversationsCollection,
        ArchivalWorkerConfiguration configuration,
        ILogger<ArchivalWorker> logger)
    {
        _messagesCollection = messagesCollection ?? throw new ArgumentNullException(nameof(messagesCollection));
        _conversationsCollection = conversationsCollection ?? throw new ArgumentNullException(nameof(conversationsCollection));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize circuit breaker for Azure Blob operations
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: _configuration.CircuitBreakerFailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(_configuration.CircuitBreakerTimeoutSeconds),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(exception, "Circuit breaker opened. Blob operations will be blocked for {Duration} seconds", duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset. Blob operations resumed");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open. Testing Blob operations");
                });
    }

    /// <summary>
    /// Executes the worker
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ArchivalWorker starting with cron schedule: {CronSchedule}", _configuration.CronSchedule);

        // Initialize Azure Blob Storage container
        if (!string.IsNullOrEmpty(_configuration.BlobStorageConnectionString))
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_configuration.BlobStorageConnectionString);
                _containerClient = blobServiceClient.GetBlobContainerClient(_configuration.BlobContainerName);
                await _containerClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
                _logger.LogInformation("Azure Blob Storage container '{ContainerName}' initialized", _configuration.BlobContainerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Blob Storage. Archival worker will continue but archiving will fail");
            }
        }
        else
        {
            _logger.LogWarning("Azure Blob Storage connection string not configured. Messages will not be archived");
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRun = GetNextRunTime();
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next archival run scheduled for {NextRun} UTC ({Delay} from now)",
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss"), delay);
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ArchiveOldMessagesAsync(stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ArchivalWorker stopping due to cancellation request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ArchivalWorker");
            throw;
        }
    }

    /// <summary>
    /// Gets the next run time based on the cron schedule
    /// </summary>
    /// <returns>The next run time</returns>
    private DateTime GetNextRunTime()
    {
        try
        {
            var cronExpression = CronExpression.Parse(_configuration.CronSchedule);
            var nextOccurrence = cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
            return nextOccurrence ?? DateTime.UtcNow.AddDays(1); // Fallback to 1 day if parsing fails
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse cron schedule '{CronSchedule}'. Defaulting to 1 day", _configuration.CronSchedule);
            return DateTime.UtcNow.AddDays(1);
        }
    }

    /// <summary>
    /// Archives old messages to Azure Blob Storage
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ArchiveOldMessagesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting archival process for messages older than {RetentionDays} days", _configuration.RetentionDays);

        // Validate blob storage is configured if DeleteAfterArchive is enabled
        if (_configuration.DeleteAfterArchive && _containerClient == null)
        {
            _logger.LogError("DeleteAfterArchive is enabled but blob storage is not configured. Skipping archival to prevent data loss");
            return;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-_configuration.RetentionDays);
        var totalArchived = 0;
        var totalDeleted = 0;
        var totalFailed = 0;
        var affectedConversations = new HashSet<string>();

        try
        {
            // Build filter for messages older than retention days
            var filter = Builders<Message>.Filter.Lt(m => m.CreatedAt, cutoffDate);

            // Process messages in batches
            var hasMore = true;
            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                // Fetch batch of messages
                var messages = await _messagesCollection
                    .Find(filter)
                    .Limit(_configuration.BatchSize)
                    .ToListAsync(cancellationToken);

                hasMore = messages.Count == _configuration.BatchSize;

                if (messages.Count == 0)
                {
                    _logger.LogInformation("No more messages to archive");
                    break;
                }

                _logger.LogInformation("Processing batch of {Count} messages", messages.Count);

                // Archive each message
                foreach (var message in messages)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Archival process cancelled");
                        break;
                    }

                    try
                    {
                        // Archive to Azure Blob Storage
                        if (_containerClient != null)
                        {
                            await ArchiveMessageToBlobAsync(message, cancellationToken);
                        }

                        // Delete from MongoDB if configured
                        if (_configuration.DeleteAfterArchive)
                        {
                            var deleteResult = await _messagesCollection.DeleteOneAsync(
                                Builders<Message>.Filter.Eq(m => m.Id, message.Id),
                                cancellationToken);

                            if (deleteResult.DeletedCount > 0)
                            {
                                totalDeleted++;
                                affectedConversations.Add(message.ConversationId);
                            }
                        }

                        totalArchived++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to archive message {MessageId}", message.Id);
                        totalFailed++;
                    }
                }

                _logger.LogInformation("Batch processed. Archived: {Archived}, Deleted: {Deleted}, Failed: {Failed}",
                    totalArchived, totalDeleted, totalFailed);
            }

            // Update recentMessages array for affected conversations
            if (_configuration.DeleteAfterArchive && affectedConversations.Count > 0)
            {
                await UpdateConversationRecentMessagesAsync(affectedConversations, cancellationToken);
            }

            _logger.LogInformation("Archival process completed. Total archived: {TotalArchived}, Total deleted: {TotalDeleted}, Total failed: {TotalFailed}, Affected conversations: {AffectedConversations}",
                totalArchived, totalDeleted, totalFailed, affectedConversations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during archival process");
        }
    }

    /// <summary>
    /// Archives a message to Azure Blob Storage using circuit breaker pattern
    /// </summary>
    /// <param name="message">The message to archive</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ArchiveMessageToBlobAsync(Message message, CancellationToken cancellationToken)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Blob container client is not initialized");
        }

        await _circuitBreaker.ExecuteAsync(async () =>
        {
            // Create blob name using year/month/day structure for better organization
            var blobName = $"{message.CreatedAt:yyyy}/{message.CreatedAt:MM}/{message.CreatedAt:dd}/{message.Id}.json";
            var blobClient = _containerClient.GetBlobClient(blobName);

            // Serialize message to JSON
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(json);

            // Upload to blob storage
            using var stream = new MemoryStream(bytes);
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

            _logger.LogDebug("Message {MessageId} archived to blob {BlobName}", message.Id, blobName);
        });
    }

    /// <summary>
    /// Updates recentMessages arrays for affected conversations after message deletion
    /// </summary>
    /// <param name="conversationIds">Set of conversation IDs that had messages deleted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task UpdateConversationRecentMessagesAsync(HashSet<string> conversationIds, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating recentMessages array for {Count} affected conversations", conversationIds.Count);
        var updatedCount = 0;

        foreach (var conversationId in conversationIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Conversation update cancelled");
                break;
            }

            try
            {
                // Get the conversation
                var conversationFilter = Builders<Conversation>.Filter.Eq(c => c.Id, conversationId);
                var conversation = await _conversationsCollection
                    .Find(conversationFilter)
                    .FirstOrDefaultAsync(cancellationToken);

                if (conversation == null || conversation.RecentMessages.Count == 0)
                {
                    continue;
                }

                // Batch check which message IDs in recentMessages still exist
                var messageFilter = Builders<Message>.Filter.In(m => m.Id, conversation.RecentMessages);
                var existingMessages = await _messagesCollection
                    .Find(messageFilter)
                    .Project(m => m.Id)
                    .ToListAsync(cancellationToken);

                var existingMessageIds = existingMessages.ToHashSet();
                var filteredRecentMessages = conversation.RecentMessages
                    .Where(id => existingMessageIds.Contains(id))
                    .ToList();

                // Update conversation if any messages were removed
                if (filteredRecentMessages.Count != conversation.RecentMessages.Count)
                {
                    var update = Builders<Conversation>.Update
                        .Set(c => c.RecentMessages, filteredRecentMessages);

                    var updateResult = await _conversationsCollection
                        .UpdateOneAsync(conversationFilter, update, cancellationToken: cancellationToken);

                    if (updateResult.IsAcknowledged && updateResult.ModifiedCount > 0)
                    {
                        updatedCount++;
                        var removedCount = conversation.RecentMessages.Count - filteredRecentMessages.Count;
                        _logger.LogDebug("Updated conversation {ConversationId}: removed {RemovedCount} archived message(s) from recentMessages",
                            conversationId, removedCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update recentMessages for conversation {ConversationId}", conversationId);
            }
        }

        _logger.LogInformation("Updated recentMessages array for {UpdatedCount} conversations", updatedCount);
    }
}
