# Agent Development Guide

This guide covers everything you need to know to develop new agents or extend existing agents in the AI Orchestration Multi-Agent Framework.

## Table of Contents

- [Overview](#overview)
- [Agent Anatomy](#agent-anatomy)
- [Creating a New Agent](#creating-a-new-agent)
- [Agent Base Classes](#agent-base-classes)
- [Working with Prompts](#working-with-prompts)
- [Event Publishing](#event-publishing)
- [Testing Agents](#testing-agents)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

Agents in this framework are specialized microservices that:
- Process specific types of requests
- Use LLMs (via Semantic Kernel) to make intelligent decisions
- Communicate via events with other agents
- Follow Clean Architecture principles

Each agent consists of four layers:
1. **Domain**: Business entities and events
2. **Application**: Agent logic and use cases
3. **Infrastructure**: External integrations
4. **Presentation**: REST API endpoints

## Agent Anatomy

### Core Components

Every agent has the following components:

```
Agents.Application.{AgentName}/
├── {AgentName}Agent.cs              # Core agent implementation
├── Commands/                         # Write operations
│   └── Execute{AgentName}Command.cs
├── Queries/                          # Read operations
│   └── Get{AgentName}StatusQuery.cs
├── EventHandlers/                    # React to domain events
│   └── {Event}Handler.cs
├── Models/                           # DTOs and request/response models
└── Validators/                       # Input validation

Agents.Domain.{AgentName}/
├── Entities/                         # Domain models
├── Events/                           # Domain events
└── ValueObjects/                     # Immutable value types

Agents.API.{AgentName}/
├── Controllers/                      # API endpoints
│   └── {AgentName}Controller.cs
├── Models/                           # API request/response DTOs
└── Program.cs                        # Startup configuration
```

## Creating a New Agent

### Step 1: Define the Domain

Create the domain layer for your agent:

```csharp
// Agents.Domain.MyAgent/Entities/MyAgentTask.cs
namespace Agents.Domain.MyAgent.Entities;

public class MyAgentTask
{
    public Guid Id { get; init; }
    public string Description { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; set; }

    public MyAgentTask()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = TaskStatus.Pending;
    }
}

public enum TaskStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
```

Create domain events:

```csharp
// Agents.Domain.MyAgent/Events/MyAgentTaskCompletedEvent.cs
namespace Agents.Domain.MyAgent.Events;

public record MyAgentTaskCompletedEvent : DomainEvent
{
    public Guid TaskId { get; init; }
    public string Result { get; init; }
    public DateTime CompletedAt { get; init; }

    public MyAgentTaskCompletedEvent(Guid taskId, string result)
    {
        TaskId = taskId;
        Result = result;
        CompletedAt = DateTime.UtcNow;
        EventType = "agents.myagent.task.completed";
    }
}
```

### Step 2: Implement the Agent

Create the agent class in the Application layer:

```csharp
// Agents.Application.MyAgent/MyAgent.cs
using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Agents.Application.MyAgent;

public class MyAgent : BaseAgent
{
    public MyAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        ILogger<MyAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger)
    {
    }

    public override async Task<AgentResult> ExecuteAsync(
        AgentRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("MyAgent executing request: {RequestId}", request.Id);

            // 1. Load the appropriate prompt
            var prompt = await _promptLoader.LoadPromptAsync(
                "myagent-processor", 
                cancellationToken);

            // 2. Prepare input data for the LLM
            var input = new
            {
                task_description = request.Payload["description"],
                context = request.Payload.GetValueOrDefault("context", "")
            };

            // 3. Invoke the LLM via Semantic Kernel
            var result = await _llmProvider.CompleteAsync<MyAgentResponse>(
                prompt, 
                input, 
                cancellationToken);

            // 4. Publish domain event
            var domainEvent = new MyAgentTaskCompletedEvent(
                request.Id, 
                result.Output);
            
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

            // 5. Return result
            return AgentResult.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MyAgent failed to execute request: {RequestId}", request.Id);
            return AgentResult.Failure(ex.Message);
        }
    }
}

public record MyAgentResponse
{
    public string Output { get; init; }
    public double Confidence { get; init; }
}
```

### Step 3: Create API Controller

```csharp
// Agents.API.MyAgent/Controllers/MyAgentController.cs
using Microsoft.AspNetCore.Mvc;
using Agents.Application.MyAgent;

namespace Agents.API.MyAgent.Controllers;

[ApiController]
[Route("api/myagent")]
public class MyAgentController : ControllerBase
{
    private readonly MyAgent _agent;
    private readonly ILogger<MyAgentController> _logger;

    public MyAgentController(MyAgent agent, ILogger<MyAgentController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    [HttpPost("execute")]
    [ProducesResponseType(typeof(AgentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Execute(
        [FromBody] MyAgentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var agentRequest = new AgentRequest
        {
            Id = Guid.NewGuid(),
            Payload = new Dictionary<string, object>
            {
                ["description"] = request.Description,
                ["context"] = request.Context ?? ""
            }
        };

        var result = await _agent.ExecuteAsync(agentRequest, cancellationToken);

        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", agent = "MyAgent" });
    }
}

public record MyAgentRequest
{
    public string Description { get; init; } = string.Empty;
    public string? Context { get; init; }
}
```

### Step 4: Configure Dependency Injection

```csharp
// Agents.API.MyAgent/Program.cs
using Agents.Application.MyAgent;
using Agents.Infrastructure.LLM;
using Agents.Infrastructure.Prompts;
using Agents.Infrastructure.EventGrid;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register agent and dependencies
builder.Services.AddScoped<MyAgent>();
builder.Services.AddSingleton<ILLMProvider, OllamaProvider>(); // or AzureOpenAIProvider
builder.Services.AddSingleton<IPromptLoader, GitHubPromptLoader>();
builder.Services.AddSingleton<IEventPublisher, EventGridPublisher>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<MyAgentHealthCheck>("myagent_health");

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### Step 5: Create Prompts

Create prompt files in the `prompts/` directory:

```yaml
# prompts/myagent/myagent-processor.prompt
---
name: myagent-processor
version: 1.0.0
description: Processes MyAgent tasks with intelligent decision making
model_requirements:
  min_tokens: 4096
  temperature: 0.7
input_schema:
  - name: task_description
    type: string
    required: true
  - name: context
    type: string
    required: false
output_schema:
  type: object
  properties:
    output: string
    confidence: number
---
You are an intelligent agent tasked with processing the following request.

Task Description: {{task_description}}
Context: {{context}}

Analyze the task and provide:
1. A detailed output addressing the task
2. A confidence score (0.0 to 1.0) indicating your certainty

Return your response as JSON matching this schema:
{
  "output": "your detailed response here",
  "confidence": 0.95
}
```

### Step 6: Add Unit Tests

```csharp
// tests/Agents.Tests.Unit/Application/MyAgentTests.cs
using Xunit;
using Moq;
using FluentAssertions;
using Agents.Application.MyAgent;
using Agents.Domain.Core.Interfaces;

namespace Agents.Tests.Unit.Application;

public class MyAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<MyAgent>> _mockLogger;
    private readonly MyAgent _agent;

    public MyAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<MyAgent>>();

        _agent = new MyAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new AgentRequest
        {
            Id = Guid.NewGuid(),
            Payload = new Dictionary<string, object>
            {
                ["description"] = "Test task"
            }
        };

        _mockPromptLoader
            .Setup(x => x.LoadPromptAsync("myagent-processor", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test prompt");

        _mockLLMProvider
            .Setup(x => x.CompleteAsync<MyAgentResponse>(
                It.IsAny<string>(), 
                It.IsAny<object>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyAgentResponse 
            { 
                Output = "Task completed", 
                Confidence = 0.95 
            });

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        _mockEventPublisher.Verify(
            x => x.PublishAsync(
                It.IsAny<MyAgentTaskCompletedEvent>(), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LLMFailure_ReturnsFailure()
    {
        // Arrange
        var request = new AgentRequest
        {
            Id = Guid.NewGuid(),
            Payload = new Dictionary<string, object>
            {
                ["description"] = "Test task"
            }
        };

        _mockPromptLoader
            .Setup(x => x.LoadPromptAsync("myagent-processor", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test prompt");

        _mockLLMProvider
            .Setup(x => x.CompleteAsync<MyAgentResponse>(
                It.IsAny<string>(), 
                It.IsAny<object>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM error"));

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("LLM error");
    }
}
```

## Agent Base Classes

### BaseAgent

All agents should inherit from `BaseAgent`:

```csharp
public abstract class BaseAgent
{
    protected readonly ILLMProvider _llmProvider;
    protected readonly IPromptLoader _promptLoader;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly ILogger _logger;

    protected BaseAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        ILogger logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _promptLoader = promptLoader ?? throw new ArgumentNullException(nameof(promptLoader));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract Task<AgentResult> ExecuteAsync(
        AgentRequest request, 
        CancellationToken cancellationToken = default);

    protected async Task<string> InvokeKernelAsync(
        string promptName, 
        object data,
        CancellationToken cancellationToken = default)
    {
        var prompt = await _promptLoader.LoadPromptAsync(promptName, cancellationToken);
        return await _llmProvider.CompleteAsync(prompt, data, cancellationToken);
    }
}
```

### AgentRequest and AgentResult

Standard request/response types:

```csharp
public class AgentRequest
{
    public Guid Id { get; init; }
    public Dictionary<string, object> Payload { get; init; } = new();
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public class AgentResult
{
    public bool IsSuccess { get; init; }
    public object? Data { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static AgentResult Success(object data) => new() 
    { 
        IsSuccess = true, 
        Data = data 
    };
    
    public static AgentResult Failure(string error) => new() 
    { 
        IsSuccess = false, 
        ErrorMessage = error 
    };
}
```

## Working with Prompts

### Loading Prompts

```csharp
// Load a prompt by name
var prompt = await _promptLoader.LoadPromptAsync("agent-name/prompt-name");

// Load with version
var versionedPrompt = await _promptLoader.LoadPromptAsync(
    "agent-name/prompt-name", 
    version: "1.2.0");
```

### Invoking LLM with Prompts

```csharp
// Simple string response
var response = await _llmProvider.CompleteAsync(prompt, inputData);

// Structured response (JSON deserialization)
var structuredResponse = await _llmProvider.CompleteAsync<MyResponseType>(
    prompt, 
    inputData);

// Streaming response
await foreach (var chunk in _llmProvider.StreamCompleteAsync(prompt, inputData))
{
    Console.Write(chunk);
}
```

## Event Publishing

### Publishing Events

```csharp
// Create domain event
var domainEvent = new MyAgentTaskCompletedEvent(taskId, result);

// Publish to Event Grid
await _eventPublisher.PublishAsync(domainEvent);

// Publish multiple events
await _eventPublisher.PublishBatchAsync(new[] { event1, event2, event3 });
```

### Subscribing to Events

```csharp
// Create event handler
public class MyEventHandler : IEventHandler<MyAgentTaskCompletedEvent>
{
    private readonly ILogger<MyEventHandler> _logger;

    public MyEventHandler(ILogger<MyEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        MyAgentTaskCompletedEvent @event, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling task completed event: {TaskId}", 
            @event.TaskId);
        
        // React to the event
        await ProcessCompletedTaskAsync(@event, cancellationToken);
    }
}
```

## Testing Agents

### Unit Testing Strategy

1. **Mock all dependencies**: Use Moq to mock ILLMProvider, IPromptLoader, IEventPublisher
2. **Test success paths**: Verify correct behavior with valid inputs
3. **Test failure paths**: Verify graceful handling of errors
4. **Verify event publishing**: Ensure events are published correctly
5. **Test edge cases**: Null inputs, empty data, malformed responses

### Integration Testing

```csharp
public class MyAgentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MyAgentIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Execute_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new MyAgentRequest
        {
            Description = "Integration test task"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/myagent/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<AgentResult>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
    }
}
```

## Best Practices

### 1. Error Handling

Always wrap agent execution in try-catch:

```csharp
try
{
    // Agent logic
    return AgentResult.Success(data);
}
catch (LLMException ex)
{
    _logger.LogError(ex, "LLM provider failed");
    return AgentResult.Failure("LLM processing failed");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return AgentResult.Failure("Internal error occurred");
}
```

### 2. Logging

Use structured logging:

```csharp
_logger.LogInformation(
    "Agent {AgentName} processing request {RequestId} with {ItemCount} items",
    nameof(MyAgent),
    request.Id,
    request.Payload.Count);
```

### 3. Validation

Validate inputs before processing:

```csharp
public class MyAgentRequestValidator : AbstractValidator<MyAgentRequest>
{
    public MyAgentRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);
            
        RuleFor(x => x.Context)
            .MaximumLength(5000)
            .When(x => x.Context != null);
    }
}
```

### 4. Idempotency

Design agents to be idempotent:

```csharp
public async Task<AgentResult> ExecuteAsync(AgentRequest request)
{
    // Check if already processed
    var existingResult = await _cache.GetAsync(request.Id.ToString());
    if (existingResult != null)
    {
        return existingResult;
    }

    // Process and cache result
    var result = await ProcessAsync(request);
    await _cache.SetAsync(request.Id.ToString(), result, TimeSpan.FromHours(1));
    
    return result;
}
```

### 5. Timeout Handling

Set appropriate timeouts:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await _agent.ExecuteAsync(request, cts.Token);
```

### 6. Retry Policies

Use Polly for resilience:

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (exception, timeSpan, retry, ctx) =>
        {
            _logger.LogWarning(
                "Retry {Retry} after {Delay}s due to {Exception}",
                retry, 
                timeSpan.TotalSeconds, 
                exception.GetType().Name);
        });

await retryPolicy.ExecuteAsync(async () => 
    await _llmProvider.CompleteAsync(prompt, data));
```

## Troubleshooting

### Common Issues

**Issue**: Agent returns empty or invalid responses

**Solution**: 
- Check prompt formatting and placeholders
- Verify input data structure matches prompt schema
- Increase LLM temperature for more creative responses
- Check LLM token limits

**Issue**: Events not being received by subscribers

**Solution**:
- Verify Event Grid subscription configuration
- Check event type matches exactly
- Review network connectivity
- Examine dead letter queue for failed events

**Issue**: Slow agent response times

**Solution**:
- Enable prompt caching
- Use streaming for long responses
- Optimize prompt length
- Consider batching requests
- Profile LLM provider latency

**Issue**: High LLM costs

**Solution**:
- Implement response caching
- Use Ollama for development/testing
- Optimize prompts to reduce token usage
- Set token limits on LLM requests

## Further Reading

- [Architecture Overview](architecture.md)
- [Prompt Authoring Guide](prompt-authoring.md)
- [Deployment Guide](deployment.md)
- [Operations Runbook](operations.md)
