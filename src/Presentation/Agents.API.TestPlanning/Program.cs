using Agents.Application.Core;
using Agents.Application.TestPlanning;
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
        Title = "TestPlanning Agent API",
        Version = "v1",
        Description = "AI-powered test planning agent for automated test strategy and case generation"
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

// Configure TestPlanning Agent
builder.Services.AddScoped<TestPlanningAgent>();

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
app.MapControllers();
app.MapDefaultEndpoints();

// Map Dapr pub/sub endpoints if enabled
if (useDapr)
{
    app.MapSubscribeHandler();
    app.UseCloudEvents();
}

app.Run();

// Mock EventPublisher for now
public class MockEventPublisher : IEventPublisher
{
    public Task PublishAsync(Agents.Domain.Core.Events.IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync(IEnumerable<Agents.Domain.Core.Events.IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
