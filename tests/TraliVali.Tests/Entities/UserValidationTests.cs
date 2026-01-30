using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for User entity validation following Given-When-Then pattern
/// </summary>
public class UserValidationTests
{
    [Fact]
    public void GivenValidUser_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hashed_password_123",
            PublicKey = "public_key_123"
        };

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEmptyEmail_WhenValidating_ThenReturnsEmailRequiredError()
    {
        // Arrange
        var user = new User
        {
            Email = "",
            DisplayName = "Test User",
            PasswordHash = "hashed_password_123",
            PublicKey = "public_key_123"
        };

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("Email is required", errors);
    }

    [Fact]
    public void GivenInvalidEmail_WhenValidating_ThenReturnsEmailFormatInvalidError()
    {
        // Arrange
        var user = new User
        {
            Email = "invalid-email",
            DisplayName = "Test User",
            PasswordHash = "hashed_password_123",
            PublicKey = "public_key_123"
        };

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("Email format is invalid", errors);
    }

    [Fact]
    public void GivenEmptyDisplayName_WhenValidating_ThenReturnsDisplayNameRequiredError()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "",
            PasswordHash = "hashed_password_123",
            PublicKey = "public_key_123"
        };

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("DisplayName is required", errors);
    }

    [Fact]
    public void GivenDisplayNameTooLong_WhenValidating_ThenReturnsDisplayNameTooLongError()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = new string('A', 101),
            PasswordHash = "hashed_password_123",
            PublicKey = "public_key_123"
        };

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("DisplayName cannot exceed 100 characters", errors);
    }

    [Fact]
    public void GivenEmptyPasswordHash_WhenValidating_ThenReturnsPasswordHashRequiredError()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "",
            PublicKey = "public_key_123"
        };

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("PasswordHash is required", errors);
    }

    [Fact]
    public void GivenEmptyPublicKey_WhenValidating_ThenReturnsPublicKeyRequiredError()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hashed_password_123",
            PublicKey = ""
        };

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("PublicKey is required", errors);
    }

    [Fact]
    public void GivenMultipleInvalidFields_WhenValidating_ThenReturnsMultipleErrors()
    {
        // Arrange
        var user = new User
        {
            Email = "",
            DisplayName = "",
            PasswordHash = "",
            PublicKey = ""
        };

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Equal(4, errors.Count);
        Assert.Contains("Email is required", errors);
        Assert.Contains("DisplayName is required", errors);
        Assert.Contains("PasswordHash is required", errors);
        Assert.Contains("PublicKey is required", errors);
    }
}
