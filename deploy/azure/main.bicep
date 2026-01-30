// Main Azure Infrastructure Template for TraliVali
// This template provisions all required Azure resources

targetScope = 'resourceGroup'

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Application name prefix')
param appName string = 'tralivali'

@description('Storage account name (must be globally unique, 3-24 characters, lowercase letters and numbers only)')
@minLength(3)
@maxLength(24)
param storageAccountName string = '${appName}${environment}${uniqueString(resourceGroup().id)}'

@description('Enable lifecycle policies for blob storage')
param enableLifecyclePolicies bool = true

@description('Tags to apply to all resources')
param tags object = {
  application: 'TraliVali'
  environment: environment
  managedBy: 'Bicep'
}

// Storage Account for blobs (messages, files, archives)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    encryption: {
      services: {
        blob: {
          enabled: true
          keyType: 'Account'
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// Blob service for storage account
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  name: 'default'
  parent: storageAccount
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

// Container for archived messages
resource archivedMessagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: 'archived-messages'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

// Container for files (shared media, documents)
resource filesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: 'files'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

// Deploy lifecycle management policies
module lifecyclePolicies 'storage-lifecycle.bicep' = if (enableLifecyclePolicies) {
  name: 'storage-lifecycle-policies'
  params: {
    storageAccountName: storageAccount.name
    enableArchiveLifecycle: true
    enableFilesLifecycle: true
    archiveCoolTierDays: 90
    archiveArchiveTierDays: 180
    filesCoolTierDays: 30
    filesArchiveTierDays: 180
  }
}

// Outputs
output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
output storageAccountPrimaryEndpoints object = storageAccount.properties.primaryEndpoints
output archivedMessagesContainerName string = archivedMessagesContainer.name
output filesContainerName string = filesContainer.name
output lifecyclePoliciesDeployed bool = enableLifecyclePolicies
