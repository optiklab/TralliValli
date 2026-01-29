using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using TraliVali.Api.Hubs;
using TraliVali.Auth;

namespace TraliVali.Tests.Hubs;

/// <summary>
/// Tests for ChatHub
/// </summary>
public class ChatHubTests
{
    private readonly Mock<ILogger<ChatHub>> _mockLogger;
    private readonly Mock<IPresenceService> _mockPresenceService;
    private readonly Mock<IHubCallerClients<IChatClient>> _mockClients;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IChatClient> _mockClient;
    private readonly ChatHub _chatHub;

    public ChatHubTests()
    {
        _mockLogger = new Mock<ILogger<ChatHub>>();
        _mockPresenceService = new Mock<IPresenceService>();
        _mockClients = new Mock<IHubCallerClients<IChatClient>>();
        _mockGroups = new Mock<IGroupManager>();
        _mockContext = new Mock<HubCallerContext>();
        _mockClient = new Mock<IChatClient>();

        _chatHub = new ChatHub(_mockLogger.Object, _mockPresenceService.Object)
        {
            Clients = _mockClients.Object,
            Groups = _mockGroups.Object,
            Context = _mockContext.Object
        };

        // Setup default user claims
        var claims = new[]
        {
            new Claim("userId", "user123"),
            new Claim("displayName", "Test User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _mockContext.Setup(c => c.User).Returns(claimsPrincipal);
        _mockContext.Setup(c => c.ConnectionId).Returns("connection123");
    }

    [Fact]
    public async Task SendMessage_ShouldCallReceiveMessage_OnGroupClients()
    {
        // Arrange
        var conversationId = "conv123";
        var messageId = "msg123";
        var content = "Hello, World!";

        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClient.Object);

        // Act
        await _chatHub.SendMessage(conversationId, messageId, content);

        // Assert
        _mockClients.Verify(c => c.Group(conversationId), Times.Once);
        _mockClient.Verify(c => c.ReceiveMessage(
            conversationId,
            messageId,
            "user123",
            "Test User",
            content,
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var conversationId = "";
        var messageId = "msg123";
        var content = "Hello, World!";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.SendMessage(conversationId, messageId, content));
    }

    [Fact]
    public async Task SendMessage_ShouldThrowException_WhenMessageIdIsEmpty()
    {
        // Arrange
        var conversationId = "conv123";
        var messageId = "";
        var content = "Hello, World!";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.SendMessage(conversationId, messageId, content));
    }

    [Fact]
    public async Task SendMessage_ShouldThrowException_WhenContentIsEmpty()
    {
        // Arrange
        var conversationId = "conv123";
        var messageId = "msg123";
        var content = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.SendMessage(conversationId, messageId, content));
    }

    [Fact]
    public async Task JoinConversation_ShouldAddToGroup_AndNotifyOthers()
    {
        // Arrange
        var conversationId = "conv123";

        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClient.Object);
        _mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.JoinConversation(conversationId);

        // Assert
        _mockGroups.Verify(g => g.AddToGroupAsync("connection123", conversationId, default), Times.Once);
        _mockClient.Verify(c => c.UserJoined(conversationId, "user123", "Test User"), Times.Once);
    }

    [Fact]
    public async Task JoinConversation_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var conversationId = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.JoinConversation(conversationId));
    }

    [Fact]
    public async Task LeaveConversation_ShouldRemoveFromGroup_AndNotifyOthers()
    {
        // Arrange
        var conversationId = "conv123";

        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClient.Object);
        _mockGroups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.LeaveConversation(conversationId);

        // Assert
        _mockGroups.Verify(g => g.RemoveFromGroupAsync("connection123", conversationId, default), Times.Once);
        _mockClient.Verify(c => c.UserLeft(conversationId, "user123", "Test User"), Times.Once);
    }

    [Fact]
    public async Task LeaveConversation_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var conversationId = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.LeaveConversation(conversationId));
    }

    [Fact]
    public async Task StartTyping_ShouldNotifyOthersInGroup()
    {
        // Arrange
        var conversationId = "conv123";

        _mockClients.Setup(c => c.OthersInGroup(It.IsAny<string>())).Returns(_mockClient.Object);

        // Act
        await _chatHub.StartTyping(conversationId);

        // Assert
        _mockClients.Verify(c => c.OthersInGroup(conversationId), Times.Once);
        _mockClient.Verify(c => c.TypingIndicator(conversationId, "user123", "Test User", true), Times.Once);
    }

    [Fact]
    public async Task StartTyping_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var conversationId = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.StartTyping(conversationId));
    }

    [Fact]
    public async Task StopTyping_ShouldNotifyOthersInGroup()
    {
        // Arrange
        var conversationId = "conv123";

        _mockClients.Setup(c => c.OthersInGroup(It.IsAny<string>())).Returns(_mockClient.Object);

        // Act
        await _chatHub.StopTyping(conversationId);

        // Assert
        _mockClients.Verify(c => c.OthersInGroup(conversationId), Times.Once);
        _mockClient.Verify(c => c.TypingIndicator(conversationId, "user123", "Test User", false), Times.Once);
    }

    [Fact]
    public async Task StopTyping_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var conversationId = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.StopTyping(conversationId));
    }

    [Fact]
    public async Task MarkAsRead_ShouldNotifyGroupClients()
    {
        // Arrange
        var conversationId = "conv123";
        var messageId = "msg123";

        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClient.Object);

        // Act
        await _chatHub.MarkAsRead(conversationId, messageId);

        // Assert
        _mockClients.Verify(c => c.Group(conversationId), Times.Once);
        _mockClient.Verify(c => c.MessageRead(conversationId, messageId, "user123"), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_ShouldThrowException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var conversationId = "";
        var messageId = "msg123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.MarkAsRead(conversationId, messageId));
    }

    [Fact]
    public async Task MarkAsRead_ShouldThrowException_WhenMessageIdIsEmpty()
    {
        // Arrange
        var conversationId = "conv123";
        var messageId = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _chatHub.MarkAsRead(conversationId, messageId));
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldNotifyAllClients_AboutUserOnline()
    {
        // Arrange
        _mockClients.Setup(c => c.All).Returns(_mockClient.Object);
        _mockPresenceService.Setup(s => s.SetOnlineAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.OnConnectedAsync();

        // Assert
        _mockPresenceService.Verify(s => s.SetOnlineAsync("user123", "connection123"), Times.Once);
        _mockClients.Verify(c => c.All, Times.Once);
        _mockClient.Verify(c => c.PresenceUpdate("user123", true, null), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldNotifyAllClients_AboutUserOffline()
    {
        // Arrange
        var lastSeenTime = DateTime.UtcNow;
        _mockClients.Setup(c => c.All).Returns(_mockClient.Object);
        _mockPresenceService.Setup(s => s.SetOfflineAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockPresenceService.Setup(s => s.GetLastSeenAsync(It.IsAny<string>()))
            .ReturnsAsync(lastSeenTime);

        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert
        _mockPresenceService.Verify(s => s.SetOfflineAsync("user123", "connection123"), Times.Once);
        _mockPresenceService.Verify(s => s.GetLastSeenAsync("user123"), Times.Once);
        _mockClients.Verify(c => c.All, Times.Once);
        _mockClient.Verify(c => c.PresenceUpdate("user123", false, lastSeenTime), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange
        ILogger<ChatHub>? logger = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ChatHub(logger!, _mockPresenceService.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenPresenceServiceIsNull()
    {
        // Arrange
        IPresenceService? presenceService = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ChatHub(_mockLogger.Object, presenceService!));
    }
}
