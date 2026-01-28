using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TraliVali.Domain.Entities;

namespace TraliVali.Auth;

/// <summary>
/// Implementation of JWT service for token generation and validation
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly RsaSecurityKey _signingKey;
    private readonly RsaSecurityKey _validationKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtService"/> class
    /// </summary>
    /// <param name="settings">JWT settings</param>
    /// <param name="blacklistService">Token blacklist service</param>
    public JwtService(JwtSettings settings, ITokenBlacklistService blacklistService)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _blacklistService = blacklistService ?? throw new ArgumentNullException(nameof(blacklistService));
        _tokenHandler = new JwtSecurityTokenHandler();

        // Load RSA keys
        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(_settings.PrivateKey);
        _signingKey = new RsaSecurityKey(privateRsa);

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(_settings.PublicKey);
        _validationKey = new RsaSecurityKey(publicRsa);
    }

    /// <inheritdoc/>
    public TokenResult GenerateToken(User user, string deviceId)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID is required", nameof(deviceId));

        var now = DateTime.UtcNow;
        var accessTokenExpiry = now.AddDays(_settings.ExpirationDays);
        var refreshTokenExpiry = now.AddDays(_settings.RefreshTokenExpirationDays);

        // Create claims for access token
        var claims = new[]
        {
            new Claim("userId", user.Id),
            new Claim("email", user.Email),
            new Claim("displayName", user.DisplayName),
            new Claim("deviceId", deviceId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString())
        };

        // Create access token
        var accessToken = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: accessTokenExpiry,
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        );

        // Create refresh token claims
        var refreshClaims = new[]
        {
            new Claim("userId", user.Id),
            new Claim("deviceId", deviceId),
            new Claim("tokenType", "refresh"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString())
        };

        // Create refresh token
        var refreshToken = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: refreshClaims,
            notBefore: now,
            expires: refreshTokenExpiry,
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        );

        return new TokenResult
        {
            AccessToken = _tokenHandler.WriteToken(accessToken),
            RefreshToken = _tokenHandler.WriteToken(refreshToken),
            ExpiresAt = accessTokenExpiry,
            RefreshExpiresAt = refreshTokenExpiry
        };
    }

    /// <inheritdoc/>
    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token is required"
            };
        }

        try
        {
            // Check if token is blacklisted
            if (await _blacklistService.IsTokenBlacklistedAsync(token))
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token has been revoked"
                };
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = _validationKey,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            
            var userId = principal.FindFirst("userId")?.Value;
            var deviceId = principal.FindFirst("deviceId")?.Value;

            return new TokenValidationResult
            {
                IsValid = true,
                Principal = principal,
                UserId = userId,
                DeviceId = deviceId
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenException ex)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Token validation failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = $"An error occurred during token validation: {ex.Message}"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<TokenResult?> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        try
        {
            // Validate refresh token
            var validationResult = await ValidateTokenAsync(refreshToken);
            if (!validationResult.IsValid)
                return null;

            // Check if it's actually a refresh token
            var tokenTypeClaim = validationResult.Principal?.FindFirst("tokenType")?.Value;
            if (tokenTypeClaim != "refresh")
                return null;

            var userId = validationResult.UserId;
            var deviceId = validationResult.DeviceId;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(deviceId))
                return null;

            // Create a minimal user object for token generation
            // In a real scenario, you would fetch the user from the database
            var user = new User
            {
                Id = userId,
                Email = validationResult.Principal?.FindFirst("email")?.Value ?? "",
                DisplayName = validationResult.Principal?.FindFirst("displayName")?.Value ?? ""
            };

            // If user doesn't have email/displayName in refresh token, we need to fetch from DB
            // For now, we'll require the caller to provide a proper user object
            // This is a simplification for the implementation

            // Blacklist the old refresh token (rotation)
            var tokenExpiry = validationResult.Principal?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
            if (tokenExpiry != null && long.TryParse(tokenExpiry, out var exp))
            {
                var expiryDate = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                await _blacklistService.BlacklistTokenAsync(refreshToken, expiryDate);
            }

            // Generate new tokens
            return GenerateToken(user, deviceId);
        }
        catch
        {
            return null;
        }
    }
}
