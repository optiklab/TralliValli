using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for Device value object following Given-When-Then pattern
/// </summary>
public class DeviceTests
{
    [Fact]
    public void GivenNewDevice_WhenCreated_ThenAllPropertiesCanBeSet()
    {
        // Arrange
        var deviceId = "device-123-abc";
        var deviceName = "iPhone 13 Pro";
        var deviceType = "mobile";
        var registeredAt = DateTime.UtcNow;

        // Act
        var device = new Device
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            DeviceType = deviceType,
            RegisteredAt = registeredAt
        };

        // Assert
        Assert.Equal(deviceId, device.DeviceId);
        Assert.Equal(deviceName, device.DeviceName);
        Assert.Equal(deviceType, device.DeviceType);
        Assert.Equal(registeredAt, device.RegisteredAt);
    }

    [Fact]
    public void GivenDevice_WhenLastActiveAtSet_ThenLastActiveAtIsCorrect()
    {
        // Arrange
        var device = new Device
        {
            DeviceId = "device-456-def",
            DeviceName = "MacBook Pro",
            DeviceType = "web"
        };
        var lastActiveAt = DateTime.UtcNow;

        // Act
        device.LastActiveAt = lastActiveAt;

        // Assert
        Assert.Equal(lastActiveAt, device.LastActiveAt);
    }

    [Fact]
    public void GivenDevice_WhenLastActiveAtNotSet_ThenLastActiveAtIsNull()
    {
        // Arrange & Act
        var device = new Device
        {
            DeviceId = "device-789-ghi",
            DeviceName = "Android Phone",
            DeviceType = "mobile"
        };

        // Assert
        Assert.Null(device.LastActiveAt);
    }

    [Fact]
    public void GivenMultipleDevices_WhenCreated_ThenEachHasUniqueProperties()
    {
        // Arrange & Act
        var device1 = new Device
        {
            DeviceId = "device-1",
            DeviceName = "Device One",
            DeviceType = "mobile"
        };

        var device2 = new Device
        {
            DeviceId = "device-2",
            DeviceName = "Device Two",
            DeviceType = "web"
        };

        // Assert
        Assert.NotEqual(device1.DeviceId, device2.DeviceId);
        Assert.NotEqual(device1.DeviceName, device2.DeviceName);
        Assert.NotEqual(device1.DeviceType, device2.DeviceType);
    }

    [Fact]
    public void GivenDevice_WhenDefaultConstructorUsed_ThenPropertiesAreInitialized()
    {
        // Arrange & Act
        var device = new Device();

        // Assert
        Assert.Equal(string.Empty, device.DeviceId);
        Assert.Equal(string.Empty, device.DeviceName);
        Assert.Equal(string.Empty, device.DeviceType);
    }
}

/// <summary>
/// Tests for Participant value object following Given-When-Then pattern
/// </summary>
public class ParticipantTests
{
    [Fact]
    public void GivenNewParticipant_WhenCreated_ThenAllPropertiesCanBeSet()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var joinedAt = DateTime.UtcNow;
        var role = "admin";

        // Act
        var participant = new Participant
        {
            UserId = userId,
            JoinedAt = joinedAt,
            Role = role
        };

