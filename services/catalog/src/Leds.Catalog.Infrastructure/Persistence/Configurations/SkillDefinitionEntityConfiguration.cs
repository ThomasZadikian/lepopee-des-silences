using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class SkillDefinitionEntityConfiguration : IEntityTypeConfiguration<SkillDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<SkillDefinitionEntity> builder)
    {
        builder.ToTable("catalog_skill_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1024).IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.SkillType).HasColumnName("skill_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.TargetingType).HasColumnName("targeting_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ManaCost).HasColumnName("mana_cost");
        builder.Property(e => e.ChargeCost).HasColumnName("charge_cost");
        builder.Property(e => e.BasePower).HasColumnName("base_power");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
