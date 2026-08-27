using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leds.Catalog.IntegrationTests.Persistence;

[Collection("CatalogPostgres")]
public sealed class CatalogIntegrityValidatorCoverageTests
{
    private readonly CatalogPostgresFixture _fixture;

    public CatalogIntegrityValidatorCoverageTests(CatalogPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Validate_should_accumulate_invalid_skill_enemy_npc_and_item_contracts()
    {
        await using var context = _fixture.CreateContext().Context;
        await new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance).ApplyBaseSeedAsync();

        var skill = await context.SkillDefinitions.FirstAsync(x => x.Status == "Active");
        skill.EmotionalRegister = "unknown-register";
        skill.Category = "Invalid";
        skill.TacticalAreaShape = "Circle";
        skill.Audience = "Nobody";
        skill.ManaCost = -1;
        // jsonb rejects malformed JSON before the publication gate can inspect it. A valid JSON
        // object is nevertheless the wrong shape for an effect array and exercises the gate's
        // deserialization failure branch without bypassing PostgreSQL's integrity rules.
        skill.EffectsJson = "{}";
        skill.AllowedArchetypesJson = "[\"missing-archetype\"]";

        var enemies = await context.EnemyDefinitions
            .Where(x => x.Status == "Active")
            .Include(x => x.StatBlock)
            .Take(11)
            .ToArrayAsync();
        enemies.Should().HaveCountGreaterThanOrEqualTo(11);
        enemies[0].Archetype = "UnknownArchetype";
        enemies[0].Registre = "unknown-register";
        enemies[0].SkillKeysJson = "[\"missing.skill\"]";
        enemies[0].StatBlock!.MaxVitality = 0;
        enemies[1].StatBlock!.Speed = 0;
        enemies[2].StatBlock!.Movement = 0;
        enemies[3].StatBlock!.AttackPower = -1;
        enemies[4].StatBlock!.Defense = -1;
        enemies[5].StatBlock!.StartingGuard = -1;
        enemies[6].StatBlock!.Focus = -1;
        enemies[7].StatBlock!.Mana = -1;
        enemies[8].StatBlock!.Charge = -1;
        enemies[9].StatBlock!.MagicAttack = -1;
        enemies[10].StatBlock!.MagicDefense = -1;

        context.EnemyDefinitions.Add(new EnemyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            Key = "coverage.enemy.without-stat-block",
            Name = "coverage.enemy.without-stat-block",
            DisplayName = "Coverage enemy",
            Description = "Coverage-only invalid publication candidate.",
            Version = "1.0",
            Status = "Active",
            Archetype = "UnknownArchetype",
            Registre = "silence",
            MenaceLevel = 1,
            BaseDifficulty = 1,
            EncounterWeight = 1,
            MinRiskLevel = 1,
            MaxRiskLevel = 1,
            BaseWeight = 1
        });

        var npcs = await context.NpcDefinitions.Where(x => x.Status == "Active").Take(2).ToArrayAsync();
        npcs.Should().HaveCountGreaterThanOrEqualTo(2);
        npcs[0].EmotionalAffinity = "unknown-register";
        npcs[0].OfferingsJson = """
            [{
              "key": "offer.coverage.missing-kit",
              "kind": "Companion",
              "targetKey": null,
              "amount": 0,
              "isMajor": true,
              "unlockConditions": []
            }]
            """;
        npcs[0].DialogueGraphJson = """
            {
              "key": "npc.coverage.dialogue",
              "version": "1.0",
              "entryNodeKey": "start",
              "nodes": {
                "start": {
                  "key": "mismatched-key",
                  "speaker": "Coverage",
                  "lines": ["..."],
                  "choices": [{
                    "key": "invalid-choice",
                    "label": "Invalid",
                    "requirements": [
                      { "kind": "WoundStateAtLeast" },
                      { "kind": "RelationshipScoreAtLeast" }
                    ],
                    "consequences": [
                      { "kind": "ArmWound" },
                      { "kind": "SootheWound" },
                      { "kind": "GrantOffering" }
                    ]
                  }]
                }
              }
            }
            """;
        npcs[1].OfferingsJson = """
            [{
              "key": "offer.coverage.invalid-kit",
              "kind": "Companion",
              "targetKey": "missing.character",
              "amount": 0,
              "isMajor": true,
              "unlockConditions": [],
              "companionKit": {
                "maxVitality": 0,
                "attackPower": 1,
                "defense": 1,
                "startingGuard": 0,
                "speed": 1,
                "initiative": 0,
                "focus": 0,
                "mana": 0,
                "charge": 0,
                "skillKeys": ["missing.skill"],
                "magicAttack": 0,
                "magicDefense": 0
              }
            }]
            """;

        var items = await context.ItemDefinitions.Where(x => x.Status == "Active").Take(2).ToArrayAsync();
        items.Should().HaveCountGreaterThanOrEqualTo(2);
        items[0].EquipmentEffectsJson = "{}";
        items[1].EquipmentEffectsJson = """
            [{ "kind": "GrantSkill", "skillKey": "missing.skill" }]
            """;

        await context.SaveChangesAsync();

        var act = () => new CatalogIntegrityValidator(context).ValidateAsync();

