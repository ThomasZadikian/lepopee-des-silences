using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Infrastructure.Catalog;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class InMemoryCatalogContentGatewayTests
{
    private readonly ICatalogContentGateway _gateway = new InMemoryCatalogContentGateway();

    [Fact]
    public async Task GetEnemyTemplateByKeyAsync_ShouldReturnEnemy_WhenKeyExists()
    {
        var result = await _gateway.GetEnemyTemplateByKeyAsync("enemy-shadow-v1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("enemy-shadow-v1");
        result.Value.SkillKeys.Should().Contain("skill-shadow-strike-v1");
    }

    [Fact]
    public async Task GetEnemyTemplateByKeyAsync_ShouldBeCaseInsensitive()
    {
        var result = await _gateway.GetEnemyTemplateByKeyAsync("ENEMY-SHADOW-V1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("enemy-shadow-v1");
    }

    [Fact]
    public async Task GetEnemyTemplateByKeyAsync_ShouldReturnFailure_WhenKeyDoesNotExist()
    {
        var result = await _gateway.GetEnemyTemplateByKeyAsync("enemy-missing-v1");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.enemy_template_not_found");
    }

    [Fact]
    public async Task GetEnemyTemplateByKeyAsync_ShouldReturnFailure_WhenKeyIsWhitespace()
    {
        var result = await _gateway.GetEnemyTemplateByKeyAsync(" ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.key_required");
    }

    [Fact]
    public async Task GetSkillTemplateByKeyAsync_ShouldReturnSkill_WhenKeyExists()
    {
        var result = await _gateway.GetSkillTemplateByKeyAsync("skill-shadow-strike-v1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("skill-shadow-strike-v1");
        result.Value.Power.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSkillTemplateByKeyAsync_ShouldReturnFailure_WhenKeyDoesNotExist()
    {
        var result = await _gateway.GetSkillTemplateByKeyAsync("skill-missing-v1");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.skill_template_not_found");
    }

    [Fact]
    public async Task GetItemTemplateByKeyAsync_ShouldReturnItem_WhenKeyExists()
    {
        var result = await _gateway.GetItemTemplateByKeyAsync("item-memory-fragment-v1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("item-memory-fragment-v1");
        result.Value.IsTemporary.Should().BeTrue();
    }

    [Fact]
    public async Task GetItemTemplateByKeyAsync_ShouldReturnFailure_WhenKeyDoesNotExist()
    {
        var result = await _gateway.GetItemTemplateByKeyAsync("item-missing-v1");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.item_template_not_found");
    }

    [Fact]
    public async Task GetEventTemplateByKeyAsync_ShouldReturnEventTemplate_WhenKeyExists()
    {
        var result = await _gateway.GetEventTemplateByKeyAsync("event-combat-shadow-v1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("event-combat-shadow-v1");
        result.Value.Type.Should().Be("Combat");
        result.Value.DefaultOutcomeKind.Should().Be("CombatStarted");
    }

    [Fact]
    public async Task GetEventTemplateByKeyAsync_ShouldReturnFailure_WhenKeyDoesNotExist()
    {
        var result = await _gateway.GetEventTemplateByKeyAsync("event-missing-v1");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.event_template_not_found");
    }

    [Fact]
    public async Task GetPalaceLawDefinitionByKeyAsync_ShouldReturnPalaceLaw_WhenKeyExists()
    {
        var result = await _gateway.GetPalaceLawDefinitionByKeyAsync("law-silence-v1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("law-silence-v1");
        result.Value.ImpactDomains.Should().Contain("Generation");
        result.Value.ImpactDomains.Should().Contain("Events");
        result.Value.ImpactDomains.Should().Contain("Narrative");
    }

    [Fact]
    public async Task GetPalaceLawDefinitionByKeyAsync_ShouldReturnFailure_WhenKeyDoesNotExist()
    {
        var result = await _gateway.GetPalaceLawDefinitionByKeyAsync("law-missing-v1");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.palace_law_definition_not_found");
    }

    // ── Enemy Definitions ──────────────────────────────────────────────

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnSeededEnemy()
    {
        var enemy = await _gateway.GetEnemyDefinitionByKeyAsync(
            "enemy.threshold.doubt-fragment");

        enemy.Should().NotBeNull();
        enemy!.Key.Should().Be("enemy.threshold.doubt-fragment");
        enemy.DisplayName.Should().Be("Fragment de Doute");
        enemy.Archetype.Should().Be("Fragile");
        enemy.CompatibleRoomTypes.Should().Contain("Threshold");
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnNull_WhenUnknown()
    {
        var enemy = await _gateway.GetEnemyDefinitionByKeyAsync("unknown");

        enemy.Should().BeNull();
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnNull_WhenKeyIsWhitespace()
    {
        var enemy = await _gateway.GetEnemyDefinitionByKeyAsync("   ");

        enemy.Should().BeNull();
    }

    [Fact]
    public async Task ListEnemyDefinitionsByRoomTypeAsync_ShouldReturnSeededEnemies()
    {
        var enemies = await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Silence");

        enemies.Should().NotBeEmpty();
        enemies.Select(e => e.Key).Should()
            .Contain("enemy.silence.mute-witness")
            .And.Contain("enemy.silence.absent-voice");
    }

    [Fact]
    public async Task ListEnemyDefinitionsByRoomTypeAsync_ShouldReturnEmpty_WhenUnknown()
    {
        var enemies = await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Unknown");

        enemies.Should().BeEmpty();
    }

    [Fact]
    public async Task ListEnemyDefinitionsByRoomTypeAsync_ShouldReturnEmpty_WhenRoomTypeIsWhitespace()
    {
        var enemies = await _gateway.ListEnemyDefinitionsByRoomTypeAsync("   ");

        enemies.Should().BeEmpty();
    }

    [Fact]
    public async Task ListCompatibleEnemyDefinitionsAsync_ShouldReturnSeededEnemies_ForKnownRoomTypeAndRiskLevel()
    {
        var enemies = await _gateway.ListCompatibleEnemyDefinitionsAsync("Threshold", 1);

        enemies.Should().NotBeEmpty();
        enemies.Should().AllSatisfy(e =>
        {
            e.MinRiskLevel.Should().BeLessThanOrEqualTo(1);
            e.MaxRiskLevel.Should().BeGreaterThanOrEqualTo(1);
        });
    }

    [Fact]
    public async Task ListCompatibleEnemyDefinitionsAsync_ShouldFilterByRiskLevel()
    {
        var enemies = await _gateway.ListCompatibleEnemyDefinitionsAsync("Rupture", 2);

        enemies.Should().NotBeEmpty();
        enemies.Should().OnlyContain(e =>
            e.MinRiskLevel <= 2 && 2 <= e.MaxRiskLevel);
    }

    [Fact]
    public async Task ListCompatibleEnemyDefinitionsAsync_ShouldReturnEmpty_WhenRoomTypeIsWhitespace()
    {
        var enemies = await _gateway.ListCompatibleEnemyDefinitionsAsync("   ", 3);

        enemies.Should().BeEmpty();
    }

    [Fact]
    public async Task InMemorySeeds_ShouldContainAtLeastOneEnemyPerRoomType()
    {
        var roomTypes = new[] { "Threshold", "Forest", "Rupture", "Silence", "Memory", "Antechamber", "Final" };

        foreach (var roomType in roomTypes)
        {
            var enemies = await _gateway.ListEnemyDefinitionsByRoomTypeAsync(roomType);
            enemies.Should().NotBeEmpty(
                $"because {roomType} should have at least one enemy definition");
        }
    }

    [Fact]
    public async Task InMemorySeeds_ShouldContainFinalEnemies()
    {
        var enemies = await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Final");

        enemies.Select(e => e.Key).Should()
            .Contain("enemy.final.silent-double")
            .And.Contain("enemy.final.last-echo");
    }

    [Fact]
    public async Task InMemorySeeds_ShouldContainAllFourteenEnemies()
    {
        var all = await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Threshold");
        all = all.Concat(await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Forest"))
            .Concat(await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Rupture"))
            .Concat(await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Silence"))
            .Concat(await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Memory"))
            .Concat(await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Antechamber"))
            .Concat(await _gateway.ListEnemyDefinitionsByRoomTypeAsync("Final"))
            .ToArray();

        all.Select(e => e.Key).Distinct().Should().HaveCount(14);
    }
}