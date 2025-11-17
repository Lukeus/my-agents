using Agents.Application.Core;
using Agents.Application.Notification;
using Agents.Application.Notification.Channels;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Dapr.Extensions;
using Agents.Infrastructure.LLM;
using Agents.Infrastructure.Persistence.SqlServer;
using Agents.Infrastructure.Prompts.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "NotificationAgent")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/notification-agent-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Notification Agent API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add Aspire service defaults
    builder.AddServiceDefaults();

    // Add services
    builder.Services.AddControllers();

    // Add FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(Agents.Application.Notification.NotificationRequestValidator).Assembly);
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

    // Configure Memory Cache for performance optimization
    builder.Services.AddMemoryCache(options =>
    {
        options.SizeLimit = 100; // Limit cache to 100 items
    });

    // Configure Prompts
    builder.Services.AddSingleton<IPromptLoader, PromptLoader>();

    // Configure Security
    builder.Services.AddSingleton<Agents.Shared.Security.IInputSanitizer, Agents.Shared.Security.InputSanitizer>();

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

    // OPTIONAL: Optimized EF Core Configuration (uncomment when database is needed)
    // Configure DbContext with performance optimizations:
    // builder.Services.AddDbContext<AgentsDbContext>(options =>
    // {
    //     var connectionString = builder.Configuration.GetConnectionString("SqlServer");
    //     options.UseSqlServer(connectionString, sqlOptions =>
    //     {
    //         // Retry policy for transient failures
    //         sqlOptions.EnableRetryOnFailure(
    //             maxRetryCount: 3,
    //             maxRetryDelay: TimeSpan.FromSeconds(10),
    //             errorNumbersToAdd: null);
    //         
    //         // Command timeout
    //         sqlOptions.CommandTimeout(30);
    //         
    //         // Migrations assembly
    //         sqlOptions.MigrationsAssembly("Agents.Infrastructure.Persistence.SqlServer");
    //     });
    //
    //     // No-tracking by default for read queries (improves performance)
    //     options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    //     
    //     // Development-only features
    //     if (builder.Environment.IsDevelopment())
    //     {
    //         options.EnableSensitiveDataLogging();
    //         options.EnableDetailedErrors();
    //     }
    // });

    // Apply database migrations
    // var connectionString = builder.Configuration.GetConnectionString("SqlServer");
    // if (!string.IsNullOrEmpty(connectionString))
    // {
    //     await app.Services.MigrateDatabaseAsync();
    // }

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

    Log.Information("Notification Agent API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notification Agent API failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

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
