using System.Reflection;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;

namespace TraliVali.Messaging;

/// <summary>
/// Implementation of email service using Azure Communication Services
/// </summary>
public class AzureCommunicationEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly AzureCommunicationEmailConfiguration _configuration;
    private readonly ILogger<AzureCommunicationEmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCommunicationEmailService"/> class
    /// </summary>
    /// <param name="configuration">The email configuration</param>
    /// <param name="logger">The logger instance</param>
    public AzureCommunicationEmailService(
        AzureCommunicationEmailConfiguration configuration,
        ILogger<AzureCommunicationEmailService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate configuration
        var errors = _configuration.Validate();
        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Invalid email configuration: {string.Join(", ", errors)}");
        }

        _emailClient = new EmailClient(_configuration.ConnectionString);
        _logger.LogInformation("AzureCommunicationEmailService initialized successfully");
    }

    /// <inheritdoc/>
    public async Task SendMagicLinkEmailAsync(
        string recipientEmail,
        string recipientName,
        string magicLink,
        CancellationToken cancellationToken = default)
    {
        ValidateEmailParameters(recipientEmail, recipientName);
        if (string.IsNullOrWhiteSpace(magicLink))
            throw new ArgumentNullException(nameof(magicLink));

        _logger.LogInformation("Sending magic link email to {Email}", recipientEmail);

        var template = await LoadTemplateAsync("MagicLinkEmail.html");
        var htmlContent = template
            .Replace("{{RecipientName}}", recipientName)
            .Replace("{{MagicLink}}", magicLink);

        await SendEmailAsync(
            recipientEmail,
            recipientName,
            "Sign in to TraliVali",
            htmlContent,
            cancellationToken);

        _logger.LogInformation("Magic link email sent successfully to {Email}", recipientEmail);
    }

    /// <inheritdoc/>
    public async Task SendInviteEmailAsync(
        string recipientEmail,
        string recipientName,
        string inviterName,
        string inviteLink,
        CancellationToken cancellationToken = default)
    {
        ValidateEmailParameters(recipientEmail, recipientName);
        if (string.IsNullOrWhiteSpace(inviterName))
            throw new ArgumentNullException(nameof(inviterName));
        if (string.IsNullOrWhiteSpace(inviteLink))
            throw new ArgumentNullException(nameof(inviteLink));

        _logger.LogInformation("Sending invite email to {Email} from {Inviter}", recipientEmail, inviterName);

        var template = await LoadTemplateAsync("InviteEmail.html");
        var htmlContent = template
            .Replace("{{RecipientName}}", recipientName)
            .Replace("{{InviterName}}", inviterName)
            .Replace("{{InviteLink}}", inviteLink);

        await SendEmailAsync(
            recipientEmail,
            recipientName,
            "You're invited to TraliVali",
            htmlContent,
            cancellationToken);

        _logger.LogInformation("Invite email sent successfully to {Email}", recipientEmail);
    }

    /// <inheritdoc/>
    public async Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string recipientName,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        ValidateEmailParameters(recipientEmail, recipientName);
        if (string.IsNullOrWhiteSpace(resetLink))
            throw new ArgumentNullException(nameof(resetLink));

        _logger.LogInformation("Sending password reset email to {Email}", recipientEmail);

        var template = await LoadTemplateAsync("PasswordResetEmail.html");
        var htmlContent = template
            .Replace("{{RecipientName}}", recipientName)
            .Replace("{{ResetLink}}", resetLink);

        await SendEmailAsync(
            recipientEmail,
            recipientName,
            "Password Reset Request",
            htmlContent,
            cancellationToken);

        _logger.LogInformation("Password reset email sent successfully to {Email}", recipientEmail);
    }

    private async Task SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken)
    {
        try
        {
            var emailMessage = new EmailMessage(
                senderAddress: _configuration.SenderAddress,
                content: new EmailContent(subject)
                {
                    Html = htmlContent
                },
                recipients: new EmailRecipients(new List<EmailAddress>
                {
                    new EmailAddress(recipientEmail, recipientName)
                }));

            EmailSendOperation emailSendOperation = await _emailClient.SendAsync(
                WaitUntil.Started,
                emailMessage,
                cancellationToken);

            _logger.LogDebug(
                "Email sent with message ID: {MessageId}, Status: {Status}",
                emailSendOperation.Id,
                emailSendOperation.HasValue ? "Started" : "Pending");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}. Error: {Error}", recipientEmail, ex.Message);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Email}", recipientEmail);
            throw;
        }
    }

    private static async Task<string> LoadTemplateAsync(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"TraliVali.Messaging.Templates.{templateName}";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Email template '{templateName}' not found");
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static void ValidateEmailParameters(string email, string name)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentNullException(nameof(email));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
    }
}
