# Error Handling and Resilience Patterns

## Overview

This document describes the error handling strategy and resilience patterns implemented in the my-agents multi-agent framework.

---

## Error Handling Strategy

### Layered Approach

The framework uses a layered error handling approach aligned with Clean Architecture:

```
┌─────────────────────────────────────────┐
│  Presentation Layer (APIs)              │
│  - Global exception handling            │
│  - HTTP status code mapping             │
│  - Client-friendly error messages       │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│  Application Layer (Agents)             │
│  - AgentResult success/failure          │
│  - Structured error metadata            │
│  - Correlation tracking                 │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│  Infrastructure Layer                   │
│  - Retry policies (Polly)               │
│  - Circuit breakers                     │
│  - Timeouts                             │
│  - Logging with context                 │
└─────────────────────────────────────────┘
```

---

## Agent Error Handling

### AgentResult Pattern

All agents return `AgentResult` which provides structured success/failure information:

```csharp
// Success
return AgentResult.Success(
    "Operation completed successfully",
    new Dictionary<string, object>
    {
        ["ResultId"] = id,
        ["ProcessedItems"] = count
    });

// Failure
return AgentResult.Failure(
    "Operation failed: validation error",
    new Dictionary<string, object>
    {
        ["ErrorCode"] = "VALIDATION_001",
        ["FailedField"] = "email",
        ["CanRetry"] = false
    });
```

### BaseAgent Exception Handling

The `BaseAgent.ExecuteAsync` method implements comprehensive exception handling:

```csharp
catch (Exception ex)
{
    stopwatch.Stop();
    
    Logger.LogError(ex,
        "Agent {AgentName} failed. " +
        "ExecutionId: {ExecutionId}, " +
        "CorrelationId: {CorrelationId}, " +
        "InitiatedBy: {InitiatedBy}, " +
        "Duration: {Duration}, " +
        "InputPreview: {InputPreview}, " +
        "InnerException: {InnerException}, " +
        "StackTrace: {StackTrace}",
        AgentName,
        context.ExecutionId,
        context.CorrelationId,
        context.InitiatedBy,
        stopwatch.Elapsed,
        input?.Substring(0, Math.Min(input.Length, 100)),
        ex.InnerException?.Message,
        ex.StackTrace);

    return AgentResult.Failure(
        $"Agent execution failed: {ex.Message}",
        new Dictionary<string, object>
        {
            ["Exception"] = ex.GetType().FullName,
            ["ExceptionMessage"] = ex.Message,
            ["InnerException"] = ex.InnerException?.Message,
            ["StackTrace"] = ex.StackTrace,
            ["Duration"] = stopwatch.Elapsed,
            ["CorrelationId"] = context.CorrelationId,
            ["ExecutionId"] = context.ExecutionId,
            ["AgentName"] = AgentName
        });
}
```

**Key Features**:
- Comprehensive context logging
- Execution duration tracking
- Input preview (first 100 chars)
- Full exception details
- Correlation ID tracking
- No exceptions thrown (graceful degradation)

---

## Resilience Patterns with Polly

### 1. Retry with Exponential Backoff

**Location**: `DaprEventPublisher`

```csharp
_retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            _logger.LogWarning(exception,
                "Retry {RetryCount} after {Delay}s for event publishing",
                retryCount,
                timeSpan.TotalSeconds);
        });
```

**Behavior**:
- Retry #1: Wait 2 seconds
- Retry #2: Wait 4 seconds  
- Retry #3: Wait 8 seconds
- Total max delay: 14 seconds

**Use Cases**:
- Transient network failures
- Temporary service unavailability
- Rate limiting

### 2. Timeout Policy

**Location**: `BaseAgent` LLM invocations

```csharp
_timeoutPolicy = Policy
    .TimeoutAsync(
        timeout: TimeSpan.FromSeconds(30),
        onTimeoutAsync: (context, timeSpan, task) =>
        {
            _logger.LogWarning(
                "LLM call timed out after {Timeout}s",
                timeSpan.TotalSeconds);
            return Task.CompletedTask;
        });
```

**Behavior**:
- Cancels operation after 30 seconds
- Prevents hanging requests
- Logs timeout events

