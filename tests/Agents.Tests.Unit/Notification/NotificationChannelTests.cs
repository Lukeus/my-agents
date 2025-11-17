using Agents.Application.Notification.Channels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Agents.Tests.Unit.Notification;

public class EmailChannelTests
{
    private readonly Mock<ILogger<EmailChannel>> _mockLogger;
    private readonly EmailChannel _channel;

    public EmailChannelTests()
    {
        _mockLogger = new Mock<ILogger<EmailChannel>>();
        _channel = new EmailChannel(_mockLogger.Object);
    }

    [Fact]
    public void ChannelName_ShouldReturnEmail()
    {
        // Assert
        _channel.ChannelName.Should().Be("Email");
    }

    [Fact]
    public async Task SendAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var content = "Test Content";

        // Act
        var result = await _channel.SendAsync(recipient, subject, content);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ShouldGenerateUniqueMessageIds()
    {
        // Act
        var result1 = await _channel.SendAsync("test1@example.com", "Subject", "Content");
        var result2 = await _channel.SendAsync("test2@example.com", "Subject", "Content");

        // Assert
        result1.MessageId.Should().NotBe(result2.MessageId);
    }

    [Fact]
    public async Task SendAsync_ShouldLogInformation()
    {
        // Act
        await _channel.SendAsync("test@example.com", "Subject", "Content");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending email")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

public class SlackChannelTests
{
    private readonly Mock<ILogger<SlackChannel>> _mockLogger;
    private readonly SlackChannel _channel;

    public SlackChannelTests()
    {
        _mockLogger = new Mock<ILogger<SlackChannel>>();
        _channel = new SlackChannel(_mockLogger.Object);
    }

    [Fact]
    public void ChannelName_ShouldReturnSlack()
    {
        // Assert
        _channel.ChannelName.Should().Be("Slack");
    }

    [Fact]
    public async Task SendAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var recipient = "#general";
        var subject = "Test Subject";
        var content = "Test Content";

        // Act
        var result = await _channel.SendAsync(recipient, subject, content);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ShouldGenerateUniqueMessageIds()
    {
        // Act
        var result1 = await _channel.SendAsync("#channel1", "Subject", "Content");
        var result2 = await _channel.SendAsync("#channel2", "Subject", "Content");

        // Assert
        result1.MessageId.Should().NotBe(result2.MessageId);
    }

    [Fact]
    public async Task SendAsync_ShouldLogInformation()
    {
        // Act
        await _channel.SendAsync("#general", "Subject", "Content");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending Slack message")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

public class SmsChannelTests
{
    private readonly Mock<ILogger<SmsChannel>> _mockLogger;
    private readonly SmsChannel _channel;

    public SmsChannelTests()
    {
        _mockLogger = new Mock<ILogger<SmsChannel>>();
        _channel = new SmsChannel(_mockLogger.Object);
    }

    [Fact]
    public void ChannelName_ShouldReturnSMS()
    {
        // Assert
        _channel.ChannelName.Should().Be("SMS");
    }

    [Fact]
    public async Task SendAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var recipient = "+1234567890";
        var subject = "Test Subject";
        var content = "Test Content";

        // Act
        var result = await _channel.SendAsync(recipient, subject, content);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ShouldGenerateUniqueMessageIds()
    {
        // Act
        var result1 = await _channel.SendAsync("+1111111111", "Subject", "Content");
        var result2 = await _channel.SendAsync("+2222222222", "Subject", "Content");

        // Assert
        result1.MessageId.Should().NotBe(result2.MessageId);
    }

    [Fact]
    public async Task SendAsync_ShouldLogInformation()
    {
        // Act
        await _channel.SendAsync("+1234567890", "Subject", "Content");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending SMS")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

public class TeamsChannelTests
{
    private readonly Mock<ILogger<TeamsChannel>> _mockLogger;
    private readonly TeamsChannel _channel;

    public TeamsChannelTests()
    {
        _mockLogger = new Mock<ILogger<TeamsChannel>>();
        _channel = new TeamsChannel(_mockLogger.Object);
    }

    [Fact]
    public void ChannelName_ShouldReturnTeams()
    {
        // Assert
        _channel.ChannelName.Should().Be("Teams");
    }

    [Fact]
    public async Task SendAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var recipient = "team@company.com";
        var subject = "Test Subject";
        var content = "Test Content";

        // Act
        var result = await _channel.SendAsync(recipient, subject, content);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ShouldGenerateUniqueMessageIds()
    {
        // Act
        var result1 = await _channel.SendAsync("team1@company.com", "Subject", "Content");
        var result2 = await _channel.SendAsync("team2@company.com", "Subject", "Content");

        // Assert
        result1.MessageId.Should().NotBe(result2.MessageId);
    }

    [Fact]
    public async Task SendAsync_ShouldLogInformation()
    {
        // Act
        await _channel.SendAsync("team@company.com", "Subject", "Content");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending Teams message")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

public class ChannelResultTests
{
    [Fact]
    public void Success_WithMessageId_ShouldCreateSuccessResult()
    {
        // Act
        var result = ChannelResult.Success("msg-123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("msg-123");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Success_WithoutMessageId_ShouldCreateSuccessResult()
    {
        // Act
        var result = ChannelResult.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_WithErrorMessage_ShouldCreateFailureResult()
    {
        // Act
        var result = ChannelResult.Failure("Connection failed");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Connection failed");
        result.MessageId.Should().BeNull();
    }
}
