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
    /// Gets or sets the conversation identifier
    /// </summary>
    [BsonElement("conversationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier who uploaded the file
    /// </summary>
    [BsonElement("uploaderId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UploaderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name
    /// </summary>
    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type
    /// </summary>
    [BsonElement("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes
    /// </summary>
    [BsonElement("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the blob storage path
    /// </summary>
    [BsonElement("blobPath")]
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the thumbnail storage path (for images/videos)
    /// </summary>
    [BsonElement("thumbnailPath")]
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was uploaded
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the content type (legacy field for backward compatibility)
    /// </summary>
    [BsonElement("contentType")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage path (legacy field for backward compatibility)
    /// </summary>
    [BsonElement("storagePath")]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier who uploaded the file (legacy field for backward compatibility)
    /// </summary>
    [BsonElement("uploadedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the file was uploaded (legacy field for backward compatibility)
    /// </summary>
    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the file has been deleted
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Validates the file entity
    /// </summary>
    /// <returns>A list of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ConversationId))
            errors.Add("ConversationId is required");

        if (string.IsNullOrWhiteSpace(UploaderId))
            errors.Add("UploaderId is required");

        if (string.IsNullOrWhiteSpace(FileName))
            errors.Add("FileName is required");

        if (string.IsNullOrWhiteSpace(MimeType))
            errors.Add("MimeType is required");

        if (Size <= 0)
            errors.Add("Size must be greater than zero");

        if (string.IsNullOrWhiteSpace(BlobPath))
            errors.Add("BlobPath is required");

        return errors;
    }
}
