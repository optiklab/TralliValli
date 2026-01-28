using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for Invite entity validation
/// </summary>
public class InviteValidationTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenInviteIsValid()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
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
    public void Validate_ShouldReturnError_WhenTokenIsEmpty()
    {
        // Arrange
        var invite = new Invite
        {
            Token = "",
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
    public void Validate_ShouldReturnError_WhenInviterIdIsEmpty()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
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
    public void Validate_ShouldReturnError_WhenExpiresAtIsBeforeCreatedAt()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
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
    public void Validate_ShouldReturnError_WhenInviteIsUsedButUsedByIsNull()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
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
    public void Validate_ShouldReturnError_WhenInviteIsUsedButUsedAtIsNull()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
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
    public void Validate_ShouldReturnNoErrors_WhenInviteIsUsedWithAllRequiredFields()
    {
        // Arrange
        var invite = new Invite
        {
            Token = Guid.NewGuid().ToString(),
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
