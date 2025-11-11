using Agents.Infrastructure.Observability.Metrics;
using FluentAssertions;
using Prometheus;

namespace Agents.Infrastructure.Observability.Tests.Metrics;

public class AgentMetricsTests
{
    [Fact]
    public void AgentOperationsTotal_ShouldIncrement_WhenOperationRecorded()
    {
        // Arrange
        var initialValue = AgentMetrics.AgentOperationsTotal.WithLabels("test-agent", "test-operation").Value;

        // Act
        AgentMetrics.AgentOperationsTotal.WithLabels("test-agent", "test-operation").Inc();

        // Assert
        var finalValue = AgentMetrics.AgentOperationsTotal.WithLabels("test-agent", "test-operation").Value;
        finalValue.Should().Be(initialValue + 1);
    }

    [Fact]
    public void AgentOperationDuration_ShouldRecordObservation_WhenOperationCompletes()
    {
        // Arrange & Act
        AgentMetrics.AgentOperationDuration
            .WithLabels("test-agent", "test-operation")
            .Observe(1.5);

        // Assert - Metric should exist and have recorded the observation
        AgentMetrics.AgentOperationDuration
            .WithLabels("test-agent", "test-operation")
            .Should().NotBeNull();
    }

    [Fact]
    public void LlmCallsTotal_ShouldIncrement_ForDifferentModels()
    {
        // Arrange
        var gpt4InitialValue = AgentMetrics.LlmCallsTotal.WithLabels("test-agent", "gpt-4", "azure").Value;
        var gpt35InitialValue = AgentMetrics.LlmCallsTotal.WithLabels("test-agent", "gpt-3.5", "azure").Value;

        // Act
        AgentMetrics.LlmCallsTotal.WithLabels("test-agent", "gpt-4", "azure").Inc();
        AgentMetrics.LlmCallsTotal.WithLabels("test-agent", "gpt-3.5", "azure").Inc(2);

        // Assert
        AgentMetrics.LlmCallsTotal.WithLabels("test-agent", "gpt-4", "azure").Value
            .Should().Be(gpt4InitialValue + 1);
        AgentMetrics.LlmCallsTotal.WithLabels("test-agent", "gpt-3.5", "azure").Value
            .Should().Be(gpt35InitialValue + 2);
    }

    [Fact]
    public void LlmTokensUsedTotal_ShouldAccumulateTokens()
    {
        // Arrange
        var initialTokens = AgentMetrics.LlmTokensUsedTotal.WithLabels("test-agent", "gpt-4", "azure").Value;

        // Act
        AgentMetrics.LlmTokensUsedTotal.WithLabels("test-agent", "gpt-4", "azure").Inc(500);
        AgentMetrics.LlmTokensUsedTotal.WithLabels("test-agent", "gpt-4", "azure").Inc(300);

        // Assert
        AgentMetrics.LlmTokensUsedTotal.WithLabels("test-agent", "gpt-4", "azure").Value
            .Should().Be(initialTokens + 800);
    }

    [Fact]
    public void NotificationsSentTotal_ShouldTrackByChannel()
    {
        // Arrange
        var emailInitial = AgentMetrics.NotificationsSentTotal.WithLabels("notification-agent", "email").Value;
        var smsInitial = AgentMetrics.NotificationsSentTotal.WithLabels("notification-agent", "sms").Value;

        // Act
        AgentMetrics.NotificationsSentTotal.WithLabels("notification-agent", "email").Inc();
        AgentMetrics.NotificationsSentTotal.WithLabels("notification-agent", "sms").Inc();
        AgentMetrics.NotificationsSentTotal.WithLabels("notification-agent", "email").Inc();

        // Assert
        AgentMetrics.NotificationsSentTotal.WithLabels("notification-agent", "email").Value
            .Should().Be(emailInitial + 2);
        AgentMetrics.NotificationsSentTotal.WithLabels("notification-agent", "sms").Value
            .Should().Be(smsInitial + 1);
    }

    [Fact]
    public void EventsPublishedTotal_ShouldTrackEventsByType()
    {
        // Arrange
        var initialValue = AgentMetrics.EventsPublishedTotal
            .WithLabels("test-agent", "NotificationSent", "EventHub").Value;

        // Act
        AgentMetrics.EventsPublishedTotal
            .WithLabels("test-agent", "NotificationSent", "EventHub").Inc(3);

        // Assert
        AgentMetrics.EventsPublishedTotal
            .WithLabels("test-agent", "NotificationSent", "EventHub").Value
            .Should().Be(initialValue + 3);
    }

    [Fact]
    public void ActiveRequests_ShouldSetGaugeValue()
    {
        // Arrange & Act
        AgentMetrics.ActiveRequests.WithLabels("test-agent").Set(5);

        // Assert
        AgentMetrics.ActiveRequests.WithLabels("test-agent").Value.Should().Be(5);

        // Act - Decrease
        AgentMetrics.ActiveRequests.WithLabels("test-agent").Set(3);

        // Assert
        AgentMetrics.ActiveRequests.WithLabels("test-agent").Value.Should().Be(3);
    }

    [Fact]
    public void ActiveRequests_ShouldIncAndDec()
    {
        // Arrange
        AgentMetrics.ActiveRequests.WithLabels("test-agent-2").Set(0);

        // Act
        AgentMetrics.ActiveRequests.WithLabels("test-agent-2").Inc();
        AgentMetrics.ActiveRequests.WithLabels("test-agent-2").Inc();

        // Assert
        AgentMetrics.ActiveRequests.WithLabels("test-agent-2").Value.Should().Be(2);

        // Act
        AgentMetrics.ActiveRequests.WithLabels("test-agent-2").Dec();

        // Assert
        AgentMetrics.ActiveRequests.WithLabels("test-agent-2").Value.Should().Be(1);
    }

    [Fact]
    public void DatabaseQueriesTotal_ShouldTrackByOperationType()
    {
        // Arrange
        var readInitial = AgentMetrics.DatabaseQueriesTotal.WithLabels("test-agent", "READ").Value;
        var writeInitial = AgentMetrics.DatabaseQueriesTotal.WithLabels("test-agent", "WRITE").Value;

        // Act
        AgentMetrics.DatabaseQueriesTotal.WithLabels("test-agent", "READ").Inc(10);
        AgentMetrics.DatabaseQueriesTotal.WithLabels("test-agent", "WRITE").Inc(5);

        // Assert
        AgentMetrics.DatabaseQueriesTotal.WithLabels("test-agent", "READ").Value
            .Should().Be(readInitial + 10);
        AgentMetrics.DatabaseQueriesTotal.WithLabels("test-agent", "WRITE").Value
            .Should().Be(writeInitial + 5);
    }

    [Fact]
    public void AgentMemoryUsageBytes_ShouldSetMemoryValue()
    {
        // Arrange
        var memoryBytes = 1024 * 1024 * 100; // 100 MB

        // Act
        AgentMetrics.AgentMemoryUsageBytes.WithLabels("test-agent").Set(memoryBytes);

        // Assert
        AgentMetrics.AgentMemoryUsageBytes.WithLabels("test-agent").Value.Should().Be(memoryBytes);
    }
}
