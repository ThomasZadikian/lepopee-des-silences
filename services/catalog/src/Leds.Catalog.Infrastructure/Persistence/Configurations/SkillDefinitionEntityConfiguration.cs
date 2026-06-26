using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class SkillDefinitionEntityConfiguration : IEntityTypeConfiguration<SkillDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<SkillDefinitionEntity> builder)
    {
        builder.ToTable("catalog_skill_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.NarrativeText).HasColumnName("narrative_text");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.SkillType).HasColumnName("skill_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.TargetingType).HasColumnName("targeting_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.TargetingMode).HasColumnName("targeting_mode").HasMaxLength(64).IsRequired();
        builder.Property(e => e.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired().HasComment("Legacy compatibility summary. Canonical effects live in catalog_effect_sets/catalog_effect_definitions.");
        builder.Property(e => e.CostType).HasColumnName("cost_type").HasMaxLength(32).HasDefaultValue("None").IsRequired();
        builder.Property(e => e.CostAmount).HasColumnName("cost_amount");
        builder.Property(e => e.ManaCost).HasColumnName("mana_cost");
        builder.Property(e => e.ChargeCost).HasColumnName("charge_cost");
        builder.Property(e => e.BasePower).HasColumnName("base_power");
        builder.Property(e => e.Power).HasColumnName("power");
        builder.Property(e => e.Accuracy).HasColumnName("accuracy").HasDefaultValue(100);
        builder.Property(e => e.ActionCost).HasColumnName("action_cost").HasDefaultValue(10);
        builder.Property(e => e.CastTime).HasColumnName("cast_time");
        builder.Property(e => e.RecoveryTime).HasColumnName("recovery_time");
        builder.Property(e => e.Cooldown).HasColumnName("cooldown");
        builder.Property(e => e.EffectSetId).HasColumnName("effect_set_id");
        builder.Property(e => e.BaseWeight).HasColumnName("base_weight").HasDefaultValue(1);
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.EffectKind).HasColumnName("effect_kind").HasMaxLength(32);
        builder.Property(e => e.EffectStatusKey).HasColumnName("effect_status_key").HasMaxLength(64);
        builder.Property(e => e.EffectMagnitude).HasColumnName("effect_magnitude").HasDefaultValue(0);
        builder.Property(e => e.EffectDurationTicks).HasColumnName("effect_duration_ticks").HasDefaultValue(0);
        builder.Property(e => e.EffectTickInterval).HasColumnName("effect_tick_interval").HasDefaultValue(0);
        builder.Property(e => e.EffectStat).HasColumnName("effect_stat").HasMaxLength(32);
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasOne(e => e.EffectSet).WithMany().HasForeignKey(e => e.EffectSetId).OnDelete(DeleteBehavior.SetNull);
    }
}
