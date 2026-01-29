using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using TraliVali.Workers;

namespace TraliVali.Tests.Workers;

/// <summary>
/// Tests for BackupWorker
/// Note: Full integration tests for backup require actual MongoDB and Azure Blob Storage.
/// These tests focus on constructor validation and basic structure verification.
/// </summary>
public class BackupWorkerTests : IDisposable
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<ILogger<BackupWorker>> _mockLogger;
    private readonly BackupWorkerConfiguration _configuration;

    public BackupWorkerTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockLogger = new Mock<ILogger<BackupWorker>>();
        
        _configuration = new BackupWorkerConfiguration
        {
            CronSchedule = "0 3 * * *",
            BlobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
            BlobContainerName = "backups",
            RetentionDays = 30,
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerTimeoutSeconds = 30
        };
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDatabaseIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BackupWorker(
            null!,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BackupWorker(
            _mockDatabase.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BackupWorker(
            _mockDatabase.Object,
            _configuration,
            null!));
    }

    [Fact]
    public void Constructor_ShouldCreateWorkerSuccessfully_WhenAllParametersAreValid()
    {
        // Act
        var worker = new BackupWorker(
            _mockDatabase.Object,
            _configuration,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void BackupWorkerConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Assert
        Assert.Equal("0 3 * * *", config.CronSchedule);
        Assert.Equal(string.Empty, config.BlobStorageConnectionString);
        Assert.Equal("backups", config.BlobContainerName);
        Assert.Equal(30, config.RetentionDays);
        Assert.Equal(5, config.CircuitBreakerFailureThreshold);
        Assert.Equal(30, config.CircuitBreakerTimeoutSeconds);
    }

    [Fact]
    public void BackupWorkerConfiguration_ShouldAllowCustomValues()
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            CronSchedule = "0 4 * * *",
            BlobStorageConnectionString = "custom-connection-string",
            BlobContainerName = "custom-backups",
            RetentionDays = 60,
            CircuitBreakerFailureThreshold = 10,
            CircuitBreakerTimeoutSeconds = 60
        };

        // Assert
        Assert.Equal("0 4 * * *", config.CronSchedule);
        Assert.Equal("custom-connection-string", config.BlobStorageConnectionString);
        Assert.Equal("custom-backups", config.BlobContainerName);
        Assert.Equal(60, config.RetentionDays);
        Assert.Equal(10, config.CircuitBreakerFailureThreshold);
        Assert.Equal(60, config.CircuitBreakerTimeoutSeconds);
    }

    [Theory]
    [InlineData("0 3 * * *")]  // 3 AM daily
    [InlineData("0 0 * * *")]  // Midnight daily
    [InlineData("0 12 * * 0")] // Noon every Sunday
    public void BackupWorkerConfiguration_ShouldAcceptValidCronExpressions(string cronSchedule)
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            CronSchedule = cronSchedule
        };

        // Assert
        Assert.Equal(cronSchedule, config.CronSchedule);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(365)]
    public void BackupWorkerConfiguration_ShouldAcceptValidRetentionDays(int retentionDays)
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            RetentionDays = retentionDays
        };

        // Assert
        Assert.Equal(retentionDays, config.RetentionDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-30)]
    public void BackupWorkerConfiguration_ShouldThrowOnInvalidRetentionDays(int retentionDays)
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.RetentionDays = retentionDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void BackupWorkerConfiguration_ShouldThrowOnInvalidCircuitBreakerFailureThreshold(int threshold)
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.CircuitBreakerFailureThreshold = threshold);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-30)]
    public void BackupWorkerConfiguration_ShouldThrowOnInvalidCircuitBreakerTimeoutSeconds(int timeout)
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.CircuitBreakerTimeoutSeconds = timeout);
    }

    [Fact]
    public void BackupWorkerConfiguration_Validate_ShouldThrowWhenBlobStorageConnectionStringIsEmpty()
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            BlobStorageConnectionString = ""
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void BackupWorkerConfiguration_Validate_ShouldThrowWhenBlobContainerNameIsEmpty()
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            BlobStorageConnectionString = "valid-connection-string",
            BlobContainerName = ""
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void BackupWorkerConfiguration_Validate_ShouldThrowWhenCronScheduleIsEmpty()
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            BlobStorageConnectionString = "valid-connection-string",
            CronSchedule = ""
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void BackupWorkerConfiguration_Validate_ShouldPassWithValidConfiguration()
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            BlobStorageConnectionString = "valid-connection-string",
            BlobContainerName = "backups",
            CronSchedule = "0 3 * * *"
        };

        // Act & Assert - Should not throw
        config.Validate();
    }

    [Fact]
    public void BackupWorkerConfiguration_ShouldHaveCorrectDefaultCronScheduleFor3AM()
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Assert - Should run at 3 AM daily
        Assert.Equal("0 3 * * *", config.CronSchedule);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
