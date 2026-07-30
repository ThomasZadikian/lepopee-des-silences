using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunCharacterStatSnapshotEntityConfiguration : IEntityTypeConfiguration<RunCharacterStatSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<RunCharacterStatSnapshotEntity> builder)
    {
        builder.ToTable("run_character_stat_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CharacterSnapshotId).HasColumnName("character_snapshot_id");
        builder.Property(e => e.MaxVitality).HasColumnName("max_vitality").IsRequired();
        builder.Property(e => e.AttackPower).HasColumnName("attack_power").IsRequired();
        builder.Property(e => e.Defense).HasColumnName("defense").IsRequired();
        builder.Property(e => e.StartingGuard).HasColumnName("starting_guard").IsRequired();
        builder.Property(e => e.Speed).HasColumnName("speed").IsRequired();
        builder.Property(e => e.Initiative).HasColumnName("initiative").IsRequired();
        builder.Property(e => e.Focus).HasColumnName("focus").IsRequired();
        builder.Property(e => e.Mana).HasColumnName("mana").IsRequired();
        builder.Property(e => e.Charge).HasColumnName("charge").IsRequired();
        builder.Property(e => e.MagicAttack).HasColumnName("magic_attack").HasDefaultValue(0);
        builder.Property(e => e.MagicDefense).HasColumnName("magic_defense").HasDefaultValue(0);

        builder.HasIndex(e => e.CharacterSnapshotId).IsUnique();
    }
}
