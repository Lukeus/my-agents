using Agents.Domain.BimClassification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agents.Infrastructure.Persistence.SqlServer.Configurations;

/// <summary>
/// Entity Framework configuration for BimClassificationSuggestion aggregate.
/// </summary>
public class BimClassificationSuggestionConfiguration : IEntityTypeConfiguration<BimClassificationSuggestion>
{
    public void Configure(EntityTypeBuilder<BimClassificationSuggestion> builder)
    {
        builder.ToTable("BimClassificationSuggestions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .IsRequired();

        builder.Property(s => s.BimElementId)
            .IsRequired();

        builder.Property(s => s.SuggestedCommodityCode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(s => s.SuggestedPricingCode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(s => s.ReasoningSummary)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.CreatedUtc)
            .IsRequired();

        builder.Property(s => s.ReviewedUtc)
            .IsRequired(false);

        builder.Property(s => s.ReviewedBy)
            .HasMaxLength(200)
            .IsRequired(false);

        // Configure DerivedItems as owned collection (stored as JSON)
        builder.OwnsMany(s => s.DerivedItems, derivedItem =>
        {
            derivedItem.ToJson();

            derivedItem.Property(d => d.DerivedCommodityCode)
                .IsRequired();

            derivedItem.Property(d => d.DerivedPricingCode)
                .IsRequired(false);

            derivedItem.Property(d => d.QuantityFormula)
                .IsRequired();

            derivedItem.Property(d => d.QuantityUnit)
                .IsRequired();
        });

        // Ignore domain events - they are transient
        builder.Ignore(s => s.DomainEvents);

        // Indexes
        builder.HasIndex(s => s.BimElementId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CreatedUtc);
    }
}