**Use Cases**:
- LLM API calls
- External service calls
- Long-running operations

### 3. Combined Resilience Pipeline

**Location**: `BaseAgent.InvokeKernelAsync`

```csharp
protected async Task<string> InvokeKernelAsync(
    string prompt,
    KernelArguments? arguments = null,
    CancellationToken cancellationToken = default)
{
    return await _resiliencePipeline.ExecuteAsync(async (ct) =>
    {
        var result = await Kernel.InvokePromptAsync(
            prompt, 
            arguments, 
            cancellationToken: ct);
        return result.GetValue<string>() ?? string.Empty;
    }, cancellationToken);
}
```

**Pipeline Composition**:
1. Timeout (30s)
2. Retry with exponential backoff (2 attempts)

**Benefits**:
- Handles transient failures automatically
- Prevents indefinite hangs
- Logs all retry attempts
- Maintains observability

---

## Batch Operation Error Handling

### Graceful Degradation

**Location**: `DaprEventPublisher.PublishAsync`

```csharp
public async Task PublishAsync(
    IEnumerable<IDomainEvent> domainEvents,
    CancellationToken cancellationToken = default)
{
    var eventsList = domainEvents.ToList();
    if (!eventsList.Any()) return;

    var publishTasks = eventsList.Select(evt => 
        _retryPolicy.ExecuteAsync(() => 
            PublishAsync(evt, cancellationToken)));
    
    var results = await Task.WhenAll(
        publishTasks.Select(t => 
            t.ContinueWith(task => 
                new { Success = !task.IsFaulted, Task = task })));

    var successCount = results.Count(r => r.Success);
    var failureCount = results.Count(r => !r.Success);

    if (failureCount > 0)
    {
        _logger.LogWarning(
            "Batch publish completed with {SuccessCount} successes and {FailureCount} failures",
            successCount,
            failureCount);
    }

    // Don't throw - some events may have succeeded
}
```

**Characteristics**:
- Partial success is acceptable
- Failures don't block successful publishes
- Comprehensive logging
- No exceptions thrown for partial failures

---

## Input Validation and Sanitization

### 1. FluentValidation

**Location**: `NotificationRequestValidator`

```csharp
public class NotificationRequestValidator : 
    AbstractValidator<NotificationRequest>
{
    public NotificationRequestValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty()
            .Must(channel => AllowedChannels.Contains(channel.ToLower()))
            .WithMessage("Channel must be one of: email, sms, teams, slack");

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(10000)
            .Must(content => !ContainsInjectionPatterns(content))
            .WithMessage("Content contains potentially malicious content");
    }
}
```

**Validation Checks**:
- Required fields
- Max lengths
- Enum values
- Email format (for email channel)
- XSS injection patterns
- Prompt injection patterns

### 2. Input Sanitization

**Location**: `InputSanitizer`

```csharp
public string Sanitize(string input)
{
    if (string.IsNullOrEmpty(input))
        return input;

    // Remove control characters
    input = Regex.Replace(input, @"[\x00-\x1F\x7F]", "");

    // Escape dangerous patterns
    var dangerousPatterns = new[]
    {
        ("```", "\\`\\`\\`"),
        ("###", "\\#\\#\\#"),
        ("SYSTEM:", "SYSTEM\\:"),
        ("INSTRUCTION:", "INSTRUCTION\\:"),
        ("IGNORE PREVIOUS", "IGNORE\\_PREVIOUS"),
        ("DISREGARD ALL", "DISREGARD\\_ALL")
    };

    foreach (var (pattern, replacement) in dangerousPatterns)
    {
        input = input.Replace(pattern, replacement, 
            StringComparison.OrdinalIgnoreCase);
    }

    // Limit excessive newlines
    input = Regex.Replace(input, @"\n{3,}", "\n\n");

    return input;
}
```

**Protections**:
- Control character removal
- Markdown code block escaping
- LLM keyword escaping
- Newline normalization

---

## Logging Strategy

### Structured Logging with Serilog

