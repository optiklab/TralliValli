# TraliVali Production Deployment

This directory contains production-ready Docker Compose configuration for deploying TraliVali.

## Quick Start

### Prerequisites

- Docker Engine 20.10 or higher
- Docker Compose v2.0 or higher
- Sufficient system resources (minimum: 4 CPU cores, 8GB RAM)

### Initial Setup

1. **Create environment file:**
   ```bash
   cd deploy
   cp .env.prod.example .env.prod
   ```

2. **Configure environment variables:**
   Edit `.env.prod` and set all required values:
   - Change all default passwords to strong, unique values
   - Generate RSA key pair for JWT authentication
   - Configure Azure services (optional)
   - Set secure signing keys

3. **Generate JWT RSA Key Pair:**
   ```bash
   # Generate private key
   openssl genrsa -out private.pem 2048
   
   # Generate public key
   openssl rsa -in private.pem -outform PEM -pubout -out public.pem
   
   # View private key (copy to JWT_PRIVATE_KEY in .env.prod)
   cat private.pem
   
   # View public key (copy to JWT_PUBLIC_KEY in .env.prod)
   cat public.pem
   
   # Clean up key files (keys are now in .env.prod)
   rm private.pem public.pem
   ```

4. **Build and start services:**
   ```bash
   docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
   ```

5. **Verify services are healthy:**
   ```bash
   docker compose -f docker-compose.prod.yml ps
   docker compose -f docker-compose.prod.yml logs -f
   ```

## Architecture

### Services

- **nginx**: Reverse proxy (only service with exposed ports)
  - Ports: 80 (HTTP), 443 (HTTPS)
  - Routes traffic to API
  - Handles SSL termination
  - Resource limits: 256MB RAM, 0.5 CPU

- **api**: .NET 8 API application
  - Internal port: 8080
  - Multi-stage Docker build
  - Optimized Alpine-based image
  - Resource limits: 2GB RAM, 2.0 CPU
  - Runs as non-root user

- **mongodb**: MongoDB database (no exposed ports)
  - Internal only (not accessible from host)
  - Resource limits: 4GB RAM, 2.0 CPU
  - Data persisted in Docker volumes

- **rabbitmq**: RabbitMQ message broker (no exposed ports)
  - Internal only (not accessible from host)
  - Resource limits: 1GB RAM, 1.0 CPU
  - Alpine-based image for smaller size

- **redis**: Redis cache (no exposed ports)
  - Internal only (not accessible from host)
  - Resource limits: 512MB RAM, 0.5 CPU
  - LRU eviction policy configured

### Security Features

✅ **Network Isolation**
- All services except nginx have no exposed ports
- Services communicate over internal Docker network
- External access only through reverse proxy

✅ **Resource Limits**
- CPU and memory limits prevent resource exhaustion
- Reservation guarantees minimum resources

✅ **Health Checks**
- All services have health checks configured
- Automatic restart on health check failures
- Configurable intervals and timeouts

✅ **Restart Policies**
- All services use `unless-stopped` restart policy
- Automatic recovery from failures
- Survives Docker daemon restarts

✅ **Security Hardening**
- API runs as non-root user (uid 1000)
- Alpine-based images for minimal attack surface
- Security headers configured in nginx
- Specific image versions (no `latest` tags)

## Operations

### View Logs

```bash
# All services
docker compose -f docker-compose.prod.yml logs -f

# Specific service
docker compose -f docker-compose.prod.yml logs -f api
docker compose -f docker-compose.prod.yml logs -f nginx
```

### Service Management

```bash
# Stop all services
docker compose -f docker-compose.prod.yml stop

# Start all services
docker compose -f docker-compose.prod.yml start

# Restart specific service
docker compose -f docker-compose.prod.yml restart api

# Scale services (if needed)
docker compose -f docker-compose.prod.yml up -d --scale api=3
```

### Health Status

```bash
# Check service health
docker compose -f docker-compose.prod.yml ps

# Inspect specific service health
docker inspect tralivali-api-prod --format='{{json .State.Health}}' | jq
```

### Updates and Deployments

```bash
# Pull latest changes and rebuild
git pull
docker compose -f docker-compose.prod.yml build --no-cache api
docker compose -f docker-compose.prod.yml up -d --no-deps api

# Rollback if needed
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d
```

### Backup and Restore

