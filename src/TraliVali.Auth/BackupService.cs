using System.IO.Compression;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TraliVali.Domain.Entities;

namespace TraliVali.Auth;

/// <summary>
/// Service for managing database backups
/// </summary>
public class BackupService : IBackupService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Backup> _backupsCollection;
    private readonly BlobContainerClient? _containerClient;
    private readonly ILogger<BackupService> _logger;
    private readonly string _blobContainerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class
    /// </summary>
    /// <param name="database">The MongoDB database instance</param>
    /// <param name="backupsCollection">The backups collection</param>
    /// <param name="blobConnectionString">Azure Blob Storage connection string</param>
    /// <param name="blobContainerName">The blob container name</param>
    /// <param name="logger">The logger instance</param>
    public BackupService(
        IMongoDatabase database,
        IMongoCollection<Backup> backupsCollection,
        string? blobConnectionString,
        string blobContainerName,
        ILogger<BackupService> logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _backupsCollection = backupsCollection ?? throw new ArgumentNullException(nameof(backupsCollection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blobContainerName = blobContainerName ?? "tralivali-backups";

        if (!string.IsNullOrEmpty(blobConnectionString))
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(blobConnectionString);
                _containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize blob container client");
            }
        }
    }

    /// <inheritdoc/>
    public async Task<Backup> TriggerBackupAsync(CancellationToken cancellationToken = default)
    {
        var backup = new Backup
        {
            CreatedAt = DateTime.UtcNow,
            Type = "manual",
            Status = BackupStatus.InProgress
        };

        try
        {
            if (_containerClient == null)
            {
                throw new InvalidOperationException("Backup storage is not configured");
            }

            await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var backupDate = backup.CreatedAt.ToString("yyyy-MM-dd");
            var collections = new[] { "users", "conversations", "messages", "invites", "files" };
            long totalSize = 0;

            foreach (var collectionName in collections)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var size = await BackupCollectionAsync(collectionName, backupDate, cancellationToken);
                totalSize += size;
            }

            backup.Size = totalSize;
            backup.FilePath = $"backups/{backupDate}";
            backup.Status = BackupStatus.Completed;
            
            _logger.LogInformation("Backup completed successfully: {FilePath}, Size: {Size} bytes", backup.FilePath, backup.Size);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed");
            backup.Status = BackupStatus.Failed;
            backup.ErrorMessage = ex.Message;
        }

        await _backupsCollection.InsertOneAsync(backup, cancellationToken: cancellationToken);
        return backup;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Backup>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backups = await _backupsCollection
            .Find(_ => true)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
        
        return backups;
    }

    /// <inheritdoc/>
    public async Task<bool> RestoreBackupAsync(string date, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_containerClient == null)
            {
                throw new InvalidOperationException("Backup storage is not configured");
            }

            if (!DateTime.TryParse(date, out var backupDate))
            {
                throw new ArgumentException($"Invalid date format: {date}", nameof(date));
            }

            var backupDateStr = backupDate.ToString("yyyy-MM-dd");
            _logger.LogInformation("Starting restore from backup date: {BackupDate}", backupDateStr);

            var collections = new[] { "users", "conversations", "messages", "invites", "files" };

            foreach (var collectionName in collections)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                await RestoreCollectionAsync(collectionName, backupDateStr, cancellationToken);
            }

            _logger.LogInformation("Restore completed successfully from backup date: {BackupDate}", backupDateStr);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed for date: {Date}", date);
            return false;
        }
    }

    /// <summary>
    /// Backs up a single collection
    /// </summary>
    /// <param name="collectionName">The collection name</param>
    /// <param name="backupDate">The backup date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The size of the backup in bytes</returns>
    private async Task<long> BackupCollectionAsync(string collectionName, string backupDate, CancellationToken cancellationToken)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Blob container client is not initialized");
        }

        _logger.LogInformation("Backing up collection: {CollectionName}", collectionName);

        var collection = _database.GetCollection<BsonDocument>(collectionName);

        using var bsonStream = new MemoryStream();
        using var writer = new BsonBinaryWriter(bsonStream);
        
        var cursor = await collection.FindAsync(new BsonDocument(), cancellationToken: cancellationToken);
        var documentCount = 0;
        
        await foreach (var document in cursor.ToAsyncEnumerable())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var context = BsonSerializationContext.CreateRoot(writer);
            BsonSerializer.Serialize(context.Writer, document);
            documentCount++;
        }

        var bsonSize = bsonStream.Length;
        _logger.LogInformation("Exported {DocumentCount} documents from {CollectionName}", documentCount, collectionName);

        bsonStream.Position = 0;
        using var compressedStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            await bsonStream.CopyToAsync(gzipStream, cancellationToken);
        }

        var compressedSize = compressedStream.Length;
        _logger.LogInformation("Compressed {CollectionName}: {CompressedSize} bytes", collectionName, compressedSize);

        var blobPath = $"backups/{backupDate}/tralivali_{collectionName}.bson.gz";
        compressedStream.Position = 0;

        var blobClient = _containerClient.GetBlobClient(blobPath);
        await blobClient.UploadAsync(compressedStream, overwrite: true, cancellationToken);
        
        _logger.LogInformation("Uploaded backup to: {BlobPath}", blobPath);

        return compressedSize;
    }

    /// <summary>
    /// Restores a single collection
    /// </summary>
    /// <param name="collectionName">The collection name</param>
    /// <param name="backupDate">The backup date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task RestoreCollectionAsync(string collectionName, string backupDate, CancellationToken cancellationToken)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Blob container client is not initialized");
        }

        _logger.LogInformation("Restoring collection: {CollectionName}", collectionName);

        var blobPath = $"backups/{backupDate}/tralivali_{collectionName}.bson.gz";
        var blobClient = _containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Backup file not found: {BlobPath}", blobPath);
            return;
        }

        using var compressedStream = new MemoryStream();
        await blobClient.DownloadToAsync(compressedStream, cancellationToken);
        compressedStream.Position = 0;

        using var bsonStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        {
            await gzipStream.CopyToAsync(bsonStream, cancellationToken);
        }

        bsonStream.Position = 0;

        var collection = _database.GetCollection<BsonDocument>(collectionName);
        
        // Clear existing data
        await collection.DeleteManyAsync(new BsonDocument(), cancellationToken);

        // Restore documents
        using var reader = new BsonBinaryReader(bsonStream);
        var documents = new List<BsonDocument>();
        
        while (bsonStream.Position < bsonStream.Length)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
                documents.Add(document);
            }
            catch (Exception ex)
            {
                // End of stream or deserialization error
                _logger.LogDebug(ex, "Finished reading documents from backup or encountered deserialization issue");
                break;
            }
        }

        if (documents.Count > 0)
        {
            await collection.InsertManyAsync(documents, cancellationToken: cancellationToken);
            _logger.LogInformation("Restored {DocumentCount} documents to {CollectionName}", documents.Count, collectionName);
        }
    }
}
