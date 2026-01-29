using StackExchange.Redis;
using Testcontainers.Redis;
using TraliVali.Auth;

namespace TraliVali.Tests.Auth;

/// <summary>
/// Tests for PresenceService
/// </summary>
public class PresenceServiceTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _redis;
    private PresenceService? _presenceService;

    public async Task InitializeAsync()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        var connectionString = _redisContainer.GetConnectionString();
        _redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        _presenceService = new PresenceService(_redis);
    }

    public async Task DisposeAsync()
    {
        if (_redis != null)
        {
            await _redis.CloseAsync();
            _redis.Dispose();
        }

        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task SetOnlineAsync_ShouldMarkUserAsOnline()
    {
        // Arrange
        var userId = "user123";
        var connectionId = "conn123";

        // Act
        await _presenceService!.SetOnlineAsync(userId, connectionId);

        // Assert
        var onlineUsers = await _presenceService.GetOnlineUsersAsync(new[] { userId });
        Assert.True(onlineUsers[userId]);
    }

    [Fact]
    public async Task SetOnlineAsync_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Arrange
        var connectionId = "conn123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _presenceService!.SetOnlineAsync("", connectionId)
        );
    }

    [Fact]
    public async Task SetOnlineAsync_ShouldThrowException_WhenConnectionIdIsEmpty()
    {
        // Arrange
        var userId = "user123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _presenceService!.SetOnlineAsync(userId, "")
        );
    }

    [Fact]
    public async Task SetOnlineAsync_ShouldClearLastSeen_WhenUserComesOnline()
    {
        // Arrange
        var userId = "user123";
        var connectionId1 = "conn123";
        var connectionId2 = "conn456";

        // User goes online, then offline, then online again
        await _presenceService!.SetOnlineAsync(userId, connectionId1);
        await _presenceService.SetOfflineAsync(userId, connectionId1);
        
        // Verify last-seen is set
        var lastSeen = await _presenceService.GetLastSeenAsync(userId);
        Assert.NotNull(lastSeen);

        // Act - User comes back online
        await _presenceService.SetOnlineAsync(userId, connectionId2);

        // Assert - Last-seen should be cleared
        lastSeen = await _presenceService.GetLastSeenAsync(userId);
        Assert.Null(lastSeen);
    }

    [Fact]
    public async Task SetOfflineAsync_ShouldMarkUserAsOffline()
    {
        // Arrange
        var userId = "user123";
        var connectionId = "conn123";
        await _presenceService!.SetOnlineAsync(userId, connectionId);

        // Act
        await _presenceService.SetOfflineAsync(userId, connectionId);

        // Assert
        var onlineUsers = await _presenceService.GetOnlineUsersAsync(new[] { userId });
        Assert.False(onlineUsers[userId]);
    }

    [Fact]
    public async Task SetOfflineAsync_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Arrange
        var connectionId = "conn123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _presenceService!.SetOfflineAsync("", connectionId)
        );
    }

    [Fact]
    public async Task SetOfflineAsync_ShouldThrowException_WhenConnectionIdIsEmpty()
    {
        // Arrange
        var userId = "user123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _presenceService!.SetOfflineAsync(userId, "")
        );
    }

    [Fact]
    public async Task SetOfflineAsync_ShouldSetLastSeenTimestamp()
    {
        // Arrange
        var userId = "user123";
        var connectionId = "conn123";
        await _presenceService!.SetOnlineAsync(userId, connectionId);

        var beforeOffline = DateTime.UtcNow;

        // Act
        await _presenceService.SetOfflineAsync(userId, connectionId);

        // Assert
        var lastSeen = await _presenceService.GetLastSeenAsync(userId);
        Assert.NotNull(lastSeen);
        Assert.True(lastSeen >= beforeOffline.AddSeconds(-1)); // Allow small time difference
        Assert.True(lastSeen <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task SetOfflineAsync_ShouldKeepUserOnline_WhenMultipleConnections()
    {
        // Arrange
        var userId = "user123";
        var connectionId1 = "conn123";
        var connectionId2 = "conn456";

        // User connects from two devices
        await _presenceService!.SetOnlineAsync(userId, connectionId1);
        await _presenceService.SetOnlineAsync(userId, connectionId2);

        // Act - Disconnect one connection
        await _presenceService.SetOfflineAsync(userId, connectionId1);

        // Assert - User should still be online
        var onlineUsers = await _presenceService.GetOnlineUsersAsync(new[] { userId });
        Assert.True(onlineUsers[userId]);

        // Last-seen should not be set yet
        var lastSeen = await _presenceService.GetLastSeenAsync(userId);
        Assert.Null(lastSeen);
    }

    [Fact]
    public async Task SetOfflineAsync_ShouldMarkOffline_WhenLastConnectionDisconnects()
    {
        // Arrange
        var userId = "user123";
        var connectionId1 = "conn123";
        var connectionId2 = "conn456";

        await _presenceService!.SetOnlineAsync(userId, connectionId1);
        await _presenceService.SetOnlineAsync(userId, connectionId2);
        await _presenceService.SetOfflineAsync(userId, connectionId1);

        // Act - Disconnect last connection
        await _presenceService.SetOfflineAsync(userId, connectionId2);

        // Assert - User should be offline
        var onlineUsers = await _presenceService.GetOnlineUsersAsync(new[] { userId });
        Assert.False(onlineUsers[userId]);

        // Last-seen should be set
        var lastSeen = await _presenceService.GetLastSeenAsync(userId);
        Assert.NotNull(lastSeen);
    }

    [Fact]
    public async Task GetOnlineUsersAsync_ShouldReturnEmptyDictionary_ForEmptyArray()
    {
        // Act
        var result = await _presenceService!.GetOnlineUsersAsync(Array.Empty<string>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOnlineUsersAsync_ShouldThrowException_WhenArrayIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _presenceService!.GetOnlineUsersAsync(null!)
        );
    }

    [Fact]
    public async Task GetOnlineUsersAsync_ShouldReturnCorrectStatus_ForMultipleUsers()
    {
        // Arrange
        var user1 = "user1";
        var user2 = "user2";
        var user3 = "user3";

        await _presenceService!.SetOnlineAsync(user1, "conn1");
        await _presenceService.SetOnlineAsync(user2, "conn2");
        // user3 never came online

        // Act
        var result = await _presenceService.GetOnlineUsersAsync(new[] { user1, user2, user3 });

        // Assert
        Assert.True(result[user1]);
        Assert.True(result[user2]);
        Assert.False(result[user3]);
    }

    [Fact]
    public async Task GetOnlineUsersAsync_ShouldSkipEmptyUserIds()
    {
        // Arrange
        var user1 = "user1";
        await _presenceService!.SetOnlineAsync(user1, "conn1");

        // Act
        var result = await _presenceService.GetOnlineUsersAsync(new[] { user1, "", "  " });

        // Assert
        Assert.Single(result);
        Assert.True(result[user1]);
    }

    [Fact]
    public async Task GetLastSeenAsync_ShouldReturnNull_ForOnlineUser()
    {
        // Arrange
        var userId = "user123";
        var connectionId = "conn123";
        await _presenceService!.SetOnlineAsync(userId, connectionId);

        // Act
        var lastSeen = await _presenceService.GetLastSeenAsync(userId);

        // Assert
        Assert.Null(lastSeen);
    }

    [Fact]
    public async Task GetLastSeenAsync_ShouldReturnNull_ForNeverSeenUser()
    {
        // Arrange
        var userId = "user123";

        // Act
        var lastSeen = await _presenceService!.GetLastSeenAsync(userId);

        // Assert
        Assert.Null(lastSeen);
    }

    [Fact]
    public async Task GetLastSeenAsync_ShouldReturnNull_ForEmptyUserId()
    {
        // Act
        var lastSeen = await _presenceService!.GetLastSeenAsync("");

        // Assert
        Assert.Null(lastSeen);
    }

    [Fact]
    public async Task GetLastSeenAsync_ShouldReturnTimestamp_ForOfflineUser()
    {
        // Arrange
        var userId = "user123";
        var connectionId = "conn123";
        await _presenceService!.SetOnlineAsync(userId, connectionId);
        
        var beforeOffline = DateTime.UtcNow;
        await _presenceService.SetOfflineAsync(userId, connectionId);
        var afterOffline = DateTime.UtcNow;

        // Act
        var lastSeen = await _presenceService.GetLastSeenAsync(userId);

        // Assert
        Assert.NotNull(lastSeen);
        Assert.True(lastSeen >= beforeOffline.AddSeconds(-1));
        Assert.True(lastSeen <= afterOffline.AddSeconds(1));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenRedisIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PresenceService(null!));
    }

    [Fact]
    public async Task PresenceTracking_ShouldHandleComplexScenario()
    {
        // Arrange - Multiple users with multiple connections
        var user1 = "user1";
        var user2 = "user2";
        var user3 = "user3";

        // Act - Complex scenario
        // User1 connects from device1
        await _presenceService!.SetOnlineAsync(user1, "conn1");
        
        // User2 connects from device1
        await _presenceService.SetOnlineAsync(user2, "conn2");
        
        // User1 connects from device2 (now has 2 connections)
        await _presenceService.SetOnlineAsync(user1, "conn1b");
        
        // User3 was online but went offline
        await _presenceService.SetOnlineAsync(user3, "conn3");
        await _presenceService.SetOfflineAsync(user3, "conn3");
        
        // User1 disconnects device1
        await _presenceService.SetOfflineAsync(user1, "conn1");

        // Assert
        var onlineStatus = await _presenceService.GetOnlineUsersAsync(new[] { user1, user2, user3 });
        Assert.True(onlineStatus[user1]); // Still online from device2
        Assert.True(onlineStatus[user2]); // Still online
        Assert.False(onlineStatus[user3]); // Offline

        var lastSeen1 = await _presenceService.GetLastSeenAsync(user1);
        var lastSeen2 = await _presenceService.GetLastSeenAsync(user2);
        var lastSeen3 = await _presenceService.GetLastSeenAsync(user3);

        Assert.Null(lastSeen1); // Online
        Assert.Null(lastSeen2); // Online
        Assert.NotNull(lastSeen3); // Offline with timestamp
    }
}
