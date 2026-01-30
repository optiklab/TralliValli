# Azure Deployment Templates

This directory contains Bicep templates for deploying TraliVali infrastructure to Azure.

## Files

- **main-with-rg.bicep**: Complete subscription-level deployment template that provisions:
  - **Resource Group**: Creates a new resource group for all resources
  - All infrastructure resources (via main.bicep module)

- **main.bicep**: Main infrastructure template (resource group scope) that provisions:
  - **Log Analytics Workspace**: Centralized logging and monitoring
  - **Container Registry**: Private Docker image registry with admin access
  - **Container Apps Environment**: Managed environment for containerized applications
  - **Container Apps (API)**: Scalable API container with auto-scaling rules
  - **Container Instance (MongoDB)**: MongoDB database with persistent storage
  - **Azure Communication Services**: Communication platform for messaging
  - **Storage Account**: Blob storage with Hot access tier
  - **Blob containers**: archived-messages and files containers
  - **File Share**: Persistent storage for MongoDB data
  - **Lifecycle management policies**: Automatic blob tiering

- **storage-lifecycle.bicep**: Blob storage lifecycle management policies module
  - Configures automatic tiering based on blob age
  - Separate policies for archives/ and files/ prefixes

- **parameters.dev.json**: Example parameter file for development environment (resource group scope)
- **parameters.prod.json**: Example parameter file for production environment (resource group scope)
- **parameters-with-rg.dev.json**: Example parameter file for development with resource group creation (subscription scope)
- **parameters-with-rg.prod.json**: Example parameter file for production with resource group creation (subscription scope)

## Infrastructure Overview

The template provisions a complete application infrastructure with:

### Compute Resources
- **Container Apps Environment**: Managed Kubernetes environment for containers
- **API Container App**: Scalable .NET API with automatic SSL, ingress, and health monitoring
- **MongoDB Container Instance**: Dedicated MongoDB container with persistent Azure File storage

### Data & Storage
- **Storage Account**: Blob storage for files and archived messages
- **Azure File Share**: Persistent storage for MongoDB data (20GB dev, 100GB prod)
- **Lifecycle Policies**: Automatic tiering to Cool (30-90 days) and Archive (180 days) tiers

### Networking & Security
- **Container Registry**: Private image registry with authentication
- **Log Analytics**: Centralized logging for all resources
- **Communication Services**: Azure Communication Services for messaging

### Environment-Based Configuration
Resources are automatically sized based on the environment parameter:
- **dev**: Minimal resources, lower costs, single replica
- **staging**: Medium resources for testing
- **prod**: High availability, zone redundancy, multiple replicas

## Prerequisites

- Azure CLI installed ([Install Guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- Azure subscription with appropriate permissions to create:
  - Resource Groups
  - Container Apps and Environments
  - Container Instances
  - Container Registry
  - Storage Accounts
  - Log Analytics Workspaces
  - Communication Services
- Logged in to Azure CLI: `az login`
- A container image built and ready to push (or use placeholder initially)

## Required Parameters

The template requires the following parameters to be provided:

### Secure Parameters (REQUIRED)
```bash
# MongoDB credentials - must be provided at deployment
mongoRootUsername="admin"
mongoRootPassword="YourSecurePassword123!"  # Use strong password
```

## Deployment

There are two deployment options:

### Option A: Complete Deployment (Subscription-level with Resource Group)

This option creates the resource group and all infrastructure in a single deployment.

```bash
# Set variables
LOCATION="eastus"
ENVIRONMENT="dev"  # or staging, prod

# Deploy everything including resource group
az deployment sub create \
  --location $LOCATION \
  --template-file main-with-rg.bicep \
  --parameters environment=$ENVIRONMENT \
  --parameters location=$LOCATION \
  --parameters mongoRootUsername="admin" \
  --parameters mongoRootPassword="YourSecurePassword123!"
```

### Option B: Deploy to Existing Resource Group

This option requires you to create the resource group first.

#### 1. Create Resource Group

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

#### 2. Deploy Complete Infrastructure

```bash
# Deploy main template with required parameters
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters environment=$ENVIRONMENT \
  --parameters location=$LOCATION \
  --parameters mongoRootUsername="admin" \
  --parameters mongoRootPassword="YourSecurePassword123!"
```

#### 3. Deploy with Parameter File (Recommended for Production)

Create a parameter file `parameters.prod.json`:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environment": {
      "value": "prod"
    },
    "location": {
      "value": "eastus"
    },
    "appName": {
      "value": "tralivali"
    },
    "mongoRootUsername": {
      "value": "admin"
    },
    "mongoRootPassword": {
      "reference": {
        "keyVault": {
          "id": "/subscriptions/{subscription-id}/resourceGroups/{rg-name}/providers/Microsoft.KeyVault/vaults/{vault-name}"
        },
        "secretName": "mongodb-root-password"
      }
    },
    "apiMinReplicas": {
      "value": 2
    },
    "apiMaxReplicas": {
      "value": 10
    },
    "logAnalyticsRetentionDays": {
      "value": 90
    }
  }
}
```

Then deploy (Option A with Resource Group):

```bash
az deployment sub create \
  --location $LOCATION \
  --template-file main-with-rg.bicep \
  --parameters @parameters.prod.json
