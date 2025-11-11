using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.LLM;
using Agents.Infrastructure.Prompts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Agents.API.Shared;

public static class ApiConfiguration
{
    public static void AddSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure LLM Provider
        services.AddLLMProvider(configuration);

        // Configure Prompts
        services.AddSingleton<IPromptLoader, PromptLoader>();

        // Configure Event Publisher (mock)
        services.AddSingleton<IEventPublisher, MockEventPublisher>();

        // Health checks
        services.AddHealthChecks();

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    public static void AddSwagger(this IServiceCollection services, string title, string description)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = title, Version = "v1", Description = description });
        });
    }
}

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
