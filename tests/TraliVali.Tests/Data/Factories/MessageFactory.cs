using MongoDB.Bson;
using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Data.Factories;

/// <summary>
/// Factory class for generating Message test entities with builder pattern support
/// </summary>
public class MessageFactory
{
    private string _id = ObjectId.GenerateNewId().ToString();
    private string _conversationId = ObjectId.GenerateNewId().ToString();
    private string _senderId = ObjectId.GenerateNewId().ToString();
    private string _type = "text";
    private string _content = "Test message content";
    private string _encryptedContent = "";
    private string? _replyTo = null;
    private DateTime _createdAt = DateTime.UtcNow;
    private List<MessageReadStatus> _readBy = new();
    private DateTime? _editedAt = null;
    private bool _isDeleted = false;
    private List<string> _attachments = new();

    /// <summary>
    /// Creates a new MessageFactory instance
    /// </summary>
    public static MessageFactory Create() => new MessageFactory();

    /// <summary>
    /// Sets the message ID
    /// </summary>
    public MessageFactory WithId(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the conversation ID
    /// </summary>
    public MessageFactory WithConversationId(string conversationId)
    {
        _conversationId = conversationId;
        return this;
    }

    /// <summary>
    /// Sets the sender ID
    /// </summary>
    public MessageFactory WithSenderId(string senderId)
    {
        _senderId = senderId;
        return this;
    }

    /// <summary>
    /// Sets the message type
    /// </summary>
    public MessageFactory WithType(string type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the message as text type
    /// </summary>
    public MessageFactory AsText()
    {
        _type = "text";
        return this;
    }

    /// <summary>
    /// Sets the message as image type
    /// </summary>
    public MessageFactory AsImage()
    {
        _type = "image";
        return this;
    }

    /// <summary>
    /// Sets the message as file type
    /// </summary>
    public MessageFactory AsFile()
    {
        _type = "file";
        return this;
    }

    /// <summary>
    /// Sets the message as system type
    /// </summary>
    public MessageFactory AsSystem()
    {
        _type = "system";
        return this;
    }

    /// <summary>
    /// Sets the message content
    /// </summary>
    public MessageFactory WithContent(string content)
    {
        _content = content;
        return this;
    }

    /// <summary>
    /// Sets the encrypted content
    /// </summary>
    public MessageFactory WithEncryptedContent(string encryptedContent)
    {
        _encryptedContent = encryptedContent;
        return this;
    }

    /// <summary>
    /// Sets both content and encrypted content
    /// </summary>
    public MessageFactory WithBothContents(string content, string encryptedContent)
    {
        _content = content;
        _encryptedContent = encryptedContent;
        return this;
    }

    /// <summary>
    /// Sets the message as a reply to another message
    /// </summary>
    public MessageFactory AsReplyTo(string messageId)
    {
        _replyTo = messageId;
        return this;
    }

    /// <summary>
    /// Sets the created at timestamp
    /// </summary>
    public MessageFactory WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    /// <summary>
    /// Adds a read status for a user
    /// </summary>
    public MessageFactory WithReadBy(string userId, DateTime? readAt = null)
    {
        _readBy.Add(new MessageReadStatus
        {
            UserId = userId,
            ReadAt = readAt ?? DateTime.UtcNow
        });
        return this;
    }

    /// <summary>
    /// Sets the read by list
    /// </summary>
    public MessageFactory WithReadByList(List<MessageReadStatus> readBy)
    {
        _readBy = readBy;
        return this;
    }

    /// <summary>
    /// Sets the message as edited
    /// </summary>
    public MessageFactory AsEdited(DateTime? editedAt = null)
    {
        _editedAt = editedAt ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Sets the message as deleted
    /// </summary>
    public MessageFactory AsDeleted()
    {
        _isDeleted = true;
        return this;
    }

    /// <summary>
    /// Adds an attachment
    /// </summary>
    public MessageFactory WithAttachment(string attachmentUrl)
    {
        _attachments.Add(attachmentUrl);
        return this;
    }

    /// <summary>
    /// Sets the attachments list
    /// </summary>
    public MessageFactory WithAttachments(List<string> attachments)
    {
        _attachments = attachments;
        return this;
    }

    /// <summary>
    /// Builds and returns the Message entity
    /// </summary>
    public Message Build()
    {
        return new Message
        {
            Id = _id,
            ConversationId = _conversationId,
            SenderId = _senderId,
            Type = _type,
            Content = _content,
            EncryptedContent = _encryptedContent,
            ReplyTo = _replyTo,
            CreatedAt = _createdAt,
            ReadBy = _readBy,
            EditedAt = _editedAt,
            IsDeleted = _isDeleted,
            Attachments = _attachments
        };
    }

    /// <summary>
    /// Builds and returns a valid message with required fields
    /// </summary>
    public static Message BuildValid()
    {
        return Create().Build();
    }

    /// <summary>
    /// Builds and returns a valid text message
    /// </summary>
    public static Message BuildTextMessage(string conversationId, string senderId, string content)
    {
        return Create()
            .WithConversationId(conversationId)
            .WithSenderId(senderId)
            .WithContent(content)
            .AsText()
            .Build();
    }

    /// <summary>
    /// Builds and returns a valid encrypted message
    /// </summary>
    public static Message BuildEncryptedMessage(string conversationId, string senderId, string encryptedContent)
    {
        return Create()
            .WithConversationId(conversationId)
            .WithSenderId(senderId)
            .WithEncryptedContent(encryptedContent)
            .WithContent("")
            .AsText()
            .Build();
    }

    /// <summary>
    /// Builds and returns an invalid message (missing required fields)
    /// </summary>
    public static Message BuildInvalid()
    {
        return new Message
        {
            ConversationId = "",
            SenderId = "",
            Type = "",
            Content = "",
            EncryptedContent = ""
        };
    }
}
