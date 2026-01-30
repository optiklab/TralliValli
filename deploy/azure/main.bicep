// Main Azure Infrastructure Template for TraliVali
// This template provisions all required Azure resources

targetScope = 'resourceGroup'

// ============================================================================
// PARAMETERS
// ============================================================================

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

// Storage Account Parameters
@description('Storage account name (must be globally unique, 3-24 characters, lowercase letters and numbers only)')
@minLength(3)
@maxLength(24)
param storageAccountName string = '${appName}${environment}${uniqueString(resourceGroup().id)}'

@description('Enable lifecycle policies for blob storage')
param enableLifecyclePolicies bool = true

// Container Registry Parameters
@description('Container Registry name (must be globally unique, 5-50 alphanumeric characters)')
@minLength(5)
@maxLength(50)
param containerRegistryName string = '${appName}${environment}${uniqueString(resourceGroup().id)}'

@description('Container Registry SKU (Basic, Standard, or Premium)')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param containerRegistrySku string = environment == 'prod' ? 'Standard' : 'Basic'

// Container Apps Parameters
@description('Container Apps Environment name')
param containerAppsEnvironmentName string = '${appName}-${environment}-env'

@description('API Container App name')
param apiContainerAppName string = '${appName}-${environment}-api'

@description('API container image (format: registry/image:tag)')
param apiContainerImage string = '${containerRegistryName}.azurecr.io/tralivali-api:latest'

@description('Minimum number of API replicas')
@minValue(0)
@maxValue(30)
param apiMinReplicas int = environment == 'prod' ? 2 : 1

@description('Maximum number of API replicas')
@minValue(1)
@maxValue(30)
param apiMaxReplicas int = environment == 'prod' ? 10 : 3

@description('API CPU allocation (in cores)')
param apiCpu string = '0.5'

@description('API memory allocation (in Gi)')
param apiMemory string = '1.0Gi'

// MongoDB Container Instance Parameters
@description('MongoDB Container Instance name')
param mongoContainerName string = '${appName}-${environment}-mongodb'

@description('MongoDB container image')
param mongoContainerImage string = 'mongo:latest'

@description('MongoDB root username')
@secure()
param mongoRootUsername string

@description('MongoDB root password')
@secure()
param mongoRootPassword string

@description('MongoDB database name')
param mongoDatabaseName string = 'tralivali'

@description('MongoDB CPU allocation (in cores)')
param mongoCpu string = environment == 'prod' ? '2.0' : '1.0'

@description('MongoDB memory allocation (in Gi)')
param mongoMemory string = environment == 'prod' ? '4.0' : '2.0'

// Azure Communication Services Parameters
@description('Azure Communication Services resource name')
param communicationServicesName string = '${appName}-${environment}-acs'

@description('Azure Communication Services data location')
@allowed([
  'Africa'
  'Asia Pacific'
  'Australia'
  'Brazil'
  'Canada'
  'Europe'
  'France'
  'Germany'
  'India'
  'Japan'
  'Korea'
  'Norway'
  'Switzerland'
  'UAE'
  'UK'
  'United States'
])
param communicationServicesDataLocation string = 'United States'

// Log Analytics Parameters
@description('Log Analytics Workspace name')
param logAnalyticsWorkspaceName string = '${appName}-${environment}-logs'

@description('Log Analytics Workspace SKU')
@allowed([
  'PerGB2018'
  'Free'
  'Standalone'
  'PerNode'
  'Standard'
  'Premium'
])
param logAnalyticsSku string = 'PerGB2018'

@description('Log Analytics data retention in days (30-730)')
@minValue(30)
@maxValue(730)
param logAnalyticsRetentionDays int = environment == 'prod' ? 90 : 30

@description('Tags to apply to all resources')
param tags object = {
  application: 'TraliVali'
  environment: environment
  managedBy: 'Bicep'
}

// ============================================================================
// RESOURCES
// ============================================================================

// Log Analytics Workspace (required for Container Apps Environment)
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: logAnalyticsSku
    }
    retentionInDays: logAnalyticsRetentionDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: environment == 'prod' ? -1 : 1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  tags: tags
  sku: {
    name: containerRegistrySku
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
      retentionPolicy: {
        days: 30
        status: 'enabled'
      }
    }
    encryption: {
      status: 'disabled'
    }
    dataEndpointEnabled: false
    zoneRedundancy: environment == 'prod' ? 'Enabled' : 'Disabled'
  }
}

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: containerAppsEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    zoneRedundant: environment == 'prod' ? true : false
  }
}

