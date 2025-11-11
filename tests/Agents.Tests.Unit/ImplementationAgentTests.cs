using Agents.Application.Core;
using Agents.Application.Implementation;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Agents.Tests.Unit;

public class ImplementationAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<ImplementationAgent>> _mockLogger;
    private readonly ImplementationAgent _agent;

    public ImplementationAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<ImplementationAgent>>();

        _agent = new ImplementationAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Theory]
    [InlineData("generate_code")]
    [InlineData("review_code")]
    [InlineData("suggest_refactoring")]
    public async Task ExecuteAsync_WithValidActions_ShouldProcess(string action)
    {
        // Arrange
        var request = new ImplementationRequest
        {
            Action = action,
            Specification = "Create a REST API endpoint",
            Language = "C#",
            Framework = ".NET",
            Patterns = "Clean Architecture"
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
        var request = new ImplementationRequest
        {
            Action = "invalid_action",
            Specification = "Test"
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
    public async Task ExecuteAsync_WithMissingSpecification_ShouldHandleGracefully()
    {
        // Arrange
        var input = "{}";
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }
}
