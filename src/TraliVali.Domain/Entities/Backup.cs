using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents a backup operation
/// </summary>
public class Backup
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup file path
    /// </summary>
    [BsonElement("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup size in bytes
    /// </summary>
    [BsonElement("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the backup was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the backup type (full, incremental, etc.)
    /// </summary>
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup status
    /// </summary>
    [BsonElement("status")]
    public BackupStatus Status { get; set; } = BackupStatus.Pending;

    /// <summary>
    /// Gets or sets the error message if backup failed
    /// </summary>
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents the status of a backup
/// </summary>
public enum BackupStatus
{
    /// <summary>
    /// Backup is pending
    /// </summary>
    Pending,

    /// <summary>
    /// Backup is in progress
    /// </summary>
    InProgress,

    /// <summary>
    /// Backup completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Backup failed
    /// </summary>
    Failed
}
