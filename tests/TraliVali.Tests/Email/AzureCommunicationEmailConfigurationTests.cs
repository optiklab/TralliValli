using Microsoft.Extensions.Logging;
using Moq;
using TraliVali.Messaging;

namespace TraliVali.Tests.Email;

/// <summary>
/// Tests for AzureCommunicationEmailConfiguration
/// </summary>
public class AzureCommunicationEmailConfigurationTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnNoErrors()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "noreply@example.com",
            SenderName = "Test Sender"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithEmptyConnectionString_ShouldReturnError()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = "noreply@example.com"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("ConnectionString is required", errors);
    }

    [Fact]
    public void Validate_WithEmptySenderAddress_ShouldReturnError()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = ""
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("SenderAddress is required", errors);
    }

    [Fact]
    public void Validate_WithInvalidSenderAddress_ShouldReturnError()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
            SenderAddress = "invalid-email"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("SenderAddress format is invalid", errors);
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var config = new AzureCommunicationEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = "invalid-email"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains("ConnectionString is required", errors);
        Assert.Contains("SenderAddress format is invalid", errors);
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("AzureCommunicationEmail", AzureCommunicationEmailConfiguration.SectionName);
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var config = new AzureCommunicationEmailConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.ConnectionString);
        Assert.Equal(string.Empty, config.SenderAddress);
        Assert.Equal("TraliVali", config.SenderName);
    }
}
