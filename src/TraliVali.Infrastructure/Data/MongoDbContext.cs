using MongoDB.Driver;
using TraliVali.Domain.Entities;

namespace TraliVali.Infrastructure.Data;

/// <summary>
/// MongoDB database context
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbContext"/> class
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string</param>
    /// <param name="databaseName">The database name</param>
    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    /// <summary>
    /// Gets the users collection
    /// </summary>
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");

    /// <summary>
    /// Gets the conversations collection
    /// </summary>
    public IMongoCollection<Conversation> Conversations => _database.GetCollection<Conversation>("conversations");

    /// <summary>
    /// Gets the messages collection
    /// </summary>
    public IMongoCollection<Message> Messages => _database.GetCollection<Message>("messages");

    /// <summary>
    /// Gets the invites collection
    /// </summary>
    public IMongoCollection<Invite> Invites => _database.GetCollection<Invite>("invites");

    /// <summary>
    /// Gets the files collection
    /// </summary>
    public IMongoCollection<Domain.Entities.File> Files => _database.GetCollection<Domain.Entities.File>("files");

    /// <summary>
    /// Gets the backups collection
    /// </summary>
    public IMongoCollection<Backup> Backups => _database.GetCollection<Backup>("backups");

    /// <summary>
    /// Creates all required indexes
    /// </summary>
    public async Task CreateIndexesAsync()
    {
        // Users: email (unique)
        var userEmailIndex = Builders<User>.IndexKeys.Ascending(u => u.Email);
        await Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(userEmailIndex, new CreateIndexOptions { Unique = true }));

        // Messages: conversationId + createdAt (compound index)
        var messageIndex = Builders<Message>.IndexKeys
            .Ascending(m => m.ConversationId)
            .Descending(m => m.CreatedAt);
        await Messages.Indexes.CreateOneAsync(new CreateIndexModel<Message>(messageIndex));

        // Conversations: participants.userId + lastMessageAt (compound index)
        var conversationIndex = Builders<Conversation>.IndexKeys
            .Ascending("participants.userId")
            .Descending(c => c.LastMessageAt);
        await Conversations.Indexes.CreateOneAsync(new CreateIndexModel<Conversation>(conversationIndex));

        // Invites: token (unique with TTL)
        var inviteTokenIndex = Builders<Invite>.IndexKeys.Ascending(i => i.Token);
        await Invites.Indexes.CreateOneAsync(
            new CreateIndexModel<Invite>(inviteTokenIndex, new CreateIndexOptions { Unique = true }));

        // Invites: TTL index on expiresAt
        var inviteTtlIndex = Builders<Invite>.IndexKeys.Ascending(i => i.ExpiresAt);
        await Invites.Indexes.CreateOneAsync(
            new CreateIndexModel<Invite>(inviteTtlIndex, new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
    }
}
