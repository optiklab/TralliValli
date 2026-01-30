# Domain Setup Quick Start Guide

This guide provides step-by-step instructions for configuring a custom domain with SSL/TLS for TraliVali.

## Choose Your Deployment Method

### Option 1: Azure Container Apps (Recommended for Azure)
**Best for:** Azure deployments, zero maintenance  
**Time:** 15-30 minutes  
**Complexity:** ⭐ Easy

[Jump to Azure setup](#option-1-azure-container-apps-setup)

### Option 2: Caddy (Recommended for Self-Hosted)
**Best for:** Self-hosted, automatic HTTPS  
**Time:** 15-20 minutes  
**Complexity:** ⭐⭐ Easy

[Jump to Caddy setup](#option-2-caddy-automatic-https-setup)

### Option 3: Nginx with Certbot
**Best for:** Traditional deployments, existing Nginx users  
**Time:** 20-30 minutes  
**Complexity:** ⭐⭐⭐ Moderate

[Jump to Nginx + Certbot setup](#option-3-nginx-with-certbot-setup)

---

## Option 1: Azure Container Apps Setup

### Prerequisites
- Azure subscription
- TraliVali deployed to Azure Container Apps
- Domain name (e.g., yourdomain.com)
- Access to domain DNS settings

### Step 1: Prepare Your Domain (5 minutes)

1. **Choose your subdomain:**
   - Example: `api.yourdomain.com` (recommended)
   - Or use root: `yourdomain.com`

2. **Get Container App information:**
   ```bash
   RESOURCE_GROUP="tralivali-rg"
   APP_NAME="tralivali-dev-api"
   ENVIRONMENT_NAME="tralivali-dev-env"
   
   # Get Container App FQDN
   az containerapp show \
     --resource-group $RESOURCE_GROUP \
     --name $APP_NAME \
     --query properties.configuration.ingress.fqdn \
     --output tsv
   ```
   
   Output example: `tralivali-dev-api.nicebeach-12345678.eastus.azurecontainerapps.io`

3. **Get verification ID:**
   ```bash
   az containerapp show \
     --resource-group $RESOURCE_GROUP \
     --name $APP_NAME \
     --query properties.customDomainVerificationId \
     --output tsv
   ```
   
   Save this ID - you'll need it for DNS.

### Step 2: Configure DNS (10 minutes)

**Log in to your domain registrar** (GoDaddy, Namecheap, Cloudflare, etc.)

#### For subdomain (api.yourdomain.com):

Add these DNS records:

| Type | Name | Value | TTL |
|------|------|-------|-----|
| CNAME | `api` | `<your-container-app-fqdn>` | 3600 |
| TXT | `asuid.api` | `<verification-id-from-step-1>` | 3600 |

**Example:**
```
Type: CNAME
Name: api
Value: tralivali-dev-api.nicebeach-12345678.eastus.azurecontainerapps.io
TTL: 3600

Type: TXT
Name: asuid.api
Value: 1234567890ABCDEF1234567890ABCDEF
TTL: 3600
```

#### For root domain (yourdomain.com):

First, get static IP:
```bash
az containerapp env show \
  --resource-group $RESOURCE_GROUP \
  --name $ENVIRONMENT_NAME \
  --query properties.staticIp \
  --output tsv
```

Add these DNS records:

| Type | Name | Value | TTL |
|------|------|-------|-----|
| A | `@` | `<static-ip>` | 3600 |
| TXT | `asuid` | `<verification-id>` | 3600 |

**Wait 5-10 minutes for DNS propagation**, then verify:
```bash
dig api.yourdomain.com
# Should show your CNAME or A record
```

### Step 3: Add Domain to Container App (5 minutes)

```bash
CUSTOM_DOMAIN="api.yourdomain.com"

# Add custom domain
az containerapp hostname add \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --hostname $CUSTOM_DOMAIN
```

### Step 4: Enable Managed SSL Certificate (5 minutes)

```bash
# Bind certificate - Azure automatically provisions from Let's Encrypt
az containerapp hostname bind \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --hostname $CUSTOM_DOMAIN \
  --environment $ENVIRONMENT_NAME \
  --validation-method CNAME
```

This command:
- ✅ Validates domain ownership
- ✅ Requests certificate from Let's Encrypt
- ✅ Installs certificate
- ✅ Configures HTTPS
- ✅ Sets up automatic renewal

### Step 5: Enable HTTPS Redirect (2 minutes)

```bash
# Disable insecure HTTP access
az containerapp ingress update \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --allow-insecure false
```

### Step 6: Verify (3 minutes)

```bash
# Check certificate status
az containerapp hostname list \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --output table

# Test HTTPS endpoint
curl -I https://api.yourdomain.com/health

# Test SSL certificate
echo | openssl s_client -servername api.yourdomain.com -connect api.yourdomain.com:443 2>/dev/null | openssl x509 -noout -dates
```

### ✅ Done!

Your API is now accessible at `https://api.yourdomain.com` with:
- Valid SSL certificate from Let's Encrypt
- Automatic certificate renewal (every 45 days before expiration)
- HTTP to HTTPS redirection
- Zero maintenance required

**Next steps:**
- Update your frontend to use the new domain
- Test all API endpoints
- Monitor certificate renewal (automatic)

---

## Option 2: Caddy Automatic HTTPS Setup

### Prerequisites
- Server with Docker and Docker Compose
- Domain name (e.g., yourdomain.com)
- Ports 80 and 443 open on server
- TraliVali source code

### Step 1: Configure DNS (5 minutes)

**Log in to your domain registrar** and add an A record:

| Type | Name | Value | TTL |
|------|------|-------|-----|
| A | `api` | `<your-server-ip>` | 3600 |

**Example:**
```
Type: A
Name: api
Value: 203.0.113.45
TTL: 3600
```

**Verify DNS propagation:**
```bash
dig +short api.yourdomain.com
# Should return your server IP
```

### Step 2: Update Caddyfile (3 minutes)

Edit `deploy/Caddyfile`:

```caddy
{
    email your-email@yourdomain.com  # Change this!
    admin off
}

api.yourdomain.com {  # Change this to your domain!
    reverse_proxy api:8080 {
        health_uri /weatherforecast
        health_interval 30s
        health_timeout 10s
    }

    header {
        X-Frame-Options "SAMEORIGIN"
        X-Content-Type-Options "nosniff"
        Referrer-Policy "no-referrer-when-downgrade"
        Content-Security-Policy "default-src 'self'; script-src 'self'; object-src 'none'; frame-ancestors 'self';"
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

### Step 3: Configure Environment (2 minutes)

```bash
cd deploy

# Copy environment file
cp .env.prod.example .env.prod

# Edit with your settings
nano .env.prod
```

Set all required variables (MongoDB password, JWT keys, etc.)

### Step 4: Start Services (5 minutes)

```bash
# Start all services with Caddy
docker compose -f docker-compose.caddy.yml --env-file .env.prod up -d

# Watch Caddy logs to see certificate provisioning
docker compose -f docker-compose.caddy.yml logs -f caddy
```

You should see logs like:
```
caddy  | obtaining certificate for api.yourdomain.com
caddy  | authorization finalized
caddy  | certificate obtained successfully
```

This takes 30-60 seconds.

### Step 5: Verify (2 minutes)

```bash
# Test HTTPS
curl -I https://api.yourdomain.com/health

# Check certificate
echo | openssl s_client -servername api.yourdomain.com -connect api.yourdomain.com:443 2>/dev/null | openssl x509 -noout -dates

# View in browser
open https://api.yourdomain.com/weatherforecast
```

### ✅ Done!

Your API is now running with:
- Automatic HTTPS via Let's Encrypt
- HTTP/2 and HTTP/3 support
- Automatic certificate renewal (30 days before expiration)
- Zero maintenance required

**Certificate location:** Stored in Docker volume `caddy_data`

**Renewal:** Fully automatic, no cron jobs needed

---

## Option 3: Nginx with Certbot Setup

### Prerequisites
- Server with Docker and Docker Compose
- Domain name
- Ports 80 and 443 open
- TraliVali source code

### Step 1: Configure DNS (5 minutes)

Add an A record:

| Type | Name | Value | TTL |
|------|------|-------|-----|
| A | `api` | `<your-server-ip>` | 3600 |

**Verify:**
```bash
dig +short api.yourdomain.com
```

### Step 2: Start Services in HTTP Mode (3 minutes)

```bash
cd deploy

# Copy and configure environment
cp .env.prod.example .env.prod
nano .env.prod

# Start services (HTTP only initially)
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d

# Verify services are running
docker compose -f docker-compose.prod.yml ps
```

### Step 3: Obtain SSL Certificate (5 minutes)

```bash
# Create directories for certbot
mkdir -p ssl/certbot/conf
mkdir -p ssl/certbot/www

# Run certbot to obtain certificate
docker run --rm \
  -v $(pwd)/ssl/certbot/conf:/etc/letsencrypt \
  -v $(pwd)/ssl/certbot/www:/var/www/certbot \
  -p 80:80 \
  certbot/certbot certonly \
  --standalone \
  --email your-email@yourdomain.com \
  --agree-tos \
  --no-eff-email \
  --force-renewal \
  -d api.yourdomain.com
```

**Note:** This temporarily uses port 80, so nginx must be stopped:
```bash
docker compose -f docker-compose.prod.yml stop nginx
# Run certbot command above
docker compose -f docker-compose.prod.yml start nginx
```

### Step 4: Update Nginx Configuration (5 minutes)

Edit `deploy/nginx.conf` and:
1. Uncomment the HTTPS server block
2. Change `api.yourdomain.com` to your actual domain
3. Uncomment HTTP to HTTPS redirect

Or use the pre-configured SSL version in the documentation.

### Step 5: Update Docker Compose (3 minutes)

Edit `deploy/docker-compose.prod.yml`, update nginx service:

```yaml
  nginx:
    image: nginx:1.25-alpine
    container_name: tralivali-nginx-prod
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"  # Uncomment this line
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - ./ssl/certbot/conf:/etc/letsencrypt:ro  # Add this line
      - ./ssl/certbot/www:/var/www/certbot:ro   # Add this line
```

### Step 6: Restart Nginx (2 minutes)

```bash
# Test configuration
docker exec tralivali-nginx-prod nginx -t

# Restart nginx with new configuration
docker compose -f docker-compose.prod.yml restart nginx

# Check logs
docker compose -f docker-compose.prod.yml logs nginx
```

### Step 7: Set Up Automatic Renewal (5 minutes)

Create renewal script `ssl/renew-cert.sh`:

```bash
#!/bin/bash
cd /path/to/deploy

# Renew certificate
docker run --rm \
  -v $(pwd)/ssl/certbot/conf:/etc/letsencrypt \
  -v $(pwd)/ssl/certbot/www:/var/www/certbot \
  certbot/certbot renew --quiet

# Reload nginx if renewal succeeded
if [ $? -eq 0 ]; then
    docker compose -f docker-compose.prod.yml restart nginx
    echo "Certificate renewed and nginx restarted"
fi
```

Make executable and add to cron:
```bash
chmod +x ssl/renew-cert.sh

# Edit crontab
crontab -e

# Add this line (runs daily at 2 AM)
0 2 * * * /path/to/deploy/ssl/renew-cert.sh >> /var/log/certbot-renew.log 2>&1
```

### Step 8: Verify (3 minutes)

```bash
# Test HTTPS
curl -I https://api.yourdomain.com/health

# Check certificate
echo | openssl s_client -servername api.yourdomain.com -connect api.yourdomain.com:443 2>/dev/null | openssl x509 -noout -dates

# Test in browser
open https://api.yourdomain.com/weatherforecast
```

### ✅ Done!

Your API is now running with:
- Valid SSL certificate from Let's Encrypt
- HTTP to HTTPS redirection
- Automatic renewal via cron job
- Full control over configuration

---

## Common Issues and Quick Fixes

### Issue: DNS not resolving

**Check:**
```bash
dig api.yourdomain.com
nslookup api.yourdomain.com
```

**Fix:** Wait 5-30 minutes for DNS propagation

### Issue: Port 80 or 443 blocked

**Check:**
```bash
sudo netstat -tlnp | grep -E ':80|:443'
telnet your-server-ip 80
```

**Fix:** Configure firewall
```bash
# Ubuntu/Debian
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# CentOS/RHEL
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https
sudo firewall-cmd --reload
```

### Issue: Certificate validation fails

**Common causes:**
1. DNS not propagated - wait longer
2. Port 80 not accessible - check firewall
3. Wrong domain in configuration - verify spelling

**Check logs:**
```bash
# Caddy
docker logs tralivali-caddy

# Certbot
cat ssl/certbot/conf/letsencrypt.log
```

### Issue: Rate limit hit

**Cause:** Too many certificate requests (5 failures per hour)

**Fix:** 
- Wait 1 hour
- Use Let's Encrypt staging environment for testing:
  ```bash
  certbot certonly --staging -d api.yourdomain.com
  ```

---

## Next Steps

After completing domain setup:

1. **Update API clients** to use new domain
2. **Test all endpoints** over HTTPS
3. **Configure CORS** if frontend is on different domain
4. **Set up monitoring** for certificate expiration
5. **Update documentation** with production URL
6. **Configure CDN** (optional) for better performance
7. **Set up backup domain** (optional) for redundancy

---

## Support

For detailed information, see the [Complete SSL Configuration Guide](SSL_CONFIGURATION.md).

**Need help?**
- Check troubleshooting section in SSL guide
- Review server logs
- Verify DNS propagation
- Check Let's Encrypt status: https://letsencrypt.status.io/

**Resources:**
- [Let's Encrypt Documentation](https://letsencrypt.org/docs/)
- [Caddy Documentation](https://caddyserver.com/docs/)
- [Certbot Documentation](https://eff-certbot.readthedocs.io/)
- [Azure Container Apps Custom Domains](https://learn.microsoft.com/en-us/azure/container-apps/custom-domains-certificates)
