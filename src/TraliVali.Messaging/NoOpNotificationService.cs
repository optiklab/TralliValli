using Microsoft.Extensions.Logging;

namespace TraliVali.Messaging;

/// <summary>
/// No-operation implementation of INotificationService that logs notifications but takes no action.
/// This is useful for development/testing environments or when notification functionality is disabled.
/// </summary>
public class NoOpNotificationService : INotificationService
{
    private readonly ILogger<NoOpNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoOpNotificationService"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public NoOpNotificationService(ILogger<NoOpNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("NoOpNotificationService initialized - notifications will be logged but not sent");
    }

    /// <inheritdoc/>
    public Task SendPushNotificationAsync(string userId, string title, string body, CancellationToken cancellationToken = default)
    {
        if (userId == null)
            throw new ArgumentNullException(nameof(userId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty or whitespace.", nameof(userId));
        if (title == null)
            throw new ArgumentNullException(nameof(title));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty or whitespace.", nameof(title));
        if (body == null)
            throw new ArgumentNullException(nameof(body));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body cannot be empty or whitespace.", nameof(body));

        _logger.LogInformation(
            "Would send push notification to user {UserId}: Title='{Title}', Body='{Body}'",
            userId, title, body);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendBatchNotificationsAsync(string[] userIds, string title, string body, CancellationToken cancellationToken = default)
    {
        if (userIds == null)
            throw new ArgumentNullException(nameof(userIds));
        if (userIds.Length == 0)
            throw new ArgumentException("User IDs array cannot be empty.", nameof(userIds));
        if (title == null)
            throw new ArgumentNullException(nameof(title));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty or whitespace.", nameof(title));
        if (body == null)
            throw new ArgumentNullException(nameof(body));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body cannot be empty or whitespace.", nameof(body));

        _logger.LogInformation(
            "Would send batch notification to {UserCount} users: Title='{Title}', Body='{Body}', UserIds=[{UserIds}]",
            userIds.Length, title, body, string.Join(", ", userIds));

        return Task.CompletedTask;
    }
}