        var assertion = await act.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Message.Should().Contain("category must be Physical or Magic")
            .And.Contain("tactical area shape is invalid")
            .And.Contain("audience is invalid")
            .And.Contain("costs, power and cooldown must be non-negative")
            .And.Contain("archetype 'missing-archetype'")
            .And.Contain("stat block contains invalid values")
            .And.Contain("stat block is required")
            .And.Contain("requires a target and kit")
            .And.Contain("companion target 'missing.character'")
            .And.Contain("companion skill 'missing.skill'")
            .And.Contain("mismatched Key")
            .And.Contain("WoundStateAtLeast requirement")
            .And.Contain("RelationshipScoreAtLeast requirement")
            .And.Contain("ArmWound consequence")
            .And.Contain("SootheWound consequence")
            .And.Contain("GrantOffering consequence")
            .And.Contain("granted skill 'missing.skill'")
            .And.Contain("invalid JSON");
    }

    [Fact]
    public async Task Validate_should_accumulate_invalid_room_world_law_and_story_contracts()
    {
        await using var context = _fixture.CreateContext().Context;
        await new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance).ApplyBaseSeedAsync();

        var rooms = await context.RoomDefinitions.Where(x => x.Status == "Active").Take(6).ToArrayAsync();
        rooms.Should().HaveCountGreaterThanOrEqualTo(6);
        rooms[0].Key = string.Empty;
        rooms[1].Version = string.Empty;
        rooms[2].Theme = string.Empty;
        rooms[3].BaseWeight = 0;
        rooms[4].MinDepth = 5;
        rooms[4].MaxDepth = 1;
        rooms[5].BossDefinitionKey = "missing.boss";

        var bosses = await context.RoomBossDefinitions.Where(x => x.Status == "Active").Take(3).ToArrayAsync();
        bosses.Should().HaveCountGreaterThanOrEqualTo(3);
        bosses[0].Version = string.Empty;
        bosses[1].BaseDifficulty = 0;
        bosses[2].EnemyDefinitionKey = "missing.enemy";

        var world = await context.WorldDefinitions
            .Where(x => x.Status == "Active")
            .Include(x => x.EntryRoomDefinition)
            .FirstAsync();
        world.Version = string.Empty;
        world.EntryRoomDefinition.Status = "Inactive";

        var roomTypes = await context.RoomTypeDefinitions.Where(x => x.Status == "Active").Take(3).ToArrayAsync();
        roomTypes.Should().HaveCountGreaterThanOrEqualTo(3);
        roomTypes[0].Key = string.Empty;
        roomTypes[1].Version = string.Empty;
        roomTypes[2].Theme = string.Empty;

        var laws = await context.PalaceLawDefinitions.Where(x => x.Status == "Active").Take(5).ToArrayAsync();
        laws.Should().HaveCountGreaterThanOrEqualTo(5);
        laws[0].Version = string.Empty;
        laws[1].Severity = 0;
        laws[2].BaseWeight = 0;
        laws[3].RoomKey = "missing.room";
        laws[4].ExclusionKeysJson = "[\"missing.law\"]";

        var now = DateTime.UtcNow;
        context.StorySequenceDefinitions.Add(new StorySequenceDefinitionEntity
        {
            Id = Guid.NewGuid(),
            Key = string.Empty,
            DisplayName = "Coverage story",
            Version = string.Empty,
            Status = "Active",
            EntryStepKey = "missing-entry",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Steps =
            [
                new StoryStepDefinitionEntity
                {
                    Id = Guid.NewGuid(),
                    Key = "coverage-step",
                    Order = 1,
                    RoomDefinitionKey = "missing.room",
                    ConditionsJson = "[]",
                    EffectsJson = "[]",
                    IsTerminal = false
                }
            ]
        });

        await context.SaveChangesAsync();

        var act = () => new CatalogIntegrityValidator(context).ValidateAsync();

        var assertion = await act.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Message.Should().Contain("key, version, theme and positive weight are required")
            .And.Contain("minimum depth cannot exceed maximum depth")
            .And.Contain("boss 'missing.boss'")
            .And.Contain("positive base difficulty")
            .And.Contain("enemy 'missing.enemy'")
            .And.Contain("active entry room is required")
            .And.Contain("Room type")
            .And.Contain("positive severity and weight")
            .And.Contain("room 'missing.room'")
            .And.Contain("excluded law 'missing.law'")
            .And.Contain("entry step 'missing-entry' does not exist")
            .And.Contain("at least one terminal step is required");
    }

    [Fact]
    public async Task Validate_should_reject_catalog_without_active_core_collections()
    {
        await using var context = _fixture.CreateContext().Context;
        await new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance).ApplyBaseSeedAsync();

        await context.SkillDefinitions.Where(x => x.Status == "Active")
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, "Inactive"));
        await context.EnemyDefinitions.Where(x => x.Status == "Active")
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, "Inactive"));
        await context.NpcDefinitions.Where(x => x.Status == "Active")
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, "Inactive"));
        await context.ItemDefinitions.Where(x => x.Status == "Active")
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, "Inactive"));

        var act = () => new CatalogIntegrityValidator(context).ValidateAsync();

        var assertion = await act.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Message.Should().Contain("at least one active skill")
            .And.Contain("at least one active enemy")
            .And.Contain("at least one active NPC")
            .And.Contain("at least one active item");
    }
}
