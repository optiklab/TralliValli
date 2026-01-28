using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace TraliVali.Auth;

/// <summary>
/// Redis-based implementation of token blacklist service
/// </summary>
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private const string BlacklistKeyPrefix = "token:blacklist:";

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenBlacklistService"/> class
    /// </summary>
    /// <param name="redis">Redis connection multiplexer</param>
    public TokenBlacklistService(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    /// <inheritdoc/>
    public async Task BlacklistTokenAsync(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        var db = _redis.GetDatabase();
        var tokenHash = HashToken(token);
        var key = $"{BlacklistKeyPrefix}{tokenHash}";
        
        // Calculate time until expiration
        var ttl = expiresAt - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
        {
            // Store the token hash with expiration
            await db.StringSetAsync(key, "1", ttl);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var db = _redis.GetDatabase();
        var tokenHash = HashToken(token);
        var key = $"{BlacklistKeyPrefix}{tokenHash}";
        
        return await db.KeyExistsAsync(key);
    }

    /// <summary>
    /// Hashes a token to reduce storage size
    /// </summary>
    /// <param name="token">The token to hash</param>
    /// <returns>The SHA256 hash of the token</returns>
    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
