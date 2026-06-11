using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Player.Infrastructure.Persistence.Configurations;

public sealed class ProcessedIntegrationEventEntityConfiguration : IEntityTypeConfiguration<ProcessedIntegrationEventEntity>
{
    public void Configure(EntityTypeBuilder<ProcessedIntegrationEventEntity> builder)
    {
        builder.ToTable("player_processed_integration_events");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId).HasColumnName("event_id");
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at_utc");

        builder.HasIndex(e => e.ProcessedAtUtc);
    }
}
