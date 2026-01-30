using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraliVali.Api.Models;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using System.Security.Claims;

namespace TraliVali.Api.Controllers;

/// <summary>
/// Controller for managing encrypted key backups
/// </summary>
[ApiController]
[Route("key-backups")]
[Authorize]
public class KeyBackupController : ControllerBase
{
    private readonly IRepository<UserKeyBackup> _keyBackupRepository;
    private readonly ILogger<KeyBackupController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyBackupController"/> class
    /// </summary>
    public KeyBackupController(
        IRepository<UserKeyBackup> keyBackupRepository,
        ILogger<KeyBackupController> logger)
    {
        _keyBackupRepository = keyBackupRepository ?? throw new ArgumentNullException(nameof(keyBackupRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Stores an encrypted key backup for the authenticated user
    /// </summary>
    /// <param name="request">The key backup data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response with backup ID</returns>
    /// <remarks>
    /// Security Notes:
    /// - The server stores only encrypted data and never has access to the user's password
    /// - The encryption is performed client-side using PBKDF2 key derivation
    /// - Each user can have only one active backup (updates replace the previous backup)
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(StoreKeyBackupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StoreKeyBackup(
        [FromBody] StoreKeyBackupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get user ID from JWT claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Key backup attempted without valid user ID in token");
                return Unauthorized(new { message = "Invalid user credentials." });
            }

            // Check if user already has a backup
            var existingBackups = await _keyBackupRepository.FindAsync(
                b => b.UserId == userId,
                cancellationToken
            );
            var existingBackup = existingBackups.FirstOrDefault();

            UserKeyBackup backup;
            if (existingBackup != null)
            {
                // Update existing backup
                existingBackup.Version = request.Version;
                existingBackup.EncryptedData = request.EncryptedData;
                existingBackup.Iv = request.Iv;
                existingBackup.Salt = request.Salt;
                existingBackup.UpdatedAt = DateTime.UtcNow;

                var validationErrors = existingBackup.Validate();
                if (validationErrors.Any())
                {
                    _logger.LogWarning("Invalid key backup data: {Errors}", string.Join(", ", validationErrors));
                    return BadRequest(new { message = "Invalid backup data.", errors = validationErrors });
                }

                await _keyBackupRepository.UpdateAsync(existingBackup.Id, existingBackup, cancellationToken);
                backup = existingBackup;
                _logger.LogInformation("Key backup updated for user: {UserId}", userId);
            }
            else
            {
                // Create new backup
                var newBackup = new UserKeyBackup
                {
                    UserId = userId,
                    Version = request.Version,
                    EncryptedData = request.EncryptedData,
                    Iv = request.Iv,
                    Salt = request.Salt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var validationErrors = newBackup.Validate();
                if (validationErrors.Any())
                {
                    _logger.LogWarning("Invalid key backup data: {Errors}", string.Join(", ", validationErrors));
                    return BadRequest(new { message = "Invalid backup data.", errors = validationErrors });
                }

                backup = await _keyBackupRepository.AddAsync(newBackup, cancellationToken);
                _logger.LogInformation("Key backup created for user: {UserId}", userId);
            }

            return Ok(new StoreKeyBackupResponse
            {
                BackupId = backup.Id,
                CreatedAt = backup.UpdatedAt,
                Message = "Key backup stored successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing key backup for user");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while storing the backup.");
        }
    }

    /// <summary>
    /// Retrieves the encrypted key backup for the authenticated user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The encrypted key backup</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetKeyBackupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKeyBackup(CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from JWT claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Key backup retrieval attempted without valid user ID in token");
                return Unauthorized(new { message = "Invalid user credentials." });
            }

            // Find user's backup
            var backups = await _keyBackupRepository.FindAsync(
                b => b.UserId == userId,
                cancellationToken
            );
            var backup = backups.FirstOrDefault();

            if (backup == null)
            {
                _logger.LogInformation("No key backup found for user: {UserId}", userId);
                return NotFound(new { message = "No key backup found." });
            }

            _logger.LogInformation("Key backup retrieved for user: {UserId}", userId);

            return Ok(new GetKeyBackupResponse
            {
                BackupId = backup.Id,
                Version = backup.Version,
                EncryptedData = backup.EncryptedData,
                Iv = backup.Iv,
                Salt = backup.Salt,
                CreatedAt = backup.CreatedAt,
                UpdatedAt = backup.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving key backup");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the backup.");
        }
    }

    /// <summary>
    /// Checks if an encrypted key backup exists for the authenticated user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether a backup exists</returns>
    [HttpGet("exists")]
    [ProducesResponseType(typeof(KeyBackupExistsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckBackupExists(CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from JWT claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Backup existence check attempted without valid user ID in token");
                return Unauthorized(new { message = "Invalid user credentials." });
            }

            // Check if backup exists
            var backups = await _keyBackupRepository.FindAsync(
                b => b.UserId == userId,
                cancellationToken
            );
            var backup = backups.FirstOrDefault();

            return Ok(new KeyBackupExistsResponse
            {
                Exists = backup != null,
                LastUpdatedAt = backup?.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking backup existence");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while checking backup existence.");
        }
    }

    /// <summary>
    /// Deletes the encrypted key backup for the authenticated user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteKeyBackup(CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from JWT claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Key backup deletion attempted without valid user ID in token");
                return Unauthorized(new { message = "Invalid user credentials." });
            }

            // Find and delete user's backup
            var backups = await _keyBackupRepository.FindAsync(
                b => b.UserId == userId,
                cancellationToken
            );
            var backup = backups.FirstOrDefault();

            if (backup == null)
            {
                _logger.LogInformation("No key backup found to delete for user: {UserId}", userId);
                return NotFound(new { message = "No key backup found." });
            }

            await _keyBackupRepository.DeleteAsync(backup.Id, cancellationToken);
            _logger.LogInformation("Key backup deleted for user: {UserId}", userId);

            return Ok(new { message = "Key backup deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key backup");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the backup.");
        }
    }
}
