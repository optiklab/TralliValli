using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for ConversationRepository
/// </summary>
public class ConversationRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly ConversationRepository _repository;

    public ConversationRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new ConversationRepository(_fixture.Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddConversation()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var conversation = new Conversation
        {
            Name = "Test Conversation",
            IsGroup = false,
            Participants = new List<Participant>
            {
                new Participant { UserId = "507f1f77bcf86cd799439011" },
                new Participant { UserId = "507f1f77bcf86cd799439012" }
            }
        };

        // Act
        var result = await _repository.AddAsync(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("Test Conversation", result.Name);
        Assert.Equal(2, result.Participants.Count);
    }

    [Fact]
    public async Task FindAsync_ShouldFindConversationsByParticipant()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var userId = "507f1f77bcf86cd799439011";
        var conversation1 = new Conversation
        {
            Name = "Conv 1",
            Participants = new List<Participant>
            {
                new Participant { UserId = userId },
                new Participant { UserId = "507f1f77bcf86cd799439012" }
            }
        };
        var conversation2 = new Conversation
        {
            Name = "Conv 2",
            Participants = new List<Participant>
            {
                new Participant { UserId = userId },
                new Participant { UserId = "507f1f77bcf86cd799439013" }
            }
        };
        var conversation3 = new Conversation
        {
            Name = "Conv 3",
            Participants = new List<Participant>
            {
                new Participant { UserId = "507f1f77bcf86cd799439014" },
                new Participant { UserId = "507f1f77bcf86cd799439015" }
            }
        };

        await _repository.AddAsync(conversation1);
        await _repository.AddAsync(conversation2);
        await _repository.AddAsync(conversation3);

        // Act
        var result = await _repository.FindAsync(c => c.Participants.Any(p => p.UserId == userId));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateConversation()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var conversation = new Conversation
        {
            Name = "Old Name",
            Participants = new List<Participant>()
        };
        var added = await _repository.AddAsync(conversation);
        added.Name = "New Name";

        // Act
        var result = await _repository.UpdateAsync(added.Id, added);

        // Assert
        Assert.True(result);
        var updated = await _repository.GetByIdAsync(added.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
    }
}
