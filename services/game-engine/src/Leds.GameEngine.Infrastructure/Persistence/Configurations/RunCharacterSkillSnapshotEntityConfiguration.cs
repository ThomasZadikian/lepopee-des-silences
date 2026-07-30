using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunCharacterSkillSnapshotEntityConfiguration : IEntityTypeConfiguration<RunCharacterSkillSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<RunCharacterSkillSnapshotEntity> builder)
    {
        builder.ToTable("run_character_skill_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CharacterSnapshotId).HasColumnName("character_snapshot_id");
        builder.Property(e => e.SkillDefinitionKey).HasColumnName("skill_definition_key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.SkillType).HasColumnName("skill_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.TargetingMode).HasColumnName("targeting_mode").HasMaxLength(64).IsRequired();
        builder.Property(e => e.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ManaCost).HasColumnName("mana_cost").HasDefaultValue(0);
        builder.Property(e => e.ChargeCost).HasColumnName("charge_cost").HasDefaultValue(0);
        builder.Property(e => e.BasePower).HasColumnName("base_power").HasDefaultValue(0);
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(16).HasDefaultValue("Physical").IsRequired();
        builder.Property(e => e.BasePowerIsPercentOfMaxVitality).HasColumnName("base_power_is_percent_of_max_vitality").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.TacticalRange).HasColumnName("tactical_range").HasDefaultValue(1).IsRequired();
        builder.Property(e => e.TacticalAreaShape).HasColumnName("tactical_area_shape").HasMaxLength(16).HasDefaultValue("Single").IsRequired();
        builder.Property(e => e.RequiresLineOfSight).HasColumnName("requires_line_of_sight").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.Cooldown).HasColumnName("cooldown").HasDefaultValue(0).IsRequired();
        builder.Property(e => e.IsUltimate).HasColumnName("is_ultimate").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.EmotionalRegister).HasColumnName("emotional_register").HasMaxLength(32).HasDefaultValue("Neutral").IsRequired();

        builder.HasIndex(e => e.CharacterSnapshotId);
        builder.HasIndex(e => e.SkillDefinitionKey);
    }
}
