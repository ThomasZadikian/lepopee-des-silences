using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Leds.GameEngine.Infrastructure.Persistence;

public sealed class GameEngineDbContextFactory : IDesignTimeDbContextFactory<GameEngineDbContext>
{
    public GameEngineDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GameEngineDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=leds_game_engine;Username=postgres;Password=postgres");

        return new GameEngineDbContext(optionsBuilder.Options);
    }
}