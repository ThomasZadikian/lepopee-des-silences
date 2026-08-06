using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class PlayerRuntimeSkillEntityConfiguration : IEntityTypeConfiguration<PlayerRuntimeSkillEntity>
{
    public void Configure(EntityTypeBuilder<PlayerRuntimeSkillEntity> builder)
    {
        builder.ToTable("run_player_skills");

        builder.HasKey(s => new { s.RunId, s.Key });

        builder.Property(s => s.RunId).HasColumnName("run_id");
        builder.Property(s => s.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        builder.Property(s => s.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(s => s.SkillType).HasColumnName("skill_type").HasMaxLength(64).IsRequired();
        builder.Property(s => s.TargetingType).HasColumnName("targeting_type").HasMaxLength(64).IsRequired();
        builder.Property(s => s.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired();
        builder.Property(s => s.ManaCost).HasColumnName("mana_cost");
        builder.Property(s => s.ChargeCost).HasColumnName("charge_cost");
        builder.Property(s => s.BasePower).HasColumnName("base_power");
        builder.Property(s => s.Category).HasColumnName("category").HasMaxLength(16).HasDefaultValue("Physical").IsRequired();
        builder.Property(s => s.BasePowerIsPercentOfMaxVitality).HasColumnName("base_power_is_percent_of_max_vitality").HasDefaultValue(false).IsRequired();
        builder.Property(s => s.TacticalRange).HasColumnName("tactical_range").HasDefaultValue(1).IsRequired();
        builder.Property(s => s.TacticalAreaShape).HasColumnName("tactical_area_shape").HasMaxLength(16).HasDefaultValue("Single").IsRequired();
        builder.Property(s => s.RequiresLineOfSight).HasColumnName("requires_line_of_sight").HasDefaultValue(false).IsRequired();
        builder.Property(s => s.Cooldown).HasColumnName("cooldown").HasDefaultValue(0).IsRequired();
        builder.Property(s => s.IsUltimate).HasColumnName("is_ultimate").HasDefaultValue(false).IsRequired();
        builder.Property(s => s.EmotionalRegister).HasColumnName("emotional_register").HasMaxLength(32).IsRequired();

        builder.HasOne(s => s.PlayerState)
            .WithMany(ps => ps.Skills)
            .HasForeignKey(s => s.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
