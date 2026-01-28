using TraliVali.Domain.Entities;

namespace TraliVali.Auth;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates an access token and refresh token for the specified user and device
    /// </summary>
    /// <param name="user">The user to generate the token for</param>
    /// <param name="deviceId">The device ID to include in the token</param>
    /// <returns>A token result containing the access token and refresh token</returns>
    TokenResult GenerateToken(User user, string deviceId);

    /// <summary>
    /// Validates a JWT token and returns the validation result
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>The validation result containing claims if valid</returns>
    Task<TokenValidationResult> ValidateTokenAsync(string token);

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <returns>A new token result with rotated refresh token</returns>
    Task<TokenResult?> RefreshTokenAsync(string refreshToken);
}
