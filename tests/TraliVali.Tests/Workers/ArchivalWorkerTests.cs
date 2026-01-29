using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using TraliVali.Domain.Entities;
using TraliVali.Workers;

namespace TraliVali.Tests.Workers;

/// <summary>
/// Tests for ArchivalWorker
/// Note: Full integration tests for archival require actual MongoDB and Azure Blob Storage.
/// These tests focus on constructor validation and basic structure verification.
/// </summary>
public class ArchivalWorkerTests : IDisposable
{
    private readonly Mock<IMongoCollection<Message>> _mockMessageCollection;
    private readonly Mock<ILogger<ArchivalWorker>> _mockLogger;
    private readonly ArchivalWorkerConfiguration _configuration;

    public ArchivalWorkerTests()
    {
        _mockMessageCollection = new Mock<IMongoCollection<Message>>();
        _mockLogger = new Mock<ILogger<ArchivalWorker>>();
        
        _configuration = new ArchivalWorkerConfiguration
        {
            CronSchedule = "0 2 * * *",
            RetentionDays = 365,
            BatchSize = 1000,
            BlobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
            BlobContainerName = "archived-messages",
            DeleteAfterArchive = true,
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerTimeoutSeconds = 30
        };
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMessagesCollectionIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ArchivalWorker(
            null!,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ArchivalWorker(
            _mockMessageCollection.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ArchivalWorker(
            _mockMessageCollection.Object,
            _configuration,
            null!));
    }

    [Fact]
    public void Constructor_ShouldCreateWorkerSuccessfully_WhenAllParametersAreValid()
    {
        // Act
        var worker = new ArchivalWorker(
            _mockMessageCollection.Object,
            _configuration,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void ArchivalWorkerConfiguration_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ArchivalWorkerConfiguration();

        // Assert
        Assert.Equal("0 2 * * *", config.CronSchedule);
        Assert.Equal(365, config.RetentionDays);
        Assert.Equal(1000, config.BatchSize);
        Assert.Equal(string.Empty, config.BlobStorageConnectionString);
        Assert.Equal("archived-messages", config.BlobContainerName);
        Assert.True(config.DeleteAfterArchive);
        Assert.Equal(5, config.CircuitBreakerFailureThreshold);
        Assert.Equal(30, config.CircuitBreakerTimeoutSeconds);
    }

    [Theory]
    [InlineData("0 2 * * *")] // Daily at 2 AM
    [InlineData("0 0 * * 0")] // Weekly on Sunday at midnight
    [InlineData("0 */6 * * *")] // Every 6 hours
    public void ArchivalWorkerConfiguration_ShouldAcceptValidCronSchedules(string cronSchedule)
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            CronSchedule = cronSchedule
        };

        // Act & Assert
        Assert.Equal(cronSchedule, config.CronSchedule);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(365)]
    [InlineData(730)]
    public void ArchivalWorkerConfiguration_ShouldAcceptValidRetentionDays(int retentionDays)
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            RetentionDays = retentionDays
        };

        // Act & Assert
        Assert.Equal(retentionDays, config.RetentionDays);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void ArchivalWorkerConfiguration_ShouldAcceptValidBatchSizes(int batchSize)
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            BatchSize = batchSize
        };

        // Act & Assert
        Assert.Equal(batchSize, config.BatchSize);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ArchivalWorkerConfiguration_ShouldAcceptDeleteAfterArchiveFlag(bool deleteAfterArchive)
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            DeleteAfterArchive = deleteAfterArchive
        };

        // Act & Assert
        Assert.Equal(deleteAfterArchive, config.DeleteAfterArchive);
    }

    [Theory]
    [InlineData(3, 15)]
    [InlineData(5, 30)]
    [InlineData(10, 60)]
    public void ArchivalWorkerConfiguration_ShouldAcceptCircuitBreakerSettings(int threshold, int timeoutSeconds)
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            CircuitBreakerFailureThreshold = threshold,
            CircuitBreakerTimeoutSeconds = timeoutSeconds
        };

        // Act & Assert
        Assert.Equal(threshold, config.CircuitBreakerFailureThreshold);
        Assert.Equal(timeoutSeconds, config.CircuitBreakerTimeoutSeconds);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
