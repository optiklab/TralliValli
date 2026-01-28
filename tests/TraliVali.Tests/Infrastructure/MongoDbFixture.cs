using MongoDB.Driver;
using Testcontainers.MongoDb;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Tests.Infrastructure;

/// <summary>
/// Test fixture for MongoDB using Testcontainers
/// </summary>
public class MongoDbFixture : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;

    /// <summary>
    /// Gets the MongoDB context for testing
    /// </summary>
    public MongoDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the MongoDB connection string
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes the fixture by starting the MongoDB container
    /// </summary>
    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:latest")
            .Build();

        await _mongoContainer.StartAsync();

        ConnectionString = _mongoContainer.GetConnectionString();
        Context = new MongoDbContext(ConnectionString, "tralivali_test");

        // Create indexes
        await Context.CreateIndexesAsync();
    }

    /// <summary>
    /// Cleans up the fixture by stopping the MongoDB container
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.StopAsync();
            await _mongoContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Cleans up all collections in the database
    /// </summary>
    public async Task CleanupAsync()
    {
        var client = new MongoClient(ConnectionString);
        var database = client.GetDatabase("tralivali_test");
        
        await database.DropCollectionAsync("users");
        await database.DropCollectionAsync("conversations");
        await database.DropCollectionAsync("messages");
        await database.DropCollectionAsync("invites");
        await database.DropCollectionAsync("files");
        await database.DropCollectionAsync("backups");

        // Recreate indexes after cleanup
        await Context.CreateIndexesAsync();
    }
}
