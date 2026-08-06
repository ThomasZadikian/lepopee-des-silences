using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leds.Catalog.IntegrationTests.Persistence;

[Collection("CatalogPostgres")]
public sealed class CatalogIntegrityValidatorTests
{
    private readonly CatalogPostgresFixture _fixture;

    public CatalogIntegrityValidatorTests(CatalogPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Canonical_seed_should_pass_the_cross_definition_publication_gate()
    {
        await using var context = _fixture.CreateContext().Context;
        var seed = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await seed.ApplyBaseSeedAsync();
        var validator = new CatalogIntegrityValidator(context);

        var act = () => validator.ValidateAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Canonical_reseed_should_repair_existing_enemy_menace_levels()
    {
        var (context, connectionString) = _fixture.CreateContext();
        await using (context)
        {
            var seed = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
            await seed.ApplyBaseSeedAsync();

            var regularEnemy = await context.EnemyDefinitions
                .SingleAsync(enemy => enemy.Key == "canon.enemy.enfant-argile");
            var bossEnemy = await context.EnemyDefinitions
                .SingleAsync(enemy => enemy.Key == "canon.enemy.himlit");
            regularEnemy.MenaceLevel = 0;
            bossEnemy.MenaceLevel = 0;
            await context.SaveChangesAsync();
        }

        await using var reseedContext = _fixture.CreateContext(connectionString);
        var reseed = new CatalogSeedRunner(reseedContext, NullLogger<CatalogSeedRunner>.Instance);
        await reseed.ApplyBaseSeedAsync();

        var repairedLevels = await reseedContext.EnemyDefinitions
            .Where(enemy => enemy.Key == "canon.enemy.enfant-argile" || enemy.Key == "canon.enemy.himlit")
            .ToDictionaryAsync(enemy => enemy.Key, enemy => enemy.MenaceLevel);

        repairedLevels["canon.enemy.enfant-argile"].Should().Be(2);
        repairedLevels["canon.enemy.himlit"].Should().Be(10);
        await new CatalogIntegrityValidator(reseedContext).ValidateAsync();
    }
}
