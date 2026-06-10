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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameEngineDbContext).Assembly);
    }
}
