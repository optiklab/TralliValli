using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraliVali.Api.Models;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;

namespace TraliVali.Api.Controllers;

/// <summary>
/// Controller for conversation operations
/// </summary>
[Authorize]
[ApiController]
[Route("conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IRepository<Conversation> _conversationRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<ConversationsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationsController"/> class
    /// </summary>
    public ConversationsController(
        IRepository<Conversation> conversationRepository,
        IRepository<User> userRepository,
        ILogger<ConversationsController> logger)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current user's conversations with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of conversations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedConversationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize));

            // Get conversations where user is a participant
            var allConversations = await _conversationRepository.FindAsync(
                c => c.Participants.Any(p => p.UserId == userId),
                cancellationToken);

            var conversations = allConversations
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToList();

            var totalCount = conversations.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedConversations = conversations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToResponse)
                .ToList();

            return Ok(new PaginatedConversationsResponse
            {
                Conversations = pagedConversations,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations for user");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving conversations.");
        }
    }

    /// <summary>
    /// Creates a new direct (1-on-1) conversation
    /// </summary>
    /// <param name="request">The conversation creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created conversation</returns>
    [HttpPost("direct")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDirectConversation(
        [FromBody] CreateDirectConversationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();

            // Check if trying to create conversation with self
            if (request.OtherUserId == userId)
            {
                return BadRequest(new { message = "Cannot create a conversation with yourself." });
            }

            // Verify the other user exists
            var otherUser = await _userRepository.GetByIdAsync(request.OtherUserId, cancellationToken);
            if (otherUser == null || !otherUser.IsActive)
            {
                return NotFound(new { message = "User not found." });
            }

            // Check if conversation already exists
            var existingConversations = await _conversationRepository.FindAsync(
                c => c.Type == "direct" &&
                     c.Participants.Any(p => p.UserId == userId) &&
                     c.Participants.Any(p => p.UserId == request.OtherUserId),
                cancellationToken);

            var existingConversation = existingConversations.FirstOrDefault();
            if (existingConversation != null)
            {
                return Ok(MapToResponse(existingConversation));
            }

            // Create new conversation
            var conversation = new Conversation
            {
                Type = "direct",
                Name = "",
                IsGroup = false,
                Participants = new List<Participant>
                {
                    new Participant { UserId = userId, Role = "member" },
                    new Participant { UserId = request.OtherUserId, Role = "member" }
                },
                CreatedAt = DateTime.UtcNow
            };

            var errors = conversation.Validate();
            if (errors.Count > 0)
            {
                return BadRequest(new { message = "Validation failed", errors });
            }

            var created = await _conversationRepository.AddAsync(conversation, cancellationToken);
            _logger.LogInformation("Direct conversation created: {ConversationId} between {UserId} and {OtherUserId}",
                created.Id, userId, request.OtherUserId);

            return CreatedAtAction(
                nameof(GetConversation),
                new { id = created.Id },
                MapToResponse(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating direct conversation");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the conversation.");
        }
    }

    /// <summary>
    /// Creates a new group conversation
    /// </summary>
    /// <param name="request">The group creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created conversation</returns>
    [HttpPost("group")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateGroupConversation(
        [FromBody] CreateGroupConversationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();

            // Verify all member users exist
            var memberIds = request.MemberUserIds.Distinct().ToList();
            
            // Add creator if not in list
            if (!memberIds.Contains(userId))
            {
                memberIds.Add(userId);
            }

            // Validate all users exist
            foreach (var memberId in memberIds)
            {
                var user = await _userRepository.GetByIdAsync(memberId, cancellationToken);
                if (user == null || !user.IsActive)
                {
                    return BadRequest(new { message = $"User {memberId} not found or inactive." });
                }
            }

            // Create participants list (creator is admin)
            var participants = memberIds.Select(id => new Participant
            {
                UserId = id,
                Role = id == userId ? "admin" : "member",
                JoinedAt = DateTime.UtcNow
            }).ToList();

            // Create new group conversation
            var conversation = new Conversation
            {
                Type = "group",
                Name = request.Name,
                IsGroup = true,
                Participants = participants,
                CreatedAt = DateTime.UtcNow
            };

            var errors = conversation.Validate();
            if (errors.Count > 0)
            {
                return BadRequest(new { message = "Validation failed", errors });
            }

            var created = await _conversationRepository.AddAsync(conversation, cancellationToken);
            _logger.LogInformation("Group conversation created: {ConversationId} by {UserId} with {MemberCount} members",
                created.Id, userId, participants.Count);

            return CreatedAtAction(
                nameof(GetConversation),
                new { id = created.Id },
                MapToResponse(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group conversation");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the conversation.");
        }
    }

    /// <summary>
    /// Gets a conversation by ID with recent messages
    /// </summary>
    /// <param name="id">The conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The conversation details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversation(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var conversation = await _conversationRepository.GetByIdAsync(id, cancellationToken);

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            // Verify user is a participant
            if (!conversation.Participants.Any(p => p.UserId == userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            return Ok(MapToResponse(conversation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation {ConversationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the conversation.");
        }
    }

    /// <summary>
    /// Updates group conversation metadata
    /// </summary>
    /// <param name="id">The conversation ID</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated conversation</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateGroupMetadata(
        string id,
        [FromBody] UpdateGroupMetadataRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var conversation = await _conversationRepository.GetByIdAsync(id, cancellationToken);

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            // Only group conversations can be updated
            if (!conversation.IsGroup)
            {
                return BadRequest(new { message = "Only group conversations can be updated." });
            }

            // Verify user is a participant with admin role
            var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            if (participant.Role != "admin")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only admins can update group metadata." });
            }

            // Update metadata
            if (request.Name != null)
            {
                conversation.Name = request.Name;
            }

            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    conversation.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var errors = conversation.Validate();
            if (errors.Count > 0)
            {
                return BadRequest(new { message = "Validation failed", errors });
            }

            await _conversationRepository.UpdateAsync(id, conversation, cancellationToken);
            _logger.LogInformation("Group conversation {ConversationId} updated by {UserId}", id, userId);

            return Ok(MapToResponse(conversation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation {ConversationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the conversation.");
        }
    }

    /// <summary>
    /// Adds a member to a group conversation
    /// </summary>
    /// <param name="id">The conversation ID</param>
    /// <param name="request">The add member request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated conversation</returns>
    [HttpPost("{id}/members")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddMember(
        string id,
        [FromBody] AddMemberRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var conversation = await _conversationRepository.GetByIdAsync(id, cancellationToken);

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            // Only group conversations can have members added
            if (!conversation.IsGroup)
            {
                return BadRequest(new { message = "Only group conversations can have members added." });
            }

            // Verify current user is a participant with admin role
            var currentParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
            if (currentParticipant == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            if (currentParticipant.Role != "admin")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only admins can add members." });
            }

            // Check if user is already a member
            if (conversation.Participants.Any(p => p.UserId == request.UserId))
            {
                return BadRequest(new { message = "User is already a member of this conversation." });
            }

            // Verify the user to add exists
            var userToAdd = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (userToAdd == null || !userToAdd.IsActive)
            {
                return NotFound(new { message = "User not found." });
            }

            // Add the new member
            conversation.Participants.Add(new Participant
            {
                UserId = request.UserId,
                Role = request.Role ?? "member",
                JoinedAt = DateTime.UtcNow
            });

            await _conversationRepository.UpdateAsync(id, conversation, cancellationToken);
            _logger.LogInformation("User {NewUserId} added to conversation {ConversationId} by {UserId}",
                request.UserId, id, userId);

            return Ok(MapToResponse(conversation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to conversation {ConversationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding the member.");
        }
    }

    /// <summary>
    /// Removes a member from a group conversation
    /// </summary>
    /// <param name="id">The conversation ID</param>
    /// <param name="userId">The user ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated conversation</returns>
    [HttpDelete("{id}/members/{userId}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveMember(
        string id,
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetUserId();
            var conversation = await _conversationRepository.GetByIdAsync(id, cancellationToken);

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found." });
            }

            // Only group conversations can have members removed
            if (!conversation.IsGroup)
            {
                return BadRequest(new { message = "Only group conversations can have members removed." });
            }

            // Verify current user is a participant
            var currentParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId);
            if (currentParticipant == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
            }

            // Users can remove themselves, or admins can remove others
            var isSelfRemoval = userId == currentUserId;
            if (!isSelfRemoval && currentParticipant.Role != "admin")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only admins can remove other members." });
            }

            // Find the participant to remove
            var participantToRemove = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participantToRemove == null)
            {
                return NotFound(new { message = "User is not a member of this conversation." });
            }

            // Remove the participant
            conversation.Participants.Remove(participantToRemove);

            // If no participants left, could delete the conversation (not implemented here)
            if (conversation.Participants.Count == 0)
            {
                _logger.LogWarning("Conversation {ConversationId} has no participants left after removal", id);
            }

            await _conversationRepository.UpdateAsync(id, conversation, cancellationToken);
            _logger.LogInformation("User {RemovedUserId} removed from conversation {ConversationId} by {UserId}",
                userId, id, currentUserId);

            return Ok(MapToResponse(conversation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from conversation {ConversationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing the member.");
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
    /// Maps a Conversation entity to a ConversationResponse
    /// </summary>
    private static ConversationResponse MapToResponse(Conversation conversation)
    {
        return new ConversationResponse
        {
            Id = conversation.Id,
            Type = conversation.Type,
            Name = conversation.Name,
            IsGroup = conversation.IsGroup,
            Participants = conversation.Participants.Select(p => new ParticipantResponse
            {
                UserId = p.UserId,
                JoinedAt = p.JoinedAt,
                LastReadAt = p.LastReadAt,
                Role = p.Role
            }).ToList(),
            RecentMessages = conversation.RecentMessages,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            Metadata = conversation.Metadata
        };
    }
}
