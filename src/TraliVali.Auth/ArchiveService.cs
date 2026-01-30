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
    private readonly IMongoCollection<ConversationKey> _conversationKeys;
    private readonly IMessageEncryptionService _encryptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveService"/> class
    /// </summary>
    /// <param name="conversations">The MongoDB conversations collection</param>
    /// <param name="messages">The MongoDB messages collection</param>
    /// <param name="users">The MongoDB users collection</param>
    /// <param name="conversationKeys">The MongoDB conversation keys collection</param>
    /// <param name="encryptionService">The message encryption service</param>
    public ArchiveService(
        IMongoCollection<Conversation> conversations,
        IMongoCollection<Message> messages,
        IMongoCollection<User> users,
        IMongoCollection<ConversationKey> conversationKeys,
        IMessageEncryptionService encryptionService)
    {
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _conversationKeys = conversationKeys ?? throw new ArgumentNullException(nameof(conversationKeys));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    /// <inheritdoc/>
    public async Task<ExportResult> ExportConversationMessagesAsync(
        string conversationId,
        DateTime startDate,
        DateTime endDate,
        string? masterPassword = null,
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

        // Try to get conversation key and derive master key if password provided
        byte[]? conversationKey = null;
        if (!string.IsNullOrWhiteSpace(masterPassword))
        {
            try
            {
                var storedKey = await _conversationKeys
                    .Find(k => k.ConversationId == conversationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (storedKey != null)
                {
                    // Derive master key from password
                    var masterKey = await _encryptionService.DeriveMasterKeyFromPasswordAsync(
                        masterPassword, 
                        storedKey.Salt);

                    // Decrypt conversation key using master key
                    conversationKey = await _encryptionService.DecryptConversationKeyAsync(
                        storedKey.EncryptedKey,
                        masterKey,
                        storedKey.Iv,
                        storedKey.Tag);
                }
            }
            catch (Exception)
            {
                // If decryption fails, continue without decryption
                // This allows graceful fallback to plain content
                conversationKey = null;
            }
        }

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
                
                // Decrypt message content using conversation key if available
                string decryptedContent;
                
                if (conversationKey != null && !string.IsNullOrWhiteSpace(m.EncryptedContent))
                {
                    try
                    {
                        // Try to decrypt using conversation key
                        // Note: We need IV and tag from the message metadata
                        // For now, we'll parse them from the encrypted content format
                        // Format expected: base64_iv:base64_tag:base64_ciphertext
                        var parts = m.EncryptedContent.Split(':');
                        if (parts.Length == 3)
                        {
                            var iv = parts[0];
                            var tag = parts[1];
                            var ciphertext = parts[2];
                            
                            decryptedContent = _encryptionService.DecryptMessageAsync(
                                ciphertext, 
                                conversationKey, 
                                iv, 
                                tag).GetAwaiter().GetResult();
                        }
                        else
                        {
                            // Fallback if format doesn't match
                            decryptedContent = !string.IsNullOrWhiteSpace(m.Content)
                                ? m.Content
                                : m.EncryptedContent;
                        }
                    }
                    catch
                    {
                        // If decryption fails, fall back to plain content or encrypted content
                        decryptedContent = !string.IsNullOrWhiteSpace(m.Content)
                            ? m.Content
                            : m.EncryptedContent;
                    }
                }
                else
                {
                    // No conversation key available, use plain content or encrypted content
                    decryptedContent = !string.IsNullOrWhiteSpace(m.Content)
                        ? m.Content
                        : m.EncryptedContent;
                }

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
