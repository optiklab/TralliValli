using StackExchange.Redis;

namespace TraliVali.Auth;

/// <summary>
/// Redis-based implementation of presence tracking service
/// Uses sorted sets to track online users with timestamps
/// </summary>
public class PresenceService : IPresenceService
{
    private readonly IConnectionMultiplexer _redis;
    private const string OnlineUsersKey = "presence:online";
    private const string UserConnectionsPrefix = "presence:connections:";
    private const string LastSeenPrefix = "presence:lastseen:";

    /// <summary>
    /// Initializes a new instance of the <see cref="PresenceService"/> class
    /// </summary>
    /// <param name="redis">Redis connection multiplexer</param>
    public PresenceService(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    /// <inheritdoc/>
    public async Task SetOnlineAsync(string userId, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("Connection ID is required", nameof(connectionId));

        var db = _redis.GetDatabase();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Add user to online sorted set with current timestamp as score
        await db.SortedSetAddAsync(OnlineUsersKey, userId, timestamp);
        
        // Track this connection for the user (in case of multiple connections)
        var connectionsKey = $"{UserConnectionsPrefix}{userId}";
        await db.SetAddAsync(connectionsKey, connectionId);
        
        // Remove last-seen timestamp when user comes online
        var lastSeenKey = $"{LastSeenPrefix}{userId}";
        await db.KeyDeleteAsync(lastSeenKey);
    }

    /// <inheritdoc/>
    public async Task SetOfflineAsync(string userId, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("Connection ID is required", nameof(connectionId));

        var db = _redis.GetDatabase();
        var connectionsKey = $"{UserConnectionsPrefix}{userId}";
        
        // Remove this specific connection
        await db.SetRemoveAsync(connectionsKey, connectionId);
        
        // Check if user has any other active connections
        var remainingConnections = await db.SetLengthAsync(connectionsKey);
        
        if (remainingConnections == 0)
        {
            // No more connections - mark user as offline
            await db.SortedSetRemoveAsync(OnlineUsersKey, userId);
            
            // Store last-seen timestamp
            var lastSeenKey = $"{LastSeenPrefix}{userId}";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await db.StringSetAsync(lastSeenKey, timestamp);
            
            // Clean up connections key
            await db.KeyDeleteAsync(connectionsKey);
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, bool>> GetOnlineUsersAsync(string[] userIds)
    {
        if (userIds == null)
            throw new ArgumentNullException(nameof(userIds));

        var result = new Dictionary<string, bool>();
        
        if (userIds.Length == 0)
            return result;

        var db = _redis.GetDatabase();
        
        // Check each user's presence in the sorted set
        foreach (var userId in userIds)
        {
            if (string.IsNullOrWhiteSpace(userId))
                continue;
                
            var score = await db.SortedSetScoreAsync(OnlineUsersKey, userId);
            result[userId] = score.HasValue;
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<DateTime?> GetLastSeenAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var db = _redis.GetDatabase();
        
        // First check if user is currently online
        var isOnline = await db.SortedSetScoreAsync(OnlineUsersKey, userId);
        if (isOnline.HasValue)
            return null; // User is online, no last-seen

        // Get last-seen timestamp
        var lastSeenKey = $"{LastSeenPrefix}{userId}";
        var timestamp = await db.StringGetAsync(lastSeenKey);
        
        if (timestamp.IsNullOrEmpty)
            return null; // Never been online or no record

        if (long.TryParse(timestamp, out var unixTimestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }

        return null;
    }
}
