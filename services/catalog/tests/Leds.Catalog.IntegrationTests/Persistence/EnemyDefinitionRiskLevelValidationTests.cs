using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leds.Catalog.IntegrationTests.Persistence;

/// <summary>
/// Guards the invariant that broke most of the Bestiaire roster silently: game-engine's
/// <c>ResolveCurrentEventCommandHandler</c> compares an enemy's MinRiskLevel/MaxRiskLevel
/// against <c>Clamp(node.RiskLevel / 20 + 1, 1, 5)</c> — a 1-5 bucket, not the raw 0-100
/// node-risk scale. An enemy seeded with riskMin/riskMax outside [1,5] can never be
/// selected for any encounter, with no error anywhere — it just never appears.
/// </summary>
[Collection("CatalogPostgres")]
public sealed class EnemyDefinitionRiskLevelValidationTests
{
    private readonly CatalogPostgresFixture _fixture;

    public EnemyDefinitionRiskLevelValidationTests(CatalogPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AllSeededEnemies_ShouldHaveRiskLevelsWithinTheRuntimeOneToFiveScale()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var runner = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await runner.ApplyBaseSeedAsync(CancellationToken.None);

        var enemies = await context.EnemyDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync();

        enemies.Should().NotBeEmpty();

        enemies.Should().OnlyContain(
            e => e.MinRiskLevel >= 1 && e.MinRiskLevel <= 5 && e.MaxRiskLevel >= 1 && e.MaxRiskLevel <= 5,
            because: "riskMin/riskMax outside [1,5] can never satisfy the runtime's clamped risk-level filter");

        enemies.Should().OnlyContain(
            e => e.MinRiskLevel <= e.MaxRiskLevel,
            because: "an inverted range would make every risk-level comparison fail");
    }
}
