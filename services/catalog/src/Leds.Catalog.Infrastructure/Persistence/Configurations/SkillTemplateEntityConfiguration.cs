using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class SkillTemplateEntityConfiguration : IEntityTypeConfiguration<SkillTemplateEntity>
{
    public void Configure(EntityTypeBuilder<SkillTemplateEntity> builder)
    {
        builder.ToTable("catalog_skill_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Element).HasColumnName("element").HasMaxLength(64).IsRequired();
        builder.Property(e => e.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.TargetType).HasColumnName("target_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ManaCost).HasColumnName("mana_cost");
        builder.Property(e => e.ChargeCost).HasColumnName("charge_cost");
        builder.Property(e => e.BasePower).HasColumnName("base_power");
        builder.Property(e => e.HealPower).HasColumnName("heal_power");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
