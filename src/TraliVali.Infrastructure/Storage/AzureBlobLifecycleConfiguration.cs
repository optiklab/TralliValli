namespace TraliVali.Infrastructure.Storage;

/// <summary>
/// Configuration for Azure Blob Storage lifecycle management policies
/// </summary>
/// <remarks>
/// Lifecycle policies should be configured in Azure Storage Account to automatically
/// tier blobs based on access patterns and retention requirements.
/// 
/// For archives/ container:
/// - Move blobs to Cool tier after 90 days of inactivity
/// - Move blobs to Archive tier after 180 days of inactivity
/// 
/// For files/ container:
/// - Move blobs to Cool tier after 30 days of inactivity
/// - Move blobs to Archive tier after 180 days of inactivity
/// 
/// See deploy/azure/storage-lifecycle.bicep for Infrastructure as Code implementation
/// or docs/AZURE_BLOB_STORAGE_CONFIGURATION.md for manual configuration instructions.
/// </remarks>
public class AzureBlobLifecycleConfiguration
{
    /// <summary>
    /// Number of days after which archive blobs are moved to Cool tier
    /// </summary>
    public const int ArchivesCoolTierDays = 90;

    /// <summary>
    /// Number of days after which archive blobs are moved to Archive tier
    /// </summary>
    public const int ArchivesArchiveTierDays = 180;

    /// <summary>
    /// Prefix for archive blobs to apply lifecycle policies
    /// </summary>
    public const string ArchivesPrefix = "archives/";

    /// <summary>
    /// Number of days after which file blobs are moved to Cool tier
    /// </summary>
    public const int FilesCoolTierDays = 30;

    /// <summary>
    /// Number of days after which file blobs are moved to Archive tier
    /// </summary>
    public const int FilesArchiveTierDays = 180;

    /// <summary>
    /// Prefix for file blobs to apply lifecycle policies
    /// </summary>
    public const string FilesPrefix = "files/";
}
