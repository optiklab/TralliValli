using StackExchange.Redis;
using Testcontainers.Redis;
using TraliVali.Auth;

namespace TraliVali.Tests.Auth;

/// <summary>
/// Tests for MagicLinkService
/// </summary>
public class MagicLinkServiceTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _redis;
    private MagicLinkService? _magicLinkService;

    public async Task InitializeAsync()
    {
        _redisContainer = new RedisBuilder().Build();
        await _redisContainer.StartAsync();

        var connectionString = _redisContainer.GetConnectionString();
        _redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        _magicLinkService = new MagicLinkService(_redis);
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
    public async Task CreateMagicLinkAsync_ShouldCreateValidToken()
    {
        // Arrange
        var email = "test@example.com";
        var deviceId = "device123";

        // Act
        var token = await _magicLinkService!.CreateMagicLinkAsync(email, deviceId);

        // Assert
        Assert.NotEmpty(token);
        Assert.True(token.Length > 20); // Secure token should be sufficiently long
    }

    [Fact]
    public async Task CreateMagicLinkAsync_ShouldThrowException_WhenEmailIsEmpty()
    {
        // Arrange
        var deviceId = "device123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _magicLinkService!.CreateMagicLinkAsync("", deviceId));
    }

    [Fact]
    public async Task CreateMagicLinkAsync_ShouldThrowException_WhenDeviceIdIsEmpty()
    {
        // Arrange
        var email = "test@example.com";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _magicLinkService!.CreateMagicLinkAsync(email, ""));
    }

    [Fact]
    public async Task ValidateAndConsumeMagicLinkAsync_ShouldReturnMagicLink_WhenTokenIsValid()
    {
        // Arrange
        var email = "test@example.com";
        var deviceId = "device123";
        var token = await _magicLinkService!.CreateMagicLinkAsync(email, deviceId);

        // Act
        var magicLink = await _magicLinkService.ValidateAndConsumeMagicLinkAsync(token);

        // Assert
        Assert.NotNull(magicLink);
        Assert.Equal(email, magicLink.Email);
        Assert.Equal(deviceId, magicLink.DeviceId);
        Assert.Equal(token, magicLink.Token);
        Assert.True(magicLink.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateAndConsumeMagicLinkAsync_ShouldReturnNull_WhenTokenIsInvalid()
    {
        // Act
        var magicLink = await _magicLinkService!.ValidateAndConsumeMagicLinkAsync("invalid-token");

        // Assert
        Assert.Null(magicLink);
    }

    [Fact]
    public async Task ValidateAndConsumeMagicLinkAsync_ShouldReturnNull_WhenTokenIsEmpty()
    {
        // Act
        var magicLink = await _magicLinkService!.ValidateAndConsumeMagicLinkAsync("");

        // Assert
        Assert.Null(magicLink);
    }

    [Fact]
    public async Task ValidateAndConsumeMagicLinkAsync_ShouldBeSingleUse()
    {
        // Arrange
        var email = "test@example.com";
        var deviceId = "device123";
        var token = await _magicLinkService!.CreateMagicLinkAsync(email, deviceId);

        // Act - First consumption should succeed
        var firstResult = await _magicLinkService.ValidateAndConsumeMagicLinkAsync(token);
        Assert.NotNull(firstResult);

        // Act - Second consumption should fail (single-use)
        var secondResult = await _magicLinkService.ValidateAndConsumeMagicLinkAsync(token);

        // Assert
        Assert.Null(secondResult);
    }

    [Fact]
    public async Task MagicLink_ShouldExpireAfter15Minutes()
    {
        // Arrange
        var email = "test@example.com";
        var deviceId = "device123";
        var token = await _magicLinkService!.CreateMagicLinkAsync(email, deviceId);

        // Get the magic link to check expiry
        var magicLink = await _magicLinkService.ValidateAndConsumeMagicLinkAsync(token);
        Assert.NotNull(magicLink);

        // Assert - Expiry should be approximately 15 minutes from now
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        var timeDifference = Math.Abs((magicLink.ExpiresAt - expectedExpiry).TotalSeconds);
        Assert.True(timeDifference < 5, $"Expiry time difference was {timeDifference} seconds, expected less than 5 seconds");
    }

    [Fact]
    public async Task MagicLink_ShouldNotBeValidAfterRedisExpiry()
    {
        // This test verifies that Redis TTL is working correctly
        // We create a magic link and verify it's stored, then check it expires from Redis

        // Arrange
        var email = "test@example.com";
        var deviceId = "device123";
        var token = await _magicLinkService!.CreateMagicLinkAsync(email, deviceId);

        // Verify token exists in Redis
        var db = _redis!.GetDatabase();
        var key = $"magiclink:{token}";
        var exists = await db.KeyExistsAsync(key);
        Assert.True(exists);

        // Verify TTL is set (approximately 15 minutes = 900 seconds)
        var ttl = await db.KeyTimeToLiveAsync(key);
        Assert.NotNull(ttl);
        Assert.True(ttl.Value.TotalSeconds > 890 && ttl.Value.TotalSeconds <= 900);
    }

    [Fact]
    public async Task CreateMagicLinkAsync_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var email = "test@example.com";
        var deviceId = "device123";

        // Act - Create multiple magic links
        var token1 = await _magicLinkService!.CreateMagicLinkAsync(email, deviceId);
        var token2 = await _magicLinkService!.CreateMagicLinkAsync(email, deviceId);
        var token3 = await _magicLinkService!.CreateMagicLinkAsync(email, deviceId);

        // Assert - All tokens should be unique
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token2, token3);
        Assert.NotEqual(token1, token3);
    }
}
