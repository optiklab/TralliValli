# Azure Resources Inventory

This document lists all Azure resources defined in the Bicep templates.

## Resources in main.bicep

### 1. Log Analytics Workspace
- **Resource Type**: `Microsoft.OperationalInsights/workspaces@2022-10-01`
- **Name**: `{appName}-{environment}-logs`
- **Purpose**: Centralized logging and monitoring for all resources
- **Configuration**:
  - SKU: PerGB2018
  - Retention: 30 days (dev), 90 days (prod)
  - Daily quota: 1GB (dev), unlimited (prod)

### 2. Container Registry
- **Resource Type**: `Microsoft.ContainerRegistry/registries@2023-07-01`
- **Name**: `{appName}{environment}{uniqueString}`
- **Purpose**: Private Docker image registry
- **Configuration**:
  - SKU: Basic (dev), Standard (prod)
  - Admin user: Enabled
  - Retention policy: 30 days
  - Zone redundancy: Enabled (prod only)

### 3. Container Apps Environment
- **Resource Type**: `Microsoft.App/managedEnvironments@2023-05-01`
- **Name**: `{appName}-{environment}-env`
- **Purpose**: Managed Kubernetes environment for containers
- **Configuration**:
  - Log Analytics integration
  - Zone redundancy: Enabled (prod only)

### 4. API Container App
- **Resource Type**: `Microsoft.App/containerApps@2023-05-01`
- **Name**: `{appName}-{environment}-api`
- **Purpose**: Scalable .NET API application
- **Configuration**:
  - CPU: 0.5 cores
  - Memory: 1GB
  - Min replicas: 1 (dev), 2 (prod)
  - Max replicas: 3 (dev), 10 (prod)
  - Ingress: HTTPS, external
  - Auto-scaling: HTTP concurrent requests (10)

### 5. MongoDB Container Instance
- **Resource Type**: `Microsoft.ContainerInstance/containerGroups@2023-05-01`
- **Name**: `{appName}-{environment}-mongodb`
- **Purpose**: MongoDB database with persistent storage
- **Configuration**:
  - Image: mongo:latest
  - CPU: 1 core (dev), 2 cores (prod)
  - Memory: 2GB (dev), 4GB (prod)
  - Network: Public IP with FQDN
  - Volume: Azure File Share (mongodb-data)

### 6. Azure Communication Services
- **Resource Type**: `Microsoft.Communication/communicationServices@2023-04-01`
- **Name**: `{appName}-{environment}-acs`
- **Purpose**: Communication platform for messaging
- **Configuration**:
  - Data location: United States (configurable)
  - Scope: Global

### 7. Storage Account
- **Resource Type**: `Microsoft.Storage/storageAccounts@2023-01-01`
- **Name**: `{appName}{environment}{uniqueString}`
- **Purpose**: Blob and file storage
- **Configuration**:
  - SKU: Standard_LRS
  - Access tier: Hot
  - HTTPS only: Yes
  - TLS version: 1.2+
  - Public blob access: Disabled

### 8. Blob Service
- **Resource Type**: `Microsoft.Storage/storageAccounts/blobServices@2023-01-01`
- **Name**: default
- **Configuration**:
  - Delete retention: 7 days
  - Container delete retention: 7 days

### 9. Archived Messages Container
- **Resource Type**: `Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01`
- **Name**: archived-messages
- **Purpose**: Storage for archived messages
- **Configuration**:
  - Public access: None

### 10. Files Container
- **Resource Type**: `Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01`
- **Name**: files
- **Purpose**: Storage for shared media and documents
- **Configuration**:
  - Public access: None

### 11. File Services
- **Resource Type**: `Microsoft.Storage/storageAccounts/fileServices@2023-01-01`
- **Name**: default
- **Configuration**:
  - Share delete retention: 7 days

### 12. MongoDB File Share
- **Resource Type**: `Microsoft.Storage/storageAccounts/fileServices/shares@2023-01-01`
- **Name**: mongodb-data
- **Purpose**: Persistent storage for MongoDB data
- **Configuration**:
  - Quota: 20GB (dev), 100GB (prod)
  - Access tier: TransactionOptimized
  - Protocol: SMB

## Module: storage-lifecycle.bicep

### Lifecycle Management Policy
- **Resource Type**: `Microsoft.Storage/storageAccounts/managementPolicies@2023-01-01`
- **Purpose**: Automatic blob tiering to optimize storage costs
- **Rules**:
  1. **archives/ prefix**
     - Cool tier: After 90 days
     - Archive tier: After 180 days
  2. **files/ prefix**
     - Cool tier: After 30 days
     - Archive tier: After 180 days

## Resource in main-with-rg.bicep

### Resource Group
- **Resource Type**: `Microsoft.Resources/resourceGroups@2021-04-01`
- **Name**: `{appName}-{environment}-rg`
- **Purpose**: Container for all infrastructure resources
- **Scope**: Subscription

## Total Resource Count

- **Main template**: 12 resources
- **Lifecycle module**: 1 resource (management policy)
- **Subscription template**: 1 resource (resource group)
- **Grand total**: 13 resources + 1 module

## Dependencies

The resources have the following deployment order:

1. Log Analytics Workspace (no dependencies)
2. Container Registry (no dependencies)
3. Storage Account (no dependencies)
4. Blob Service → Storage Account
5. Containers → Blob Service
6. File Services → Storage Account
7. File Share → File Services
8. Container Apps Environment → Log Analytics Workspace
9. API Container App → Container Apps Environment, Container Registry, Storage Account, MongoDB, Communication Services
10. MongoDB Container Instance → Storage Account, File Share
11. Communication Services (no dependencies)
12. Lifecycle Policies (module) → Storage Account

## Resource Naming Convention

All resources follow consistent naming patterns:

- **With unique suffix**: `{appName}{environment}{uniqueString(resourceGroup().id)}`
  - Storage Account
  - Container Registry
  
- **With dashes**: `{appName}-{environment}-{resourceType}`
  - Container Apps Environment: `tralivali-dev-env`
  - API Container App: `tralivali-dev-api`
  - MongoDB: `tralivali-dev-mongodb`
  - Log Analytics: `tralivali-dev-logs`
  - Communication Services: `tralivali-dev-acs`

- **Resource Group**: `{appName}-{environment}-rg`
  - Example: `tralivali-dev-rg`
