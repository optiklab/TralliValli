# Azure Blob Storage Lifecycle Policy Verification

This document describes how to manually verify Azure Blob Storage lifecycle policies using the Azurite emulator.

## Overview

Azure Blob Storage lifecycle policies automatically move blobs between access tiers (Hot → Cool → Archive) based on age and access patterns to optimize storage costs. The application uses lifecycle policies defined in `AzureBlobLifecycleConfiguration.cs`.

## Lifecycle Policy Configuration

The application defines the following lifecycle policies:

### Archives Container
- **Cool tier**: After 90 days of inactivity
- **Archive tier**: After 180 days of inactivity
- **Prefix**: `archives/`

### Files Container
- **Cool tier**: After 30 days of inactivity
- **Archive tier**: After 180 days of inactivity
- **Prefix**: `files/`

## Manual Verification Steps

### 1. Verify Policy Configuration

Check that the lifecycle configuration constants match the intended policy:

```csharp
// From AzureBlobLifecycleConfiguration.cs
public const int ArchivesCoolTierDays = 90;
public const int ArchivesArchiveTierDays = 180;
public const int FilesCoolTierDays = 30;
public const int FilesArchiveTierDays = 180;
```

### 2. Verify Policy Application in Azure Portal

**Note**: Azurite emulator does **not** support lifecycle management policies. These must be verified in a real Azure Storage Account.

1. Navigate to Azure Portal → Storage Account
2. Go to "Data management" → "Lifecycle management"
3. Verify that rules exist for:
   - Archives container with correct prefixes and tier transitions
   - Files container with correct prefixes and tier transitions

### 3. Verify Policy Execution

To verify that policies are being applied correctly:

1. **Upload test blobs** to the storage account
2. **Check blob tier** using Azure Portal or Azure Storage Explorer:
   ```bash
   # Using Azure CLI
   az storage blob show --account-name <account-name> \
     --container-name archives \
     --name <blob-name> \
     --query properties.tier
   ```
3. **Verify tier transitions** after the configured time periods

### 4. Integration with Infrastructure as Code

The lifecycle policies are defined in the Bicep template:
- **File**: `deploy/azure/storage-lifecycle.bicep`
- **Documentation**: `docs/AZURE_BLOB_STORAGE_CONFIGURATION.md`

Verify that the Bicep template values match the configuration constants:

```bicep
// Example from storage-lifecycle.bicep
rules: [
  {
    name: 'ArchiveTierPolicy'
    definition: {
      filters: {
        blobTypes: ['blockBlob']
        prefixMatch: ['archives/']
      }
      actions: {
        baseBlob: {
          tierToCool: {
            daysAfterModificationGreaterThan: 90
          }
          tierToArchive: {
            daysAfterModificationGreaterThan: 180
          }
        }
      }
    }
  }
]
```

## Limitations with Azurite

The Azurite emulator has the following limitations regarding lifecycle policies:

1. **No lifecycle management support**: Azurite does not automatically apply lifecycle policies
2. **Manual tier changes**: You cannot manually change blob access tiers in Azurite
3. **Testing approach**: Integration tests verify blob operations (upload, download, list, delete) but cannot test tier transitions

## Testing Strategy

Given Azurite's limitations, the testing strategy is:

1. **Unit/Integration Tests** (with Azurite):
   - ✅ Upload functionality
   - ✅ Download functionality
   - ✅ Listing functionality
   - ✅ Deletion functionality
   - ✅ Presigned URL generation
   - ✅ Configuration constants verification

2. **Manual Verification** (with Azure Storage):
   - 🔧 Lifecycle policy configuration
   - 🔧 Tier transitions over time
   - 🔧 Cost optimization verification

3. **Infrastructure Testing** (with Bicep/ARM):
   - 🔧 Policy deployment validation
   - 🔧 Policy syntax verification

## Recommended Manual Testing Process

### Pre-deployment
1. Review `AzureBlobLifecycleConfiguration.cs` for correct values
2. Review `deploy/azure/storage-lifecycle.bicep` for matching values
3. Run `dotnet test` to verify all integration tests pass

### Post-deployment
1. Deploy infrastructure using Bicep template
2. Verify lifecycle policies in Azure Portal
3. Upload test blobs with backdated creation times (if possible)
4. Monitor blob tiers over the configured time periods
5. Verify cost optimization metrics in Azure Cost Management

## References

- [Azure Blob Storage lifecycle management](https://docs.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview)
- [Azurite limitations](https://github.com/Azure/Azurite#features-and-limitations)
- Project documentation: `docs/AZURE_BLOB_STORAGE_CONFIGURATION.md`
- Bicep template: `deploy/azure/storage-lifecycle.bicep`
