using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Leds.Player.Infrastructure.Persistence;

public sealed class PlayerDbContextFactory : IDesignTimeDbContextFactory<PlayerDbContext>
{
    public PlayerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlayerDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5433;Database=leds_player;Username=postgres;Password=postgres");

        return new PlayerDbContext(optionsBuilder.Options);
    }
}
