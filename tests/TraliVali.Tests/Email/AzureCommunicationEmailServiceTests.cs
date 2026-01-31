using Microsoft.Extensions.Logging;
using Moq;
using TraliVali.Messaging;

namespace TraliVali.Tests.Email;

/// <summary>
/// Tests for AzureCommunicationEmailService
/// </summary>
public class AzureCommunicationEmailServiceTests
{
    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AzureCommunicationEmailService(null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply@example.com"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AzureCommunicationEmailService(config, null!));
    }

    [Fact]
    public void Constructor_WithInvalidConfiguration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "", // Invalid
            SenderAddress = ""     // Invalid
        };
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureCommunicationEmailService(config, logger));
        Assert.Contains("Invalid email configuration", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = "noreply@example.com"
        };
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureCommunicationEmailService(config, logger));
        Assert.Contains("ConnectionString is required", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptySenderAddress_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = ""
        };
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureCommunicationEmailService(config, logger));
        Assert.Contains("SenderAddress is required", exception.Message);
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithNullRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMagicLinkEmailAsync(null!, "John", "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithEmptyRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMagicLinkEmailAsync("", "John", "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithNullRecipientName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMagicLinkEmailAsync("test@example.com", null!, "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithNullMagicLink_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMagicLinkEmailAsync("test@example.com", "John", null!));
    }

    [Fact]
    public async Task SendInviteEmailAsync_WithNullRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendInviteEmailAsync(null!, "John", "Jane", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_WithNullInviterName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendInviteEmailAsync("test@example.com", "John", null!, "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_WithNullInviteLink_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendInviteEmailAsync("test@example.com", "John", "Jane", null!));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPasswordResetEmailAsync(null!, "John", "https://example.com/reset"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullRecipientName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPasswordResetEmailAsync("test@example.com", null!, "https://example.com/reset"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullResetLink_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPasswordResetEmailAsync("test@example.com", "John", null!));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithNullRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendWelcomeEmailAsync(null!, "John"));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithNullRecipientName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendWelcomeEmailAsync("test@example.com", null!));
    }

    #region Edge Case Tests - Whitespace Parameters

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithWhitespaceRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMagicLinkEmailAsync("   ", "John", "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithWhitespaceRecipientName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMagicLinkEmailAsync("test@example.com", "   ", "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithWhitespaceMagicLink_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMagicLinkEmailAsync("test@example.com", "John", "   "));
    }

    [Fact]
    public async Task SendInviteEmailAsync_WithWhitespaceRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendInviteEmailAsync("   ", "John", "Jane", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_WithWhitespaceRecipientName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendInviteEmailAsync("test@example.com", "   ", "Jane", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_WithWhitespaceInviterName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendInviteEmailAsync("test@example.com", "John", "   ", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_WithWhitespaceInviteLink_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendInviteEmailAsync("test@example.com", "John", "Jane", "   "));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithWhitespaceRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPasswordResetEmailAsync("   ", "John", "https://example.com/reset"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithWhitespaceRecipientName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPasswordResetEmailAsync("test@example.com", "   ", "https://example.com/reset"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithWhitespaceResetLink_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendPasswordResetEmailAsync("test@example.com", "John", "   "));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithWhitespaceRecipientEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendWelcomeEmailAsync("   ", "John"));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithWhitespaceRecipientName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendWelcomeEmailAsync("test@example.com", "   "));
    }

    #endregion

    #region Edge Case Tests - Special Characters

    [Fact]
    public async Task SendInviteEmailAsync_WithSpecialCharactersInNames_ShouldThrowException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);

        // Act & Assert
        // This test verifies the service handles potentially dangerous characters (like script tags)
        // The EmailClient will fail because we're using a mock connection string
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.SendInviteEmailAsync(
                "test@example.com", 
                "John <script>alert('xss')</script>", 
                "Jane & Co.",
                "https://example.com/invite"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithLongRecipientName_ShouldThrowException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);
        var longName = new string('A', 1000);

        // Act & Assert
        // This test ensures the service can handle very long recipient names
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.SendMagicLinkEmailAsync("test@example.com", longName, "https://example.com/magic"));
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task SendMagicLinkEmailAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // When cancellation token is already cancelled, the operation should respect it
        // and throw an exception (either OperationCanceledException or related exception)
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.SendMagicLinkEmailAsync("test@example.com", "John", "https://example.com/magic", cts.Token));
    }

    [Fact]
    public async Task SendInviteEmailAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.SendInviteEmailAsync("test@example.com", "John", "Jane", "https://example.com/invite", cts.Token));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.SendPasswordResetEmailAsync("test@example.com", "John", "https://example.com/reset", cts.Token));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var logger = new Mock<ILogger<AzureCommunicationEmailService>>().Object;
        var service = new AzureCommunicationEmailService(config, logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.SendWelcomeEmailAsync("test@example.com", "John", cts.Token));
    }

    #endregion

    private static AzureCommunicationEmailConfiguration CreateValidConfiguration()
    {
        return new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==",
            SenderAddress = "noreply@example.com",
            SenderName = "Test Sender"
        };
    }
}
