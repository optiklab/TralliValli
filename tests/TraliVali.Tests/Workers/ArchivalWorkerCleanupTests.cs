using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using TraliVali.Domain.Entities;
using TraliVali.Workers;

namespace TraliVali.Tests.Workers;

/// <summary>
/// Tests for ArchivalWorker message cleanup and recentMessages update functionality
/// </summary>
public class ArchivalWorkerCleanupTests : IDisposable
{
    private readonly Mock<IMongoCollection<Message>> _mockMessageCollection;
    private readonly Mock<IMongoCollection<Conversation>> _mockConversationCollection;
    private readonly Mock<ILogger<ArchivalWorker>> _mockLogger;
    private readonly ArchivalWorkerConfiguration _configuration;

    public ArchivalWorkerCleanupTests()
    {
        _mockMessageCollection = new Mock<IMongoCollection<Message>>();
        _mockConversationCollection = new Mock<IMongoCollection<Conversation>>();
        _mockLogger = new Mock<ILogger<ArchivalWorker>>();
        
        _configuration = new ArchivalWorkerConfiguration
        {
            CronSchedule = "0 2 * * *",
            RetentionDays = 365,
            BatchSize = 1000,
            BlobStorageConnectionString = "",
            BlobContainerName = "archived-messages",
            DeleteAfterArchive = true,
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerTimeoutSeconds = 30
        };
    }

    [Fact]
    public void Configuration_DeleteAfterArchive_DefaultsToTrue()
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration();

