using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Events.EventHub;
using Agents.Infrastructure.Events.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.Events.Extensions;

public static class EventExtensions
{
    /// <summary>
    /// Adds event publishing infrastructure
    /// </summary>
    public static IServiceCollection AddEventPublishing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var eventProvider = configuration["EventProvider"] ?? "ServiceBus";

        if (eventProvider.Equals("EventHub", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEventPublisher, EventHubPublisher>();
        }
        else
        {
            services.AddSingleton<IEventPublisher, ServiceBusPublisher>();
        }

        return services;
    }

    /// <summary>
    /// Adds Service Bus event consumer as a hosted service
    /// </summary>
    public static IServiceCollection AddServiceBusConsumer(
        this IServiceCollection services,
        string topicName,
        string subscriptionName)
    {
        services.AddHostedService(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var logger = provider.GetRequiredService<ILogger<ServiceBusConsumer>>();
            return new ServiceBusConsumer(configuration, logger, topicName, subscriptionName);
        });

        return services;
    }
}
