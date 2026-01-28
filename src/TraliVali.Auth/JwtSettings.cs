namespace TraliVali.Auth;

/// <summary>
/// Configuration settings for JWT token generation and validation
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Gets or sets the RSA private key in PEM format for signing tokens
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the RSA public key in PEM format for validating tokens
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token issuer
    /// </summary>
    public string Issuer { get; set; } = "TraliVali";

    /// <summary>
    /// Gets or sets the token audience
    /// </summary>
    public string Audience { get; set; } = "TraliVali";

    /// <summary>
    /// Gets or sets the token expiration in days (default: 7 days)
    /// </summary>
    public int ExpirationDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the refresh token expiration in days (default: 30 days)
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
