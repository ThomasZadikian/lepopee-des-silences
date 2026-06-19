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
    public DbSet<EnemyStatBlockEntity> EnemyStatBlocks => Set<EnemyStatBlockEntity>();
    public DbSet<EnemySkillLinkEntity> EnemySkillLinks => Set<EnemySkillLinkEntity>();
    public DbSet<ItemDefinitionEntity> ItemDefinitions => Set<ItemDefinitionEntity>();
    public DbSet<PalaceLawDefinitionEntity> PalaceLawDefinitions => Set<PalaceLawDefinitionEntity>();
    public DbSet<CurseDefinitionEntity> CurseDefinitions => Set<CurseDefinitionEntity>();
    public DbSet<EffectSetEntity> EffectSets => Set<EffectSetEntity>();
    public DbSet<EffectDefinitionEntity> EffectDefinitions => Set<EffectDefinitionEntity>();
    public DbSet<RewardTemplateEntity> RewardTemplates => Set<RewardTemplateEntity>();
    public DbSet<RewardTemplateOptionEntity> RewardTemplateOptions => Set<RewardTemplateOptionEntity>();
    public DbSet<RoomDefinitionEntity> RoomDefinitions => Set<RoomDefinitionEntity>();
    public DbSet<RoomTypeDefinitionEntity> RoomTypeDefinitions => Set<RoomTypeDefinitionEntity>();
    public DbSet<RoomBossDefinitionEntity> RoomBossDefinitions => Set<RoomBossDefinitionEntity>();
    public DbSet<RoomSpecialMechanicEntity> RoomSpecialMechanics => Set<RoomSpecialMechanicEntity>();
    public DbSet<RoomEnemyPoolEntity> RoomEnemyPools => Set<RoomEnemyPoolEntity>();
    public DbSet<RoomEnemyPoolEntryEntity> RoomEnemyPoolEntries => Set<RoomEnemyPoolEntryEntity>();
    public DbSet<RoomRewardPoolEntity> RoomRewardPools => Set<RoomRewardPoolEntity>();
    public DbSet<RoomRewardPoolEntryEntity> RoomRewardPoolEntries => Set<RoomRewardPoolEntryEntity>();
    public DbSet<RoomLawPoolEntity> RoomLawPools => Set<RoomLawPoolEntity>();
    public DbSet<RoomLawPoolEntryEntity> RoomLawPoolEntries => Set<RoomLawPoolEntryEntity>();
    public DbSet<RoomCursePoolEntity> RoomCursePools => Set<RoomCursePoolEntity>();
    public DbSet<RoomCursePoolEntryEntity> RoomCursePoolEntries => Set<RoomCursePoolEntryEntity>();
    public DbSet<CatalogTagEntity> CatalogTags => Set<CatalogTagEntity>();
    public DbSet<CatalogSeedVersionEntity> CatalogSeedVersions => Set<CatalogSeedVersionEntity>();
    public DbSet<NpcDefinitionEntity> NpcDefinitions => Set<NpcDefinitionEntity>();
    public DbSet<EnemyTemplateEntity> EnemyTemplates => Set<EnemyTemplateEntity>();
    public DbSet<SkillTemplateEntity> SkillTemplates => Set<SkillTemplateEntity>();
    public DbSet<EventTemplateEntity> EventTemplates => Set<EventTemplateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
