using Agents.Application.Core;
using Agents.Application.ServiceDesk;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Agents.Tests.Unit;

public class ServiceDeskAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<ServiceDeskAgent>> _mockLogger;
    private readonly ServiceDeskAgent _agent;

    public ServiceDeskAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<ServiceDeskAgent>>();

        _agent = new ServiceDeskAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithCheckSLAAction_ShouldReturnSLAStatus()
    {
        // Arrange
        var request = new ServiceDeskRequest
        {
            Action = "check_sla",
            TicketId = "TICKET-123",
            TicketTitle = "Test Ticket",
            TicketDescription = "Test Description"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("triage_ticket")]
    [InlineData("suggest_solution")]
    [InlineData("escalate")]
    public async Task ExecuteAsync_WithValidActions_ShouldProcess(string action)
    {
        // Arrange
        var request = new ServiceDeskRequest
        {
            Action = action,
            TicketId = "TICKET-123",
            TicketTitle = "Application Error",
            TicketDescription = "Getting 500 error when accessing dashboard",
            Category = "Technical"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownAction_ShouldReturnFailure()
    {
        // Arrange
        var request = new ServiceDeskRequest
        {
            Action = "unknown",
            TicketId = "T-1",
            TicketTitle = "Title",
            TicketDescription = "Desc"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown action");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJson_ShouldReturnFailure()
    {
        // Arrange
        var input = "invalid json";
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }
}
