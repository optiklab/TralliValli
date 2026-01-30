using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TraliVali.Api.Models;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Infrastructure.Storage;

namespace TraliVali.Api.Controllers;

/// <summary>
/// Controller for file operations
/// </summary>
[Authorize]
[ApiController]
[Route("files")]
public class FilesController : ControllerBase
{
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Conversation> _conversationRepository;
    private readonly IAzureBlobService _blobService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<FilesController> _logger;

    // Allowed MIME types
    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        // Images
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",
        // Documents
        "application/pdf", "text/plain",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        // Archives
        "application/zip", "application/x-7z-compressed", "application/x-rar-compressed",
        // Video
        "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo",
        // Audio
        "audio/mpeg", "audio/wav", "audio/ogg", "audio/mp4"
    };

    private const long MaxFileSizeBytes = 104857600; // 100MB

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesController"/> class
    /// </summary>
    public FilesController(
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Conversation> conversationRepository,
        IAzureBlobService blobService,
        IMessagePublisher messagePublisher,
        ILogger<FilesController> logger)
    {
        _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a presigned upload URL for direct file upload to Azure Blob Storage
    /// </summary>
    /// <param name="request">The upload URL request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Presigned upload URL with file metadata</returns>
    [HttpPost("upload-url")]
    [ProducesResponseType(typeof(GenerateUploadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateUploadUrl(
        [FromBody] GenerateUploadUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            // Validate file type
            if (!AllowedMimeTypes.Contains(request.MimeType.ToLowerInvariant()))
            {
                return BadRequest(new { message = $"File type '{request.MimeType}' is not allowed." });
            }

            // Validate file size
            if (request.Size <= 0 || request.Size > MaxFileSizeBytes)
            {
                return BadRequest(new { message = $"File size must be between 1 byte and {MaxFileSizeBytes / 1024 / 1024}MB." });
            }

            // Verify conversation exists and user has access
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            if (!conversation.Participants.Any(p => p.UserId == userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            // Create file record
            var file = new Domain.Entities.File
            {
                ConversationId = request.ConversationId,
                UploaderId = userId,
                FileName = request.FileName,
                MimeType = request.MimeType,
                Size = request.Size,
                BlobPath = string.Empty, // Will be set after path generation
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var createdFile = await _fileRepository.AddAsync(file, cancellationToken);

            // Generate blob path with file ID
            var blobPath = $"files/{request.ConversationId}/{createdFile.Id}/{request.FileName}";
            
            // Update file with blob path
            createdFile.BlobPath = blobPath;
            await _fileRepository.UpdateAsync(createdFile.Id, createdFile, cancellationToken);

            // Generate presigned upload URL
            var expiresIn = TimeSpan.FromHours(1);
            var uploadUrl = _blobService.GenerateUploadUrl(blobPath, expiresIn);

            _logger.LogInformation(
                "Generated upload URL for file {FileId} in conversation {ConversationId} by user {UserId}",
                createdFile.Id, request.ConversationId, userId);

            return Ok(new GenerateUploadUrlResponse
            {
                FileId = createdFile.Id,
                UploadUrl = uploadUrl,
                BlobPath = blobPath,
                ExpiresAt = DateTime.UtcNow.Add(expiresIn)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating upload URL");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while generating the upload URL." });
        }
    }

    /// <summary>
    /// Confirms that a file upload has been completed
    /// </summary>
    /// <param name="request">The completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation response</returns>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(CompleteUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteUpload(
        [FromBody] CompleteUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            // Get the file
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
            if (file == null)
            {
                return NotFound(new { message = "File not found." });
            }

            // Verify conversation exists and user has access
            var conversation = await _conversationRepository.GetByIdAsync(file.ConversationId, cancellationToken);
            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            if (!conversation.Participants.Any(p => p.UserId == userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            // Verify the user is the uploader
            if (file.UploaderId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, 
                    new { message = "Only the file uploader can complete the upload." });
            }

            // Publish message to files.process queue for background processing
            try
            {
                var filePayload = new
                {
                    fileId = file.Id,
                    blobPath = file.BlobPath,
                    mimeType = file.MimeType,
                    fileName = file.FileName
                };
                var messageJson = JsonSerializer.Serialize(filePayload);
                await _messagePublisher.PublishAsync("files.process", messageJson, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish file processing message for file {FileId}", file.Id);
                // Continue - the file upload is completed, processing can be retried later
            }

            _logger.LogInformation(
                "File upload completed for file {FileId} in conversation {ConversationId} by user {UserId}",
                file.Id, file.ConversationId, userId);

            return Ok(new CompleteUploadResponse
            {
                FileId = file.Id,
                Message = "File upload completed successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing file upload for file {FileId}", request.FileId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while completing the file upload." });
        }
    }

    /// <summary>
    /// Generates a presigned download URL for a file
    /// </summary>
    /// <param name="id">The file ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Presigned download URL</returns>
    [HttpGet("{id}/download-url")]
    [ProducesResponseType(typeof(GenerateDownloadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateDownloadUrl(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            // Get the file
            var file = await _fileRepository.GetByIdAsync(id, cancellationToken);
            if (file == null || file.IsDeleted)
            {
                return NotFound(new { message = "File not found." });
            }

            // Verify conversation exists and user has access
            var conversation = await _conversationRepository.GetByIdAsync(file.ConversationId, cancellationToken);
            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            if (!conversation.Participants.Any(p => p.UserId == userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            // Generate presigned download URL
            var expiresIn = TimeSpan.FromHours(1);
            var downloadUrl = _blobService.GenerateDownloadUrl(file.BlobPath, expiresIn);

            _logger.LogInformation(
                "Generated download URL for file {FileId} in conversation {ConversationId} by user {UserId}",
                id, file.ConversationId, userId);

            return Ok(new GenerateDownloadUrlResponse
            {
                FileId = file.Id,
                DownloadUrl = downloadUrl,
                FileName = file.FileName,
                ExpiresAt = DateTime.UtcNow.Add(expiresIn)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for file {FileId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while generating the download URL." });
        }
    }

    /// <summary>
    /// Soft deletes a file
    /// </summary>
    /// <param name="id">The file ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteFile(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            // Get the file
            var file = await _fileRepository.GetByIdAsync(id, cancellationToken);
            if (file == null)
            {
                return NotFound(new { message = "File not found." });
            }

            // Verify conversation exists and user has access
            var conversation = await _conversationRepository.GetByIdAsync(file.ConversationId, cancellationToken);
            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            if (!conversation.Participants.Any(p => p.UserId == userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            // Only the uploader can delete the file
            if (file.UploaderId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, 
                    new { message = "Only the file uploader can delete it." });
            }

            // Soft delete the file
            file.IsDeleted = true;
            await _fileRepository.UpdateAsync(id, file, cancellationToken);

            _logger.LogInformation("File {FileId} soft deleted by user {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while deleting the file." });
        }
    }

    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    /// <returns>The user ID</returns>
    /// <exception cref="InvalidOperationException">Thrown when user ID claim is not found</exception>
    private string GetUserId()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User ID claim not found");
            throw new InvalidOperationException("User ID claim not found. Authentication may have failed.");
        }
        return userId;
    }
}
