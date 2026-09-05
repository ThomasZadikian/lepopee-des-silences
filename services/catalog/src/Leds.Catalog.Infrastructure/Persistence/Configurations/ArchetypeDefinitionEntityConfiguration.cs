using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ArchetypeDefinitionEntityConfiguration : IEntityTypeConfiguration<ArchetypeDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<ArchetypeDefinitionEntity> builder)
    {
        builder.ToTable("catalog_archetype_definitions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("id");
        builder.Property(entity => entity.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Description).HasColumnName("description").IsRequired();
        builder.Property(entity => entity.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.BaseStatsJson).HasColumnName("base_stats_json").HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.ProficiencyTagsJson).HasColumnName("proficiency_tags_json").HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.StarterEquipmentJson).HasColumnName("starter_equipment_json").HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.StarterKnownSkillsJson).HasColumnName("starter_known_skills_json").HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.StarterEquippedSkillsJson).HasColumnName("starter_equipped_skills_json").HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(entity => entity.Key).IsUnique();
        builder.HasIndex(entity => entity.Status);
    }
}
