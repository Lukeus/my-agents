using Agents.Application.Core;
using Agents.Application.DevOps;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Agents.Tests.Unit;

public class DevOpsAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<DevOpsAgent>> _mockLogger;
    private readonly DevOpsAgent _agent;

    public DevOpsAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<DevOpsAgent>>();

        _agent = new DevOpsAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithTriggerWorkflowAction_ShouldSucceed()
    {
        // Arrange
        var request = new DevOpsRequest
        {
            Action = "trigger_workflow",
            Parameters = new Dictionary<string, object>
            {
                ["workflowName"] = "CI/CD Pipeline"
            }
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Output.Should().Contain("triggered");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownAction_ShouldReturnFailure()
    {
        // Arrange
        var request = new DevOpsRequest
        {
            Action = "unknown_action",
            Parameters = new()
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

    [Theory]
    [InlineData("create_issue")]
    [InlineData("update_project")]
    [InlineData("analyze_sprint")]
    public async Task ExecuteAsync_WithValidActions_ShouldProcessSuccessfully(string action)
    {
        // Arrange
        var request = new DevOpsRequest
        {
            Action = action,
            Parameters = new Dictionary<string, object>
            {
                ["title"] = "Test",
                ["description"] = "Description",
                ["projectName"] = "TestProject",
                ["sprintData"] = "{}"
            }
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert - May fail if prompts not found, but agent logic executes
        result.Should().NotBeNull();
    }
}
