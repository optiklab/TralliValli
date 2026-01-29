namespace TraliVali.Messaging;

/// <summary>
/// Interface for sending push notifications to users
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a push notification to a single user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="title">The notification title</param>
    /// <param name="body">The notification body content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendPushNotificationAsync(string userId, string title, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the same push notification to multiple users
    /// </summary>
    /// <param name="userIds">Array of user identifiers to receive the notification</param>
    /// <param name="title">The notification title</param>
    /// <param name="body">The notification body content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendBatchNotificationsAsync(string[] userIds, string title, string body, CancellationToken cancellationToken = default);
}
