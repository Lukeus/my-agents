using Agents.Application.Core;
using Agents.Application.Notification;
using Agents.Application.Notification.Channels;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.LLM;
using Agents.Infrastructure.Prompts.Services;
using Agents.Infrastructure.Persistence.SqlServer;
using Agents.Infrastructure.Dapr.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Notification Agent API",
        Version = "v1",
        Description = "AI-powered notification agent supporting multiple channels (Email, SMS, Teams, Slack)"
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

// Configure Notification services
builder.Services.AddSingleton<INotificationChannelFactory, NotificationChannelFactory>();
builder.Services.AddScoped<NotificationAgent>();

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

// Apply database migrations
var connectionString = builder.Configuration.GetConnectionString("SqlServer");
if (!string.IsNullOrEmpty(connectionString))
{
    await app.Services.MigrateDatabaseAsync();
}

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
app.MapHealthChecks("/health");

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
