using System.Security.Cryptography;
using System.Text;
using QRCoder;
using TraliVali.Domain.Entities;
using MongoDB.Driver;

namespace TraliVali.Auth;

/// <summary>
/// MongoDB-based implementation of invite service with HMAC-SHA256 signed tokens and QR code generation
/// </summary>
public class InviteService : IInviteService
{
    private readonly IMongoCollection<Invite> _invites;
    private readonly string _signingKey;
    private const int TokenLength = 32; // 256 bits

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteService"/> class
    /// </summary>
    /// <param name="invites">The MongoDB invites collection</param>
    /// <param name="signingKey">The key for HMAC-SHA256 signing</param>
    public InviteService(IMongoCollection<Invite> invites, string signingKey)
    {
        _invites = invites ?? throw new ArgumentNullException(nameof(invites));
        if (string.IsNullOrWhiteSpace(signingKey))
            throw new ArgumentException("Signing key is required", nameof(signingKey));
        _signingKey = signingKey;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateInviteLinkAsync(string inviterId, int expiryHours)
    {
        if (string.IsNullOrWhiteSpace(inviterId))
            throw new ArgumentException("Inviter ID is required", nameof(inviterId));
        if (expiryHours <= 0)
            throw new ArgumentException("Expiry hours must be positive", nameof(expiryHours));

        // Generate a secure random token
        var token = GenerateSecureToken();
        
        // Truncate to millisecond precision to match MongoDB storage precision
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);
        expiresAt = new DateTime(expiresAt.Ticks - (expiresAt.Ticks % TimeSpan.TicksPerMillisecond), expiresAt.Kind);
        
        // Sign the token with HMAC-SHA256 (include timestamp for security)
        var signature = SignToken(token, inviterId, expiresAt);
        var signedToken = $"{token}.{signature}";

        // Create the invite entity
        var invite = new Invite
        {
            Token = signedToken,
            Email = "invite@system.local", // Use placeholder email for system-generated invites
            InviterId = inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        // Store in MongoDB
        await _invites.InsertOneAsync(invite);

        return signedToken;
    }

    /// <inheritdoc/>
    public string GenerateInviteQrCode(string inviteLink)
    {
        if (string.IsNullOrWhiteSpace(inviteLink))
            throw new ArgumentException("Invite link is required", nameof(inviteLink));

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(inviteLink, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        var qrCodeImage = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeImage);
    }

    /// <inheritdoc/>
    public async Task<InviteValidationResult?> ValidateInviteAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        // Parse the token to get the parts
        var parts = token.Split('.');
        if (parts.Length != 2)
            return null;

        // Find the invite in the database first
        var filter = Builders<Invite>.Filter.Eq(i => i.Token, token);
        var invite = await _invites.Find(filter).FirstOrDefaultAsync();

        if (invite == null)
            return null;

        // Verify the token signature using stored data
        if (!VerifyTokenSignature(token, invite.InviterId, invite.ExpiresAt))
            return null;

        // Check if expired
        if (invite.ExpiresAt <= DateTime.UtcNow)
            return null;

        // Check if already used
        if (invite.IsUsed)
            return null;

        return new InviteValidationResult
        {
            Token = invite.Token,
            InviterId = invite.InviterId,
            CreatedAt = invite.CreatedAt,
            ExpiresAt = invite.ExpiresAt,
            IsUsed = invite.IsUsed
        };
    }

    /// <inheritdoc/>
    public async Task<bool> RedeemInviteAsync(string token, string userId)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        // Parse the token to get the parts
        var parts = token.Split('.');
        if (parts.Length != 2)
            return false;

        // Atomic update with all validation checks in the filter
        var filter = Builders<Invite>.Filter.And(
            Builders<Invite>.Filter.Eq(i => i.Token, token),
            Builders<Invite>.Filter.Eq(i => i.IsUsed, false),
            Builders<Invite>.Filter.Gt(i => i.ExpiresAt, DateTime.UtcNow)
        );
        
        var update = Builders<Invite>.Update
            .Set(i => i.IsUsed, true)
            .Set(i => i.UsedBy, userId)
            .Set(i => i.UsedAt, DateTime.UtcNow);

        var result = await _invites.UpdateOneAsync(filter, update);
        
        // If update succeeded, verify the signature
        if (result.IsAcknowledged && result.ModifiedCount > 0)
        {
            // Retrieve the invite to verify signature
            var inviteFilter = Builders<Invite>.Filter.Eq(i => i.Token, token);
            var invite = await _invites.Find(inviteFilter).FirstOrDefaultAsync();
            
            if (invite != null && !VerifyTokenSignature(token, invite.InviterId, invite.ExpiresAt))
            {
                // Signature verification failed - rollback by marking as unused
                var rollbackUpdate = Builders<Invite>.Update
                    .Set(i => i.IsUsed, false)
                    .Set(i => i.UsedBy, null)
                    .Set(i => i.UsedAt, null);
                await _invites.UpdateOneAsync(inviteFilter, rollbackUpdate);
                return false;
            }
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// Generates a secure random token for invite links
    /// </summary>
    /// <returns>A URL-safe base64 encoded token</returns>
    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Signs a token with HMAC-SHA256
    /// </summary>
    /// <param name="token">The token to sign</param>
    /// <param name="inviterId">The inviter ID</param>
    /// <param name="expiresAt">The expiration timestamp</param>
    /// <returns>The signature</returns>
    private string SignToken(string token, string inviterId, DateTime expiresAt)
    {
        // Include timestamp (ticks) instead of hours for immutable signature
        var data = $"{token}:{inviterId}:{expiresAt.Ticks}";
        var keyBytes = Encoding.UTF8.GetBytes(_signingKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        var signatureBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(signatureBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Verifies the signature of a token
    /// </summary>
    /// <param name="signedToken">The signed token to verify</param>
    /// <param name="inviterId">The inviter ID from the database</param>
    /// <param name="expiresAt">The expiration timestamp from the database</param>
    /// <returns>True if the signature is valid, false otherwise</returns>
    private bool VerifyTokenSignature(string signedToken, string inviterId, DateTime expiresAt)
    {
        var parts = signedToken.Split('.');
        if (parts.Length != 2)
            return false;

        var token = parts[0];
        var providedSignature = parts[1];

        // Truncate to millisecond precision to match what was signed
        expiresAt = new DateTime(expiresAt.Ticks - (expiresAt.Ticks % TimeSpan.TicksPerMillisecond), expiresAt.Kind);

        // Recompute the signature using the stored data
        var expectedSignature = SignToken(token, inviterId, expiresAt);

        // Compare signatures using constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedSignature),
            Encoding.UTF8.GetBytes(expectedSignature)
        );
    }
}