// API Container App
resource apiContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: apiContainerAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'container-registry-password'
        }
      ]
      secrets: [
        {
          name: 'container-registry-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
        {
          name: 'mongodb-connection-string'
          value: 'mongodb://${mongoRootUsername}:${mongoRootPassword}@${mongoContainerInstance.properties.ipAddress.fqdn}:27017/${mongoDatabaseName}?authSource=admin'
        }
        {
          name: 'storage-connection-string'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${az.environment().suffixes.storage}'
        }
        {
          name: 'communication-services-connection-string'
          value: communicationServices.listKeys().primaryConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiContainerImage
          resources: {
            cpu: json(apiCpu)
            memory: apiMemory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'MongoDB__ConnectionString'
              secretRef: 'mongodb-connection-string'
            }
            {
              name: 'Azure__StorageConnectionString'
              secretRef: 'storage-connection-string'
            }
            {
              name: 'Azure__CommunicationServicesConnectionString'
              secretRef: 'communication-services-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: apiMinReplicas
        maxReplicas: apiMaxReplicas
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}

// MongoDB Container Instance
resource mongoContainerInstance 'Microsoft.ContainerInstance/containerGroups@2023-05-01' = {
  name: mongoContainerName
  location: location
  tags: tags
  properties: {
    containers: [
      {
        name: 'mongodb'
        properties: {
          image: mongoContainerImage
          ports: [
            {
              port: 27017
              protocol: 'TCP'
            }
          ]
          environmentVariables: [
            {
              name: 'MONGO_INITDB_ROOT_USERNAME'
              value: mongoRootUsername
            }
            {
              name: 'MONGO_INITDB_ROOT_PASSWORD'
              secureValue: mongoRootPassword
            }
            {
              name: 'MONGO_INITDB_DATABASE'
              value: mongoDatabaseName
            }
          ]
          resources: {
            requests: {
              cpu: json(mongoCpu)
              memoryInGB: json(mongoMemory)
            }
          }
          volumeMounts: [
            {
              name: 'mongodb-data'
              mountPath: '/data/db'
            }
          ]
        }
      }
    ]
    osType: 'Linux'
    restartPolicy: 'Always'
    ipAddress: {
      type: 'Public'
      dnsNameLabel: '${mongoContainerName}-${uniqueString(resourceGroup().id)}'
      ports: [
        {
          port: 27017
          protocol: 'TCP'
        }
      ]
    }
    volumes: [
      {
        name: 'mongodb-data'
        azureFile: {
          shareName: 'mongodb-data'
          storageAccountName: storageAccount.name
          storageAccountKey: storageAccount.listKeys().keys[0].value
        }
      }
    ]
  }
}

// Azure Communication Services
resource communicationServices 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServicesName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: communicationServicesDataLocation
  }
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

// File share for MongoDB data persistence
resource fileServices 'Microsoft.Storage/storageAccounts/fileServices@2023-01-01' = {
  name: 'default'
  parent: storageAccount
  properties: {
    shareDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource mongoFileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-01-01' = {
  name: 'mongodb-data'
  parent: fileServices
  properties: {
    accessTier: 'TransactionOptimized'
    shareQuota: environment == 'prod' ? 100 : 20
    enabledProtocols: 'SMB'
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

// ============================================================================
// OUTPUTS
// ============================================================================

// Log Analytics Outputs
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output logAnalyticsCustomerId string = logAnalyticsWorkspace.properties.customerId

// Container Registry Outputs
output containerRegistryId string = containerRegistry.id
output containerRegistryName string = containerRegistry.name
output containerRegistryLoginServer string = containerRegistry.properties.loginServer

// Container Apps Outputs
output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsEnvironmentName string = containerAppsEnvironment.name
output apiContainerAppId string = apiContainerApp.id
output apiContainerAppName string = apiContainerApp.name
output apiContainerAppFqdn string = apiContainerApp.properties.configuration.ingress.fqdn

// MongoDB Outputs
output mongoContainerInstanceId string = mongoContainerInstance.id
output mongoContainerInstanceName string = mongoContainerInstance.name
output mongoContainerInstanceFqdn string = mongoContainerInstance.properties.ipAddress.fqdn
output mongoContainerInstanceIpAddress string = mongoContainerInstance.properties.ipAddress.ip

// Communication Services Outputs
output communicationServicesId string = communicationServices.id
output communicationServicesName string = communicationServices.name

// Storage Outputs
output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
output storageAccountPrimaryEndpoints object = storageAccount.properties.primaryEndpoints
output archivedMessagesContainerName string = archivedMessagesContainer.name
output filesContainerName string = filesContainer.name
output lifecyclePoliciesDeployed bool = enableLifecyclePolicies
