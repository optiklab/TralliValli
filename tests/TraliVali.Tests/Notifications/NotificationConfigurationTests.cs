using TraliVali.Messaging;

namespace TraliVali.Tests.Notifications;

/// <summary>
/// Tests for NotificationConfiguration
/// </summary>
public class NotificationConfigurationTests
{
    [Fact]
    public void Constructor_ShouldHaveDefaultProviderNone()
    {
        // Arrange & Act
        var config = new NotificationConfiguration();

        // Assert
        Assert.Equal("None", config.Provider);
    }

    [Fact]
    public void Validate_WithValidNoneProvider_ShouldReturnNoErrors()
    {
        // Arrange
        var config = new NotificationConfiguration
        {
            Provider = "None"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithValidNoneProviderLowerCase_ShouldReturnNoErrors()
    {
        // Arrange
        var config = new NotificationConfiguration
        {
            Provider = "none"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithEmptyProvider_ShouldReturnError()
    {
        // Arrange
        var config = new NotificationConfiguration
        {
            Provider = ""
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("Provider is required", errors[0]);
    }

    [Fact]
    public void Validate_WithNullProvider_ShouldReturnError()
    {
        // Arrange
        var config = new NotificationConfiguration
        {
            Provider = null!
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("Provider is required", errors[0]);
    }

    [Fact]
    public void Validate_WithInvalidProvider_ShouldReturnError()
    {
        // Arrange
        var config = new NotificationConfiguration
        {
            Provider = "InvalidProvider"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("InvalidProvider", errors[0]);
        Assert.Contains("not supported", errors[0]);
    }

    [Fact]
    public void SectionName_ShouldBeNotifications()
    {
        // Assert
        Assert.Equal("Notifications", NotificationConfiguration.SectionName);
    }
}
