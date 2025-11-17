using Agents.Application.BimClassification;
using Agents.Application.BimClassification.Requests;
using Agents.Application.BimClassification.Services;
using Agents.Application.Core;
using Agents.Domain.BimClassification.Interfaces;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Dapr.Extensions;
using Agents.Infrastructure.LLM;
using Agents.Infrastructure.Persistence.Redis.Repositories;
using Agents.Infrastructure.Persistence.SqlServer.Repositories;
using Agents.Infrastructure.Prompts.Services;

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

// Configure Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "BimClassification:";
});

// Configure repositories
builder.Services.AddScoped<IBimElementRepository, BimElementRepository>();
builder.Services.AddScoped<IClassificationCacheRepository, RedisClassificationCacheRepository>();

// Configure application services
builder.Services.AddScoped<BimClassificationService>();

// Configure BIM Classification Agent
builder.Services.AddScoped<BimClassificationAgent>();

// Health checks
builder.Services.AddHealthChecks();

// CORS - Configured with specific allowed origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Fallback to development-only open CORS if no origins configured
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                throw new InvalidOperationException(
                    "CORS AllowedOrigins must be configured in production. Add 'Cors:AllowedOrigins' to appsettings.json");
            }
        }
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

// Map single element classification endpoint (legacy)
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

// Map batch classification endpoint (recommended for 100M+ records)
app.MapPost("/api/bimclassification/batch",
    async (BatchClassifyRequest request,
           BimClassificationService service,
           CancellationToken ct) =>
    {
        var context = new AgentContext { CancellationToken = ct };
        var result = await service.ClassifyBatchAsync(
            request.ElementIds,
            context,
            ct);

        return Results.Ok(new
        {
            totalElements = result.TotalElements,
            totalPatterns = result.TotalPatterns,
            cachedPatterns = result.CachedPatterns,
            newlyClassified = result.NewlyClassifiedPatterns,
            cacheHitRate = result.TotalPatterns == 0 ? 0.0 : result.CachedPatterns / (double)result.TotalPatterns,
            suggestions = result.Suggestions,
            patternMapping = result.PatternMapping
        });
    })
    .WithName("ClassifyBatch")
    .WithOpenApi()
    .WithDescription("Classifies a batch of BIM elements using pattern aggregation and caching");

// Map cache statistics endpoint
app.MapGet("/api/bimclassification/cache/stats",
    async (IClassificationCacheRepository cache, CancellationToken ct) =>
    {
        var stats = await cache.GetStatisticsAsync(ct);
        return Results.Ok(new
        {
            hitCount = stats.HitCount,
            missCount = stats.MissCount,
            hitRate = stats.HitRate,
            totalItems = stats.TotalItems
        });
    })
    .WithName("GetCacheStatistics")
    .WithOpenApi();

// Map cache invalidation endpoint
app.MapDelete("/api/bimclassification/cache/{patternHash}",
    async (string patternHash, IClassificationCacheRepository cache, CancellationToken ct) =>
    {
        await cache.InvalidateByPatternHashAsync(patternHash, ct);
        return Results.Ok(new { message = "Cache invalidated", patternHash });
    })
    .WithName("InvalidateCache")
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
