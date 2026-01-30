using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for Invite entity validation following Given-When-Then pattern
/// </summary>
public class InviteValidationTests
{
    [Fact]
    public void GivenValidInvite_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "invitee@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void GivenEmptyToken_WhenValidating_ThenReturnsTokenRequiredError()
    {
        // Arrange
        var invite = new Invite
        {
            Token = "",
            Email = "invitee@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Contains("Token is required", errors);
    }

    [Fact]
    public void GivenEmptyEmail_WhenValidating_ThenReturnsEmailRequiredError()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "",
            InviterId = "507f1f77bcf86cd799439011",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Contains("Email is required", errors);
    }

    [Fact]
    public void GivenInvalidEmail_WhenValidating_ThenReturnsEmailFormatInvalidError()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "invalid-email",
            InviterId = "507f1f77bcf86cd799439011",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Contains("Email format is invalid", errors);
    }

    [Fact]
    public void GivenEmptyInviterId_WhenValidating_ThenReturnsInviterIdRequiredError()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "invitee@example.com",
            InviterId = "",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Contains("InviterId is required", errors);
    }

    [Fact]
    public void GivenExpiresAtBeforeCreatedAt_WhenValidating_ThenReturnsExpiresAtAfterCreatedAtError()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "invitee@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(-7)
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Contains("ExpiresAt must be after CreatedAt", errors);
    }

    [Fact]
    public void GivenInviteIsUsedButUsedByIsNull_WhenValidating_ThenReturnsUsedByRequiredError()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "invitee@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = true,
            UsedBy = null,
            UsedAt = DateTime.UtcNow
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Contains("UsedBy is required when invite is used", errors);
    }

    [Fact]
    public void GivenInviteIsUsedButUsedAtIsNull_WhenValidating_ThenReturnsUsedAtRequiredError()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "invitee@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = true,
            UsedBy = "507f1f77bcf86cd799439012",
            UsedAt = null
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Contains("UsedAt is required when invite is used", errors);
    }

    [Fact]
    public void GivenUsedInviteWithAllRequiredFields_WhenValidating_ThenReturnsNoErrors()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
            Email = "invitee@example.com",
            InviterId = "507f1f77bcf86cd799439011",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = true,
            UsedBy = "507f1f77bcf86cd799439012",
            UsedAt = DateTime.UtcNow
        };

        // Act
        var errors = invite.Validate();

        // Assert
        Assert.Empty(errors);
    }
}
