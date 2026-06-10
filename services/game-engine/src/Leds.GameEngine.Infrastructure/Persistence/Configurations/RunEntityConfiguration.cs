using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunEntityConfiguration : IEntityTypeConfiguration<RunEntity>
{
    public void Configure(EntityTypeBuilder<RunEntity> builder)
    {
        builder.ToTable("runs");

        builder.HasKey(run => run.Id);

        builder.Property(run => run.Id).HasColumnName("id");
        builder.Property(run => run.PlayerId).HasColumnName("player_id");
        builder.Property(run => run.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
        builder.Property(run => run.Seed).HasColumnName("seed").HasMaxLength(128).IsRequired();
        builder.Property(run => run.GeneratorVersion).HasColumnName("generator_version").HasMaxLength(64).IsRequired();
        builder.Property(run => run.MarkovMatrixVersion).HasColumnName("markov_matrix_version").HasMaxLength(64).IsRequired();
        builder.Property(run => run.CurrentDepth).HasColumnName("current_depth");
        builder.Property(run => run.ActiveCombatId).HasColumnName("active_combat_id");
        builder.Property(run => run.PendingRewardOfferId).HasColumnName("pending_reward_offer_id");
        builder.Property(run => run.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(run => run.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(run => run.PlayerId);
        builder.HasIndex(run => run.Status);
        builder.HasIndex(run => run.CreatedAtUtc);
    }
}
