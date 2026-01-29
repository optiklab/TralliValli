namespace TraliVali.Infrastructure.Storage;

/// <summary>
/// Configuration for Azure Blob Storage lifecycle management policies
/// </summary>
/// <remarks>
/// Lifecycle policies should be configured in Azure Storage Account to automatically:
/// - Move blobs to Cool tier after 90 days of inactivity
/// - Move blobs to Archive tier after 180 days of inactivity
/// 
/// Example Azure CLI commands to configure lifecycle policies:
/// 
/// Create a lifecycle policy JSON file (lifecycle-policy.json):
/// {
///   "rules": [
///     {
///       "enabled": true,
///       "name": "move-to-cool-tier",
///       "type": "Lifecycle",
///       "definition": {
///         "actions": {
///           "baseBlob": {
///             "tierToCool": {
///               "daysAfterModificationGreaterThan": 90
///             }
///           }
///         },
///         "filters": {
///           "blobTypes": ["blockBlob"],
///           "prefixMatch": ["archives/"]
///         }
///       }
///     },
///     {
///       "enabled": true,
///       "name": "move-to-archive-tier",
///       "type": "Lifecycle",
///       "definition": {
///         "actions": {
///           "baseBlob": {
///             "tierToArchive": {
///               "daysAfterModificationGreaterThan": 180
///             }
///           }
///         },
///         "filters": {
///           "blobTypes": ["blockBlob"],
///           "prefixMatch": ["archives/"]
///         }
///       }
///     }
///   ]
/// }
/// 
/// Apply the policy using Azure CLI:
/// az storage account management-policy create \
///   --account-name [storage-account-name] \
///   --policy @lifecycle-policy.json \
///   --resource-group [resource-group-name]
/// 
/// Or using Azure Portal:
/// 1. Navigate to your Storage Account
/// 2. Select "Lifecycle management" under "Data management"
/// 3. Add rule to move to Cool tier after 90 days
/// 4. Add rule to move to Archive tier after 180 days
/// 5. Set prefix filter to "archives/" to apply only to archive blobs
/// </remarks>
public class AzureBlobLifecycleConfiguration
{
    /// <summary>
    /// Number of days after which blobs are moved to Cool tier
    /// </summary>
    public const int CoolTierDays = 90;

    /// <summary>
    /// Number of days after which blobs are moved to Archive tier
    /// </summary>
    public const int ArchiveTierDays = 180;

    /// <summary>
    /// Prefix for archive blobs to apply lifecycle policies
    /// </summary>
    public const string ArchivePrefix = "archives/";
}
