using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TraliVali.Api.Hubs;

/// <summary>
/// SignalR hub for real-time chat functionality
/// </summary>
[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly ILogger<ChatHub> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatHub"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a message to all users in a conversation
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="messageId">The ID of the message</param>
    /// <param name="content">The message content</param>
    /// <returns>A task representing the async operation</returns>
    public async Task SendMessage(string conversationId, string messageId, string content)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("Message ID is required", nameof(messageId));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        var userId = GetUserId();
        var userName = GetUserName();

        _logger.LogInformation("User {UserId} sending message {MessageId} to conversation {ConversationId}", 
            userId, messageId, conversationId);

        await Clients.Group(conversationId).ReceiveMessage(
            conversationId, 
            messageId, 
            userId, 
            userName, 
            content, 
            DateTime.UtcNow);
    }

    /// <summary>
    /// Joins a conversation group
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to join</param>
    /// <returns>A task representing the async operation</returns>
    public async Task JoinConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));

        var userId = GetUserId();
        var userName = GetUserName();

        _logger.LogInformation("User {UserId} joining conversation {ConversationId}", userId, conversationId);

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        await Clients.Group(conversationId).UserJoined(conversationId, userId, userName);
    }

    /// <summary>
    /// Leaves a conversation group
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to leave</param>
    /// <returns>A task representing the async operation</returns>
    public async Task LeaveConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));

        var userId = GetUserId();
        var userName = GetUserName();

        _logger.LogInformation("User {UserId} leaving conversation {ConversationId}", userId, conversationId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        await Clients.Group(conversationId).UserLeft(conversationId, userId, userName);
    }

    /// <summary>
    /// Notifies other users in a conversation that the current user is typing
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <returns>A task representing the async operation</returns>
    public async Task StartTyping(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));

        var userId = GetUserId();
        var userName = GetUserName();

        _logger.LogDebug("User {UserId} started typing in conversation {ConversationId}", userId, conversationId);

        await Clients.OthersInGroup(conversationId).TypingIndicator(conversationId, userId, userName, true);
    }

    /// <summary>
    /// Notifies other users in a conversation that the current user stopped typing
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <returns>A task representing the async operation</returns>
    public async Task StopTyping(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));

        var userId = GetUserId();
        var userName = GetUserName();

        _logger.LogDebug("User {UserId} stopped typing in conversation {ConversationId}", userId, conversationId);

        await Clients.OthersInGroup(conversationId).TypingIndicator(conversationId, userId, userName, false);
    }

    /// <summary>
    /// Marks a message as read by the current user
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="messageId">The ID of the message to mark as read</param>
    /// <returns>A task representing the async operation</returns>
    public async Task MarkAsRead(string conversationId, string messageId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("Message ID is required", nameof(messageId));

        var userId = GetUserId();

        _logger.LogDebug("User {UserId} marked message {MessageId} as read in conversation {ConversationId}", 
            userId, messageId, conversationId);

        await Clients.Group(conversationId).MessageRead(conversationId, messageId, userId);
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    /// <returns>A task representing the async operation</returns>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        
        _logger.LogInformation("User {UserId} connected with connection ID {ConnectionId}", 
            userId, Context.ConnectionId);

        // Notify all clients about this user's online status
        await Clients.All.PresenceUpdate(userId, true, null);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    /// <param name="exception">The exception that caused the disconnect, if any</param>
    /// <returns>A task representing the async operation</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        
        _logger.LogInformation("User {UserId} disconnected from connection ID {ConnectionId}", 
            userId, Context.ConnectionId);

        // Notify all clients about this user's offline status
        await Clients.All.PresenceUpdate(userId, false, DateTime.UtcNow);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gets the user ID from the current user's claims
    /// </summary>
    /// <returns>The user ID</returns>
    private string GetUserId()
    {
        return Context.User?.FindFirst("userId")?.Value 
            ?? Context.User?.Identity?.Name 
            ?? "unknown";
    }

    /// <summary>
    /// Gets the user display name from the current user's claims
    /// </summary>
    /// <returns>The user display name</returns>
    private string GetUserName()
    {
        return Context.User?.FindFirst("displayName")?.Value 
            ?? Context.User?.Identity?.Name 
            ?? "Unknown User";
    }
}
