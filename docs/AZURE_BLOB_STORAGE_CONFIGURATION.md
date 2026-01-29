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

To optimize storage costs, configure lifecycle management policies to automatically move archives to cooler storage tiers:

- **Cool tier**: After 90 days of inactivity
- **Archive tier**: After 180 days of inactivity

### Configuring via Azure Portal

1. Navigate to your Storage Account in the Azure Portal
2. Under "Data management", select "Lifecycle management"
3. Click "Add a rule"
4. Create two rules:

#### Rule 1: Move to Cool Tier
- **Name**: `move-to-cool-tier`
- **Rule scope**: Limit blobs with filters
- **Blob type**: Block blobs
- **Prefix match**: `archives/`
- **Action**: Tier to cool storage
- **Days after last modification**: 90

#### Rule 2: Move to Archive Tier
- **Name**: `move-to-archive-tier`
- **Rule scope**: Limit blobs with filters
- **Blob type**: Block blobs
- **Prefix match**: `archives/`
- **Action**: Tier to archive storage
- **Days after last modification**: 180

### Configuring via Azure CLI

1. Create a lifecycle policy file `lifecycle-policy.json`:

```json
{
  "rules": [
    {
      "enabled": true,
      "name": "move-to-cool-tier",
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
      "name": "move-to-archive-tier",
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
| Hot | Highest | Immediate | Recent archives (< 90 days) |
| Cool | Medium | Immediate | Older archives (90-180 days) |
| Archive | Lowest | Hours | Historical archives (> 180 days) |

**Note**: Retrieving data from Archive tier requires rehydration and may take several hours.

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