```

Or deploy (Option B to existing Resource Group):

```bash
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters @parameters.prod.json
```

#### 4. Customize Parameters

You can override any default parameters:

```bash
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters environment=prod \
  --parameters location=westus2 \
  --parameters appName=myapp \
  --parameters apiMinReplicas=3 \
  --parameters apiMaxReplicas=15 \
  --parameters mongoCpu=2.0 \
  --parameters mongoMemory=4.0 \
  --parameters mongoRootUsername="admin" \
  --parameters mongoRootPassword="YourSecurePassword123!"
```

## Template Parameters Reference

### Environment Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `environment` | string | `dev` | Environment name (dev, staging, prod) |
| `location` | string | Resource Group location | Azure region for all resources |
| `appName` | string | `tralivali` | Application name prefix |
| `tags` | object | See template | Tags applied to all resources |

### Storage Account Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `storageAccountName` | string | `{appName}{env}{uniqueString}` | Globally unique storage account name (3-24 chars, lowercase alphanumeric) |
| `enableLifecyclePolicies` | bool | `true` | Enable automatic blob tiering policies |

### Container Registry Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `containerRegistryName` | string | `{appName}{env}{uniqueString}` | Globally unique registry name (5-50 alphanumeric) |
| `containerRegistrySku` | string | `Basic` (dev), `Standard` (prod) | Registry SKU (Basic, Standard, Premium) |

### Container Apps Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `containerAppsEnvironmentName` | string | `{appName}-{env}-env` | Container Apps Environment name |
| `apiContainerAppName` | string | `{appName}-{env}-api` | API Container App name |
| `apiContainerImage` | string | `{registry}.azurecr.io/tralivali-api:latest` | API container image |
| `apiMinReplicas` | int | `1` (dev), `2` (prod) | Minimum API replicas (0-30) |
| `apiMaxReplicas` | int | `3` (dev), `10` (prod) | Maximum API replicas (1-30) |
| `apiCpu` | string | `0.5` | API CPU cores |
| `apiMemory` | string | `1.0Gi` | API memory allocation |

### MongoDB Container Instance Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `mongoContainerName` | string | `{appName}-{env}-mongodb` | MongoDB Container Instance name |
| `mongoContainerImage` | string | `mongo:latest` | MongoDB container image |
| `mongoRootUsername` | string (secure) | **REQUIRED** | MongoDB root username |
| `mongoRootPassword` | string (secure) | **REQUIRED** | MongoDB root password |
| `mongoDatabaseName` | string | `tralivali` | MongoDB database name |
| `mongoCpu` | string | `1.0` (dev), `2.0` (prod) | MongoDB CPU cores |
| `mongoMemory` | string | `2.0` (dev), `4.0` (prod) | MongoDB memory (GB) |

### Azure Communication Services Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `communicationServicesName` | string | `{appName}-{env}-acs` | Communication Services resource name |
| `communicationServicesDataLocation` | string | `United States` | Data location (United States, Europe, etc.) |

### Log Analytics Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `logAnalyticsWorkspaceName` | string | `{appName}-{env}-logs` | Log Analytics Workspace name |
| `logAnalyticsSku` | string | `PerGB2018` | Pricing tier |
| `logAnalyticsRetentionDays` | int | `30` (dev), `90` (prod) | Log retention period (30-730 days) |

## Outputs

After successful deployment, the template provides these outputs:

### Log Analytics
- `logAnalyticsWorkspaceId`: Workspace resource ID
- `logAnalyticsWorkspaceName`: Workspace name
- `logAnalyticsCustomerId`: Customer ID for log ingestion

### Container Registry
- `containerRegistryId`: Registry resource ID
- `containerRegistryName`: Registry name
- `containerRegistryLoginServer`: Registry login server URL

### Container Apps
- `containerAppsEnvironmentId`: Environment resource ID
- `containerAppsEnvironmentName`: Environment name
- `apiContainerAppId`: API app resource ID
- `apiContainerAppName`: API app name
- `apiContainerAppFqdn`: API public URL (HTTPS)

### MongoDB
- `mongoContainerInstanceId`: Container instance resource ID
- `mongoContainerInstanceName`: Container instance name
- `mongoContainerInstanceFqdn`: MongoDB FQDN
- `mongoContainerInstanceIpAddress`: MongoDB public IP

### Communication Services
- `communicationServicesId`: Communication Services resource ID
- `communicationServicesName`: Communication Services name

### Storage
- `storageAccountName`: Storage account name
- `storageAccountId`: Storage account resource ID
- `storageAccountPrimaryEndpoints`: Storage endpoints
- `archivedMessagesContainerName`: Archive container name
- `filesContainerName`: Files container name
- `lifecyclePoliciesDeployed`: Whether lifecycle policies are enabled

## Post-Deployment Steps

### 1. Get Deployment Outputs

```bash
# Get all outputs
az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs

