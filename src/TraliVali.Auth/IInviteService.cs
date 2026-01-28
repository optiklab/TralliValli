namespace TraliVali.Auth;

/// <summary>
/// Service for managing invite links and QR codes
/// </summary>
public interface IInviteService
{
    /// <summary>
    /// Generates a signed invite link for the specified inviter
    /// </summary>
    /// <param name="inviterId">The ID of the user creating the invite</param>
    /// <param name="expiryHours">Number of hours until the invite expires</param>
    /// <returns>The invite link token</returns>
    Task<string> GenerateInviteLinkAsync(string inviterId, int expiryHours);

    /// <summary>
    /// Generates a QR code image for the invite link
    /// </summary>
    /// <param name="inviteLink">The invite link to encode</param>
    /// <returns>The QR code as a base64-encoded PNG image</returns>
    string GenerateInviteQrCode(string inviteLink);

    /// <summary>
    /// Validates an invite token
    /// </summary>
    /// <param name="token">The invite token to validate</param>
    /// <returns>The invite details if valid, null otherwise</returns>
    Task<InviteValidationResult?> ValidateInviteAsync(string token);

    /// <summary>
    /// Redeems an invite token for a user
    /// </summary>
    /// <param name="token">The invite token to redeem</param>
    /// <param name="userId">The ID of the user redeeming the invite</param>
    /// <returns>True if the invite was successfully redeemed, false otherwise</returns>
    Task<bool> RedeemInviteAsync(string token, string userId);
}