**Configuration**: `Notification API Program.cs`

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogLevel.Warning)
    .MinimumLevel.Override("System", LogLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                       "{Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/agents-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] " +
                       "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

**Features**:
- Console and file sinks
- Daily log rotation
- Structured output
- Context enrichment
- Machine name and thread ID

### Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| **Trace** | Detailed diagnostic | "Processing template variable: {Variable}" |
| **Debug** | Development info | "Cache hit for prompt: {Key}" |
| **Information** | General flow | "Agent {Name} started execution" |
| **Warning** | Recoverable errors | "Retry {Count} after transient failure" |
| **Error** | Failures requiring attention | "Agent execution failed: {Message}" |
| **Critical** | System failures | "Database connection failed" |

---

## Error Correlation

### Correlation ID Tracking

All operations use correlation IDs for request tracing:

```csharp
public class AgentContext
{
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
    public string ExecutionId { get; init; } = Guid.NewGuid().ToString();
    public string? InitiatedBy { get; init; }
    public CancellationToken CancellationToken { get; init; }
}
```

**Propagation**:
1. HTTP request → API generates CorrelationId
2. API → Agent passes CorrelationId in AgentContext
3. Agent → Infrastructure logs with CorrelationId
4. Infrastructure → External services includes CorrelationId in headers

**Benefits**:
- End-to-end request tracing
- Distributed system debugging
- Performance analysis
- Error investigation

---

## Exception Types and Handling

### Common Exceptions

| Exception | Layer | Handling |
|-----------|-------|----------|
| `ValidationException` | Application | Return `AgentResult.Failure` with validation errors |
| `FileNotFoundException` | Infrastructure | Log and return failure, don't retry |
| `HttpRequestException` | Infrastructure | Retry with exponential backoff |
| `TimeoutException` | Infrastructure | Log timeout, return failure |
| `OperationCanceledException` | All layers | Graceful cancellation, log and return |
| `InvalidOperationException` | Domain | Log and return failure with context |

### Custom Exceptions

The framework uses `AgentResult` pattern instead of custom exceptions, but infrastructure may throw:

```csharp
// Don't throw custom exceptions in agents
public class BadInputException : Exception // ❌ Avoid

// Use AgentResult instead
return AgentResult.Failure(
    "Invalid input provided",
    new Dictionary<string, object>
    {
        ["ErrorCode"] = "INPUT_INVALID",
        ["Field"] = "email"
    }); // ✅ Preferred
```

---

## Best Practices

### DO ✅

1. **Use AgentResult for all agent operations**
   ```csharp
   return AgentResult.Success("Operation completed", metadata);
   ```

2. **Log exceptions with full context**
   ```csharp
   Logger.LogError(ex, "Operation failed. CorrelationId: {CorrelationId}", id);
   ```

3. **Use Polly for external calls**
   ```csharp
   await _retryPolicy.ExecuteAsync(() => ExternalApiCall());
   ```

4. **Validate input early**
   ```csharp
   var validationResult = await _validator.ValidateAsync(request);
   if (!validationResult.IsValid)
       return AgentResult.Failure("Validation failed", errors);
   ```

5. **Sanitize user input before LLM calls**
   ```csharp
   var sanitized = _sanitizer.Sanitize(userInput);
   ```

### DON'T ❌

1. **Don't swallow exceptions silently**
   ```csharp
   catch (Exception) { } // ❌ Never do this
   ```

2. **Don't throw exceptions from agents**
   ```csharp
   throw new CustomException(); // ❌ Use AgentResult instead
   ```

3. **Don't retry on non-transient errors**
   ```csharp
   // ❌ Don't retry validation errors
   _retryPolicy.Execute(() => ValidateInput());
   ```

4. **Don't log sensitive information**
   ```csharp
   Logger.LogError("Password: {Password}", password); // ❌ Never
   ```

5. **Don't block async operations**
   ```csharp
   var result = AsyncMethod().Result; // ❌ Use await
   ```

---

## Troubleshooting

See [troubleshooting.md](troubleshooting.md) for common issues and solutions.

---

## Related Documentation

- [Architecture Documentation](architecture.md)
- [Testing Guide](testing_guide.md)
- [Security Best Practices](security.md)
- [Performance Optimization](performance.md)
