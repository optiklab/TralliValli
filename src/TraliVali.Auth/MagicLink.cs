namespace TraliVali.Auth;

/// <summary>
/// Represents a magic link authentication token
/// </summary>
public class MagicLink
{
    /// <summary>
    /// Gets or sets the magic link token (unique identifier)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the magic link was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the magic link expires (15 minutes after creation)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the device ID requesting authentication
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
}
