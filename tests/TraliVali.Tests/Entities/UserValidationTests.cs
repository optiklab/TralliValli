using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for User entity validation
/// </summary>
public class UserValidationTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenUserIsValid()
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
    public void Validate_ShouldReturnError_WhenEmailIsEmpty()
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
    public void Validate_ShouldReturnError_WhenEmailIsInvalid()
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
    public void Validate_ShouldReturnError_WhenDisplayNameIsEmpty()
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
    public void Validate_ShouldReturnError_WhenDisplayNameIsTooLong()
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
    public void Validate_ShouldReturnError_WhenPasswordHashIsEmpty()
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
    public void Validate_ShouldReturnError_WhenPublicKeyIsEmpty()
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
    public void Validate_ShouldReturnMultipleErrors_WhenMultipleFieldsAreInvalid()
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
