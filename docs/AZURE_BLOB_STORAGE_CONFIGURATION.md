# Azure Blob Storage Archive Configuration

This document describes how to configure Azure Blob Storage for message archiving in TraliVali.

## Overview

The `IAzureBlobService` provides methods for managing conversation message archives in Azure Blob Storage:

- `UploadArchiveAsync` - Upload an archive to storage
- `DownloadArchiveAsync` - Download an archive from storage
- `ListArchivesAsync` - List archives with a given prefix
- `DeleteArchiveAsync` - Delete an archive from storage

## Archive Path Structure

Archives are stored using the following path structure:

```
archives/{year}/{month}/messages_{conversationId}_{date}.json
```

**Example:**
```
archives/2024/01/messages_conv123_2024-01-15.json
archives/2024/02/messages_conv456_2024-02-20.json
```

This structure allows for:
- Easy organization by year and month
- Efficient prefix-based listing
- Clear identification of conversation and date
- Lifecycle policy application by prefix

## Lifecycle Management Policies

To optimize storage costs, configure lifecycle management policies to automatically move blobs to cooler storage tiers based on age. TraliVali uses different policies for different containers:

### Archives Container (`archives/` prefix)
- **Cool tier**: After 90 days of inactivity
- **Archive tier**: After 180 days of inactivity

### Files Container (`files/` prefix)
- **Cool tier**: After 30 days of inactivity
- **Archive tier**: After 180 days of inactivity

### Configuring via Bicep (Recommended)

The recommended approach is to use Infrastructure as Code with Bicep templates:

```bash
# Deploy infrastructure with lifecycle policies
cd deploy/azure
az deployment group create \
  --resource-group <resource-group-name> \
  --template-file main.bicep \
  --parameters environment=dev
```

See [deploy/azure/README.md](../deploy/azure/README.md) for detailed deployment instructions.

### Configuring via Azure Portal

1. Navigate to your Storage Account in the Azure Portal
2. Under "Data management", select "Lifecycle management"
3. Click "Add a rule"
4. Create the following rules:

#### Rule 1: Move Archives to Cool Tier
- **Name**: `move-archives-to-cool`
- **Rule scope**: Limit blobs with filters
- **Blob type**: Block blobs
- **Prefix match**: `archives/`
- **Action**: Tier to cool storage
- **Days after last modification**: 90

#### Rule 2: Move Archives to Archive Tier
- **Name**: `move-archives-to-archive`
- **Rule scope**: Limit blobs with filters
- **Blob type**: Block blobs
- **Prefix match**: `archives/`
- **Action**: Tier to archive storage
- **Days after last modification**: 180

#### Rule 3: Move Files to Cool Tier
- **Name**: `move-files-to-cool`
- **Rule scope**: Limit blobs with filters
- **Blob type**: Block blobs
- **Prefix match**: `files/`
- **Action**: Tier to cool storage
- **Days after last modification**: 30

#### Rule 4: Move Files to Archive Tier
- **Name**: `move-files-to-archive`
- **Rule scope**: Limit blobs with filters
- **Blob type**: Block blobs
- **Prefix match**: `files/`
- **Action**: Tier to archive storage
- **Days after last modification**: 180

### Configuring via Azure CLI

1. Create a lifecycle policy file `lifecycle-policy.json`:

```json
{
  "rules": [
    {
      "enabled": true,
      "name": "move-archives-to-cool",
      "type": "Lifecycle",
      "definition": {
        "actions": {
          "baseBlob": {
            "tierToCool": {
              "daysAfterModificationGreaterThan": 90
            }
          }
        },
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["archives/"]
        }
      }
    },
    {
      "enabled": true,
      "name": "move-archives-to-archive",
      "type": "Lifecycle",
      "definition": {
        "actions": {
          "baseBlob": {
            "tierToArchive": {
              "daysAfterModificationGreaterThan": 180
            }
          }
        },
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["archives/"]
        }
      }
    },
    {
      "enabled": true,
      "name": "move-files-to-cool",
      "type": "Lifecycle",
      "definition": {
        "actions": {
          "baseBlob": {
            "tierToCool": {
              "daysAfterModificationGreaterThan": 30
            }
          }
        },
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["files/"]
        }
      }
    },
    {
      "enabled": true,
      "name": "move-files-to-archive",
      "type": "Lifecycle",
      "definition": {
        "actions": {
          "baseBlob": {
            "tierToArchive": {
              "daysAfterModificationGreaterThan": 180
            }
          }
        },
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["files/"]
        }
      }
    }
  ]
}
```

