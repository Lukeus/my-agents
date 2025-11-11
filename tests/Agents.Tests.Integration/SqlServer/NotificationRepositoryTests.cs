using Agents.Domain.Notification.Entities;
using Agents.Infrastructure.Persistence.SqlServer;
using Agents.Infrastructure.Persistence.SqlServer.Data;
using Agents.Infrastructure.Persistence.SqlServer.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Agents.Tests.Integration.SqlServer;

/// <summary>
/// Integration tests for NotificationRepository using SQL Server Testcontainers.
/// </summary>
public class NotificationRepositoryTests : IAsyncLifetime
{
    private MsSqlContainer? _msSqlContainer;
    private IServiceProvider? _serviceProvider;
    private AgentsDbContext? _context;

    public async Task InitializeAsync()
    {
        // Start SQL Server container
        _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("StrongP@ssw0rd!")
            .Build();

        await _msSqlContainer.StartAsync();

        // Setup DI container
        var services = new ServiceCollection();
        services.AddSqlServerPersistence(_msSqlContainer.GetConnectionString());

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AgentsDbContext>();

        // Apply migrations
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (_msSqlContainer != null)
        {
            await _msSqlContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task AddAsync_ShouldPersistNotification()
    {
        // Arrange
        var repository = _serviceProvider!.GetRequiredService<INotificationRepository>();
        var notification = new Notification(
            id: Guid.NewGuid().ToString(),
            channel: "email",
            recipient: "test@example.com",
            subject: "Test Subject",
            content: "Test Content");

        // Act
        await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        // Assert
        var retrieved = await repository.GetByIdAsync(notification.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Channel.Should().Be("email");
        retrieved.Recipient.Should().Be("test@example.com");
        retrieved.Subject.Should().Be("Test Subject");
        retrieved.Content.Should().Be("Test Content");
        retrieved.Status.Should().Be(NotificationStatus.Pending);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNotification()
    {
        // Arrange
        var repository = _serviceProvider!.GetRequiredService<INotificationRepository>();
        var notification = new Notification(
            id: Guid.NewGuid().ToString(),
            channel: "sms",
            recipient: "+1234567890",
            subject: string.Empty,
            content: "SMS Content");

        await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        // Act
        notification.MarkAsSent();
        await repository.UpdateAsync(notification);
        await repository.SaveChangesAsync();

        // Assert
        var retrieved = await repository.GetByIdAsync(notification.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(NotificationStatus.Sent);
        retrieved.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnNotificationsByStatus()
    {
        // Arrange
        var repository = _serviceProvider!.GetRequiredService<INotificationRepository>();
        
        var notification1 = new Notification(Guid.NewGuid().ToString(), "email", "user1@test.com", "Sub1", "Content1");
        var notification2 = new Notification(Guid.NewGuid().ToString(), "email", "user2@test.com", "Sub2", "Content2");
        var notification3 = new Notification(Guid.NewGuid().ToString(), "sms", "+123", string.Empty, "SMS");
        
        notification1.MarkAsSent();
        notification3.MarkAsSent();

        await repository.AddAsync(notification1);
        await repository.AddAsync(notification2);
        await repository.AddAsync(notification3);
        await repository.SaveChangesAsync();

        // Act
        var sentNotifications = await repository.GetByStatusAsync(NotificationStatus.Sent);
        var pendingNotifications = await repository.GetByStatusAsync(NotificationStatus.Pending);

        // Assert
        sentNotifications.Should().HaveCount(2);
        sentNotifications.Should().Contain(n => n.Id == notification1.Id);
        sentNotifications.Should().Contain(n => n.Id == notification3.Id);
        
        pendingNotifications.Should().HaveCount(1);
        pendingNotifications.Should().Contain(n => n.Id == notification2.Id);
    }

    [Fact]
    public async Task GetByChannelAsync_ShouldReturnNotificationsByChannel()
    {
        // Arrange
        var repository = _serviceProvider!.GetRequiredService<INotificationRepository>();
        
        var emailNotification = new Notification(Guid.NewGuid().ToString(), "email", "user@test.com", "Sub", "Content");
        var smsNotification = new Notification(Guid.NewGuid().ToString(), "sms", "+123", string.Empty, "SMS");

        await repository.AddAsync(emailNotification);
        await repository.AddAsync(smsNotification);
        await repository.SaveChangesAsync();

        // Act
        var emailNotifications = await repository.GetByChannelAsync("email");
        var smsNotifications = await repository.GetByChannelAsync("sms");

        // Assert
        emailNotifications.Should().HaveCount(1);
        emailNotifications.First().Id.Should().Be(emailNotification.Id);
        
        smsNotifications.Should().HaveCount(1);
        smsNotifications.First().Id.Should().Be(smsNotification.Id);
    }

    [Fact]
    public async Task GetRetryableAsync_ShouldReturnFailedNotificationsUnderRetryLimit()
    {
        // Arrange
        var repository = _serviceProvider!.GetRequiredService<INotificationRepository>();
        
        var notification1 = new Notification(Guid.NewGuid().ToString(), "email", "user1@test.com", "Sub1", "Content1");
        var notification2 = new Notification(Guid.NewGuid().ToString(), "email", "user2@test.com", "Sub2", "Content2");
        var notification3 = new Notification(Guid.NewGuid().ToString(), "email", "user3@test.com", "Sub3", "Content3");

        // Fail notification1 once (retryable)
        notification1.MarkAsFailed("Error 1");
        
        // Fail notification2 three times (not retryable with maxRetries=3)
        notification2.MarkAsFailed("Error 1");
        notification2.MarkAsFailed("Error 2");
        notification2.MarkAsFailed("Error 3");
        
        // notification3 is still pending

        await repository.AddAsync(notification1);
        await repository.AddAsync(notification2);
        await repository.AddAsync(notification3);
        await repository.SaveChangesAsync();

        // Act
        var retryable = await repository.GetRetryableAsync(maxRetries: 3);

        // Assert
        retryable.Should().HaveCount(1);
        retryable.First().Id.Should().Be(notification1.Id);
    }

    [Fact]
    public async Task GetByRecipientAsync_ShouldReturnNotificationsByRecipient()
    {
        // Arrange
        var repository = _serviceProvider!.GetRequiredService<INotificationRepository>();
        
        var notification1 = new Notification(Guid.NewGuid().ToString(), "email", "user@test.com", "Sub1", "Content1");
        var notification2 = new Notification(Guid.NewGuid().ToString(), "sms", "user@test.com", "Sub2", "Content2");
        var notification3 = new Notification(Guid.NewGuid().ToString(), "email", "other@test.com", "Sub3", "Content3");

        await repository.AddAsync(notification1);
        await repository.AddAsync(notification2);
        await repository.AddAsync(notification3);
        await repository.SaveChangesAsync();

        // Act
        var userNotifications = await repository.GetByRecipientAsync("user@test.com");
        var otherNotifications = await repository.GetByRecipientAsync("other@test.com");

        // Assert
        userNotifications.Should().HaveCount(2);
        userNotifications.Should().Contain(n => n.Id == notification1.Id);
        userNotifications.Should().Contain(n => n.Id == notification2.Id);
        
        otherNotifications.Should().HaveCount(1);
        otherNotifications.First().Id.Should().Be(notification3.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveNotification()
    {
        // Arrange
        var repository = _serviceProvider!.GetRequiredService<INotificationRepository>();
        var notification = new Notification(
            id: Guid.NewGuid().ToString(),
            channel: "email",
            recipient: "delete@test.com",
            subject: "Delete Test",
            content: "Content");

        await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(notification.Id);
        await repository.SaveChangesAsync();

        // Assert
        var retrieved = await repository.GetByIdAsync(notification.Id);
        retrieved.Should().BeNull();
    }
}
