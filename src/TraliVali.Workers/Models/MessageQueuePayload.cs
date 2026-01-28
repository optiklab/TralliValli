namespace TraliVali.Workers.Models;

/// <summary>
/// Represents the payload of a message in the queue
/// </summary>
public class MessageQueuePayload
{
    /// <summary>
    /// Gets or sets the conversation identifier
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender user identifier
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender's display name
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type (e.g., text, image, file, system)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted message content for end-to-end encryption
    /// </summary>
    public string? EncryptedContent { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the message being replied to
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the list of file attachments
    /// </summary>
    public List<string> Attachments { get; set; } = new();
}
