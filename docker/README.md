# Docker Deployment Guide

This directory contains Docker-related files for building and running the AI Agents microservices.

## Contents

- `Dockerfile.template` - Template Dockerfile for creating service-specific Dockerfiles
- Individual Dockerfiles are located in each API project directory

## Building Images

### Build Single Image

```bash
# From repository root
docker build -f src/Presentation/Agents.API.Notification/Dockerfile -t agents-notification-api:latest .
```

### Build All Images

```bash
# Build all services
docker build -f src/Presentation/Agents.API.Notification/Dockerfile -t agents-notification-api:latest .
docker build -f src/Presentation/Agents.API.DevOps/Dockerfile -t agents-devops-api:latest .
docker build -f src/Presentation/Agents.API.TestPlanning/Dockerfile -t agents-testplanning-api:latest .
docker build -f src/Presentation/Agents.API.Implementation/Dockerfile -t agents-implementation-api:latest .
docker build -f src/Presentation/Agents.API.ServiceDesk/Dockerfile -t agents-servicedesk-api:latest .
```

### Build with Docker Compose

```bash
# From repository root
docker-compose build
```

## Running Locally

### Using Docker Compose

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### Access Services

Once running, services are available at:
- **Notification API**: http://localhost:5001/swagger
- **DevOps API**: http://localhost:5002/swagger
- **TestPlanning API**: http://localhost:5003/swagger
- **Implementation API**: http://localhost:5004/swagger
- **ServiceDesk API**: http://localhost:5005/swagger
- **SQL Server**: localhost:1433 (sa/YourStrong@Passw0rd)
- **Ollama**: http://localhost:11434

### Pull Ollama Model

After starting Ollama container:

```bash
# Pull a model (e.g., llama3)
docker exec -it agents-ollama-1 ollama pull llama3

# Or from host
curl http://localhost:11434/api/pull -d '{"name":"llama3"}'

# List available models
docker exec -it agents-ollama-1 ollama list
```

## Docker Image Optimization

### Multi-Stage Build Benefits

Our Dockerfiles use multi-stage builds:
1. **Build Stage**: Uses SDK image to build application
2. **Publish Stage**: Publishes optimized release build
3. **Runtime Stage**: Uses minimal runtime image with only published artifacts

This reduces final image size by ~70% compared to single-stage builds.

### Image Sizes (Approximate)

- **Notification API**: ~220 MB
- **DevOps API**: ~215 MB  
- **TestPlanning API**: ~215 MB
- **Implementation API**: ~215 MB
- **ServiceDesk API**: ~215 MB

### Layer Caching

Dockerfiles are optimized for layer caching:
1. Solution and project files copied first
2. Dependencies restored (cached if unchanged)
3. Source code copied last
4. Build and publish

This speeds up subsequent builds significantly.

## Security Features

### Non-Root User

All containers run as non-root user `appuser` (UID 1000) for security.

### Health Checks

Built-in health checks:
- Interval: 30 seconds
- Timeout: 3 seconds
- Start period: 10 seconds
- Retries: 3

### Minimal Base Images

Using Alpine Linux-based runtime images for smaller attack surface.

### No Diagnostics in Production

`DOTNET_EnableDiagnostics=0` disables debugging features in production.

## Pushing to Azure Container Registry

### Login to ACR

```bash
# Azure CLI
az acr login --name <ACR_NAME>

# Or Docker
docker login <ACR_NAME>.azurecr.io -u <USERNAME> -p <PASSWORD>
```

### Tag and Push

```bash
# Tag images
docker tag agents-notification-api:latest <ACR_NAME>.azurecr.io/agents-notification-api:latest
docker tag agents-notification-api:latest <ACR_NAME>.azurecr.io/agents-notification-api:v1.0.0

# Push images
docker push <ACR_NAME>.azurecr.io/agents-notification-api:latest
docker push <ACR_NAME>.azurecr.io/agents-notification-api:v1.0.0
```

### Push All Services

