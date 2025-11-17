using Prometheus;

namespace Agents.Infrastructure.Observability.Metrics;

/// <summary>
/// Collects and exposes Prometheus metrics for AI agent operations
/// </summary>
public static class AgentMetrics
{
    private static readonly string[] _agentNameLabel = { "agent_name" };
    private static readonly string[] _agentAndOperationLabels = { "agent_name", "operation" };
    private static readonly string[] _agentOperationStatusLabels = { "agent_name", "operation", "status" };
    private static readonly string[] _channelLabels = { "agent_name", "channel" };
    private static readonly string[] _llmLabels = { "agent_name", "model", "provider" };

    // Agent operation counters
    public static readonly Counter AgentOperationsTotal = Prometheus.Metrics
        .CreateCounter(
            "agent_operations_total",
            "Total number of agent operations",
            _agentAndOperationLabels);

    public static readonly Counter AgentOperationsSuccessTotal = Prometheus.Metrics
        .CreateCounter(
            "agent_operations_success_total",
            "Total number of successful agent operations",
            _agentAndOperationLabels);

    public static readonly Counter AgentOperationsErrorTotal = Prometheus.Metrics
        .CreateCounter(
            "agent_operations_error_total",
            "Total number of failed agent operations",
            _agentOperationStatusLabels);

    // Agent operation duration
    public static readonly Histogram AgentOperationDuration = Prometheus.Metrics
        .CreateHistogram(
            "agent_operation_duration_seconds",
            "Duration of agent operations in seconds",
            new HistogramConfiguration
            {
                LabelNames = _agentAndOperationLabels,
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10) // 10ms to ~10s
            });

    // LLM-specific metrics
    public static readonly Counter LlmCallsTotal = Prometheus.Metrics
        .CreateCounter(
            "llm_calls_total",
            "Total number of LLM API calls",
            _llmLabels);

    public static readonly Counter LlmTokensUsedTotal = Prometheus.Metrics
        .CreateCounter(
            "llm_tokens_used_total",
            "Total number of LLM tokens consumed",
            _llmLabels);

    public static readonly Histogram LlmCallDuration = Prometheus.Metrics
        .CreateHistogram(
            "llm_call_duration_seconds",
            "Duration of LLM API calls in seconds",
            new HistogramConfiguration
            {
                LabelNames = _llmLabels,
                Buckets = Histogram.ExponentialBuckets(0.1, 2, 10) // 100ms to ~100s
            });

    public static readonly Counter LlmErrorsTotal = Prometheus.Metrics
        .CreateCounter(
            "llm_errors_total",
            "Total number of LLM API errors",
            new[] { "agent_name", "model", "provider", "error_type" });

    // Notification-specific metrics
    public static readonly Counter NotificationsSentTotal = Prometheus.Metrics
        .CreateCounter(
            "notifications_sent_total",
            "Total number of notifications sent",
            _channelLabels);

    public static readonly Counter NotificationsFailedTotal = Prometheus.Metrics
        .CreateCounter(
            "notifications_failed_total",
            "Total number of failed notifications",
            new[] { "agent_name", "channel", "error_type" });

    public static readonly Histogram NotificationSendDuration = Prometheus.Metrics
        .CreateHistogram(
            "notification_send_duration_seconds",
            "Duration of notification send operations",
            new HistogramConfiguration
            {
                LabelNames = _channelLabels,
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });

    // Event processing metrics
    public static readonly Counter EventsPublishedTotal = Prometheus.Metrics
        .CreateCounter(
            "events_published_total",
            "Total number of events published",
            new[] { "agent_name", "event_type", "destination" });

    public static readonly Counter EventsReceivedTotal = Prometheus.Metrics
        .CreateCounter(
            "events_received_total",
            "Total number of events received",
            new[] { "agent_name", "event_type", "source" });

    public static readonly Counter EventProcessingErrorsTotal = Prometheus.Metrics
        .CreateCounter(
            "event_processing_errors_total",
            "Total number of event processing errors",
            new[] { "agent_name", "event_type", "error_type" });

    public static readonly Histogram EventProcessingDuration = Prometheus.Metrics
        .CreateHistogram(
            "event_processing_duration_seconds",
            "Duration of event processing in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "agent_name", "event_type" },
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });

    // Database metrics
    public static readonly Counter DatabaseQueriesTotal = Prometheus.Metrics
        .CreateCounter(
            "database_queries_total",
            "Total number of database queries",
            new[] { "agent_name", "operation_type" });

    public static readonly Histogram DatabaseQueryDuration = Prometheus.Metrics
        .CreateHistogram(
            "database_query_duration_seconds",
            "Duration of database queries in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "agent_name", "operation_type" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // 1ms to ~1s
            });

    // Active requests gauge
    public static readonly Gauge ActiveRequests = Prometheus.Metrics
        .CreateGauge(
            "agent_active_requests",
            "Number of active agent requests",
            _agentNameLabel);

    // Memory and performance
    public static readonly Gauge AgentMemoryUsageBytes = Prometheus.Metrics
        .CreateGauge(
            "agent_memory_usage_bytes",
            "Agent memory usage in bytes",
            _agentNameLabel);
}
