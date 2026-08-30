using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class AdaptiveInfluenceEntityConfiguration : IEntityTypeConfiguration<Entities.AdaptiveInfluenceEntity>
{
    public void Configure(EntityTypeBuilder<Entities.AdaptiveInfluenceEntity> builder)
    {
        builder.ToTable("run_adaptive_influences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.RunId).HasColumnName("run_id").IsRequired();
        builder.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.SourceKey).HasColumnName("source_key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.InfluenceType).HasColumnName("influence_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.InfluenceTag).HasColumnName("influence_tag").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").HasColumnType("numeric(10,4)");
        builder.Property(e => e.ValueMode).HasColumnName("value_mode").HasMaxLength(32);
        builder.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(64).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(e => e.ConsumedAtUtc).HasColumnName("consumed_at_utc");

        builder.HasOne(e => e.Run).WithMany().HasForeignKey(e => e.RunId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.RunId).HasDatabaseName("IX_run_adaptive_influences_run_id");
        builder.HasIndex(e => e.SourceType).HasDatabaseName("IX_run_adaptive_influences_source_type");
        builder.HasIndex(e => e.InfluenceTag).HasDatabaseName("IX_run_adaptive_influences_influence_tag");
    }
}
