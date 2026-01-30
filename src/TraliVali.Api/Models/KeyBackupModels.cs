using System.ComponentModel.DataAnnotations;

namespace TraliVali.Api.Models;

/// <summary>
/// Request to store an encrypted key backup
/// </summary>
public class StoreKeyBackupRequest
{
    /// <summary>
    /// Backup format version
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Version { get; set; }

    /// <summary>
    /// Encrypted backup data (Base64 encoded)
    /// </summary>
    [Required]
    [MinLength(1)]
    public string EncryptedData { get; set; } = string.Empty;

    /// <summary>
    /// Initialization vector for decryption (Base64 encoded)
    /// </summary>
    [Required]
    [MinLength(1)]
    public string Iv { get; set; } = string.Empty;

    /// <summary>
    /// PBKDF2 salt for key derivation (Base64 encoded)
    /// </summary>
    [Required]
    [MinLength(1)]
    public string Salt { get; set; } = string.Empty;
}

/// <summary>
/// Response after storing a key backup
/// </summary>
public class StoreKeyBackupResponse
{
    /// <summary>
    /// The backup ID
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// When the backup was created/updated
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response containing the encrypted key backup
/// </summary>
public class GetKeyBackupResponse
{
    /// <summary>
    /// The backup ID
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Backup format version
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Encrypted backup data (Base64 encoded)
    /// </summary>
    public string EncryptedData { get; set; } = string.Empty;

    /// <summary>
    /// Initialization vector for decryption (Base64 encoded)
    /// </summary>
    public string Iv { get; set; } = string.Empty;

    /// <summary>
    /// PBKDF2 salt for key derivation (Base64 encoded)
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// When the backup was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the backup was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response indicating whether a backup exists
/// </summary>
public class KeyBackupExistsResponse
{
    /// <summary>
    /// Whether a backup exists for the user
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// When the backup was last updated (if it exists)
    /// </summary>
    public DateTime? LastUpdatedAt { get; set; }
}
