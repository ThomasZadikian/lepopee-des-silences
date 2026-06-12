using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ItemDefinitionEntityConfiguration : IEntityTypeConfiguration<ItemDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<ItemDefinitionEntity> builder)
    {
        builder.ToTable("catalog_item_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1024).IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Rarity).HasColumnName("rarity").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(64).IsRequired();
        builder.Property(e => e.EffectValue).HasColumnName("effect_value");
        builder.Property(e => e.Price).HasColumnName("price");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
