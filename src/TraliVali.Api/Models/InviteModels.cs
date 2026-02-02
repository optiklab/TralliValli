using System.ComponentModel.DataAnnotations;

namespace TraliVali.Api.Models;

/// <summary>
/// Request model for generating an invite
/// </summary>
public class GenerateInviteRequest
{
    /// <summary>
    /// Gets or sets the number of hours until the invite expires
    /// </summary>
    [Required]
    [Range(1, 168, ErrorMessage = "Expiry hours must be between 1 and 168 (1 week)")]
    public int ExpiryHours { get; set; } = 24;
}

/// <summary>
/// Response model for invite generation
/// </summary>
public class GenerateInviteResponse
{
    /// <summary>
    /// Gets or sets the invite code/token
    /// </summary>
    public string InviteCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full invite link URL
    /// </summary>
    public string InviteLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the QR code as a data URL
    /// </summary>
    public string QrCodeDataUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the invite expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
