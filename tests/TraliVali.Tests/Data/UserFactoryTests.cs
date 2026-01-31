using MongoDB.Bson;
using TraliVali.Domain.Entities;
using TraliVali.Tests.Data.Factories;

namespace TraliVali.Tests.Data;

/// <summary>
/// Tests for UserFactory to ensure it generates valid test data
/// </summary>
public class UserFactoryTests
{
    [Fact]
    public void BuildValid_ShouldCreateValidUser()
    {
        // Act
        var user = UserFactory.BuildValid();

        // Assert
        Assert.NotNull(user);
        Assert.NotEmpty(user.Email);
        Assert.NotEmpty(user.DisplayName);
        Assert.NotEmpty(user.PasswordHash);
        Assert.NotEmpty(user.PublicKey);
        Assert.True(user.IsActive);
        Assert.Equal("user", user.Role);
        
        var errors = user.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void BuildInvalid_ShouldCreateInvalidUser()
    {
        // Act
        var user = UserFactory.BuildInvalid();

        // Assert
        var errors = user.Validate();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void WithEmail_ShouldSetEmail()
    {
        // Arrange
        var expectedEmail = "custom@example.com";

        // Act
        var user = UserFactory.Create()
            .WithEmail(expectedEmail)
            .Build();

        // Assert
        Assert.Equal(expectedEmail, user.Email);
    }

    [Fact]
    public void WithDisplayName_ShouldSetDisplayName()
    {
        // Arrange
        var expectedName = "Custom User";

        // Act
        var user = UserFactory.Create()
            .WithDisplayName(expectedName)
            .Build();

        // Assert
        Assert.Equal(expectedName, user.DisplayName);
    }

    [Fact]
    public void AsAdmin_ShouldSetRoleToAdmin()
    {
        // Act
        var user = UserFactory.Create()
            .AsAdmin()
            .Build();

        // Assert
        Assert.Equal("admin", user.Role);
    }

    [Fact]
    public void AsInactive_ShouldSetIsActiveFalse()
    {
        // Act
        var user = UserFactory.Create()
            .AsInactive()
            .Build();

        // Assert
        Assert.False(user.IsActive);
    }

    [Fact]
    public void WithDevice_ShouldAddDevice()
    {
        // Act
        var user = UserFactory.Create()
            .WithDevice("device-1", "iPhone 15", "mobile")
            .Build();

        // Assert
        Assert.Single(user.Devices);
        Assert.Equal("device-1", user.Devices[0].DeviceId);
        Assert.Equal("iPhone 15", user.Devices[0].DeviceName);
        Assert.Equal("mobile", user.Devices[0].DeviceType);
    }

    [Fact]
    public void WithInvitedBy_ShouldSetInvitedBy()
    {
        // Arrange
        var inviterId = ObjectId.GenerateNewId().ToString();

        // Act
        var user = UserFactory.Create()
            .WithInvitedBy(inviterId)
            .Build();

        // Assert
        Assert.Equal(inviterId, user.InvitedBy);
    }

    [Fact]
    public void WithLastLoginAt_ShouldSetLastLoginAt()
    {
        // Arrange
        var lastLogin = DateTime.UtcNow.AddDays(-1);

        // Act
        var user = UserFactory.Create()
            .WithLastLoginAt(lastLogin)
            .Build();

        // Assert
        Assert.Equal(lastLogin, user.LastLoginAt);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueEmails()
    {
        // Act
        var user1 = UserFactory.BuildValid();
        var user2 = UserFactory.BuildValid();

        // Assert
        Assert.NotEqual(user1.Email, user2.Email);
    }

    [Fact]
    public void WithCreatedAt_ShouldSetCreatedAt()
    {
        // Arrange
        var createdDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var user = UserFactory.Create()
            .WithCreatedAt(createdDate)
            .Build();

        // Assert
        Assert.Equal(createdDate, user.CreatedAt);
    }

    [Fact]
    public void BuilderPattern_ShouldAllowChaining()
    {
        // Act
        var user = UserFactory.Create()
            .WithEmail("chain@example.com")
            .WithDisplayName("Chained User")
            .AsAdmin()
            .WithDevice("device-1", "MacBook Pro", "web")
            .AsActive()
            .Build();

        // Assert
        Assert.Equal("chain@example.com", user.Email);
        Assert.Equal("Chained User", user.DisplayName);
        Assert.Equal("admin", user.Role);
        Assert.Single(user.Devices);
        Assert.True(user.IsActive);
    }
}
