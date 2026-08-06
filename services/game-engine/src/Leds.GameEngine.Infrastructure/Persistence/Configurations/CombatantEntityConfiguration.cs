using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class CombatantEntityConfiguration : IEntityTypeConfiguration<CombatantEntity>
{
    public void Configure(EntityTypeBuilder<CombatantEntity> builder)
    {
        builder.ToTable("run_combatants");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.CombatId).HasColumnName("combat_id");
        builder.Property(c => c.SourceKey).HasColumnName("source_key").HasMaxLength(128).IsRequired();
        builder.Property(c => c.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(c => c.Side).HasColumnName("side").HasMaxLength(32).IsRequired();
        builder.Property(c => c.Archetype).HasColumnName("archetype").HasMaxLength(128).IsRequired();
        builder.Property(c => c.NaturalEmotionalRegister).HasColumnName("natural_emotional_register").HasMaxLength(32).HasDefaultValue("Neutral").IsRequired();
        builder.Property(c => c.MaxVitality).HasColumnName("max_vitality");
        builder.Property(c => c.CurrentVitality).HasColumnName("current_vitality");
        builder.Property(c => c.Guard).HasColumnName("guard");
        builder.Property(c => c.BaseGuard).HasColumnName("base_guard");
        builder.Property(c => c.Mana).HasColumnName("mana");
        builder.Property(c => c.MaxMana).HasColumnName("max_mana").HasDefaultValue(int.MaxValue);
        builder.Property(c => c.Charge).HasColumnName("charge").HasPrecision(4, 1);
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(c => c.HasActedThisCombat).HasColumnName("has_acted_this_combat").HasDefaultValue(false);
        builder.Property(c => c.AttackTypeOverride).HasColumnName("attack_type_override");
        builder.Property(c => c.TypedDamageReductionsJson).HasColumnName("typed_damage_reductions_json");
        builder.Property(c => c.HitChanceBonusPercent).HasColumnName("hit_chance_bonus_percent").HasDefaultValue(0);
        builder.Property(c => c.DotDurationReductionPercent).HasColumnName("dot_duration_reduction_percent").HasDefaultValue(0);
        builder.Property(c => c.DotDamageReductionPercent).HasColumnName("dot_damage_reduction_percent").HasDefaultValue(0);
        builder.Property(c => c.DotDamageBonusPercent).HasColumnName("dot_damage_bonus_percent").HasDefaultValue(0);
        builder.Property(c => c.MagicDamageBonusPercent).HasColumnName("magic_damage_bonus_percent").HasDefaultValue(0);
        builder.Property(c => c.MagicDamageReductionPercent).HasColumnName("magic_damage_reduction_percent").HasDefaultValue(0);
        builder.Property(c => c.CriticalChanceBonusPercent).HasColumnName("critical_chance_bonus_percent").HasDefaultValue(0);
        builder.Property(c => c.HealingBonusPercent).HasColumnName("healing_bonus_percent").HasDefaultValue(0);
        builder.Property(c => c.StatusEffectsJson).HasColumnName("status_effects_json");

        builder.HasOne(c => c.Combat)
            .WithMany(combat => combat.Combatants)
            .HasForeignKey(c => c.CombatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.CombatId);
        builder.HasIndex(c => c.Side);
        builder.HasIndex(c => c.Status);
    }
}
