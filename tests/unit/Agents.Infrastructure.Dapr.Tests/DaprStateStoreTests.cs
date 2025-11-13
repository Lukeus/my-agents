using Agents.Infrastructure.Dapr.State;
using Dapr.Client;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Agents.Infrastructure.Dapr.Tests;

public class DaprStateStoreTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly Mock<ILogger<DaprStateStore>> _mockLogger;
    private readonly DaprStateStore _stateStore;

    public DaprStateStoreTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        _mockLogger = new Mock<ILogger<DaprStateStore>>();
        _stateStore = new DaprStateStore(_mockDaprClient.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDaprClientIsNull()
    {
        // Act
        var act = () => new DaprStateStore(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("daprClient");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new DaprStateStore(_mockDaprClient.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStateAsync_ShouldThrowArgumentException_WhenKeyIsNullOrWhitespace(string? key)
    {
        // Act
        var act = async () => await _stateStore.GetStateAsync<string>(key!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public async Task GetStateAsync_ShouldReturnValue_WhenKeyExists()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = "test-value";

        _mockDaprClient
            .Setup(x => x.GetStateAsync<string>(
                "agents-statestore",
                key,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _stateStore.GetStateAsync<string>(key, CancellationToken.None);

        // Assert
        result.Should().Be(expectedValue);
        _mockDaprClient.Verify(
            x => x.GetStateAsync<string>(
                "agents-statestore",
                key,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStateAsync_ShouldReturnDefault_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "non-existent-key";

        _mockDaprClient
            .Setup(x => x.GetStateAsync<string>(
                "agents-statestore",
                key,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => default(string)!);

        // Act
        var result = await _stateStore.GetStateAsync<string>(key, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStateAsync_ShouldThrow_WhenDaprClientThrows()
    {
        // Arrange
        var key = "test-key";
        var expectedException = new Exception("Dapr error");

        _mockDaprClient
            .Setup(x => x.GetStateAsync<string>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = async () => await _stateStore.GetStateAsync<string>(key, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Dapr error");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveStateAsync_ShouldThrowArgumentException_WhenKeyIsNullOrWhitespace(string? key)
    {
        // Act
        var act = async () => await _stateStore.SaveStateAsync(key!, "value", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public async Task SaveStateAsync_ShouldSaveState_WhenValidKeyAndValueProvided()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        _mockDaprClient
            .Setup(x => x.SaveStateAsync(
                "agents-statestore",
                key,
                value,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _stateStore.SaveStateAsync(key, value, CancellationToken.None);

        // Assert
        _mockDaprClient.Verify();
    }

    [Fact]
    public async Task SaveStateAsync_ShouldThrow_WhenDaprClientThrows()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var expectedException = new Exception("Dapr error");

        _mockDaprClient
            .Setup(x => x.SaveStateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = async () => await _stateStore.SaveStateAsync(key, value, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Dapr error");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteStateAsync_ShouldThrowArgumentException_WhenKeyIsNullOrWhitespace(string? key)
    {
        // Act
        var act = async () => await _stateStore.DeleteStateAsync(key!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public async Task DeleteStateAsync_ShouldDeleteState_WhenValidKeyProvided()
    {
        // Arrange
        var key = "test-key";

        _mockDaprClient
            .Setup(x => x.DeleteStateAsync(
                "agents-statestore",
                key,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _stateStore.DeleteStateAsync(key, CancellationToken.None);

        // Assert
        _mockDaprClient.Verify();
    }

    [Fact]
    public async Task DeleteStateAsync_ShouldThrow_WhenDaprClientThrows()
    {
        // Arrange
        var key = "test-key";
        var expectedException = new Exception("Dapr error");

        _mockDaprClient
            .Setup(x => x.DeleteStateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = async () => await _stateStore.DeleteStateAsync(key, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Dapr error");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsAsync_ShouldThrowArgumentException_WhenKeyIsNullOrWhitespace(string? key)
    {
        // Act
        var act = async () => await _stateStore.ExistsAsync(key!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var key = "existing-key";

        _mockDaprClient
            .Setup(x => x.GetStateAsync<object>(
                "agents-statestore",
                key,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // Act
        var result = await _stateStore.ExistsAsync(key, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "non-existent-key";

        _mockDaprClient
            .Setup(x => x.GetStateAsync<object>(
                "agents-statestore",
                key,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => default(object)!);

        // Act
        var result = await _stateStore.ExistsAsync(key, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetBulkStateAsync_ShouldThrowArgumentNullException_WhenKeysIsNull()
    {
        // Act
        var act = async () => await _stateStore.GetBulkStateAsync<string>(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("keys");
    }

    [Fact]
    public async Task GetBulkStateAsync_ShouldReturnEmptyDictionary_WhenKeysIsEmpty()
    {
        // Arrange
        var emptyKeys = Enumerable.Empty<string>();

        // Act
        var result = await _stateStore.GetBulkStateAsync<string>(emptyKeys, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _mockDaprClient.Verify(
            x => x.GetBulkStateAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetBulkStateAsync_ShouldReturnMultipleValues_WhenValidKeysProvided()
    {
        // Arrange
        var keys = new List<string> { "key1", "key2", "key3" };
        var bulkStateItems = new List<BulkStateItem>
        {
            new BulkStateItem("key1", "\"value1\"", null!),
            new BulkStateItem("key2", "\"value2\"", null!),
            new BulkStateItem("key3", "\"value3\"", null!)
        };

        _mockDaprClient
            .Setup(x => x.GetBulkStateAsync(
                "agents-statestore",
                It.IsAny<IReadOnlyList<string>>(),
                10,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkStateItems);

        // Act
        var result = await _stateStore.GetBulkStateAsync<string>(keys, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result["key1"].Should().Be("value1");
        result["key2"].Should().Be("value2");
        result["key3"].Should().Be("value3");
    }
}
