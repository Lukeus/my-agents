# BIM Classification Agent Implementation Plan

## Problem Statement

Extend the `my-agents` AI orchestration framework with a new **BIM Classification Agent** that analyzes Building Information Modeling (BIM) elements and proposes commodity/pricing classifications and derived item rules. The agent must operate **safely** as an advisory system - proposing suggestions rather than directly modifying classification rules - while leveraging the existing clean architecture, Dapr eventing, and Semantic Kernel LLM integration.

**Key Requirements:**
- Integrate seamlessly with existing `my-agents` clean architecture patterns
- Use .NET 9, C#, and SQL Server 2017 for persistence
- Agent proposes classifications/rules; never directly mutates authoritative data
- Efficiently tokenize BIM data for LLM consumption
- Follow all existing architectural constraints (Clean Architecture, SOLID, Dapr abstraction)

---

## Current State

### Existing Architecture

The `my-agents` repository implements a production-ready, event-driven microservice framework with:

**Clean Architecture Layers:**
```
Domain/                    ← Entities, Value Objects, Events, Interfaces (no external dependencies)
├── Agents.Domain.Core/    ← Base classes: AggregateRoot<TId>, DomainEvent, IEventPublisher
├── Agents.Domain.Notification/
├── Agents.Domain.DevOps/
├── Agents.Domain.TestPlanning/
├── Agents.Domain.Implementation/
├── Agents.Domain.PromptRegistry/
└── Agents.Domain.ServiceDesk/

Application/               ← Use cases, agents, CQRS handlers
├── Agents.Application.Core/    ← BaseAgent, ILLMProvider, AgentContext, AgentResult
├── Agents.Application.Notification/
├── Agents.Application.DevOps/
├── Agents.Application.TestPlanning/
├── Agents.Application.Implementation/
├── Agents.Application.PromptRegistry/
└── Agents.Application.ServiceDesk/

Infrastructure/            ← External integrations (LLM, Events, Persistence, Dapr)
├── Agents.Infrastructure.LLM/
├── Agents.Infrastructure.Prompts/
├── Agents.Infrastructure.Dapr/
├── Agents.Infrastructure.EventGrid/
├── Agents.Infrastructure.EventHub/
├── Agents.Infrastructure.ServiceBus/
├── Agents.Infrastructure.Persistence.SqlServer/
└── Agents.Infrastructure.Persistence.CosmosDB/

Presentation/              ← REST APIs
├── Agents.API.Notification/
├── Agents.API.DevOps/
├── Agents.API.TestPlanning/
├── Agents.API.Implementation/
├── Agents.API.ServiceDesk/
└── Agents.API.Gateway/
```

### Key Existing Patterns

**BaseAgent Pattern** (`src/Application/Agents.Application.Core/BaseAgent.cs`):
```csharp
public abstract class BaseAgent
{
    protected readonly ILLMProvider LLMProvider;
    protected readonly IPromptLoader PromptLoader;
    protected readonly IEventPublisher EventPublisher;
    protected readonly ILogger Logger;
    
    protected abstract Task<AgentResult> ExecuteCoreAsync(string input, AgentContext context);
    protected async Task<string> LoadPromptAsync(string promptName, Dictionary<string, object> variables);
    protected async Task<string> InvokeKernelAsync(string promptText, ...);
    protected async Task PublishEventAsync(IDomainEvent domainEvent, ...);
}
```

**Domain Entity Pattern** (e.g., `src/Domain/Agents.Domain.Notification/Entities/Notification.cs`):
```csharp
public class Notification : AggregateRoot<string>
{
    // Properties with private setters
    public string Channel { get; private set; }
    public NotificationStatus Status { get; private set; }
    
    // Constructor
    public Notification(string id, string channel, ...) : base(id) { }
    
    // Behavior methods that raise domain events
    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        AddDomainEvent(new NotificationSentEvent { ... });
    }
}
```

**Domain Events** (`src/Domain/Agents.Domain.Notification/Events/NotificationEvents.cs`):
```csharp
public class NotificationSentEvent : DomainEvent
{
    public required string NotificationId { get; init; }
    public required string Channel { get; init; }
    public required bool IsSuccess { get; init; }
}
```

