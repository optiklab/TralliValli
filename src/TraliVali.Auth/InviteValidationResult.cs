namespace TraliVali.Auth;

/// <summary>
/// Represents the result of an invite validation
/// </summary>
public class InviteValidationResult
{
    /// <summary>
    /// Gets or sets the invite token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the inviter's user ID
    /// </summary>
    public string InviterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the invite was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the invite expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether the invite has been used
    /// </summary>
    public bool IsUsed { get; set; }
}
