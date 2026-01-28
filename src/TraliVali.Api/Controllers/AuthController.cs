using Microsoft.AspNetCore.Mvc;
using TraliVali.Api.Models;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Messaging;

namespace TraliVali.Api.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMagicLinkService _magicLinkService;
    private readonly IJwtService _jwtService;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Invite> _inviteRepository;
    private readonly IInviteService _inviteService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class
    /// </summary>
    public AuthController(
        IMagicLinkService magicLinkService,
        IJwtService jwtService,
        ITokenBlacklistService tokenBlacklistService,
        IRepository<User> userRepository,
        IRepository<Invite> inviteRepository,
        IInviteService inviteService,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _magicLinkService = magicLinkService ?? throw new ArgumentNullException(nameof(magicLinkService));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _tokenBlacklistService = tokenBlacklistService ?? throw new ArgumentNullException(nameof(tokenBlacklistService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _inviteRepository = inviteRepository ?? throw new ArgumentNullException(nameof(inviteRepository));
        _inviteService = inviteService ?? throw new ArgumentNullException(nameof(inviteService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Requests a magic link to be sent to the specified email
    /// </summary>
    /// <param name="request">The magic link request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    /// <remarks>
    /// TODO: Add rate limiting in production to prevent:
    /// - Email spam attacks
    /// - User enumeration via timing attacks
    /// Consider implementing rate limiting per IP address and per email address.
    /// </remarks>
    [HttpPost("request-magic-link")]
    [ProducesResponseType(typeof(RequestMagicLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestMagicLink(
        [FromBody] RequestMagicLinkRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if user exists
            var users = await _userRepository.FindAsync(u => u.Email == request.Email, cancellationToken);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                _logger.LogWarning("Magic link requested for non-existent user: {Email}", request.Email);
                // Return success even if user doesn't exist (security best practice)
                return Ok(new RequestMagicLinkResponse
                {
                    Message = "If the email exists in our system, a magic link has been sent."
                });
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Magic link requested for inactive user: {Email}", request.Email);
                // Return success even if user is inactive (security best practice)
                return Ok(new RequestMagicLinkResponse
                {
                    Message = "If the email exists in our system, a magic link has been sent."
                });
            }

            // Generate magic link token
            var token = await _magicLinkService.CreateMagicLinkAsync(request.Email, request.DeviceId);

            // Construct magic link URL
            var magicLinkUrl = $"{Request.Scheme}://{Request.Host}/auth/verify?token={token}";

            // Send magic link email
            await _emailService.SendMagicLinkEmailAsync(
                user.Email,
                user.DisplayName,
                magicLinkUrl,
                cancellationToken);

            _logger.LogInformation("Magic link sent to user: {Email}", request.Email);

            return Ok(new RequestMagicLinkResponse
            {
                Message = "If the email exists in our system, a magic link has been sent."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting magic link for email: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Verifies a magic link token and returns JWT tokens
    /// </summary>
    /// <param name="request">The magic link verification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT access and refresh tokens</returns>
    [HttpPost("verify-magic-link")]
    [ProducesResponseType(typeof(VerifyMagicLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyMagicLink(
        [FromBody] VerifyMagicLinkRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate and consume magic link (single-use)
            var magicLink = await _magicLinkService.ValidateAndConsumeMagicLinkAsync(request.Token);

            if (magicLink == null)
            {
                _logger.LogWarning("Invalid or expired magic link token");
                return Unauthorized(new { message = "Invalid or expired magic link." });
            }

            // Get user from database
            var users = await _userRepository.FindAsync(u => u.Email == magicLink.Email, cancellationToken);
            var user = users.FirstOrDefault();

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Magic link verified but user not found or inactive: {Email}", magicLink.Email);
                return Unauthorized(new { message = "Invalid or expired magic link." });
            }

            // Update last login time (best effort - don't fail if update fails)
            try
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update last login time for user: {Email}", user.Email);
                // Continue anyway - this is not critical
            }

            // Generate JWT tokens
            var tokenResult = _jwtService.GenerateToken(user, magicLink.DeviceId);

            _logger.LogInformation("Magic link verified and JWT tokens generated for user: {Email}", user.Email);

            return Ok(new VerifyMagicLinkResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                RefreshExpiresAt = tokenResult.RefreshExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying magic link");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="request">The token refresh request</param>
    /// <returns>New JWT access and refresh tokens</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Refresh the token
            var tokenResult = await _jwtService.RefreshTokenAsync(request.RefreshToken);

            if (tokenResult == null)
            {
                _logger.LogWarning("Invalid refresh token");
                return Unauthorized(new { message = "Invalid refresh token." });
            }

            _logger.LogInformation("Token refreshed successfully");

            return Ok(new RefreshTokenResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                RefreshExpiresAt = tokenResult.RefreshExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Logs out a user by blacklisting their access token
    /// </summary>
    /// <param name="request">The logout request</param>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate the token to get expiry information
            var validationResult = await _jwtService.ValidateTokenAsync(request.AccessToken);

            if (validationResult.IsValid && validationResult.Principal != null)
            {
                // Get token expiry from claims
                var expClaim = validationResult.Principal.FindFirst("exp")?.Value;
                if (expClaim != null && long.TryParse(expClaim, out var exp))
                {
                    var expiryDate = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                    await _tokenBlacklistService.BlacklistTokenAsync(request.AccessToken, expiryDate);
                    _logger.LogInformation("User logged out, token blacklisted");
                }
            }

            // Always return success for logout (even if token was invalid)
            return Ok(new LogoutResponse
            {
                Message = "Logged out successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Validates an invite token
    /// </summary>
    /// <param name="token">The invite token to validate</param>
    /// <returns>Validation result</returns>
    [HttpGet("invite/{token}")]
    [ProducesResponseType(typeof(ValidateInviteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateInvite(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return NotFound(new ValidateInviteResponse
                {
                    IsValid = false,
                    Message = "Invalid invite token."
                });
            }

            var validationResult = await _inviteService.ValidateInviteAsync(token);

            if (validationResult == null)
            {
                _logger.LogWarning("Invite validation failed for token");
                return NotFound(new ValidateInviteResponse
                {
                    IsValid = false,
                    Message = "Invalid or expired invite token."
                });
            }

            _logger.LogInformation("Invite validated successfully");

            return Ok(new ValidateInviteResponse
            {
                IsValid = true,
                ExpiresAt = validationResult.ExpiresAt,
                Message = "Invite is valid."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invite token");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Registers a new user with an invite token
    /// </summary>
    /// <param name="request">The registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT tokens for the newly registered user</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Validate invite
            var validationResult = await _inviteService.ValidateInviteAsync(request.InviteToken);
            if (validationResult == null)
            {
                _logger.LogWarning("Registration attempted with invalid invite token");
                return BadRequest(new { message = "Invalid or expired invite token." });
            }

            // Normalize email for consistency
            var normalizedEmail = request.Email.ToLowerInvariant();
            
            // Trim and validate display name
            var trimmedDisplayName = request.DisplayName.Trim();
            if (string.IsNullOrWhiteSpace(trimmedDisplayName))
            {
                return BadRequest(new { message = "Display name cannot be empty." });
            }

            // Check if user already exists (using normalized email)
            var existingUsers = await _userRepository.FindAsync(u => u.Email == normalizedEmail, cancellationToken);
            if (existingUsers.Any())
            {
                _logger.LogWarning("Registration attempted with existing email: {Email}", normalizedEmail);
                return BadRequest(new { message = "User with this email already exists." });
            }

            // 2. Create user with placeholder values for passwordless auth
            var user = new User
            {
                Email = normalizedEmail,
                DisplayName = trimmedDisplayName,
                PasswordHash = "N/A", // Passwordless auth using magic links
                PublicKey = "TBD", // Will be set by client during first login
                InvitedBy = validationResult.InviterId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createdUser = await _userRepository.AddAsync(user, cancellationToken);
            _logger.LogInformation("User created successfully: {Email}", createdUser.Email);

            // 3. Mark invite as used (critical: do this immediately after user creation)
            var inviteRedeemed = await _inviteService.RedeemInviteAsync(request.InviteToken, createdUser.Id);
            if (!inviteRedeemed)
            {
                // Invite redemption failed - this is a critical error
                // The user was created but invite is still valid, creating inconsistent state
                // We should delete the user to maintain consistency
                _logger.LogError("Failed to redeem invite after user creation, rolling back user creation");
                try
                {
                    await _userRepository.DeleteAsync(createdUser.Id, cancellationToken);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError(deleteEx, "Failed to rollback user creation after invite redemption failure");
                }
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during registration. Please try again.");
            }

            // 4. Send welcome email (best effort - don't fail if email fails)
            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    createdUser.Email,
                    createdUser.DisplayName,
                    cancellationToken);
                _logger.LogInformation("Welcome email sent to {Email}", createdUser.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", createdUser.Email);
                // Continue anyway - this is not critical
            }

            // 5. Return JWT
            var tokenResult = _jwtService.GenerateToken(createdUser, "registration");

            _logger.LogInformation("User registered successfully: {Email}", createdUser.Email);

            return Ok(new RegisterResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                RefreshExpiresAt = tokenResult.RefreshExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }
}
