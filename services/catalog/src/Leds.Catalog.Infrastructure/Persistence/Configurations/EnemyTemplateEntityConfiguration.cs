using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class EnemyTemplateEntityConfiguration : IEntityTypeConfiguration<EnemyTemplateEntity>
{
    public void Configure(EntityTypeBuilder<EnemyTemplateEntity> builder)
    {
        builder.ToTable("catalog_enemy_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Archetype).HasColumnName("archetype").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Element).HasColumnName("element").HasMaxLength(64).IsRequired();
        builder.Property(e => e.MaxHealth).HasColumnName("max_health");
        builder.Property(e => e.Strength).HasColumnName("strength");
        builder.Property(e => e.Intelligence).HasColumnName("intelligence");
        builder.Property(e => e.Speed).HasColumnName("speed");
        builder.Property(e => e.PhysicalResistance).HasColumnName("physical_resistance");
        builder.Property(e => e.MagicalResistance).HasColumnName("magical_resistance");
        builder.Property(e => e.ExperienceReward).HasColumnName("experience_reward");
        builder.Property(e => e.GoldReward).HasColumnName("gold_reward");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
