using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Dapr.PubSub;
using Dapr.Client;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Agents.Infrastructure.Dapr.Tests;

public class DaprEventPublisherTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly Mock<ILogger<DaprEventPublisher>> _mockLogger;
    private readonly DaprEventPublisher _publisher;

    public DaprEventPublisherTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        _mockLogger = new Mock<ILogger<DaprEventPublisher>>();
        _publisher = new DaprEventPublisher(_mockDaprClient.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDaprClientIsNull()
    {
        // Act
        var act = () => new DaprEventPublisher(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("daprClient");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new DaprEventPublisher(_mockDaprClient.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowArgumentNullException_WhenDomainEventIsNull()
    {
        // Act
        var act = async () => await _publisher.PublishAsync((IDomainEvent)null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvent");
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishSingleEvent_WhenValidEventProvided()
    {
        // Arrange
        var testEvent = new TestDomainEvent { EventId = Guid.NewGuid() };
        var cancellationToken = CancellationToken.None;

        _mockDaprClient
            .Setup(x => x.PublishEventAsync(
                "agents-pubsub",
                "testdomainevent",
                It.IsAny<object>(),
                cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _publisher.PublishAsync(testEvent, cancellationToken);

        // Assert
        _mockDaprClient.Verify(
            x => x.PublishEventAsync(
                "agents-pubsub",
                "testdomainevent",
                It.IsAny<object>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogError_WhenPublishFails()
    {
        // Arrange
        var testEvent = new TestDomainEvent { EventId = Guid.NewGuid() };
        var expectedException = new Exception("Publish failed");

        _mockDaprClient
            .Setup(x => x.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = async () => await _publisher.PublishAsync(testEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Publish failed");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowArgumentNullException_WhenEventsCollectionIsNull()
    {
        // Act
        var act = async () => await _publisher.PublishAsync((IEnumerable<IDomainEvent>)null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvents");
    }

    [Fact]
    public async Task PublishAsync_ShouldDoNothing_WhenEventsCollectionIsEmpty()
    {
        // Arrange
        var emptyEvents = Enumerable.Empty<IDomainEvent>();

        // Act
        await _publisher.PublishAsync(emptyEvents, CancellationToken.None);

        // Assert
        _mockDaprClient.Verify(
            x => x.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishMultipleEvents_WhenValidEventsProvided()
    {
        // Arrange
        var events = new List<IDomainEvent>
        {
            new TestDomainEvent { EventId = Guid.NewGuid() },
            new TestDomainEvent { EventId = Guid.NewGuid() },
            new TestDomainEvent { EventId = Guid.NewGuid() }
        };

        _mockDaprClient
            .Setup(x => x.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _publisher.PublishAsync(events, CancellationToken.None);

        // Assert
        _mockDaprClient.Verify(
            x => x.PublishEventAsync(
                "agents-pubsub",
                "testdomainevent",
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task PublishAsync_ShouldUseCorrectTopicName_WhenPublishingEvent()
    {
        // Arrange
        var testEvent = new CustomTestEvent { EventId = Guid.NewGuid() };

        _mockDaprClient
            .Setup(x => x.PublishEventAsync(
                "agents-pubsub",
                "customtestevent",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _publisher.PublishAsync(testEvent, CancellationToken.None);

        // Assert
        _mockDaprClient.Verify();
    }

    // Test domain event classes
    private class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public Guid? CausationId { get; set; }
    }

    private class CustomTestEvent : IDomainEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public Guid? CausationId { get; set; }
    }
}
