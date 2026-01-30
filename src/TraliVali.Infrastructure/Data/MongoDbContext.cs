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
    /// <exception cref="ArgumentNullException">Thrown when connectionString or databaseName is null or empty</exception>
    public MongoDbContext(string connectionString, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentNullException(nameof(databaseName));

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
    /// Gets the archival stats collection
    /// </summary>
    public IMongoCollection<ArchivalStats> ArchivalStats => _database.GetCollection<ArchivalStats>("archivalStats");

    /// <summary>
    /// Gets the user key backups collection
    /// </summary>
    public IMongoCollection<UserKeyBackup> UserKeyBackups => _database.GetCollection<UserKeyBackup>("userKeyBackups");

    /// <summary>
    /// Gets the conversation keys collection
    /// </summary>
    public IMongoCollection<ConversationKey> ConversationKeys => _database.GetCollection<ConversationKey>("conversationKeys");

    /// <summary>
    /// Gets the MongoDB database instance
    /// </summary>
    public IMongoDatabase Database => _database;

    /// <summary>
    /// Creates all required indexes
    /// </summary>
    public async Task CreateIndexesAsync()
    {
        try
        {
            // Users: email (unique)
            var userEmailIndex = Builders<User>.IndexKeys.Ascending(u => u.Email);
            await Users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(userEmailIndex, new CreateIndexOptions { Unique = true }));
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            // Index already exists with different options or same keys, ignore
        }

        try
        {
            // Messages: conversationId + createdAt (compound index)
            var messageIndex = Builders<Message>.IndexKeys
                .Ascending(m => m.ConversationId)
                .Descending(m => m.CreatedAt);
            await Messages.Indexes.CreateOneAsync(new CreateIndexModel<Message>(messageIndex));
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            // Index already exists, ignore
        }

        try
        {
            // Conversations: participants.userId + lastMessageAt (compound index)
            var conversationIndex = Builders<Conversation>.IndexKeys
                .Ascending("participants.userId")
                .Descending(c => c.LastMessageAt);
            await Conversations.Indexes.CreateOneAsync(new CreateIndexModel<Conversation>(conversationIndex));
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            // Index already exists, ignore
        }

        try
        {
            // Invites: token (unique with TTL)
            var inviteTokenIndex = Builders<Invite>.IndexKeys.Ascending(i => i.Token);
            await Invites.Indexes.CreateOneAsync(
                new CreateIndexModel<Invite>(inviteTokenIndex, new CreateIndexOptions { Unique = true }));
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            // Index already exists, ignore
        }

        try
        {
            // Invites: TTL index on expiresAt
            var inviteTtlIndex = Builders<Invite>.IndexKeys.Ascending(i => i.ExpiresAt);
            await Invites.Indexes.CreateOneAsync(
                new CreateIndexModel<Invite>(inviteTtlIndex, new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            // Index already exists, ignore
        }

        try
        {
            // UserKeyBackups: userId (unique)
            var userKeyBackupIndex = Builders<UserKeyBackup>.IndexKeys.Ascending(b => b.UserId);
            await UserKeyBackups.Indexes.CreateOneAsync(
                new CreateIndexModel<UserKeyBackup>(userKeyBackupIndex, new CreateIndexOptions { Unique = true }));
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            // Index already exists, ignore
        }

        try
        {
            // ConversationKeys: conversationId (unique)
            var conversationKeyIndex = Builders<ConversationKey>.IndexKeys.Ascending(k => k.ConversationId);
            await ConversationKeys.Indexes.CreateOneAsync(
                new CreateIndexModel<ConversationKey>(conversationKeyIndex, new CreateIndexOptions { Unique = true }));
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            // Index already exists, ignore
        }
    }
}
