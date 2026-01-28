namespace TraliVali.Messaging;

/// <summary>
/// Interface for sending emails through Azure Communication Services
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a magic link authentication email
    /// </summary>
    /// <param name="recipientEmail">The recipient's email address</param>
    /// <param name="recipientName">The recipient's name</param>
    /// <param name="magicLink">The magic link URL for authentication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendMagicLinkEmailAsync(string recipientEmail, string recipientName, string magicLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an invite notification email
    /// </summary>
    /// <param name="recipientEmail">The recipient's email address</param>
    /// <param name="recipientName">The recipient's name</param>
    /// <param name="inviterName">The name of the person sending the invite</param>
    /// <param name="inviteLink">The invite link URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendInviteEmailAsync(string recipientEmail, string recipientName, string inviterName, string inviteLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email
    /// </summary>
    /// <param name="recipientEmail">The recipient's email address</param>
    /// <param name="recipientName">The recipient's name</param>
    /// <param name="resetLink">The password reset link URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default);
}
