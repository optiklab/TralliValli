using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraliVali.Api.Models;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;

namespace TraliVali.Api.Controllers;

/// <summary>
/// Controller for message operations
/// </summary>
[Authorize]
[ApiController]
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _messageRepository;
    private readonly IRepository<Conversation> _conversationRepository;
    private readonly ILogger<MessagesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagesController"/> class
    /// </summary>
    public MessagesController(
        IMessageRepository messageRepository,
        IRepository<Conversation> conversationRepository,
        ILogger<MessagesController> logger)
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets messages for a conversation with cursor-based pagination
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="before">Optional cursor for pagination (ISO 8601 timestamp)</param>
    /// <param name="limit">Number of messages per page (default: 50, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of messages</returns>
    [HttpGet("conversations/{conversationId}/messages")]
    [ProducesResponseType(typeof(PaginatedMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMessages(
        string conversationId,
        [FromQuery] string? before = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            // Validate limit parameter
            limit = Math.Max(1, Math.Min(100, limit));

            // Verify conversation exists and user has access
            var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            if (!conversation.Participants.Any(p => p.UserId == userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            // Parse cursor if provided
            DateTime? beforeCursor = null;
            if (!string.IsNullOrWhiteSpace(before))
            {
                if (!DateTime.TryParse(before, out var parsedCursor))
                {
                    return BadRequest(new { message = "Invalid cursor format. Expected ISO 8601 timestamp." });
                }
                beforeCursor = parsedCursor.ToUniversalTime();
            }

            // Get messages with pagination
            var (messages, hasMore) = await _messageRepository.GetMessagesByConversationAsync(
                conversationId,
                beforeCursor,
                limit,
                cancellationToken);

            var messagesList = messages.ToList();
            var nextCursor = hasMore && messagesList.Any()
                ? messagesList.Last().CreatedAt
                : (DateTime?)null;

            return Ok(new PaginatedMessagesResponse
            {
                Messages = messagesList.Select(MapToResponse).ToList(),
                HasMore = hasMore,
                NextCursor = nextCursor,
                Count = messagesList.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for conversation {ConversationId}", conversationId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving messages.");
        }
    }

    /// <summary>
    /// Searches messages in a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="query">The search query</param>
    /// <param name="limit">Maximum number of results (default: 50, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching messages</returns>
    [HttpGet("conversations/{conversationId}/messages/search")]
    [ProducesResponseType(typeof(SearchMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchMessages(
        string conversationId,
        [FromQuery] string query,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Search query is required." });
            }

            if (query.Length > 500)
            {
                return BadRequest(new { message = "Search query must be 500 characters or less." });
            }

            var userId = GetUserId();

            // Validate limit parameter
            limit = Math.Max(1, Math.Min(100, limit));

            // Verify conversation exists and user has access
            var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            if (!conversation.Participants.Any(p => p.UserId == userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            // Search messages
            var messages = await _messageRepository.SearchMessagesAsync(
                conversationId,
                query,
                limit,
                cancellationToken);

            var messagesList = messages.ToList();

            return Ok(new SearchMessagesResponse
            {
                Messages = messagesList.Select(MapToResponse).ToList(),
                Count = messagesList.Count,
                Query = query
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching messages in conversation {ConversationId}", conversationId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching messages.");
        }
    }

    /// <summary>
    /// Soft deletes a message
    /// </summary>
    /// <param name="id">The message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpDelete("messages/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMessage(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            // Get the message
            var message = await _messageRepository.GetByIdAsync(id, cancellationToken);
            if (message == null)
            {
                return NotFound(new { message = "Message not found." });
            }

            // Verify conversation exists and user has access
            var conversation = await _conversationRepository.GetByIdAsync(message.ConversationId, cancellationToken);
            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            if (!conversation.Participants.Any(p => p.UserId == userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            // Only the sender can delete their own message
            if (message.SenderId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only the message sender can delete it." });
            }

            // Soft delete the message
            var deleted = await _messageRepository.SoftDeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete message.");
            }

            _logger.LogInformation("Message {MessageId} soft deleted by user {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the message.");
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

    /// <summary>
    /// Maps a Message entity to a MessageResponse
    /// </summary>
    private static MessageResponse MapToResponse(Message message)
    {
        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Type = message.Type,
            Content = message.Content,
            EncryptedContent = message.EncryptedContent,
            ReplyTo = message.ReplyTo,
            CreatedAt = message.CreatedAt,
            ReadBy = message.ReadBy.Select(r => new MessageReadStatusResponse
            {
                UserId = r.UserId,
                ReadAt = r.ReadAt
            }).ToList(),
            EditedAt = message.EditedAt,
            IsDeleted = message.IsDeleted,
            Attachments = message.Attachments
        };
    }
}
