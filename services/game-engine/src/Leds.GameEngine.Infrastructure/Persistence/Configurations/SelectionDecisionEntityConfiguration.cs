using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class SelectionDecisionEntityConfiguration : IEntityTypeConfiguration<Entities.SelectionDecisionEntity>
{
    public void Configure(EntityTypeBuilder<Entities.SelectionDecisionEntity> builder)
    {
        builder.ToTable("run_selection_decisions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.RunId)
            .HasColumnName("run_id")
            .IsRequired();

        builder.Property(e => e.DecisionType)
            .HasColumnName("decision_type")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.ContextKey)
            .HasColumnName("context_key")
            .HasMaxLength(160);

        builder.Property(e => e.SelectedKey)
            .HasColumnName("selected_key")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(e => e.SelectionGroup)
            .HasColumnName("selection_group")
            .HasMaxLength(64);

        builder.Property(e => e.Seed)
            .HasColumnName("seed")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.AlgorithmVersion)
            .HasColumnName("algorithm_version")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(e => e.MatrixVersion)
            .HasColumnName("matrix_version")
            .HasMaxLength(64);

        builder.Property(e => e.InfluenceSummaryKey)
            .HasColumnName("influence_summary_key")
            .HasMaxLength(160);

        builder.HasOne(e => e.Run)
            .WithMany()
            .HasForeignKey(e => e.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.RunId)
            .HasDatabaseName("IX_run_selection_decisions_run_id");

        builder.HasIndex(e => e.DecisionType)
            .HasDatabaseName("IX_run_selection_decisions_decision_type");

        builder.HasIndex(e => e.SelectedKey)
            .HasDatabaseName("IX_run_selection_decisions_selected_key");
    }
}
