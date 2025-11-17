using System.Text.Json;
using Agents.Application.Core;
using Agents.Application.DevOps;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using Agents.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Xunit;

namespace Agents.Tests.Unit.DevOps;

public class DevOpsAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IInputSanitizer> _mockInputSanitizer;
    private readonly Mock<ILogger<DevOpsAgent>> _mockLogger;
    private readonly DevOpsAgent _agent;

    public DevOpsAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockInputSanitizer = new Mock<IInputSanitizer>();
        _mockLogger = new Mock<ILogger<DevOpsAgent>>();

        // Setup sanitizer to pass through by default
        _mockInputSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns<string>(input => input);

        _agent = new DevOpsAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockInputSanitizer.Object,
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
    [InlineData("create_issue", "Issue created successfully")]
    [InlineData("update_project", "Project updated")]
    [InlineData("analyze_sprint", "Sprint analysis complete")]
    public async Task ExecuteAsync_WithValidActions_ShouldProcessSuccessfully(string action, string expectedOutput)
    {
        // Arrange
        var (kernel, mockChat) = SemanticKernelTestHelper.CreateMockKernel(
            "{\"result\":\"success\",\"details\":\"Operation completed\"}");
        SemanticKernelTestHelper.SetupMockLLMProvider(_mockLLMProvider, kernel);

        // Setup prompt loader to return valid prompts without template variables
        var prompt = SemanticKernelTestHelper.CreateMockPrompt("Analyze the DevOps request and provide recommendations.", $"devops-{action}");
        _mockPromptLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

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

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Output.Should().Contain(expectedOutput);

        // Verify LLM was invoked for actions that use it
        if (action != "trigger_workflow")
        {
            mockChat.Verify(
                c => c.GetChatMessageContentsAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<PromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
    }
}
