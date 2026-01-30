# CD Pipeline Setup Guide

This guide explains how to configure the GitHub Actions CD pipeline for deploying TraliVali to Azure Container Apps.

## Overview

The CD pipeline automatically:
1. Builds a Docker image from the source code
2. Pushes the image to Azure Container Registry
3. Deploys the image to Azure Container Apps
4. Verifies the deployment

## Triggers

- **Automatic**: Deploys to `development` on every push to `main` branch
- **Manual**: Deploy to any environment via workflow dispatch in GitHub Actions UI

## Environments

The pipeline supports three environments:
- **development**: Auto-deploys on push to main
- **staging**: Manual deployment only
- **production**: Manual deployment with required approval

## Required GitHub Secrets

Configure the following secrets in your GitHub repository (`Settings` → `Secrets and variables` → `Actions`):

### Global Secrets

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `AZURE_CREDENTIALS` | Azure service principal credentials in JSON format | See below |
| `AZURE_CONTAINER_REGISTRY_NAME` | Name of your Azure Container Registry (without .azurecr.io) | `tralivalidevxyz123` |

### Development Environment Secrets

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `AZURE_RESOURCE_GROUP` | Resource group name for dev environment | `tralivali-dev-rg` |
| `AZURE_CONTAINER_APP_NAME` | Container App name for dev environment | `tralivali-dev-api` |

### Staging Environment Secrets (Optional)

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `AZURE_RESOURCE_GROUP_STAGING` | Resource group name for staging environment | `tralivali-staging-rg` |
| `AZURE_CONTAINER_APP_NAME_STAGING` | Container App name for staging environment | `tralivali-staging-api` |

### Production Environment Secrets

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `AZURE_RESOURCE_GROUP_PROD` | Resource group name for production environment | `tralivali-prod-rg` |
| `AZURE_CONTAINER_APP_NAME_PROD` | Container App name for production environment | `tralivali-prod-api` |

## Creating Azure Service Principal

To create the `AZURE_CREDENTIALS` secret, you have two options:

### Option 1: OpenID Connect (OIDC) - Recommended

OIDC provides better security with short-lived tokens and no stored secrets. This is the recommended approach for GitHub Actions.

#### Steps for OIDC Setup:

1. **Create an Azure App Registration:**

```bash
# Set your subscription ID
SUBSCRIPTION_ID="your-subscription-id"

# Create the app registration
az ad app create --display-name "github-actions-tralivali"
```

2. **Create a service principal:**

```bash
# Get the app ID from the previous step
APP_ID=$(az ad app list --display-name "github-actions-tralivali" --query "[0].appId" -o tsv)

# Create service principal
az ad sp create --id $APP_ID

# Assign Contributor role
az role assignment create \
  --assignee $APP_ID \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID
```

3. **Configure federated credentials:**

```bash
# Get the object ID of the service principal
OBJECT_ID=$(az ad sp show --id $APP_ID --query id -o tsv)

# Create federated credential for the main branch
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-actions-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_ORG/TralliValli:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Also create for workflow_dispatch if needed
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-actions-workflow",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_ORG/TralliValli:environment:production",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

4. **Configure GitHub repository:**

Add these secrets to your GitHub repository:
- `AZURE_CLIENT_ID`: The Application (client) ID from the app registration
- `AZURE_TENANT_ID`: Your Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID`: Your subscription ID

5. **Update the workflow file** to use OIDC authentication:

Replace the login step with:
```yaml
- name: Log in to Azure
  uses: azure/login@v1
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

### Option 2: Service Principal with Secret (Legacy)

> **Note:** This method uses the `--sdk-auth` flag which is deprecated and will be removed in future Azure CLI versions. Use Option 1 (OIDC) for new deployments.

```bash
# Set your subscription ID
SUBSCRIPTION_ID="your-subscription-id"

# Create a service principal with Contributor role
az ad sp create-for-rbac \
  --name "github-actions-tralivali" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID \
  --sdk-auth
