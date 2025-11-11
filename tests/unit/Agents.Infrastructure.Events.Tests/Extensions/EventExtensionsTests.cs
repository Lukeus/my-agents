using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Events.EventHub;
using Agents.Infrastructure.Events.Extensions;
using Agents.Infrastructure.Events.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Agents.Infrastructure.Events.Tests.Extensions;

public class EventExtensionsTests
{
    [Fact]
    public void AddEventPublishing_ShouldRegisterServiceBusPublisher_ByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey"
            })
            .Build();
        services.AddLogging();

        // Act
        services.AddEventPublishing(configuration);

        // Assert - Verify IEventPublisher is registered
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEventPublisher));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(ServiceBusPublisher));
    }

    [Fact]
    public void AddEventPublishing_ShouldRegisterEventHubPublisher_WhenConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EventProvider"] = "EventHub",
                ["EventHub:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
                ["EventHub:EventHubName"] = "test-events"
            })
            .Build();
        services.AddLogging();

        // Act
        services.AddEventPublishing(configuration);

        // Assert - Verify IEventPublisher is registered
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEventPublisher));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(EventHubPublisher));
    }

    [Fact]
    public void AddServiceBusConsumer_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act
        services.AddServiceBusConsumer("test-topic", "test-subscription");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        hostedServices.Should().NotBeEmpty();
        hostedServices.Should().ContainItemsAssignableTo<ServiceBusConsumer>();
    }
}
