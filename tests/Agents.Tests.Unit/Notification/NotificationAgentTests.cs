using System.Text.Json;
using Agents.Application.Core;
using Agents.Application.Notification;
using Agents.Application.Notification.Channels;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using Agents.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Agents.Tests.Unit.Notification;

public class NotificationAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IPromptLoader> _mockPromptLoader;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<INotificationChannelFactory> _mockChannelFactory;
    private readonly Mock<IInputSanitizer> _mockInputSanitizer;
    private readonly Mock<ILogger<NotificationAgent>> _mockLogger;
    private readonly NotificationAgent _agent;

    public NotificationAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockPromptLoader = new Mock<IPromptLoader>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockChannelFactory = new Mock<INotificationChannelFactory>();
        _mockInputSanitizer = new Mock<IInputSanitizer>();
        _mockLogger = new Mock<ILogger<NotificationAgent>>();

        _mockInputSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns<string>(input => input);

        _agent = new NotificationAgent(
            _mockLLMProvider.Object,
            _mockPromptLoader.Object,
            _mockEventPublisher.Object,
            _mockChannelFactory.Object,
            _mockInputSanitizer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidEmailRequest_ShouldSendSuccessfully()
    {
        // Arrange
        var request = new NotificationRequest
        {
            Channel = "email",
            Recipient = "test@example.com",
            Subject = "Test Subject",
            Content = "Test Content"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        var mockChannel = new Mock<INotificationChannel>();
        mockChannel.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChannelResult.Success("msg-123"));

        _mockChannelFactory.Setup(f => f.CreateChannel("email"))
            .Returns(mockChannel.Object);

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Output.Should().Contain("successfully");
        result.Metadata.Should().ContainKey("Channel");
        result.Metadata["Channel"].Should().Be("email");
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
    public async Task ExecuteAsync_WithChannelFailure_ShouldReturnFailure()
    {
        // Arrange
        var request = new NotificationRequest
        {
            Channel = "email",
            Recipient = "test@example.com",
            Subject = "Test",
            Content = "Content"
        };
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext();

        var mockChannel = new Mock<INotificationChannel>();
        mockChannel.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChannelResult.Failure("Network error"));

        _mockChannelFactory.Setup(f => f.CreateChannel("email"))
            .Returns(mockChannel.Object);

        // Act
        var result = await _agent.ExecuteAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Network error");
        result.Metadata.Should().ContainKey("CanRetry");
    }
}
