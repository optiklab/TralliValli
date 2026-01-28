using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for MessageRepository
/// </summary>
public class MessageRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly MessageRepository _repository;

    public MessageRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new MessageRepository(_fixture.Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddMessage()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var message = new Message
        {
            ConversationId = "507f1f77bcf86cd799439011",
            SenderId = "507f1f77bcf86cd799439012",
            Content = "Test message",
            IsDeleted = false
        };

        // Act
        var result = await _repository.AddAsync(message);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("Test message", result.Content);
    }

    [Fact]
    public async Task FindAsync_ShouldFindMessagesByConversationId()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var conversationId = "507f1f77bcf86cd799439011";
        var message1 = new Message
        {
            ConversationId = conversationId,
            SenderId = "507f1f77bcf86cd799439012",
            Content = "Message 1"
        };
        var message2 = new Message
        {
            ConversationId = conversationId,
            SenderId = "507f1f77bcf86cd799439012",
            Content = "Message 2"
        };
        var message3 = new Message
        {
            ConversationId = "507f1f77bcf86cd799439099",
            SenderId = "507f1f77bcf86cd799439012",
            Content = "Message 3"
        };

        await _repository.AddAsync(message1);
        await _repository.AddAsync(message2);
        await _repository.AddAsync(message3);

        // Act
        var result = await _repository.FindAsync(m => m.ConversationId == conversationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, m => Assert.Equal(conversationId, m.ConversationId));
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteMessage()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var message = new Message
        {
            ConversationId = "507f1f77bcf86cd799439011",
            SenderId = "507f1f77bcf86cd799439012",
            Content = "Test"
        };
        var added = await _repository.AddAsync(message);

        // Act
        var result = await _repository.DeleteAsync(added.Id);

        // Assert
        Assert.True(result);
        var deleted = await _repository.GetByIdAsync(added.Id);
        Assert.Null(deleted);
    }
}
