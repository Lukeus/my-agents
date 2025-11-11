using Agents.Application.Core;
using Agents.Application.TestPlanning;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Agents.Tests.Unit;

public class TestPlanningAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<TestPlanningAgent>> _mockLogger;
    private readonly TestPlanningAgent _agent;

    public TestPlanningAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<TestPlanningAgent>>();

        _agent = new TestPlanningAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidRequest_ShouldReturnFailure()
    {
        // Arrange
        var input = "invalid";
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("generate_spec")]
    [InlineData("create_strategy")]
    [InlineData("analyze_coverage")]
    public async Task ExecuteAsync_WithValidType_ShouldProcess(string type)
    {
        // Arrange
        var request = new TestPlanningRequest
        {
            Type = type,
            FeatureDescription = "User authentication feature",
            Requirements = "Secure login with MFA",
            TestFramework = "xUnit"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownType_ShouldReturnFailure()
    {
        // Arrange
        var request = new TestPlanningRequest
        {
            Type = "invalid_type",
            FeatureDescription = "Test"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown type");
    }
}