**Prompt Files** (`prompts/notification-email-formatter.prompt`):
```yaml
---
name: notification-email-formatter
version: 1.0.0
description: Formats notification content for email delivery
model_requirements:
  min_tokens: 2000
  temperature: 0.3
schema:
  parameters:
    - name: subject
      type: string
      required: true
---
You are an expert email formatter...
{{subject}}
{{content}}
```

**API Structure** (`src/Presentation/Agents.API.Notification/Program.cs`):
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLLMProvider(builder.Configuration);
builder.Services.AddSingleton<IPromptLoader, PromptLoader>();
builder.Services.AddDaprEventPublisher(); // or MockEventPublisher
builder.Services.AddScoped<NotificationAgent>();
// ... map endpoints
```

**Aspire Orchestration** (`src/AppHost/Agents.AppHost/Program.cs`):
```csharp
var notificationApi = builder.AddProject<Projects.Agents_API_Notification>("notification-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithDaprSidecar("notification-api");
```

### Technology Stack (Confirmed)
- **.NET 9** with C# latest features
- **Microsoft Semantic Kernel 1.67.1** for LLM orchestration
- **.NET Aspire 8.2.2** for local orchestration
- **Dapr 1.14.0** for distributed runtime
- **Entity Framework Core 9.0** for SQL Server persistence
- **SQL Server 2017+** for relational data
- **xUnit, Moq, FluentAssertions** for testing

---

## Proposed Changes

### 1. New Domain Layer Projects

#### `src/Domain/Agents.Domain.BimClassification/`

**File: `Entities/BimElementView.cs`**
```csharp
namespace Agents.Domain.BimClassification.Entities;

/// <summary>
/// Read-only view of a BIM element for classification analysis.
/// This is NOT the canonical BIM element - it's a snapshot for the agent.
/// </summary>
public sealed class BimElementView
{
    public long Id { get; init; }
    public string ExternalId { get; init; } = default!;
    public string ProjectId { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string? Family { get; init; }
    public string? Type { get; init; }
    public string? Spec { get; init; }
    public string? LocationType { get; init; }  // Indoor/Outdoor/Roof/etc.
    public string? Material { get; init; }
    public decimal? LengthMm { get; init; }
    public decimal? WidthMm { get; init; }
    public decimal? HeightMm { get; init; }
    public decimal? DiameterMm { get; init; }
    public string? MetaJson { get; init; }  // Additional BIM properties as JSON
}
```

**File: `Entities/BimClassificationSuggestion.cs`**
```csharp
namespace Agents.Domain.BimClassification.Entities;

/// <summary>
/// Aggregate root representing an agent-generated classification suggestion.
/// This is ADVISORY ONLY - never the canonical classification.
/// </summary>
public sealed class BimClassificationSuggestion : AggregateRoot<long>
{
    public long BimElementId { get; private set; }
    public string? SuggestedCommodityCode { get; private set; }
    public string? SuggestedPricingCode { get; private set; }
    public List<DerivedItemSuggestion> DerivedItems { get; private set; } = new();
    public string ReasoningSummary { get; private set; } = default!;
    public SuggestionStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime? ReviewedUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    
    private BimClassificationSuggestion() : base(0) { }  // EF Core
    
    public BimClassificationSuggestion(
        long bimElementId,
        string? commodityCode,
        string? pricingCode,
        IEnumerable<DerivedItemSuggestion> derivedItems,
        string reasoningSummary) 
        : base(0)  // ID assigned by DB
    {
        BimElementId = bimElementId;
        SuggestedCommodityCode = commodityCode;
        SuggestedPricingCode = pricingCode;
        DerivedItems = derivedItems.ToList();
        ReasoningSummary = reasoningSummary;
        Status = SuggestionStatus.Pending;
        CreatedUtc = DateTime.UtcNow;
        
        AddDomainEvent(new ClassificationSuggestedEvent
        {
            BimElementId = bimElementId,
            SuggestionId = Id,
            CommodityCode = commodityCode
        });
    }
    
    public void Approve(string approvedBy)
    {
        Status = SuggestionStatus.Approved;
        ReviewedUtc = DateTime.UtcNow;
        ReviewedBy = approvedBy;
        
        AddDomainEvent(new ClassificationSuggestionApprovedEvent
        {
            SuggestionId = Id,
            BimElementId = BimElementId,
            ApprovedBy = approvedBy
        });
    }
    
    public void Reject(string rejectedBy, string reason)
    {
        Status = SuggestionStatus.Rejected;
        ReviewedUtc = DateTime.UtcNow;
        ReviewedBy = rejectedBy;
        
        AddDomainEvent(new ClassificationSuggestionRejectedEvent
        {
            SuggestionId = Id,
            BimElementId = BimElementId,
            RejectedBy = rejectedBy,
            Reason = reason
        });
    }
}

public sealed class DerivedItemSuggestion
{
    public string DerivedCommodityCode { get; init; } = default!;
    public string? DerivedPricingCode { get; init; }
    public string QuantityFormula { get; init; } = default!;
    public string QuantityUnit { get; init; } = default!;
}

public enum SuggestionStatus
{
    Pending,
    Approved,
    Rejected
}
```

**File: `Events/BimClassificationEvents.cs`**
```csharp
namespace Agents.Domain.BimClassification.Events;

public class ClassificationSuggestedEvent : DomainEvent
{
    public required long BimElementId { get; init; }
    public required long SuggestionId { get; init; }
    public string? CommodityCode { get; init; }
}

public class ClassificationSuggestionApprovedEvent : DomainEvent
{
    public required long SuggestionId { get; init; }
    public required long BimElementId { get; init; }
    public required string ApprovedBy { get; init; }
}

public class ClassificationSuggestionRejectedEvent : DomainEvent
{
    public required long SuggestionId { get; init; }
    public required long BimElementId { get; init; }
    public required string RejectedBy { get; init; }
    public required string Reason { get; init; }
}
```

**File: `ValueObjects/BimElementPromptModel.cs`**
```csharp
namespace Agents.Domain.BimClassification.ValueObjects;

/// <summary>
/// Token-efficient representation of a BIM element for prompt generation.
/// </summary>
public sealed class BimElementPromptModel
{
    public long Id { get; init; }
    public string Category { get; init; } = default!;
    public string? Family { get; init; }
    public string? Type { get; init; }
    public string? Spec { get; init; }
    public string? LocationType { get; init; }
    public string? Material { get; init; }
    public decimal? LengthMm { get; init; }
    public decimal? WidthMm { get; init; }
    public decimal? HeightMm { get; init; }
    public decimal? DiameterMm { get; init; }
    public IReadOnlyDictionary<string, string> Meta { get; init; } = 
        new Dictionary<string, string>();
    
    /// <summary>
    /// Formats to token-efficient prompt text.
    /// </summary>
    public string ToPromptString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ElementId: {Id}");
        sb.AppendLine($"Category: {Category}");
        
        if (!string.IsNullOrWhiteSpace(Spec))
            sb.AppendLine($"Spec: {Spec}");
        if (!string.IsNullOrWhiteSpace(LocationType))
            sb.AppendLine($"LocationType: {LocationType}");
        if (!string.IsNullOrWhiteSpace(Material))
            sb.AppendLine($"Material: {Material}");
        if (!string.IsNullOrWhiteSpace(Family))
            sb.AppendLine($"Family: {Family}");
        if (!string.IsNullOrWhiteSpace(Type))
            sb.AppendLine($"Type: {Type}");
        
        if (LengthMm is not null || WidthMm is not null || 
            HeightMm is not null || DiameterMm is not null)
        {
            sb.AppendLine("Dimensions (mm):");
            if (LengthMm is not null) sb.AppendLine($"  Length: {LengthMm}");
            if (WidthMm is not null) sb.AppendLine($"  Width: {WidthMm}");
            if (HeightMm is not null) sb.AppendLine($"  Height: {HeightMm}");
            if (DiameterMm is not null) sb.AppendLine($"  Diameter: {DiameterMm}");
        }
        
        if (Meta.Count > 0)
        {
            sb.AppendLine("Metadata:");
            foreach (var (key, value) in Meta)
            {
                var trimmed = value.Length > 80 ? value[..80] + "..." : value;
                sb.AppendLine($"  {key}: {trimmed}");
            }
        }
        
        return sb.ToString();
    }
}
```

**File: `Agents.Domain.BimClassification.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\Agents.Domain.Core\Agents.Domain.Core.csproj" />
  </ItemGroup>
</Project>
```

---

### 2. New Application Layer Project

#### `src/Application/Agents.Application.BimClassification/`

**File: `Requests/ClassifyBimElementRequest.cs`**
```csharp
namespace Agents.Application.BimClassification.Requests;

public sealed class ClassifyBimElementRequest
{
    public long BimElementId { get; init; }
    
    /// <summary>
    /// Optional: pre-fetched BIM element data as JSON
    /// </summary>
    public string? ElementJson { get; init; }
    
    /// <summary>
    /// Optional: existing classification JSON (if any)
    /// </summary>
    public string? ExistingClassificationJson { get; init; }
}
```

**File: `Responses/ClassifyBimElementResponse.cs`**
```csharp
namespace Agents.Application.BimClassification.Responses;

public sealed class ClassifyBimElementResponse
{
    public long SuggestionId { get; init; }
    public string RawModelOutput { get; init; } = default!;
    public string NormalizedSuggestionJson { get; init; } = default!;
}
```

**File: `BimClassificationAgent.cs`**
```csharp
using Agents.Application.Core;
using Agents.Application.BimClassification.Requests;
using Agents.Application.BimClassification.Responses;
using Agents.Domain.BimClassification.Entities;
using Agents.Domain.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Agents.Application.BimClassification;

/// <summary>
/// Agent responsible for analyzing BIM elements and proposing classifications.
/// IMPORTANT: This agent produces SUGGESTIONS only, never direct classifications.
/// </summary>
public class BimClassificationAgent : BaseAgent
{
    public BimClassificationAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        ILogger<BimClassificationAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger, "BimClassificationAgent")
    {
    }
    
    protected override async Task<AgentResult> ExecuteCoreAsync(
        string input, 
        AgentContext context)
    {
        try
        {
            var request = JsonSerializer.Deserialize<ClassifyBimElementRequest>(input);
            if (request == null)
            {
                return AgentResult.Failure("Invalid BIM classification request format");
            }
            
            // Load system prompt (contains safety constraints)
            var systemPrompt = await LoadPromptAsync(
                "prompts/bim-classifier/system.prompt",
                new Dictionary<string, object>());
            
            // Load user prompt with element data
            var userPrompt = await LoadPromptAsync(
                "prompts/bim-classifier/user.prompt",
                new Dictionary<string, object>
                {
                    ["elementJson"] = request.ElementJson ?? "{}",
                    ["existingClassificationJson"] = request.ExistingClassificationJson ?? "{}"
                });
            
            var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";
            
            // Invoke LLM
            var rawResult = await InvokeKernelAsync(
                fullPrompt, 
                cancellationToken: context.CancellationToken);
            
            // Parse and normalize to suggestion JSON
            var normalizedJson = NormalizeToSuggestionJson(rawResult);
            
            // Deserialize to validate structure
            var suggestion = JsonSerializer.Deserialize<BimClassificationSuggestion>(
                normalizedJson);
            
            if (suggestion == null)
            {
                return AgentResult.Failure("Failed to parse LLM output into valid suggestion");
            }
            
            // Publish event
            await PublishEventAsync(
                suggestion.DomainEvents.First(), 
                context.CancellationToken);
            
            return AgentResult<ClassifyBimElementResponse>.Success(
                new ClassifyBimElementResponse
                {
                    SuggestionId = suggestion.Id,
                    RawModelOutput = rawResult,
                    NormalizedSuggestionJson = normalizedJson
                },
                "Classification suggestion generated successfully",
                new Dictionary<string, object>
                {
                    ["BimElementId"] = request.BimElementId,
                    ["SuggestionId"] = suggestion.Id
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error classifying BIM element");
            return AgentResult.Failure($"Error: {ex.Message}");
        }
    }
    
    private static string NormalizeToSuggestionJson(string raw)
    {
        // V1: Simple trim and validation
        // V2: Add JSON repair logic if needed
        var trimmed = raw.Trim();
        
        // Basic validation: must start with { and end with }
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
        {
            throw new InvalidOperationException(
                "LLM output is not valid JSON. Output must be strict JSON.");
        }
        
        return trimmed;
    }
}
```

**File: `Agents.Application.BimClassification.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\Domain\Agents.Domain.BimClassification\Agents.Domain.BimClassification.csproj" />
    <ProjectReference Include="..\Agents.Application.Core\Agents.Application.Core.csproj" />
  </ItemGroup>
</Project>
```

---

### 3. New Presentation Layer Project

#### `src/Presentation/Agents.API.BimClassification/`

**File: `Program.cs`**
```csharp
using Agents.Application.Core;
using Agents.Application.BimClassification;
using Agents.Application.BimClassification.Requests;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.LLM;
using Agents.Infrastructure.Prompts.Services;
using Agents.Infrastructure.Dapr.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "BIM Classification Agent API",
        Version = "v1",
        Description = "AI-powered BIM element classification suggestion agent"
    });
});

