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
