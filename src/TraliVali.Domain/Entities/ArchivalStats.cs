using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents archival statistics
/// </summary>
public class ArchivalStats
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the archival run occurred
    /// </summary>
    [BsonElement("runAt")]
    public DateTime RunAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the number of messages archived
    /// </summary>
    [BsonElement("messagesArchived")]
    public int MessagesArchived { get; set; }

    /// <summary>
    /// Gets or sets the storage used in bytes
    /// </summary>
    [BsonElement("storageUsed")]
    public long StorageUsed { get; set; }

    /// <summary>
    /// Gets or sets the status of the archival run
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Gets or sets the error message if archival failed
    /// </summary>
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }
}
