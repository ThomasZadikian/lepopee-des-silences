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
        builder.Property(e => e.CurrentCharge).HasColumnName("current_charge").IsRequired();
        builder.Property(e => e.AtbGaugeValue).HasColumnName("atb_gauge_value");
        builder.Property(e => e.ActionRecoveryUntilTick).HasColumnName("action_recovery_until_tick");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.CombatantId).IsUnique();

        builder.HasOne(e => e.Combatant)
            .WithOne(e => e.RuntimeState)
            .HasForeignKey<CombatantRuntimeStateEntity>(e => e.CombatantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
