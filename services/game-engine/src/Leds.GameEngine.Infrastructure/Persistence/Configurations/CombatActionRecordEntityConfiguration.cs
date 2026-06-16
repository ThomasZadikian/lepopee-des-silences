using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class CombatActionRecordEntityConfiguration : IEntityTypeConfiguration<CombatActionRecordEntity>
{
    public void Configure(EntityTypeBuilder<CombatActionRecordEntity> builder)
    {
        builder.ToTable("run_combat_actions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CombatId)
            .HasColumnName("combat_id")
            .IsRequired();

        builder.Property(x => x.TurnNumber)
            .HasColumnName("turn_number")
            .IsRequired();

        builder.Property(x => x.ActorId)
            .HasColumnName("actor_id")
            .IsRequired();

        builder.Property(x => x.ActorSide)
            .HasColumnName("actor_side")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.SkillKey)
            .HasColumnName("skill_key")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.SkillDisplayName)
            .HasColumnName("skill_display_name")
            .HasMaxLength(256);

        builder.Property(x => x.TargetIds)
            .HasColumnName("target_ids")
            .HasColumnType("text")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(x => x.SourceType)
            .HasColumnName("source_type")
            .HasMaxLength(64);

        builder.Property(x => x.SourceKey)
            .HasColumnName("source_key")
            .HasMaxLength(160);

        builder.Property(x => x.RawDamage)
            .HasColumnName("raw_damage")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.MitigatedDamage)
            .HasColumnName("mitigated_damage")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.VitalityDamage)
            .HasColumnName("vitality_damage")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.GuardDamage)
            .HasColumnName("guard_damage")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.GuardAbsorbed)
            .HasColumnName("guard_absorbed")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.GuardGained)
            .HasColumnName("guard_gained")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.DamageDealt)
            .HasColumnName("damage_dealt")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.DamageTaken)
            .HasColumnName("damage_taken")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.HealingDone)
            .HasColumnName("healing_done")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.HealingReceived)
            .HasColumnName("healing_received")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.EffectsApplied)
            .HasColumnName("effects_applied")
            .HasColumnType("text")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.HasOne(x => x.Combat)
            .WithMany()
            .HasForeignKey(x => x.CombatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CombatId);
        builder.HasIndex(x => x.TurnNumber);
        builder.HasIndex(x => x.ActorId);
        builder.HasIndex(x => x.OccurredAtUtc);
    }
}
