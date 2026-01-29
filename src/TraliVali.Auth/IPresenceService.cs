namespace TraliVali.Auth;

/// <summary>
/// Service for tracking user presence and last-seen timestamps
/// </summary>
public interface IPresenceService
{
    /// <summary>
    /// Marks a user as online with a specific connection
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="connectionId">The SignalR connection ID</param>
    /// <returns>A task representing the async operation</returns>
    Task SetOnlineAsync(string userId, string connectionId);

    /// <summary>
    /// Marks a user as offline and records their last-seen timestamp
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="connectionId">The SignalR connection ID to remove</param>
    /// <returns>A task representing the async operation</returns>
    Task SetOfflineAsync(string userId, string connectionId);

    /// <summary>
    /// Gets the online status of multiple users
    /// </summary>
    /// <param name="userIds">Array of user IDs to check</param>
    /// <returns>Dictionary mapping user IDs to their online status</returns>
    Task<Dictionary<string, bool>> GetOnlineUsersAsync(string[] userIds);

    /// <summary>
    /// Gets the last-seen timestamp for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>The last-seen timestamp, or null if the user is currently online or has never been seen</returns>
    Task<DateTime?> GetLastSeenAsync(string userId);
}
