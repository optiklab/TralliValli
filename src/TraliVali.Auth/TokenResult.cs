namespace TraliVali.Auth;

/// <summary>
/// Result of token generation containing access and refresh tokens
/// </summary>
public class TokenResult
{
    /// <summary>
    /// Gets or sets the JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration time in UTC
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the refresh token expiration time in UTC
    /// </summary>
    public DateTime RefreshExpiresAt { get; set; }
}
