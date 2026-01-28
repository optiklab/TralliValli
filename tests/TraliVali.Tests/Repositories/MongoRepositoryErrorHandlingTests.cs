using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for MongoRepository error handling
/// </summary>
public class MongoRepositoryErrorHandlingTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly UserRepository _repository;

    public MongoRepositoryErrorHandlingTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new UserRepository(_fixture.Context);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdIsInvalid()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var invalidId = "invalid-id";

        // Act
        var result = await _repository.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdIsEmpty()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var result = await _repository.GetByIdAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdIsWhitespace()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var result = await _repository.GetByIdAsync("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenIdIsInvalid()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user = new User { Email = "test@example.com", DisplayName = "Test" };
        var invalidId = "invalid-id";

        // Act
        var result = await _repository.UpdateAsync(invalidId, user);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenIdIsEmpty()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user = new User { Email = "test@example.com", DisplayName = "Test" };

        // Act
        var result = await _repository.UpdateAsync("", user);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var validId = "507f1f77bcf86cd799439011";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _repository.UpdateAsync(validId, null!);
        });
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenIdIsInvalid()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var invalidId = "invalid-id";

        // Act
        var result = await _repository.DeleteAsync(invalidId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenIdIsEmpty()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act
        var result = await _repository.DeleteAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
    {
        // Arrange
        await _fixture.CleanupAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _repository.AddAsync(null!);
        });
    }
}
