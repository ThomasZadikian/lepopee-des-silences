using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Player.Infrastructure.Persistence.Configurations;

public sealed class PlayerCharacterStatBlockEntityConfiguration : IEntityTypeConfiguration<PlayerCharacterStatBlockEntity>
{
    public void Configure(EntityTypeBuilder<PlayerCharacterStatBlockEntity> builder)
    {
        builder.ToTable("player_character_stat_blocks");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.PlayerCharacterId).HasColumnName("player_character_id");
        builder.Property(s => s.MaxVitality).HasColumnName("max_vitality").HasDefaultValue(100);
        builder.Property(s => s.AttackPower).HasColumnName("attack_power").HasDefaultValue(12);
        builder.Property(s => s.Defense).HasColumnName("defense").HasDefaultValue(6);
        builder.Property(s => s.StartingGuard).HasColumnName("starting_guard").HasDefaultValue(0);
        builder.Property(s => s.Speed).HasColumnName("speed").HasDefaultValue(10);
        builder.Property(s => s.Initiative).HasColumnName("initiative").HasDefaultValue(10);
        builder.Property(s => s.Focus).HasColumnName("focus").HasDefaultValue(0);
        builder.Property(s => s.Mana).HasColumnName("mana").HasDefaultValue(0);
        builder.Property(s => s.Charge).HasColumnName("charge").HasDefaultValue(0);
        builder.Property(s => s.MagicAttack).HasColumnName("magic_attack").HasDefaultValue(0);
        builder.Property(s => s.MagicDefense).HasColumnName("magic_defense").HasDefaultValue(0);
        builder.Property(s => s.Movement).HasColumnName("movement").HasDefaultValue(4).IsRequired();
        builder.HasIndex(s => s.PlayerCharacterId).IsUnique();
        builder.HasOne(s => s.PlayerCharacter)
            .WithOne(c => c.StatBlock)
            .HasForeignKey<PlayerCharacterStatBlockEntity>(s => s.PlayerCharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlayerCharacterSkillEntityConfiguration : IEntityTypeConfiguration<PlayerCharacterSkillEntity>
{
    public void Configure(EntityTypeBuilder<PlayerCharacterSkillEntity> builder)
    {
        builder.ToTable("player_character_skills");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.PlayerCharacterId).HasColumnName("player_character_id");
        builder.Property(s => s.SkillDefinitionKey).HasColumnName("skill_definition_key").HasMaxLength(160).IsRequired();
        builder.Property(s => s.UnlockedAtUtc).HasColumnName("unlocked_at_utc");
        builder.Property(s => s.Source).HasColumnName("source").HasMaxLength(64);
        builder.Property(s => s.IsEquipped).HasColumnName("is_equipped").HasDefaultValue(false);
        builder.HasIndex(s => new { s.PlayerCharacterId, s.SkillDefinitionKey }).IsUnique();
        builder.HasOne(s => s.PlayerCharacter)
            .WithMany(c => c.Skills)
            .HasForeignKey(s => s.PlayerCharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlayerPermanentUnlockEntityConfiguration : IEntityTypeConfiguration<PlayerPermanentUnlockEntity>
{
    public void Configure(EntityTypeBuilder<PlayerPermanentUnlockEntity> builder)
    {
        builder.ToTable("player_permanent_unlocks");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.PlayerProfileId).HasColumnName("player_profile_id");
        builder.Property(u => u.UnlockKey).HasColumnName("unlock_key").HasMaxLength(160).IsRequired();
        builder.Property(u => u.UnlockType).HasColumnName("unlock_type").HasMaxLength(64).IsRequired();
        builder.Property(u => u.SourceRunId).HasColumnName("source_run_id");
        builder.Property(u => u.UnlockedAtUtc).HasColumnName("unlocked_at_utc");
        builder.HasIndex(u => new { u.PlayerProfileId, u.UnlockKey }).IsUnique();
        builder.HasOne(u => u.PlayerProfile)
            .WithMany(p => p.PermanentUnlocks)
            .HasForeignKey(u => u.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlayerCharacterItemEntityConfiguration : IEntityTypeConfiguration<PlayerCharacterItemEntity>
{
    public void Configure(EntityTypeBuilder<PlayerCharacterItemEntity> builder)
    {
        builder.ToTable("player_character_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.PlayerCharacterId).HasColumnName("player_character_id");
        builder.Property(i => i.ItemDefinitionKey).HasColumnName("item_definition_key").HasMaxLength(160).IsRequired();
        builder.Property(i => i.AcquiredAtUtc).HasColumnName("acquired_at_utc");
        builder.Property(i => i.Source).HasColumnName("source").HasMaxLength(64);
        builder.Property(i => i.IsEquipped).HasColumnName("is_equipped").HasDefaultValue(false);
        builder.Property(i => i.EquipmentSlot).HasColumnName("equipment_slot").HasMaxLength(16).HasDefaultValue("Relic").IsRequired();
        builder.HasIndex(i => new { i.PlayerCharacterId, i.ItemDefinitionKey }).IsUnique();
        builder.HasOne(i => i.PlayerCharacter)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.PlayerCharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlayerPermanentItemEntityConfiguration : IEntityTypeConfiguration<PlayerPermanentItemEntity>
{
    public void Configure(EntityTypeBuilder<PlayerPermanentItemEntity> builder)
    {
        builder.ToTable("player_permanent_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.PlayerProfileId).HasColumnName("player_profile_id");
        builder.Property(i => i.ItemDefinitionKey).HasColumnName("item_definition_key").HasMaxLength(160).IsRequired();
        builder.Property(i => i.SourceRunId).HasColumnName("source_run_id");
        builder.Property(i => i.AcquiredAtUtc).HasColumnName("acquired_at_utc");
        builder.Property(i => i.ContainedLiquidDefinitionKey).HasColumnName("contained_liquid_definition_key").HasMaxLength(256);
        builder.HasIndex(i => new { i.PlayerProfileId, i.ItemDefinitionKey }).IsUnique();
        builder.HasOne(i => i.PlayerProfile)
            .WithMany(p => p.PermanentItems)
            .HasForeignKey(i => i.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlayerRunStatisticEntityConfiguration : IEntityTypeConfiguration<PlayerRunStatisticEntity>
{
    public void Configure(EntityTypeBuilder<PlayerRunStatisticEntity> builder)
    {
        builder.ToTable("player_run_statistics");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.PlayerProfileId).HasColumnName("player_profile_id");
        builder.Property(s => s.RunId).HasColumnName("run_id");
        builder.Property(s => s.Seed).HasColumnName("seed").HasMaxLength(128).IsRequired();
        builder.Property(s => s.FinalDepth).HasColumnName("final_depth");
        builder.Property(s => s.Outcome).HasColumnName("outcome").HasMaxLength(32).IsRequired();
        builder.Property(s => s.GeneratorVersion).HasColumnName("generator_version").HasMaxLength(64).IsRequired();
        builder.Property(s => s.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(s => s.EndedAtUtc).HasColumnName("ended_at_utc");
        builder.Property(s => s.TotalDamageDealt).HasColumnName("total_damage_dealt").HasDefaultValue(0);
        builder.Property(s => s.TotalDamageTaken).HasColumnName("total_damage_taken").HasDefaultValue(0);
        builder.Property(s => s.TotalGuardAbsorbed).HasColumnName("total_guard_absorbed").HasDefaultValue(0);
        builder.Property(s => s.TotalHealingDone).HasColumnName("total_healing_done").HasDefaultValue(0);
        builder.Property(s => s.CombatsWon).HasColumnName("combats_won").HasDefaultValue(0);
        builder.Property(s => s.CombatsLost).HasColumnName("combats_lost").HasDefaultValue(0);
        builder.Property(s => s.RewardsSelected).HasColumnName("rewards_selected").HasDefaultValue(0);
        builder.Property(s => s.TotalItemsUsed).HasColumnName("total_items_used").HasDefaultValue(0);
        builder.HasIndex(s => s.RunId).IsUnique();
        builder.HasOne(s => s.PlayerProfile)
            .WithMany(p => p.RunStatistics)
            .HasForeignKey(s => s.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlayerNpcReputationScoreEntityConfiguration : IEntityTypeConfiguration<PlayerNpcReputationScoreEntity>
{
    public void Configure(EntityTypeBuilder<PlayerNpcReputationScoreEntity> builder)
    {
        builder.ToTable("player_npc_reputation_scores");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.PlayerProfileId).HasColumnName("player_profile_id");
        builder.Property(s => s.NpcKey).HasColumnName("npc_key").HasMaxLength(128).IsRequired();
        builder.Property(s => s.Score).HasColumnName("score").HasDefaultValue(0);
        builder.Property(s => s.TimesMet).HasColumnName("times_met").HasDefaultValue(0);
        builder.Property(s => s.CurrentDialogueNodeKey).HasColumnName("current_dialogue_node_key").HasMaxLength(256);
        builder.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(s => new { s.PlayerProfileId, s.NpcKey }).IsUnique();
        builder.HasOne(s => s.PlayerProfile)
            .WithMany(p => p.NpcReputationScores)
            .HasForeignKey(s => s.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
