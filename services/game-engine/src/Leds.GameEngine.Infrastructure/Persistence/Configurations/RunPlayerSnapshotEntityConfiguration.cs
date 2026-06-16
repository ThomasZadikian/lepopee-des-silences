using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunPlayerSnapshotEntityConfiguration : IEntityTypeConfiguration<RunPlayerSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<RunPlayerSnapshotEntity> builder)
    {
        builder.ToTable("run_player_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.RunId).HasColumnName("run_id");
        builder.Property(e => e.PlayerId).HasColumnName("player_id");
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.HasIndex(e => e.RunId).IsUnique();

        builder.HasOne(e => e.Run)
            .WithOne(e => e.PlayerSnapshot)
            .HasForeignKey<RunPlayerSnapshotEntity>(e => e.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Characters)
            .WithOne(e => e.PlayerSnapshot)
            .HasForeignKey(e => e.PlayerSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
