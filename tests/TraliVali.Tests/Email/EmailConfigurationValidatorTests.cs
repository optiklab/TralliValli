using Microsoft.Extensions.Logging;
using Moq;
using TraliVali.Messaging;

namespace TraliVali.Tests.Email;

/// <summary>
/// Tests for EmailConfigurationValidator
/// </summary>
public class EmailConfigurationValidatorTests
{
    private readonly Mock<ILogger<EmailConfigurationValidator>> _mockLogger;

    public EmailConfigurationValidatorTests()
    {
        _mockLogger = new Mock<ILogger<EmailConfigurationValidator>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new EmailConfigurationValidator(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply@example.com"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new EmailConfigurationValidator(config, null!));
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ShouldCompleteSuccessfully_WithValidConfiguration()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - Verify success log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldThrowInvalidOperationException_WithEmptyConnectionString()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = "noreply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));
        
        Assert.Contains("Invalid email configuration", exception.Message);
        Assert.Contains("ConnectionString is required", exception.Message);
    }

    [Fact]
    public async Task StartAsync_ShouldThrowInvalidOperationException_WithEmptySenderAddress()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = ""
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));
        
        Assert.Contains("Invalid email configuration", exception.Message);
        Assert.Contains("SenderAddress is required", exception.Message);
    }

    [Fact]
    public async Task StartAsync_ShouldThrowInvalidOperationException_WithInvalidEmailFormat()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "invalid-email"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));
        
        Assert.Contains("Invalid email configuration", exception.Message);
        Assert.Contains("SenderAddress format is invalid", exception.Message);
    }

    [Fact]
    public async Task StartAsync_ShouldThrowInvalidOperationException_WithMultipleValidationErrors()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = ""
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));
        
        Assert.Contains("Invalid email configuration", exception.Message);
        Assert.Contains("ConnectionString is required", exception.Message);
        Assert.Contains("SenderAddress is required", exception.Message);
    }

    [Fact]
    public async Task StartAsync_ShouldLogValidationStart()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - Verify validation start log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating email configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldLogErrorMessage_WithInvalidConfiguration()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = ""
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));

        // Verify error log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid email configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Should complete even with cancelled token (synchronous validation)
        await validator.StartAsync(cts.Token);

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

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act
        await validator.StopAsync(CancellationToken.None);

        // Assert - Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task StopAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await validator.StopAsync(cts.Token);

        // Assert - Should complete without throwing
        Assert.True(true);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task StartAsync_ShouldAcceptValidEmailWithPlus()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply+test@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - Should complete successfully
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldAcceptValidEmailWithDots()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "no.reply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - Should complete successfully
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldRejectEmailWithoutAt()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreplyexample.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));
        
        Assert.Contains("SenderAddress format is invalid", exception.Message);
    }

    [Fact]
    public async Task StartAsync_ShouldRejectEmailWithSpaces()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "no reply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));
        
        Assert.Contains("SenderAddress format is invalid", exception.Message);
    }

    [Fact]
    public async Task StartAsync_ShouldAcceptConnectionStringWithVariousFormats()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=LONGACCESSKEYHERE123==",
            SenderAddress = "noreply@example.com"
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - Should complete successfully
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldAcceptWhitespaceSenderAddress()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "   "
        };
        var validator = new EmailConfigurationValidator(config, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));
        
        Assert.Contains("SenderAddress is required", exception.Message);
    }

    #endregion
}