```bash
# List volume names first
docker volume ls | grep mongodb

# Backup volumes (replace PROJECT_NAME with actual prefix from volume ls output)
docker run --rm -v PROJECT_NAME_mongodb_data:/data -v $(pwd)/backups:/backup alpine tar czf /backup/mongodb-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .

# Restore volumes (replace PROJECT_NAME with actual prefix from volume ls output)
docker run --rm -v PROJECT_NAME_mongodb_data:/data -v $(pwd)/backups:/backup alpine tar xzf /backup/mongodb-YYYYMMDD-HHMMSS.tar.gz -C /data
```

### Monitoring

```bash
# Resource usage
docker stats

# Disk usage
docker system df -v

# Network inspection (check actual network name first)
docker network ls
docker network inspect <ACTUAL_NETWORK_NAME>
```

## SSL/TLS Configuration

For HTTPS support:

1. **Obtain SSL certificates** (e.g., from Let's Encrypt):
   ```bash
   # Using certbot
   sudo certbot certonly --standalone -d yourdomain.com
   ```

2. **Copy certificates to deploy directory:**
   ```bash
   mkdir -p deploy/ssl
   sudo cp /etc/letsencrypt/live/yourdomain.com/fullchain.pem deploy/ssl/
   sudo cp /etc/letsencrypt/live/yourdomain.com/privkey.pem deploy/ssl/
   ```

3. **Update nginx.conf** to include SSL configuration (commented sections)

4. **Restart nginx:**
   ```bash
   docker compose -f docker-compose.prod.yml restart nginx
   ```

**📚 For comprehensive SSL configuration including:**
- Azure Container Apps managed certificates (automatic HTTPS)
- Caddy reverse proxy with automatic Let's Encrypt
- Certbot with Nginx for manual certificate management
- DNS configuration (A, CNAME, TXT records)
- Certificate renewal automation
- Troubleshooting guide

**See the complete [SSL Configuration Guide](../docs/SSL_CONFIGURATION.md)**

## Troubleshooting

### Service Won't Start

1. Check logs:
   ```bash
   docker compose -f docker-compose.prod.yml logs [service-name]
   ```

2. Verify environment variables:
   ```bash
   docker compose -f docker-compose.prod.yml config
   ```

3. Check resource availability:
   ```bash
   docker system df
   docker stats
   ```

### Health Check Failures

Wait 60 seconds after startup for all health checks to stabilize. If issues persist:

```bash
# View detailed health check logs
docker inspect tralivali-api-prod --format='{{range .State.Health.Log}}{{.Output}}{{end}}'

# Check if services can communicate
docker exec tralivali-api-prod wget --spider http://mongodb:27017
```

### Connection Issues

```bash
# Verify network connectivity
docker network inspect tralivali-prod-network

# Test nginx to API connectivity
docker exec tralivali-nginx-prod wget --spider http://api:8080/weatherforecast

# Check API environment variables
docker exec tralivali-api-prod env | grep -E 'MONGO|REDIS|RABBITMQ'
```

### Performance Issues

1. Check resource usage:
   ```bash
   docker stats
   ```

2. Adjust resource limits in `docker-compose.prod.yml`

3. Scale services if needed:
   ```bash
   docker compose -f docker-compose.prod.yml up -d --scale api=2
   ```

## Production Checklist

Before deploying to production:

- [ ] All passwords changed from defaults
- [ ] JWT RSA keys generated and configured
- [ ] SSL/TLS certificates obtained and configured
- [ ] Azure services configured (if using email/blob storage)
- [ ] Resource limits tuned for your environment
- [ ] Monitoring and alerting configured
- [ ] Backup strategy implemented
- [ ] Firewall rules configured (only ports 80/443 open)
- [ ] Log aggregation configured
- [ ] Database backups scheduled
- [ ] Security audit completed
- [ ] Create dedicated `/health` endpoint in API (currently using `/weatherforecast` as placeholder)

## Resource Requirements

### Minimum Requirements
- **CPU**: 4 cores
- **RAM**: 8GB
- **Disk**: 50GB SSD
- **Network**: 100 Mbps

### Recommended Production
- **CPU**: 8 cores
- **RAM**: 16GB
- **Disk**: 100GB SSD
- **Network**: 1 Gbps

## Support

For issues or questions:
- Check logs: `docker compose -f docker-compose.prod.yml logs`
- Review health checks: `docker compose -f docker-compose.prod.yml ps`
- Consult main project documentation
- Create an issue in the repository

## License

See main project LICENSE file.
