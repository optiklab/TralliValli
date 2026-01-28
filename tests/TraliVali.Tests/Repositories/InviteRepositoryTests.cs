using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for InviteRepository
/// </summary>
public class InviteRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly InviteRepository _repository;

    public InviteRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new InviteRepository(_fixture.Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddInvite()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "invitee@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false
        };

        // Act
        var result = await _repository.AddAsync(invite);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("invitee@example.com", result.Email);
        Assert.False(result.IsUsed);
    }

    [Fact]
    public async Task FindAsync_ShouldFindInviteByToken()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var token = Guid.NewGuid().ToString();
        var invite = new Invite
        {
            Token = token,
            Email = "test@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await _repository.AddAsync(invite);

        // Act
        var result = await _repository.FindAsync(i => i.Token == token);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(token, result.First().Token);
    }

    [Fact]
    public async Task FindAsync_ShouldFindUnusedInvites()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var invite1 = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "test1@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false
        };
        var invite2 = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "test2@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = true,
            UsedBy = "507f1f77bcf86cd799439012",
            UsedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(invite1);
        await _repository.AddAsync(invite2);

        // Act
        var result = await _repository.FindAsync(i => !i.IsUsed);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.False(result.First().IsUsed);
    }

    [Fact]
    public async Task UpdateAsync_ShouldMarkInviteAsUsed()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false
        };
        var added = await _repository.AddAsync(invite);
        added.IsUsed = true;
        added.UsedAt = DateTime.UtcNow;

        // Act
        var result = await _repository.UpdateAsync(added.Id, added);

        // Assert
        Assert.True(result);
        var updated = await _repository.GetByIdAsync(added.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsUsed);
        Assert.NotNull(updated.UsedAt);
    }
}
