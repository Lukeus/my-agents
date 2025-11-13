# Dapr Components and Configuration

This directory contains Dapr component definitions and configuration for the AI Agents framework.

## Directory Structure

```
dapr/
├── components/
│   ├── local/                    # Components for local development
│   │   ├── pubsub-redis.yaml
│   │   └── statestore-redis.yaml
│   ├── dev/                      # Components for dev environment
│   │   ├── pubsub-servicebus.yaml
│   │   └── statestore-cosmos.yaml
│   └── prod/                     # Components for production
│       ├── pubsub-servicebus.yaml
│       └── statestore-cosmos.yaml
├── configuration/
│   └── config.yaml              # Dapr configuration
└── README.md
```

## Components

### Pub/Sub Component (`agents-pubsub`)

**Local**: Redis-based pub/sub for development
- No authentication required
- Runs on localhost:6379

**Dev/Prod**: Azure Service Bus Topics
- Requires connection string from Azure Key Vault
- Configured with appropriate consumer IDs per environment

### State Store Component (`agents-statestore`)

**Local**: Redis-based state store for development
- No authentication required
- Runs on localhost:6379
- Supports actor state store

**Dev/Prod**: Azure Cosmos DB
- Requires endpoint and master key from Azure Key Vault
- Separate databases per environment
- Supports actor state store

## Configuration

The `config.yaml` file in the `configuration/` directory defines:
- OpenTelemetry tracing settings
- Metrics enablement
- Feature flags (Service Invocation, PubSub, State)
- Access control policies

## Usage

### Local Development

```bash
# Run with local components
dapr run --app-id notification-api --app-port 8080 --dapr-http-port 3500 --components-path ./dapr/components/local --config ./dapr/configuration/config.yaml
```

### Kubernetes Deployment

The components will be deployed as Kubernetes Custom Resources:

```bash
# Deploy dev components
kubectl apply -f dapr/components/dev/

# Deploy prod components
kubectl apply -f dapr/components/prod/

# Deploy configuration
kubectl apply -f dapr/configuration/
```

### Docker Compose

Components are mounted as volumes in the Dapr sidecar containers. See `docker-compose.dapr.yml`.

## Secrets Management

For dev and prod environments, secrets are managed via Kubernetes secrets:

```bash
# Create Azure Service Bus secret
kubectl create secret generic azure-servicebus \
  --from-literal=connectionString="<your-connection-string>"

# Create Azure Cosmos DB secret
kubectl create secret generic azure-cosmos \
  --from-literal=endpoint="<your-endpoint>" \
  --from-literal=masterKey="<your-master-key>"
```

## Testing Components

Test pub/sub:
```bash
# Publish message
dapr publish --publish-app-id notification-api --pubsub agents-pubsub --topic notificationsentevent --data '{"id":"123","message":"test"}'
```

Test state store:
```bash
# Save state
dapr invoke --app-id notification-api --method /state --data '{"key":"test","value":"hello"}'
```

## References

- [Dapr Components Documentation](https://docs.dapr.io/reference/components-reference/)
- [Redis Pub/Sub](https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-redis-pubsub/)
- [Azure Service Bus](https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-azure-servicebus/)
- [Cosmos DB State Store](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-azure-cosmosdb/)
