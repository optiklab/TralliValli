using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for BackupRepository
/// </summary>
public class BackupRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly BackupRepository _repository;

    public BackupRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new BackupRepository(_fixture.Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddBackup()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup = new Backup
        {
            FilePath = "/backups/backup_2024_01_01.tar.gz",
            Size = 1024000,
            Type = "full",
            Status = BackupStatus.Pending
        };

        // Act
        var result = await _repository.AddAsync(backup);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("/backups/backup_2024_01_01.tar.gz", result.FilePath);
        Assert.Equal(BackupStatus.Pending, result.Status);
    }

    [Fact]
    public async Task FindAsync_ShouldFindCompletedBackups()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup1 = new Backup
        {
            FilePath = "/backups/backup1.tar.gz",
            Size = 1024,
            Type = "full",
            Status = BackupStatus.Completed
        };
        var backup2 = new Backup
        {
            FilePath = "/backups/backup2.tar.gz",
            Size = 2048,
            Type = "incremental",
            Status = BackupStatus.Failed,
            ErrorMessage = "Error occurred"
        };
        var backup3 = new Backup
        {
            FilePath = "/backups/backup3.tar.gz",
            Size = 512,
            Type = "full",
            Status = BackupStatus.Completed
        };

        await _repository.AddAsync(backup1);
        await _repository.AddAsync(backup2);
        await _repository.AddAsync(backup3);

        // Act
        var result = await _repository.FindAsync(b => b.Status == BackupStatus.Completed);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal(BackupStatus.Completed, b.Status));
    }

    [Fact]
    public async Task FindAsync_ShouldFindBackupsByType()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup1 = new Backup { FilePath = "/backups/full1.tar.gz", Size = 1024, Type = "full", Status = BackupStatus.Completed };
        var backup2 = new Backup { FilePath = "/backups/inc1.tar.gz", Size = 512, Type = "incremental", Status = BackupStatus.Completed };
        var backup3 = new Backup { FilePath = "/backups/full2.tar.gz", Size = 2048, Type = "full", Status = BackupStatus.Completed };

        await _repository.AddAsync(backup1);
        await _repository.AddAsync(backup2);
        await _repository.AddAsync(backup3);

        // Act
        var result = await _repository.FindAsync(b => b.Type == "full");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal("full", b.Type));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBackupStatus()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup = new Backup
        {
            FilePath = "/backups/test.tar.gz",
            Size = 1024,
            Type = "full",
            Status = BackupStatus.Pending
        };
        var added = await _repository.AddAsync(backup);
        added.Status = BackupStatus.Completed;

        // Act
        var result = await _repository.UpdateAsync(added.Id, added);

        // Assert
        Assert.True(result);
        var updated = await _repository.GetByIdAsync(added.Id);
        Assert.NotNull(updated);
        Assert.Equal(BackupStatus.Completed, updated.Status);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSetErrorMessage_WhenBackupFails()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var backup = new Backup
        {
            FilePath = "/backups/test.tar.gz",
            Size = 1024,
            Type = "full",
            Status = BackupStatus.InProgress
        };
        var added = await _repository.AddAsync(backup);
        added.Status = BackupStatus.Failed;
        added.ErrorMessage = "Disk full";

        // Act
        var result = await _repository.UpdateAsync(added.Id, added);

        // Assert
        Assert.True(result);
        var updated = await _repository.GetByIdAsync(added.Id);
        Assert.NotNull(updated);
        Assert.Equal(BackupStatus.Failed, updated.Status);
        Assert.Equal("Disk full", updated.ErrorMessage);
    }
}
