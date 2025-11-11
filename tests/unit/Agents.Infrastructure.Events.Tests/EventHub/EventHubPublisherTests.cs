using Agents.Domain.Core.Events;
using Agents.Infrastructure.Events.EventHub;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Agents.Infrastructure.Events.Tests.EventHub;

public class EventHubPublisherTests
{
    private readonly Mock<ILogger<EventHubPublisher>> _loggerMock;
    private readonly IConfiguration _configuration;

    public EventHubPublisherTests()
    {
        _loggerMock = new Mock<ILogger<EventHubPublisher>>();

        var configDict = new Dictionary<string, string?>
        {
            ["EventHub:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
            ["EventHub:EventHubName"] = "test-events"
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
        var act = () => new EventHubPublisher(emptyConfig, _loggerMock.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenEventHubNameMissing()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            ["EventHub:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act & Assert
        var act = () => new EventHubPublisher(config, _loggerMock.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EventHubName*");
    }

    [Fact]
    public void Constructor_ShouldCreatePublisher_WhenConfigurationValid()
    {
        // Act
        var publisher = new EventHubPublisher(_configuration, _loggerMock.Object);

        // Assert
        publisher.Should().NotBeNull();
    }

    // Note: Full integration tests with actual Event Hub would be in integration test project
}

// Test domain event for testing purposes
public class TestDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public Guid? CausationId { get; } = null;
    public string TestData { get; set; } = "Test";
}
