using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents an encrypted conversation key stored separately from archives
/// </summary>
public class ConversationKey
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation ID this key belongs to
    /// </summary>
    [BsonElement("conversationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted conversation key (Base64 encoded)
    /// </summary>
    [BsonElement("encryptedKey")]
    public string EncryptedKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initialization vector for key decryption (Base64 encoded)
    /// </summary>
    [BsonElement("iv")]
    public string Iv { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PBKDF2 salt for master key derivation (Base64 encoded)
    /// </summary>
    [BsonElement("salt")]
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication tag for AES-GCM (Base64 encoded)
    /// </summary>
    [BsonElement("tag")]
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the key was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the key was last rotated
    /// </summary>
    [BsonElement("rotatedAt")]
    public DateTime? RotatedAt { get; set; }

    /// <summary>
    /// Gets or sets the key version for rotation tracking
    /// </summary>
    [BsonElement("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Validates the conversation key entity
    /// </summary>
    /// <returns>A list of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ConversationId))
            errors.Add("ConversationId is required");

        if (string.IsNullOrWhiteSpace(EncryptedKey))
            errors.Add("EncryptedKey is required");

        if (string.IsNullOrWhiteSpace(Iv))
            errors.Add("Iv is required");

        if (string.IsNullOrWhiteSpace(Salt))
            errors.Add("Salt is required");

        if (string.IsNullOrWhiteSpace(Tag))
            errors.Add("Tag is required");

        if (Version <= 0)
            errors.Add("Version must be positive");

        return errors;
    }
}