2. Apply the policy:

```bash
az storage account management-policy create \
  --account-name <storage-account-name> \
  --policy @lifecycle-policy.json \
  --resource-group <resource-group-name>
```

## Usage Example

```csharp
// Initialize service
var connectionString = "DefaultEndpointsProtocol=https;AccountName=...";
var blobService = new AzureBlobService(connectionString, "archives");
await blobService.EnsureContainerExistsAsync();

// Upload archive
var conversationId = "conv123";
var date = DateTime.UtcNow;
var path = AzureBlobService.GenerateArchivePath(conversationId, date);
// Result: archives/2024/01/messages_conv123_2024-01-15.json

using var stream = new MemoryStream(jsonBytes);
await blobService.UploadArchiveAsync(stream, path);

// List archives for a conversation
var prefix = $"archives/2024/01/";
var archives = await blobService.ListArchivesAsync(prefix);

// Download archive
using var downloadStream = await blobService.DownloadArchiveAsync(path);
// Process stream...

// Delete archive
bool deleted = await blobService.DeleteArchiveAsync(path);
```

## Testing

The implementation includes comprehensive integration tests using the Azurite emulator:

```bash
dotnet test --filter "FullyQualifiedName~AzureBlobServiceTests"
```

All 28 tests cover:
- CRUD operations
- Path structure validation
- Error handling
- Resource disposal
- Archive organization

## Cost Optimization

The lifecycle policies help optimize costs:

| Tier | Cost (per GB/month) | Access Time | Use Case |
|------|---------------------|-------------|----------|
| Hot | Highest | Immediate | Recent archives/files (< 30-90 days) |
| Cool | Medium | Immediate | Older archives/files (30-180 days) |
| Archive | Lowest | Hours* | Historical archives/files (> 180 days) |

*Retrieving data from Archive tier requires rehydration and may take several hours.

### Cost Considerations

- **Storage costs**: Archive tier costs ~$0.002/GB/month vs Hot tier ~$0.02/GB/month (90% savings)
- **Retrieval costs**: Archive tier has per-GB data retrieval fees
- **Rehydration costs**: High priority rehydration costs significantly more than Standard
- **Early deletion fees**: Deleting blobs before minimum duration (Cool: 30 days, Archive: 180 days) incurs fees

## Rehydrating Archived Blobs

Blobs in the Archive tier are offline and must be rehydrated before they can be read. This is important to understand when users need to access older files or message archives.

### Rehydration Process Overview

When a blob is in Archive tier:
1. The blob cannot be read directly
2. You must change the tier to Hot or Cool to rehydrate it
3. Rehydration takes time (1-15 hours depending on priority)
4. Once rehydrated, the blob can be accessed immediately

### Rehydration Priority Options

#### Standard Priority
- **Duration**: Up to 15 hours
- **Cost**: Standard pricing
- **Use case**: Non-urgent retrieval, batch operations
- **Recommended for**: Scheduled exports, bulk operations

#### High Priority
- **Duration**: Typically under 1 hour
- **Cost**: 10x higher than Standard priority
- **Use case**: Urgent user requests
- **Recommended for**: User-initiated downloads requiring quick access

### Rehydration via Azure CLI

**Option 1: Copy to Hot Tier (Recommended)**
```bash
# Copy archived blob to Hot tier (keeps original in Archive)
az storage blob copy start \
  --account-name <storage-account-name> \
  --destination-container files \
  --destination-blob <destination-path> \
  --source-uri <archived-blob-url> \
  --tier Hot \
  --rehydrate-priority Standard
```

**Option 2: Change Tier In-Place**
```bash
# Change blob tier directly (overwrites archive copy)
az storage blob set-tier \
  --account-name <storage-account-name> \
  --container-name files \
  --name <blob-path> \
  --tier Hot \
  --rehydrate-priority High
```

### Rehydration via Azure Portal

1. Navigate to Storage Account → Containers
2. Browse to the archived blob
3. Right-click the blob and select "Change tier"
4. Select target tier (Hot or Cool)
5. Choose rehydration priority (Standard or High)
6. Click "Save"
7. Monitor the blob properties to track rehydration progress

### Programmatic Rehydration (C#)

