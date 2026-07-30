using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class CombatantRuntimeStateEntityConfiguration : IEntityTypeConfiguration<CombatantRuntimeStateEntity>
{
    public void Configure(EntityTypeBuilder<CombatantRuntimeStateEntity> builder)
    {
        builder.ToTable("run_combatant_runtime_states");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CombatantId).HasColumnName("combatant_id");
        builder.Property(e => e.CurrentVitality).HasColumnName("current_vitality").IsRequired();
        builder.Property(e => e.CurrentGuard).HasColumnName("current_guard").IsRequired();
        builder.Property(e => e.CurrentFocus).HasColumnName("current_focus").IsRequired();
        builder.Property(e => e.CurrentMana).HasColumnName("current_mana").IsRequired();
        builder.Property(e => e.MaxMana).HasColumnName("max_mana").HasDefaultValue(int.MaxValue).IsRequired();
        builder.Property(e => e.CurrentCharge).HasColumnName("current_charge").IsRequired();
        // Legacy CLR members remain temporarily readable by the old aggregate mapper,
        // but are no longer part of the canonical tactical persistence model.
        builder.Ignore(e => e.AtbGaugeValue);
        builder.Ignore(e => e.ActionRecoveryUntilTick);
        builder.Ignore(e => e.AtbFillPerTick);
        builder.Ignore(e => e.AtbTempoRoomFactorPerMille);
        builder.Ignore(e => e.AtbTempoCombatantFactorPerMille);
        builder.Property(e => e.TempoMomentumPerMille).HasColumnName("tempo_momentum_per_mille").HasDefaultValue(0).IsRequired();
        builder.Property(e => e.ThreatValue).HasColumnName("threat_value").HasDefaultValue(0d).IsRequired();
        builder.Property(e => e.LastAttackerId).HasColumnName("last_attacker_id");
        builder.Property(e => e.TookPowerfulHitSinceLastAction)
            .HasColumnName("took_powerful_hit_since_last_action").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.CombatantId).IsUnique();

        builder.HasOne(e => e.Combatant)
            .WithOne(e => e.RuntimeState)
            .HasForeignKey<CombatantRuntimeStateEntity>(e => e.CombatantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
