namespace TraliVali.Api.Models;

/// <summary>
/// Response for manual archival trigger
/// </summary>
public class TriggerArchivalResponse
{
    /// <summary>
    /// Gets or sets the status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of messages archived
    /// </summary>
    public int MessagesArchived { get; set; }
}

/// <summary>
/// Response for archival statistics
/// </summary>
public class ArchivalStatsResponse
{
    /// <summary>
    /// Gets or sets the date and time of the last archival run
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages archived
    /// </summary>
    public int TotalMessagesArchived { get; set; }

    /// <summary>
    /// Gets or sets the total storage used in bytes
    /// </summary>
    public long TotalStorageUsed { get; set; }

    /// <summary>
    /// Gets or sets the status of the last run
    /// </summary>
    public string? LastRunStatus { get; set; }
}

/// <summary>
/// Response for backup trigger
/// </summary>
public class TriggerBackupResponse
{
    /// <summary>
    /// Gets or sets the backup ID
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response for listing backups
/// </summary>
public class BackupListResponse
{
    /// <summary>
    /// Gets or sets the list of backups
    /// </summary>
    public List<BackupInfo> Backups { get; set; } = new();
}

/// <summary>
/// Information about a backup
/// </summary>
public class BackupInfo
{
    /// <summary>
    /// Gets or sets the backup ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of the backup
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the backup file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the backup type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response for backup restore
/// </summary>
public class RestoreBackupResponse
{
    /// <summary>
    /// Gets or sets the status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the restore was successful
    /// </summary>
    public bool Success { get; set; }
}

/// <summary>
/// Response for listing archives
/// </summary>
public class ArchiveListResponse
{
    /// <summary>
    /// Gets or sets the list of archives
    /// </summary>
    public List<ArchiveInfo> Archives { get; set; } = new();
}

/// <summary>
/// Information about an archive
/// </summary>
public class ArchiveInfo
{
    /// <summary>
    /// Gets or sets the archive path
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the download URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size in bytes
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets the last modified date
    /// </summary>
    public DateTime? LastModified { get; set; }
}
