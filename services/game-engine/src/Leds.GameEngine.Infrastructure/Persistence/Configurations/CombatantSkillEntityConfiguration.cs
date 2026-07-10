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

        builder.HasOne(s => s.Combatant)
            .WithMany(c => c.Skills)
            .HasForeignKey(s => s.CombatantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.CombatantId);
        builder.HasIndex(s => s.Key);
    }
}