        // Assert
        Assert.Equal(userId, participant.UserId);
        Assert.Equal(joinedAt, participant.JoinedAt);
        Assert.Equal(role, participant.Role);
    }

    [Fact]
    public void GivenParticipant_WhenDefaultRoleUsed_ThenRoleIsMember()
    {
        // Arrange & Act
        var participant = new Participant
        {
            UserId = "507f1f77bcf86cd799439011"
        };

        // Assert
        Assert.Equal("member", participant.Role);
    }

    [Fact]
    public void GivenParticipant_WhenLastReadAtSet_ThenLastReadAtIsCorrect()
    {
        // Arrange
        var participant = new Participant
        {
            UserId = "507f1f77bcf86cd799439011",
            Role = "member"
        };
        var lastReadAt = DateTime.UtcNow;

        // Act
        participant.LastReadAt = lastReadAt;

        // Assert
        Assert.Equal(lastReadAt, participant.LastReadAt);
    }

    [Fact]
    public void GivenParticipant_WhenLastReadAtNotSet_ThenLastReadAtIsNull()
    {
        // Arrange & Act
        var participant = new Participant
        {
            UserId = "507f1f77bcf86cd799439011"
        };

        // Assert
        Assert.Null(participant.LastReadAt);
    }

    [Fact]
    public void GivenParticipant_WhenRoleSetToAdmin_ThenRoleIsAdmin()
    {
        // Arrange
        var participant = new Participant
        {
            UserId = "507f1f77bcf86cd799439011"
        };

        // Act
        participant.Role = "admin";

        // Assert
        Assert.Equal("admin", participant.Role);
    }

    [Fact]
    public void GivenMultipleParticipants_WhenCreated_ThenEachHasUniqueUserId()
    {
        // Arrange & Act
        var participant1 = new Participant
        {
            UserId = "507f1f77bcf86cd799439011",
            Role = "admin"
        };

        var participant2 = new Participant
        {
            UserId = "507f1f77bcf86cd799439022",
            Role = "member"
        };

        // Assert
        Assert.NotEqual(participant1.UserId, participant2.UserId);
    }

    [Fact]
    public void GivenParticipant_WhenDefaultConstructorUsed_ThenPropertiesAreInitialized()
    {
        // Arrange & Act
        var participant = new Participant();

        // Assert
        Assert.Equal(string.Empty, participant.UserId);
        Assert.Equal("member", participant.Role);
    }
}

/// <summary>
/// Tests for MessageReadStatus value object following Given-When-Then pattern
/// </summary>
public class MessageReadStatusTests
{
    [Fact]
    public void GivenNewMessageReadStatus_WhenCreated_ThenAllPropertiesCanBeSet()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var readAt = DateTime.UtcNow;

        // Act
        var readStatus = new MessageReadStatus
        {
            UserId = userId,
            ReadAt = readAt
        };

        // Assert
        Assert.Equal(userId, readStatus.UserId);
        Assert.Equal(readAt, readStatus.ReadAt);
    }

    [Fact]
    public void GivenMultipleMessageReadStatuses_WhenCreated_ThenEachHasUniqueUserId()
    {
        // Arrange & Act
        var readStatus1 = new MessageReadStatus
        {
            UserId = "507f1f77bcf86cd799439011",
            ReadAt = DateTime.UtcNow
        };

        var readStatus2 = new MessageReadStatus
        {
            UserId = "507f1f77bcf86cd799439022",
            ReadAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(readStatus1.UserId, readStatus2.UserId);
    }

    [Fact]
    public void GivenMessageReadStatus_WhenReadAtInPast_ThenReadAtIsCorrect()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-5);

        // Act
        var readStatus = new MessageReadStatus
        {
            UserId = "507f1f77bcf86cd799439011",
            ReadAt = pastDate
        };

        // Assert
        Assert.Equal(pastDate, readStatus.ReadAt);
        Assert.True(readStatus.ReadAt < DateTime.UtcNow);
    }

    [Fact]
    public void GivenMessageReadStatus_WhenDefaultConstructorUsed_ThenPropertiesAreInitialized()
    {
        // Arrange & Act
        var readStatus = new MessageReadStatus();

        // Assert
        Assert.Equal(string.Empty, readStatus.UserId);
    }

    [Fact]
    public void GivenMultipleMessageReadStatuses_WhenCreatedWithDifferentTimestamps_ThenReadAtTimesAreDifferent()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddMinutes(-10);
        var currentDate = DateTime.UtcNow;

        // Act
        var readStatus1 = new MessageReadStatus
        {
            UserId = "507f1f77bcf86cd799439011",
            ReadAt = pastDate
        };

        var readStatus2 = new MessageReadStatus
        {
            UserId = "507f1f77bcf86cd799439022",
            ReadAt = currentDate
        };

        // Assert
        Assert.True(readStatus2.ReadAt > readStatus1.ReadAt);
    }
}