// Configure LLM Provider
builder.Services.AddLLMProvider(builder.Configuration);

// Configure Prompts
builder.Services.AddSingleton<IPromptLoader, PromptLoader>();

// Configure Event Publisher - Dapr or Mock
var useDapr = builder.Configuration.GetValue<bool>("Dapr:Enabled");
if (useDapr)
{
    builder.Services.AddDaprClient();
    builder.Services.AddDaprEventPublisher();
}
else
{
    builder.Services.AddSingleton<IEventPublisher, MockEventPublisher>();
}

// Configure BIM Classification Agent
builder.Services.AddScoped<BimClassificationAgent>();

// Health checks
builder.Services.AddHealthChecks();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

// Map agent endpoint
app.MapPost("/api/bimclassification/execute",
    async (ClassifyBimElementRequest request,
           BimClassificationAgent agent,
           CancellationToken ct) =>
    {
        var context = new AgentContext { CancellationToken = ct };
        var input = System.Text.Json.JsonSerializer.Serialize(request);
        var result = await agent.ExecuteAsync(input, context);
        
        return result.IsSuccess 
            ? Results.Ok(result) 
            : Results.BadRequest(result);
    })
    .WithName("ClassifyBimElement")
    .WithOpenApi();

app.MapGet("/api/bimclassification/health", () => 
    Results.Ok(new { status = "Healthy" }))
    .WithName("Health")
    .WithOpenApi();

