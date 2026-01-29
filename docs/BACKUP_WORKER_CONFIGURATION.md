# BackupWorker Configuration and Usage

This document describes how to configure and use the BackupWorker for automated MongoDB backups in TraliVali.

## Overview

The `BackupWorker` is a background service that automatically backs up MongoDB collections to Azure Blob Storage. It runs on a configurable schedule (default: daily at 3 AM) and handles:

- **Collection Export**: Exports MongoDB collections to BSON format
- **Compression**: Compresses backups using gzip for optimal storage
- **Upload**: Uploads compressed backups to Azure Blob Storage
- **Retention**: Automatically cleans up backups older than the configured retention period

## Collections Backed Up

The BackupWorker backs up the following MongoDB collections:

1. `users` - User accounts and profiles
2. `conversations` - Conversation metadata
3. `messages` - Chat messages
4. `invites` - Invitation tokens
5. `files` - File metadata

## Backup Path Structure

Backups are stored using the following path structure:

```
backups/{date}/tralivali_{collection}.bson.gz
```

**Examples:**
```
backups/2024-01-15/tralivali_users.bson.gz
backups/2024-01-15/tralivali_conversations.bson.gz
backups/2024-01-15/tralivali_messages.bson.gz
backups/2024-01-15/tralivali_invites.bson.gz
backups/2024-01-15/tralivali_files.bson.gz
```

This structure allows for:
- Easy organization by date
- Simple restoration of entire day's backup
- Efficient cleanup of old backups
- Clear identification of backup contents

## Configuration

### appsettings.json

Add the following configuration to your `appsettings.json`:

```json
{
  "BackupWorker": {
    "CronSchedule": "0 3 * * *",
    "BlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
    "BlobContainerName": "backups",
    "RetentionDays": 30,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerTimeoutSeconds": 30
  }
}
```

### Environment Variables

Alternatively, you can use environment variables:

```bash
BACKUP_CRON_SCHEDULE="0 3 * * *"
BACKUP_BLOB_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=..."
BACKUP_BLOB_CONTAINER_NAME="backups"
BACKUP_RETENTION_DAYS=30
BACKUP_CIRCUIT_BREAKER_FAILURE_THRESHOLD=5
BACKUP_CIRCUIT_BREAKER_TIMEOUT_SECONDS=30
```

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `CronSchedule` | `"0 3 * * *"` | Cron schedule for backup runs (default: daily at 3 AM UTC) |
| `BlobStorageConnectionString` | `""` | Azure Blob Storage connection string |
| `BlobContainerName` | `"backups"` | Container name for storing backups |
| `RetentionDays` | `30` | Number of days to retain backups before automatic deletion |
| `CircuitBreakerFailureThreshold` | `5` | Number of failures before circuit breaker opens |
| `CircuitBreakerTimeoutSeconds` | `30` | Duration circuit breaker stays open after failure |

### Cron Schedule Examples

- `"0 3 * * *"` - Daily at 3:00 AM UTC
- `"0 0 * * *"` - Daily at midnight UTC
- `"0 2 * * 0"` - Weekly on Sunday at 2:00 AM UTC
- `"0 4 1 * *"` - Monthly on the 1st at 4:00 AM UTC

## Service Registration

Register the BackupWorker in your `Program.cs` or `Startup.cs`:

```csharp
// Configure BackupWorker
var backupConfig = new BackupWorkerConfiguration
{
    CronSchedule = builder.Configuration.GetValue<string>("BackupWorker:CronSchedule") ?? "0 3 * * *",
    BlobStorageConnectionString = builder.Configuration.GetValue<string>("BackupWorker:BlobStorageConnectionString") 
        ?? Environment.GetEnvironmentVariable("BACKUP_BLOB_STORAGE_CONNECTION_STRING") ?? "",
    BlobContainerName = builder.Configuration.GetValue<string>("BackupWorker:BlobContainerName") ?? "backups",
    RetentionDays = builder.Configuration.GetValue<int>("BackupWorker:RetentionDays", 30),
    CircuitBreakerFailureThreshold = builder.Configuration.GetValue<int>("BackupWorker:CircuitBreakerFailureThreshold", 5),
    CircuitBreakerTimeoutSeconds = builder.Configuration.GetValue<int>("BackupWorker:CircuitBreakerTimeoutSeconds", 30)
};

builder.Services.AddSingleton(backupConfig);

// Register BackupWorker as a hosted service
builder.Services.AddHostedService<BackupWorker>(sp =>
{
    var dbContext = sp.GetRequiredService<MongoDbContext>();
    var config = sp.GetRequiredService<BackupWorkerConfiguration>();
    var logger = sp.GetRequiredService<ILogger<BackupWorker>>();
    
    // Get the underlying IMongoDatabase from the MongoDbContext
    var database = dbContext.Users.Database;
    
    return new BackupWorker(database, config, logger);
});
```

## Backup Process

The BackupWorker performs the following steps during each backup run:

1. **Schedule**: Waits until the next scheduled run time based on the cron expression
2. **Export**: For each collection:
   - Retrieves all documents from MongoDB
   - Serializes to BSON format
   - Logs document count and size
3. **Compress**: Compresses BSON data using gzip (typically 60-80% compression)
4. **Upload**: Uploads to Azure Blob Storage with circuit breaker protection
5. **Cleanup**: Removes backups older than the retention period
6. **Logging**: Records success/failure metrics and timing information

## Monitoring

The BackupWorker provides detailed logging at various levels:

