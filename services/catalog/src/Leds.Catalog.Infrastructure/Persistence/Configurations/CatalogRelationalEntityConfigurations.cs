using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class CatalogTagEntityConfiguration : IEntityTypeConfiguration<CatalogTagEntity>
{
    public void Configure(EntityTypeBuilder<CatalogTagEntity> builder)
    {
        builder.ToTable("catalog_tags");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TagKey).HasColumnName("tag_key").HasMaxLength(128).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(64);
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(e => e.TagKey).IsUnique();
    }
}

public sealed class EnemyStatBlockEntityConfiguration : IEntityTypeConfiguration<EnemyStatBlockEntity>
{
    public void Configure(EntityTypeBuilder<EnemyStatBlockEntity> builder)
    {
        builder.ToTable("catalog_enemy_stat_blocks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EnemyDefinitionId).HasColumnName("enemy_definition_id");
        builder.Property(e => e.MaxVitality).HasColumnName("max_vitality");
        builder.Property(e => e.AttackPower).HasColumnName("attack_power");
        builder.Property(e => e.Defense).HasColumnName("defense");
        builder.Property(e => e.StartingGuard).HasColumnName("starting_guard");
        builder.Property(e => e.Speed).HasColumnName("speed");
        builder.Property(e => e.Initiative).HasColumnName("initiative");
        builder.Property(e => e.Recovery).HasColumnName("recovery");
        builder.Property(e => e.Focus).HasColumnName("focus");
        builder.Property(e => e.Mana).HasColumnName("mana");
        builder.Property(e => e.Charge).HasColumnName("charge");
        builder.Property(e => e.MagicAttack).HasColumnName("magic_attack").HasDefaultValue(0);
        builder.Property(e => e.MagicDefense).HasColumnName("magic_defense").HasDefaultValue(0);
        builder.HasIndex(e => e.EnemyDefinitionId).IsUnique();
        builder.HasOne(e => e.EnemyDefinition)
            .WithOne(e => e.StatBlock)
            .HasForeignKey<EnemyStatBlockEntity>(e => e.EnemyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EnemySkillLinkEntityConfiguration : IEntityTypeConfiguration<EnemySkillLinkEntity>
{
    public void Configure(EntityTypeBuilder<EnemySkillLinkEntity> builder)
    {
        builder.ToTable("catalog_enemy_skill_links");
        builder.HasKey(e => new { e.EnemyDefinitionId, e.SkillDefinitionKey });
        builder.Property(e => e.EnemyDefinitionId).HasColumnName("enemy_definition_id");
        builder.Property(e => e.SkillDefinitionKey).HasColumnName("skill_definition_key").HasMaxLength(160).IsRequired();
        builder.HasOne(e => e.EnemyDefinition)
            .WithMany(e => e.SkillLinks)
            .HasForeignKey(e => e.EnemyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EffectSetEntityConfiguration : IEntityTypeConfiguration<EffectSetEntity>
{
    public void Configure(EntityTypeBuilder<EffectSetEntity> builder)
    {
        builder.ToTable("catalog_effect_sets");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}

public sealed class EffectDefinitionEntityConfiguration : IEntityTypeConfiguration<EffectDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<EffectDefinitionEntity> builder)
    {
        builder.ToTable("catalog_effect_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EffectSetId).HasColumnName("effect_set_id");
        builder.Property(e => e.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.TargetScope).HasColumnName("target_scope").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").HasPrecision(10, 4);
        builder.Property(e => e.ValueMode).HasColumnName("value_mode").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(64).IsRequired();
        builder.Property(e => e.StackPolicy).HasColumnName("stack_policy").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Condition).HasColumnName("condition").HasMaxLength(256);
        builder.Property(e => e.Order).HasColumnName("order");
        builder.Property(e => e.BehaviorTag).HasColumnName("behavior_tag").HasMaxLength(128);
        builder.Property(e => e.GenerationTag).HasColumnName("generation_tag").HasMaxLength(128);
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.HasIndex(e => new { e.EffectSetId, e.Order });
        builder.HasOne(e => e.EffectSet)
            .WithMany(e => e.Effects)
            .HasForeignKey(e => e.EffectSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CurseDefinitionEntityConfiguration : IEntityTypeConfiguration<CurseDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<CurseDefinitionEntity> builder)
    {
        builder.ToTable("catalog_curse_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.NarrativeText).HasColumnName("narrative_text");
        builder.Property(e => e.Severity).HasColumnName("severity");
        builder.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Trigger).HasColumnName("trigger").HasMaxLength(64);
        builder.Property(e => e.EffectSetId).HasColumnName("effect_set_id");
        builder.Property(e => e.BaseWeight).HasColumnName("base_weight");
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasOne(e => e.EffectSet).WithMany().HasForeignKey(e => e.EffectSetId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class RewardTemplateEntityConfiguration : IEntityTypeConfiguration<RewardTemplateEntity>
{
    public void Configure(EntityTypeBuilder<RewardTemplateEntity> builder)
    {
        builder.ToTable("catalog_reward_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.MinOptions).HasColumnName("min_options");
        builder.Property(e => e.MaxOptions).HasColumnName("max_options");
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.BaseWeight).HasColumnName("base_weight");
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}

public sealed class RewardTemplateOptionEntityConfiguration : IEntityTypeConfiguration<RewardTemplateOptionEntity>
{
    public void Configure(EntityTypeBuilder<RewardTemplateOptionEntity> builder)
    {
        builder.ToTable("catalog_reward_template_options");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.RewardTemplateId).HasColumnName("reward_template_id");
        builder.Property(e => e.RewardType).HasColumnName("reward_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Label).HasColumnName("label").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.PayloadKey).HasColumnName("payload_key").HasMaxLength(256);
        builder.Property(e => e.BaseAmount).HasColumnName("base_amount");
        builder.Property(e => e.ScalingMode).HasColumnName("scaling_mode").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Weight).HasColumnName("weight");
        builder.Property(e => e.EffectSetId).HasColumnName("effect_set_id");
        builder.HasOne(e => e.RewardTemplate).WithMany(e => e.Options).HasForeignKey(e => e.RewardTemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.EffectSet).WithMany().HasForeignKey(e => e.EffectSetId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class RoomDefinitionEntityConfiguration : IEntityTypeConfiguration<RoomDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<RoomDefinitionEntity> builder)
    {
        builder.ToTable("catalog_room_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.NarrativeText).HasColumnName("narrative_text");
        builder.Property(e => e.RoomFamily).HasColumnName("room_family").HasMaxLength(64).IsRequired();
        builder.Property(e => e.RoomRarity).HasColumnName("room_rarity").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Theme).HasColumnName("theme").HasMaxLength(128).IsRequired();
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.BaseWeight).HasColumnName("base_weight");
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.EnemyPoolKey).HasColumnName("enemy_pool_key").HasMaxLength(160);
        builder.Property(e => e.RewardPoolKey).HasColumnName("reward_pool_key").HasMaxLength(160);
        builder.Property(e => e.LawPoolKey).HasColumnName("law_pool_key").HasMaxLength(160);
        builder.Property(e => e.CursePoolKey).HasColumnName("curse_pool_key").HasMaxLength(160);
        builder.Property(e => e.SpecialMechanicKey).HasColumnName("special_mechanic_key").HasMaxLength(160);
        builder.Property(e => e.BossDefinitionKey).HasColumnName("boss_definition_key").HasMaxLength(160);
        builder.Property(e => e.IsUnique).HasColumnName("is_unique");
        builder.Property(e => e.IsCulturalEcho).HasColumnName("is_cultural_echo");
        builder.Property(e => e.WorldDefinitionId).HasColumnName("world_definition_id");
        builder.Property(e => e.ReachabilityMode).HasColumnName("reachability_mode").HasMaxLength(32).IsRequired();
        builder.Property(e => e.TriggersStrictChain).HasColumnName("triggers_strict_chain");
        builder.Property(e => e.ExcludeFromOpenPool).HasColumnName("exclude_from_open_pool");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.WorldDefinitionId);
        builder.HasOne(e => e.WorldDefinition)
            .WithMany(w => w.Rooms)
            .HasForeignKey(e => e.WorldDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WorldDefinitionEntityConfiguration : IEntityTypeConfiguration<WorldDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<WorldDefinitionEntity> builder)
    {
        builder.ToTable("catalog_world_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.EntryRoomDefinitionId).HasColumnName("entry_room_definition_id");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        // Restrict (not Cascade): the entry room and its World reference each other,
        // and a cascade cycle between the two tables is rejected by the database.
        builder.HasOne(e => e.EntryRoomDefinition)
            .WithMany()
            .HasForeignKey(e => e.EntryRoomDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RoomReachabilityEntityConfiguration : IEntityTypeConfiguration<RoomReachabilityEntity>
{
    public void Configure(EntityTypeBuilder<RoomReachabilityEntity> builder)
    {
        builder.ToTable("catalog_room_reachability");
        builder.HasKey(e => new { e.FromRoomDefinitionId, e.ToRoomDefinitionId });
        builder.Property(e => e.FromRoomDefinitionId).HasColumnName("from_room_definition_id");
        builder.Property(e => e.ToRoomDefinitionId).HasColumnName("to_room_definition_id");
        builder.HasOne(e => e.FromRoomDefinition)
            .WithMany(e => e.ReachableTo)
            .HasForeignKey(e => e.FromRoomDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.ToRoomDefinition)
            .WithMany(e => e.ReachableFrom)
            .HasForeignKey(e => e.ToRoomDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RoomThemeAffinityEntityConfiguration : IEntityTypeConfiguration<RoomThemeAffinityEntity>
{
    public void Configure(EntityTypeBuilder<RoomThemeAffinityEntity> builder)
    {
        builder.ToTable("catalog_room_theme_affinities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ThemeFrom).HasColumnName("theme_from").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ThemeTo).HasColumnName("theme_to").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Weight).HasColumnName("weight").HasColumnType("numeric(6,3)");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => new { e.ThemeFrom, e.ThemeTo }).IsUnique();
    }
}

public sealed class NpcReputationAffinityEntityConfiguration : IEntityTypeConfiguration<NpcReputationAffinityEntity>
{
    public void Configure(EntityTypeBuilder<NpcReputationAffinityEntity> builder)
    {
        builder.ToTable("catalog_npc_reputation_affinities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.NpcKeyFrom).HasColumnName("npc_key_from").HasMaxLength(160).IsRequired();
        builder.Property(e => e.NpcKeyTo).HasColumnName("npc_key_to").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Weight).HasColumnName("weight").HasColumnType("numeric(6,3)");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => new { e.NpcKeyFrom, e.NpcKeyTo }).IsUnique();
    }
}

public sealed class RoomTypeDefinitionEntityConfiguration : IEntityTypeConfiguration<RoomTypeDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<RoomTypeDefinitionEntity> builder)
    {
        builder.ToTable("catalog_room_type_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.Theme).HasColumnName("theme").HasMaxLength(128).IsRequired();
        builder.Property(e => e.DefaultRarity).HasColumnName("default_rarity").HasMaxLength(64).IsRequired();
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.HasIndex(e => e.Key).IsUnique();
    }
}

public sealed class RoomSpecialMechanicEntityConfiguration : IEntityTypeConfiguration<RoomSpecialMechanicEntity>
{
    public void Configure(EntityTypeBuilder<RoomSpecialMechanicEntity> builder)
    {
        builder.ToTable("catalog_room_special_mechanics");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.MechanicType).HasColumnName("mechanic_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.EffectSetId).HasColumnName("effect_set_id");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasOne(e => e.EffectSet).WithMany().HasForeignKey(e => e.EffectSetId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class RoomBossDefinitionEntityConfiguration : IEntityTypeConfiguration<RoomBossDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<RoomBossDefinitionEntity> builder)
    {
        builder.ToTable("catalog_room_boss_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.RoomType).HasColumnName("room_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.EnemyDefinitionKey).HasColumnName("enemy_definition_key").HasMaxLength(160);
        builder.Property(e => e.DangerHint).HasColumnName("danger_hint").HasMaxLength(512);
        builder.Property(e => e.BaseDifficulty).HasColumnName("base_difficulty");
        builder.Property(e => e.BaseWeight).HasColumnName("base_weight");
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.TagsJson).HasColumnName("tags_json");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
