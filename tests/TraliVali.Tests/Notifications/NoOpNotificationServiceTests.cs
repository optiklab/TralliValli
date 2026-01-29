using Microsoft.Extensions.Logging;
using Moq;
using TraliVali.Messaging;

namespace TraliVali.Tests.Notifications;

/// <summary>
/// Tests for NoOpNotificationService
/// </summary>
public class NoOpNotificationServiceTests
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NoOpNotificationService(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldLogInitialization()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<NoOpNotificationService>>();

        // Act
        var service = new NoOpNotificationService(loggerMock.Object);

        // Assert
        Assert.NotNull(service);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NoOpNotificationService initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithNullUserId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPushNotificationAsync(null!, "Title", "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithEmptyUserId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPushNotificationAsync("", "Title", "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithNullTitle_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPushNotificationAsync("user123", null!, "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithEmptyTitle_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPushNotificationAsync("user123", "", "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithNullBody_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPushNotificationAsync("user123", "Title", null!));
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithEmptyBody_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPushNotificationAsync("user123", "Title", ""));
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithValidParameters_ShouldLogNotification()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<NoOpNotificationService>>();
        var service = new NoOpNotificationService(loggerMock.Object);
        var userId = "user123";
        var title = "Test Notification";
        var body = "This is a test notification";

        // Act
        await service.SendPushNotificationAsync(userId, title, body);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Would send push notification") &&
                    v.ToString()!.Contains(userId) &&
                    v.ToString()!.Contains(title) &&
                    v.ToString()!.Contains(body)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act
        var task = service.SendPushNotificationAsync("user123", "Title", "Body");

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithNullUserIds_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendBatchNotificationsAsync(null!, "Title", "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithEmptyUserIds_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendBatchNotificationsAsync(Array.Empty<string>(), "Title", "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithNullTitle_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendBatchNotificationsAsync(new[] { "user1", "user2" }, null!, "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithEmptyTitle_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendBatchNotificationsAsync(new[] { "user1", "user2" }, "", "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithNullBody_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendBatchNotificationsAsync(new[] { "user1", "user2" }, "Title", null!));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithEmptyBody_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendBatchNotificationsAsync(new[] { "user1", "user2" }, "Title", ""));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithValidParameters_ShouldLogNotification()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<NoOpNotificationService>>();
        var service = new NoOpNotificationService(loggerMock.Object);
        var userIds = new[] { "user1", "user2", "user3" };
        var title = "Batch Notification";
        var body = "This is a batch notification";

        // Act
        await service.SendBatchNotificationsAsync(userIds, title, body);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Would send batch notification") &&
                    v.ToString()!.Contains(userIds.Length.ToString()) &&
                    v.ToString()!.Contains(title) &&
                    v.ToString()!.Contains(body)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithSingleUser_ShouldLogCorrectly()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<NoOpNotificationService>>();
        var service = new NoOpNotificationService(loggerMock.Object);
        var userIds = new[] { "user1" };

        // Act
        await service.SendBatchNotificationsAsync(userIds, "Title", "Body");

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("1 users")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpNotificationService>>().Object;
        var service = new NoOpNotificationService(logger);

        // Act
        var task = service.SendBatchNotificationsAsync(new[] { "user1", "user2" }, "Title", "Body");

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }
}