- **Information**: Startup, schedule, backup completion, metrics
- **Warning**: Configuration issues, cleanup failures
- **Error**: Backup failures, circuit breaker events

### Log Examples

```
BackupWorker starting with cron schedule: 0 3 * * *
Next backup run scheduled for 2024-01-16 03:00:00 UTC (8h 45m from now)
Starting backup process at 2024-01-16 03:00:00
Backing up collection: users
Exported 1523 documents from users, BSON size: 524288 bytes
Compressed users: 104857 bytes (compression: 80.0%)
Uploaded backup to: backups/2024-01-16/tralivali_users.bson.gz
Backup process completed in 12.34 seconds. Success: 5, Failed: 0
Cleaned up 2 old backup(s)
```

## Retention Policy

The BackupWorker automatically deletes backups older than the configured `RetentionDays`:

- Default retention: 30 days
- Cleanup runs before each backup operation
- Only affects backups in the `backups/` prefix
- Deletion is logged for audit purposes

### Manual Retention Configuration

You can also configure lifecycle management policies in Azure Storage Account for additional cost optimization:

```json
{
  "rules": [
    {
      "enabled": true,
      "name": "delete-old-backups",
      "type": "Lifecycle",
      "definition": {
        "actions": {
          "baseBlob": {
            "delete": {
              "daysAfterModificationGreaterThan": 30
            }
          }
        },
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["backups/"]
        }
      }
    }
  ]
}
```

## Restoring from Backup

To restore a backup:

1. **Download** the backup from Azure Blob Storage:
   ```bash
   az storage blob download \
     --account-name <account-name> \
     --container-name backups \
     --name backups/2024-01-16/tralivali_users.bson.gz \
     --file users.bson.gz
   ```

2. **Decompress** the backup:
   ```bash
   gunzip users.bson.gz
   ```

3. **Restore** to MongoDB using `mongorestore`:
   ```bash
   mongorestore --archive=users.bson --nsInclude=tralivali.users
   ```

## Circuit Breaker

The BackupWorker uses a circuit breaker pattern to handle Azure Blob Storage failures:

- **Threshold**: After 5 consecutive failures (configurable)
- **Timeout**: Circuit stays open for 30 seconds (configurable)
- **Recovery**: Automatically attempts to close after timeout
- **Protection**: Prevents cascading failures and excessive retries

### Circuit Breaker States

1. **Closed**: Normal operation, requests pass through
2. **Open**: Failures exceeded threshold, requests fail immediately
3. **Half-Open**: Testing if service recovered, single request allowed

## Error Handling

The BackupWorker handles various error scenarios:

- **Missing Configuration**: Logs error and exits if connection string not provided
- **MongoDB Errors**: Logs collection-level failures, continues with remaining collections
- **Azure Blob Errors**: Uses circuit breaker, retries failed uploads
- **Network Issues**: Circuit breaker provides automatic retry with backoff
- **Cancellation**: Gracefully stops when application shuts down

## Testing

Run the BackupWorker tests:

```bash
dotnet test --filter "FullyQualifiedName~BackupWorkerTests"
```

All 15 tests cover:
- Constructor validation
- Configuration validation
- Cron schedule parsing
- Retention period settings
- Circuit breaker configuration

## Performance Considerations

- **Collection Size**: Large collections may take several minutes to backup
- **Compression**: Gzip compression is CPU-intensive but saves storage
- **Network**: Upload speed depends on bandwidth to Azure
- **Memory**: Backups are held in memory during compression
- **Concurrent Access**: Worker doesn't impact MongoDB read/write performance

### Typical Backup Times

| Collection Size | Documents | Backup Time |
|----------------|-----------|-------------|
| 100 MB | 10K | ~30 seconds |
| 1 GB | 100K | ~3 minutes |
| 10 GB | 1M | ~30 minutes |

## Security Considerations

1. **Connection Strings**: Store in Azure Key Vault or secure environment variables
2. **Access Control**: Use Azure RBAC for blob storage access
3. **Encryption**: Backups are encrypted at rest in Azure Storage (by default)
4. **Audit Logs**: Monitor backup operations via application logs
5. **Network Security**: Use Azure Private Link for blob storage access
6. **Data Retention**: Comply with data protection regulations (GDPR, etc.)

## Cost Optimization

Backup storage costs can be optimized:

- **Compression**: Gzip reduces storage by 60-80%
- **Retention**: 30-day retention minimizes storage costs
- **Storage Tier**: Consider Cool or Archive tier for older backups
- **Monitoring**: Track storage usage in Azure Cost Management

### Estimated Costs

For a 10 GB database with 30-day retention:

- Daily backup size (compressed): ~2-4 GB
- Monthly storage: ~60-120 GB
- Cost (Hot tier): ~$1-2/month
- Cost (Cool tier): ~$0.50-1/month

## Troubleshooting

### Worker Not Starting

Check logs for:
- Missing blob storage connection string
- Invalid cron schedule
- MongoDB connection issues

### Backups Not Appearing

Verify:
- Azure Storage Account credentials
- Container exists and is accessible
- No network connectivity issues
- Circuit breaker not in open state

### Large Backup Times

Consider:
- Reducing backup frequency
- Implementing incremental backups (future enhancement)
- Increasing Azure bandwidth allocation
- Splitting large collections

## Future Enhancements

Potential improvements for the BackupWorker:

- Incremental backups (only changed documents)
- Parallel collection exports
- Backup verification and integrity checks
- Point-in-time recovery support
- Email notifications on backup success/failure
- Metrics and monitoring integration (Prometheus, etc.)
- Support for additional export formats (JSON, CSV)
