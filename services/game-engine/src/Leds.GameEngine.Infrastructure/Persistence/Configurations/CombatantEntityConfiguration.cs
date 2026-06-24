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
        builder.Property(c => c.MaxVitality).HasColumnName("max_vitality");
        builder.Property(c => c.CurrentVitality).HasColumnName("current_vitality");
        builder.Property(c => c.Guard).HasColumnName("guard");
        builder.Property(c => c.BaseGuard).HasColumnName("base_guard");
        builder.Property(c => c.Mana).HasColumnName("mana");
        builder.Property(c => c.Charge).HasColumnName("charge");
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(c => c.AttackTypeOverride).HasColumnName("attack_type_override");

        builder.HasOne(c => c.Combat)
            .WithMany(combat => combat.Combatants)
            .HasForeignKey(c => c.CombatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.CombatId);
        builder.HasIndex(c => c.Side);
        builder.HasIndex(c => c.Status);
    }
}