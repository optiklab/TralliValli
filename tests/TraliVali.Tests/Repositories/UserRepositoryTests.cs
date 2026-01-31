using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Data.Factories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for UserRepository
/// </summary>
public class UserRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly UserRepository _repository;

    public UserRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new UserRepository(_fixture.Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUser()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user = UserFactory.Create()
            .WithEmail("test@example.com")
            .WithDisplayName("Test User")
            .Build();

        // Act
        var result = await _repository.AddAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user = UserFactory.Create()
            .WithEmail("test@example.com")
            .WithDisplayName("Test User")
            .Build();
        var addedUser = await _repository.AddAsync(user);

        // Act
        var result = await _repository.GetByIdAsync(addedUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(addedUser.Id, result.Id);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user1 = UserFactory.Create().WithEmail("user1@example.com").WithDisplayName("User 1").Build();
        var user2 = UserFactory.Create().WithEmail("user2@example.com").WithDisplayName("User 2").Build();
        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task FindAsync_ShouldReturnMatchingUsers()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user1 = UserFactory.Create().WithEmail("test1@example.com").AsActive().Build();
        var user2 = UserFactory.Create().WithEmail("test2@example.com").AsInactive().Build();
        var user3 = UserFactory.Create().WithEmail("test3@example.com").AsActive().Build();
        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);
        await _repository.AddAsync(user3);

        // Act
        var result = await _repository.FindAsync(u => u.IsActive);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, u => Assert.True(u.IsActive));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user = UserFactory.Create()
            .WithEmail("test@example.com")
            .WithDisplayName("Old Name")
            .Build();
        var addedUser = await _repository.AddAsync(user);

        addedUser.DisplayName = "New Name";

        // Act
        var result = await _repository.UpdateAsync(addedUser.Id, addedUser);

        // Assert
        Assert.True(result);

        var updatedUser = await _repository.GetByIdAsync(addedUser.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("New Name", updatedUser.DisplayName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user = UserFactory.Create()
            .WithId("507f1f77bcf86cd799439011")
            .WithEmail("test@example.com")
            .WithDisplayName("Test")
            .Build();

        // Act
        var result = await _repository.UpdateAsync(user.Id, user);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUser()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var user = new User { Email = "test@example.com", DisplayName = "Test User" };
        var addedUser = await _repository.AddAsync(user);

        // Act
        var result = await _repository.DeleteAsync(addedUser.Id);

        // Assert
        Assert.True(result);

        var deletedUser = await _repository.GetByIdAsync(addedUser.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var nonExistentId = "507f1f77bcf86cd799439011";

        // Act
        var result = await _repository.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnTotalCount_WhenNoPredicateProvided()
    {
        // Arrange
        await _fixture.CleanupAsync();
        await _repository.AddAsync(new User { Email = "user1@example.com", DisplayName = "User 1" });
        await _repository.AddAsync(new User { Email = "user2@example.com", DisplayName = "User 2" });
        await _repository.AddAsync(new User { Email = "user3@example.com", DisplayName = "User 3" });

        // Act
        var result = await _repository.CountAsync();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnMatchingCount_WhenPredicateProvided()
    {
        // Arrange
        await _fixture.CleanupAsync();
        await _repository.AddAsync(new User { Email = "user1@example.com", DisplayName = "User 1", IsActive = true });
        await _repository.AddAsync(new User { Email = "user2@example.com", DisplayName = "User 2", IsActive = false });
        await _repository.AddAsync(new User { Email = "user3@example.com", DisplayName = "User 3", IsActive = true });

        // Act
        var result = await _repository.CountAsync(u => u.IsActive);

        // Assert
        Assert.Equal(2, result);
    }
}
