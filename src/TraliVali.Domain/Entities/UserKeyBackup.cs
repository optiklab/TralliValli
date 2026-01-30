using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents an encrypted backup of a user's cryptographic keys
/// </summary>
public class UserKeyBackup
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID who owns this backup
    /// </summary>
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup format version
    /// </summary>
    [BsonElement("version")]
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the encrypted backup data (Base64 encoded)
    /// </summary>
    [BsonElement("encryptedData")]
    public string EncryptedData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initialization vector for decryption (Base64 encoded)
    /// </summary>
    [BsonElement("iv")]
    public string Iv { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PBKDF2 salt for key derivation (Base64 encoded)
    /// </summary>
    [BsonElement("salt")]
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the backup was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the backup was last updated
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates the user key backup entity
    /// </summary>
    /// <returns>A list of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(UserId))
            errors.Add("UserId is required");

        if (Version <= 0)
            errors.Add("Version must be positive");

        if (string.IsNullOrWhiteSpace(EncryptedData))
            errors.Add("EncryptedData is required");

        if (string.IsNullOrWhiteSpace(Iv))
            errors.Add("Iv is required");

        if (string.IsNullOrWhiteSpace(Salt))
            errors.Add("Salt is required");

        return errors;
    }
}
