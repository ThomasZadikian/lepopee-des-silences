using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.Infrastructure.Persistence;

public sealed class GameEngineDbContext : DbContext
{
    public GameEngineDbContext(DbContextOptions<GameEngineDbContext> options)
        : base(options)
    {
    }

    public DbSet<RunEntity> Runs => Set<RunEntity>();
    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();
    public DbSet<MapNodeEntity> MapNodes => Set<MapNodeEntity>();
    public DbSet<MapNodeParentNodeEntity> MapNodeParentNodes => Set<MapNodeParentNodeEntity>();
    public DbSet<RunMemoryFragmentEntity> RunMemoryFragments => Set<RunMemoryFragmentEntity>();
    public DbSet<RunActivePalaceLawEntity> RunActivePalaceLaws => Set<RunActivePalaceLawEntity>();
    public DbSet<CombatEntity> Combats => Set<CombatEntity>();
    public DbSet<CombatantEntity> Combatants => Set<CombatantEntity>();
    public DbSet<CombatantSkillEntity> CombatantSkills => Set<CombatantSkillEntity>();
    public DbSet<PlayerRuntimeStateEntity> PlayerRuntimeStates => Set<PlayerRuntimeStateEntity>();
    public DbSet<PlayerRuntimeSkillEntity> PlayerRuntimeSkills => Set<PlayerRuntimeSkillEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameEngineDbContext).Assembly);
    }
}