app.MapControllers();
app.MapDefaultEndpoints();

// Map Dapr pub/sub endpoints if enabled
if (useDapr)
{
    app.MapSubscribeHandler();
    app.UseCloudEvents();
}

app.Run();

// Mock EventPublisher
public class MockEventPublisher : IEventPublisher
{
    public Task PublishAsync(
        Agents.Domain.Core.Events.IDomainEvent domainEvent, 
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task PublishAsync(
        IEnumerable<Agents.Domain.Core.Events.IDomainEvent> domainEvents, 
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

**File: `appsettings.json`**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Dapr": {
    "Enabled": false
  },
  "LLMProvider": {
    "ProviderType": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelId": "llama3.2"
    },
    "AzureOpenAI": {
      "Endpoint": "",
      "ApiKey": "",
      "DeploymentName": "",
      "ModelId": "gpt-4"
    }
  }
}
```

**File: `Agents.API.BimClassification.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="10.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\Application\Agents.Application.BimClassification\Agents.Application.BimClassification.csproj" />
    <ProjectReference Include="..\..\Infrastructure\Agents.Infrastructure.LLM\Agents.Infrastructure.LLM.csproj" />
    <ProjectReference Include="..\..\Infrastructure\Agents.Infrastructure.Prompts\Agents.Infrastructure.Prompts.csproj" />
    <ProjectReference Include="..\..\Infrastructure\Agents.Infrastructure.Dapr\Agents.Infrastructure.Dapr.csproj" />
    <ProjectReference Include="..\Agents.API.Shared\Agents.API.Shared.csproj" />
  </ItemGroup>
