using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for UserKeyBackupRepository
/// </summary>
public class UserKeyBackupRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly UserKeyBackupRepository _repository;

    public UserKeyBackupRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new UserKeyBackupRepository(_fixture.Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserKeyBackup()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "base64encodeddata==",
            Iv = "base64iv==",
            Salt = "base64salt=="
        };

        // Act
        var result = await _repository.AddAsync(backup);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("507f1f77bcf86cd799439011", result.UserId);
        Assert.Equal(1, result.Version);
        Assert.Equal("base64encodeddata==", result.EncryptedData);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUserKeyBackup_WhenBackupExists()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "data==",
            Iv = "iv==",
            Salt = "salt=="
        };
        var added = await _repository.AddAsync(backup);

        // Act
        var result = await _repository.GetByIdAsync(added.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(added.Id, result.Id);
        Assert.Equal("507f1f77bcf86cd799439011", result.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenBackupDoesNotExist()
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
    public async Task GetAllAsync_ShouldReturnAllUserKeyBackups()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup1 = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "data1==",
            Iv = "iv1==",
            Salt = "salt1=="
        };
        var backup2 = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439012",
            Version = 1,
            EncryptedData = "data2==",
            Iv = "iv2==",
            Salt = "salt2=="
        };
        await _repository.AddAsync(backup1);
        await _repository.AddAsync(backup2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task FindAsync_ShouldReturnMatchingUserKeyBackups()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var userId = "507f1f77bcf86cd799439011";
        var backup1 = new UserKeyBackup
        {
            UserId = userId,
            Version = 1,
            EncryptedData = "data1==",
            Iv = "iv1==",
            Salt = "salt1=="
        };
        var backup2 = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439012",
            Version = 1,
            EncryptedData = "data2==",
            Iv = "iv2==",
            Salt = "salt2=="
        };
        await _repository.AddAsync(backup1);
        await _repository.AddAsync(backup2);

        // Act
        var result = await _repository.FindAsync(b => b.UserId == userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(userId, result.First().UserId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUserKeyBackup()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "olddata==",
            Iv = "iv==",
            Salt = "salt=="
        };
        var added = await _repository.AddAsync(backup);
        added.Version = 2;
        added.EncryptedData = "newdata==";

        // Act
        var result = await _repository.UpdateAsync(added.Id, added);

        // Assert
        Assert.True(result);

        var updated = await _repository.GetByIdAsync(added.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Version);
        Assert.Equal("newdata==", updated.EncryptedData);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenBackupDoesNotExist()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup = new UserKeyBackup
        {
            Id = "507f1f77bcf86cd799439011",
            UserId = "507f1f77bcf86cd799439012",
            Version = 1,
            EncryptedData = "data==",
            Iv = "iv==",
            Salt = "salt=="
        };

        // Act
        var result = await _repository.UpdateAsync(backup.Id, backup);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUserKeyBackup()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup = new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "data==",
            Iv = "iv==",
            Salt = "salt=="
        };
        var added = await _repository.AddAsync(backup);

        // Act
        var result = await _repository.DeleteAsync(added.Id);

        // Assert
        Assert.True(result);

        var deleted = await _repository.GetByIdAsync(added.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenBackupDoesNotExist()
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
        await _repository.AddAsync(new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "data1==",
            Iv = "iv1==",
            Salt = "salt1=="
        });
        await _repository.AddAsync(new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439012",
            Version = 1,
            EncryptedData = "data2==",
            Iv = "iv2==",
            Salt = "salt2=="
        });

        // Act
        var result = await _repository.CountAsync();

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnMatchingCount_WhenPredicateProvided()
    {
        // Arrange
        await _fixture.CleanupAsync();
        await _repository.AddAsync(new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439011",
            Version = 1,
            EncryptedData = "data1==",
            Iv = "iv1==",
            Salt = "salt1=="
        });
        await _repository.AddAsync(new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439012",
            Version = 2,
            EncryptedData = "data2==",
            Iv = "iv2==",
            Salt = "salt2=="
        });
        await _repository.AddAsync(new UserKeyBackup
        {
            UserId = "507f1f77bcf86cd799439013",
            Version = 2,
            EncryptedData = "data3==",
            Iv = "iv3==",
            Salt = "salt3=="
        });

        // Act
        var result = await _repository.CountAsync(b => b.Version == 2);

        // Assert
        Assert.Equal(2, result);
    }
}
