using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leds.Catalog.UnitTests.Infrastructure.Persistence;

public sealed class CatalogSeedRunnerTests
{
    [Fact]
    public async Task ApplyBaseSeedAsync_ShouldSeedDataModelCatalogDefinitions()
    {
        await using var context = CreateContext();
        var runner = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);

        await runner.ApplyBaseSeedAsync(CancellationToken.None);

        (await context.CatalogSeedVersions.AnyAsync(v => v.Version == "alpha-0.8.1")).Should().BeTrue();
        (await context.EnemyStatBlocks.AnyAsync()).Should().BeTrue();
        (await context.EffectSets.Include(e => e.Effects).AnyAsync(e => e.Effects.Any())).Should().BeTrue();
        (await context.CurseDefinitions.AnyAsync(c => c.Key == "curse.threshold.souffle-lourd")).Should().BeTrue();
        (await context.RewardTemplates.Include(r => r.Options).AnyAsync(r => r.Options.Any())).Should().BeTrue();
        (await context.RoomDefinitions.AnyAsync(r => r.Key == "room.rare.creuset-equivalences" && r.IsCulturalEcho)).Should().BeTrue();
        (await context.RoomEnemyPools.Include(p => p.Entries).AnyAsync(p => p.Entries.Any())).Should().BeTrue();
        (await context.CatalogTags.AnyAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task ApplyBaseSeedAsync_ShouldPersistEnemyWithStatBlockAndSkillLinks()
    {
        await using var context = CreateContext();
        var runner = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);

        await runner.ApplyBaseSeedAsync(CancellationToken.None);

        var enemy = await context.EnemyDefinitions
            .Include(e => e.StatBlock)
            .Include(e => e.SkillLinks)
            .SingleAsync(e => e.Key == "enemy.threshold.echo");

        enemy.StatBlock.Should().NotBeNull();
        enemy.StatBlock!.MaxVitality.Should().BeGreaterThan(0);
        enemy.SkillLinks.Should().Contain(l => l.SkillDefinitionKey == "skill.basic.strike");
    }

    private static CatalogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }
}
