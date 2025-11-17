using System.Text.Json;
using Agents.Application.Core;
using Agents.Application.Implementation;
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

namespace Agents.Tests.Unit.Implementation;

public class ImplementationAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IInputSanitizer> _mockInputSanitizer;
    private readonly Mock<ILogger<ImplementationAgent>> _mockLogger;
    private readonly ImplementationAgent _agent;

    public ImplementationAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockInputSanitizer = new Mock<IInputSanitizer>();
        _mockLogger = new Mock<ILogger<ImplementationAgent>>();

        _mockInputSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns<string>(input => input);

        _agent = new ImplementationAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockInputSanitizer.Object,
            _mockLogger.Object);
    }

    [Theory]
    [InlineData("generate_code")]
    [InlineData("review_code")]
    [InlineData("suggest_refactoring")]
    public async Task ExecuteAsync_WithValidActions_ShouldProcess(string action)
    {
        // Arrange
        var (kernel, mockChat) = SemanticKernelTestHelper.CreateMockKernel(
            "{\"code\":\"public class UserController { }\",\"suggestions\":[\"Add validation\"]}");
        SemanticKernelTestHelper.SetupMockLLMProvider(_mockLLMProvider, kernel);

        // Setup prompt loader to return valid prompts without template variables
        var prompt = SemanticKernelTestHelper.CreateMockPrompt("Generate code implementation based on the specification.", $"implementation-{action}");
        _mockPromptLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

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
