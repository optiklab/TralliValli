using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents a message in a conversation
/// </summary>
public class Message
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
    /// Gets or sets the sender user identifier
    /// </summary>
    [BsonElement("senderId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type (e.g., text, image, file, system)
    /// </summary>
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content (plain text or metadata)
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted message content for end-to-end encryption
    /// </summary>
    [BsonElement("encryptedContent")]
    public string EncryptedContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the message being replied to
    /// </summary>
    [BsonElement("replyTo")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the list of user identifiers who have read this message
    /// </summary>
    [BsonElement("readBy")]
    public List<MessageReadStatus> ReadBy { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when the message was edited
    /// </summary>
    [BsonElement("editedAt")]
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message has been deleted
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the list of file attachments
    /// </summary>
    [BsonElement("attachments")]
    public List<string> Attachments { get; set; } = new();

    /// <summary>
    /// Validates the message entity
    /// </summary>
    /// <returns>A list of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ConversationId))
            errors.Add("ConversationId is required");

        if (string.IsNullOrWhiteSpace(SenderId))
            errors.Add("SenderId is required");

        if (string.IsNullOrWhiteSpace(Type))
            errors.Add("Type is required");

        if (string.IsNullOrWhiteSpace(Content) && string.IsNullOrWhiteSpace(EncryptedContent))
            errors.Add("Either Content or EncryptedContent is required");

        return errors;
    }
}

/// <summary>
/// Represents the read status of a message by a user
/// </summary>
public class MessageReadStatus
{
    /// <summary>
    /// Gets or sets the user identifier who read the message
    /// </summary>
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the message was read
    /// </summary>
    [BsonElement("readAt")]
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}
