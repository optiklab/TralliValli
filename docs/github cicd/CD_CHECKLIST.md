# CD Pipeline Configuration Checklist

Use this checklist to configure and verify your CD pipeline setup.

## ☐ Prerequisites

- [ ] Azure subscription with appropriate permissions
- [ ] Azure Container Registry deployed
- [ ] Azure Container Apps deployed (dev, staging, prod)
- [ ] Azure CLI installed locally (for setup)
- [ ] GitHub repository admin access

## ☐ Step 1: Azure Service Principal Setup

Choose one authentication method:

### Option A: OIDC (Recommended) ⭐

- [ ] Create Azure App Registration
- [ ] Create Service Principal
- [ ] Assign Contributor role to subscription or resource groups
- [ ] Configure federated credentials for GitHub
- [ ] Note down: Client ID, Tenant ID, Subscription ID

### Option B: Service Principal with Secret (Legacy)

- [ ] Run `az ad sp create-for-rbac` with `--sdk-auth`
- [ ] Copy the JSON output
- [ ] Store securely (you'll add to GitHub next)

**Reference:** See `.github/CD_SETUP.md` → "Creating Azure Service Principal"

## ☐ Step 2: GitHub Secrets Configuration

Go to: Repository → Settings → Secrets and variables → Actions

### Global Secrets (Required)

- [ ] `AZURE_CREDENTIALS` 
  - Value: Service principal JSON (if using Option B)
  - OR skip if using OIDC
- [ ] `AZURE_CONTAINER_REGISTRY_NAME`
  - Value: Registry name without .azurecr.io (e.g., `tralivalidevxyz`)

### Development Environment (Required)

- [ ] `AZURE_RESOURCE_GROUP`
  - Value: Resource group name (e.g., `tralivali-dev-rg`)
- [ ] `AZURE_CONTAINER_APP_NAME`
  - Value: Container app name (e.g., `tralivali-dev-api`)

### Staging Environment (Optional)

- [ ] `AZURE_RESOURCE_GROUP_STAGING`
- [ ] `AZURE_CONTAINER_APP_NAME_STAGING`

### Production Environment (If deploying to prod)

- [ ] `AZURE_RESOURCE_GROUP_PROD`
- [ ] `AZURE_CONTAINER_APP_NAME_PROD`

### OIDC Secrets (If using OIDC instead of AZURE_CREDENTIALS)

- [ ] `AZURE_CLIENT_ID`
- [ ] `AZURE_TENANT_ID`
- [ ] `AZURE_SUBSCRIPTION_ID`

**Note:** If using OIDC, update workflow to use client-id, tenant-id, subscription-id instead of creds

## ☐ Step 3: GitHub Environments Setup

Go to: Repository → Settings → Environments

### Create Environments

- [ ] Create `development` environment
  - No restrictions needed
  - Used for automatic deployments from main
  
- [ ] Create `staging` environment (optional)
  - Add wait timer if desired
  - Used for manual pre-production testing
  
- [ ] Create `production` environment
  - ⚠️ **Enable "Required reviewers"**
  - Add GitHub usernames who can approve deployments
  - Optionally add wait timer (e.g., 5 minutes)

## ☐ Step 4: Get Azure Resource Information

Run these commands to find your resource names:

```bash
# Set your resource group
RESOURCE_GROUP="tralivali-dev-rg"

# Get Container Registry name
az acr list --resource-group $RESOURCE_GROUP --query "[0].name" -o tsv

# Get Container App name
az containerapp list --resource-group $RESOURCE_GROUP --query "[0].name" -o tsv

# Get resource group name (if you don't know it)
az group list --query "[?contains(name, 'tralivali')].name" -o tsv
```

- [ ] Registry name: ________________
- [ ] Dev resource group: ________________
- [ ] Dev container app: ________________
- [ ] Staging resource group (if applicable): ________________
- [ ] Staging container app (if applicable): ________________
- [ ] Prod resource group: ________________
- [ ] Prod container app: ________________

## ☐ Step 5: Verify Azure Container Registry Access

Ensure Container Apps can pull from ACR:

```bash
# Option 1: Using managed identity (recommended)
az containerapp update \
  --name <container-app-name> \
  --resource-group <resource-group> \
  --registry-server <registry-name>.azurecr.io

# Option 2: Using admin credentials (simpler, less secure)
# Admin credentials should already be enabled in ACR
```

- [ ] Verified Container App can pull from ACR

## ☐ Step 6: Test the Workflow

### Manual Test (Safest)

1. Go to: Actions → "CD - Deploy to Azure"
2. Click "Run workflow"
3. Select environment: **dev**
4. Click "Run workflow" button
5. Monitor the workflow execution

- [ ] Workflow started successfully
- [ ] Build job completed
- [ ] Docker image pushed to ACR
- [ ] Deployment job completed
- [ ] Deployment verification passed
- [ ] Application URL accessible

### Automatic Test

1. Make a small change (e.g., update README.md)
2. Commit and push to `main` branch
3. Workflow should trigger automatically

- [ ] Automatic deployment triggered on push to main
- [ ] Development deployment successful

## ☐ Step 7: Test Production Deployment (Optional)

1. Go to: Actions → "CD - Deploy to Azure"
2. Click "Run workflow"
3. Select environment: **prod**
4. Click "Run workflow" button
5. Workflow should pause for approval
6. Approve the deployment (if you're a reviewer)
7. Deployment should proceed

- [ ] Production deployment requires approval
- [ ] Approval process works correctly
- [ ] Production deployment successful

## ☐ Step 8: Verify Application

```bash
# Get application URL
az containerapp show \
  --name <container-app-name> \
  --resource-group <resource-group> \
  --query properties.configuration.ingress.fqdn \
  --output tsv

# Test endpoint
curl https://<your-app-url>/weatherforecast
```

- [ ] Application is accessible via HTTPS
- [ ] Health check endpoint responds correctly
- [ ] Application functions as expected

## ☐ Step 9: Configure Optional Settings

### Custom Health Check Endpoint

If your app has a dedicated health endpoint:

1. Edit `.github/workflows/deploy.yml`
2. Change `HEALTH_CHECK_ENDPOINT: /weatherforecast` to your endpoint
3. Commit and push

- [ ] Health check endpoint configured (if needed)

### Adjust Retry Logic

If deployments need more/less time:

1. Edit `.github/workflows/deploy.yml`
2. Modify `MAX_RETRIES` and sleep duration in verification steps
3. Commit and push

- [ ] Retry logic adjusted (if needed)

## ☐ Step 10: Documentation and Handoff

- [ ] Share `.github/CD_QUICK_START.md` with team
- [ ] Document any custom configurations
- [ ] Add deployment runbook to team wiki
- [ ] Train team members on approval process
- [ ] Set up alerts for failed deployments (Azure Monitor)

## 🎯 Success Criteria

All of these should be true:

- ✅ Secrets are configured in GitHub
- ✅ Environments are set up with proper protections
- ✅ Manual deployment to dev works
- ✅ Automatic deployment from main works
- ✅ Production requires and receives approval
- ✅ Application is accessible after deployment
- ✅ Health checks pass
- ✅ Team knows how to use the pipeline

## 📞 Need Help?

- **Setup Guide**: `.github/CD_SETUP.md` - Complete configuration guide
- **Quick Start**: `.github/CD_QUICK_START.md` - Fast reference
- **Summary**: `TASK55_COMPLETE.md` - Implementation details
- **Azure Docs**: `deploy/azure/README.md` - Infrastructure setup

## 🐛 Troubleshooting

**Deployment fails with authentication error:**
- Check `AZURE_CREDENTIALS` format
- Verify service principal has correct permissions
- Try regenerating credentials

**Image not found error:**
- Verify registry name is correct
- Check Container App has pull permissions
- Confirm image was pushed successfully

**Health check fails:**
- Check application logs in Azure
- Verify endpoint exists and is accessible
- Adjust `HEALTH_CHECK_ENDPOINT` if needed

**Production won't deploy:**
- Verify `production` environment exists
- Check if reviewers are configured
- Ensure reviewer has approved the deployment

---

**Status**: [ ] Complete - Pipeline is ready for production use!
