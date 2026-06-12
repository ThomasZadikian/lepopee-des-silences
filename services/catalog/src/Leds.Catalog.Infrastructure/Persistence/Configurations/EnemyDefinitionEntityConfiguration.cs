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
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1024).IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Archetype).HasColumnName("archetype").HasMaxLength(64).IsRequired();
        builder.Property(e => e.BaseDifficulty).HasColumnName("base_difficulty");
        builder.Property(e => e.MinRiskLevel).HasColumnName("min_risk_level");
        builder.Property(e => e.MaxRiskLevel).HasColumnName("max_risk_level");
        builder.Property(e => e.CompatibleRoomTypesJson).HasColumnName("compatible_room_types_json");
        builder.Property(e => e.TagsJson).HasColumnName("tags_json");
        builder.Property(e => e.SkillKeysJson).HasColumnName("skill_keys_json");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
