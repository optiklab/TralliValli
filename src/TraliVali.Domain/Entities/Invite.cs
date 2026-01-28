using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents an invitation to join the platform
/// </summary>
public class Invite
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique invitation token
    /// </summary>
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the invitee
    /// </summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier who created the invite
    /// </summary>
    [BsonElement("invitedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string InvitedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the invite was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the invite expires
    /// </summary>
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the invite has been used
    /// </summary>
    [BsonElement("isUsed")]
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the invite was used
    /// </summary>
    [BsonElement("usedAt")]
    public DateTime? UsedAt { get; set; }
}