</Project>
```

---

### 4. Prompt Files

#### `prompts/bim-classifier/system.prompt`

```yaml
---
name: bim-classifier-system
version: 1.0.0
description: System prompt for BIM classification agent with safety constraints
model_requirements:
  min_tokens: 4000
  temperature: 0.2
---
You are an assistant for a deterministic, data-centric BIM classification rules engine.

CRITICAL CONSTRAINTS:
- You MUST NOT assume you can directly classify BIM elements at runtime.
- Your role is to PROPOSE new classifications or rules that a human and deterministic engine will review.
- You are ADVISORY ONLY. You do not have authority to modify production classifications.
- All outputs MUST be valid JSON matching this exact schema:

{
  "suggestedCommodityCode": "string | null",
  "suggestedPricingCode": "string | null",
  "derivedItems": [
    {
      "derivedCommodityCode": "string",
      "derivedPricingCode": "string | null",
      "quantityFormula": "string",
      "quantityUnit": "string"
    }
  ],
  "reasoningSummary": "short explanation of your analysis and suggestions"
}

RULES:
1. If you are not confident, set codes to null and explain your uncertainty in reasoningSummary.
2. For derived items, use precise quantityFormula expressions (e.g., "LengthMm * 2" for insulation on both sides of a duct).
3. Never instruct anyone to bypass the deterministic rules engine.
4. If existing classifications appear incorrect, explain the issue in reasoningSummary but do not override.
5. Focus on pattern recognition: look for opportunities to create generalizable rules, not one-off classifications.

