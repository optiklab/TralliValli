namespace TraliVali.Auth;

/// <summary>
/// Service for managing blacklisted tokens
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist
    /// </summary>
    /// <param name="token">The token to blacklist</param>
    /// <param name="expiresAt">When the token expires</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task BlacklistTokenAsync(string token, DateTime expiresAt);

    /// <summary>
    /// Checks if a token is blacklisted
    /// </summary>
    /// <param name="token">The token to check</param>
    /// <returns>True if the token is blacklisted, false otherwise</returns>
    Task<bool> IsTokenBlacklistedAsync(string token);
}
