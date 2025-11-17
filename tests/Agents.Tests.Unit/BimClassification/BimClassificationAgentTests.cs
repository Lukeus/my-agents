using System.Text.Json;
using Agents.Application.BimClassification;
using Agents.Application.BimClassification.Requests;
using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Models;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Moq;
using Xunit;

namespace Agents.Tests.Unit.BimClassification;

public class BimClassificationAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IInputSanitizer> _mockInputSanitizer;
    private readonly Mock<ILogger<BimClassificationAgent>> _mockLogger;
    private readonly BimClassificationAgent _agent;

    public BimClassificationAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockInputSanitizer = new Mock<IInputSanitizer>();
        _mockLogger = new Mock<ILogger<BimClassificationAgent>>();

        _mockInputSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns<string>(input => input);

        _agent = new BimClassificationAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockInputSanitizer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldProcessSuccessfully()
    {
        // Arrange
        var mockChatCompletion = new Mock<IChatCompletionService>();
        var validJson = @"{
            ""bimElementId"": 123,
            ""commodityCode"": ""WALL-001"",
            ""pricingCode"": ""PRICE-W001"",
            ""derivedItems"": [],
            ""reasoningSummary"": ""Concrete exterior wall classified as WALL-001 based on material and type.""
        }";
        var chatMessageContent = new ChatMessageContent(AuthorRole.Assistant, validJson);

        mockChatCompletion
            .Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { chatMessageContent });

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(mockChatCompletion.Object);
        var kernel = kernelBuilder.Build();

        _mockLLMProvider.Setup(p => p.GetKernel()).Returns(kernel);

        var mockPrompt = new Prompt
        {
            Content = "Test prompt for {elementJson}",
            Metadata = new PromptMetadata
            {
                Name = "bim-classifier",
                Version = "1.0",
                Description = "Test prompt for BIM classification"
            },
            FilePath = "/test/prompt.prompt",
            ContentHash = "abc123",
            LoadedAt = DateTime.UtcNow
        };

        _mockPromptLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPrompt);

        var request = new ClassifyBimElementRequest
        {
            BimElementId = 123L,
            ElementJson = JsonSerializer.Serialize(new
            {
                name = "Exterior Wall",
                type = "Wall",
                material = "Concrete"
            }),
            ExistingClassificationJson = "{}"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Output.Should().NotBeNullOrEmpty();
        result.Metadata.Should().ContainKey("BimElementId");
        result.Metadata["BimElementId"].Should().Be(123L);

        // Verify LLM was invoked
        _mockLLMProvider.Verify(p => p.GetKernel(), Times.AtLeastOnce);
        _mockPromptLoader.Verify(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mockChatCompletion.Verify(
            c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "LLM chat completion should be invoked");
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
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }


    [Fact]
    public async Task ExecuteAsync_WhenPromptNotFound_ShouldReturnFailure()
    {
        // Arrange
        _mockPromptLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("Prompt file not found"));

        var request = new ClassifyBimElementRequest
        {
            BimElementId = 789L,
            ElementJson = JsonSerializer.Serialize(new
            {
                name = "Fire Door",
                type = "Door",
                material = "Steel"
            }),
            ExistingClassificationJson = "{}"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Error");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ExecuteAsync_WithMissingElementJson_ShouldHandleGracefully(string? elementJson)
    {
        // Arrange
        var mockChatCompletion = new Mock<IChatCompletionService>();
        var validJson = @"{
            ""bimElementId"": 999,
            ""commodityCode"": ""UNKNOWN"",
            ""pricingCode"": null,
            ""derivedItems"": [],
            ""reasoningSummary"": ""Unable to classify element due to missing or invalid JSON data.""
        }";
        var chatMessageContent = new ChatMessageContent(AuthorRole.Assistant, validJson);

        mockChatCompletion
            .Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { chatMessageContent });

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(mockChatCompletion.Object);
        var kernel = kernelBuilder.Build();

        _mockLLMProvider.Setup(p => p.GetKernel()).Returns(kernel);

        var mockPrompt = new Prompt
        {
            Content = "Test prompt",
            Metadata = new PromptMetadata
            {
                Name = "test",
                Version = "1.0",
                Description = "Test prompt for edge case"
            },
            FilePath = "/test/prompt.prompt",
            ContentHash = "abc123",
            LoadedAt = DateTime.UtcNow
        };

        _mockPromptLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPrompt);

        var request = new ClassifyBimElementRequest
        {
            BimElementId = 999L,
            ElementJson = elementJson,
            ExistingClassificationJson = "{}"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert - Should complete without throwing, metadata should contain BimElementId
        result.Should().NotBeNull();
        result.Metadata.Should().ContainKey("BimElementId");
        result.Metadata["BimElementId"].Should().Be(999L);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSanitizeInput()
    {
        // Arrange
        var mockChatCompletion = new Mock<IChatCompletionService>();
        var validJson = @"{
            ""bimElementId"": 101,
            ""commodityCode"": ""WINDOW-001"",
            ""pricingCode"": ""PRICE-WIN001"",
            ""derivedItems"": [],
            ""reasoningSummary"": ""Glass window classified as WINDOW-001.""
        }";
        var chatMessageContent = new ChatMessageContent(AuthorRole.Assistant, validJson);

        mockChatCompletion
            .Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { chatMessageContent });

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(mockChatCompletion.Object);
        var kernel = kernelBuilder.Build();

        _mockLLMProvider.Setup(p => p.GetKernel()).Returns(kernel);

        var mockPrompt = new Prompt
        {
            Content = "Test prompt",
            Metadata = new PromptMetadata
            {
                Name = "test",
                Version = "1.0",
                Description = "Test prompt for sanitizer verification"
            },
            FilePath = "/test/prompt.prompt",
            ContentHash = "abc123",
            LoadedAt = DateTime.UtcNow
        };

        _mockPromptLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPrompt);

        var request = new ClassifyBimElementRequest
        {
            BimElementId = 101L,
            ElementJson = JsonSerializer.Serialize(new
            {
                name = "Window",
                type = "Window",
                material = "Glass"
            }),
            ExistingClassificationJson = "{}"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();

        // Verify sanitizer was called
        _mockInputSanitizer.Verify(
            s => s.Sanitize(It.IsAny<string>()),
            Times.AtLeastOnce,
            "Input sanitizer should be called for prompt variables");
    }
}
