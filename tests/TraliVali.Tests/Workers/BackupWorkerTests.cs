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

    [Theory]
    [InlineData(7, 7)]
    [InlineData(30, 30)]
    [InlineData(90, 90)]
    [InlineData(365, 365)]
    public void BackupWorkerConfiguration_RetentionDays_ShouldBeConfigurable(int configuredDays, int expectedDays)
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            RetentionDays = configuredDays
        };

        // Assert
        Assert.Equal(expectedDays, config.RetentionDays);
    }

    [Fact]
    public void BackupWorkerConfiguration_RetentionDays_ShouldHaveDefaultValue()
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Assert - Default retention should be 30 days
        Assert.Equal(30, config.RetentionDays);
    }

    [Theory]
    [InlineData(1, "Backup retention policy: Backups older than 1 day")]
    [InlineData(7, "Backup retention policy: Backups older than 7 days")]
    [InlineData(30, "Backup retention policy: Backups older than 30 days")]
    [InlineData(90, "Backup retention policy: Backups older than 90 days")]
    public void BackupWorkerConfiguration_ShouldSupportCustomRetentionPeriods(int retentionDays, string expectedLogMessage)
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            RetentionDays = retentionDays
        };

        // Assert
        Assert.Equal(retentionDays, config.RetentionDays);
        // Verify the retention configuration can be used in logging scenarios
        var logMessage = $"Backup retention policy: Backups older than {config.RetentionDays} days";
        Assert.Contains(expectedLogMessage, logMessage);
    }

    [Fact]
    public void BackupWorkerConfiguration_RetentionDays_ShouldRejectZero()
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => config.RetentionDays = 0);
        Assert.Contains("RetentionDays must be greater than 0", exception.Message);
    }

    [Fact]
    public void BackupWorkerConfiguration_RetentionDays_ShouldRejectNegativeValues()
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => config.RetentionDays = -10);
        Assert.Contains("RetentionDays must be greater than 0", exception.Message);
    }

    [Fact]
    public void BackupWorkerConfiguration_ShouldSupportDifferentBlobContainerNames()
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            BlobContainerName = "custom-backups"
        };

        // Assert
        Assert.Equal("custom-backups", config.BlobContainerName);
    }

    [Fact]
    public void BackupWorkerConfiguration_ShouldHaveDefaultBlobContainerName()
    {
        // Arrange
        var config = new BackupWorkerConfiguration();

        // Assert - Default container should be "backups"
        Assert.Equal("backups", config.BlobContainerName);
    }

    [Fact]
    public void BackupWorkerConfiguration_Validate_ShouldPassWithAllRequiredFields()
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            BlobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
            BlobContainerName = "backups",
            CronSchedule = "0 3 * * *",
            RetentionDays = 30
        };

        // Act & Assert - Should not throw
        config.Validate();
        Assert.Equal(30, config.RetentionDays);
    }

    [Theory]
    [InlineData("0 0 * * *", 30)]  // Midnight, 30 days
    [InlineData("0 2 * * *", 7)]   // 2 AM, 7 days
    [InlineData("0 3 * * *", 60)]  // 3 AM, 60 days
    [InlineData("0 4 * * 0", 90)]  // 4 AM Sundays, 90 days
    public void BackupWorkerConfiguration_ShouldSupportDifferentScheduleAndRetentionCombinations(
        string cronSchedule, int retentionDays)
    {
        // Arrange
        var config = new BackupWorkerConfiguration
        {
            CronSchedule = cronSchedule,
            RetentionDays = retentionDays,
            BlobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net"
        };

        // Act & Assert
        Assert.Equal(cronSchedule, config.CronSchedule);
        Assert.Equal(retentionDays, config.RetentionDays);
        config.Validate();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
