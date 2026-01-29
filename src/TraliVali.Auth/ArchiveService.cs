using MongoDB.Driver;
using TraliVali.Auth.Models;
using TraliVali.Domain.Entities;

namespace TraliVali.Auth;

/// <summary>
/// MongoDB-based implementation of archive service
/// </summary>
public class ArchiveService : IArchiveService
{
    private readonly IMongoCollection<Conversation> _conversations;
    private readonly IMongoCollection<Message> _messages;
    private readonly IMongoCollection<User> _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveService"/> class
    /// </summary>
    /// <param name="conversations">The MongoDB conversations collection</param>
    /// <param name="messages">The MongoDB messages collection</param>
    /// <param name="users">The MongoDB users collection</param>
    public ArchiveService(
        IMongoCollection<Conversation> conversations,
        IMongoCollection<Message> messages,
        IMongoCollection<User> users)
    {
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    /// <inheritdoc/>
    public async Task<ExportResult> ExportConversationMessagesAsync(
        string conversationId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));

        if (startDate > endDate)
            throw new ArgumentException($"Start date must be before or equal to end date (startDate: {startDate:O}, endDate: {endDate:O})");

        // Get conversation details
        var conversation = await _conversations
            .Find(c => c.Id == conversationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
            throw new InvalidOperationException($"Conversation with ID '{conversationId}' not found");

        // Build filter for messages within date range
        var filterBuilder = Builders<Message>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(m => m.ConversationId, conversationId),
            filterBuilder.Gte(m => m.CreatedAt, startDate),
            filterBuilder.Lte(m => m.CreatedAt, endDate),
            filterBuilder.Eq(m => m.IsDeleted, false)
        );

        // Get messages sorted by creation date
        var messages = await _messages
            .Find(filter)
            .SortBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        // Get all unique sender IDs from messages
        var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

        // Get all user details for senders and participants
        var participantUserIds = conversation.Participants.Select(p => p.UserId).ToList();
        var allUserIds = senderIds.Union(participantUserIds).Distinct().ToList();

        var userFilter = Builders<User>.Filter.In(u => u.Id, allUserIds);
        var users = await _users
            .Find(userFilter)
            .ToListAsync(cancellationToken);

        // Create a dictionary for quick user lookup
        var userDictionary = users.ToDictionary(u => u.Id, u => u);

        // Build participant information
        var participants = conversation.Participants
            .Select(p =>
            {
                var user = userDictionary.TryGetValue(p.UserId, out var u) ? u : null;
                return new ParticipantInfo
                {
                    UserId = p.UserId,
                    DisplayName = user?.DisplayName ?? "Unknown User",
                    Email = user?.Email ?? string.Empty,
                    Role = p.Role
                };
            })
            .ToList();

        // Build exported messages with decrypted content and sender names
        var exportedMessages = messages
            .Select(m =>
            {
                var sender = userDictionary.TryGetValue(m.SenderId, out var s) ? s : null;
                
                // Decrypt message: For now, prefer plain Content over EncryptedContent
                // Note: Full encryption/decryption will be implemented in Phase 5
                // Until then, we export the readable Content field for usability
                var decryptedContent = !string.IsNullOrWhiteSpace(m.Content)
                    ? m.Content
                    : m.EncryptedContent; // Fallback to encrypted if no plain content

                return new ExportedMessage
                {
                    MessageId = m.Id,
                    SenderId = m.SenderId,
                    SenderName = sender?.DisplayName ?? "Unknown User",
                    Type = m.Type,
                    Content = decryptedContent,
                    ReplyTo = m.ReplyTo,
                    CreatedAt = m.CreatedAt,
                    EditedAt = m.EditedAt,
                    Attachments = m.Attachments
                };
            })
            .ToList();

        // Build and return the export result
        return new ExportResult
        {
            ExportedAt = DateTime.UtcNow,
            ConversationId = conversation.Id,
            ConversationName = conversation.Name,
            Participants = participants,
            MessagesCount = exportedMessages.Count,
            Messages = exportedMessages
        };
    }
}
