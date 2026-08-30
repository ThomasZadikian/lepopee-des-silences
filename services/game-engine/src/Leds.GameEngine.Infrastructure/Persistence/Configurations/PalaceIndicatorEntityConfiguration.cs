using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class PalaceIndicatorEntityConfiguration : IEntityTypeConfiguration<Entities.PalaceIndicatorEntity>
{
    public void Configure(EntityTypeBuilder<Entities.PalaceIndicatorEntity> builder)
    {
        builder.ToTable("run_palace_indicator_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.RunId).HasColumnName("run_id").IsRequired();
        builder.Property(e => e.IndicatorKey).HasColumnName("indicator_key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayLabel).HasColumnName("display_label").HasMaxLength(256).IsRequired();
        builder.Property(e => e.NarrativeText).HasColumnName("narrative_text").IsRequired();
        builder.Property(e => e.Intensity).HasColumnName("intensity").HasMaxLength(64).IsRequired();
        builder.Property(e => e.SourceDecisionId).HasColumnName("source_decision_id");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");

        builder.HasOne(e => e.Run).WithMany().HasForeignKey(e => e.RunId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.RunId).HasDatabaseName("IX_run_palace_indicator_snapshots_run_id");
        builder.HasIndex(e => e.IndicatorKey).HasDatabaseName("IX_run_palace_indicator_snapshots_indicator_key");
    }
}
