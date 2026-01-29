using TraliVali.Auth.Models;

namespace TraliVali.Auth;

/// <summary>
/// Service for archiving and exporting conversation messages
/// </summary>
public interface IArchiveService
{
    /// <summary>
    /// Exports conversation messages to JSON format within a specified date range
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to export</param>
    /// <param name="startDate">The start date for the export (inclusive)</param>
    /// <param name="endDate">The end date for the export (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An export result containing the JSON data</returns>
    Task<ExportResult> ExportConversationMessagesAsync(
        string conversationId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
