using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for ArchivalStats entity following Given-When-Then pattern
/// </summary>
public class ArchivalStatsTests
{
    [Fact]
    public void GivenNewArchivalStats_WhenCreated_ThenDefaultStatusIsSuccess()
    {
        // Arrange & Act
        var stats = new ArchivalStats();

        // Assert
        Assert.Equal("Success", stats.Status);
    }

    [Fact]
    public void GivenArchivalStats_WhenAllPropertiesSet_ThenPropertiesAreCorrect()
    {
        // Arrange
        var runAt = DateTime.UtcNow;
        var messagesArchived = 1500;
        var storageUsed = 5242880L; // 5MB

        // Act
        var stats = new ArchivalStats
        {
            RunAt = runAt,
            MessagesArchived = messagesArchived,
            StorageUsed = storageUsed,
            Status = "Success"
        };

        // Assert
        Assert.Equal(runAt, stats.RunAt);
        Assert.Equal(messagesArchived, stats.MessagesArchived);
        Assert.Equal(storageUsed, stats.StorageUsed);
        Assert.Equal("Success", stats.Status);
    }

    [Fact]
    public void GivenArchivalStats_WhenStatusSetToFailed_ThenStatusIsFailedAndErrorMessageCanBeSet()
    {
        // Arrange
        var stats = new ArchivalStats
        {
            RunAt = DateTime.UtcNow,
            MessagesArchived = 0,
            StorageUsed = 0
        };

        // Act
        stats.Status = "Failed";
        stats.ErrorMessage = "Database connection timeout";

        // Assert
        Assert.Equal("Failed", stats.Status);
        Assert.Equal("Database connection timeout", stats.ErrorMessage);
    }

    [Fact]
    public void GivenArchivalStats_WhenErrorMessageIsNull_ThenErrorMessageIsNull()
    {
        // Arrange & Act
        var stats = new ArchivalStats
        {
            RunAt = DateTime.UtcNow,
            MessagesArchived = 1000,
            StorageUsed = 4194304L, // 4MB
            Status = "Success"
        };

        // Assert
        Assert.Null(stats.ErrorMessage);
    }

    [Fact]
    public void GivenArchivalStats_WhenNoMessagesArchived_ThenMessagesArchivedIsZero()
    {
        // Arrange & Act
        var stats = new ArchivalStats
        {
            RunAt = DateTime.UtcNow,
            MessagesArchived = 0,
            StorageUsed = 0,
            Status = "Success"
        };

        // Assert
        Assert.Equal(0, stats.MessagesArchived);
        Assert.Equal(0, stats.StorageUsed);
    }

    [Fact]
    public void GivenArchivalStats_WhenLargeNumberOfMessagesArchived_ThenStoresCorrectValue()
    {
        // Arrange
        var largeCount = 1000000; // 1 million messages
        var largeStorage = 10737418240L; // 10GB

        // Act
        var stats = new ArchivalStats
        {
            RunAt = DateTime.UtcNow,
            MessagesArchived = largeCount,
            StorageUsed = largeStorage,
            Status = "Success"
        };

        // Assert
        Assert.Equal(largeCount, stats.MessagesArchived);
        Assert.Equal(largeStorage, stats.StorageUsed);
    }

    [Fact]
    public void GivenArchivalStats_WhenPartialSuccess_ThenCanSetPartialStatus()
    {
        // Arrange & Act
        var stats = new ArchivalStats
        {
            RunAt = DateTime.UtcNow,
            MessagesArchived = 500,
            StorageUsed = 2097152L, // 2MB
            Status = "Partial",
            ErrorMessage = "Some messages could not be archived"
        };

        // Assert
        Assert.Equal("Partial", stats.Status);
        Assert.Equal(500, stats.MessagesArchived);
        Assert.NotNull(stats.ErrorMessage);
    }
}
