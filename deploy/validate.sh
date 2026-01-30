#!/bin/bash
# Production Docker Compose Validation Script
# This script validates the production Docker Compose setup

set -e

echo "==================================="
echo "Production Docker Setup Validation"
echo "==================================="
echo ""

# Check if required files exist
echo "✓ Checking required files..."
files=(
    "docker-compose.prod.yml"
    "nginx.conf"
    ".env.prod.example"
    "README.md"
    ".gitignore"
    "../src/TraliVali.Api/Dockerfile"
)

for file in "${files[@]}"; do
    if [ -f "$file" ]; then
        echo "  ✓ $file exists"
    else
        echo "  ✗ $file missing"
        exit 1
    fi
done

echo ""
echo "✓ Validating docker-compose.prod.yml syntax..."
docker compose -f docker-compose.prod.yml config > /dev/null 2>&1 || {
    echo "  ✗ docker-compose.prod.yml has syntax errors"
    exit 1
}
echo "  ✓ docker-compose.prod.yml syntax is valid"

echo ""
echo "✓ Checking security features..."

# Check for no exposed ports except nginx
exposed_services=$(docker compose -f docker-compose.prod.yml config | grep -A 100 "^  [a-z]" | grep "ports:" | wc -l)
if [ "$exposed_services" -eq 1 ]; then
    echo "  ✓ Only one service (nginx) has exposed ports"
else
    echo "  ✗ Multiple services have exposed ports (should be only nginx)"
fi

# Check for resource limits
services_with_limits=$(docker compose -f docker-compose.prod.yml config | grep -c "memory:" || true)
if [ "$services_with_limits" -ge 5 ]; then
    echo "  ✓ Resource limits configured for services"
else
    echo "  ⚠ Some services may be missing resource limits"
fi

# Check for health checks
services_with_healthcheck=$(docker compose -f docker-compose.prod.yml config | grep -c "test:" || true)
if [ "$services_with_healthcheck" -ge 5 ]; then
    echo "  ✓ Health checks configured for services"
else
    echo "  ⚠ Some services may be missing health checks"
fi

# Check for restart policies
services_with_restart=$(docker compose -f docker-compose.prod.yml config | grep -c "unless-stopped" || true)
if [ "$services_with_restart" -ge 5 ]; then
    echo "  ✓ Restart policies configured for services"
else
    echo "  ⚠ Some services may be missing restart policies"
fi

echo ""
echo "✓ Checking Dockerfile..."

# Check if Dockerfile exists
if [ -f "../src/TraliVali.Api/Dockerfile" ]; then
    echo "  ✓ Dockerfile exists"
    
    # Check for multi-stage build
    stages=$(grep -c "^FROM " ../src/TraliVali.Api/Dockerfile || true)
    if [ "$stages" -ge 3 ]; then
        echo "  ✓ Multi-stage build detected ($stages stages)"
    else
        echo "  ✗ Multi-stage build not found (expected at least 3 stages)"
    fi
    
    # Check for Alpine base image (smaller size)
    if grep -q "alpine" ../src/TraliVali.Api/Dockerfile; then
        echo "  ✓ Using Alpine-based images for smaller size"
    else
        echo "  ⚠ Not using Alpine images (may result in larger image size)"
    fi
    
    # Check for non-root user
    if grep -q "USER appuser" ../src/TraliVali.Api/Dockerfile; then
        echo "  ✓ Running as non-root user (appuser)"
    else
        echo "  ✗ Not running as non-root user"
    fi
    
    # Check for health check in Dockerfile
    if grep -q "HEALTHCHECK" ../src/TraliVali.Api/Dockerfile; then
        echo "  ✓ Health check defined in Dockerfile"
    else
        echo "  ⚠ No health check in Dockerfile"
    fi
else
    echo "  ✗ Dockerfile not found"
    exit 1
fi

echo ""
echo "==================================="
echo "✓ Validation Complete!"
echo "==================================="
echo ""
echo "Next steps:"
echo "1. Copy .env.prod.example to .env.prod"
echo "2. Configure all environment variables in .env.prod"
echo "3. Generate JWT RSA keys"
echo "4. Build and test: docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build"
