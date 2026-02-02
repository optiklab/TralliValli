using System.ComponentModel.DataAnnotations;

namespace TraliVali.Api.Models;

/// <summary>
/// Request model for requesting a magic link
/// </summary>
public class RequestMagicLinkRequest
{
    /// <summary>
    /// Gets or sets the email address to send the magic link to
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device ID requesting authentication
    /// </summary>
    [Required]
    public string DeviceId { get; set; } = string.Empty;
}

/// <summary>
/// Response model for magic link request
/// </summary>
public class RequestMagicLinkResponse
{
    /// <summary>
    /// Gets or sets a message indicating the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request model for verifying a magic link
/// </summary>
public class VerifyMagicLinkRequest
{
    /// <summary>
    /// Gets or sets the magic link token to verify
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Response model for magic link verification
/// </summary>
public class VerifyMagicLinkResponse
{
    /// <summary>
    /// Gets or sets the access token (JWT)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the access token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when the refresh token expires
    /// </summary>
    public DateTime RefreshExpiresAt { get; set; }
}

/// <summary>
/// Request model for refreshing a token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Response model for token refresh
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>
    /// Gets or sets the new access token (JWT)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the new access token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when the new refresh token expires
    /// </summary>
    public DateTime RefreshExpiresAt { get; set; }
}

/// <summary>
/// Request model for logout
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// Gets or sets the access token to blacklist
    /// </summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}

/// <summary>
/// Response model for logout
/// </summary>
public class LogoutResponse
{
    /// <summary>
    /// Gets or sets a message indicating the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the invite token (optional when system is not bootstrapped)
    /// </summary>
    public string? InviteToken { get; set; }

    /// <summary>
    /// Gets or sets the email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Response model for user registration
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// Gets or sets the access token (JWT)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the access token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when the refresh token expires
    /// </summary>
    public DateTime RefreshExpiresAt { get; set; }
}

/// <summary>
/// Response model for invite validation
/// </summary>
public class ValidateInviteResponse
{
    /// <summary>
    /// Gets or sets whether the invite is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets when the invite expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets a message describing the validation result
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Response model for system status
/// </summary>
public class SystemStatusResponse
{
    /// <summary>
    /// Gets or sets whether the system has been bootstrapped (has at least one user)
    /// </summary>
    public bool IsBootstrapped { get; set; }

    /// <summary>
    /// Gets or sets whether registration requires an invite
    /// </summary>
    public bool RequiresInvite { get; set; }
}
