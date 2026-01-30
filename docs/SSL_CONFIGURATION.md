# SSL/TLS Configuration Guide

This guide covers multiple approaches for configuring SSL/TLS certificates for TraliVali, including Azure Container Apps managed certificates and self-hosted options with Let's Encrypt.

## Table of Contents

- [Option 1: Azure Container Apps Managed Certificates](#option-1-azure-container-apps-managed-certificates)
- [Option 2: Caddy Reverse Proxy (Automatic Let's Encrypt)](#option-2-caddy-reverse-proxy-automatic-lets-encrypt)
- [Option 3: Certbot with Nginx (Manual Let's Encrypt)](#option-3-certbot-with-nginx-manual-lets-encrypt)
- [DNS Configuration](#dns-configuration)
- [Certificate Renewal](#certificate-renewal)
- [Troubleshooting](#troubleshooting)

---

## Option 1: Azure Container Apps Managed Certificates

Azure Container Apps provides automatic SSL certificate management for custom domains, eliminating manual certificate handling.

### Prerequisites

- Azure Container Apps deployment (see [Azure Deployment Guide](../deploy/azure/README.md))
- A registered domain name
- Access to domain DNS settings
- Container App deployed and running

### Step-by-Step Guide

#### Step 1: Verify Container App Deployment

```bash
# Set your resource group and app name
RESOURCE_GROUP="tralivali-rg"
APP_NAME="tralivali-dev-api"

# Verify Container App is running
az containerapp show \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --query properties.configuration.ingress.fqdn \
  --output tsv
```

This command returns your default Container App URL (e.g., `tralivali-dev-api.nicebeach-12345678.eastus.azurecontainerapps.io`).

#### Step 2: Configure DNS Records

Before adding a custom domain, configure DNS records to point to your Container App.

##### Option A: Using CNAME Record (Recommended for subdomains)

For subdomains like `api.yourdomain.com`:

```
Type:  CNAME
Name:  api (or your subdomain)
Value: <your-container-app-fqdn>
TTL:   3600 (or your preference)
```

**Example:**
```
Type:  CNAME
Name:  api
Value: tralivali-dev-api.nicebeach-12345678.eastus.azurecontainerapps.io
TTL:   3600
```

##### Option B: Using A Record (For apex/root domain)

For apex domains like `yourdomain.com`, you need the static IP:

```bash
# Get the static IP address of your Container Apps Environment
ENVIRONMENT_NAME="tralivali-dev-env"

az containerapp env show \
  --resource-group $RESOURCE_GROUP \
  --name $ENVIRONMENT_NAME \
  --query properties.staticIp \
  --output tsv
```

Configure DNS A record:

```
Type:  A
Name:  @ (root domain) or subdomain
Value: <static-ip-address>
TTL:   3600
```

**Example:**
```
Type:  A
Name:  @
Value: 20.51.234.123
TTL:   3600
```

##### DNS Validation Record (Required for Certificate)

Azure requires a TXT record for domain validation:

```
Type:  TXT
Name:  asuid.<your-subdomain> or asuid (for root)
Value: <verification-token>
TTL:   3600
```

Get the verification token:

```bash
# Get the custom domain verification ID
az containerapp show \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --query properties.customDomainVerificationId \
  --output tsv
```

**Example:**
```
Type:  TXT
Name:  asuid.api
Value: 1234567890ABCDEF1234567890ABCDEF
TTL:   3600
```

#### Step 3: Add Custom Domain to Container App

```bash
# Add custom domain (without certificate first)
CUSTOM_DOMAIN="api.yourdomain.com"

az containerapp hostname add \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --hostname $CUSTOM_DOMAIN
```

#### Step 4: Enable Managed Certificate

```bash
# Create and bind managed certificate
az containerapp hostname bind \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --hostname $CUSTOM_DOMAIN \
  --environment $ENVIRONMENT_NAME \
  --validation-method CNAME
```

Azure will automatically:
- Validate domain ownership via DNS
- Issue a free SSL certificate from Let's Encrypt
- Configure HTTPS ingress
- Auto-renew the certificate before expiration

#### Step 5: Verify SSL Configuration

```bash
# Check certificate status
az containerapp hostname list \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --output table

# Test HTTPS endpoint
curl -I https://$CUSTOM_DOMAIN/health
```

#### Step 6: Configure HTTP to HTTPS Redirect

Update your Container App to redirect HTTP traffic to HTTPS:

```bash
az containerapp ingress update \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --allow-insecure false
```

### DNS Configuration Summary for Azure

| Record Type | Name | Value | Purpose |
|-------------|------|-------|---------|
| CNAME | `api` | `<app-fqdn>` | Route traffic to Container App |
| TXT | `asuid.api` | `<verification-id>` | Domain ownership validation |

Or for apex domain:

| Record Type | Name | Value | Purpose |
|-------------|------|-------|---------|
| A | `@` | `<static-ip>` | Route traffic to Container App |
| TXT | `asuid` | `<verification-id>` | Domain ownership validation |

### Benefits of Azure Managed Certificates

✅ **Automatic certificate provisioning** - No manual CSR generation  
✅ **Automatic renewal** - Certificates renewed 45 days before expiration  
✅ **No cost** - Free SSL certificates included  
✅ **Zero maintenance** - Azure handles everything  
✅ **Multiple domains** - Support for multiple custom domains per app  
✅ **Wildcard support** - Wildcard certificates available in some regions  

### Limitations

⚠️ Certificate managed by Azure (not exportable)  
⚠️ Requires Container Apps Environment with custom domains enabled  
⚠️ Domain validation required (DNS changes must propagate)  

---

## Option 2: Caddy Reverse Proxy (Automatic Let's Encrypt)

Caddy is a modern web server with automatic HTTPS via Let's Encrypt built-in, requiring zero manual certificate management.

### Why Choose Caddy?

✅ **Automatic HTTPS** - Certificates obtained and renewed automatically  
✅ **Zero configuration** - Just specify your domain name  
✅ **HTTP/2 and HTTP/3** - Modern protocol support  
✅ **Simpler than Nginx** - No complex SSL configuration  
✅ **Self-hosted control** - Full control over your infrastructure  

### Prerequisites

- Docker and Docker Compose installed
- A registered domain name
- Ports 80 and 443 accessible from the internet
- Domain DNS configured to point to your server

### Step-by-Step Guide with Caddy

#### Step 1: Create Caddyfile Configuration

Create `deploy/Caddyfile`:

```caddy
{
    # Global options
    email admin@yourdomain.com  # For Let's Encrypt notifications
    admin off                   # Disable admin API for security
}

# Your domain - Caddy automatically handles HTTPS
api.yourdomain.com {
    # Automatic HTTPS with Let's Encrypt
    # No manual certificate configuration needed!

    # Reverse proxy to API
    reverse_proxy api:8080 {
        # Health check
        health_uri /weatherforecast
        health_interval 30s
        health_timeout 10s
    }

    # Security headers
    header {
        X-Frame-Options "SAMEORIGIN"
        X-Content-Type-Options "nosniff"
        Referrer-Policy "no-referrer-when-downgrade"
        Content-Security-Policy "default-src 'self'; script-src 'self'; object-src 'none'; frame-ancestors 'self';"
        Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"
        -Server  # Remove server header
    }

    # File upload size limit
    request_body {
        max_size 100MB
    }

    # Logging
    log {
        output file /var/log/caddy/access.log
        format json
    }
}

# Redirect www to non-www (optional)
www.api.yourdomain.com {
    redir https://api.yourdomain.com{uri} permanent
}
```

#### Step 2: Create Docker Compose with Caddy

Create `deploy/docker-compose.caddy.yml`:

```yaml
version: '3.8'

services:
  # Caddy Reverse Proxy with Automatic HTTPS
  caddy:
    image: caddy:2.7-alpine
    container_name: tralivali-caddy
    restart: unless-stopped
    ports:
      - "80:80"     # HTTP - required for Let's Encrypt challenge
      - "443:443"   # HTTPS
      - "443:443/udp"  # HTTP/3 (QUIC)
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy_data:/data                    # Certificate storage
      - caddy_config:/config                # Caddy configuration
      - caddy_logs:/var/log/caddy          # Access logs
    environment:
      - ACME_AGREE=true                    # Agree to Let's Encrypt ToS
    depends_on:
      api:
        condition: service_healthy
    networks:
      - tralivali-prod-network
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 256M
        reservations:
          cpus: '0.25'
          memory: 128M
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:80"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 10s

  # API Application
  api:
    build:
      context: ..
      dockerfile: src/TraliVali.Api/Dockerfile
    container_name: tralivali-api-prod
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - MongoDB__ConnectionString=mongodb://${MONGO_ROOT_USERNAME}:${MONGO_ROOT_PASSWORD}@mongodb:27017/${MONGO_DATABASE}?authSource=admin
      - MongoDB__DatabaseName=${MONGO_DATABASE}
      - Redis__ConnectionString=redis:6379,password=${REDIS_PASSWORD}
      - RabbitMQ__ConnectionString=amqp://${RABBITMQ_USERNAME}:${RABBITMQ_PASSWORD}@rabbitmq:5672/${RABBITMQ_VHOST}
      - Jwt__PrivateKey=${JWT_PRIVATE_KEY}
      - Jwt__PublicKey=${JWT_PUBLIC_KEY}
      - Jwt__Issuer=${JWT_ISSUER:-TraliVali}
      - Jwt__Audience=${JWT_AUDIENCE:-TraliVali}
      - Jwt__ExpirationDays=${JWT_EXPIRATION_DAYS:-7}
      - Jwt__RefreshTokenExpirationDays=${JWT_REFRESH_TOKEN_EXPIRATION_DAYS:-30}
    depends_on:
      mongodb:
        condition: service_healthy
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    networks:
      - tralivali-prod-network
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          cpus: '1.0'
          memory: 1G
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/weatherforecast"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s

  # MongoDB - No exposed ports
  mongodb:
    image: mongo:7.0
    container_name: tralivali-mongodb-prod
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_ROOT_USERNAME}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_ROOT_PASSWORD}
      MONGO_INITDB_DATABASE: ${MONGO_DATABASE}
    volumes:
      - mongodb_data:/data/db
      - mongodb_config:/data/configdb
    networks:
      - tralivali-prod-network
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 4G
        reservations:
          cpus: '1.0'
          memory: 2G
    healthcheck:
      test: echo 'db.runCommand("ping").ok' | mongosh test --quiet
      interval: 30s
      timeout: 10s
      retries: 5
      start_period: 40s

  # RabbitMQ - No exposed ports
  rabbitmq:
    image: rabbitmq:3.12-alpine
    container_name: tralivali-rabbitmq-prod
    restart: unless-stopped
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USERNAME}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
      RABBITMQ_DEFAULT_VHOST: ${RABBITMQ_VHOST:-/}
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
      - rabbitmq_logs:/var/log/rabbitmq
    networks:
      - tralivali-prod-network
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 30s
      timeout: 10s
      retries: 5
      start_period: 40s

  # Redis - No exposed ports
  redis:
    image: redis:7-alpine
    container_name: tralivali-redis-prod
    restart: unless-stopped
    command: sh -c "redis-server --requirepass $${REDIS_PASSWORD} --appendonly yes --maxmemory 512mb --maxmemory-policy allkeys-lru"
    environment:
      REDIS_PASSWORD: ${REDIS_PASSWORD}
    volumes:
      - redis_data:/data
    networks:
      - tralivali-prod-network
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
    healthcheck:
      test: ["CMD", "sh", "-c", "redis-cli -a $$REDIS_PASSWORD ping"]
      interval: 30s
      timeout: 10s
      retries: 5
      start_period: 20s

volumes:
  caddy_data:
    driver: local
  caddy_config:
    driver: local
  caddy_logs:
    driver: local
  mongodb_data:
    driver: local
  mongodb_config:
    driver: local
  rabbitmq_data:
    driver: local
  rabbitmq_logs:
    driver: local
  redis_data:
    driver: local

networks:
  tralivali-prod-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

#### Step 3: Configure DNS for Caddy

Point your domain to your server's IP address:

**For subdomain (api.yourdomain.com):**
```
Type:  A
Name:  api
Value: <your-server-ip>
TTL:   3600
```

**For root domain (yourdomain.com):**
```
Type:  A
Name:  @
Value: <your-server-ip>
TTL:   3600
```

#### Step 4: Deploy with Caddy

```bash
# Navigate to deploy directory
cd /path/to/tralivali/deploy

# Ensure environment variables are set
cp .env.prod.example .env.prod
# Edit .env.prod with your values

# Start services with Caddy
docker compose -f docker-compose.caddy.yml --env-file .env.prod up -d

# Watch logs to see certificate provisioning
docker compose -f docker-compose.caddy.yml logs -f caddy
```

Caddy will automatically:
1. Request a certificate from Let's Encrypt
2. Complete the ACME challenge
3. Install the certificate
4. Configure HTTPS
5. Set up automatic renewal

#### Step 5: Verify HTTPS

```bash
# Check certificate
curl -vI https://api.yourdomain.com/health 2>&1 | grep -i "SSL\|certificate"

# Test API over HTTPS
curl https://api.yourdomain.com/weatherforecast

# Check certificate expiration
echo | openssl s_client -servername api.yourdomain.com -connect api.yourdomain.com:443 2>/dev/null | openssl x509 -noout -dates
```

### Advanced Caddy Configuration

#### Multiple Domains

```caddy
api.yourdomain.com, api.example.org {
    reverse_proxy api:8080
}
```

#### Custom Rate Limiting

```caddy
api.yourdomain.com {
    rate_limit {
        zone dynamic {
            key {remote_host}
            events 100
            window 1m
        }
    }
    
    reverse_proxy api:8080
}
```

#### Path-based Routing

```caddy
yourdomain.com {
    # API routes
    handle /api/* {
        reverse_proxy api:8080
    }
    
    # Static frontend
    handle /* {
        root * /var/www/html
        file_server
        try_files {path} /index.html
    }
}
```

### Caddy Certificate Storage

Certificates are stored in the `caddy_data` Docker volume:
- Location in container: `/data/caddy`
- Includes: Private keys, certificates, ACME account data
- Persistence: Survives container restarts

### Caddy vs Nginx

| Feature | Caddy | Nginx |
|---------|-------|-------|
| Automatic HTTPS | ✅ Built-in | ❌ Requires certbot |
| Configuration | Simple | Complex |
| HTTP/3 Support | ✅ Native | ⚠️ Requires QUIC build |
| Certificate Renewal | ✅ Automatic | ⚠️ Requires cron job |
| Memory Usage | ~50MB | ~20MB |
| Learning Curve | Easy | Moderate |

---

## Option 3: Certbot with Nginx (Manual Let's Encrypt)

For those who prefer traditional Nginx with manual Let's Encrypt certificate management using Certbot.

### Prerequisites

- Server with Nginx installed (or Docker Compose setup)
- Ports 80 and 443 accessible from the internet
- Domain DNS configured to point to your server
- Root or sudo access

### Step-by-Step Guide with Certbot

#### Step 1: Install Certbot

**Ubuntu/Debian:**
```bash
sudo apt update
sudo apt install certbot python3-certbot-nginx
```

**RHEL/CentOS:**
```bash
sudo yum install certbot python3-certbot-nginx
```

**Using Docker:**
```bash
docker pull certbot/certbot
```

#### Step 2: Configure Nginx for HTTP First

Update `deploy/nginx.conf`:

```nginx
server {
    listen 80;
    server_name api.yourdomain.com;

    # Let's Encrypt challenge directory
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    # Redirect all other traffic to HTTPS (after certificate is obtained)
    # location / {
    #     return 301 https://$host$request_uri;
    # }

    # Temporary: Proxy to API during initial setup
    location / {
        proxy_pass http://api:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

#### Step 3: Obtain Certificate Using Certbot

**Method A: Standalone mode (stops Nginx temporarily)**
```bash
# Stop Nginx
sudo systemctl stop nginx

# Obtain certificate
sudo certbot certonly --standalone \
  -d api.yourdomain.com \
  --email admin@yourdomain.com \
  --agree-tos \
  --no-eff-email

# Start Nginx
sudo systemctl start nginx
```

**Method B: Webroot mode (Nginx keeps running)**
```bash
# Create webroot directory
sudo mkdir -p /var/www/certbot

# Obtain certificate
sudo certbot certonly --webroot \
  -w /var/www/certbot \
  -d api.yourdomain.com \
  --email admin@yourdomain.com \
  --agree-tos \
  --no-eff-email
```

**Method C: Using Docker Compose**

Add to `docker-compose.prod.yml`:

```yaml
  certbot:
    image: certbot/certbot
    container_name: tralivali-certbot
    volumes:
      - ./ssl/certbot/conf:/etc/letsencrypt
      - ./ssl/certbot/www:/var/www/certbot
    command: certonly --webroot --webroot-path=/var/www/certbot --email admin@yourdomain.com --agree-tos --no-eff-email -d api.yourdomain.com
```

Mount the certbot webroot in nginx:
```yaml
  nginx:
    volumes:
      - ./ssl/certbot/www:/var/www/certbot:ro
      - ./ssl/certbot/conf:/etc/letsencrypt:ro
```

Run once to obtain certificate:
```bash
docker compose -f docker-compose.prod.yml run --rm certbot
```

#### Step 4: Configure Nginx for HTTPS

Update `deploy/nginx.conf` with SSL configuration:

```nginx
upstream api_backend {
    server api:8080 max_fails=3 fail_timeout=30s;
    keepalive 32;
}

# HTTP server - redirect to HTTPS
server {
    listen 80;
    server_name api.yourdomain.com;

    # Let's Encrypt challenge
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    # Redirect to HTTPS
    location / {
        return 301 https://$host$request_uri;
    }
}

# HTTPS server
server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;

    # SSL certificate configuration
    ssl_certificate /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.yourdomain.com/privkey.pem;

    # SSL security settings
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers 'ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384';
    ssl_prefer_server_ciphers on;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;
    ssl_session_tickets off;

    # OCSP Stapling
    ssl_stapling on;
    ssl_stapling_verify on;
    ssl_trusted_certificate /etc/letsencrypt/live/api.yourdomain.com/chain.pem;
    resolver 8.8.8.8 8.8.4.4 valid=300s;
    resolver_timeout 5s;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "no-referrer-when-downgrade" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self'; object-src 'none'; frame-ancestors 'self';" always;

    # Client body size limit
    client_max_body_size 100M;

    # Proxy timeouts
    proxy_connect_timeout 60s;
    proxy_send_timeout 60s;
    proxy_read_timeout 60s;

    # API endpoints
    location /api/ {
        proxy_pass http://api_backend/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # SignalR WebSocket endpoint
    location /hubs/ {
        proxy_pass http://api_backend/hubs/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        
        # WebSocket specific timeouts
        proxy_read_timeout 3600s;
        proxy_send_timeout 3600s;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://api_backend/weatherforecast;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        access_log off;
    }
}
```

#### Step 5: Update Docker Compose for SSL

Update `docker-compose.prod.yml`:

```yaml
  nginx:
    image: nginx:1.25-alpine
    container_name: tralivali-nginx-prod
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - ./ssl/certbot/conf:/etc/letsencrypt:ro      # SSL certificates
      - ./ssl/certbot/www:/var/www/certbot:ro       # ACME challenge
```

#### Step 6: Reload Nginx

```bash
# Test configuration
docker exec tralivali-nginx-prod nginx -t

# Reload Nginx
docker exec tralivali-nginx-prod nginx -s reload

# Or restart container
docker compose -f docker-compose.prod.yml restart nginx
```

#### Step 7: Set Up Automatic Renewal

**Using Cron (system certbot):**
```bash
# Test renewal
sudo certbot renew --dry-run

# Add to crontab
sudo crontab -e

# Add this line (runs twice daily)
0 0,12 * * * certbot renew --quiet --post-hook "systemctl reload nginx"
```

**Using Docker Compose:**

Create `deploy/docker-compose.certbot-renew.yml`:

```yaml
version: '3.8'

services:
  certbot-renew:
    image: certbot/certbot
    volumes:
      - ./ssl/certbot/conf:/etc/letsencrypt
      - ./ssl/certbot/www:/var/www/certbot
    command: renew --quiet
```

Add to host cron:
```bash
# Edit crontab
crontab -e

# Add renewal job (runs daily at 2 AM)
0 2 * * * cd /path/to/deploy && docker compose -f docker-compose.certbot-renew.yml run --rm certbot-renew && docker compose -f docker-compose.prod.yml restart nginx >> /var/log/certbot-renew.log 2>&1
```

---

## DNS Configuration

### Understanding DNS Records for SSL

SSL certificate issuance requires domain validation. Here's what you need to know about DNS records:

#### A Record (IPv4 Address)

Maps a domain name to an IP address.

```
Type:  A
Name:  @ (root) or subdomain (api, www)
Value: <IP address>
TTL:   3600 (1 hour) or higher
```

**When to use:**
- Pointing root domain (yourdomain.com)
- Pointing subdomain to specific server
- Required for Let's Encrypt validation

**Example:**
```
api.yourdomain.com → 203.0.113.45
```

#### CNAME Record (Canonical Name)

Creates an alias for another domain name.

```
Type:  CNAME
Name:  subdomain (api, www)
Value: <target domain or FQDN>
TTL:   3600
```

**When to use:**
- Pointing subdomain to Azure/AWS/Cloud FQDN
- Load balancer endpoints
- CDN endpoints

**Example:**
```
api.yourdomain.com → tralivali-app.azurecontainerapps.io
```

⚠️ **Limitations:**
- Cannot be used for root domain (yourdomain.com)
- Cannot coexist with other records of same name

#### TXT Record (Text)

Stores text data, used for domain verification.

```
Type:  TXT
Name:  _acme-challenge or asuid.subdomain
Value: <validation token>
TTL:   300 (5 minutes) for validation
```

**When to use:**
- Let's Encrypt DNS-01 challenge
- Azure domain verification
- Domain ownership proof

**Example - Let's Encrypt DNS challenge:**
```
_acme-challenge.api.yourdomain.com → "validation-token-string"
```

**Example - Azure verification:**
```
asuid.api.yourdomain.com → "azure-verification-id"
```

#### CAA Record (Certificate Authority Authorization)

Specifies which CAs can issue certificates for your domain.

```
Type:  CAA
Name:  @ or subdomain
Value: 0 issue "letsencrypt.org"
TTL:   3600
```

**When to use (optional but recommended):**
- Restrict certificate issuance to trusted CAs
- Prevent unauthorized certificate issuance

**Example - Allow Let's Encrypt only:**
```
0 issue "letsencrypt.org"
0 issuewild "letsencrypt.org"
0 iodef "mailto:security@yourdomain.com"
```

### DNS Propagation

After updating DNS records:

1. **TTL Matters**: Time To Live determines how long DNS cache holds old values
   - Lower TTL = Faster propagation, more DNS queries
   - Recommended: 300-3600 seconds

2. **Check Propagation**:
   ```bash
   # Check from multiple locations
   dig api.yourdomain.com
   dig +short api.yourdomain.com
   
   # Check from specific DNS server
   dig @8.8.8.8 api.yourdomain.com
   
   # Use online tools
   # https://www.whatsmydns.net/
   # https://dnschecker.org/
   ```

3. **Typical Propagation Time**:
   - Local: Immediate to few minutes
   - Regional: 15-60 minutes
   - Global: 1-24 hours (usually < 6 hours)

### DNS Configuration Examples by Provider

#### Cloudflare

1. Log in to Cloudflare dashboard
2. Select your domain
3. Go to **DNS** tab
4. Add records:

```
Type:  A
Name:  api
IPv4:  203.0.113.45
Proxy: Off (Orange cloud off) ⚠️ Important for Let's Encrypt
TTL:   Auto
```

⚠️ **Important**: Disable Cloudflare proxy (orange cloud) during initial certificate setup, or use DNS-01 challenge.

#### Google Domains / Cloud DNS

1. Go to DNS settings
2. Add custom resource records:

```
Name:  api
Type:  A
TTL:   1H
Data:  203.0.113.45
```

#### Namecheap

1. Advanced DNS tab
2. Add record:

```
Type:  A Record
Host:  api
Value: 203.0.113.45
TTL:   Automatic
```

#### GoDaddy

1. DNS Management
2. Add record:

```
Type:  A
Name:  api
Value: 203.0.113.45
TTL:   600 seconds
```

### DNS Troubleshooting

```bash
# Verify DNS resolution
nslookup api.yourdomain.com

# Check DNS propagation globally
dig +short api.yourdomain.com @8.8.8.8
dig +short api.yourdomain.com @1.1.1.1

# Check TXT records (for validation)
dig TXT _acme-challenge.api.yourdomain.com +short
dig TXT asuid.api.yourdomain.com +short

# Check CAA records
dig CAA yourdomain.com +short
```

---

## Certificate Renewal

### Automatic Renewal

#### Azure Container Apps
- **Renewal Schedule**: Automatic, 45 days before expiration
- **Action Required**: None
- **Monitoring**: Check hostname binding status

```bash
az containerapp hostname list \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --output table
```

#### Caddy
- **Renewal Schedule**: Automatic, 30 days before expiration
- **Action Required**: None
- **Monitoring**: Check Caddy logs

```bash
docker logs tralivali-caddy 2>&1 | grep -i "renew\|certificate"
```

#### Certbot with Nginx
- **Renewal Schedule**: Set up via cron (recommended: twice daily)
- **Action Required**: Configure cron job
- **Monitoring**: Check certbot renewal logs

```bash
# Test renewal
sudo certbot renew --dry-run

# Check certificate expiration
sudo certbot certificates

# View renewal logs
sudo tail -f /var/log/letsencrypt/letsencrypt.log
```

### Manual Renewal

If automatic renewal fails:

**Certbot:**
```bash
# Renew all certificates
sudo certbot renew

# Force renew specific certificate
sudo certbot renew --cert-name api.yourdomain.com --force-renewal

# Reload Nginx
sudo systemctl reload nginx
# Or for Docker:
docker compose -f docker-compose.prod.yml restart nginx
```

### Certificate Monitoring

Set up alerts for certificate expiration:

**Using OpenSSL:**
```bash
#!/bin/bash
# check-cert-expiry.sh

DOMAIN="api.yourdomain.com"
WARN_DAYS=30

expiry_date=$(echo | openssl s_client -servername $DOMAIN -connect $DOMAIN:443 2>/dev/null | openssl x509 -noout -enddate | cut -d= -f2)
expiry_epoch=$(date -d "$expiry_date" +%s)
current_epoch=$(date +%s)
days_until_expiry=$(( ($expiry_epoch - $current_epoch) / 86400 ))

if [ $days_until_expiry -lt $WARN_DAYS ]; then
    echo "WARNING: Certificate for $DOMAIN expires in $days_until_expiry days!"
    # Send alert (email, Slack, etc.)
fi
```

**External Monitoring Services:**
- [SSL Labs](https://www.ssllabs.com/ssltest/) - Free SSL testing
- [Uptime Robot](https://uptimerobot.com/) - Free uptime + SSL monitoring
- [Let's Monitor](https://letsmonitor.org/) - Free Let's Encrypt expiration alerts

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: Certificate validation fails

**Symptoms:**
- "Failed authorization procedure"
- "Connection refused"
- "Timeout during connect"

**Solutions:**

1. **Check DNS propagation:**
   ```bash
   dig +short api.yourdomain.com
   # Should return your server IP
   ```

2. **Verify port 80 is accessible:**
   ```bash
   curl -v http://api.yourdomain.com/.well-known/acme-challenge/test
   ```

3. **Check firewall:**
   ```bash
   # Allow HTTP and HTTPS
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw status
   ```

4. **Verify Nginx/Caddy is running:**
   ```bash
   docker ps | grep -E "nginx|caddy"
   ```

#### Issue: Certificate renewal fails

**Solutions:**

1. **Test renewal in dry-run mode:**
   ```bash
   sudo certbot renew --dry-run
   ```

2. **Check rate limits:**
   - Let's Encrypt: 5 failed validations per account, per hostname, per hour
   - Wait 1 hour and try again

3. **Force renewal (if close to expiration):**
   ```bash
   sudo certbot renew --force-renewal
   ```

4. **Check logs:**
   ```bash
   # Certbot logs
   sudo tail -n 100 /var/log/letsencrypt/letsencrypt.log
   
   # Caddy logs
   docker logs tralivali-caddy --tail 100
   ```

#### Issue: Mixed content warnings (HTTPS page loading HTTP resources)

**Solutions:**

1. **Update API to use HTTPS URLs in responses**
2. **Check Content-Security-Policy header**
3. **Use protocol-relative URLs:** `//api.yourdomain.com` instead of `http://`
4. **Set `X-Forwarded-Proto` header** in reverse proxy

#### Issue: WebSocket connection fails over HTTPS

**Solutions:**

1. **Ensure WebSocket upgrade headers are set:**
   ```nginx
   proxy_set_header Upgrade $http_upgrade;
   proxy_set_header Connection "upgrade";
   ```

2. **Check timeout settings:**
   ```nginx
   proxy_read_timeout 3600s;
   proxy_send_timeout 3600s;
   ```

3. **Verify WSS (WebSocket Secure) is used in client:**
   ```javascript
   const connection = new WebSocket("wss://api.yourdomain.com/hubs/chat");
   ```

#### Issue: "Too many certificates already issued"

**Cause:** Let's Encrypt rate limit hit (50 certificates per registered domain per week)

**Solutions:**

1. **Use staging environment during testing:**
   ```bash
   certbot certonly --staging -d api.yourdomain.com
   ```

2. **Wait for rate limit window (7 days) to reset**

3. **Consider using wildcard certificate:**
   ```bash
   certbot certonly --dns-route53 -d "*.yourdomain.com"
   ```
   (Requires DNS-01 challenge)

#### Issue: Certificate not trusted in browser

**Symptoms:**
- "Your connection is not private" error
- "NET::ERR_CERT_AUTHORITY_INVALID"

**Solutions:**

1. **Check certificate chain:**
   ```bash
   openssl s_client -connect api.yourdomain.com:443 -showcerts
   ```

2. **Ensure fullchain.pem is used (not cert.pem):**
   ```nginx
   ssl_certificate /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem;
   ```

3. **Verify certificate with SSL Labs:**
   https://www.ssllabs.com/ssltest/analyze.html?d=api.yourdomain.com

#### Issue: Caddy not obtaining certificate

**Solutions:**

1. **Check Caddy logs:**
   ```bash
   docker logs tralivali-caddy
   ```

2. **Verify email is set in Caddyfile:**
   ```caddy
   {
       email admin@yourdomain.com
   }
   ```

3. **Ensure domain is accessible:**
   ```bash
   curl -I http://api.yourdomain.com
   ```

4. **Check Caddy data volume permissions:**
   ```bash
   docker volume inspect tralivali_caddy_data
   ```

### Testing SSL Configuration

```bash
# Test SSL/TLS configuration
openssl s_client -connect api.yourdomain.com:443 -servername api.yourdomain.com

# Check certificate details
echo | openssl s_client -connect api.yourdomain.com:443 -servername api.yourdomain.com 2>/dev/null | openssl x509 -noout -text

# Test with curl
curl -vI https://api.yourdomain.com/health

# Check certificate expiration
echo | openssl s_client -servername api.yourdomain.com -connect api.yourdomain.com:443 2>/dev/null | openssl x509 -noout -dates

# Test with SSL Labs (comprehensive)
# Visit: https://www.ssllabs.com/ssltest/analyze.html?d=api.yourdomain.com
```

### Getting Help

If issues persist:

1. **Review logs carefully** - Most issues are evident in error logs
2. **Check Let's Encrypt status** - https://letsencrypt.status.io/
3. **Community Forums:**
   - Let's Encrypt: https://community.letsencrypt.org/
   - Caddy: https://caddy.community/
   - Nginx: https://forum.nginx.org/
4. **Documentation:**
   - Let's Encrypt: https://letsencrypt.org/docs/
   - Caddy: https://caddyserver.com/docs/
   - Certbot: https://eff-certbot.readthedocs.io/

---

## Security Best Practices

1. ✅ **Use HTTPS Everywhere** - Redirect all HTTP traffic to HTTPS
2. ✅ **Enable HSTS** - Force browsers to use HTTPS
3. ✅ **Strong TLS Configuration** - Use TLS 1.2+ only
4. ✅ **Keep Certificates Private** - Never commit private keys to git
5. ✅ **Monitor Expiration** - Set up alerts for certificate expiration
6. ✅ **Use Strong Ciphers** - Disable weak ciphers and protocols
7. ✅ **OCSP Stapling** - Improve certificate validation performance
8. ✅ **Regular Updates** - Keep web server and SSL libraries updated

## Summary

This guide covered three approaches for SSL/TLS configuration:

| Approach | Best For | Complexity | Maintenance |
|----------|----------|------------|-------------|
| **Azure Managed Certs** | Azure deployments | ⭐ Easy | ⭐ None |
| **Caddy** | Self-hosted, simple setup | ⭐⭐ Easy | ⭐ Minimal |
| **Certbot + Nginx** | Traditional, full control | ⭐⭐⭐ Moderate | ⭐⭐ Low |

**Recommended:**
- Use **Azure Managed Certificates** if deploying to Azure Container Apps
- Use **Caddy** for self-hosted deployments (simplest automatic HTTPS)
- Use **Certbot + Nginx** if you need fine-grained control or already use Nginx

All approaches provide valid, trusted SSL certificates from Let's Encrypt at no cost.
