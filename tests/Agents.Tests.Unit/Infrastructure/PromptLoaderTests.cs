using Agents.Infrastructure.Prompts.Models;
using Agents.Infrastructure.Prompts.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Agents.Tests.Unit.Infrastructure;

public class PromptLoaderTests : IDisposable
{
    private readonly Mock<ILogger<PromptLoader>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly PromptLoader _promptLoader;
    private readonly List<string> _tempFiles = new();
    private readonly string _tempDirectory;

    public PromptLoaderTests()
    {
        _mockLogger = new Mock<ILogger<PromptLoader>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        _promptLoader = new PromptLoader(_mockLogger.Object, _memoryCache);
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"prompt_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);

        _memoryCache.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task LoadPromptAsync_WithValidFile_ShouldLoadAndCachePrompt()
    {
        // Arrange
        var promptContent = @"---
name: test-prompt
version: 1.0.0
description: Test prompt
author: Test Author
---
This is a test prompt with {{variable1}} and {{variable2}}.";

        var filePath = CreateTempPromptFile(promptContent);

        // Act
        var prompt = await _promptLoader.LoadPromptAsync(filePath);

        // Assert
        prompt.Should().NotBeNull();
        prompt.Metadata.Name.Should().Be("test-prompt");
        prompt.Metadata.Version.Should().Be("1.0.0");
        prompt.Metadata.Description.Should().Be("Test prompt");
        prompt.Content.Should().Contain("{{variable1}}");
        prompt.FilePath.Should().Be(filePath);
        prompt.ContentHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadPromptAsync_CalledTwice_ShouldUseCacheOnSecondCall()
    {
        // Arrange
        var promptContent = @"---
name: cached-prompt
version: 1.0.0
description: Cached prompt
---
Test content";

        var filePath = CreateTempPromptFile(promptContent);

        // Act
        var prompt1 = await _promptLoader.LoadPromptAsync(filePath);
        var prompt2 = await _promptLoader.LoadPromptAsync(filePath);

        // Assert
        prompt1.Should().BeSameAs(prompt2);
        prompt1.LoadedAt.Should().Be(prompt2.LoadedAt); // Cached instance
    }

    [Fact]
    public async Task LoadPromptAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.prompt");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _promptLoader.LoadPromptAsync(nonExistentPath));
    }

    [Fact]
    public async Task LoadPromptAsync_WithoutYamlFrontmatter_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var promptContent = "This is invalid content without YAML frontmatter";
        var filePath = CreateTempPromptFile(promptContent);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _promptLoader.LoadPromptAsync(filePath));

        exception.Message.Should().Contain("must start with YAML frontmatter");
    }

    [Fact]
    public async Task LoadPromptAsync_WithUnclosedYamlFrontmatter_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var promptContent = @"---
