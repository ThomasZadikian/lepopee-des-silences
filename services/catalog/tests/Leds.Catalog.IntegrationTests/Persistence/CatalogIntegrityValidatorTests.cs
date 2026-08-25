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
    public async Task Validate_should_fail_when_a_dialogue_choice_references_a_missing_node()
    {
        await using var context = _fixture.CreateContext().Context;
        var seed = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await seed.ApplyBaseSeedAsync();

        var npc = await context.NpcDefinitions.FirstAsync();
        npc.DialogueGraphJson = """
            {
              "key": "npc.broken.dialogue",
              "version": "1.0",
              "entryNodeKey": "start",
              "nodes": {
                "start": {
                  "key": "start",
                  "speaker": "Test",
                  "lines": ["..."],
                  "choices": [
                    { "key": "go", "label": "...", "requirements": [], "consequences": [], "nextNodeKey": "does-not-exist" }
                  ]
                }
              }
            }
            """;
        await context.SaveChangesAsync();

        var validator = new CatalogIntegrityValidator(context);

        var act = () => validator.ValidateAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*references missing node 'does-not-exist'*");
    }

    [Fact]
    public async Task Validate_should_fail_when_a_dialogue_node_has_duplicate_choice_keys()
    {
        await using var context = _fixture.CreateContext().Context;
        var seed = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await seed.ApplyBaseSeedAsync();

        var npc = await context.NpcDefinitions.FirstAsync();
        npc.DialogueGraphJson = """
            {
              "key": "npc.broken.dialogue",
              "version": "1.0",
              "entryNodeKey": "start",
              "nodes": {
                "start": {
                  "key": "start",
                  "speaker": "Test",
                  "lines": ["..."],
                  "choices": [
                    { "key": "go", "label": "A", "requirements": [], "consequences": [] },
                    { "key": "go", "label": "B", "requirements": [], "consequences": [] }
                  ]
                }
              }
            }
            """;
        await context.SaveChangesAsync();

        var validator = new CatalogIntegrityValidator(context);

        var act = () => validator.ValidateAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplicate choice key 'go'*");
    }

    [Fact]
    public async Task Validate_should_fail_when_the_entry_node_does_not_exist()
    {
        await using var context = _fixture.CreateContext().Context;
        var seed = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await seed.ApplyBaseSeedAsync();

        var npc = await context.NpcDefinitions.FirstAsync();
        npc.DialogueGraphJson = """
            {
              "key": "npc.broken.dialogue",
              "version": "1.0",
              "entryNodeKey": "does-not-exist",
              "nodes": {
                "start": { "key": "start", "speaker": "Test", "lines": ["..."], "choices": [] }
              }
            }
            """;
        await context.SaveChangesAsync();

        var validator = new CatalogIntegrityValidator(context);

        var act = () => validator.ValidateAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*entry node 'does-not-exist' does not exist*");
    }

    [Fact]
    public async Task Validate_should_not_gate_publication_on_legacy_enemy_menace()
    {
        await using var context = _fixture.CreateContext().Context;
        var seed = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await seed.ApplyBaseSeedAsync();

        var enemy = await context.EnemyDefinitions.FirstAsync();
        enemy.MenaceLevel = 0;
        await context.SaveChangesAsync();

        var act = () => new CatalogIntegrityValidator(context).ValidateAsync();

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
