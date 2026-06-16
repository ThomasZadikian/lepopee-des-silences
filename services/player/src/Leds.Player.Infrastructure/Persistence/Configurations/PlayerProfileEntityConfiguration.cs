using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Player.Infrastructure.Persistence.Configurations;

public sealed class PlayerProfileEntityConfiguration : IEntityTypeConfiguration<PlayerProfileEntity>
{
    public void Configure(EntityTypeBuilder<PlayerProfileEntity> builder)
    {
        builder.ToTable("player_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.AuthSubjectId).HasColumnName("auth_subject_id").HasMaxLength(160);
        builder.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
        builder.Property(p => p.TotalRunsStarted).HasColumnName("total_runs_started").HasDefaultValue(0);
        builder.Property(p => p.TotalRunsCompleted).HasColumnName("total_runs_completed").HasDefaultValue(0);
        builder.Property(p => p.TotalRunsFailed).HasColumnName("total_runs_failed").HasDefaultValue(0);
        builder.Property(p => p.TotalRunsAbandoned).HasColumnName("total_runs_abandoned").HasDefaultValue(0);
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(p => p.AuthSubjectId);
    }
}
