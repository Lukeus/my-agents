using Agents.Domain.Core.Events;
using Agents.Infrastructure.Dapr.PubSub;
using Dapr.Client;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Agents.Tests.Integration.Dapr;

/// <summary>
/// Integration tests for DaprEventPublisher focusing on:
/// - Retry behavior with Polly resilience policies
/// - Batch publishing with partial failures
/// - Error handling and logging
/// </summary>
public class DaprEventPublisherTests
{
    private readonly Mock<ILogger<DaprEventPublisher>> _mockLogger;

    public DaprEventPublisherTests()
    {
        _mockLogger = new Mock<ILogger<DaprEventPublisher>>();
    }

    [Fact]
    public async Task PublishAsync_WithSuccessfulPublish_ShouldLogSuccessfully()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var testEvent = new TestDomainEvent { EventId = Guid.NewGuid() };

        mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);

        // Act
        await publisher.PublishAsync(testEvent);

        // Assert
        mockDaprClient.Verify(
            c => c.PublishEventAsync(
                "agents-pubsub",
                "testdomainevent",
                It.Is<IDomainEvent>(e => e.EventId == testEvent.EventId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyLogContains(_mockLogger, LogLevel.Information, "Successfully published event");
    }

    [Fact]
    public async Task PublishAsync_WithTransientFailureThenSuccess_ShouldRetryAndSucceed()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var testEvent = new TestDomainEvent { EventId = Guid.NewGuid() };
        var attemptCount = 0;

        // Fail first 2 attempts, succeed on 3rd
        mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new InvalidOperationException("Transient error simulating Dapr failure");
                }
                return Task.CompletedTask;
            });

        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);

        // Act
        await publisher.PublishAsync(testEvent);

        // Assert
        attemptCount.Should().Be(3, "should retry twice after initial failure");

        mockDaprClient.Verify(
            c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IDomainEvent>(e => e.EventId == testEvent.EventId),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        VerifyLogContains(_mockLogger, LogLevel.Warning, "Event publish retry");
        VerifyLogContains(_mockLogger, LogLevel.Information, "Successfully published event");
    }

    [Fact]
    public async Task PublishAsync_WithPersistentFailure_ShouldExhaustRetriesAndThrow()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var testEvent = new TestDomainEvent { EventId = Guid.NewGuid() };
        var attemptCount = 0;

        // Always fail
        mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Persistent error simulating Dapr failure");
            });

        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(testEvent));

        attemptCount.Should().Be(4, "should attempt once + 3 retries (MaxRetryAttempts=3)");

        VerifyLogContains(_mockLogger, LogLevel.Error, "Failed to publish event");
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync((IDomainEvent)null!));
    }

    [Fact]
    public async Task PublishAsync_Batch_WithAllSuccessful_ShouldPublishAllEvents()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var events = new List<TestDomainEvent>
        {
            new() { EventId = Guid.NewGuid() },
            new() { EventId = Guid.NewGuid() },
            new() { EventId = Guid.NewGuid() }
        };

        mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);

        // Act
        await publisher.PublishAsync(events);

        // Assert
        mockDaprClient.Verify(
            c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        VerifyLogContains(_mockLogger, LogLevel.Information, "Successfully published all 3 events");
    }

    [Fact]
    public async Task PublishAsync_Batch_WithPartialFailure_ShouldLogFailuresButNotThrow()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var events = new List<TestDomainEvent>
        {
            new() { EventId = Guid.NewGuid() },
            new() { EventId = Guid.NewGuid() },
            new() { EventId = Guid.NewGuid() }
        };

        var eventIds = events.Select(e => e.EventId).ToList();
        mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IDomainEvent>(e => e.EventId == eventIds[1]), // Fail the 2nd event
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Persistent failure for event 2 simulating Dapr failure"));

        mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IDomainEvent>(e => e.EventId != eventIds[1]), // Succeed for other events
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);

        // Act
        await publisher.PublishAsync(events);

        // Assert - Should NOT throw, handles partial failures gracefully
        VerifyLogContains(_mockLogger, LogLevel.Error, "Batch publish partially failed");
    }

    [Fact]
    public async Task PublishAsync_Batch_WithEmptyList_ShouldLogAndReturnImmediately()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);
        var emptyList = new List<TestDomainEvent>();

        // Act
        await publisher.PublishAsync(emptyList);

        // Assert
        mockDaprClient.Verify(
            c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyLogContains(_mockLogger, LogLevel.Debug, "No events to publish");
    }

    [Fact]
    public async Task PublishAsync_Batch_WithNullList_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync((IEnumerable<IDomainEvent>)null!));
    }

    [Fact]
    public void DaprEventPublisher_Constructor_WithNullDaprClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DaprEventPublisher(null!, _mockLogger.Object));
    }

    [Fact]
    public void DaprEventPublisher_Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DaprEventPublisher(mockDaprClient.Object, null!));
    }

    [Fact]
    public async Task PublishAsync_WithCancellationRequested_ShouldRespectCancellation()
    {
        // Arrange
        var mockDaprClient = new Mock<DaprClient>();
        var testEvent = new TestDomainEvent { EventId = Guid.NewGuid() };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var publisher = new DaprEventPublisher(mockDaprClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            publisher.PublishAsync(testEvent, cts.Token));
    }

    /// <summary>
    /// Test domain event for integration testing
    /// </summary>
    private class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; init; }
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public Guid? CausationId { get; init; }
    }

    /// <summary>
    /// Helper to verify log messages contain expected text
    /// </summary>
    private static void VerifyLogContains(
        Mock<ILogger<DaprEventPublisher>> mockLogger,
        LogLevel logLevel,
        string expectedText)
    {
        mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedText)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
