using Microsoft.Extensions.Logging;

namespace TraliVali.Messaging;

/// <summary>
/// No-operation email service for local development when Azure Communication Email is not configured.
/// Logs email actions but does not actually send emails.
/// </summary>
public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoOpEmailService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public NoOpEmailService(ILogger<NoOpEmailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task SendMagicLinkEmailAsync(string recipientEmail, string recipientName, string magicLink, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp Email] Magic link email to {Email} ({Name}): {MagicLink}", recipientEmail, recipientName, magicLink);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendInviteEmailAsync(string recipientEmail, string recipientName, string inviterName, string inviteLink, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp Email] Invite email to {Email} ({Name}) from {Inviter}: {InviteLink}", recipientEmail, recipientName, inviterName, inviteLink);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp Email] Password reset email to {Email} ({Name}): {ResetLink}", recipientEmail, recipientName, resetLink);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendWelcomeEmailAsync(string recipientEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp Email] Welcome email to {Email} ({Name})", recipientEmail, recipientName);
        return Task.CompletedTask;
    }
}
