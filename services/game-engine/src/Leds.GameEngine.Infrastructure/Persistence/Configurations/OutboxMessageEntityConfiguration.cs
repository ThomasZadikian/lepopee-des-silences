using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("game_engine_outbox_messages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(128).IsRequired();
        builder.Property(e => e.EventVersion).HasColumnName("event_version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at_utc");
        builder.Property(e => e.RetryCount).HasColumnName("retry_count");
        builder.Property(e => e.LastError).HasColumnName("last_error");
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        builder.Property(e => e.CausationId).HasColumnName("causation_id");
        builder.Property(e => e.Destination).HasColumnName("destination").HasMaxLength(128);

        builder.HasIndex(e => e.ProcessedAtUtc);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.OccurredAtUtc);
    }
}
