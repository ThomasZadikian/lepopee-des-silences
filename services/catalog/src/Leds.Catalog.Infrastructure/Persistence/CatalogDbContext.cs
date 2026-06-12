using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<SkillDefinitionEntity> SkillDefinitions => Set<SkillDefinitionEntity>();
    public DbSet<EnemyDefinitionEntity> EnemyDefinitions => Set<EnemyDefinitionEntity>();
    public DbSet<ItemDefinitionEntity> ItemDefinitions => Set<ItemDefinitionEntity>();
    public DbSet<PalaceLawDefinitionEntity> PalaceLawDefinitions => Set<PalaceLawDefinitionEntity>();
    public DbSet<CatalogSeedVersionEntity> CatalogSeedVersions => Set<CatalogSeedVersionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
