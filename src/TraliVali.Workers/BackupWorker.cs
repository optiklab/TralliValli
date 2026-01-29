using System.IO.Compression;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;
using TraliVali.Domain.Entities;

namespace TraliVali.Workers;

/// <summary>
/// Background worker that backs up MongoDB collections to Azure Blob Storage
/// </summary>
public class BackupWorker : BackgroundService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<BackupWorker> _logger;
    private readonly BackupWorkerConfiguration _configuration;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private BlobContainerClient? _containerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupWorker"/> class
    /// </summary>
    /// <param name="database">The MongoDB database instance</param>
    /// <param name="configuration">The worker configuration</param>
    /// <param name="logger">The logger instance</param>
    public BackupWorker(
        IMongoDatabase database,
        BackupWorkerConfiguration configuration,
        ILogger<BackupWorker> logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
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
        _logger.LogInformation("BackupWorker starting with cron schedule: {CronSchedule}", _configuration.CronSchedule);

        // Initialize Azure Blob Storage container
        if (!string.IsNullOrEmpty(_configuration.BlobStorageConnectionString))
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_configuration.BlobStorageConnectionString);
                _containerClient = blobServiceClient.GetBlobContainerClient(_configuration.BlobContainerName);
                await _containerClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
                _logger.LogInformation("Azure Blob Storage container '{ContainerName}' initialized", _configuration.BlobContainerName);

                // Configure lifecycle management policy for 30-day retention
                await ConfigureLifecycleManagementAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Blob Storage. Backup worker will exit");
                return;
            }
        }
        else
        {
            _logger.LogError("Azure Blob Storage connection string not configured. Backup worker will exit");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRun = GetNextRunTime();
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next backup run scheduled for {NextRun} UTC ({Delay} from now)",
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss"), delay);
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await PerformBackupAsync(stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BackupWorker stopping due to cancellation request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in BackupWorker");
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
    /// Configures lifecycle management policy for automatic retention
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ConfigureLifecycleManagementAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Note: Lifecycle management policies are typically configured at the storage account level
            // This method logs information about the retention policy that should be configured
            _logger.LogInformation(
                "Backup retention policy: Backups older than {RetentionDays} days should be automatically deleted. " +
                "Configure lifecycle management rules in Azure Storage Account to enforce this policy.",
                _configuration.RetentionDays);

            // Clean up old backups programmatically
            await CleanupOldBackupsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to configure lifecycle management. Old backups will not be automatically deleted");
        }
    }

    /// <summary>
    /// Cleans up old backups beyond the retention period
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task CleanupOldBackupsAsync(CancellationToken cancellationToken)
    {
        if (_containerClient == null)
        {
            return;
        }

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_configuration.RetentionDays);
            _logger.LogInformation("Cleaning up backups older than {CutoffDate}", cutoffDate.ToString("yyyy-MM-dd"));

            var deletedCount = 0;
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: "backups/", cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Parse date from blob path (format: backups/yyyy-MM-dd/...)
                var pathParts = blobItem.Name.Split('/');
                if (pathParts.Length >= 2 && DateTime.TryParse(pathParts[1], out var blobDate))
                {
                    if (blobDate < cutoffDate)
                    {
                        try
                        {
                            var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                            deletedCount++;
                            _logger.LogDebug("Deleted old backup: {BlobName}", blobItem.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old backup: {BlobName}", blobItem.Name);
                        }
                    }
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {DeletedCount} old backup(s)", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backups");
        }
    }

    /// <summary>
    /// Performs backup of all MongoDB collections
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task PerformBackupAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting backup process at {StartTime}", startTime.ToString("yyyy-MM-dd HH:mm:ss"));

        var collections = new[] { "users", "conversations", "messages", "invites", "files" };
        var backupDate = startTime.ToString("yyyy-MM-dd");
        var successCount = 0;
        var failedCount = 0;

        foreach (var collectionName in collections)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Backup process cancelled");
                break;
            }

            try
            {
                await BackupCollectionAsync(collectionName, backupDate, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup collection {CollectionName}", collectionName);
                failedCount++;
            }
        }

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Backup process completed in {Duration:F2} seconds. Success: {SuccessCount}, Failed: {FailedCount}",
            duration.TotalSeconds, successCount, failedCount);
    }

    /// <summary>
    /// Backs up a single MongoDB collection to Azure Blob Storage
    /// </summary>
    /// <param name="collectionName">The name of the collection to backup</param>
    /// <param name="backupDate">The backup date in yyyy-MM-dd format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task BackupCollectionAsync(string collectionName, string backupDate, CancellationToken cancellationToken)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Blob container client is not initialized");
        }

        _logger.LogInformation("Backing up collection: {CollectionName}", collectionName);

        // Get the collection from MongoDB
        var collection = _database.GetCollection<BsonDocument>(collectionName);

        // Export collection to BSON format
        var documentCount = 0;
        using var bsonStream = new MemoryStream();
        
        // Use BsonBinaryWriter to write documents
        var writer = new BsonBinaryWriter(bsonStream);
        try
        {
            var cursor = await collection.FindAsync(new BsonDocument(), cancellationToken: cancellationToken);
            await foreach (var document in cursor.ToAsyncEnumerable())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Write each document as BSON
                var context = BsonSerializationContext.CreateRoot(writer);
                BsonSerializer.Serialize(context.Writer, document);
                documentCount++;
            }
        }
        finally
        {
            writer.Dispose();
        }

        var bsonSize = bsonStream.Length;
        _logger.LogInformation("Exported {DocumentCount} documents from {CollectionName}, BSON size: {Size} bytes",
            documentCount, collectionName, bsonSize);

        // Compress with gzip
        bsonStream.Position = 0;
        using var compressedStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            await bsonStream.CopyToAsync(gzipStream, cancellationToken);
        }

        var compressedSize = compressedStream.Length;
        var compressionRatio = bsonSize > 0 ? (1 - (double)compressedSize / bsonSize) * 100 : 0;
        _logger.LogInformation("Compressed {CollectionName}: {CompressedSize} bytes (compression: {CompressionRatio:F1}%)",
            collectionName, compressedSize, compressionRatio);

        // Upload to Azure Blob Storage using circuit breaker
        var blobPath = $"backups/{backupDate}/tralivali_{collectionName}.bson.gz";
        compressedStream.Position = 0;

        await _circuitBreaker.ExecuteAsync(async () =>
        {
            var blobClient = _containerClient.GetBlobClient(blobPath);
            await blobClient.UploadAsync(compressedStream, overwrite: true, cancellationToken);
            _logger.LogInformation("Uploaded backup to: {BlobPath}", blobPath);
        });
    }
}
