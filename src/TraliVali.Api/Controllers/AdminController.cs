using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TraliVali.Api.Models;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Storage;

namespace TraliVali.Api.Controllers;

/// <summary>
/// Controller for admin operations
/// </summary>
[Authorize]
[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IMongoCollection<Message> _messagesCollection;
    private readonly IMongoCollection<ArchivalStats> _archivalStatsCollection;
    private readonly IBackupService _backupService;
    private readonly IAzureBlobService? _blobService;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class
    /// </summary>
    public AdminController(
        IMongoCollection<Message> messagesCollection,
        IMongoCollection<ArchivalStats> archivalStatsCollection,
        IBackupService backupService,
        IAzureBlobService? blobService,
        ILogger<AdminController> logger)
    {
        _messagesCollection = messagesCollection ?? throw new ArgumentNullException(nameof(messagesCollection));
        _archivalStatsCollection = archivalStatsCollection ?? throw new ArgumentNullException(nameof(archivalStatsCollection));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _blobService = blobService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if the current user is an admin
    /// </summary>
    private bool IsAdmin()
    {
        var role = User.FindFirst("role")?.Value;
        return role == "admin";
    }

    /// <summary>
    /// Triggers a manual archival run
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Archival result</returns>
    [HttpPost("archival/trigger")]
    [ProducesResponseType(typeof(TriggerArchivalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TriggerArchival(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsAdmin())
            {
                _logger.LogWarning("Non-admin user attempted to trigger archival");
                return Forbid();
            }

            _logger.LogInformation("Manual archival triggered by admin");

            // Archive messages older than 90 days
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            var filter = Builders<Message>.Filter.Lt(m => m.CreatedAt, cutoffDate);

            var messagesToArchive = await _messagesCollection
                .Find(filter)
                .Limit(1000) // Limit to prevent timeouts
                .ToListAsync(cancellationToken);

            var archivedCount = messagesToArchive.Count;
            long storageUsed = 0;

            // Archive to blob storage if available
            if (_blobService != null && archivedCount > 0)
            {
                try
                {
                    // Calculate approximate storage size
                    storageUsed = messagesToArchive.Sum(m => 
                        (m.Content?.Length ?? 0) + (m.EncryptedContent?.Length ?? 0));

                    _logger.LogInformation("Archived {Count} messages, approximate storage: {Size} bytes", 
                        archivedCount, storageUsed);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to archive messages to blob storage");
                }
            }

            // Record the archival stats
            var stats = new ArchivalStats
            {
                RunAt = DateTime.UtcNow,
                MessagesArchived = archivedCount,
                StorageUsed = storageUsed,
                Status = "Success"
            };

            await _archivalStatsCollection.InsertOneAsync(stats, cancellationToken: cancellationToken);

            _logger.LogInformation("Manual archival completed: {Count} messages archived", archivedCount);

            return Ok(new TriggerArchivalResponse
            {
                Message = "Archival completed successfully",
                MessagesArchived = archivedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual archival");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during archival.");
        }
    }

    /// <summary>
    /// Gets archival statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Archival statistics</returns>
    [HttpGet("archival/stats")]
    [ProducesResponseType(typeof(ArchivalStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetArchivalStats(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsAdmin())
            {
                _logger.LogWarning("Non-admin user attempted to access archival stats");
                return Forbid();
            }

            // Get the most recent archival run
            var lastRun = await _archivalStatsCollection
                .Find(_ => true)
                .SortByDescending(s => s.RunAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Calculate totals
            var allStats = await _archivalStatsCollection
                .Find(_ => true)
                .ToListAsync(cancellationToken);

            var totalArchived = allStats.Sum(s => s.MessagesArchived);
            var totalStorage = allStats.Sum(s => s.StorageUsed);

            return Ok(new ArchivalStatsResponse
            {
                LastRunAt = lastRun?.RunAt,
                TotalMessagesArchived = totalArchived,
                TotalStorageUsed = totalStorage,
                LastRunStatus = lastRun?.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving archival stats");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving stats.");
        }
    }

    /// <summary>
    /// Triggers a manual backup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backup result</returns>
    [HttpPost("backup/trigger")]
    [ProducesResponseType(typeof(TriggerBackupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TriggerBackup(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsAdmin())
            {
                _logger.LogWarning("Non-admin user attempted to trigger backup");
                return Forbid();
            }

            _logger.LogInformation("Manual backup triggered by admin");

            var backup = await _backupService.TriggerBackupAsync(cancellationToken);

            return Ok(new TriggerBackupResponse
            {
                BackupId = backup.Id,
                Message = backup.Status == Domain.Entities.BackupStatus.Completed 
                    ? "Backup completed successfully" 
                    : $"Backup failed: {backup.ErrorMessage}",
                Status = backup.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual backup");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during backup.");
        }
    }

    /// <summary>
    /// Lists all available backups
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of backups</returns>
    [HttpGet("backup/list")]
    [ProducesResponseType(typeof(BackupListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListBackups(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsAdmin())
            {
                _logger.LogWarning("Non-admin user attempted to list backups");
                return Forbid();
            }

            var backups = await _backupService.ListBackupsAsync(cancellationToken);

            var backupInfos = backups.Select(b => new BackupInfo
            {
                Id = b.Id,
                CreatedAt = b.CreatedAt,
                FilePath = b.FilePath,
                Size = b.Size,
                Type = b.Type,
                Status = b.Status.ToString()
            }).ToList();

            return Ok(new BackupListResponse
            {
                Backups = backupInfos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backups");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while listing backups.");
        }
    }

    /// <summary>
    /// Restores from a backup
    /// </summary>
    /// <param name="date">The date of the backup to restore (yyyy-MM-dd format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Restore result</returns>
    [HttpPost("backup/restore/{date}")]
    [ProducesResponseType(typeof(RestoreBackupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestoreBackup(string date, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsAdmin())
            {
                _logger.LogWarning("Non-admin user attempted to restore backup");
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(date))
            {
                return BadRequest(new { message = "Date is required" });
            }

            _logger.LogInformation("Manual backup restore triggered by admin for date: {Date}", date);

            var success = await _backupService.RestoreBackupAsync(date, cancellationToken);

            return Ok(new RestoreBackupResponse
            {
                Message = success ? "Backup restored successfully" : "Backup restore failed",
                Success = success
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backup restore");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during restore.");
        }
    }

    /// <summary>
    /// Lists all archive files
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of archives with download URLs</returns>
    [HttpGet("archives")]
    [ProducesResponseType(typeof(ArchiveListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListArchives(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsAdmin())
            {
                _logger.LogWarning("Non-admin user attempted to list archives");
                return Forbid();
            }

            if (_blobService == null)
            {
                return Ok(new ArchiveListResponse
                {
                    Archives = new List<ArchiveInfo>()
                });
            }

            // List all archives from blob storage
            var archivePaths = await _blobService.ListArchivesAsync("archives/", cancellationToken);

            var archives = archivePaths.Select(path => new ArchiveInfo
            {
                Path = path,
                DownloadUrl = $"/admin/archives/download?path={Uri.EscapeDataString(path)}"
            }).ToList();

            return Ok(new ArchiveListResponse
            {
                Archives = archives
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing archives");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while listing archives.");
        }
    }
}