```bash
#!/bin/bash
ACR_NAME="myacr"
VERSION="v1.0.0"

for service in notification devops testplanning implementation servicedesk; do
  echo "Pushing agents-${service}-api..."
  docker tag agents-${service}-api:latest ${ACR_NAME}.azurecr.io/agents-${service}-api:latest
  docker tag agents-${service}-api:latest ${ACR_NAME}.azurecr.io/agents-${service}-api:${VERSION}
  docker push ${ACR_NAME}.azurecr.io/agents-${service}-api:latest
  docker push ${ACR_NAME}.azurecr.io/agents-${service}-api:${VERSION}
done
```

## Scanning for Vulnerabilities

### Using Trivy

```bash
# Install Trivy (Linux/macOS)
curl -sfL https://raw.githubusercontent.com/aquasecurity/trivy/main/contrib/install.sh | sh -s -- -b /usr/local/bin

# Scan image
trivy image agents-notification-api:latest

# Scan for critical/high severity only
trivy image --severity CRITICAL,HIGH agents-notification-api:latest

# Generate SARIF report
trivy image --format sarif --output results.sarif agents-notification-api:latest
```

### Using Docker Scout

```bash
# Enable Docker Scout
docker scout quickview agents-notification-api:latest

# View CVEs
docker scout cves agents-notification-api:latest

# Compare with base image
docker scout compare --to mcr.microsoft.com/dotnet/aspnet:9.0-alpine agents-notification-api:latest
```

## Troubleshooting

### Build Failures

```bash
# Check Docker version
docker --version

# Verify Docker daemon is running
docker ps

# Clean build cache
docker builder prune -a

# Check disk space
docker system df
```

### Container Won't Start

```bash
# View container logs
docker logs <container_id>

# Inspect container
docker inspect <container_id>

# Check health status
docker ps --filter health=unhealthy
```

### Connection Issues

```bash
# Test network connectivity
docker network inspect agents-network

# Check if ports are exposed
docker port <container_id>

# Test from another container
docker run --rm --network agents-network curlimages/curl curl http://notification-api:8080/health
```

### Database Connection Issues

```bash
# Check SQL Server logs
docker logs agents-sqlserver-1

# Test SQL connection
docker exec -it agents-sqlserver-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT @@VERSION"

# Verify connection string in app
docker exec -it agents-notification-api-1 env | grep ConnectionStrings
```

## Environment Variables

### Common Variables

- `ASPNETCORE_ENVIRONMENT`: Development, Staging, Production
- `ASPNETCORE_URLS`: Listening URLs (default: http://+:8080)
- `DOTNET_EnableDiagnostics`: Enable/disable diagnostics (0 or 1)

### LLM Provider

```bash
# Azure OpenAI
LLMProvider__ProviderType=AzureOpenAI
LLMProvider__AzureOpenAI__Endpoint=https://your-instance.openai.azure.com
LLMProvider__AzureOpenAI__ApiKey=<api-key>
LLMProvider__AzureOpenAI__DeploymentName=gpt-4

# Ollama (local)
LLMProvider__ProviderType=Ollama
LLMProvider__Ollama__Endpoint=http://ollama:11434
```

## Performance Tuning

### Adjust Resource Limits

Edit `docker-compose.yml`:

```yaml
services:
  notification-api:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
```

### Enable BuildKit

```bash
# Enable BuildKit for faster builds
export DOCKER_BUILDKIT=1

# Or in docker-compose.yml
export COMPOSE_DOCKER_CLI_BUILD=1
export DOCKER_BUILDKIT=1

docker-compose build
```

## Cleaning Up

```bash
# Stop and remove containers
docker-compose down

# Remove volumes
docker-compose down -v

# Remove images
docker rmi agents-notification-api agents-devops-api agents-testplanning-api agents-implementation-api agents-servicedesk-api

# Clean everything
docker system prune -a --volumes
```

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Documentation](https://learn.microsoft.com/en-us/dotnet/core/docker/introduction)
- [Ollama Documentation](https://github.com/ollama/ollama)
