using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class CatalogSeedVersionEntityConfiguration : IEntityTypeConfiguration<CatalogSeedVersionEntity>
{
    public void Configure(EntityTypeBuilder<CatalogSeedVersionEntity> builder)
    {
        builder.ToTable("catalog_seed_versions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.SeedKey).HasColumnName("seed_key").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Checksum).HasColumnName("checksum").HasMaxLength(256);
        builder.Property(e => e.AppliedAtUtc).HasColumnName("applied_at_utc");

        builder.HasIndex(e => new { e.SeedKey, e.Version }).IsUnique();
    }
}
