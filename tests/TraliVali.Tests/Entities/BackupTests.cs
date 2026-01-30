using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for Backup entity following Given-When-Then pattern
/// </summary>
public class BackupTests
{
    [Fact]
    public void GivenNewBackup_WhenCreated_ThenDefaultStatusIsPending()
    {
        // Arrange & Act
        var backup = new Backup();

        // Assert
        Assert.Equal(BackupStatus.Pending, backup.Status);
    }

    [Fact]
    public void GivenBackup_WhenStatusSetToInProgress_ThenStatusIsInProgress()
    {
        // Arrange
        var backup = new Backup
        {
            FilePath = "/backups/backup_20240101.zip",
            Size = 1024000,
            Type = "full"
        };

        // Act
        backup.Status = BackupStatus.InProgress;

        // Assert
        Assert.Equal(BackupStatus.InProgress, backup.Status);
    }

    [Fact]
    public void GivenBackup_WhenStatusSetToCompleted_ThenStatusIsCompleted()
    {
        // Arrange
        var backup = new Backup
        {
            FilePath = "/backups/backup_20240101.zip",
            Size = 1024000,
            Type = "full"
        };

        // Act
        backup.Status = BackupStatus.Completed;

        // Assert
        Assert.Equal(BackupStatus.Completed, backup.Status);
    }

    [Fact]
    public void GivenBackup_WhenStatusSetToFailed_ThenStatusIsFailedAndErrorMessageCanBeSet()
    {
        // Arrange
        var backup = new Backup
        {
            FilePath = "/backups/backup_20240101.zip",
            Size = 1024000,
            Type = "full"
        };

        // Act
        backup.Status = BackupStatus.Failed;
        backup.ErrorMessage = "Disk space full";

        // Assert
        Assert.Equal(BackupStatus.Failed, backup.Status);
        Assert.Equal("Disk space full", backup.ErrorMessage);
    }

    [Fact]
    public void GivenBackup_WhenAllPropertiesSet_ThenPropertiesAreCorrect()
    {
        // Arrange
        var filePath = "/backups/backup_20240101.zip";
        var size = 2048000L;
        var type = "incremental";
        var createdAt = DateTime.UtcNow;

        // Act
        var backup = new Backup
        {
            FilePath = filePath,
            Size = size,
            Type = type,
            CreatedAt = createdAt,
            Status = BackupStatus.Completed
        };

        // Assert
        Assert.Equal(filePath, backup.FilePath);
        Assert.Equal(size, backup.Size);
        Assert.Equal(type, backup.Type);
        Assert.Equal(createdAt, backup.CreatedAt);
        Assert.Equal(BackupStatus.Completed, backup.Status);
    }

    [Fact]
    public void GivenBackup_WhenErrorMessageIsNull_ThenErrorMessageIsNull()
    {
        // Arrange & Act
        var backup = new Backup
        {
            FilePath = "/backups/backup_20240101.zip",
            Size = 1024000,
            Type = "full",
            Status = BackupStatus.Completed
        };

        // Assert
        Assert.Null(backup.ErrorMessage);
    }
}

/// <summary>
/// Tests for BackupStatus enum
/// </summary>
public class BackupStatusTests
{
    [Fact]
    public void GivenBackupStatus_WhenConvertingToString_ThenReturnsCorrectNames()
    {
        // Arrange & Act & Assert
        Assert.Equal("Pending", BackupStatus.Pending.ToString());
        Assert.Equal("InProgress", BackupStatus.InProgress.ToString());
        Assert.Equal("Completed", BackupStatus.Completed.ToString());
        Assert.Equal("Failed", BackupStatus.Failed.ToString());
    }
}
