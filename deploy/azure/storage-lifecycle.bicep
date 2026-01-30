// Azure Blob Storage Lifecycle Management Policy
// This template configures automatic tiering of blobs based on access patterns

@description('Name of the storage account')
param storageAccountName string

@description('Enable lifecycle policies for archives/ prefix')
param enableArchiveLifecycle bool = true

@description('Enable lifecycle policies for files/ prefix')
param enableFilesLifecycle bool = true

@description('Days after which archive blobs move to Cool tier')
param archiveCoolTierDays int = 90

@description('Days after which archive blobs move to Archive tier')
param archiveArchiveTierDays int = 180

@description('Days after which file blobs move to Cool tier')
param filesCoolTierDays int = 30

@description('Days after which file blobs move to Archive tier')
param filesArchiveTierDays int = 180

// Reference existing storage account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

// Configure lifecycle management policy
resource lifecyclePolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2023-01-01' = {
  name: 'default'
  parent: storageAccount
  properties: {
    policy: {
      rules: concat(
        enableArchiveLifecycle ? [
          {
            enabled: true
            name: 'move-archives-to-cool'
            type: 'Lifecycle'
            definition: {
              actions: {
                baseBlob: {
                  tierToCool: {
                    daysAfterModificationGreaterThan: archiveCoolTierDays
                  }
                }
              }
              filters: {
                blobTypes: [
                  'blockBlob'
                ]
                prefixMatch: [
                  'archives/'
                ]
              }
            }
          }
          {
            enabled: true
            name: 'move-archives-to-archive'
            type: 'Lifecycle'
            definition: {
              actions: {
                baseBlob: {
                  tierToArchive: {
                    daysAfterModificationGreaterThan: archiveArchiveTierDays
                  }
                }
              }
              filters: {
                blobTypes: [
                  'blockBlob'
                ]
                prefixMatch: [
                  'archives/'
                ]
              }
            }
          }
        ] : [],
        enableFilesLifecycle ? [
          {
            enabled: true
            name: 'move-files-to-cool'
            type: 'Lifecycle'
            definition: {
              actions: {
                baseBlob: {
                  tierToCool: {
                    daysAfterModificationGreaterThan: filesCoolTierDays
                  }
                }
              }
              filters: {
                blobTypes: [
                  'blockBlob'
                ]
                prefixMatch: [
                  'files/'
                ]
              }
            }
          }
          {
            enabled: true
            name: 'move-files-to-archive'
            type: 'Lifecycle'
            definition: {
              actions: {
                baseBlob: {
                  tierToArchive: {
                    daysAfterModificationGreaterThan: filesArchiveTierDays
                  }
                }
              }
              filters: {
                blobTypes: [
                  'blockBlob'
                ]
                prefixMatch: [
                  'files/'
                ]
              }
            }
          }
        ] : []
      )
    }
  }
}

output lifecyclePolicyName string = lifecyclePolicy.name
output storageAccountId string = storageAccount.id
