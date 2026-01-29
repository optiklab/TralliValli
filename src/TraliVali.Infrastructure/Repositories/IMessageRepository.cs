using TraliVali.Domain.Entities;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Extended repository interface for Message operations
/// </summary>
public interface IMessageRepository : IRepository<Message>
{
    /// <summary>
    /// Gets messages for a conversation with cursor-based pagination
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="beforeCursor">Optional cursor for pagination (timestamp of the oldest message from previous page)</param>
    /// <param name="limit">Maximum number of messages to return (default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing messages and a flag indicating if there are more messages</returns>
    Task<(IEnumerable<Message> Messages, bool HasMore)> GetMessagesByConversationAsync(
        string conversationId,
        DateTime? beforeCursor = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches messages in a conversation by content
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="searchQuery">The search query</param>
    /// <param name="limit">Maximum number of results (default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of matching messages</returns>
    Task<IEnumerable<Message>> SearchMessagesAsync(
        string conversationId,
        string searchQuery,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a message by marking it as deleted
    /// </summary>
    /// <param name="id">The message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully marked as deleted, false otherwise</returns>
    Task<bool> SoftDeleteAsync(string id, CancellationToken cancellationToken = default);
}
