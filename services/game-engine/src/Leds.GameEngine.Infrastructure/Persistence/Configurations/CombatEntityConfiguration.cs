using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class CombatEntityConfiguration : IEntityTypeConfiguration<CombatEntity>
{
    public void Configure(EntityTypeBuilder<CombatEntity> builder)
    {
        builder.ToTable("run_active_combats");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.RunId).HasColumnName("run_id");
        builder.Property(c => c.RoomId).HasColumnName("room_id");
        builder.Property(c => c.NodeId).HasColumnName("node_id");
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
        builder.Property(c => c.TurnNumber).HasColumnName("turn_number");
        builder.Property(c => c.CurrentTick).HasColumnName("current_tick");
        builder.Property(c => c.HitCounter).HasColumnName("hit_counter");
        builder.Property(c => c.HitCounterDoubleDamageEnabled).HasColumnName("hit_counter_double_damage_enabled");
        builder.Property(c => c.ActiveCombatantId).HasColumnName("active_combatant_id");
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(c => c.RunId);
        builder.HasIndex(c => c.Status);
    }
}