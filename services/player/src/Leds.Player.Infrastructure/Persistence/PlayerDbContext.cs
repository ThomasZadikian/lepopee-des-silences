using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Player.Infrastructure.Persistence;

public sealed class PlayerDbContext : DbContext
{
    public PlayerDbContext(DbContextOptions<PlayerDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlayerProfileEntity> PlayerProfiles => Set<PlayerProfileEntity>();
    public DbSet<PlayerCharacterEntity> PlayerCharacters => Set<PlayerCharacterEntity>();
    public DbSet<PlayerCharacterStatBlockEntity> PlayerCharacterStatBlocks => Set<PlayerCharacterStatBlockEntity>();
    public DbSet<PlayerCharacterSkillEntity> PlayerCharacterSkills => Set<PlayerCharacterSkillEntity>();
    public DbSet<PlayerPermanentUnlockEntity> PlayerPermanentUnlocks => Set<PlayerPermanentUnlockEntity>();
    public DbSet<PlayerCharacterItemEntity> PlayerCharacterItems => Set<PlayerCharacterItemEntity>();
    public DbSet<PlayerPermanentItemEntity> PlayerPermanentItems => Set<PlayerPermanentItemEntity>();
    public DbSet<PlayerRunStatisticEntity> PlayerRunStatistics => Set<PlayerRunStatisticEntity>();
    public DbSet<ProcessedIntegrationEventEntity> ProcessedIntegrationEvents => Set<ProcessedIntegrationEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlayerDbContext).Assembly);
    }
}
