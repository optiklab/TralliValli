using MongoDB.Driver;
using TraliVali.Domain.Entities;

namespace TraliVali.Auth;

/// <summary>
/// MongoDB-based implementation of conversation service
/// </summary>
public class ConversationService : IConversationService
{
    private readonly IMongoCollection<Conversation> _conversations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationService"/> class
    /// </summary>
    /// <param name="conversations">The MongoDB conversations collection</param>
    public ConversationService(IMongoCollection<Conversation> conversations)
    {
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
    }

    /// <inheritdoc/>
    public async Task<Conversation> CreateDirectConversationAsync(string userId1, string userId2)
    {
        if (string.IsNullOrWhiteSpace(userId1))
            throw new ArgumentException("User ID 1 is required", nameof(userId1));
        if (string.IsNullOrWhiteSpace(userId2))
            throw new ArgumentException("User ID 2 is required", nameof(userId2));
        if (userId1 == userId2)
            throw new ArgumentException("Cannot create a direct conversation with the same user");

        // Check if a direct conversation already exists between these two users
        var existingConversation = await FindExistingDirectConversationAsync(userId1, userId2);
        if (existingConversation != null)
        {
            return existingConversation;
        }

        // Create new direct conversation
        var conversation = new Conversation
        {
            Type = "direct",
            Name = string.Empty, // Direct conversations typically don't have names
            IsGroup = false,
            Participants = new List<Participant>
            {
                new Participant
                {
                    UserId = userId1,
                    JoinedAt = DateTime.UtcNow,
                    Role = "member"
                },
                new Participant
                {
                    UserId = userId2,
                    JoinedAt = DateTime.UtcNow,
                    Role = "member"
                }
            },
            CreatedAt = DateTime.UtcNow
        };

        await _conversations.InsertOneAsync(conversation);
        return conversation;
    }

    /// <inheritdoc/>
    public async Task<Conversation> CreateGroupConversationAsync(string name, string creatorId, string[] memberIds)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(creatorId))
            throw new ArgumentException("Creator ID is required", nameof(creatorId));
        if (memberIds == null || memberIds.Length == 0)
            throw new ArgumentException("At least one member is required", nameof(memberIds));

        // Create list of participants, starting with the creator as admin
        var participants = new List<Participant>
        {
            new Participant
            {
                UserId = creatorId,
                JoinedAt = DateTime.UtcNow,
                Role = "admin"
            }
        };

        // Add other members
        foreach (var memberId in memberIds)
        {
            if (!string.IsNullOrWhiteSpace(memberId) && memberId != creatorId)
            {
                participants.Add(new Participant
                {
                    UserId = memberId,
                    JoinedAt = DateTime.UtcNow,
                    Role = "member"
                });
            }
        }

        // Create the group conversation
        var conversation = new Conversation
        {
            Type = "group",
            Name = name,
            IsGroup = true,
            Participants = participants,
            CreatedAt = DateTime.UtcNow
        };

        await _conversations.InsertOneAsync(conversation);
        return conversation;
    }

    /// <inheritdoc/>
    public async Task<bool> AddMemberAsync(string conversationId, string userId, string role = "member")
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required", nameof(role));

        // Add the new participant using atomic operation
        var newParticipant = new Participant
        {
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            Role = role
        };

        // Use filter to ensure conversation exists and user is not already a participant
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.Id, conversationId),
            Builders<Conversation>.Filter.Not(
                Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId)
            )
        );
        var update = Builders<Conversation>.Update.Push(c => c.Participants, newParticipant);
        var result = await _conversations.UpdateOneAsync(filter, update);

        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveMemberAsync(string conversationId, string userId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        // Remove the participant using PullFilter to match by UserId only
        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, conversationId);
        var update = Builders<Conversation>.Update.PullFilter(c => c.Participants, p => p.UserId == userId);
        var result = await _conversations.UpdateOneAsync(filter, update);

        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateGroupMetadataAsync(string conversationId, string? name = null, string? avatar = null)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID is required", nameof(conversationId));

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(avatar))
            return false; // Nothing to update

        // Build the update definition dynamically
        var updateBuilder = Builders<Conversation>.Update;
        var updates = new List<UpdateDefinition<Conversation>>();

        if (!string.IsNullOrWhiteSpace(name))
        {
            updates.Add(updateBuilder.Set(c => c.Name, name));
        }

        if (!string.IsNullOrWhiteSpace(avatar))
        {
            updates.Add(updateBuilder.Set("metadata.avatar", avatar));
        }

        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, conversationId);
        var update = updateBuilder.Combine(updates);
        var result = await _conversations.UpdateOneAsync(filter, update);

        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        // Find all conversations where the user is a participant
        var filter = Builders<Conversation>.Filter.ElemMatch(
            c => c.Participants,
            p => p.UserId == userId
        );

        var conversations = await _conversations.Find(filter).ToListAsync();
        return conversations;
    }

    /// <summary>
    /// Finds an existing direct conversation between two users
    /// </summary>
    /// <param name="userId1">The ID of the first user</param>
    /// <param name="userId2">The ID of the second user</param>
    /// <returns>The existing conversation if found, null otherwise</returns>
    private async Task<Conversation?> FindExistingDirectConversationAsync(string userId1, string userId2)
    {
        // Find a direct conversation that has exactly these two users
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.IsGroup, false),
            Builders<Conversation>.Filter.Size(c => c.Participants, 2),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId1),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId2)
        );

        return await _conversations.Find(filter).FirstOrDefaultAsync();
    }
}
