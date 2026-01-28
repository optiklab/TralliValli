using StackExchange.Redis;
using Testcontainers.Redis;
using TraliVali.Auth;

namespace TraliVali.Tests.Auth;

/// <summary>
/// Tests for TokenBlacklistService
/// </summary>
public class TokenBlacklistServiceTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _redis;
    private TokenBlacklistService? _blacklistService;

    public async Task InitializeAsync()
    {
        _redisContainer = new RedisBuilder().Build();

        await _redisContainer.StartAsync();

        var connectionString = _redisContainer.GetConnectionString();
        _redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        _blacklistService = new TokenBlacklistService(_redis);
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
    public async Task BlacklistTokenAsync_ShouldBlacklistToken()
    {
        // Arrange
        var token = "test.token.123";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        await _blacklistService!.BlacklistTokenAsync(token, expiresAt);

        // Assert
        var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(token);
        Assert.True(isBlacklisted);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_ShouldReturnFalse_ForNonBlacklistedToken()
    {
        // Arrange
        var token = "non.blacklisted.token";

        // Act
        var isBlacklisted = await _blacklistService!.IsTokenBlacklistedAsync(token);

        // Assert
        Assert.False(isBlacklisted);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_ShouldReturnFalse_ForEmptyToken()
    {
        // Act
        var isBlacklisted = await _blacklistService!.IsTokenBlacklistedAsync("");

        // Assert
        Assert.False(isBlacklisted);
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldThrowException_WhenTokenIsEmpty()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _blacklistService!.BlacklistTokenAsync("", expiresAt)
        );
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldExpireToken_AfterTtl()
    {
        // Arrange
        var token = "expiring.token.123";
        var expiresAt = DateTime.UtcNow.AddSeconds(2);

        // Act
        await _blacklistService!.BlacklistTokenAsync(token, expiresAt);
        
        // Verify it's blacklisted initially
        var isBlacklistedBefore = await _blacklistService.IsTokenBlacklistedAsync(token);
        Assert.True(isBlacklistedBefore);

        // Wait for expiration
        await Task.Delay(3000);

        // Assert - Token should no longer be blacklisted
        var isBlacklistedAfter = await _blacklistService.IsTokenBlacklistedAsync(token);
        Assert.False(isBlacklistedAfter);
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldNotStore_WhenTokenAlreadyExpired()
    {
        // Arrange
        var token = "already.expired.token";
        var expiresAt = DateTime.UtcNow.AddHours(-1); // Already expired

        // Act
        await _blacklistService!.BlacklistTokenAsync(token, expiresAt);

        // Assert - Should not be stored
        var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(token);
        Assert.False(isBlacklisted);
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldHandleMultipleTokens()
    {
        // Arrange
        var token1 = "token.one.123";
        var token2 = "token.two.456";
        var token3 = "token.three.789";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        await _blacklistService!.BlacklistTokenAsync(token1, expiresAt);
        await _blacklistService.BlacklistTokenAsync(token2, expiresAt);
        await _blacklistService.BlacklistTokenAsync(token3, expiresAt);

        // Assert
        Assert.True(await _blacklistService.IsTokenBlacklistedAsync(token1));
        Assert.True(await _blacklistService.IsTokenBlacklistedAsync(token2));
        Assert.True(await _blacklistService.IsTokenBlacklistedAsync(token3));
        Assert.False(await _blacklistService.IsTokenBlacklistedAsync("token.four.000"));
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldHashTokens()
    {
        // Arrange
        var longToken = new string('a', 1000) + ".token.data";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        await _blacklistService!.BlacklistTokenAsync(longToken, expiresAt);

        // Assert - Should be able to check blacklisted status
        var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(longToken);
        Assert.True(isBlacklisted);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenRedisIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TokenBlacklistService(null!));
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_ShouldReturnFalse_ForWhitespaceToken()
    {
        // Act
        var isBlacklisted = await _blacklistService!.IsTokenBlacklistedAsync("   ");

        // Assert
        Assert.False(isBlacklisted);
    }
}
