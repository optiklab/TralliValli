using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents a file attachment
/// </summary>
public class File
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name
    /// </summary>
    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type
    /// </summary>
    [BsonElement("contentType")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes
    /// </summary>
    [BsonElement("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the storage path or URL
    /// </summary>
    [BsonElement("storagePath")]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier who uploaded the file
    /// </summary>
    [BsonElement("uploadedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the file was uploaded
    /// </summary>
    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the file has been deleted
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
