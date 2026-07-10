using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunEntityConfiguration : IEntityTypeConfiguration<RunEntity>
{
    public void Configure(EntityTypeBuilder<RunEntity> builder)
    {
        builder.ToTable("runs");

        builder.HasKey(run => run.Id);

        builder.Property(run => run.Id).HasColumnName("id");
        builder.Property(run => run.PlayerId).HasColumnName("player_id");
        builder.Property(run => run.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
        builder.Property(run => run.Seed).HasColumnName("seed").HasMaxLength(128).IsRequired();
        builder.Property(run => run.GeneratorVersion).HasColumnName("generator_version").HasMaxLength(64).IsRequired();
        builder.Property(run => run.MarkovMatrixVersion).HasColumnName("markov_matrix_version").HasMaxLength(64).IsRequired();
        builder.Property(run => run.CurrentRoomId).HasColumnName("current_room_id");
        builder.Property(run => run.CurrentRoomIndex).HasColumnName("current_room_index");
        builder.Property(run => run.ActiveCombatId).HasColumnName("active_combat_id");
        builder.Property(run => run.PendingRewardOfferId).HasColumnName("pending_reward_offer_id");
        builder.Property(run => run.MaxHp).HasColumnName("max_hp");
        builder.Property(run => run.CurrentHp).HasColumnName("current_hp");
        builder.Property(run => run.Attack).HasColumnName("attack");
        builder.Property(run => run.Defense).HasColumnName("defense");
        builder.Property(run => run.Speed).HasColumnName("speed");
        builder.Property(run => run.Focus).HasColumnName("focus");
        builder.Property(run => run.RunItemCapacity).HasColumnName("run_item_capacity").HasDefaultValue(Run.DefaultRunItemCapacity);
        builder.Property(run => run.TypedDamageReductionsJson).HasColumnName("typed_damage_reductions_json");
        builder.Property(run => run.HitChanceBonusPercent).HasColumnName("hit_chance_bonus_percent").HasDefaultValue(0);
        builder.Property(run => run.DotDurationReductionPercent).HasColumnName("dot_duration_reduction_percent").HasDefaultValue(0);
        builder.Property(run => run.DotDamageReductionPercent).HasColumnName("dot_damage_reduction_percent").HasDefaultValue(0);
        builder.Property(run => run.MagicDamageBonusPercent).HasColumnName("magic_damage_bonus_percent").HasDefaultValue(0);
        builder.Property(run => run.MagicDamageReductionPercent).HasColumnName("magic_damage_reduction_percent").HasDefaultValue(0);
        builder.Property(run => run.CriticalChanceBonusPercent).HasColumnName("critical_chance_bonus_percent").HasDefaultValue(0);
        builder.Property(run => run.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(run => run.EndedAtUtc).HasColumnName("ended_at_utc");
        builder.Property(run => run.SavedAtUtc).HasColumnName("saved_at_utc");
        builder.Property(run => run.PreSuspendStatus).HasColumnName("pre_suspend_status").HasMaxLength(64);
        builder.Property(run => run.SnapshotCurrentHp).HasColumnName("snapshot_current_hp");
        builder.Property(run => run.SnapshotAttack).HasColumnName("snapshot_attack");
        builder.Property(run => run.SnapshotDefense).HasColumnName("snapshot_defense");
        builder.Property(run => run.SnapshotSpeed).HasColumnName("snapshot_speed");
        builder.Property(run => run.SnapshotMemoryFragments).HasColumnName("snapshot_memory_fragments");
        builder.Property(run => run.SnapshotActivePalaceLaws).HasColumnName("snapshot_active_palace_laws");
        builder.Property(run => run.SnapshotRunItemIds).HasColumnName("snapshot_run_item_ids");
        builder.Property(run => run.SnapshotRunModifierIds).HasColumnName("snapshot_run_modifier_ids");
        builder.Property(run => run.ActiveNpcKey).HasColumnName("active_npc_key").HasMaxLength(160);
        builder.Property(run => run.NpcRelationshipsJson).HasColumnName("npc_relationships_json");
        builder.Property(run => run.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(run => run.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(run => run.PlayerId);
        builder.HasIndex(run => run.Status);
        builder.HasIndex(run => run.CreatedAtUtc);

        builder.HasOne(run => run.ActiveCombat)
            .WithOne(c => c.Run)
            .HasForeignKey<RunEntity>(run => run.ActiveCombatId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}