Add rehydration support to your application:

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class BlobRehydrationService
{
    private readonly BlobServiceClient _blobServiceClient;
    
    public BlobRehydrationService(string connectionString)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
    }
    
    /// <summary>
    /// Rehydrate a blob from Archive tier to Hot tier
    /// </summary>
    public async Task<bool> RehydrateBlobAsync(
        string containerName, 
        string blobName, 
        RehydratePriority priority = RehydratePriority.Standard,
        CancellationToken cancellationToken = default)
    {
        var blobClient = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);
        
        // Check current tier
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        
        if (properties.Value.AccessTier == AccessTier.Archive)
        {
            // Start rehydration
            await blobClient.SetAccessTierAsync(
                AccessTier.Hot, 
                rehydratePriority: priority,
                cancellationToken: cancellationToken);
            
            return true;
        }
        
        return false; // Already rehydrated or not in Archive tier
    }
    
    /// <summary>
    /// Check if a blob is still rehydrating
    /// </summary>
    public async Task<bool> IsRehydratingAsync(
        string containerName, 
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);
        
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        
        // ArchiveStatus is null when rehydration is complete
        return properties.Value.ArchiveStatus != null;
    }
    
    /// <summary>
    /// Wait for rehydration to complete
    /// </summary>
    public async Task WaitForRehydrationAsync(
        string containerName, 
        string blobName,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollingInterval ?? TimeSpan.FromSeconds(30);
        var blobClient = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var properties = await blobClient.GetPropertiesAsync(
                cancellationToken: cancellationToken);
            
            if (properties.Value.ArchiveStatus == null)
            {
                // Rehydration complete
                return;
            }
            
            await Task.Delay(interval, cancellationToken);
        }
    }
}
```

### Monitoring Rehydration Status

**Via Azure CLI:**
```bash
# Check blob status
az storage blob show \
  --account-name <storage-account-name> \
  --container-name files \
  --name <blob-path> \
  --query '[name, properties.accessTier, properties.archiveStatus, properties.rehydratePriority]' \
  --output table
```

**Archive Status Values:**
- `null` - Blob is not archived or rehydration is complete
- `rehydrate-pending-to-hot` - Rehydration to Hot tier in progress
- `rehydrate-pending-to-cool` - Rehydration to Cool tier in progress

### Best Practices for Rehydration

1. **Plan Ahead**: Consider rehydration time when designing user experiences
2. **Use Standard Priority by Default**: Reserve High priority for truly urgent requests
3. **Async Processing**: Implement background jobs for batch rehydration
4. **User Communication**: Inform users about expected wait times for archived content
5. **Caching Strategy**: After rehydration, consider keeping frequently accessed blobs in Hot tier
6. **Monitoring**: Track rehydration operations and their costs
7. **Copy Instead of Tier Change**: Use copy operations to preserve archived copies

### Application Integration Example

```csharp
public async Task<Stream> GetFileAsync(string fileId)
{
    var file = await _fileRepository.GetByIdAsync(fileId);
    var blobClient = _blobServiceClient
        .GetBlobContainerClient("files")
        .GetBlobClient(file.BlobPath);
    
    // Check if blob is archived
    var properties = await blobClient.GetPropertiesAsync();
    
    if (properties.Value.AccessTier == AccessTier.Archive)
    {
        // Start rehydration with High priority for user requests
        await blobClient.SetAccessTierAsync(
            AccessTier.Hot, 
            rehydratePriority: RehydratePriority.High);
        
        // Return a response indicating the file needs to be rehydrated
        throw new InvalidOperationException(
            "File is being restored from archive storage. This may take up to 1 hour. " +
            "Please try again later.");
    }
    
    // Check if rehydration is in progress
    if (properties.Value.ArchiveStatus != null)
    {
        throw new InvalidOperationException(
            "File is currently being restored from archive storage. " +
            "Please try again in a few minutes.");
    }
    
    // Blob is available, download it
    return await blobClient.OpenReadAsync();
}
```

## Configuration

Add the following to your `appsettings.json`:

```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
    "ContainerName": "archives"
  }
}
```

Or use environment variables:

```bash
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=...
AZURE_STORAGE_CONTAINER_NAME=archives
```

## Security Considerations

1. Use Azure Key Vault to store connection strings
2. Enable Azure Storage encryption at rest (enabled by default)
3. Use Shared Access Signatures (SAS) for time-limited access
4. Enable Azure Storage firewall rules
5. Monitor access logs using Azure Monitor
