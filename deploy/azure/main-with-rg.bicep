// Subscription-level deployment template
// This template creates a Resource Group and deploys all infrastructure to it

targetScope = 'subscription'

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
param location string = 'eastus'

@description('Application name prefix')
param appName string = 'tralivali'

@description('Resource Group name')
param resourceGroupName string = '${appName}-${environment}-rg'

@description('MongoDB root username')
@secure()
param mongoRootUsername string

@description('MongoDB root password')
@secure()
param mongoRootPassword string

@description('Tags to apply to all resources')
param tags object = {
  application: 'TraliVali'
  environment: environment
  managedBy: 'Bicep'
}

// ============================================================================
// RESOURCES
// ============================================================================

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Deploy infrastructure to Resource Group
module infrastructure 'main.bicep' = {
  name: 'infrastructure-deployment'
  scope: resourceGroup
  params: {
    environment: environment
    location: location
    appName: appName
    mongoRootUsername: mongoRootUsername
    mongoRootPassword: mongoRootPassword
    tags: tags
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output resourceGroupName string = resourceGroup.name
output resourceGroupId string = resourceGroup.id

// Pass through infrastructure outputs
output logAnalyticsWorkspaceId string = infrastructure.outputs.logAnalyticsWorkspaceId
output containerRegistryLoginServer string = infrastructure.outputs.containerRegistryLoginServer
output apiContainerAppFqdn string = infrastructure.outputs.apiContainerAppFqdn
output mongoContainerInstanceFqdn string = infrastructure.outputs.mongoContainerInstanceFqdn
output storageAccountName string = infrastructure.outputs.storageAccountName
output communicationServicesId string = infrastructure.outputs.communicationServicesId
