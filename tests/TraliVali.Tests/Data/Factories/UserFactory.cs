using MongoDB.Bson;
using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Data.Factories;

/// <summary>
/// Factory class for generating User test entities with builder pattern support
/// </summary>
public class UserFactory
{
    private string _id = ObjectId.GenerateNewId().ToString();
    private string _email = $"test.user.{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";
    private string _displayName = "Test User";
    private string _passwordHash = "hashed_password_123";
    private string _publicKey = "public_key_123";
    private List<Device> _devices = new();
    private DateTime _createdAt = DateTime.UtcNow;
    private string? _invitedBy = null;
    private DateTime? _lastLoginAt = null;
    private bool _isActive = true;
    private string _role = "user";

    /// <summary>
    /// Creates a new UserFactory instance
    /// </summary>
    public static UserFactory Create() => new UserFactory();

    /// <summary>
    /// Sets the user ID
    /// </summary>
    public UserFactory WithId(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the user email
    /// </summary>
    public UserFactory WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>
    /// Sets the user display name
    /// </summary>
    public UserFactory WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    /// <summary>
    /// Sets the password hash
    /// </summary>
    public UserFactory WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    /// <summary>
    /// Sets the public key
    /// </summary>
    public UserFactory WithPublicKey(string publicKey)
    {
        _publicKey = publicKey;
        return this;
    }

    /// <summary>
    /// Adds a device to the user
    /// </summary>
    public UserFactory WithDevice(string deviceId, string deviceName, string deviceType = "web")
    {
        _devices.Add(new Device
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            DeviceType = deviceType,
            RegisteredAt = DateTime.UtcNow
        });
        return this;
    }

    /// <summary>
    /// Sets the devices list
    /// </summary>
    public UserFactory WithDevices(List<Device> devices)
    {
        _devices = devices;
        return this;
    }

    /// <summary>
    /// Sets the created at timestamp
    /// </summary>
    public UserFactory WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    /// <summary>
    /// Sets the invited by user ID
    /// </summary>
    public UserFactory WithInvitedBy(string invitedBy)
    {
        _invitedBy = invitedBy;
        return this;
    }

    /// <summary>
    /// Sets the last login timestamp
    /// </summary>
    public UserFactory WithLastLoginAt(DateTime lastLoginAt)
    {
        _lastLoginAt = lastLoginAt;
        return this;
    }

    /// <summary>
    /// Sets the user as inactive
    /// </summary>
    public UserFactory AsInactive()
    {
        _isActive = false;
        return this;
    }

    /// <summary>
    /// Sets the user as active
    /// </summary>
    public UserFactory AsActive()
    {
        _isActive = true;
        return this;
    }

    /// <summary>
    /// Sets the user role
    /// </summary>
    public UserFactory WithRole(string role)
    {
        _role = role;
        return this;
    }

    /// <summary>
    /// Sets the user as admin
    /// </summary>
    public UserFactory AsAdmin()
    {
        _role = "admin";
        return this;
    }

    /// <summary>
    /// Builds and returns the User entity
    /// </summary>
    public User Build()
    {
        return new User
        {
            Id = _id,
            Email = _email,
            DisplayName = _displayName,
            PasswordHash = _passwordHash,
            PublicKey = _publicKey,
            Devices = _devices,
            CreatedAt = _createdAt,
            InvitedBy = _invitedBy,
            LastLoginAt = _lastLoginAt,
            IsActive = _isActive,
            Role = _role
        };
    }

    /// <summary>
    /// Builds and returns a valid user with all required fields
    /// </summary>
    public static User BuildValid()
    {
        return Create().Build();
    }

    /// <summary>
    /// Builds and returns an invalid user (missing required fields)
    /// </summary>
    public static User BuildInvalid()
    {
        return new User
        {
            Email = "",
            DisplayName = "",
            PasswordHash = "",
            PublicKey = ""
        };
    }
}