name: test
version: 1.0.0
description: Test
This is missing closing delimiter";

        var filePath = CreateTempPromptFile(promptContent);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _promptLoader.LoadPromptAsync(filePath));

        exception.Message.Should().Contain("closing YAML frontmatter delimiter");
    }

    [Fact]
    public async Task LoadPromptAsync_WithInvalidYaml_ShouldThrowException()
    {
        // Arrange
        var promptContent = @"---
name: test
version: 1.0.0
description: [unclosed array
---
Test content";

        var filePath = CreateTempPromptFile(promptContent);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            () => _promptLoader.LoadPromptAsync(filePath));
    }

    [Fact]
    public async Task LoadPromptsFromDirectoryAsync_WithMultipleFiles_ShouldLoadAllPrompts()
    {
        // Arrange
        var subDir = Path.Combine(_tempDirectory, "subdir");
        Directory.CreateDirectory(subDir);

        CreateTempPromptFile(@"---
name: prompt1
version: 1.0.0
description: First prompt
---
Content 1", Path.Combine(_tempDirectory, "prompt1.prompt"));

        CreateTempPromptFile(@"---
name: prompt2
version: 1.0.0
description: Second prompt
---
Content 2", Path.Combine(subDir, "prompt2.prompt"));

        // Act
        var prompts = await _promptLoader.LoadPromptsFromDirectoryAsync(_tempDirectory);

        // Assert
        prompts.Should().HaveCount(2);
        prompts.Select(p => p.Metadata.Name).Should().Contain(new[] { "prompt1", "prompt2" });
    }

    [Fact]
    public async Task LoadPromptsFromDirectoryAsync_WithNonExistentDirectory_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_tempDirectory, "nonexistent");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _promptLoader.LoadPromptsFromDirectoryAsync(nonExistentDir));
    }

    [Fact]
    public async Task LoadPromptsFromDirectoryAsync_WithPartialFailures_ShouldLoadSuccessfulPrompts()
    {
        // Arrange
        CreateTempPromptFile(@"---
name: valid-prompt
version: 1.0.0
description: Valid prompt
---
Valid content", Path.Combine(_tempDirectory, "valid.prompt"));

        CreateTempPromptFile("Invalid content without YAML",
            Path.Combine(_tempDirectory, "invalid.prompt"));

        // Act
        var prompts = await _promptLoader.LoadPromptsFromDirectoryAsync(_tempDirectory);

        // Assert
        prompts.Should().HaveCount(1);
        prompts[0].Metadata.Name.Should().Be("valid-prompt");
    }

    [Fact]
    public async Task LoadPromptsFromDirectoryAsync_CalledTwice_ShouldUseCacheOnSecondCall()
    {
        // Arrange
        CreateTempPromptFile(@"---
name: cached-dir-prompt
version: 1.0.0
description: Test
---
Content", Path.Combine(_tempDirectory, "test.prompt"));

        // Act
        var prompts1 = await _promptLoader.LoadPromptsFromDirectoryAsync(_tempDirectory);
        var prompts2 = await _promptLoader.LoadPromptsFromDirectoryAsync(_tempDirectory);

        // Assert
        prompts1.Should().HaveCount(1);
        prompts2.Should().HaveCount(1);
        // Cached result should be the same list instance
        prompts1.Should().BeSameAs(prompts2);
    }

    [Fact]
    public async Task LoadPromptsFromDirectoryAsync_WithCustomPattern_ShouldOnlyLoadMatchingFiles()
    {
        // Arrange
        CreateTempPromptFile(@"---
name: test1
version: 1.0.0
description: Test
---
Content", Path.Combine(_tempDirectory, "test.prompt"));

        CreateTempPromptFile(@"---
name: test2
version: 1.0.0
description: Test
---
Content", Path.Combine(_tempDirectory, "test.txt"));

        // Act
        var prompts = await _promptLoader.LoadPromptsFromDirectoryAsync(_tempDirectory, "*.prompt");

        // Assert
        prompts.Should().HaveCount(1);
        prompts[0].Metadata.Name.Should().Be("test1");
    }

    [Fact]
    public void WatchDirectory_WhenFileChanged_ShouldInvokeCallback()
    {
        // Arrange
        var changedFiles = new List<string>();
        var filePath = CreateTempPromptFile(@"---
name: watch-test
version: 1.0.0
description: Test
---
Content");

        using var watcher = _promptLoader.WatchDirectory(
            _tempDirectory,
            onChanged: (path) => changedFiles.Add(path),
            onCreated: (_) => { },
            onDeleted: (_) => { });

        // Act
        Thread.Sleep(100); // Give watcher time to initialize
        File.WriteAllText(filePath, @"---
name: watch-test
version: 1.0.1
description: Updated
---
Updated content");
        Thread.Sleep(500); // Give watcher time to detect change

        // Assert
        changedFiles.Should().Contain(filePath);
    }

    [Fact]
    public void WatchDirectory_WhenFileCreated_ShouldInvokeCallback()
    {
        // Arrange
        var createdFiles = new List<string>();

        using var watcher = _promptLoader.WatchDirectory(
            _tempDirectory,
            onChanged: (_) => { },
            onCreated: (path) => createdFiles.Add(path),
            onDeleted: (_) => { });

        // Act
        Thread.Sleep(100); // Give watcher time to initialize
        var newFilePath = CreateTempPromptFile(@"---
name: new-prompt
version: 1.0.0
description: New
---
Content");
        Thread.Sleep(500); // Give watcher time to detect creation

        // Assert
        createdFiles.Should().Contain(newFilePath);
    }

    [Fact]
    public void WatchDirectory_WhenFileDeleted_ShouldInvokeCallback()
    {
        // Arrange
        var deletedFiles = new List<string>();
        var filePath = CreateTempPromptFile(@"---
name: delete-test
version: 1.0.0
description: Test
---
Content");

        using var watcher = _promptLoader.WatchDirectory(
            _tempDirectory,
            onChanged: (_) => { },
            onCreated: (_) => { },
            onDeleted: (path) => deletedFiles.Add(path));

        // Act
        Thread.Sleep(100); // Give watcher time to initialize
        File.Delete(filePath);
        Thread.Sleep(500); // Give watcher time to detect deletion

        // Assert
        deletedFiles.Should().Contain(filePath);
    }

    [Fact]
    public async Task LoadPromptAsync_WithComplexMetadata_ShouldParseCorrectly()
    {
        // Arrange
        var promptContent = @"---
name: complex-prompt
version: 2.1.0-beta
description: Complex prompt with full metadata
author: Test Author
tags:
  - test
  - complex
model_requirements:
  min_tokens: 1000
  temperature: 0.7
  top_p: 0.9
input_schema:
  - name: user_input
    type: string
    required: true
    description: User input field
---
Complex prompt content with {{user_input}}";

        var filePath = CreateTempPromptFile(promptContent);

        // Act
        var prompt = await _promptLoader.LoadPromptAsync(filePath);

        // Assert
        prompt.Metadata.Name.Should().Be("complex-prompt");
        prompt.Metadata.Version.Should().Be("2.1.0-beta");
        prompt.Metadata.Tags.Should().Contain(new[] { "test", "complex" });
        prompt.Metadata.ModelRequirements.Should().NotBeNull();
        prompt.Metadata.ModelRequirements!.MinTokens.Should().Be(1000);
        prompt.Metadata.InputSchema.Should().HaveCount(1);
        prompt.Metadata.InputSchema![0].Name.Should().Be("user_input");
    }

    [Fact]
    public async Task LoadPromptAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var promptContent = @"---
name: test
version: 1.0.0
description: Test
---
Content";
        var filePath = CreateTempPromptFile(promptContent);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _promptLoader.LoadPromptAsync(filePath, cts.Token));
    }

    private string CreateTempPromptFile(string content, string? customPath = null)
    {
        var filePath = customPath ?? Path.Combine(_tempDirectory, $"{Guid.NewGuid()}.prompt");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, content);
        _tempFiles.Add(filePath);
        return filePath;
    }
}
