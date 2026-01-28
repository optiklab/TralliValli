namespace TraliVali.Auth;

/// <summary>
/// Service for managing magic link authentication tokens
/// </summary>
public interface IMagicLinkService
{
    /// <summary>
    /// Creates a new magic link for the specified email
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="deviceId">The device ID requesting authentication</param>
    /// <returns>The magic link token</returns>
    Task<string> CreateMagicLinkAsync(string email, string deviceId);

    /// <summary>
    /// Validates and consumes a magic link token
    /// </summary>
    /// <param name="token">The magic link token</param>
    /// <returns>The magic link data if valid, null otherwise</returns>
    Task<MagicLink?> ValidateAndConsumeMagicLinkAsync(string token);
}
