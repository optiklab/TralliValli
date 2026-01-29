using System.ComponentModel.DataAnnotations;

namespace TraliVali.Api.Models;

/// <summary>
/// Request model for creating a direct conversation
/// </summary>
public class CreateDirectConversationRequest
{
    /// <summary>
    /// Gets or sets the user ID to create a conversation with
    /// </summary>
    [Required]
    public string OtherUserId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a group conversation
/// </summary>
public class CreateGroupConversationRequest
{
    /// <summary>
    /// Gets or sets the name of the group
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of user IDs to add to the group
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> MemberUserIds { get; set; } = new();
}

/// <summary>
/// Request model for updating group metadata
/// </summary>
public class UpdateGroupMetadataRequest
{
    /// <summary>
    /// Gets or sets the name of the group
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Request model for adding a member to a conversation
/// </summary>
public class AddMemberRequest
{
    /// <summary>
    /// Gets or sets the user ID to add
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role for the new member (defaults to "member")
    /// </summary>
    public string? Role { get; set; }
}

/// <summary>
/// Response model for a conversation
/// </summary>
public class ConversationResponse
{
    /// <summary>
    /// Gets or sets the conversation ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a group conversation
    /// </summary>
    public bool IsGroup { get; set; }

    /// <summary>
    /// Gets or sets the list of participants
    /// </summary>
    public List<ParticipantResponse> Participants { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of recent message IDs
    /// </summary>
    public List<string> RecentMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets when the conversation was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the last message was sent
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Response model for a participant
/// </summary>
public class ParticipantResponse
{
    /// <summary>
    /// Gets or sets the user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the participant joined
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Gets or sets when the participant last read messages
    /// </summary>
    public DateTime? LastReadAt { get; set; }

    /// <summary>
    /// Gets or sets the role of the participant
    /// </summary>
    public string Role { get; set; } = "member";
}

/// <summary>
/// Response model for paginated conversations list
/// </summary>
public class PaginatedConversationsResponse
{
    /// <summary>
    /// Gets or sets the list of conversations
    /// </summary>
    public List<ConversationResponse> Conversations { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of conversations
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages
    /// </summary>
    public int TotalPages { get; set; }
}
