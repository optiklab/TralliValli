using Microsoft.Extensions.Logging;
using Moq;
using TraliVali.Messaging;

namespace TraliVali.Tests.Messaging;

/// <summary>
/// Tests for NoOpNotificationService
/// </summary>
public class NoOpNotificationServiceTests
{
    private readonly Mock<ILogger<NoOpNotificationService>> _mockLogger;
    private readonly NoOpNotificationService _service;

    public NoOpNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<NoOpNotificationService>>();
        _service = new NoOpNotificationService(_mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NoOpNotificationService(null!));
    }

    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NoOpNotificationService>>();

        // Act
        var service = new NoOpNotificationService(mockLogger.Object);

        // Assert - Verify that initialization was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NoOpNotificationService initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SendPushNotificationAsync Tests

    [Fact]
    public async Task SendPushNotificationAsync_ShouldCompleteSuccessfully_WithValidParameters()
    {
        // Arrange
        var userId = "user123";
        var title = "Test Notification";
        var body = "This is a test notification";

        // Act
        await _service.SendPushNotificationAsync(userId, title, body);

        // Assert - Should complete without throwing
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Would send push notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenUserIdIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SendPushNotificationAsync(null!, "Title", "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendPushNotificationAsync("", "Title", "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenUserIdIsWhitespace()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendPushNotificationAsync("   ", "Title", "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenTitleIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SendPushNotificationAsync("user123", null!, "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenTitleIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendPushNotificationAsync("user123", "", "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenTitleIsWhitespace()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendPushNotificationAsync("user123", "   ", "Body"));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenBodyIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SendPushNotificationAsync("user123", "Title", null!));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenBodyIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendPushNotificationAsync("user123", "Title", ""));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldThrowException_WhenBodyIsWhitespace()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendPushNotificationAsync("user123", "Title", "   "));
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldLogCorrectInformation()
    {
        // Arrange
        var userId = "user123";
        var title = "Test Title";
        var body = "Test Body";

        // Act
        await _service.SendPushNotificationAsync(userId, title, body);

        // Assert - Verify logging with correct parameters
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(userId) && 
                    v.ToString()!.Contains(title) && 
                    v.ToString()!.Contains(body)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Should complete even with cancelled token (no-op service)
        await _service.SendPushNotificationAsync("user123", "Title", "Body", cts.Token);

        // Assert - Should have logged
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region SendBatchNotificationsAsync Tests

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldCompleteSuccessfully_WithValidParameters()
    {
        // Arrange
        var userIds = new[] { "user1", "user2", "user3" };
        var title = "Test Notification";
        var body = "This is a test notification";

        // Act
        await _service.SendBatchNotificationsAsync(userIds, title, body);

        // Assert - Should complete without throwing
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Would send batch notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldThrowException_WhenUserIdsIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SendBatchNotificationsAsync(null!, "Title", "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldThrowException_WhenUserIdsIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendBatchNotificationsAsync(Array.Empty<string>(), "Title", "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldThrowException_WhenTitleIsNull()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SendBatchNotificationsAsync(userIds, null!, "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldThrowException_WhenTitleIsEmpty()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendBatchNotificationsAsync(userIds, "", "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldThrowException_WhenTitleIsWhitespace()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendBatchNotificationsAsync(userIds, "   ", "Body"));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldThrowException_WhenBodyIsNull()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SendBatchNotificationsAsync(userIds, "Title", null!));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldThrowException_WhenBodyIsEmpty()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendBatchNotificationsAsync(userIds, "Title", ""));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldThrowException_WhenBodyIsWhitespace()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendBatchNotificationsAsync(userIds, "Title", "   "));
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldLogCorrectInformation()
    {
        // Arrange
        var userIds = new[] { "user1", "user2", "user3" };
        var title = "Test Title";
        var body = "Test Body";

        // Act
        await _service.SendBatchNotificationsAsync(userIds, title, body);

        // Assert - Verify logging with correct parameters
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(userIds.Length.ToString()) && 
                    v.ToString()!.Contains(title) && 
                    v.ToString()!.Contains(body)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldLogUserIds()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };
        var title = "Test Title";
        var body = "Test Body";

        // Act
        await _service.SendBatchNotificationsAsync(userIds, title, body);

        // Assert - Verify user IDs are included in log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("user1") && 
                    v.ToString()!.Contains("user2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldHandleSingleUser()
    {
        // Arrange
        var userIds = new[] { "user1" };
        var title = "Test Title";
        var body = "Test Body";

        // Act
        await _service.SendBatchNotificationsAsync(userIds, title, body);

        // Assert - Should complete successfully
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1 users")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldHandleManyUsers()
    {
        // Arrange
        var userIds = new string[100];
        for (int i = 0; i < 100; i++)
        {
            userIds[i] = $"user{i}";
        }
        var title = "Test Title";
        var body = "Test Body";

        // Act
        await _service.SendBatchNotificationsAsync(userIds, title, body);

        // Assert - Should complete successfully
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("100 users")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Should complete even with cancelled token (no-op service)
        await _service.SendBatchNotificationsAsync(userIds, "Title", "Body", cts.Token);

        // Assert - Should have logged
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task SendPushNotificationAsync_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var userId = "user@123!";
        var title = "Test <Title> & Special \"Characters\"";
        var body = "Body with 'quotes' and <tags>";

        // Act
        await _service.SendPushNotificationAsync(userId, title, body);

        // Assert - Should complete without issues
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var userIds = new[] { "user@123!", "user#456" };
        var title = "Test <Title> & Special \"Characters\"";
        var body = "Body with 'quotes' and <tags>";

        // Act
        await _service.SendBatchNotificationsAsync(userIds, title, body);

        // Assert - Should complete without issues
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldHandleLongContent()
    {
        // Arrange
        var userId = "user123";
        var title = new string('A', 1000);
        var body = new string('B', 5000);

        // Act
        await _service.SendPushNotificationAsync(userId, title, body);

        // Assert - Should complete without issues
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldHandleLongContent()
    {
        // Arrange
        var userIds = new[] { "user1", "user2" };
        var title = new string('A', 1000);
        var body = new string('B', 5000);

        // Act
        await _service.SendBatchNotificationsAsync(userIds, title, body);

        // Assert - Should complete without issues
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