EXAMPLES OF GOOD REASONING:
- "This appears to be an outdoor duct based on LocationType=Outdoor. Recommend CommodityCode=DUCT-EXT and derived insulation item INSUL-EXT with quantityFormula='LengthMm * DiameterMm * PI'."
- "Low confidence classification due to missing Spec field. Suggest CommodityCode=null until specification is provided."
- "Element matches pattern: Category=Ducts + Material=Galvanized + LocationType=Indoor. High confidence for CommodityCode=DUCT-GAL-INT."
```

#### `prompts/bim-classifier/user.prompt`

```yaml
---
name: bim-classifier-user
version: 1.0.0
description: User prompt for BIM element classification
---
You are given a BIM element as JSON:

{{elementJson}}

You are also given the current classification state (may be empty):

{{existingClassificationJson}}

TASK:
Analyze this data and propose:
1. Reasonable commodity/pricing codes (if obvious from the data)
2. Additional derived items that should be generated (e.g., insulation for outdoor ducts, hangers for pipes)
3. Clear reasoning for your suggestions

Return ONLY JSON as specified in the system instructions. Do not include markdown code blocks or any other text.
```

---

### 5. Aspire Orchestration Update

**File: `src/AppHost/Agents.AppHost/Program.cs`** (add after existing agents)

```csharp
// Add BIM Classification Agent API with Dapr sidecar
var bimclassificationApi = builder.AddProject<Projects.Agents_API_BimClassification>("bimclassification-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("ConnectionStrings__SqlServer", sqlServer)
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("bimclassification-api");
```

---

### 6. Solution File Update

**File: `Agents.sln`** (add new projects)

```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Agents.Domain.BimClassification", "src\Domain\Agents.Domain.BimClassification\Agents.Domain.BimClassification.csproj", "{NEW-GUID-1}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Agents.Application.BimClassification", "src\Application\Agents.Application.BimClassification\Agents.Application.BimClassification.csproj", "{NEW-GUID-2}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Agents.API.BimClassification", "src\Presentation\Agents.API.BimClassification\Agents.API.BimClassification.csproj", "{NEW-GUID-3}"
EndProject
```

---

### 7. Database Schema (SQL Server 2017)

**File: `database/migrations/001_BimClassificationSuggestions.sql`**

```sql
-- BIM Classification Suggestions Table
CREATE TABLE BimClassificationSuggestions (
    Id                        BIGINT IDENTITY PRIMARY KEY,
    BimElementId              BIGINT NOT NULL,
    SuggestedCommodityCode    NVARCHAR(64) NULL,
    SuggestedPricingCode      NVARCHAR(64) NULL,
    DerivedItemsJson          NVARCHAR(MAX) NULL,  -- JSON array of derived items
    ReasoningSummary          NVARCHAR(2048) NOT NULL,
    Status                    NVARCHAR(32) NOT NULL DEFAULT 'Pending',  -- Pending/Approved/Rejected
    CreatedUtc                DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ReviewedUtc               DATETIME2 NULL,
    ReviewedBy                NVARCHAR(128) NULL,
    
    CONSTRAINT CHK_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected')),
    INDEX IX_BimElementId (BimElementId),
    INDEX IX_Status (Status),
    INDEX IX_CreatedUtc (CreatedUtc)
);

-- Optional: Rule Suggestions Table (for future use)
CREATE TABLE RuleSuggestions (
    Id                  BIGINT IDENTITY PRIMARY KEY,
    Source              NVARCHAR(64) NOT NULL DEFAULT 'BimClassificationAgent',
    BimElementId        BIGINT NULL,
    SuggestedRuleType   NVARCHAR(32) NOT NULL,  -- CommodityRule/DerivedItemRule
    SuggestedRuleJson   NVARCHAR(MAX) NOT NULL,
    ReasoningSummary    NVARCHAR(1024) NULL,
    CreatedUtc          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status              NVARCHAR(32) NOT NULL DEFAULT 'Pending',
    StatusChangedUtc    DATETIME2 NULL,
    StatusChangedBy     NVARCHAR(128) NULL,
    
    CONSTRAINT CHK_RuleStatus CHECK (Status IN ('Pending', 'Approved', 'Rejected')),
    INDEX IX_Status (Status),
    INDEX IX_CreatedUtc (CreatedUtc)
);
```

---

### 8. Unit Tests

**File: `tests/Agents.Tests.Unit/BimClassification/BimClassificationAgentTests.cs`**

```csharp
using Agents.Application.BimClassification;
using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Agents.Tests.Unit.BimClassification;

public class BimClassificationAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<BimClassificationAgent>> _mockLogger;
    private readonly BimClassificationAgent _agent;
    
    public BimClassificationAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<BimClassificationAgent>>();
        
        _agent = new BimClassificationAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }
    
    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = new ClassifyBimElementRequest
        {
            BimElementId = 123,
            ElementJson = "{\"category\":\"Ducts\"}"
        };
        
        var input = System.Text.Json.JsonSerializer.Serialize(request);
        var context = new AgentContext();
        
        // Mock prompt loading
        _mockPromptLoader
            .Setup(x => x.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Prompt { Content = "Test prompt" });
        
        // Mock LLM response
        var mockKernel = new Mock<Kernel>();
        _mockLLMProvider.Setup(x => x.GetKernel()).Returns(mockKernel.Object);
        
        // Mock kernel response with valid JSON
        var jsonResponse = @"{
            ""suggestedCommodityCode"": ""DUCT-001"",
            ""suggestedPricingCode"": ""PRICE-001"",
            ""derivedItems"": [],
            ""reasoningSummary"": ""Test reasoning""
        }";
        
        // TODO: Complete mock setup for InvokePromptAsync
        
        // Act
        var result = await _agent.ExecuteAsync(input, context);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        // Arrange
        var input = "invalid json";
        var context = new AgentContext();
        
        // Act
        var result = await _agent.ExecuteAsync(input, context);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid BIM classification request");
    }
}
```

---

## Implementation Steps

### Phase 1: Domain Layer (Day 1)
1. Create `src/Domain/Agents.Domain.BimClassification/` project
2. Implement entities: `BimElementView`, `BimClassificationSuggestion`, `DerivedItemSuggestion`
3. Implement value objects: `BimElementPromptModel` with `ToPromptString()` method
4. Implement domain events: `ClassificationSuggestedEvent`, `ClassificationSuggestionApprovedEvent`, `ClassificationSuggestionRejectedEvent`
5. Add project to `Agents.sln`
6. Build and verify: `dotnet build src/Domain/Agents.Domain.BimClassification/Agents.Domain.BimClassification.csproj`

### Phase 2: Application Layer (Day 1-2)
1. Create `src/Application/Agents.Application.BimClassification/` project
2. Implement `ClassifyBimElementRequest` and `ClassifyBimElementResponse`
3. Implement `BimClassificationAgent` extending `BaseAgent`
4. Add project references to Domain and Core
5. Add project to `Agents.sln`
6. Build and verify: `dotnet build src/Application/Agents.Application.BimClassification/Agents.Application.BimClassification.csproj`

### Phase 3: Prompt Creation (Day 2)
1. Create `prompts/bim-classifier/` directory
2. Create `system.prompt` with safety constraints
3. Create `user.prompt` with element analysis template
4. Test prompt formatting manually

### Phase 4: Presentation Layer (Day 2-3)
1. Create `src/Presentation/Agents.API.BimClassification/` project
2. Implement `Program.cs` with minimal API endpoints
3. Create `appsettings.json`
4. Add project references
5. Add project to `Agents.sln`
6. Build and verify: `dotnet build src/Presentation/Agents.API.BimClassification/Agents.API.BimClassification.csproj`

### Phase 5: Aspire Integration (Day 3)
1. Update `src/AppHost/Agents.AppHost/Program.cs` to include BIM Classification API
2. Test Aspire startup: `dotnet run --project src/AppHost/Agents.AppHost/Agents.AppHost.csproj`
3. Verify API appears in Aspire Dashboard (http://localhost:15000)
4. Test health endpoint: `curl http://localhost:5006/api/bimclassification/health`

