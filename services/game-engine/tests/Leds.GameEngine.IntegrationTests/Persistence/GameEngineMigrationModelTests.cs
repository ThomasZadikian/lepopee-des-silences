using FluentAssertions;
using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.IntegrationTests.Persistence;

public sealed class GameEngineMigrationModelTests
{
    [Fact]
    public void Current_model_should_match_the_latest_migration_snapshot()
    {
        var options = new DbContextOptionsBuilder<GameEngineDbContext>()
            .UseNpgsql("Host=localhost;Database=model_check;Username=model_check;Password=model_check")
            .Options;

        using var context = new GameEngineDbContext(options);

        context.Database.HasPendingModelChanges().Should().BeFalse();
    }
}
