using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ItemDefinitionEntityConfiguration : IEntityTypeConfiguration<ItemDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<ItemDefinitionEntity> builder)
    {
        builder.ToTable("catalog_item_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.NarrativeText).HasColumnName("narrative_text");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(64).IsRequired();
        builder.Property(e => e.FlavorTag).HasColumnName("flavor_tag").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Rarity).HasColumnName("rarity").HasMaxLength(64).IsRequired();
        builder.Property(e => e.UsageMode).HasColumnName("usage_mode").HasMaxLength(64).HasDefaultValue("NotUsable").IsRequired();
        builder.Property(e => e.Lifecycle).HasColumnName("lifecycle").HasMaxLength(64).HasDefaultValue("RuntimeRunOnly").IsRequired();
        builder.Property(e => e.StackPolicy).HasColumnName("stack_policy").HasMaxLength(32).HasDefaultValue("Additive").IsRequired();
        builder.Property(e => e.MaxStack).HasColumnName("max_stack").HasDefaultValue(1);
        builder.Property(e => e.IsUsableInCombat).HasColumnName("is_usable_in_combat");
        builder.Property(e => e.IsUsableOutsideCombat).HasColumnName("is_usable_outside_combat");
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.BaseWeight).HasColumnName("base_weight").HasDefaultValue(1);
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(64).IsRequired().HasComment("Legacy compatibility column. Use lifecycle/usage_mode for data-model-0.1 definitions.");
        builder.Property(e => e.EffectValue).HasColumnName("effect_value").HasComment("Sole source of truth for a used item's effect magnitude, alongside effect_run_type. See ItemDefinitionEntity.EffectRunType.");
        builder.Property(e => e.EffectRunType).HasColumnName("effect_run_type").HasMaxLength(32);
        builder.Property(e => e.TacticalRange).HasColumnName("tactical_range").HasDefaultValue(1);
        builder.Property(e => e.TacticalAreaShape).HasColumnName("tactical_area_shape").HasMaxLength(16).HasDefaultValue("Single").IsRequired();
        builder.Property(e => e.RequiresLineOfSight).HasColumnName("requires_line_of_sight").HasDefaultValue(false);
        builder.Property(e => e.BasicAttackPower).HasColumnName("basic_attack_power");
        builder.Property(e => e.BasicAttackCategory).HasColumnName("basic_attack_category").HasMaxLength(16);
        builder.Property(e => e.Price).HasColumnName("price");
        builder.Property(e => e.EquipmentEffectsJson).HasColumnName("equipment_effects_json").HasColumnType("jsonb");
        builder.Property(e => e.IsContainer).HasColumnName("is_container").HasDefaultValue(false);
        builder.Property(e => e.ContainerCapacity).HasColumnName("container_capacity");
        builder.Property(e => e.IsLiquid).HasColumnName("is_liquid").HasDefaultValue(false);
        builder.Property(e => e.ReadablePagesJson).HasColumnName("readable_pages_json").HasColumnType("jsonb");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
