# Monitoring and Observability

This directory contains Kubernetes manifests and configurations for monitoring and observability of the AI Agent Orchestration Platform.

## Components

### Prometheus
Metrics collection and storage system.

**Features:**
- Service discovery for all agent services
- 15-day retention period
- 50Gi persistent storage
- Scrapes metrics every 30 seconds
- RBAC configuration for Kubernetes service discovery

**Metrics Exposed:**
- `agent_operations_total` - Total agent operations by type and status
- `agent_operation_duration_seconds` - Operation duration histogram
- `agent_llm_calls_total` - LLM API calls by model and status
- `agent_llm_tokens_total` - Token usage by model and type (prompt/completion)
- `agent_notifications_total` - Notifications by channel and status
- `agent_events_published_total` - Domain events published by type
- `agent_events_consumed_total` - Domain events consumed by type
- `agent_database_queries_total` - Database queries by operation
- `agent_active_requests` - Current active HTTP requests
- `agent_memory_bytes` - Memory usage by agent type
- Plus standard .NET metrics from prometheus-net

**Access:**
```bash
# Port-forward to access Prometheus UI
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# Open http://localhost:9090
```

### Grafana
Visualization and dashboards for metrics and logs.

**Features:**
- Pre-configured Prometheus datasource
- Agent overview dashboard with 8 panels
- 10Gi persistent storage for dashboards and settings
- Admin credentials in ConfigMap (change in production!)

**Dashboards:**
- **Agents Overview** - System-wide metrics including:
  - Agent operations rate and success rate
  - LLM token usage trends
  - Notification delivery rates
  - Event publishing/consumption rates
  - Database query performance
  - Active requests and memory usage

**Access:**
```bash
# Port-forward to access Grafana UI
kubectl port-forward -n monitoring svc/grafana 3000:3000
# Open http://localhost:3000
# Default credentials: admin/admin
```

## Deployment

### Deploy All Monitoring Components
```bash
# Create monitoring namespace
kubectl create namespace monitoring

# Deploy Prometheus
kubectl apply -f k8s/monitoring/prometheus.yaml

# Deploy Grafana
kubectl apply -f k8s/monitoring/grafana.yaml

# Verify deployments
kubectl get pods -n monitoring
kubectl get svc -n monitoring
kubectl get pvc -n monitoring
```

### Verify Metrics Collection
```bash
# Check Prometheus targets
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# Navigate to http://localhost:9090/targets

# All agent service endpoints should be UP:
# - agent-api:8080/metrics
# - notification-api:8080/metrics
# - workflow-api:8080/metrics
# - orchestration-api:8080/metrics
# - integration-api:8080/metrics
```

## OpenTelemetry Tracing

All services export distributed traces with:
- HTTP request/response spans
- Database operation spans
- LLM API call spans
- Custom agent operation spans

**Trace Context:**
- Trace ID propagated via W3C Trace Context headers
- Span IDs for parent-child relationships
- Correlation with logs via trace ID

**Export:**
Currently configured for console export. To enable Jaeger/Zipkin:

```csharp
// In ObservabilityExtensions.cs
builder.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://jaeger:4317");
});
```

## Structured Logging

All services use Serilog with structured logging:

**Log Enrichment:**
- Machine name
- Thread ID
- Environment name
- Correlation ID (from trace context)

**Log Levels:**
- Information: API requests, agent operations, notifications
- Warning: Retries, fallbacks, validation failures
- Error: Exceptions, failed operations, integration errors

**Sinks:**
- Console (JSON format for Kubernetes)
- Application Insights (Azure Monitor)

**Query Logs:**
```bash
# Stream logs from all agent services
kubectl logs -n agents -l app=agent-api --tail=100 -f

# Query logs in Application Insights
traces
| where cloud_RoleName startswith "agent-"
| where timestamp > ago(1h)
| project timestamp, message, severityLevel, customDimensions
| order by timestamp desc
```

## Event-Driven Monitoring

### Azure Event Hub
Used for high-throughput domain event publishing.

**Configuration:**
```json
{
  "EventProvider": "EventHub",
  "EventHub": {
    "ConnectionString": "Endpoint=sb://...",
    "EventHubName": "agent-events"
  }
}
```

**Metrics:**
- Events published per second
- Batch sizes
- Publishing latency

### Azure Service Bus
Used for reliable event consumption with dead-letter queues.

**Configuration:**
```json
{
  "EventProvider": "ServiceBus",
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://...",
    "TopicPrefix": "agents"
  }
}
```

**Topics:**
- `agents.agents` - Agent domain events
- `agents.notifications` - Notification events
- `agents.workflows` - Workflow events
- `agents.integrations` - Integration events

**Monitoring:**
```bash
# Check Service Bus metrics in Azure Portal
az servicebus topic show --resource-group agents-rg \
  --namespace-name agents-sb --name agents.agents \
  --query "countDetails"
```

## Alerts (Future Phase)

Recommended alerts to configure:
- High error rate (>5% of operations failing)
- LLM token usage exceeding budget
- Notification delivery failures
- Database query slow performance (>1s)
- Memory usage exceeding 80%
- Event publishing failures
- Pod restarts or crash loops

## Production Considerations

1. **Prometheus:**
   - Increase retention period for production
   - Configure remote write to long-term storage (Thanos, Cortex, Azure Monitor)
   - Set up high availability with multiple replicas

2. **Grafana:**
   - Use secure secret management for admin credentials
   - Configure OAuth/SSO for user authentication
   - Set up alert notification channels (email, Slack, PagerDuty)

3. **Logs:**
   - Centralize logs in Azure Monitor or ELK stack
   - Configure log retention policies
   - Set up log-based alerts

4. **Traces:**
   - Deploy Jaeger or use Azure Monitor for trace storage
   - Configure sampling rates for high-volume services
   - Set trace retention policies

5. **Events:**
   - Monitor Event Hub/Service Bus quotas and throttling
   - Configure dead-letter queue monitoring
   - Set up alerts for event processing delays

## Troubleshooting

### No Metrics in Prometheus
```bash
# Check Prometheus config
kubectl exec -n monitoring prometheus-0 -- cat /etc/prometheus/prometheus.yml

# Check service discovery
kubectl get endpoints -n agents

# Verify metrics endpoint
kubectl port-forward -n agents svc/agent-api 8080:8080
curl http://localhost:8080/metrics
```

### Grafana Dashboard Not Loading
```bash
# Check datasource configuration
kubectl logs -n monitoring deployment/grafana

# Verify Prometheus connection
kubectl exec -n monitoring deployment/grafana -- \
  curl http://prometheus:9090/api/v1/query?query=up
```

### Missing Traces
- Verify OpenTelemetry exporter is configured
- Check trace sampling rate (default: 100%)
- Ensure W3C Trace Context headers are propagated

### Event Publishing Failures
- Verify Azure Event Hub/Service Bus connection strings
- Check namespace permissions and access policies
- Monitor Azure service health and quotas

## References

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Serilog Documentation](https://serilog.net/)
- [Azure Event Hubs](https://learn.microsoft.com/azure/event-hubs/)
- [Azure Service Bus](https://learn.microsoft.com/azure/service-bus-messaging/)
