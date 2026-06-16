using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class EnemyDefinitionEntityConfiguration : IEntityTypeConfiguration<EnemyDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<EnemyDefinitionEntity> builder)
    {
        builder.ToTable("catalog_enemy_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.NarrativeText).HasColumnName("narrative_text");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Archetype).HasColumnName("archetype").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Family).HasColumnName("family").HasMaxLength(64);
        builder.Property(e => e.Rank).HasColumnName("rank").HasMaxLength(64).HasDefaultValue("Common").IsRequired();
        builder.Property(e => e.Role).HasColumnName("role").HasMaxLength(64);
        builder.Property(e => e.BaseDifficulty).HasColumnName("base_difficulty");
        builder.Property(e => e.EncounterWeight).HasColumnName("encounter_weight").HasDefaultValue(1);
        builder.Property(e => e.MinRiskLevel).HasColumnName("min_risk_level");
        builder.Property(e => e.MaxRiskLevel).HasColumnName("max_risk_level");
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.IsBoss).HasColumnName("is_boss");
        builder.Property(e => e.IsElite).HasColumnName("is_elite");
        builder.Property(e => e.RewardProfileKey).HasColumnName("reward_profile_key").HasMaxLength(128);
        builder.Property(e => e.BaseWeight).HasColumnName("base_weight").HasDefaultValue(1);
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.CompatibleRoomTypesJson).HasColumnName("compatible_room_types_json").HasComment("Legacy JSON compatibility column. Structured room pools/tags are relational in data-model-0.1.");
        builder.Property(e => e.TagsJson).HasColumnName("tags_json").HasComment("Legacy JSON compatibility column. Use catalog_enemy_tags for structured tags.");
        builder.Property(e => e.SkillKeysJson).HasColumnName("skill_keys_json").HasComment("Legacy JSON compatibility column. Use catalog_enemy_skill_links for structured skill links.");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
