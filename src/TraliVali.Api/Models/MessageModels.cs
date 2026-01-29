using System.ComponentModel.DataAnnotations;

namespace TraliVali.Api.Models;

/// <summary>
/// Response model for a message
/// </summary>
public class MessageResponse
{
    /// <summary>
    /// Gets or sets the message ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation ID
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender ID
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted content
    /// </summary>
    public string EncryptedContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message being replied to
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets when the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the read status list
    /// </summary>
    public List<MessageReadStatusResponse> ReadBy { get; set; } = new();

    /// <summary>
    /// Gets or sets when the message was edited
    /// </summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the message is deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the list of attachments
    /// </summary>
    public List<string> Attachments { get; set; } = new();
}

/// <summary>
/// Response model for message read status
/// </summary>
public class MessageReadStatusResponse
{
    /// <summary>
    /// Gets or sets the user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the message was read
    /// </summary>
    public DateTime ReadAt { get; set; }
}

/// <summary>
/// Response model for paginated messages with cursor-based pagination
/// </summary>
public class PaginatedMessagesResponse
{
    /// <summary>
    /// Gets or sets the list of messages
    /// </summary>
    public List<MessageResponse> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets whether there are more messages available
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Gets or sets the cursor for the next page (timestamp of the oldest message)
    /// </summary>
    public DateTime? NextCursor { get; set; }

    /// <summary>
    /// Gets or sets the number of messages in this response
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Request model for searching messages
/// </summary>
public class SearchMessagesRequest
{
    /// <summary>
    /// Gets or sets the search query
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of results (default: 50, max: 100)
    /// </summary>
    [Range(1, 100)]
    public int Limit { get; set; } = 50;
}

/// <summary>
/// Response model for search messages
/// </summary>
public class SearchMessagesResponse
{
    /// <summary>
    /// Gets or sets the list of messages
    /// </summary>
    public List<MessageResponse> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of matching messages
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the search query
    /// </summary>
    public string Query { get; set; } = string.Empty;
}
