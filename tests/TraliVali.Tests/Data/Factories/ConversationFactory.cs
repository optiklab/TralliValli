using MongoDB.Bson;
using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Data.Factories;

/// <summary>
/// Factory class for generating Conversation test entities with builder pattern support
/// </summary>
public class ConversationFactory
{
    private string _id = ObjectId.GenerateNewId().ToString();
    private string _type = "direct";
    private string _name = "Test Conversation";
    private List<Participant> _participants = new();
    private List<string> _recentMessages = new();
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _lastMessageAt = null;
    private Dictionary<string, string> _metadata = new();
    private bool _isGroup = false;

    /// <summary>
    /// Creates a new ConversationFactory instance
    /// </summary>
    public static ConversationFactory Create() => new ConversationFactory();

    /// <summary>
    /// Sets the conversation ID
    /// </summary>
    public ConversationFactory WithId(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the conversation type
    /// </summary>
    public ConversationFactory WithType(string type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the conversation as a direct message
    /// </summary>
    public ConversationFactory AsDirect()
    {
        _type = "direct";
        _isGroup = false;
        return this;
    }

    /// <summary>
    /// Sets the conversation as a group
    /// </summary>
    public ConversationFactory AsGroup()
    {
        _type = "group";
        _isGroup = true;
        return this;
    }

    /// <summary>
    /// Sets the conversation as a channel
    /// </summary>
    public ConversationFactory AsChannel()
    {
        _type = "channel";
        _isGroup = true;
        return this;
    }

    /// <summary>
    /// Sets the conversation name
    /// </summary>
    public ConversationFactory WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Adds a participant to the conversation
    /// </summary>
    public ConversationFactory WithParticipant(string userId, string role = "member", DateTime? joinedAt = null)
    {
        _participants.Add(new Participant
        {
            UserId = userId,
            JoinedAt = joinedAt ?? DateTime.UtcNow,
            Role = role
        });
        return this;
    }

    /// <summary>
    /// Sets the participants list
    /// </summary>
    public ConversationFactory WithParticipants(List<Participant> participants)
    {
        _participants = participants;
        return this;
    }

    /// <summary>
    /// Adds a participant as admin
    /// </summary>
    public ConversationFactory WithAdminParticipant(string userId)
    {
        return WithParticipant(userId, "admin");
    }

    /// <summary>
    /// Adds recent message IDs
    /// </summary>
    public ConversationFactory WithRecentMessage(string messageId)
    {
        _recentMessages.Add(messageId);
        return this;
    }

    /// <summary>
    /// Sets the recent messages list
    /// </summary>
    public ConversationFactory WithRecentMessages(List<string> recentMessages)
    {
        _recentMessages = recentMessages;
        return this;
    }

    /// <summary>
    /// Sets the created at timestamp
    /// </summary>
    public ConversationFactory WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    /// <summary>
    /// Sets the last message timestamp
    /// </summary>
    public ConversationFactory WithLastMessageAt(DateTime lastMessageAt)
    {
        _lastMessageAt = lastMessageAt;
        return this;
    }

    /// <summary>
    /// Adds metadata entry
    /// </summary>
    public ConversationFactory WithMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the metadata dictionary
    /// </summary>
    public ConversationFactory WithMetadata(Dictionary<string, string> metadata)
    {
        _metadata = metadata;
        return this;
    }

    /// <summary>
    /// Builds and returns the Conversation entity
    /// </summary>
    public Conversation Build()
    {
        return new Conversation
        {
            Id = _id,
            Type = _type,
            Name = _name,
            Participants = _participants,
            RecentMessages = _recentMessages,
            CreatedAt = _createdAt,
            LastMessageAt = _lastMessageAt,
            Metadata = _metadata,
            IsGroup = _isGroup
        };
    }

    /// <summary>
    /// Builds and returns a valid conversation with required fields
    /// </summary>
    public static Conversation BuildValid()
    {
        var userId = ObjectId.GenerateNewId().ToString();
        return Create()
            .WithParticipant(userId)
            .Build();
    }

    /// <summary>
    /// Builds and returns a valid direct conversation between two users
    /// </summary>
    public static Conversation BuildDirectConversation(string userId1, string userId2)
    {
        return Create()
            .AsDirect()
            .WithName($"Direct: {userId1.Substring(0, 8)} - {userId2.Substring(0, 8)}")
            .WithParticipant(userId1)
            .WithParticipant(userId2)
            .Build();
    }

    /// <summary>
    /// Builds and returns a valid group conversation
    /// </summary>
    public static Conversation BuildGroupConversation(string name, params string[] userIds)
    {
        var factory = Create()
            .AsGroup()
            .WithName(name);

        foreach (var userId in userIds)
        {
            factory.WithParticipant(userId);
        }

        return factory.Build();
    }

    /// <summary>
    /// Builds and returns an invalid conversation (missing required fields)
    /// </summary>
    public static Conversation BuildInvalid()
    {
        return new Conversation
        {
            Type = "",
            Participants = new List<Participant>()
        };
    }
}