# Get specific output (e.g., API URL)
az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.apiContainerAppFqdn.value \
  --output tsv
```

### 2. Push Docker Image to Container Registry

```bash
# Get registry credentials
REGISTRY_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.containerRegistryName.value \
  --output tsv)

REGISTRY_SERVER=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.containerRegistryLoginServer.value \
  --output tsv)

# Login to registry
az acr login --name $REGISTRY_NAME

# Tag and push your image
docker tag tralivali-api:latest $REGISTRY_SERVER/tralivali-api:latest
docker push $REGISTRY_SERVER/tralivali-api:latest
```

### 3. Update Container App with New Image

After pushing a new image, update the Container App:

```bash
# Update container app with new revision
az containerapp update \
  --name $(az deployment group show \
    --resource-group $RESOURCE_GROUP \
    --name main \
    --query properties.outputs.apiContainerAppName.value \
    --output tsv) \
  --resource-group $RESOURCE_GROUP \
  --image $REGISTRY_SERVER/tralivali-api:latest
```

### 4. Verify MongoDB Connection

```bash
# Get MongoDB FQDN
MONGO_FQDN=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.mongoContainerInstanceFqdn.value \
  --output tsv)

# Connect to MongoDB (requires mongosh installed locally)
mongosh "mongodb://admin:YourSecurePassword123!@$MONGO_FQDN:27017/tralivali?authSource=admin"
```

### 5. Get Communication Services Connection String

```bash
# Get Communication Services connection string
az communication list-key \
  --resource-group $RESOURCE_GROUP \
  --name $(az deployment group show \
    --resource-group $RESOURCE_GROUP \
    --name main \
    --query properties.outputs.communicationServicesName.value \
    --output tsv) \
  --query primaryConnectionString \
  --output tsv
```

### 6. Access API Application

```bash
# Get API URL
API_URL=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.apiContainerAppFqdn.value \
  --output tsv)

echo "API URL: https://$API_URL"

# Test API
curl https://$API_URL/health
```

### 7. Configure Custom Domain and SSL (Optional)

For production deployments, you should configure a custom domain with managed SSL certificates.

**📚 See the [SSL Configuration Guide](../../docs/SSL_CONFIGURATION.md)** for detailed instructions on:
- Configuring Azure Container Apps managed certificates
- DNS setup (A records, CNAME records, TXT validation)
- Automatic certificate provisioning and renewal
- HTTP to HTTPS redirection

Quick steps:
1. Add DNS records pointing to your Container App
2. Add custom domain to Container App
3. Enable managed certificate
4. Azure automatically provisions and renews Let's Encrypt certificates

See [Option 1: Azure Container Apps Managed Certificates](../../docs/SSL_CONFIGURATION.md#option-1-azure-container-apps-managed-certificates) in the SSL guide.

## Monitoring and Logs

### View Container App Logs

```bash
# Stream Container App logs
az containerapp logs show \
  --name $(az deployment group show \
    --resource-group $RESOURCE_GROUP \
    --name main \
    --query properties.outputs.apiContainerAppName.value \
    --output tsv) \
  --resource-group $RESOURCE_GROUP \
  --follow

# View logs in Log Analytics
az monitor log-analytics query \
  --workspace $(az deployment group show \
    --resource-group $RESOURCE_GROUP \
    --name main \
    --query properties.outputs.logAnalyticsCustomerId.value \
    --output tsv) \
  --analytics-query "ContainerAppConsoleLogs_CL | where TimeGenerated > ago(1h) | order by TimeGenerated desc"
```

### View MongoDB Logs

```bash
# Get container instance logs
az container logs \
  --resource-group $RESOURCE_GROUP \
  --name $(az deployment group show \
    --resource-group $RESOURCE_GROUP \
    --name main \
    --query properties.outputs.mongoContainerInstanceName.value \
    --output tsv)
```

## Deploy Only Lifecycle Policies (Optional)

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
