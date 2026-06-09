using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;
using Moq;

namespace Leds.GameEngine.UnitTests.Combats.EncounterDrafts;

public sealed class CombatEncounterDraftGeneratorTests
{
    private static readonly CatalogEnemyDefinition FragmentDoute = new(
        Key: "enemy.threshold.doubt-fragment",
        DisplayName: "Fragment de Doute",
        Description: "Un éclat de silence.",
        Archetype: "Fragile",
        CompatibleRoomTypes: new[] { "Threshold" },
        BaseDifficulty: 1,
        MinRiskLevel: 1,
        MaxRiskLevel: 2,
        Tags: new[] { "threshold", "fragile" },
        SkillKeys: new[] { "skill.basic.strike" });

    private static readonly CatalogEnemyDefinition ResistanceInterieure = new(
        Key: "enemy.threshold.inner-resistance",
        DisplayName: "Résistance Intérieure",
        Description: "La première défense.",
        Archetype: "Guard",
        CompatibleRoomTypes: new[] { "Threshold" },
        BaseDifficulty: 2,
        MinRiskLevel: 2,
        MaxRiskLevel: 3,
        Tags: new[] { "threshold", "guard" },
        SkillKeys: new[] { "skill.basic.strike", "skill.basic.shield" });

    private static readonly CatalogEnemyDefinition SilentDouble = new(
        Key: "enemy.final.silent-double",
        DisplayName: "Double Silencieux",
        Description: "Votre propre silence.",
        Archetype: "Elite",
        CompatibleRoomTypes: new[] { "Final" },
        BaseDifficulty: 8,
        MinRiskLevel: 4,
        MaxRiskLevel: 5,
        Tags: new[] { "final", "elite", "mirror" },
        SkillKeys: new[] { "skill.basic.strike", "skill.basic.disable" });

    private static readonly CombatEncounterDraftContext DefaultContext = new(
        RunId: Guid.NewGuid(),
        RoomId: Guid.NewGuid(),
        NodeId: Guid.NewGuid(),
        RoomType: "Threshold",
        RoomIndex: 0,
        RiskLevel: 2,
        EncounterType: "Combat",
        EnemyCount: 1);

    [Fact]
    public async Task GenerateAsync_ShouldUseCompatibleEnemiesFromCatalogGateway()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Should().NotBeNull();
        draft.Enemies.Should().NotBeEmpty();
        draft.Enemies.Single().EnemyKey.Should().Be("enemy.threshold.doubt-fragment");
    }

    [Fact]
    public async Task GenerateAsync_ShouldFilterByRoomTypeAndRiskLevel_ThroughGateway()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        await generator.GenerateAsync(DefaultContext);

        gateway.Verify(g => g.ListCompatibleEnemyDefinitionsAsync(
            "Threshold", 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreateOneEnemy_WhenRiskLevelIsLow()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute, ResistanceInterieure });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var context = DefaultContext with { RiskLevel = 1, EnemyCount = 1 };
        var draft = await generator.GenerateAsync(context);

        draft.Enemies.Should().ContainSingle();
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreateTwoEnemies_WhenRiskLevelIsHighAndEnoughEnemiesExist()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute, ResistanceInterieure });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var context = DefaultContext with { RiskLevel = 4, EnemyCount = 2 };
        var draft = await generator.GenerateAsync(context);

        draft.Enemies.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreateOnlyAvailableEnemies_WhenNotEnoughCompatibleEnemiesExist()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var context = DefaultContext with { RiskLevel = 3, EnemyCount = 2 };
        var draft = await generator.GenerateAsync(context);

        draft.Enemies.Should().ContainSingle();
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrow_WhenNoCompatibleEnemiesExist()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CatalogEnemyDefinition>());

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var context = DefaultContext with { RiskLevel = 1, EnemyCount = 1 };
        var act = () => generator.GenerateAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No compatible enemy definitions found*");
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreatePlayerAlly()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Allies.Should().NotBeEmpty();
        draft.Allies.Should().Contain(a => a.AllyKey == "player.self");
        draft.Allies.Should().Contain(a => a.Role == "Protagonist");
    }

    [Fact]
    public async Task GenerateAsync_ShouldBeDeterministic_ForSameInput()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ResistanceInterieure, FragmentDoute });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var context = DefaultContext with { RiskLevel = 3, EnemyCount = 2 };
        var draft1 = await generator.GenerateAsync(context);
        var draft2 = await generator.GenerateAsync(context);

        draft1.Enemies.Select(e => e.EnemyKey)
            .Should().Equal(draft2.Enemies.Select(e => e.EnemyKey));
    }

    [Fact]
    public async Task GenerateAsync_ShouldPreserveEnemySkillKeys()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Enemies.Single().SkillKeys.Should().BeEquivalentTo("skill.basic.strike");
    }

    [Fact]
    public async Task GenerateAsync_ShouldPreserveEnemyTags()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Enemies.Single().Tags.Should().BeEquivalentTo("threshold", "fragile");
    }

    [Fact]
    public async Task GenerateAsync_ShouldSelectEliteWithTag_WhenEncounterTypeIsElite()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Final", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { SilentDouble });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var context = DefaultContext with
        {
            RoomType = "Final",
            RiskLevel = 4,
            EncounterType = "Elite",
            EnemyCount = 1
        };
        var draft = await generator.GenerateAsync(context);

        draft.Enemies.Should().ContainSingle();
        draft.Enemies.Single().EnemyKey.Should().Be("enemy.final.silent-double");
    }

    [Fact]
    public async Task GenerateAsync_ShouldSelectHardestEnemy_WhenEncounterTypeIsRare()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute, ResistanceInterieure });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var context = DefaultContext with
        {
            RiskLevel = 3,
            EncounterType = "Rare",
            EnemyCount = 1
        };
        var draft = await generator.GenerateAsync(context);

        draft.Enemies.Should().ContainSingle();
        draft.Enemies.Single().EnemyKey.Should().Be("enemy.threshold.inner-resistance");
    }

    [Fact]
    public async Task GenerateAsync_ShouldPopulateDraftMetadata()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync("Threshold", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { FragmentDoute });

        var generator = new CombatEncounterDraftGenerator(gateway.Object);

        var ctx = DefaultContext;
        var draft = await generator.GenerateAsync(ctx);

        draft.RunId.Should().Be(ctx.RunId);
        draft.RoomId.Should().Be(ctx.RoomId);
        draft.NodeId.Should().Be(ctx.NodeId);
        draft.RoomType.Should().Be("Threshold");
        draft.RoomIndex.Should().Be(ctx.RoomIndex);
        draft.RiskLevel.Should().Be(ctx.RiskLevel);
        draft.EncounterType.Should().Be("Combat");
    }
}
