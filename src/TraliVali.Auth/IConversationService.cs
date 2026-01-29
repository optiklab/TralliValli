using TraliVali.Domain.Entities;

namespace TraliVali.Auth;

/// <summary>
/// Service for managing conversations between users
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Creates a direct conversation between two users
    /// </summary>
    /// <param name="userId1">The ID of the first user</param>
    /// <param name="userId2">The ID of the second user</param>
    /// <returns>The created conversation, or existing conversation if one already exists between the users</returns>
    Task<Conversation> CreateDirectConversationAsync(string userId1, string userId2);

    /// <summary>
    /// Creates a group conversation with multiple members
    /// </summary>
    /// <param name="name">The name of the group conversation</param>
    /// <param name="creatorId">The ID of the user creating the group</param>
    /// <param name="memberIds">Array of member user IDs to add to the group</param>
    /// <returns>The created group conversation</returns>
    Task<Conversation> CreateGroupConversationAsync(string name, string creatorId, string[] memberIds);

    /// <summary>
    /// Adds a member to an existing conversation
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="userId">The ID of the user to add</param>
    /// <param name="role">The role of the user in the conversation (default: "member")</param>
    /// <returns>True if the member was added successfully, false otherwise</returns>
    Task<bool> AddMemberAsync(string conversationId, string userId, string role = "member");

    /// <summary>
    /// Removes a member from a conversation
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="userId">The ID of the user to remove</param>
    /// <returns>True if the member was removed successfully, false otherwise</returns>
    Task<bool> RemoveMemberAsync(string conversationId, string userId);

    /// <summary>
    /// Updates the metadata of a group conversation
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="name">The new name for the conversation (optional)</param>
    /// <param name="avatar">The new avatar URL for the conversation (optional)</param>
    /// <returns>True if the metadata was updated successfully, false otherwise</returns>
    Task<bool> UpdateGroupMetadataAsync(string conversationId, string? name = null, string? avatar = null);

    /// <summary>
    /// Gets all conversations for a specific user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>List of conversations the user is a participant in</returns>
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId);
}
