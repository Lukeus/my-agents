using Agents.Application.Core;
using Agents.Application.TestPlanning;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using Agents.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using System.Text.Json;
using Xunit;

namespace Agents.Tests.Unit.TestPlanning;

public class TestPlanningAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IInputSanitizer> _mockInputSanitizer;
    private readonly Mock<ILogger<TestPlanningAgent>> _mockLogger;
    private readonly TestPlanningAgent _agent;

    public TestPlanningAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockInputSanitizer = new Mock<IInputSanitizer>();
        _mockLogger = new Mock<ILogger<TestPlanningAgent>>();

        _mockInputSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns<string>(input => input);

        _agent = new TestPlanningAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockInputSanitizer.Object,
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
        var (kernel, mockChat) = SemanticKernelTestHelper.CreateMockKernel(
            "{\"testCases\":[{\"name\":\"Login test\",\"steps\":[\"Open app\",\"Enter credentials\"]}]}");
        SemanticKernelTestHelper.SetupMockLLMProvider(_mockLLMProvider, kernel);

        // Setup prompt loader to return valid prompts without template variables
        var prompt = SemanticKernelTestHelper.CreateMockPrompt("Generate a comprehensive test plan for the given feature.", $"testplanning-{type}");
        _mockPromptLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

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
        result.IsSuccess.Should().BeTrue();
        result.Output.Should().NotBeNullOrEmpty();

        // Verify LLM was invoked
        mockChat.Verify(
            c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
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
