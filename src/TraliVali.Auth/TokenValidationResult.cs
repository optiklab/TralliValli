using System.Security.Claims;

namespace TraliVali.Auth;

/// <summary>
/// Result of token validation
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the token is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the claims principal from the token
    /// </summary>
    public ClaimsPrincipal? Principal { get; set; }

    /// <summary>
    /// Gets or sets the error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the user ID from the token
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the device ID from the token
    /// </summary>
    public string? DeviceId { get; set; }
}
