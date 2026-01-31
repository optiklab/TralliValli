using TraliVali.Domain.Entities;
using TraliVali.Tests.Data.Factories;

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
        var user = UserFactory.BuildValid();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEmptyEmail_WhenValidating_ThenReturnsEmailRequiredError()
    {
        // Arrange
        var user = UserFactory.Create()
            .WithEmail("")
            .Build();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("Email is required", errors);
    }

    [Fact]
    public void GivenInvalidEmail_WhenValidating_ThenReturnsEmailFormatInvalidError()
    {
        // Arrange
        var user = UserFactory.Create()
            .WithEmail("invalid-email")
            .Build();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("Email format is invalid", errors);
    }

    [Fact]
    public void GivenEmptyDisplayName_WhenValidating_ThenReturnsDisplayNameRequiredError()
    {
        // Arrange
        var user = UserFactory.Create()
            .WithDisplayName("")
            .Build();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("DisplayName is required", errors);
    }

    [Fact]
    public void GivenDisplayNameTooLong_WhenValidating_ThenReturnsDisplayNameTooLongError()
    {
        // Arrange
        var user = UserFactory.Create()
            .WithDisplayName(new string('A', 101))
            .Build();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("DisplayName cannot exceed 100 characters", errors);
    }

    [Fact]
    public void GivenEmptyPasswordHash_WhenValidating_ThenReturnsPasswordHashRequiredError()
    {
        // Arrange
        var user = UserFactory.Create()
            .WithPasswordHash("")
            .Build();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("PasswordHash is required", errors);
    }

    [Fact]
    public void GivenEmptyPublicKey_WhenValidating_ThenReturnsPublicKeyRequiredError()
    {
        // Arrange
        var user = UserFactory.Create()
            .WithPublicKey("")
            .Build();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("PublicKey is required", errors);
    }

    [Fact]
    public void GivenMultipleInvalidFields_WhenValidating_ThenReturnsMultipleErrors()
    {
        // Arrange
        var user = UserFactory.BuildInvalid();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Equal(4, errors.Count);
        Assert.Contains("Email is required", errors);
        Assert.Contains("DisplayName is required", errors);
        Assert.Contains("PasswordHash is required", errors);
        Assert.Contains("PublicKey is required", errors);
    }

    [Fact]
    public void GivenEmailWithSpaces_WhenValidating_ThenReturnsEmailFormatInvalidError()
    {
        // Arrange
        var user = UserFactory.Create()
            .WithEmail("test @example.com")
            .Build();

        // Act
        var errors = user.Validate();

        // Assert
        Assert.Contains("Email format is invalid", errors);
    }

    [Fact]
    public void GivenDefaultUserRole_WhenCreated_ThenRoleIsUser()
    {
        // Arrange & Act
        var user = UserFactory.BuildValid();

        // Assert
        Assert.Equal("user", user.Role);
    }

    [Fact]
    public void GivenDefaultIsActive_WhenCreated_ThenIsActiveIsTrue()
    {
        // Arrange & Act
        var user = UserFactory.BuildValid();

        // Assert
        Assert.True(user.IsActive);
    }
}
