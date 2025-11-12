# Operations Runbook

This runbook provides operational procedures for maintaining, monitoring, and troubleshooting the AI Orchestration Multi-Agent Framework in production.

## Table of Contents

- [Overview](#overview)
- [Monitoring and Observability](#monitoring-and-observability)
- [Alerting](#alerting)
- [Incident Response](#incident-response)
- [Common Issues and Resolution](#common-issues-and-resolution)
- [Maintenance Procedures](#maintenance-procedures)
- [Performance Tuning](#performance-tuning)
- [Disaster Recovery](#disaster-recovery)
- [Security Operations](#security-operations)

## Overview

### System Health Indicators

| Indicator | Healthy | Warning | Critical |
|-----------|---------|---------|----------|
| API Response Time (p95) | < 500ms | 500ms-2s | > 2s |
| Error Rate | < 1% | 1-5% | > 5% |
| Pod CPU Usage | < 70% | 70-85% | > 85% |
| Pod Memory Usage | < 80% | 80-90% | > 90% |
| Event Processing Lag | < 1min | 1-5min | > 5min |
| LLM Token Usage | Within budget | 80-100% | > 100% |

### On-Call Responsibilities

- Monitor alerts and dashboards
- Respond to incidents within SLA
- Perform root cause analysis
- Document all incidents
- Execute emergency procedures

## Monitoring and Observability

### Key Dashboards

#### 1. System Overview Dashboard

**Access**: Grafana → Dashboards → Agent System Overview

**Key Metrics**:
- Overall system health status
- Request rate across all agents
- Error rate by agent
- p50/p95/p99 latency
- Active pods per agent
- Resource utilization

**Query Examples**:
```promql
# Request rate per agent
rate(http_requests_total{job="agent-api"}[5m])

# Error rate
rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m])

# p95 latency
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))
```

#### 2. Agent Performance Dashboard

**Metrics per Agent**:
- Request throughput
- Success/failure ratio
- LLM invocation count
- Prompt loading time
- Event publishing success rate

```promql
# LLM invocation duration
histogram_quantile(0.95, rate(llm_invocation_duration_seconds_bucket{agent="notification"}[5m]))

# Event publishing failures
rate(event_publish_failures_total{agent="notification"}[5m])
```

#### 3. Infrastructure Dashboard

**Kubernetes Metrics**:
- Pod status (running/pending/failed)
- Node CPU/memory utilization
- Persistent volume usage
- Network I/O
- Container restarts

```bash
# View in kubectl
kubectl top nodes
kubectl top pods -n agents-dev
kubectl get pods -n agents-dev --field-selector=status.phase!=Running
```

#### 4. Cost Dashboard

**Track**:
- Azure OpenAI token usage
- Compute costs (AKS nodes)
- Storage costs (Cosmos DB, Azure SQL)
- Event infrastructure costs
- Total daily/monthly spend

### Log Aggregation

**Application Insights Queries**:

```kusto
// Error logs in last hour
traces
| where timestamp > ago(1h)
| where severityLevel >= 3
| project timestamp, message, customDimensions
| order by timestamp desc

// Slow requests
requests
| where duration > 2000
| project timestamp, name, duration, resultCode
| order by duration desc

// Failed LLM invocations
dependencies
| where type == "LLM"
| where success == false
| project timestamp, target, resultCode, duration
```

**Search Logs by Correlation ID**:

```kusto
union traces, requests, dependencies
| where customDimensions.CorrelationId == "guid-here"
| project timestamp, itemType, message, name
| order by timestamp asc
```

### Distributed Tracing

**OpenTelemetry Traces**:

View end-to-end request flow:
1. API request received
2. Prompt loaded
3. LLM invoked
4. Event published
5. Response returned

**Query by Operation**:
```kusto
traces
| where operation_Name == "NotificationAgent.Execute"
| project timestamp, operation_Id, message, duration
```

## Alerting

### Critical Alerts

#### High Error Rate

**Trigger**: Error rate > 5% for 5 minutes
**Severity**: P1
**Notification**: PagerDuty + Teams

**Response**:
1. Check error logs for patterns
2. Identify failing agent
3. Check LLM provider status
4. Review recent deployments
5. Rollback if necessary

```powershell
# Check recent errors
kubectl logs -n agents-dev deployment/notification-agent --tail=100 | grep -i error

# Check LLM connectivity
kubectl exec -it -n agents-dev deployment/notification-agent -- curl -v https://oai-agents.openai.azure.com/
```

#### High Latency

**Trigger**: p95 latency > 2s for 5 minutes
**Severity**: P1
**Notification**: PagerDuty + Teams

**Response**:
1. Check CPU/memory utilization
2. Review LLM response times
3. Check for database slow queries
4. Scale pods if needed
5. Optimize prompts if LLM is slow

```powershell
# Scale up pods
kubectl scale deployment notification-agent --replicas=5 -n agents-dev

# Check resource usage
kubectl top pods -n agents-dev
```

#### Pod Crashes

**Trigger**: Container restart > 3 in 10 minutes
**Severity**: P1
**Notification**: PagerDuty

**Response**:
1. Check pod logs for crash reason
2. Review resource limits
3. Check for OOM kills
4. Inspect recent configuration changes

```powershell
# Check crash logs
kubectl logs -n agents-dev <pod-name> --previous

# Check pod events
kubectl describe pod -n agents-dev <pod-name>

# Check OOM kills
kubectl get pod -n agents-dev <pod-name> -o jsonpath='{.status.containerStatuses[0].lastState}'
```

### Warning Alerts

#### Elevated Response Time

**Trigger**: p95 latency > 1s for 10 minutes
**Severity**: P2
**Notification**: Teams

#### High Resource Usage

**Trigger**: CPU > 85% or Memory > 90% for 10 minutes
**Severity**: P2
**Notification**: Teams

#### Event Processing Lag

**Trigger**: Event queue depth > 1000 messages
**Severity**: P2
**Notification**: Teams

### Budget Alerts

#### LLM Token Usage

**Trigger**: Token usage > 80% of daily budget
**Severity**: P2
**Notification**: Teams + Email

**Response**:
1. Review token consumption by agent
2. Identify prompts using excessive tokens
3. Implement caching for repeated queries
4. Consider switching to cheaper model for dev/test

## Incident Response

### Severity Levels

| Level | Description | Response Time | Examples |
|-------|-------------|---------------|----------|
| P0 | Critical - System Down | 15 minutes | All agents unavailable, data loss |
| P1 | High - Major Impact | 30 minutes | Single agent down, high error rate |
| P2 | Medium - Degraded Service | 2 hours | High latency, minor feature broken |
| P3 | Low - Minor Issue | Next business day | Cosmetic issues, enhancement requests |

### Incident Response Workflow

#### 1. Detection
- Alert triggers
- User report
- Monitoring dashboard shows anomaly

#### 2. Assessment
```powershell
# Quick health check
kubectl get pods -n agents-dev
kubectl get svc -n agents-dev
curl https://agents.yourdomain.com/api/notification/health

# Check recent changes
kubectl rollout history deployment/notification-agent -n agents-dev
az deployment group list --resource-group rg-agents-dev --query "[0]"
```

#### 3. Containment
- Stop deployment if in progress
- Rollback if recent deployment caused issue
- Scale resources if capacity issue
- Isolate affected component

#### 4. Resolution
- Apply fix or workaround
- Validate fix in staging first (if possible)
- Deploy to production
- Monitor for stability

#### 5. Post-Mortem
- Document incident timeline
- Identify root cause
- Preventive measures
- Update runbooks

### Emergency Procedures

#### Complete System Outage

```powershell
# 1. Check AKS cluster health
az aks show --resource-group rg-agents-dev --name aks-agents-dev --query powerState

# 2. Check all pods
kubectl get pods -n agents-dev --all-namespaces

# 3. Check critical Azure services
az resource list --resource-group rg-agents-dev --output table

# 4. Restart all deployments (last resort)
kubectl rollout restart deployment -n agents-dev
```

#### Database Connection Failure

```powershell
# 1. Check Cosmos DB status
az cosmosdb show --resource-group rg-agents-dev --name cosmos-agents-dev --query "readLocations[0].failoverPriority"

# 2. Check Azure SQL
az sql db show --resource-group rg-agents-dev --server sql-agents-dev --name AgentsDB --query status

# 3. Test connectivity from pod
kubectl exec -it -n agents-dev deployment/notification-agent -- /bin/sh
# Inside pod: test connection
```

#### LLM Provider Failure

```powershell
# 1. Check Azure OpenAI status
az cognitiveservices account show --resource-group rg-agents-dev --name oai-agents-dev --query "properties.provisioningState"

# 2. Switch to backup deployment (if configured)
kubectl set env deployment/notification-agent -n agents-dev \
  LLMProvider__AzureOpenAI__Endpoint="https://oai-agents-backup.openai.azure.com/"

# 3. Fallback to Ollama (temporary)
kubectl set env deployment/notification-agent -n agents-dev \
  LLMProvider__ProviderType="Ollama"
```

## Common Issues and Resolution

### Issue: Agent Returning 503 Service Unavailable

**Symptoms**: API returns 503, pods are running

**Diagnosis**:
```powershell
# Check pod readiness
kubectl get pods -n agents-dev -o wide

# Check readiness probe
kubectl describe pod -n agents-dev <pod-name> | grep -A 10 Readiness
```

**Resolution**:
- Increase `initialDelaySeconds` for readiness probe
- Check application startup time
- Verify dependencies (database, LLM) are reachable

### Issue: High Memory Usage / OOMKilled

**Symptoms**: Pods restarting frequently, OOMKilled in events

**Diagnosis**:
```powershell
# Check memory limits
kubectl describe pod -n agents-dev <pod-name> | grep -A 5 Limits

# Check actual usage
kubectl top pod -n agents-dev <pod-name>
```

**Resolution**:
```yaml
# Increase memory limits
resources:
  limits:
    memory: "1Gi"  # Increase from 512Mi
  requests:
    memory: "512Mi"
```

### Issue: Slow LLM Response Times

**Symptoms**: High p95 latency, timeouts

**Diagnosis**:
- Check Azure OpenAI metrics in Azure Portal
- Review prompt complexity and length
- Check token usage

**Resolution**:
1. Optimize prompts to reduce tokens
2. Enable prompt caching
3. Increase timeout values
4. Scale Azure OpenAI deployment (more TPM)

### Issue: Event Publishing Failures

**Symptoms**: Events not reaching subscribers

**Diagnosis**:
```powershell
# Check Event Grid topic
az eventgrid topic show --resource-group rg-agents-dev --name topic-agents-dev

# Check dead letter queue
az eventgrid event-subscription show \
  --name sub-notification \
  --source-resource-id /subscriptions/.../topics/topic-agents-dev \
  --query deadLetterDestination
```

**Resolution**:
- Verify Event Grid credentials
- Check network connectivity
- Review subscriber endpoint configuration
- Process dead letter queue messages

### Issue: Certificate Expiration

**Symptoms**: HTTPS errors, browser warnings

**Diagnosis**:
```powershell
# Check certificate expiration
kubectl get certificate -n agents-dev
kubectl describe certificate agents-tls -n agents-dev
```

**Resolution**:
```powershell
# Manual renewal (cert-manager should auto-renew)
kubectl delete certificate agents-tls -n agents-dev
kubectl apply -f k8s/base/ingress.yaml
```

## Maintenance Procedures

### Regular Maintenance Schedule

| Task | Frequency | Owner |
|------|-----------|-------|
| Review and rotate secrets | Monthly | DevOps |
| Update dependencies (NuGet packages) | Quarterly | Dev Team |
| Review and optimize costs | Monthly | Operations |
| Backup validation | Weekly | Operations |
| Security patching | As needed | DevOps |
| Performance review | Monthly | Dev Team + SRE |
| Capacity planning | Quarterly | Operations |

### Scaling Agents

**Manual Scaling**:
```powershell
# Scale specific agent
kubectl scale deployment notification-agent --replicas=5 -n agents-dev

# Scale all agents
kubectl scale deployment --all --replicas=3 -n agents-dev
```

**Adjust HPA Limits**:
```yaml
spec:
  minReplicas: 3
  maxReplicas: 15
```

### Updating Agent Version

```powershell
# 1. Build and push new image
docker build -t acragentsdev.azurecr.io/agents-api-notification:1.1.0 .
docker push acragentsdev.azurecr.io/agents-api-notification:1.1.0

# 2. Update deployment
kubectl set image deployment/notification-agent \
  notification-agent=acragentsdev.azurecr.io/agents-api-notification:1.1.0 \
  -n agents-dev

# 3. Monitor rollout
kubectl rollout status deployment/notification-agent -n agents-dev

# 4. Verify
kubectl get pods -n agents-dev
```

### Secret Rotation

```powershell
# 1. Update in Key Vault
az keyvault secret set \
  --vault-name kv-agents-dev \
  --name "AzureOpenAI--ApiKey" \
  --value "new-api-key"

# 2. Restart pods to pick up new secret (if using CSI driver)
kubectl rollout restart deployment -n agents-dev

# 3. Verify
kubectl logs -n agents-dev deployment/notification-agent | grep "initialized"
```

### Database Maintenance

**Cosmos DB**:
```powershell
# Check indexing policy
az cosmosdb sql container show \
  --resource-group rg-agents-dev \
  --account-name cosmos-agents-dev \
  --database-name AgentsDB \
  --name Events

# Update throughput
az cosmosdb sql container throughput update \
  --resource-group rg-agents-dev \
  --account-name cosmos-agents-dev \
  --database-name AgentsDB \
  --name Events \
  --throughput 800
```

**Azure SQL**:
```sql
-- Update statistics
EXEC sp_updatestats;

-- Rebuild fragmented indexes
ALTER INDEX ALL ON AgentExecutions REBUILD;

-- Check query performance
SELECT TOP 10 
    qs.execution_count,
    qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time,
    qt.text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
ORDER BY avg_elapsed_time DESC;
```

## Performance Tuning

### Prompt Optimization

1. **Reduce Token Count**: Shorten prompts without losing clarity
2. **Cache Prompts**: Enable prompt caching in infrastructure
3. **Batch Requests**: Group similar requests when possible

### Resource Optimization

```yaml
# Right-size resources based on actual usage
resources:
  requests:
    memory: "256Mi"  # Start conservative
    cpu: "250m"
  limits:
    memory: "512Mi"  # Leave headroom
    cpu: "500m"
```

### Connection Pooling

```csharp
// Configure SQL connection pooling
services.AddDbContext<AgentDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MinBatchSize(10);
        sqlOptions.MaxBatchSize(100);
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
    }));
```

## Disaster Recovery

### Backup Procedures

**Automated Backups**:
- Cosmos DB: Continuous backup (automatic)
- Azure SQL: Automated backups (7-35 days retention)
- Configuration: GitOps (infrastructure as code)

**Manual Backup**:
```powershell
# Backup Kubernetes resources
kubectl get all -n agents-dev -o yaml > backup-agents-dev.yaml

# Backup ConfigMaps and Secrets
kubectl get configmaps -n agents-dev -o yaml > backup-configmaps.yaml
kubectl get secrets -n agents-dev -o yaml > backup-secrets.yaml
```

### Recovery Procedures

**Restore from Backup**:
```powershell
# 1. Restore Azure infrastructure
az deployment group create \
  --resource-group rg-agents-dev \
  --template-file main.bicep \
  --parameters @parameters/dev.parameters.json

# 2. Restore Kubernetes resources
kubectl apply -f backup-agents-dev.yaml

# 3. Verify
kubectl get pods -n agents-dev
```

**Point-in-Time Recovery (Azure SQL)**:
```powershell
az sql db restore \
  --resource-group rg-agents-dev \
  --server sql-agents-dev \
  --name AgentsDB \
  --dest-name AgentsDB-Restored \
  --time "2025-11-11T10:00:00Z"
```

## Security Operations

### Security Monitoring

- Review Azure Security Center recommendations weekly
- Monitor Key Vault access logs
- Track failed authentication attempts
- Audit RBAC changes

### Vulnerability Management

```powershell
# Scan container images
az acr task run \
  --registry acragentsdev \
  --name security-scan

# Check for vulnerabilities
trivy image acragentsdev.azurecr.io/agents-api-notification:1.0.0
```

### Security Incident Response

1. **Isolate**: Stop affected services
2. **Investigate**: Review logs and access patterns
3. **Remediate**: Patch vulnerabilities, rotate credentials
4. **Document**: Post-incident report
5. **Prevent**: Update security controls

## Further Reading

- [Architecture Overview](architecture.md)
- [Agent Development Guide](agent-development.md)
- [Prompt Authoring Guide](prompt-authoring.md)
- [Deployment Guide](deployment.md)
