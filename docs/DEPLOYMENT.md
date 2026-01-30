# TraliVali Deployment Guide

A comprehensive guide for deploying TraliVali to production using Azure or Docker Compose.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Deployment Options](#deployment-options)
  - [Option 1: Azure Deployment (Recommended)](#option-1-azure-deployment-recommended)
  - [Option 2: Docker Compose Deployment](#option-2-docker-compose-deployment)
- [Post-Deployment Configuration](#post-deployment-configuration)
- [First Admin User Creation](#first-admin-user-creation)
- [Verifying Deployment](#verifying-deployment)
- [Monitoring and Maintenance](#monitoring-and-maintenance)
- [Troubleshooting](#troubleshooting)

---

## Overview

TraliVali is a self-hosted, invite-only messaging platform with end-to-end encryption. This guide covers two deployment methods:

1. **Azure Deployment** - Production-ready cloud deployment using Azure Container Apps, managed MongoDB, and automatic scaling
2. **Docker Compose** - Self-hosted deployment for on-premises or VM-based hosting

**Estimated Deployment Time:**
- Azure: 30-45 minutes (first time)
- Docker Compose: 20-30 minutes

---

## Prerequisites

### General Requirements

- [x] Domain name (e.g., `yourdomain.com` or `api.yourdomain.com`)
- [x] Email address for SSL certificates and notifications
- [x] Strong passwords for all services (minimum 20 characters)

### Azure Deployment Requirements

- [x] Azure subscription with contributor permissions
- [x] Azure CLI installed ([Install Guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- [x] Docker installed for building images ([Install Guide](https://docs.docker.com/get-docker/))
- [x] Git for cloning repository
- [x] Access to domain DNS settings

**Azure Permissions Required:**
- Create Resource Groups
- Create Container Apps and Environments
- Create Container Instances
- Create Container Registry
- Create Storage Accounts
- Create Log Analytics Workspaces
- Create Communication Services

### Docker Compose Requirements

- [x] Linux server (Ubuntu 20.04+ recommended) or macOS
- [x] Minimum 4 CPU cores, 8GB RAM, 50GB SSD
- [x] Docker Engine 20.10+ installed
- [x] Docker Compose v2.0+ installed
- [x] Ports 80 and 443 accessible from internet
- [x] Git for cloning repository

---

## Deployment Options

## Option 1: Azure Deployment (Recommended)

Azure deployment provides a production-ready, scalable infrastructure with automatic SSL, monitoring, and high availability.

### Step 1: Azure Account Setup (5 minutes)

1. **Create Azure Account** (if you don't have one):
   - Visit [Azure Portal](https://portal.azure.com/)
   - Click "Create a free account"
   - Follow registration steps
   - Provide credit card for verification (free tier available)

2. **Install Azure CLI**:

   ```bash
   # macOS
   brew install azure-cli
   
   # Ubuntu/Debian
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   
   # Windows (PowerShell)
   Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi
   Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
   ```

3. **Verify Installation**:

   ```bash
   az --version
   # Should show: azure-cli 2.x.x or higher
   ```

### Step 2: Subscription Selection (2 minutes)

1. **Login to Azure**:

   ```bash
   az login
   ```
   
   This opens a browser window for authentication.

2. **List Available Subscriptions**:

   ```bash
   az account list --output table
   ```
   
   **Example Output:**
   ```
   Name                  SubscriptionId                        State    IsDefault
   --------------------  ------------------------------------  -------  -----------
   Pay-As-You-Go         12345678-1234-1234-1234-123456789012  Enabled  True
   Free Trial            87654321-4321-4321-4321-210987654321  Enabled  False
   ```

3. **Select Subscription** (if you have multiple):

   ```bash
   az account set --subscription "12345678-1234-1234-1234-123456789012"
   ```

4. **Verify Active Subscription**:

   ```bash
   az account show --output table
   ```

### Step 3: Resource Group Creation (3 minutes)

Resource groups organize all your Azure resources in one logical container.

1. **Set Variables**:

   ```bash
   # Configuration
   RESOURCE_GROUP="tralivali-prod-rg"
   LOCATION="eastus"  # Options: eastus, westus2, westeurope, etc.
   ENVIRONMENT="prod"  # Options: dev, staging, prod
   APP_NAME="tralivali"
   
   # Generate strong passwords (SAVE THESE SECURELY!)
   MONGO_PASSWORD=$(openssl rand -base64 32)
   echo "MongoDB Password: $MONGO_PASSWORD"
   ```

   **⚠️ IMPORTANT:** Save the MongoDB password securely. You'll need it later.

2. **Create Resource Group**:

   ```bash
   az group create \
     --name $RESOURCE_GROUP \
     --location $LOCATION \
     --tags Environment=$ENVIRONMENT Application=TraliVali
   ```

   **Expected Output:**
   ```json
   {
     "id": "/subscriptions/.../resourceGroups/tralivali-prod-rg",
     "location": "eastus",
     "name": "tralivali-prod-rg",
     "properties": {
       "provisioningState": "Succeeded"
     }
   }
   ```

### Step 4: Running Bicep Deployment (15-20 minutes)

Bicep templates provision all Azure infrastructure automatically.

1. **Clone Repository**:

   ```bash
   git clone https://github.com/optiklab/TralliValli.git
   cd TralliValli/deploy/azure
   ```

2. **Review Available Templates**:

   - `main.bicep` - Main infrastructure template (requires existing resource group)
   - `main-with-rg.bicep` - Creates resource group + infrastructure (subscription-level)

3. **Deploy Infrastructure**:

   **Option A: Deploy to existing resource group**

   ```bash
   az deployment group create \
     --resource-group $RESOURCE_GROUP \
     --template-file main.bicep \
     --parameters environment=$ENVIRONMENT \
     --parameters location=$LOCATION \
     --parameters appName=$APP_NAME \
     --parameters mongoRootUsername="admin" \
     --parameters mongoRootPassword="$MONGO_PASSWORD"
   ```

   **Option B: Create everything at subscription level**

   ```bash
   az deployment sub create \
     --location $LOCATION \
     --template-file main-with-rg.bicep \
     --parameters environment=$ENVIRONMENT \
     --parameters location=$LOCATION \
     --parameters appName=$APP_NAME \
     --parameters mongoRootUsername="admin" \
     --parameters mongoRootPassword="$MONGO_PASSWORD"
   ```

4. **Monitor Deployment Progress**:

   The deployment takes 15-20 minutes. You can monitor progress in Azure Portal:
   - Go to [Azure Portal](https://portal.azure.com/)
   - Navigate to your Resource Group
   - Click "Deployments" in left menu
   - Watch deployment status

   **Resources Being Created:**
   - ✅ Log Analytics Workspace (for monitoring)
   - ✅ Container Registry (for Docker images)
   - ✅ Storage Account (for files and archived messages)
   - ✅ File Share (for MongoDB data persistence)
   - ✅ Container Apps Environment
   - ✅ API Container App
   - ✅ MongoDB Container Instance
   - ✅ Azure Communication Services

5. **Capture Deployment Outputs**:

   ```bash
   # Get all outputs
   az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main \
     --query properties.outputs
   
   # Get specific outputs
   API_FQDN=$(az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main \
     --query properties.outputs.apiContainerAppFqdn.value \
     --output tsv)
   
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
   
   echo "API URL: https://$API_FQDN"
   echo "Registry: $REGISTRY_SERVER"
   ```

### Step 5: Build and Push Docker Image (10 minutes)

1. **Build Docker Image**:

   ```bash
   cd ../../  # Back to repository root
   
   docker build -t tralivali-api:latest \
     -f src/TraliVali.Api/Dockerfile .
   ```

2. **Login to Azure Container Registry**:

   ```bash
   az acr login --name $REGISTRY_NAME
   ```

3. **Tag and Push Image**:

   ```bash
   docker tag tralivali-api:latest $REGISTRY_SERVER/tralivali-api:latest
   docker push $REGISTRY_SERVER/tralivali-api:latest
   ```

4. **Update Container App**:

   ```bash
   API_APP_NAME=$(az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main \
     --query properties.outputs.apiContainerAppName.value \
     --output tsv)
   
   az containerapp update \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --image $REGISTRY_SERVER/tralivali-api:latest
   ```

### Step 6: DNS Configuration (10 minutes)

Configure your custom domain to point to the deployed application.

1. **Get Container App FQDN**:

   ```bash
   echo "Container App FQDN: $API_FQDN"
   ```

2. **Get Custom Domain Verification ID**:

   ```bash
   VERIFICATION_ID=$(az containerapp show \
     --resource-group $RESOURCE_GROUP \
     --name $API_APP_NAME \
     --query properties.customDomainVerificationId \
     --output tsv)
   
   echo "Verification ID: $VERIFICATION_ID"
   ```

3. **Configure DNS Records**:

   Login to your domain registrar (GoDaddy, Namecheap, Cloudflare, etc.) and add these records:

   **For subdomain (e.g., api.yourdomain.com):**

   | Type  | Name      | Value                  | TTL  |
   |-------|-----------|------------------------|------|
   | CNAME | `api`     | `$API_FQDN`           | 3600 |
   | TXT   | `asuid.api` | `$VERIFICATION_ID`  | 3600 |

   **Example:**
   ```
   Type: CNAME
   Name: api
   Value: tralivali-prod-api.nicebeach-12345678.eastus.azurecontainerapps.io
   TTL: 3600
   
   Type: TXT
   Name: asuid.api
   Value: 1234567890ABCDEF1234567890ABCDEF
   TTL: 3600
   ```

   **For root domain (yourdomain.com):**

   First get the static IP:
   ```bash
   ENV_NAME=$(az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main \
     --query properties.outputs.containerAppsEnvironmentName.value \
     --output tsv)
   
   STATIC_IP=$(az containerapp env show \
     --resource-group $RESOURCE_GROUP \
     --name $ENV_NAME \
     --query properties.staticIp \
     --output tsv)
   
   echo "Static IP: $STATIC_IP"
   ```

   | Type | Name  | Value         | TTL  |
   |------|-------|---------------|------|
   | A    | `@`   | `$STATIC_IP`  | 3600 |
   | TXT  | `asuid` | `$VERIFICATION_ID` | 3600 |

4. **Verify DNS Propagation** (wait 5-10 minutes):

   ```bash
   dig api.yourdomain.com
   # Should show your CNAME record
   
   nslookup api.yourdomain.com
   # Should resolve to Container App FQDN
   ```

### Step 7: SSL Certificate Setup (5 minutes)

Azure Container Apps provides free, automatic SSL certificates via Let's Encrypt.

1. **Add Custom Domain**:

   ```bash
   CUSTOM_DOMAIN="api.yourdomain.com"  # Change to your domain
   
   az containerapp hostname add \
     --resource-group $RESOURCE_GROUP \
     --name $API_APP_NAME \
     --hostname $CUSTOM_DOMAIN
   ```

2. **Bind SSL Certificate**:

   ```bash
   az containerapp hostname bind \
     --resource-group $RESOURCE_GROUP \
     --name $API_APP_NAME \
     --hostname $CUSTOM_DOMAIN \
     --environment $ENV_NAME \
     --validation-method CNAME
   ```

   This command automatically:
   - ✅ Validates domain ownership
   - ✅ Requests Let's Encrypt certificate
   - ✅ Installs certificate
   - ✅ Configures HTTPS
   - ✅ Sets up automatic renewal (every 45 days)

3. **Enable HTTPS-Only**:

   ```bash
   az containerapp ingress update \
     --resource-group $RESOURCE_GROUP \
     --name $API_APP_NAME \
     --allow-insecure false
   ```

4. **Verify SSL Certificate**:

   ```bash
   curl -I https://api.yourdomain.com/weatherforecast
   
   echo | openssl s_client -servername api.yourdomain.com \
     -connect api.yourdomain.com:443 2>/dev/null | \
     openssl x509 -noout -dates
   ```

### Step 8: Environment Variables Configuration (5 minutes)

Configure application settings in Container App.

1. **Get Connection Strings**:

   ```bash
   # MongoDB Connection String
   MONGO_FQDN=$(az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main \
     --query properties.outputs.mongoContainerInstanceFqdn.value \
     --output tsv)
   
   MONGODB_CONNECTION="mongodb://admin:$MONGO_PASSWORD@$MONGO_FQDN:27017/tralivali?authSource=admin"
   
   # Storage Account Connection String
   STORAGE_ACCOUNT=$(az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main \
     --query properties.outputs.storageAccountName.value \
     --output tsv)
   
   STORAGE_CONNECTION=$(az storage account show-connection-string \
     --name $STORAGE_ACCOUNT \
     --resource-group $RESOURCE_GROUP \
     --output tsv)
   
   # Communication Services Connection String
   ACS_NAME=$(az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main \
     --query properties.outputs.communicationServicesName.value \
     --output tsv)
   
   ACS_CONNECTION=$(az communication list-key \
     --resource-group $RESOURCE_GROUP \
     --name $ACS_NAME \
     --query primaryConnectionString \
     --output tsv)
   ```

2. **Generate JWT Keys**:

   ```bash
   # Generate RSA key pair for JWT authentication
   openssl genrsa -out private.pem 2048
   openssl rsa -in private.pem -outform PEM -pubout -out public.pem
   
   # Read keys (include BEGIN/END lines)
   JWT_PRIVATE_KEY=$(cat private.pem | awk '{printf "%s\\n", $0}' | sed '$s/\\n$//')
   JWT_PUBLIC_KEY=$(cat public.pem | awk '{printf "%s\\n", $0}' | sed '$s/\\n$//')
   
   # Generate invite signing key
   INVITE_SIGNING_KEY=$(openssl rand -base64 32)
   
   # ⚠️ IMPORTANT: Save these values securely BEFORE deleting key files!
   echo "JWT_PRIVATE_KEY=$JWT_PRIVATE_KEY"
   echo "JWT_PUBLIC_KEY=$JWT_PUBLIC_KEY"
   echo "INVITE_SIGNING_KEY=$INVITE_SIGNING_KEY"
   echo ""
   echo "Copy these values to a secure location (password manager, Azure Key Vault, etc.)"
   echo "Press Enter after you have securely saved these values..."
   read
   
   # Clean up key files only after values are saved
   rm private.pem public.pem
   ```

3. **Update Container App Environment Variables**:

   ```bash
   az containerapp update \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --set-env-vars \
       "MONGODB_CONNECTION_STRING=$MONGODB_CONNECTION" \
       "AZURE_BLOB_STORAGE_CONNECTION_STRING=$STORAGE_CONNECTION" \
       "AZURE_COMMUNICATION_EMAIL_CONNECTION_STRING=$ACS_CONNECTION" \
       "JWT_PRIVATE_KEY=$JWT_PRIVATE_KEY" \
       "JWT_PUBLIC_KEY=$JWT_PUBLIC_KEY" \
       "JWT_ISSUER=TraliVali" \
       "JWT_AUDIENCE=TraliVali" \
       "JWT_EXPIRATION_DAYS=7" \
       "INVITE_SIGNING_KEY=$INVITE_SIGNING_KEY" \
       "ASPNETCORE_ENVIRONMENT=Production"
   ```

   **Note:** Container App automatically restarts after updating environment variables.

### Step 9: MongoDB Initialization (5 minutes)

Verify MongoDB is running and accessible.

1. **Check MongoDB Container Status**:

   ```bash
   MONGO_CONTAINER=$(az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main \
     --query properties.outputs.mongoContainerInstanceName.value \
     --output tsv)
   
   az container show \
     --resource-group $RESOURCE_GROUP \
     --name $MONGO_CONTAINER \
     --query instanceView.state \
     --output tsv
   # Should show: Running
   ```

2. **View MongoDB Logs**:

   ```bash
   az container logs \
     --resource-group $RESOURCE_GROUP \
     --name $MONGO_CONTAINER \
     --tail 50
   ```

   Look for successful startup messages:
   ```
   Waiting for connections on port 27017
   ```

3. **Test MongoDB Connection** (requires mongosh installed locally):

   ```bash
   mongosh "$MONGODB_CONNECTION"
   
   # Inside mongosh:
   show dbs
   use tralivali
   db.version()
   exit
   ```

   **Install mongosh:**
   ```bash
   # macOS
   brew install mongosh
   
   # Ubuntu/Debian
   wget -qO - https://www.mongodb.org/static/pgp/server-6.0.asc | sudo apt-key add -
   echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu focal/mongodb-org/6.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-6.0.list
   sudo apt-get update
   sudo apt-get install -y mongodb-mongosh
   ```

### Step 10: First Admin User Creation (10 minutes)

TraliVali uses an invite-only system. Here's how to create the first admin user.

**Understanding the Flow:**

1. First user must be created directly in MongoDB (bootstrap process)
2. This user becomes the admin
3. Admin can then generate invites for other users
4. New users register using invite tokens

**Steps:**

1. **Connect to MongoDB**:

   ```bash
   mongosh "$MONGODB_CONNECTION"
   ```

2. **Create First Admin User**:

   ```javascript
   use tralivali
   
   // Create the first admin user
   db.users.insertOne({
     "_id": ObjectId(),
     "email": "admin@yourdomain.com",  // Change to your email
     "displayName": "System Administrator",
     "passwordHash": "N/A",  // Passwordless auth via magic links
     "role": "admin",
     "isActive": true,
     "createdAt": new Date(),
     "updatedAt": new Date()
   })
   
   // Verify creation
   db.users.findOne({ "email": "admin@yourdomain.com" })
   
   exit
   ```

3. **Test Admin Login**:

   ```bash
   # Request magic link
   curl -X POST https://api.yourdomain.com/auth/request-magic-link \
     -H "Content-Type: application/json" \
     -d '{"email": "admin@yourdomain.com"}'
   ```

   **Expected Response:**
   ```json
   {
     "message": "If the email exists in our system, a magic link has been sent."
   }
   ```

   **⚠️ Note:** If email is not configured (Azure Communication Services), check application logs for the magic link token:

   ```bash
   az containerapp logs show \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --follow
   ```

   Look for a log entry like:
   ```
   Magic link token: abc123def456...
   ```

4. **Authenticate with Magic Link**:

   ```bash
   # Use the token from email or logs
   MAGIC_TOKEN="abc123def456..."  # Replace with actual token
   
   curl -X POST https://api.yourdomain.com/auth/verify-magic-link \
     -H "Content-Type: application/json" \
     -d "{\"token\": \"$MAGIC_TOKEN\"}"
   ```

   **Expected Response:**
   ```json
   {
     "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
     "refreshToken": "def456ghi789...",
     "expiresIn": 604800
   }
   ```

5. **Save Access Token**:

   ```bash
   ACCESS_TOKEN="eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
   ```

6. **Generate Invite for First Regular User**:

   ```bash
   curl -X POST https://api.yourdomain.com/invites/generate \
     -H "Authorization: Bearer $ACCESS_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"expiryHours": 168}'  # 7 days
   ```

   **Expected Response:**
   ```json
   {
     "inviteLink": "https://app.yourdomain.com/register?token=xyz789abc123...",
     "qrCode": "iVBORw0KGgoAAAANSUhEUgAA...",
     "expiresAt": "2026-02-06T12:00:00Z"
   }
   ```

**✅ Admin user is now created and can invite other users!**

---

## Option 2: Docker Compose Deployment

Self-hosted deployment using Docker Compose on your own server or VM.

### Step 1: Server Preparation (10 minutes)

1. **Provision Server**:
   - Minimum: 4 CPU cores, 8GB RAM, 50GB SSD
   - Recommended: 8 CPU cores, 16GB RAM, 100GB SSD
   - OS: Ubuntu 20.04+ or similar Linux distribution

2. **Update System**:

   ```bash
   sudo apt update && sudo apt upgrade -y
   ```

3. **Install Docker**:

   ```bash
   # Install Docker
   curl -fsSL https://get.docker.com -o get-docker.sh
   sudo sh get-docker.sh
   
   # Add current user to docker group
   sudo usermod -aG docker $USER
   newgrp docker
   
   # Verify installation
   docker --version
   docker compose version
   ```

4. **Configure Firewall**:

   ```bash
   # Ubuntu/Debian
   sudo ufw allow 22/tcp    # SSH
   sudo ufw allow 80/tcp    # HTTP
   sudo ufw allow 443/tcp   # HTTPS
   sudo ufw enable
   
   # Verify
   sudo ufw status
   ```

### Step 2: DNS Configuration (5 minutes)

Configure DNS to point to your server.

1. **Get Server IP**:

   ```bash
   curl ifconfig.me
   # Or
   ip addr show
   ```

2. **Add DNS A Record**:

   Login to your domain registrar and add:

   | Type | Name | Value              | TTL  |
   |------|------|--------------------|------|
   | A    | `api` | `YOUR_SERVER_IP`  | 3600 |

3. **Verify DNS**:

   ```bash
   dig +short api.yourdomain.com
   # Should return your server IP
   ```

### Step 3: Clone Repository and Configure (5 minutes)

1. **Clone Repository**:

   ```bash
   cd /opt
   sudo git clone https://github.com/optiklab/TralliValli.git
   sudo chown -R $USER:$USER TralliValli
   cd TralliValli/deploy
   ```

2. **Create Environment File**:

   ```bash
   cp .env.prod.example .env.prod
   ```

3. **Edit Environment Variables**:

   ```bash
   nano .env.prod
   ```

   **Required Changes:**

   ```bash
   # MongoDB - Change password
   MONGO_ROOT_PASSWORD=YOUR_STRONG_MONGO_PASSWORD_HERE
   
   # RabbitMQ - Change password
   RABBITMQ_PASSWORD=YOUR_STRONG_RABBITMQ_PASSWORD_HERE
   
   # Redis - Change password
   REDIS_PASSWORD=YOUR_STRONG_REDIS_PASSWORD_HERE
   
   # JWT Keys - Generate with commands below
   JWT_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----"
   JWT_PUBLIC_KEY="-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
   
   # Invite signing key
   INVITE_SIGNING_KEY=YOUR_32_CHAR_RANDOM_STRING_HERE
   ```

4. **Generate JWT Keys**:

   ```bash
   # Generate RSA key pair
   openssl genrsa -out private.pem 2048
   openssl rsa -in private.pem -outform PEM -pubout -out public.pem
   
   # View private key (copy to .env.prod)
   cat private.pem
   
   # View public key (copy to .env.prod)
   cat public.pem
   
   # Clean up
   rm private.pem public.pem
   ```

   **Note:** The keys should be stored with literal `\n` characters (not actual newlines) in the `.env.prod` file. The application will parse these correctly. Example format:
   ```bash
   JWT_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBg...\n-----END PRIVATE KEY-----"
   ```

5. **Generate Invite Signing Key**:

   ```bash
   openssl rand -base64 32
   # Copy output to INVITE_SIGNING_KEY in .env.prod
   ```

### Step 4: SSL Certificate Setup (10 minutes)

We'll use Caddy for automatic SSL with Let's Encrypt.

1. **Update Caddyfile**:

   ```bash
   nano Caddyfile
   ```

   Replace `api.example.com` with your domain:

   ```caddy
   {
       email your-email@yourdomain.com
       admin off
   }
   
   api.yourdomain.com {
       reverse_proxy api:8080 {
           health_uri /weatherforecast
           health_interval 30s
           health_timeout 10s
       }
   
       header {
           X-Frame-Options "SAMEORIGIN"
           X-Content-Type-Options "nosniff"
           Referrer-Policy "no-referrer-when-downgrade"
           Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"
           -Server
       }
   
       request_body {
           max_size 100MB
       }
   
       log {
           output file /var/log/caddy/access.log
           format json
       }
   }
   ```

### Step 5: Start Services (5 minutes)

1. **Start All Services**:

   ```bash
   docker compose -f docker-compose.caddy.yml --env-file .env.prod up -d
   ```

2. **Watch Startup Logs**:

   ```bash
   docker compose -f docker-compose.caddy.yml logs -f
   ```

   Look for:
   ```
   caddy  | obtaining certificate for api.yourdomain.com
   caddy  | certificate obtained successfully
   api    | Now listening on: http://0.0.0.0:8080
   mongodb| Waiting for connections on port 27017
   ```

3. **Verify Services**:

   ```bash
   docker compose -f docker-compose.caddy.yml ps
   ```

   All services should show "healthy" status:
   ```
   NAME                    STATUS              PORTS
   tralivali-api           Up (healthy)        
   tralivali-caddy         Up (healthy)        0.0.0.0:80->80/tcp, 0.0.0.0:443->443/tcp
   tralivali-mongodb       Up (healthy)        
   tralivali-rabbitmq      Up (healthy)        
   tralivali-redis         Up (healthy)        
   ```

### Step 6: MongoDB Initialization (5 minutes)

1. **Connect to MongoDB Container**:

   ```bash
   docker exec -it tralivali-mongodb mongosh \
     -u admin \
     -p YOUR_STRONG_MONGO_PASSWORD_HERE \
     --authenticationDatabase admin \
     tralivali
   ```

2. **Verify Database**:

   ```javascript
   show dbs
   use tralivali
   db.version()
   exit
   ```

### Step 7: First Admin User Creation (10 minutes)

Same process as Azure deployment (see Step 10 in Azure section), but connect to MongoDB locally:

```bash
docker exec -it tralivali-mongodb mongosh \
  -u admin \
  -p YOUR_STRONG_MONGO_PASSWORD_HERE \
  --authenticationDatabase admin \
  tralivali
```

Then follow the same user creation steps from Azure deployment.

---

## Post-Deployment Configuration

### Configure Email Notifications (Optional)

If you want to send magic link emails instead of logging tokens:

**For Azure:**

1. **Set up Azure Communication Services Email**:
   - Go to Azure Portal
   - Navigate to Communication Services resource
   - Enable Email service
   - Configure sending domain

2. **Update Container App Environment**:

   ```bash
   az containerapp update \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --set-env-vars \
       "EMAIL_SENDER_ADDRESS=noreply@yourdomain.com" \
       "EMAIL_SENDER_NAME=TraliVali"
   ```

**For Docker Compose:**

Update `.env.prod`:
```bash
AZURE_COMMUNICATION_EMAIL_CONNECTION_STRING=endpoint=https://...
EMAIL_SENDER_ADDRESS=noreply@yourdomain.com
EMAIL_SENDER_NAME=TraliVali
```

Restart services:
```bash
docker compose -f docker-compose.caddy.yml restart api
```

### Configure Blob Storage for Backups (Optional)

**For Azure:**

Blob storage is automatically configured during deployment.

**For Docker Compose:**

1. Create Azure Storage Account for backups
2. Get connection string
3. Update `.env.prod`:

```bash
AZURE_BLOB_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;...
BACKUP_CONTAINER_NAME=tralivali-backups
```

---

## Verifying Deployment

### Health Checks

1. **Check API Health**:

   ```bash
   # Azure
   curl -I https://api.yourdomain.com/weatherforecast
   
   # Docker Compose
   curl -I https://api.yourdomain.com/weatherforecast
   ```

   **Expected Response:**
   ```
   HTTP/2 200
   content-type: application/json; charset=utf-8
   ```

2. **Check SSL Certificate**:

   ```bash
   echo | openssl s_client -servername api.yourdomain.com \
     -connect api.yourdomain.com:443 2>/dev/null | \
     openssl x509 -noout -dates -issuer
   ```

   **Expected Output:**
   ```
   notBefore=Jan 30 12:00:00 2026 GMT
   notAfter=Apr 30 12:00:00 2026 GMT
   issuer=C = US, O = Let's Encrypt, CN = R3
   ```

3. **Test Authentication Flow**:

   ```bash
   # Request magic link
   curl -X POST https://api.yourdomain.com/auth/request-magic-link \
     -H "Content-Type: application/json" \
     -d '{"email": "admin@yourdomain.com"}'
   
   # Check logs for token (if email not configured)
   # Azure
   az containerapp logs show \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --tail 50
   
   # Docker Compose
   docker compose -f docker-compose.caddy.yml logs api --tail 50
   ```

4. **Test Database Connection**:

   ```bash
   # Azure
   mongosh "$MONGODB_CONNECTION" --eval "db.version()"
   
   # Docker Compose
   docker exec tralivali-mongodb mongosh \
     -u admin -p YOUR_PASSWORD \
     --authenticationDatabase admin \
     --eval "db.version()"
   ```

### Functional Tests

1. **Create Test Invite** (requires admin access token):

   ```bash
   curl -X POST https://api.yourdomain.com/invites/generate \
     -H "Authorization: Bearer $ACCESS_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"expiryHours": 168}'
   ```

2. **Register Test User** (use invite token from above):

   ```bash
   curl -X POST https://api.yourdomain.com/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@yourdomain.com",
       "displayName": "Test User",
       "inviteToken": "YOUR_INVITE_TOKEN"
     }'
   ```

3. **Create Test Conversation**:

   ```bash
   curl -X POST https://api.yourdomain.com/conversations \
     -H "Authorization: Bearer $ACCESS_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "Test Conversation",
       "participantIds": ["USER_ID_1", "USER_ID_2"]
     }'
   ```

---

## Monitoring and Maintenance

### Azure Monitoring

1. **View Application Logs**:

   ```bash
   # Stream live logs
   az containerapp logs show \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --follow
   
   # View recent logs
   az containerapp logs show \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --tail 100
   ```

2. **Check Resource Usage**:

   ```bash
   # Container App metrics (example for last 24 hours)
   START_TIME=$(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%SZ)
   END_TIME=$(date -u +%Y-%m-%dT%H:%M:%SZ)
   
   az monitor metrics list \
     --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.App/containerApps/$API_APP_NAME" \
     --metric "Requests" \
     --start-time $START_TIME \
     --end-time $END_TIME
   ```

3. **View MongoDB Logs**:

   ```bash
   az container logs \
     --resource-group $RESOURCE_GROUP \
     --name $MONGO_CONTAINER \
     --tail 100
   ```

4. **Set Up Alerts** (via Azure Portal):
   - Go to Resource Group → Alerts
   - Create alert rules for:
     - High CPU usage (> 80%)
     - High memory usage (> 80%)
     - Failed requests (> 5%)
     - Response time (> 2 seconds)

### Docker Compose Monitoring

1. **View Logs**:

   ```bash
   # All services
   docker compose -f docker-compose.caddy.yml logs -f
   
   # Specific service
   docker compose -f docker-compose.caddy.yml logs -f api
   docker compose -f docker-compose.caddy.yml logs -f mongodb
   docker compose -f docker-compose.caddy.yml logs -f caddy
   ```

2. **Check Resource Usage**:

   ```bash
   # Real-time stats
   docker stats
   
   # Service status
   docker compose -f docker-compose.caddy.yml ps
   ```

3. **Check Disk Usage**:

   ```bash
   # Docker disk usage
   docker system df -v
   
   # Volume usage
   docker volume ls
   df -h
   ```

### Backup Procedures

**MongoDB Backup:**

```bash
# Azure (using mongodump)
mongosh "$MONGODB_CONNECTION" --eval "use admin; db.runCommand({dbStats: 1})"
# Then use mongodump (install MongoDB Database Tools)
mongodump --uri="$MONGODB_CONNECTION" --out=./backups/$(date +%Y%m%d)

# Docker Compose
docker exec tralivali-mongodb mongodump \
  -u admin -p YOUR_STRONG_MONGO_PASSWORD_HERE \
  --authenticationDatabase admin \
  --out /data/backup/$(date +%Y%m%d)

# Copy backup to host
docker cp tralivali-mongodb:/data/backup ./backups/
```

**Automated Backup Script:**

Create the backup script at `/opt/tralivali-backup.sh`:

```bash
#!/bin/bash
# MongoDB Backup Script for TraliVali
# Save this file as: /opt/tralivali-backup.sh

BACKUP_DIR="/opt/backups/tralivali"
DATE=$(date +%Y%m%d-%H%M%S)

# Load MongoDB password from environment or set it here
# Replace with your actual MongoDB password
MONGO_PASSWORD="${MONGO_ROOT_PASSWORD:-YOUR_STRONG_MONGO_PASSWORD_HERE}"

mkdir -p $BACKUP_DIR

# Backup MongoDB
docker exec tralivali-mongodb mongodump \
  -u admin -p "$MONGO_PASSWORD" \
  --authenticationDatabase admin \
  --gzip \
  --archive=/data/backup/mongodb-$DATE.gz

docker cp tralivali-mongodb:/data/backup/mongodb-$DATE.gz $BACKUP_DIR/

# Keep only last 30 days
find $BACKUP_DIR -name "mongodb-*.gz" -mtime +30 -delete

echo "Backup completed: $BACKUP_DIR/mongodb-$DATE.gz"
```

Make executable and add to crontab:
```bash
chmod +x /opt/tralivali-backup.sh

# Edit crontab
crontab -e

# Add this line (runs daily at 2 AM):
# 0 2 * * * /opt/tralivali-backup.sh >> /var/log/tralivali-backup.log 2>&1
```

### Certificate Renewal

**Azure:**
- Automatic renewal by Azure (no action needed)
- Certificates renew 45 days before expiration

**Docker Compose with Caddy:**
- Automatic renewal by Caddy (no action needed)
- Certificates renew 30 days before expiration
- Check renewal logs: `docker compose -f docker-compose.caddy.yml logs caddy | grep renew`

### Updates and Upgrades

**Azure Deployment:**

```bash
# Pull latest code
cd /path/to/TralliValli
git pull origin main

# Rebuild and push image
docker build -t tralivali-api:latest -f src/TraliVali.Api/Dockerfile .
docker tag tralivali-api:latest $REGISTRY_SERVER/tralivali-api:latest
docker push $REGISTRY_SERVER/tralivali-api:latest

# Update Container App (creates new revision)
az containerapp update \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --image $REGISTRY_SERVER/tralivali-api:latest
```

**Docker Compose:**

```bash
cd /opt/TralliValli
git pull origin main

# Rebuild and restart
docker compose -f deploy/docker-compose.caddy.yml build --no-cache api
docker compose -f deploy/docker-compose.caddy.yml up -d --no-deps api

# Verify
docker compose -f deploy/docker-compose.caddy.yml ps
```

---

## Troubleshooting

### Common Issues

#### Issue 1: DNS Not Resolving

**Symptoms:**
- Domain doesn't resolve to server
- SSL certificate fails to provision

**Diagnosis:**
```bash
dig api.yourdomain.com
nslookup api.yourdomain.com
```

**Solutions:**

1. **Wait for DNS propagation** (5-30 minutes)
2. **Verify DNS records** in domain registrar
3. **Check DNS syntax:**
   - CNAME value should NOT have trailing dot unless required by registrar
   - TXT record should be exact verification ID
4. **Clear DNS cache:**
   ```bash
   # macOS
   sudo dscacheutil -flushcache
   
   # Ubuntu/Linux
   sudo systemd-resolve --flush-caches
   
   # Windows
   ipconfig /flushdns
   ```

#### Issue 2: SSL Certificate Provisioning Fails

**Symptoms:**
- HTTPS not working
- Certificate errors in browser
- "Unable to obtain certificate" in logs

**Diagnosis:**

```bash
# Azure
az containerapp hostname list \
  --resource-group $RESOURCE_GROUP \
  --name $API_APP_NAME \
  --output table

# Docker Compose (Caddy)
docker compose -f docker-compose.caddy.yml logs caddy | grep -i certificate
```

**Solutions:**

1. **Verify DNS is resolving correctly**
2. **Check port 80 is accessible:**
   ```bash
   telnet api.yourdomain.com 80
   ```
3. **Let's Encrypt rate limits:**
   - Limit: 5 failed validations per hour
   - Solution: Wait 1 hour, fix issues, try again
4. **Firewall blocking HTTP challenge:**
   - Ensure port 80 is open for Let's Encrypt validation
5. **Domain verification failed:**
   - Verify TXT record is correct
   - Wait longer for DNS propagation

#### Issue 3: Container App Not Starting

**Symptoms:**
- Application not accessible
- Container in failed or pending state

**Diagnosis:**

```bash
# Check container status
az containerapp show \
  --resource-group $RESOURCE_GROUP \
  --name $API_APP_NAME \
  --query properties.runningStatus

# View logs
az containerapp logs show \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --tail 100
```

**Solutions:**

1. **Check environment variables:**
   ```bash
   az containerapp show \
     --resource-group $RESOURCE_GROUP \
     --name $API_APP_NAME \
     --query properties.template.containers[0].env
   ```

2. **Verify image exists in registry:**
   ```bash
   az acr repository show-tags \
     --name $REGISTRY_NAME \
     --repository tralivali-api
   ```

3. **Check MongoDB connectivity:**
   ```bash
   az container logs \
     --resource-group $RESOURCE_GROUP \
     --name $MONGO_CONTAINER
   ```

4. **Resource limits:**
   - Increase CPU/memory if needed
   - Check quota limits in subscription

#### Issue 4: MongoDB Connection Failures

**Symptoms:**
- API cannot connect to MongoDB
- "Connection refused" or "timeout" errors

**Diagnosis:**

```bash
# Azure - Check MongoDB container
az container show \
  --resource-group $RESOURCE_GROUP \
  --name $MONGO_CONTAINER \
  --query instanceView.state

# Docker Compose
docker exec tralivali-mongodb mongosh \
  -u admin -p PASSWORD \
  --authenticationDatabase admin \
  --eval "db.serverStatus()"
```

**Solutions:**

1. **Verify MongoDB is running:**
   ```bash
   # Azure
   az container logs \
     --resource-group $RESOURCE_GROUP \
     --name $MONGO_CONTAINER
   
   # Docker Compose
   docker compose -f docker-compose.caddy.yml logs mongodb
   ```

2. **Check connection string format:**
   ```
   Correct: mongodb://username:password@host:27017/database?authSource=admin
   ```

3. **Verify credentials:**
   - Username and password match deployment parameters
   - Password special characters are URL-encoded

4. **Network connectivity:**
   ```bash
   # Docker Compose
   docker exec tralivali-api ping mongodb
   ```

5. **Storage issues:**
   - MongoDB volume may be full
   - Check disk space: `df -h`

#### Issue 5: Magic Link Not Received

**Symptoms:**
- User requests magic link but doesn't receive email
- No error message shown

**Diagnosis:**

```bash
# Check application logs for magic link token
# Azure
az containerapp logs show \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --tail 50 | grep -i "magic"

# Docker Compose
docker compose -f docker-compose.caddy.yml logs api | grep -i "magic"
```

**Solutions:**

1. **Email not configured:**
   - If Azure Communication Services is not set up, magic link token will be logged
   - Find token in logs and use it manually

2. **Configure email service:**
   ```bash
   # Azure - Update environment variables
   az containerapp update \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --set-env-vars \
       "AZURE_COMMUNICATION_EMAIL_CONNECTION_STRING=..." \
       "EMAIL_SENDER_ADDRESS=noreply@yourdomain.com"
   ```

3. **Check spam folder**

4. **Verify email configuration:**
   - Sender domain is verified
   - Email service quota not exceeded

#### Issue 6: High Memory Usage

**Symptoms:**
- Container restarting frequently
- Out of memory errors in logs

**Diagnosis:**

```bash
# Azure
az monitor metrics list \
  --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.App/containerApps/$API_APP_NAME" \
  --metric "MemoryPercentage"

# Docker Compose
docker stats
```

**Solutions:**

1. **Increase memory limits:**

   ```bash
   # Azure - Update container resources
   az containerapp update \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --cpu 1.0 \
     --memory 2.0Gi
   ```

   For Docker Compose, edit `docker-compose.caddy.yml`:
   ```yaml
   api:
     deploy:
       resources:
         limits:
           memory: 4GB  # Increase from 2GB
   ```

2. **Check for memory leaks:**
   - Review application logs
   - Monitor memory over time

3. **Optimize application:**
   - Review large object allocations
   - Implement caching strategies

#### Issue 7: File Upload Failures

**Symptoms:**
- File uploads timeout or fail
- "Request entity too large" errors

**Diagnosis:**

```bash
# Check upload limits in Caddyfile
cat deploy/Caddyfile | grep max_size

# Check nginx configuration
cat deploy/nginx.conf | grep client_max_body_size
```

**Solutions:**

1. **Increase upload limits in Caddyfile:**
   ```caddy
   request_body {
       max_size 100MB  # Increase as needed
   }
   ```

2. **For nginx, update nginx.conf:**
   ```nginx
   client_max_body_size 100M;
   ```

3. **Restart reverse proxy:**
   ```bash
   docker compose -f docker-compose.caddy.yml restart caddy
   ```

### Getting Help

**Check Logs First:**

```bash
# Azure - All services
az containerapp logs show --name $API_APP_NAME --resource-group $RESOURCE_GROUP --tail 200
az container logs --resource-group $RESOURCE_GROUP --name $MONGO_CONTAINER --tail 200

# Docker Compose - All services
docker compose -f docker-compose.caddy.yml logs --tail 200
```

**Collect Diagnostic Information:**

```bash
# System info
uname -a
docker --version
docker compose version

# Service status
docker ps -a
docker compose -f docker-compose.caddy.yml ps

# Resource usage
docker stats --no-stream
df -h

# Network info
docker network ls
docker network inspect tralivali-prod-network
```

**Community Support:**

- GitHub Issues: https://github.com/optiklab/TralliValli/issues
- Check existing documentation in `/docs` directory

**Security Issues:**

- Report privately to repository maintainers
- DO NOT post security issues publicly

---

## Additional Resources

### Documentation

- [Azure Deployment Templates](../deploy/azure/README.md)
- [SSL Configuration Guide](SSL_CONFIGURATION.md)
- [Domain Setup Guide](DOMAIN_SETUP_GUIDE.md)
- [Docker Compose Setup](docker-compose-setup.md)
- [Backup Worker Configuration](BACKUP_WORKER_CONFIGURATION.md)
- [Azure Blob Storage Configuration](AZURE_BLOB_STORAGE_CONFIGURATION.md)
- [E2E Testing Guide](E2E_TESTING.md)

### External Resources

- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [MongoDB Documentation](https://www.mongodb.com/docs/)
- [Let's Encrypt Documentation](https://letsencrypt.org/docs/)
- [Caddy Documentation](https://caddyserver.com/docs/)

---

## Security Best Practices

### Credentials Management

- ✅ Use strong passwords (minimum 20 characters, mix of uppercase, lowercase, numbers, symbols)
- ✅ Store credentials securely (Azure Key Vault, password manager)
- ✅ Never commit credentials to version control
- ✅ Rotate passwords regularly (every 90 days)
- ✅ Use different passwords for each service

### Network Security

- ✅ Enable HTTPS only (disable HTTP)
- ✅ Use firewall to restrict access
- ✅ Keep services internal (no public access to MongoDB, Redis, RabbitMQ)
- ✅ Use VPN for administrative access
- ✅ Enable network segmentation

### Application Security

- ✅ Keep software updated
- ✅ Enable audit logging
- ✅ Monitor for suspicious activity
- ✅ Implement rate limiting
- ✅ Regular security audits
- ✅ Backup data regularly

### Compliance

- ✅ Data encryption at rest and in transit
- ✅ Regular backups with retention policy
- ✅ Access control and authentication
- ✅ Audit trails for compliance
- ✅ Privacy policy and terms of service

---

## Conclusion

You have successfully deployed TraliVali! 🎉

**What's Next:**

1. ✅ Create your admin user
2. ✅ Generate invites for your team
3. ✅ Configure email notifications (optional)
4. ✅ Set up monitoring and alerts
5. ✅ Configure regular backups
6. ✅ Test all functionality
7. ✅ Deploy frontend application

**Production Checklist:**

- [ ] All passwords changed from defaults
- [ ] SSL certificates configured and working
- [ ] DNS configured correctly
- [ ] Admin user created
- [ ] Email service configured (optional)
- [ ] Backups scheduled
- [ ] Monitoring and alerts configured
- [ ] Security audit completed
- [ ] Documentation reviewed
- [ ] Team onboarded

**Need Help?**

- Review troubleshooting section above
- Check application logs
- Consult additional documentation
- Create GitHub issue for bugs or feature requests

---

**Document Version:** 1.0  
**Last Updated:** 2024-01-30  
**Maintained By:** TraliVali Development Team
