# CD Pipeline Quick Reference

## 📦 What Was Created

```
.github/workflows/deploy.yml    - CD workflow for Azure deployment
.github/CD_SETUP.md            - Complete setup guide
TASK55_COMPLETE.md             - Implementation summary
```

## 🚀 Quick Start

### 1. Configure Secrets (5 minutes)

Go to GitHub repo → Settings → Secrets and variables → Actions

Add these secrets:
```
AZURE_CREDENTIALS                    # Azure service principal JSON
AZURE_CONTAINER_REGISTRY_NAME        # e.g., tralivalidevxyz123
AZURE_RESOURCE_GROUP                 # e.g., tralivali-dev-rg
AZURE_CONTAINER_APP_NAME             # e.g., tralivali-dev-api
```

Get these from your Azure deployment:
```bash
# Get registry name
az acr list --resource-group tralivali-dev-rg --query "[0].name" -o tsv

# Get container app name
az containerapp list --resource-group tralivali-dev-rg --query "[0].name" -o tsv
```

### 2. Set Up Environments (2 minutes)

Go to GitHub repo → Settings → Environments

Create these environments:
- `development` (no restrictions)
- `staging` (optional)
- `production` (add required reviewers for manual approval)

### 3. Test the Workflow (1 minute)

Actions tab → "CD - Deploy to Azure" → Run workflow → Select "dev"

## 🔐 Security - IMPORTANT!

**Use OIDC (Recommended):**
```bash
# Create app registration
az ad app create --display-name "github-actions-tralivali"

# Configure federated credentials for GitHub
# See .github/CD_SETUP.md for complete OIDC setup
```

**Or use Service Principal (Legacy):**
```bash
az ad sp create-for-rbac \
  --name "github-actions-tralivali" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUB_ID \
  --sdk-auth
```

## 🎯 How It Works

**Push to main** → Automatically deploys to development
**Manual trigger** → Choose any environment (dev/staging/prod)
**Production** → Requires manual approval (if configured)

## 📊 Workflow Jobs

1. **build-and-push**: Builds Docker image → Pushes to ACR
2. **deploy-dev**: Updates Container App → Verifies health
3. **deploy-staging**: Manual only → Verifies health
4. **deploy-production**: Manual + approval → Verifies health

## 🔍 Monitoring

```bash
# Check deployment status
az containerapp show \
  --name tralivali-dev-api \
  --resource-group tralivali-dev-rg \
  --query properties.provisioningState

# View logs
az containerapp logs show \
  --name tralivali-dev-api \
  --resource-group tralivali-dev-rg \
  --follow
```

## 🎨 Customization

Edit `.github/workflows/deploy.yml`:

```yaml
env:
  HEALTH_CHECK_ENDPOINT: /weatherforecast  # Change this to your health endpoint
```

## 📚 Full Documentation

- Setup guide: `.github/CD_SETUP.md`
- Completion summary: `TASK55_COMPLETE.md`
- Azure infrastructure: `deploy/azure/README.md`

## ✅ All Acceptance Criteria Met

- ✓ CD workflow created
- ✓ Docker image built and pushed
- ✓ Azure Container Apps deployment
- ✓ Secrets configured (documented)
- ✓ Manual approval for production

## 🆘 Troubleshooting

**Authentication error?**
- Check `AZURE_CREDENTIALS` is valid JSON
- Verify service principal has Contributor role

**Image not found?**
- Check registry name in secrets
- Verify Container App can pull from ACR

**Deployment timeout?**
- Check Container App logs
- Verify health check endpoint exists

**Need help?** See `.github/CD_SETUP.md` for detailed troubleshooting.
