# Task 022: Create Backup Worker

**Phase**: Phase 2 - Infrastructure and Services

## Description

Create a BackupWorker that runs daily at 3 AM to back up MongoDB collections to Azure Blob Storage. The worker exports collections to BSON format, compresses them with gzip, and uploads to a structured path in Azure Blob Storage with automatic 30-day retention.

## Implementation Details

### BackupWorker Features
- **Scheduled Execution**: Runs daily at 3 AM using cron scheduling (Cronos library)
- **Collection Export**: Exports users, conversations, messages, invites, and files collections
- **BSON Format**: Uses MongoDB BSON serialization for efficient storage
- **Compression**: Gzip compression (typically 60-80% size reduction)
- **Azure Upload**: Uploads to `backups/{date}/tralivali_{collection}.bson.gz`
- **Retention**: Automatically deletes backups older than 30 days
- **Circuit Breaker**: Polly circuit breaker for Azure Blob Storage resilience
- **Logging**: Comprehensive logging with metrics and performance data

### Files Created
- `src/TraliVali.Workers/BackupWorker.cs` - Main worker implementation
- `src/TraliVali.Workers/BackupWorkerConfiguration.cs` - Configuration class
- `tests/TraliVali.Tests/Workers/BackupWorkerTests.cs` - Unit tests (15 tests)
- `docs/BACKUP_WORKER_CONFIGURATION.md` - Comprehensive documentation

### Configuration
```json
{
  "BackupWorker": {
    "CronSchedule": "0 3 * * *",
    "BlobStorageConnectionString": "...",
    "BlobContainerName": "backups",
    "RetentionDays": 30,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerTimeoutSeconds": 30
  }
}
```

## Acceptance Criteria

- [x] Worker runs daily at 3 AM (configurable via cron schedule)
- [x] All collections exported to BSON format
- [x] Files compressed with gzip
- [x] Uploaded to correct path: `backups/{date}/tralivali_{collection}.bson.gz`
- [x] 30-day retention configured (automatic cleanup)

## Testing

All 15 unit tests pass:
```bash
dotnet test --filter "FullyQualifiedName~BackupWorkerTests"
```

Tests cover:
- Constructor validation
- Configuration defaults and custom values
- Cron schedule parsing
- Retention period validation
- Circuit breaker settings

## Documentation

See [BACKUP_WORKER_CONFIGURATION.md](../BACKUP_WORKER_CONFIGURATION.md) for:
- Configuration guide
- Service registration
- Monitoring and logging
- Backup restoration procedures
- Performance considerations
- Security best practices

## Related Tasks

See [PROJECT_ROADMAP.md](../PROJECT_ROADMAP.md) for dependencies and related tasks.

## Notes

- Worker requires Azure Blob Storage connection string to be configured
- Service registration needs to be added in Program.cs/Startup.cs for deployment
- Backups are stored in BSON format for efficient MongoDB restoration
- Gzip compression provides significant storage savings
- Circuit breaker pattern protects against Azure service failures

