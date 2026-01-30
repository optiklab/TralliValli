# Azure Deployment Templates

This directory contains Bicep templates for deploying TraliVali infrastructure to Azure.

## Files

- **main.bicep**: Main infrastructure template that provisions:
  - Azure Storage Account with Hot access tier
  - Blob containers (archived-messages, files)
  - Lifecycle management policies
  - Security configurations (HTTPS only, TLS 1.2+, private access)

- **storage-lifecycle.bicep**: Blob storage lifecycle management policies module
  - Configures automatic tiering based on blob age
  - Separate policies for archives/ and files/ prefixes

## Prerequisites

- Azure CLI installed ([Install Guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- Azure subscription with appropriate permissions
- Logged in to Azure CLI: `az login`

## Deployment

### 1. Create Resource Group

```bash
# Set variables
RESOURCE_GROUP="tralivali-rg"
LOCATION="eastus"
ENVIRONMENT="dev"  # or staging, prod

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### 2. Deploy Infrastructure

```bash
# Deploy main template
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters environment=$ENVIRONMENT \
  --parameters location=$LOCATION
```

### 3. Deploy Only Lifecycle Policies (Optional)

If you already have a storage account and only want to configure lifecycle policies:

```bash
# Set your storage account name
STORAGE_ACCOUNT_NAME="yourStorageAccountName"

# Deploy lifecycle policies
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file storage-lifecycle.bicep \
  --parameters storageAccountName=$STORAGE_ACCOUNT_NAME
```

### 4. Customize Parameters

You can override default parameters:

```bash
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters environment=prod \
  --parameters location=westus2 \
  --parameters appName=myapp \
  --parameters enableLifecyclePolicies=true
```

## Lifecycle Policy Configuration

The templates configure automatic blob tiering to optimize storage costs:

### Archives Container (`archives/` prefix)
- **Cool tier**: After 90 days of inactivity
- **Archive tier**: After 180 days of inactivity

### Files Container (`files/` prefix)
- **Cool tier**: After 30 days of inactivity
- **Archive tier**: After 180 days of inactivity

### Storage Tier Comparison

| Tier    | Access Time | Cost (per GB) | Use Case |
|---------|-------------|---------------|----------|
| Hot     | Immediate   | Highest       | Frequently accessed files |
| Cool    | Immediate   | Medium        | Infrequently accessed files (30-90+ days old) |
| Archive | Hours*      | Lowest        | Rarely accessed files (180+ days old) |

*Archive tier requires rehydration before access (see below)

## Rehydration Process

Blobs in the Archive tier must be rehydrated before they can be read.

### Rehydration Options

#### 1. Standard Priority (Default)
- **Duration**: Up to 15 hours
- **Cost**: Lower
- **Use case**: Non-urgent retrieval

```bash
# Copy blob to Hot tier (keeps original in Archive)
az storage blob copy start \
  --account-name <storage-account> \
  --destination-container files \
  --destination-blob <path-to-file> \
  --source-uri <blob-url> \
  --tier Hot \
  --rehydrate-priority Standard
```

#### 2. High Priority
- **Duration**: Typically under 1 hour
- **Cost**: Higher (10x standard priority)
- **Use case**: Urgent retrieval

```bash
# Change blob tier directly (High priority)
az storage blob set-tier \
  --account-name <storage-account> \
  --container-name files \
  --name <path-to-file> \
  --tier Hot \
  --rehydrate-priority High
```

### Rehydration via Azure Portal

1. Navigate to Storage Account → Containers
2. Select the blob in Archive tier
3. Click "Change tier"
4. Select target tier (Hot or Cool)
5. Choose rehydration priority
6. Click "Save"

### Monitoring Rehydration Status

```bash
# Check blob archive status
az storage blob show \
  --account-name <storage-account> \
  --container-name files \
  --name <path-to-file> \
  --query '[archiveStatus, rehydratePriority, accessTier]' \
  --output table
```

**Archive Status Values**:
- `null` - Blob is not archived or rehydration is complete
- `rehydrate-pending-to-hot` - Rehydration to Hot tier in progress
- `rehydrate-pending-to-cool` - Rehydration to Cool tier in progress

### Programmatic Rehydration (C#)

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

// Initialize client
var blobClient = new BlobClient(connectionString, containerName, blobName);

// Check current tier
var properties = await blobClient.GetPropertiesAsync();
if (properties.Value.AccessTier == AccessTier.Archive)
{
    // Rehydrate to Hot tier with High priority
    await blobClient.SetAccessTierAsync(
        AccessTier.Hot, 
        rehydratePriority: RehydratePriority.High);
    
    // Wait for rehydration to complete
    while (true)
    {
        properties = await blobClient.GetPropertiesAsync();
        if (properties.Value.ArchiveStatus == null)
        {
            // Rehydration complete
            break;
        }
        await Task.Delay(TimeSpan.FromSeconds(30));
    }
}

// Now blob can be downloaded
using var stream = await blobClient.OpenReadAsync();
// Process stream...
```

## Best Practices

1. **Plan for Rehydration Time**: Consider Archive tier only for data you don't need immediately
2. **Use Cool Tier for Warm Data**: Data accessed occasionally but needs immediate availability
3. **Monitor Costs**: Track rehydration operations and early deletion fees
4. **Lifecycle Policy Testing**: Test policies in dev/staging before production
5. **Backup Strategy**: Keep critical data in Hot/Cool tiers with geo-redundancy

## Validation

After deployment, verify lifecycle policies are active:

```bash
# List lifecycle policies
az storage account management-policy show \
  --account-name <storage-account> \
  --resource-group $RESOURCE_GROUP
```

## Troubleshooting

### Issue: Deployment fails with "storage account name not available"
**Solution**: Change `storageAccountName` parameter to a unique value (globally unique across Azure)

### Issue: Lifecycle policies not applied
**Solution**: Ensure blobs match the prefix filters (`archives/`, `files/`) and are block blobs

### Issue: Blob stuck in rehydration
**Solution**: Rehydration can take up to 15 hours for Standard priority. Check status with `az storage blob show`

## Cost Optimization

- **Early deletion fees**: Deleting blobs before minimum storage duration (Cool: 30 days, Archive: 180 days) incurs early deletion fees
- **Rehydration costs**: High priority rehydration costs 10x more than Standard
- **Data retrieval**: Archive tier has per-GB data retrieval costs

## Security Notes

- HTTPS traffic only (enforced)
- TLS 1.2+ minimum version
- Public blob access disabled
- Soft delete enabled (7 days retention)
- Consider enabling Azure Storage firewall rules in production

## References

- [Azure Blob Storage Lifecycle Management](https://docs.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview)
- [Blob Rehydration from Archive Tier](https://docs.microsoft.com/en-us/azure/storage/blobs/archive-rehydrate-overview)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure Storage Pricing](https://azure.microsoft.com/en-us/pricing/details/storage/blobs/)