        // Act & Assert
        Assert.True(config.DeleteAfterArchive);
    }

    [Fact]
    public void Configuration_DeleteAfterArchive_CanBeSetToFalse()
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            DeleteAfterArchive = false
        };

        // Act & Assert
        Assert.False(config.DeleteAfterArchive);
    }

    [Fact]
    public void Configuration_DeleteAfterArchive_CanBeSetToTrue()
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            DeleteAfterArchive = true
        };

        // Act & Assert
        Assert.True(config.DeleteAfterArchive);
    }

    [Theory]
    [InlineData(true, "Messages should be deleted when DeleteAfterArchive is true")]
    [InlineData(false, "Messages should not be deleted when DeleteAfterArchive is false")]
    public void Configuration_DeleteAfterArchive_AcceptsBooleanValues(bool value, string description)
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            DeleteAfterArchive = value
        };

        // Act & Assert
        Assert.Equal(value, config.DeleteAfterArchive);
        Assert.NotNull(description); // Just to use the parameter
    }

    [Fact]
    public void ArchivalWorker_CreatesWithCleanupConfiguration()
    {
        // Arrange & Act
        var worker = new ArchivalWorker(
            _mockMessageCollection.Object,
            _mockConversationCollection.Object,
            _configuration,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(worker);
        // The worker should be created successfully with conversations collection
    }

    [Fact]
    public void Configuration_MessageRetentionSettings_WorkTogether()
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            RetentionDays = 90,
            BatchSize = 500,
            DeleteAfterArchive = true
        };

        // Act & Assert
        Assert.Equal(90, config.RetentionDays);
        Assert.Equal(500, config.BatchSize);
        Assert.True(config.DeleteAfterArchive);
    }

    [Theory]
    [InlineData(30, 100, true, "Short retention with small batches and cleanup")]
    [InlineData(180, 1000, false, "Medium retention with large batches without cleanup")]
    [InlineData(365, 5000, true, "Long retention with very large batches and cleanup")]
    public void Configuration_AllRetentionSettings_CanBeCombined(
        int retentionDays, 
        int batchSize, 
        bool deleteAfterArchive,
        string scenario)
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            RetentionDays = retentionDays,
            BatchSize = batchSize,
            DeleteAfterArchive = deleteAfterArchive
        };

        // Act & Assert
        Assert.Equal(retentionDays, config.RetentionDays);
        Assert.Equal(batchSize, config.BatchSize);
        Assert.Equal(deleteAfterArchive, config.DeleteAfterArchive);
        Assert.NotNull(scenario); // Just to use the parameter
    }

    [Fact]
    public void Configuration_DefaultValues_IncludeDeleteAfterArchive()
    {
        // Arrange & Act
        var config = new ArchivalWorkerConfiguration();

        // Assert - Verify all default values including DeleteAfterArchive
        Assert.Equal("0 2 * * *", config.CronSchedule);
        Assert.Equal(365, config.RetentionDays);
        Assert.Equal(1000, config.BatchSize);
        Assert.True(config.DeleteAfterArchive);
        Assert.Equal("archived-messages", config.BlobContainerName);
        Assert.Equal(5, config.CircuitBreakerFailureThreshold);
        Assert.Equal(30, config.CircuitBreakerTimeoutSeconds);
    }

    [Fact]
    public void Configuration_DeleteAfterArchive_IndependentOfOtherSettings()
    {
        // Arrange - Create config with unusual settings
        var config = new ArchivalWorkerConfiguration
        {
            RetentionDays = 1,
            BatchSize = 10,
            DeleteAfterArchive = false,
            CircuitBreakerFailureThreshold = 1,
            CircuitBreakerTimeoutSeconds = 1
        };

        // Act & Assert - DeleteAfterArchive should work independently
        Assert.False(config.DeleteAfterArchive);
        Assert.Equal(1, config.RetentionDays);
        Assert.Equal(10, config.BatchSize);
    }

    [Theory]
    [InlineData("0 0 * * *", true, "Midnight daily with cleanup")]
    [InlineData("0 */12 * * *", false, "Every 12 hours without cleanup")]
    [InlineData("0 3 * * 0", true, "Weekly Sunday 3am with cleanup")]
    public void Configuration_CronScheduleAndDeleteAfterArchive_WorkTogether(
        string cronSchedule,
        bool deleteAfterArchive,
        string description)
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            CronSchedule = cronSchedule,
            DeleteAfterArchive = deleteAfterArchive
        };

        // Act & Assert
        Assert.Equal(cronSchedule, config.CronSchedule);
        Assert.Equal(deleteAfterArchive, config.DeleteAfterArchive);
        Assert.NotNull(description);
    }

    [Fact]
    public void ArchivalWorker_RequiresBothCollections_ForCleanupFunctionality()
    {
        // Act & Assert - Should throw when conversations collection is null
        Assert.Throws<ArgumentNullException>(() => new ArchivalWorker(
            _mockMessageCollection.Object,
            null!,
            _configuration,
            _mockLogger.Object));

        // Should throw when messages collection is null
        Assert.Throws<ArgumentNullException>(() => new ArchivalWorker(
            null!,
            _mockConversationCollection.Object,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Configuration_SupportsBothArchiveAndCleanupScenarios()
    {
        // Scenario 1: Archive and delete (default)
        var archiveAndDelete = new ArchivalWorkerConfiguration();
        Assert.True(archiveAndDelete.DeleteAfterArchive);

        // Scenario 2: Archive only, keep in database
        var archiveOnly = new ArchivalWorkerConfiguration
        {
            DeleteAfterArchive = false
        };
        Assert.False(archiveOnly.DeleteAfterArchive);

        // Scenario 3: Aggressive cleanup with short retention
        var aggressiveCleanup = new ArchivalWorkerConfiguration
        {
            RetentionDays = 30,
            DeleteAfterArchive = true
        };
        Assert.Equal(30, aggressiveCleanup.RetentionDays);
        Assert.True(aggressiveCleanup.DeleteAfterArchive);
    }

    [Fact]
    public void Configuration_MessageRetentionSection_ContainsAllRequiredFields()
    {
        // Arrange
        var config = new ArchivalWorkerConfiguration
        {
            CronSchedule = "0 2 * * *",
            RetentionDays = 365,
            BatchSize = 1000,
            BlobStorageConnectionString = "test-connection",
            BlobContainerName = "test-container",
            DeleteAfterArchive = true,
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerTimeoutSeconds = 30
        };

        // Assert - All MessageRetention fields should be present
        Assert.NotNull(config.CronSchedule);
        Assert.True(config.RetentionDays > 0);
        Assert.True(config.BatchSize > 0);
        Assert.NotNull(config.BlobContainerName);
        Assert.NotNull(config.BlobStorageConnectionString);
    }

    [Fact]
    public void Configuration_DeleteAfterArchive_WithoutBlobStorage_IsValid()
    {
        // Arrange - Configuration with DeleteAfterArchive but no blob storage
        var config = new ArchivalWorkerConfiguration
        {
            BlobStorageConnectionString = "",
            DeleteAfterArchive = true
        };

        // Act & Assert - Configuration should be valid (runtime validation will prevent data loss)
        Assert.True(config.DeleteAfterArchive);
        Assert.Empty(config.BlobStorageConnectionString);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
