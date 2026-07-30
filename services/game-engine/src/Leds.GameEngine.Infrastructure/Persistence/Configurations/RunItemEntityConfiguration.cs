using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunItemEntityConfiguration : IEntityTypeConfiguration<RunItemEntity>
{
    public void Configure(EntityTypeBuilder<RunItemEntity> builder)
    {
        builder.ToTable("run_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.RunId).HasColumnName("run_id");
        builder.Property(item => item.DefinitionKey).HasColumnName("definition_key").HasMaxLength(256).IsRequired();
        builder.Property(item => item.DefinitionVersion).HasColumnName("definition_version").HasMaxLength(32);
        builder.Property(item => item.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(1024);
        builder.Property(item => item.NarrativeText).HasColumnName("narrative_text");
        builder.Property(item => item.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(item => item.Rarity).HasColumnName("rarity").HasMaxLength(64).IsRequired();
        builder.Property(item => item.Category).HasColumnName("category").HasMaxLength(64);
        builder.Property(item => item.Quantity).HasColumnName("quantity");
        builder.Property(item => item.MaxStack).HasColumnName("max_stack");
        builder.Property(item => item.UsageMode).HasColumnName("usage_mode").HasMaxLength(64);
        builder.Property(item => item.Lifecycle).HasColumnName("lifecycle").HasMaxLength(64);
        builder.Property(item => item.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired();
        builder.Property(item => item.EffectAmount).HasColumnName("effect_amount");
        builder.Property(item => item.EffectSetKey).HasColumnName("effect_set_key").HasMaxLength(160);
        builder.Property(item => item.EffectSummary).HasColumnName("effect_summary");
        builder.Property(item => item.IsUsableInCombat).HasColumnName("is_usable_in_combat");
        builder.Property(item => item.IsUsableOutsideCombat).HasColumnName("is_usable_outside_combat");
        builder.Property(item => item.SourceRewardOptionId).HasColumnName("source_reward_option_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(item => item.IsContainer).HasColumnName("is_container").HasDefaultValue(false);
        builder.Property(item => item.ContainerCapacity).HasColumnName("container_capacity");
        builder.Property(item => item.IsLiquid).HasColumnName("is_liquid").HasDefaultValue(false);
        builder.Property(item => item.ContainedLiquidDefinitionKey).HasColumnName("contained_liquid_definition_key").HasMaxLength(256);
        builder.Property(item => item.TacticalRange).HasColumnName("tactical_range").HasDefaultValue(1);
        builder.Property(item => item.TacticalAreaShape).HasColumnName("tactical_area_shape").HasMaxLength(16).HasDefaultValue("Single").IsRequired();
        builder.Property(item => item.RequiresLineOfSight).HasColumnName("requires_line_of_sight").HasDefaultValue(false);

        builder.HasOne(item => item.Run)
            .WithMany(run => run.InventoryItems)
            .HasForeignKey(item => item.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => item.RunId);
        builder.HasIndex(item => item.DefinitionKey);
        builder.HasIndex(item => item.SourceRewardOptionId);
    }
}