```

This command will output JSON credentials. Copy the entire JSON output and add it as the `AZURE_CREDENTIALS` secret in GitHub.

The JSON format should look like:
```json
{
  "clientId": "xxx",
  "clientSecret": "xxx",
  "subscriptionId": "xxx",
  "tenantId": "xxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

### Scoped Service Principal (More Secure)

For better security, create separate service principals for each environment with limited scope:

#### Using OIDC (Recommended):

```bash
# For development environment
APP_ID=$(az ad app create --display-name "github-actions-tralivali-dev" --query appId -o tsv)
az ad sp create --id $APP_ID

az role assignment create \
  --assignee $APP_ID \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/tralivali-dev-rg

# Create federated credential
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-dev-env",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_ORG/TralliValli:environment:development",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

#### Using Service Principal with Secret (Legacy):

> **Note:** The `--sdk-auth` flag is deprecated. Use OIDC method above for new deployments.

```bash
# For development environment
az ad sp create-for-rbac \
  --name "github-actions-tralivali-dev" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/tralivali-dev-rg \
  --sdk-auth

# For production environment
az ad sp create-for-rbac \
  --name "github-actions-tralivali-prod" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/tralivali-prod-rg \
  --sdk-auth
```

## Getting Azure Resource Information

If you've already deployed the infrastructure using the Bicep templates, retrieve the resource names:

```bash
# Set your resource group name
RESOURCE_GROUP="tralivali-dev-rg"

# Get Container Registry name
az acr list --resource-group $RESOURCE_GROUP --query "[0].name" -o tsv

# Get Container App name
az containerapp list --resource-group $RESOURCE_GROUP --query "[0].name" -o tsv
```

## Configuring Manual Approval for Production

To add manual approval for production deployments:

1. Go to your repository on GitHub
2. Navigate to `Settings` → `Environments`
3. Click on the `production` environment (create it if it doesn't exist)
4. Enable "Required reviewers"
5. Add the GitHub users who should approve production deployments
6. (Optional) Set a wait timer before deployment can proceed

When a production deployment is triggered, the workflow will pause and wait for approval from the designated reviewers.

## Usage

### Automatic Deployment to Development

Simply push to the `main` branch:

```bash
git push origin main
```

The pipeline will automatically build and deploy to the development environment.

### Manual Deployment

1. Go to the "Actions" tab in your GitHub repository
2. Click on "CD - Deploy to Azure" workflow
3. Click "Run workflow"
4. Select the environment (dev, staging, or prod)
5. Click "Run workflow" button

For production deployments, the workflow will wait for manual approval before proceeding.

## Workflow Jobs

### 1. build-and-push
- Checks out the code
- Logs in to Azure and ACR
- Builds Docker image from `src/TraliVali.Api/Dockerfile`
- Tags image with both commit SHA and `latest`
- Pushes both tags to Azure Container Registry

### 2. deploy-dev
- Runs only for pushes to main or manual deployment to dev
- Logs in to Azure
- Updates the Container App with the new image
- Verifies the deployment
- Outputs the application URL

### 3. deploy-staging (Optional)
- Runs only for manual deployment to staging
- Same steps as deploy-dev but uses staging secrets

### 4. deploy-production
- Runs only for manual deployment to production
- Requires manual approval (if configured)
- Same steps as deploy-dev but uses production secrets

## Monitoring Deployments

### View Workflow Runs
1. Go to the "Actions" tab in your repository
2. Click on a workflow run to see details
3. Click on individual jobs to see logs

### Check Application Health
After deployment, check the application:

```bash
# Get the application URL
az containerapp show \
  --name tralivali-dev-api \
  --resource-group tralivali-dev-rg \
  --query properties.configuration.ingress.fqdn \
  --output tsv

# Test the endpoint
curl https://your-app-url.azurecontainerapps.io/weatherforecast
```

### View Container App Logs
```bash
# Stream logs
az containerapp logs show \
  --name tralivali-dev-api \
  --resource-group tralivali-dev-rg \
  --follow

# View recent logs
az containerapp logs show \
  --name tralivali-dev-api \
  --resource-group tralivali-dev-rg \
  --tail 100
```

## Troubleshooting

### Authentication Errors

If you see authentication errors:
1. Verify `AZURE_CREDENTIALS` secret is correctly formatted JSON
2. Ensure the service principal has Contributor role on the resource group
3. Check if the service principal has expired credentials

### Image Not Found

If the container app can't find the image:
1. Verify the Container Registry name matches the secret
2. Check that the image was successfully pushed
3. Ensure the Container App has access to pull from ACR (managed identity or admin credentials)

### Deployment Timeout

If deployment times out:
1. Check Azure Container App logs for startup errors
2. Verify application configuration and environment variables
3. Check if health check endpoint is accessible

### Manual Workflow Not Showing Environments

If manual deployments don't show environment options:
1. Ensure you have created the environments in repository settings
2. Check that environment names match exactly: `development`, `staging`, `production`

## Best Practices

1. **Use separate environments**: Keep development, staging, and production resources completely separate
2. **Enable approval for production**: Always require manual approval for production deployments
3. **Tag images with commit SHA**: The pipeline tags images with both SHA and `latest` for traceability
4. **Monitor deployments**: Set up alerts in Azure Monitor for deployment failures
5. **Test in staging first**: Always test in staging before deploying to production
6. **Keep secrets secure**: Never commit secrets to the repository
7. **Rotate credentials regularly**: Update service principal credentials periodically

## Related Documentation

- [Azure Deployment Templates](../deploy/azure/README.md)
- [Dockerfile](../src/TraliVali.Api/Dockerfile)
- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
