# Docker Compose Local Development Setup

This document describes how to set up and use the Docker Compose environment for local development of the TraliVali messaging platform.

## Prerequisites

- Docker Engine 20.10 or higher
- Docker Compose v2.0 or higher

## Quick Start

1. **Copy the environment file:**
   ```bash
   cp .env.example .env
   ```

2. **Start all services:**
   ```bash
   docker compose up -d
   ```

3. **Check service status:**
   ```bash
   docker compose ps
   ```

4. **View logs:**
   ```bash
   docker compose logs -f
   ```

5. **Stop all services:**
   ```bash
   docker compose down
   ```

## Services

### MongoDB
- **Image:** `mongo:latest`
- **Port:** 27017 (configurable via `MONGO_PORT`)
- **Management:** Connect using MongoDB Compass or mongosh
- **Connection String:** `mongodb://admin:password@localhost:27017/tralivali?authSource=admin`
- **Data Persistence:** Volumes for `/data/db` and `/data/configdb`

### RabbitMQ
- **Image:** `rabbitmq:3-management`
- **AMQP Port:** 5672 (configurable via `RABBITMQ_PORT`)
- **Management UI Port:** 15672 (configurable via `RABBITMQ_MANAGEMENT_PORT`)
- **Management UI:** http://localhost:15672
- **Default Credentials:** admin/password (configurable via `.env`)
- **Data Persistence:** Volumes for `/var/lib/rabbitmq` and `/var/log/rabbitmq`

### Redis
- **Image:** `redis:7-alpine`
- **Port:** 6379 (configurable via `REDIS_PORT`)
- **Connection:** `redis-cli -h localhost -p 6379 -a password`
- **Data Persistence:** Volume for `/data` with AOF (Append Only File) enabled

## Health Checks

All services are configured with health checks:

- **MongoDB:** Pings the database every 10 seconds
- **RabbitMQ:** Uses `rabbitmq-diagnostics -q ping` every 10 seconds
- **Redis:** Increments a counter every 10 seconds

Services are considered healthy after passing their health checks, which typically takes 20-30 seconds after startup.

## Data Persistence

All service data is persisted using Docker volumes:

- `trallivalli_mongodb_data` - MongoDB database files
- `trallivalli_mongodb_config` - MongoDB configuration
- `trallivalli_rabbitmq_data` - RabbitMQ data and messages
- `trallivalli_rabbitmq_logs` - RabbitMQ logs
- `trallivalli_redis_data` - Redis data with AOF

Data persists across container restarts. To completely remove all data:

```bash
docker compose down -v
```

⚠️ **Warning:** The `-v` flag removes volumes and deletes all data!

## Configuration

All configuration is managed through environment variables in the `.env` file. Copy `.env.example` to `.env` and modify as needed:

```bash
cp .env.example .env
# Edit .env with your preferred values
```

### Key Environment Variables

- `MONGO_ROOT_USERNAME`, `MONGO_ROOT_PASSWORD` - MongoDB credentials
- `MONGO_DATABASE` - Default database name
- `RABBITMQ_USERNAME`, `RABBITMQ_PASSWORD` - RabbitMQ credentials
- `REDIS_PASSWORD` - Redis authentication password
- Port configurations for all services

See `.env.example` for the complete list of variables with documentation.

## Useful Commands

### View Service Logs
```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f mongodb
docker compose logs -f rabbitmq
docker compose logs -f redis
```

### Restart a Service
```bash
docker compose restart mongodb
docker compose restart rabbitmq
docker compose restart redis
```

### Execute Commands in Containers
```bash
# MongoDB shell
docker exec -it tralivali-mongodb mongosh

# Redis CLI
docker exec -it tralivali-redis redis-cli -a password

# RabbitMQ management commands
docker exec -it tralivali-rabbitmq rabbitmqctl status
```

### Check Resource Usage
```bash
docker stats
```

## Troubleshooting

### Service Won't Start

1. Check if ports are already in use:
   ```bash
   netstat -tuln | grep -E '27017|5672|15672|6379'
   ```

2. Check service logs:
   ```bash
   docker compose logs [service-name]
   ```

3. Verify environment variables:
   ```bash
   docker compose config
   ```

### Health Check Failures

Wait 30 seconds after startup for health checks to stabilize. If a service remains unhealthy:

```bash
# Check health status
docker inspect tralivali-mongodb --format='{{json .State.Health}}' | jq

# View health check logs
docker inspect tralivali-mongodb --format='{{range .State.Health.Log}}{{.Output}}{{end}}'
```

### Clean Start

To start fresh with no data:

```bash
docker compose down -v
docker compose up -d
```

## Production Notes

⚠️ **This setup is for local development only!**

For production:

1. Use strong, unique passwords for all services
2. Enable TLS/SSL encryption
3. Configure proper authentication and authorization
4. Use secrets management (e.g., Docker Secrets, Azure Key Vault)
5. Implement proper backup strategies
6. Configure resource limits
7. Use specific image tags instead of `latest`
8. Review and harden security settings

## Next Steps

Once the services are running:

1. Configure the application's connection strings in `appsettings.json`
2. Run database migrations (when implemented)
3. Start the .NET API: `dotnet run --project src/TraliVali.Api`
4. Access RabbitMQ Management UI at http://localhost:15672

## Support

For issues or questions, please refer to the main project documentation or create an issue in the repository.
