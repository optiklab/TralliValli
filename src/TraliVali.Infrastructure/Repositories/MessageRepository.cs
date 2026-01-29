using MongoDB.Bson;
using MongoDB.Driver;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Repository for Message entities
/// </summary>
public class MessageRepository : MongoRepository<Message>, IMessageRepository
{
    private readonly IMongoCollection<Message> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRepository"/> class
    /// </summary>
    /// <param name="context">The MongoDB context</param>
    public MessageRepository(MongoDbContext context) : base(context.Messages)
    {
        _collection = context.Messages;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Message> Messages, bool HasMore)> GetMessagesByConversationAsync(
        string conversationId,
        DateTime? beforeCursor = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Build filter for conversation, cursor, and exclude soft-deleted messages
        var filterBuilder = Builders<Message>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(m => m.ConversationId, conversationId),
            filterBuilder.Eq(m => m.IsDeleted, false)
        );

        if (beforeCursor.HasValue)
        {
            filter = filterBuilder.And(
                filter,
                filterBuilder.Lt(m => m.CreatedAt, beforeCursor.Value)
            );
        }

        // Fetch limit + 1 to check if there are more messages
        var messages = await _collection
            .Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .Limit(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > limit;
        var resultMessages = hasMore ? messages.Take(limit) : messages;

        return (resultMessages, hasMore);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Message>> SearchMessagesAsync(
        string conversationId,
        string searchQuery,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Message>.Filter;

        // Build filter for conversation, text search, and exclude soft-deleted messages
        var filter = filterBuilder.And(
            filterBuilder.Eq(m => m.ConversationId, conversationId),
            filterBuilder.Eq(m => m.IsDeleted, false),
            filterBuilder.Text(searchQuery)
        );

        var messages = await _collection
            .Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return messages;
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var filter = Builders<Message>.Filter.Eq("_id", objectId);
        var update = Builders<Message>.Update.Set(m => m.IsDeleted, true);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }
}
