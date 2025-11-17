using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Models;
using Agents.Infrastructure.Prompts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace Agents.Tests.Unit.Helpers;

/// <summary>
/// Helper class for creating Semantic Kernel mocks in unit tests
/// </summary>
public static class SemanticKernelTestHelper
{
    /// <summary>
    /// Creates a mock Kernel with a mock ChatCompletionService that returns the specified JSON response
    /// </summary>
    public static (Kernel kernel, Mock<IChatCompletionService> mockChatCompletion) CreateMockKernel(string jsonResponse)
    {
        var mockChatCompletion = new Mock<IChatCompletionService>();
        var chatMessageContent = new ChatMessageContent(AuthorRole.Assistant, jsonResponse);

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

        return (kernel, mockChatCompletion);
    }

    /// <summary>
    /// Creates a mock prompt with the specified content and metadata
    /// </summary>
    public static Prompt CreateMockPrompt(string content, string name = "test-prompt", string version = "1.0")
    {
        return new Prompt
        {
            Content = content,
            Metadata = new PromptMetadata
            {
                Name = name,
                Version = version,
                Description = $"Test prompt: {name}"
            },
            FilePath = $"/test/{name}.prompt",
            ContentHash = "test-hash-123",
            LoadedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sets up a mock LLM provider to return the specified kernel
    /// </summary>
    public static void SetupMockLLMProvider(Mock<ILLMProvider> mockProvider, Kernel kernel)
    {
        mockProvider.Setup(p => p.GetKernel()).Returns(kernel);
        mockProvider.Setup(p => p.ProviderName).Returns("MockProvider");
        mockProvider.Setup(p => p.IsAvailable).Returns(true);
    }

    /// <summary>
    /// Sets up a mock prompt loader to return the specified prompt
    /// </summary>
    public static void SetupMockPromptLoader(Mock<Agents.Infrastructure.Prompts.Services.IPromptLoader> mockLoader, Prompt prompt)
    {
        mockLoader.Setup(p => p.LoadPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);
    }

    /// <summary>
    /// Sets up a mock prompt loader to return different prompts based on the prompt name
    /// </summary>
    public static void SetupMockPromptLoaderWithMultiplePrompts(
        Mock<Agents.Infrastructure.Prompts.Services.IPromptLoader> mockLoader,
        Dictionary<string, Prompt> prompts)
    {
        foreach (var kvp in prompts)
        {
            mockLoader.Setup(p => p.LoadPromptAsync(
                    It.Is<string>(name => name.Contains(kvp.Key)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(kvp.Value);
        }
    }
}
