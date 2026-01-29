using System.Text.Json.Serialization;

namespace TraliVali.Auth.Models;

/// <summary>
/// Represents the result of an archive export operation
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Gets or sets the timestamp when the export was created
    /// </summary>
    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation name
    /// </summary>
    [JsonPropertyName("conversationName")]
    public string ConversationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of participants
    /// </summary>
    [JsonPropertyName("participants")]
    public List<ParticipantInfo> Participants { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of messages in the export
    /// </summary>
    [JsonPropertyName("messagesCount")]
    public int MessagesCount { get; set; }

    /// <summary>
    /// Gets or sets the list of exported messages
    /// </summary>
    [JsonPropertyName("messages")]
    public List<ExportedMessage> Messages { get; set; } = new();
}

/// <summary>
/// Represents participant information in an export
/// </summary>
public class ParticipantInfo
{
    /// <summary>
    /// Gets or sets the user identifier
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the participant
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Represents an exported message
/// </summary>
public class ExportedMessage
{
    /// <summary>
    /// Gets or sets the message identifier
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender user identifier
    /// </summary>
    [JsonPropertyName("senderId")]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender's display name
    /// </summary>
    [JsonPropertyName("senderName")]
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decrypted message content
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the message being replied to
    /// </summary>
    [JsonPropertyName("replyTo")]
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was edited
    /// </summary>
    [JsonPropertyName("editedAt")]
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Gets or sets the list of file attachment references
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<string> Attachments { get; set; } = new();
}
