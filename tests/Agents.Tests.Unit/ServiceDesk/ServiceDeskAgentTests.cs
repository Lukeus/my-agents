using Agents.Application.Core;
using Agents.Application.ServiceDesk;
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

namespace Agents.Tests.Unit.ServiceDesk;

public class ServiceDeskAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IInputSanitizer> _mockInputSanitizer;
    private readonly Mock<ILogger<ServiceDeskAgent>> _mockLogger;
    private readonly ServiceDeskAgent _agent;

    public ServiceDeskAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockInputSanitizer = new Mock<IInputSanitizer>();
        _mockLogger = new Mock<ILogger<ServiceDeskAgent>>();

        _mockInputSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns<string>(input => input);

        _agent = new ServiceDeskAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockInputSanitizer.Object,
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
        var (kernel, mockChat) = SemanticKernelTestHelper.CreateMockKernel(
            "{\"priority\":\"high\",\"category\":\"technical\",\"solution\":\"Check logs\"}");
        SemanticKernelTestHelper.SetupMockLLMProvider(_mockLLMProvider, kernel);

        // Setup prompt loader to return valid prompts without template variables
        var prompt = SemanticKernelTestHelper.CreateMockPrompt("Analyze the support ticket and provide resolution guidance.", $"servicedesk-{action}");
        _mockPromptLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

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
