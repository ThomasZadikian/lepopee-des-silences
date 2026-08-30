using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class CombatantSkillEntityConfiguration : IEntityTypeConfiguration<CombatantSkillEntity>
{
    public void Configure(EntityTypeBuilder<CombatantSkillEntity> builder)
    {
        builder.ToTable("run_combatant_skills");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.CombatantId).HasColumnName("combatant_id");
        builder.Property(s => s.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        builder.Property(s => s.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(s => s.SkillType).HasColumnName("skill_type").HasMaxLength(64).IsRequired();
        builder.Property(s => s.TargetingType).HasColumnName("targeting_type").HasMaxLength(64).IsRequired();
        builder.Property(s => s.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired();
        builder.Property(s => s.ManaCost).HasColumnName("mana_cost");
        builder.Property(s => s.ChargeCost).HasColumnName("charge_cost");
        builder.Property(s => s.BasePower).HasColumnName("base_power");
        builder.Property(s => s.Tags).HasColumnName("tags");
        builder.Property(s => s.Category).HasColumnName("category").HasMaxLength(16).HasDefaultValue("Physical").IsRequired();
        builder.Property(s => s.BasePowerIsPercentOfMaxVitality).HasColumnName("base_power_is_percent_of_max_vitality").HasDefaultValue(false).IsRequired();
        builder.Property(s => s.StatusEffectsJson).HasColumnName("status_effects_json");
        builder.Property(s => s.TacticalRange).HasColumnName("tactical_range").HasDefaultValue(1);
        builder.Property(s => s.TacticalAreaShape).HasColumnName("tactical_area_shape").HasMaxLength(16).HasDefaultValue("Single").IsRequired();
        builder.Property(s => s.RequiresLineOfSight).HasColumnName("requires_line_of_sight").HasDefaultValue(false);
        builder.Property(s => s.Cooldown).HasColumnName("cooldown").HasDefaultValue(0);
        builder.Property(s => s.IsUltimate).HasColumnName("is_ultimate").HasDefaultValue(false);
        builder.Property(s => s.EmotionalRegister).HasColumnName("emotional_register").HasMaxLength(32).IsRequired();

        builder.HasOne(s => s.Combatant)
            .WithMany(c => c.Skills)
            .HasForeignKey(s => s.CombatantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.CombatantId);
        builder.HasIndex(s => s.Key);
    }
}
