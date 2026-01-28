namespace TraliVali.Api.Hubs;

/// <summary>
/// Strongly-typed interface defining client-side methods for SignalR chat
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Called when a new message is received in a conversation
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="messageId">The ID of the message</param>
    /// <param name="senderId">The ID of the user who sent the message</param>
    /// <param name="senderName">The display name of the sender</param>
    /// <param name="content">The message content</param>
    /// <param name="timestamp">The timestamp when the message was sent</param>
    /// <returns>A task representing the async operation</returns>
    Task ReceiveMessage(string conversationId, string messageId, string senderId, string senderName, string content, DateTime timestamp);

    /// <summary>
    /// Called when a user joins a conversation
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="userId">The ID of the user who joined</param>
    /// <param name="userName">The display name of the user who joined</param>
    /// <returns>A task representing the async operation</returns>
    Task UserJoined(string conversationId, string userId, string userName);

    /// <summary>
    /// Called when a user leaves a conversation
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="userId">The ID of the user who left</param>
    /// <param name="userName">The display name of the user who left</param>
    /// <returns>A task representing the async operation</returns>
    Task UserLeft(string conversationId, string userId, string userName);

    /// <summary>
    /// Called when a user starts or stops typing in a conversation
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="userId">The ID of the user typing</param>
    /// <param name="userName">The display name of the user typing</param>
    /// <param name="isTyping">True if user is typing, false if they stopped</param>
    /// <returns>A task representing the async operation</returns>
    Task TypingIndicator(string conversationId, string userId, string userName, bool isTyping);

    /// <summary>
    /// Called when a user has read a message
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="messageId">The ID of the message that was read</param>
    /// <param name="userId">The ID of the user who read the message</param>
    /// <returns>A task representing the async operation</returns>
    Task MessageRead(string conversationId, string messageId, string userId);

    /// <summary>
    /// Called when a user's presence status changes
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="isOnline">True if the user is online, false if offline</param>
    /// <param name="lastSeen">The timestamp of when the user was last seen (if offline)</param>
    /// <returns>A task representing the async operation</returns>
    Task PresenceUpdate(string userId, bool isOnline, DateTime? lastSeen);
}
