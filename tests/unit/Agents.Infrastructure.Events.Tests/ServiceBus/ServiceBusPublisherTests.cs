using Agents.Domain.Core.Events;
using Agents.Infrastructure.Events.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Agents.Infrastructure.Events.Tests.ServiceBus;

public class ServiceBusPublisherTests
{
    private readonly Mock<ILogger<ServiceBusPublisher>> _loggerMock;
    private readonly IConfiguration _configuration;

    public ServiceBusPublisherTests()
    {
        _loggerMock = new Mock<ILogger<ServiceBusPublisher>>();
        
        var configDict = new Dictionary<string, string?>
        {
            ["ServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
            ["ServiceBus:NotificationTopic"] = "notification-events",
            ["ServiceBus:DevOpsTopic"] = "devops-events"
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenConnectionStringMissing()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        // Act & Assert
        var act = () => new ServiceBusPublisher(emptyConfig, _loggerMock.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void Constructor_ShouldCreatePublisher_WhenConfigurationValid()
    {
        // Act
        var publisher = new ServiceBusPublisher(_configuration, _loggerMock.Object);

        // Assert
        publisher.Should().NotBeNull();
    }

    // Note: Full integration tests with actual Service Bus would be in integration test project
}

// Test events for testing topic routing
public class NotificationCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public Guid? CausationId { get; } = null;
    public string RecipientEmail { get; set; } = "test@example.com";
}

public class DevOpsTaskCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public Guid? CausationId { get; } = null;
    public string TaskTitle { get; set; } = "Test Task";
}
