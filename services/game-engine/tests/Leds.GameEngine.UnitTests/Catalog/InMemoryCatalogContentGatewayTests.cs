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
}