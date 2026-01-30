using TraliVali.Auth.Models;

namespace TraliVali.Auth;

/// <summary>
/// Service for archiving and exporting conversation messages
/// </summary>
public interface IArchiveService
{
    /// <summary>
    /// Exports conversation messages to structured data for JSON serialization within a specified date range
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to export</param>
    /// <param name="startDate">The start date for the export (inclusive)</param>
    /// <param name="endDate">The end date for the export (inclusive)</param>
    /// <param name="masterPassword">The user's master password for decrypting conversation keys (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An export result object containing the structured conversation data with decrypted messages</returns>
    /// <remarks>
    /// If masterPassword is provided, the service will attempt to decrypt messages using the conversation key.
    /// If masterPassword is not provided or decryption fails, the service falls back to plain content or encrypted content.
    /// </remarks>
    Task<ExportResult> ExportConversationMessagesAsync(
        string conversationId,
        DateTime startDate,
        DateTime endDate,
        string? masterPassword = null,
        CancellationToken cancellationToken = default);
}