### Phase 6: Database Schema (Day 3)
1. Create SQL migration script for `BimClassificationSuggestions` table
2. Create SQL migration script for `RuleSuggestions` table (optional)
3. Apply migrations to local SQL Server
4. Verify tables created

### Phase 7: Unit Tests (Day 4)
1. Create `tests/Agents.Tests.Unit/BimClassification/` directory
2. Implement `BimClassificationAgentTests.cs`
3. Implement `BimElementPromptModelTests.cs`
4. Run tests: `dotnet test --filter "Category=Unit"`
5. Ensure >80% code coverage

### Phase 8: Integration Testing (Day 4-5)
1. Test end-to-end via Swagger UI (http://localhost:5006/swagger)
2. Test with mock Ollama responses
3. Test Dapr event publishing
4. Verify suggestion data persists to database
5. Test approval/rejection workflow

### Phase 9: Documentation (Day 5)
1. Update `WARP.md` with BIM Classification agent information
2. Create `docs/bim-classification-agent-guide.md`
3. Update `README.md` with new agent capabilities
4. Document safety constraints and suggestion workflow

---

## Safety Constraints & Best Practices

### Agent Safety Rules
1. **Never Direct Classification**: Agent writes ONLY to `BimClassificationSuggestions` table
2. **Human-in-the-Loop**: All suggestions require approval before becoming canonical classifications
3. **Strict JSON Output**: LLM output must be valid JSON matching predefined schema
4. **Confidence Thresholds**: Low-confidence suggestions set codes to `null` with explanation
5. **No Arbitrary Commands**: Agent output is pure data, never executable instructions

### Tokenization Best Practices
1. **Minimal Representation**: Use `BimElementPromptModel.ToPromptString()` to create compact text
2. **Truncate Long Metadata**: Limit metadata values to 80 characters
3. **Dimension Grouping**: Group related dimensions (Length/Width/Height) for clarity
4. **No Geometry Data**: Never include full BIM geometry in prompts

### Integration with Rules Engine
1. Deterministic rules engine runs FIRST
2. Agent activates only for unclassified or low-confidence elements
3. Agent suggestions flow through approval workflow
4. Approved suggestions can be converted to canonical rules in rules engine

---

## Success Criteria

- [ ] All new projects build without errors
- [ ] BIM Classification Agent API appears in Aspire Dashboard
- [ ] Agent responds to POST `/api/bimclassification/execute` with valid JSON
- [ ] Domain events are published via Dapr
- [ ] Suggestions are persisted to SQL Server
- [ ] Unit tests pass with >80% coverage
- [ ] Prompts enforce safety constraints (no direct classification)
- [ ] Agent follows existing clean architecture patterns
- [ ] Documentation updated (WARP.md, README.md)

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| LLM produces non-JSON output | Implement strict JSON validation + repair logic in `NormalizeToSuggestionJson()` |
| Agent suggests incorrect classifications | Human approval required; suggestions are advisory only |
| Large BIM datasets overwhelm token limits | Use `ToPromptString()` for efficient tokenization; process elements individually |
| Dependency conflicts in solution | Follow existing package versions from other agents |
| Dapr not configured | Falls back to `MockEventPublisher` when `Dapr:Enabled=false` |

---

## Next Steps After Approval

1. Review and approve this plan
2. Begin Phase 1 (Domain Layer implementation)
3. Iterate through phases 2-9 sequentially
4. Regular checkpoints after each phase completion
5. Final integration testing with full Aspire stack
