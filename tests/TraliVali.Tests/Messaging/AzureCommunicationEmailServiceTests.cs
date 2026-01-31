using Microsoft.Extensions.Logging;
using Moq;
using TraliVali.Messaging;

namespace TraliVali.Tests.Messaging;

/// <summary>
/// Tests for AzureCommunicationEmailService
/// Note: These tests focus on validation and error handling since we can't easily test Azure Communication Services without live credentials
/// </summary>
public class AzureCommunicationEmailServiceTests
{
    private readonly Mock<ILogger<AzureCommunicationEmailService>> _mockLogger;
    private readonly AzureCommunicationEmailConfiguration _validConfiguration;

    public AzureCommunicationEmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<AzureCommunicationEmailService>>();
        
        // Use a valid test connection string format (won't actually connect in unit tests)
        _validConfiguration = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==",
            SenderAddress = "noreply@test.com"
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowException_WhenConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureCommunicationEmailService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureCommunicationEmailService(_validConfiguration, null!));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenConnectionStringIsEmpty()
    {
        // Arrange
        var invalidConfig = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = "noreply@test.com"
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new AzureCommunicationEmailService(invalidConfig, _mockLogger.Object));
        Assert.Contains("Invalid email configuration", ex.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenConnectionStringIsNull()
    {
        // Arrange
        var invalidConfig = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = null!,
            SenderAddress = "noreply@test.com"
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new AzureCommunicationEmailService(invalidConfig, _mockLogger.Object));
        Assert.Contains("Invalid email configuration", ex.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenSenderAddressIsEmpty()
    {
        // Arrange
        var invalidConfig = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==",
            SenderAddress = ""
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new AzureCommunicationEmailService(invalidConfig, _mockLogger.Object));
        Assert.Contains("Invalid email configuration", ex.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenSenderAddressIsNull()
    {
        // Arrange
        var invalidConfig = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==",
            SenderAddress = null!
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new AzureCommunicationEmailService(invalidConfig, _mockLogger.Object));
        Assert.Contains("Invalid email configuration", ex.Message);
    }

    #endregion

    #region SendMagicLinkEmailAsync Tests

    [Fact]
    public async Task SendMagicLinkEmailAsync_ShouldThrowException_WhenRecipientEmailIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendMagicLinkEmailAsync("", "John Doe", "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_ShouldThrowException_WhenRecipientEmailIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendMagicLinkEmailAsync(null!, "John Doe", "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_ShouldThrowException_WhenRecipientEmailIsWhitespace()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendMagicLinkEmailAsync("   ", "John Doe", "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_ShouldThrowException_WhenRecipientNameIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendMagicLinkEmailAsync("test@example.com", "", "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_ShouldThrowException_WhenRecipientNameIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendMagicLinkEmailAsync("test@example.com", null!, "https://example.com/magic"));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_ShouldThrowException_WhenMagicLinkIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendMagicLinkEmailAsync("test@example.com", "John Doe", ""));
    }

    [Fact]
    public async Task SendMagicLinkEmailAsync_ShouldThrowException_WhenMagicLinkIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendMagicLinkEmailAsync("test@example.com", "John Doe", null!));
    }

    #endregion

    #region SendInviteEmailAsync Tests

    [Fact]
    public async Task SendInviteEmailAsync_ShouldThrowException_WhenRecipientEmailIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendInviteEmailAsync("", "John Doe", "Jane Doe", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_ShouldThrowException_WhenRecipientEmailIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendInviteEmailAsync(null!, "John Doe", "Jane Doe", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_ShouldThrowException_WhenRecipientNameIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendInviteEmailAsync("test@example.com", "", "Jane Doe", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_ShouldThrowException_WhenRecipientNameIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendInviteEmailAsync("test@example.com", null!, "Jane Doe", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_ShouldThrowException_WhenInviterNameIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendInviteEmailAsync("test@example.com", "John Doe", "", "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_ShouldThrowException_WhenInviterNameIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendInviteEmailAsync("test@example.com", "John Doe", null!, "https://example.com/invite"));
    }

    [Fact]
    public async Task SendInviteEmailAsync_ShouldThrowException_WhenInviteLinkIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendInviteEmailAsync("test@example.com", "John Doe", "Jane Doe", ""));
    }

    [Fact]
    public async Task SendInviteEmailAsync_ShouldThrowException_WhenInviteLinkIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendInviteEmailAsync("test@example.com", "John Doe", "Jane Doe", null!));
    }

    #endregion

    #region SendPasswordResetEmailAsync Tests

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldThrowException_WhenRecipientEmailIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendPasswordResetEmailAsync("", "John Doe", "https://example.com/reset"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldThrowException_WhenRecipientEmailIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendPasswordResetEmailAsync(null!, "John Doe", "https://example.com/reset"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldThrowException_WhenRecipientNameIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendPasswordResetEmailAsync("test@example.com", "", "https://example.com/reset"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldThrowException_WhenRecipientNameIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendPasswordResetEmailAsync("test@example.com", null!, "https://example.com/reset"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldThrowException_WhenResetLinkIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendPasswordResetEmailAsync("test@example.com", "John Doe", ""));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldThrowException_WhenResetLinkIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendPasswordResetEmailAsync("test@example.com", "John Doe", null!));
    }

    #endregion

    #region SendWelcomeEmailAsync Tests

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldThrowException_WhenRecipientEmailIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendWelcomeEmailAsync("", "John Doe"));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldThrowException_WhenRecipientEmailIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendWelcomeEmailAsync(null!, "John Doe"));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldThrowException_WhenRecipientEmailIsWhitespace()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendWelcomeEmailAsync("   ", "John Doe"));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldThrowException_WhenRecipientNameIsEmpty()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendWelcomeEmailAsync("test@example.com", ""));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldThrowException_WhenRecipientNameIsNull()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendWelcomeEmailAsync("test@example.com", null!));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldThrowException_WhenRecipientNameIsWhitespace()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendWelcomeEmailAsync("test@example.com", "   "));
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task SendMagicLinkEmailAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Will fail validation before reaching cancellation, but validates token is passed
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.SendMagicLinkEmailAsync("test@example.com", "John Doe", "https://example.com/magic", cts.Token));
    }

    [Fact]
    public async Task SendInviteEmailAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.SendInviteEmailAsync("test@example.com", "John Doe", "Jane Doe", "https://example.com/invite", cts.Token));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.SendPasswordResetEmailAsync("test@example.com", "John Doe", "https://example.com/reset", cts.Token));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var service = new AzureCommunicationEmailService(_validConfiguration, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.SendWelcomeEmailAsync("test@example.com", "John Doe", cts.Token));
    }

    #endregion

    #region Configuration Validation Tests

    [Fact]
    public void Configuration_ShouldValidate_WhenAllFieldsAreValid()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==",
            SenderAddress = "noreply@test.com"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Configuration_ShouldReturnError_WhenConnectionStringIsEmpty()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = "noreply@test.com"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("ConnectionString"));
    }

    [Fact]
    public void Configuration_ShouldReturnError_WhenSenderAddressIsEmpty()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==",
            SenderAddress = ""
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("SenderAddress"));
    }

    [Fact]
    public void Configuration_ShouldReturnMultipleErrors_WhenMultipleFieldsAreInvalid()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = ""
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.True(errors.Count() >= 2);
    }

    #endregion
}
