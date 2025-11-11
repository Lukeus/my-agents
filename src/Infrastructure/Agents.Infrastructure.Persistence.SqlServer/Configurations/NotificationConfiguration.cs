using Agents.Domain.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agents.Infrastructure.Persistence.SqlServer.Configurations;

/// <summary>
/// Entity Framework configuration for Notification aggregate.
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.Channel)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Recipient)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Subject)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Content)
            .HasMaxLength(10000)
            .IsRequired();

        builder.Property(n => n.FormattedContent)
            .HasMaxLength(10000)
            .IsRequired(false);

        builder.Property(n => n.SentAt)
            .IsRequired(false);

        builder.Property(n => n.DeliveredAt)
            .IsRequired(false);

        builder.Property(n => n.ErrorMessage)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(n => n.RetryCount)
            .IsRequired();

        // Ignore domain events - they are transient
        builder.Ignore(n => n.DomainEvents);

        // Indexes
        builder.HasIndex(n => n.Channel);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.SentAt);
    }
}
