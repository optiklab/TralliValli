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
    /// Gets or sets a value indicating whether this is a group conversation
    /// </summary>
    [BsonElement("isGroup")]
    public bool IsGroup { get; set; }
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
}
