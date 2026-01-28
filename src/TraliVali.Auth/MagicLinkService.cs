using System.Security.Cryptography;
using System.Text.Json;
using StackExchange.Redis;

namespace TraliVali.Auth;

/// <summary>
/// Redis-based implementation of magic link service
/// </summary>
public class MagicLinkService : IMagicLinkService
{
    private readonly IConnectionMultiplexer _redis;
    private const string MagicLinkKeyPrefix = "magiclink:";
    private static readonly TimeSpan MagicLinkExpiry = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Initializes a new instance of the <see cref="MagicLinkService"/> class
    /// </summary>
    /// <param name="redis">Redis connection multiplexer</param>
    public MagicLinkService(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    /// <inheritdoc/>
    public async Task<string> CreateMagicLinkAsync(string email, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID is required", nameof(deviceId));

        // Generate a secure random token
        var token = GenerateSecureToken();

        var magicLink = new MagicLink
        {
            Token = token,
            Email = email,
            DeviceId = deviceId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(MagicLinkExpiry)
        };

        var db = _redis.GetDatabase();
        var key = $"{MagicLinkKeyPrefix}{token}";
        var value = JsonSerializer.Serialize(magicLink);

        // Store with 15-minute expiry
        await db.StringSetAsync(key, value, MagicLinkExpiry);

        return token;
    }

    /// <inheritdoc/>
    public async Task<MagicLink?> ValidateAndConsumeMagicLinkAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var db = _redis.GetDatabase();
        var key = $"{MagicLinkKeyPrefix}{token}";

        // Get and delete in one operation (single-use)
        var value = await db.StringGetDeleteAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        try
        {
            var magicLink = JsonSerializer.Deserialize<MagicLink>(value.ToString());

            // Double-check expiry (Redis TTL should handle this, but be safe)
            if (magicLink != null && magicLink.ExpiresAt > DateTime.UtcNow)
            {
                return magicLink;
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a secure random token for magic links
    /// </summary>
    /// <returns>A URL-safe base64 encoded token</returns>
    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32]; // 256 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
