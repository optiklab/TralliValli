using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents a conversation between users
/// </summary>
public class Conversation
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation type (e.g., direct, group, channel)
    /// </summary>
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation name
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of participants
    /// </summary>
    [BsonElement("participants")]
    public List<Participant> Participants { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of recent messages (limited to 50)
    /// </summary>
    [BsonElement("recentMessages")]
    public List<string> RecentMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when the conversation was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time of the last message
    /// </summary>
    [BsonElement("lastMessageAt")]
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the conversation
    /// </summary>
    [BsonElement("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this is a group conversation
    /// </summary>
    [BsonElement("isGroup")]
    public bool IsGroup { get; set; }

    /// <summary>
    /// Validates the conversation entity
    /// </summary>
    /// <returns>A list of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Type))
            errors.Add("Type is required");

        if (Participants.Count == 0)
            errors.Add("At least one participant is required");

        if (RecentMessages.Count > 50)
            errors.Add("RecentMessages cannot exceed 50 items");

        return errors;
    }
}

/// <summary>
/// Represents a participant in a conversation
/// </summary>
public class Participant
{
    /// <summary>
    /// Gets or sets the user identifier
    /// </summary>
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the participant joined
    /// </summary>
    [BsonElement("joinedAt")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the participant last read messages
    /// </summary>
    [BsonElement("lastReadAt")]
    public DateTime? LastReadAt { get; set; }

    /// <summary>
    /// Gets or sets the role of the participant in the conversation (e.g., admin, member)
    /// </summary>
    [BsonElement("role")]
    public string Role { get; set; } = "member";
}
