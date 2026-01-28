using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraliVali.Domain.Entities;

/// <summary>
/// Represents a user in the messaging platform
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name
    /// </summary>
    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password for authentication
    /// </summary>
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's public encryption key
    /// </summary>
    [BsonElement("publicKey")]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of registered devices for this user
    /// </summary>
    [BsonElement("devices")]
    public List<Device> Devices { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when the user was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who invited this user
    /// </summary>
    [BsonElement("invitedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? InvitedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user last logged in
    /// </summary>
    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user account is active
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Validates the user entity
    /// </summary>
    /// <returns>A list of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        else if (!IsValidEmail(Email))
            errors.Add("Email format is invalid");

        if (string.IsNullOrWhiteSpace(DisplayName))
            errors.Add("DisplayName is required");
        else if (DisplayName.Length > 100)
            errors.Add("DisplayName cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(PasswordHash))
            errors.Add("PasswordHash is required");

        if (string.IsNullOrWhiteSpace(PublicKey))
            errors.Add("PublicKey is required");

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents a device registered to a user
/// </summary>
public class Device
{
    /// <summary>
    /// Gets or sets the unique device identifier
    /// </summary>
    [BsonElement("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device name or description
    /// </summary>
    [BsonElement("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device type (e.g., web, mobile)
    /// </summary>
    [BsonElement("deviceType")]
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the device was registered
    /// </summary>
    [BsonElement("registeredAt")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the device was last active
    /// </summary>
    [BsonElement("lastActiveAt")]
    public DateTime? LastActiveAt { get; set; }
}
