using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.EncounterComposition;
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

    private static readonly CatalogSkillDefinition SkillStrike = new(
        Key: "skill.basic.strike",
        DisplayName: "Frappe",
        Description: "Une attaque de base.",
        SkillType: "Damage",
        TargetingType: "SingleEnemy",
        EffectType: "Damage",
        ManaCost: 5,
        ChargeCost: 0,
        BasePower: 10,
        Tags: ["basic", "damage"]);

    private static readonly CatalogSkillDefinition SkillShield = new(
        Key: "skill.basic.shield",
        DisplayName: "Bouclier",
        Description: "Un bouclier protecteur.",
        SkillType: "Defense",
        TargetingType: "Self",
        EffectType: "Buff",
        ManaCost: 4,
        ChargeCost: 0,
        BasePower: 0,
        Tags: ["basic", "shield"]);

    private static readonly CatalogSkillDefinition SkillDisable = new(
        Key: "skill.basic.disable",
        DisplayName: "Neutralisation",
        Description: "Désactive les capacités ennemies.",
        SkillType: "Debuff",
        TargetingType: "SingleEnemy",
        EffectType: "Debuff",
        ManaCost: 8,
        ChargeCost: 1,
        BasePower: 0,
        Tags: ["basic", "disable"]);

    private static readonly IReadOnlyCollection<CatalogSkillDefinition> AllTestSkills =
        [SkillStrike, SkillShield, SkillDisable];

    private static readonly CombatEncounterDraftContext DefaultContext = new(
        RunId: Guid.NewGuid(),
        RoomId: Guid.NewGuid(),
        NodeId: Guid.NewGuid(),
        RoomType: "Threshold",
        RoomIndex: 0,
        RiskLevel: 2,
        EncounterType: "Combat",
        EnemyCount: 1);

    private static Mock<ICatalogContentGateway> CreateGatewayWithSkills(
        CatalogEnemyDefinition[] enemies)
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enemies);

        gateway
            .Setup(g => g.ListSkillDefinitionsByKeysAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<string> keys, CancellationToken _) =>
                AllTestSkills.Where(s =>
                    keys.Any(k => string.Equals(k, s.Key, StringComparison.OrdinalIgnoreCase)))
                    .ToArray());

        return gateway;
    }

    private static Mock<IEncounterCompositionPolicy> CreatePolicyThatReturns(
        CatalogEnemyDefinition[] enemies)
    {
        var policy = new Mock<IEncounterCompositionPolicy>();
        policy
            .Setup(p => p.Compose(It.IsAny<EncounterCompositionContext>()))
            .Returns(new EncounterCompositionResult(
                DifficultyBudget: 5,
                EnemyCount: enemies.Length,
                SelectedEnemies: enemies));
        return policy;
    }

    private static CombatEncounterDraftGenerator CreateGenerator(
        Mock<ICatalogContentGateway> gateway,
        Mock<IEncounterCompositionPolicy>? policy = null)
    {
        policy ??= CreatePolicyThatReturns([FragmentDoute]);
        return new CombatEncounterDraftGenerator(gateway.Object, policy.Object);
    }

    [Fact]
    public async Task GenerateAsync_ShouldUseCompatibleEnemiesFromCatalogGateway()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var generator = CreateGenerator(gateway);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Should().NotBeNull();
        draft.Enemies.Should().NotBeEmpty();
        draft.Enemies.Single().EnemyKey.Should().Be("enemy.threshold.doubt-fragment");
    }

    [Fact]
    public async Task GenerateAsync_ShouldFilterByRoomTypeAndRiskLevel_ThroughGateway()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var generator = CreateGenerator(gateway);

        await generator.GenerateAsync(DefaultContext);

        gateway.Verify(g => g.ListCompatibleEnemyDefinitionsAsync(
            "Threshold", 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreatePlayerAlly()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var generator = CreateGenerator(gateway);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Allies.Should().NotBeEmpty();
        draft.Allies.Should().Contain(a => a.AllyKey == "player.self");
        draft.Allies.Should().Contain(a => a.Role == "Protagonist");
    }

    [Fact]
    public async Task GenerateAsync_ShouldPreserveEnemySkillKeys()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var generator = CreateGenerator(gateway);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Enemies.Single().SkillKeys.Should().BeEquivalentTo("skill.basic.strike");
    }

    [Fact]
    public async Task GenerateAsync_ShouldPreserveEnemyTags()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var generator = CreateGenerator(gateway);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Enemies.Single().Tags.Should().BeEquivalentTo("threshold", "fragile");
    }

    [Fact]
    public async Task GenerateAsync_ShouldPopulateDraftMetadata()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var generator = CreateGenerator(gateway);

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

    [Fact]
    public async Task GenerateAsync_ShouldResolveEnemySkillDefinitions()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var generator = CreateGenerator(gateway);

        var draft = await generator.GenerateAsync(DefaultContext);

        var enemy = draft.Enemies.Single();
        enemy.Skills.Should().NotBeEmpty();
        enemy.Skills.Should().Contain(s => s.Key == "skill.basic.strike");
        enemy.Skills.Single().DisplayName.Should().Be("Frappe");
        enemy.Skills.Single().ManaCost.Should().Be(5);
    }

    [Fact]
    public async Task GenerateAsync_ShouldBatchLoadSkillDefinitions()
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ResistanceInterieure });

        gateway
            .Setup(g => g.ListSkillDefinitionsByKeysAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([SkillStrike, SkillShield]);

        var generator = CreateGenerator(gateway);

        await generator.GenerateAsync(DefaultContext);

        gateway.Verify(g => g.ListSkillDefinitionsByKeysAsync(
            It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ShouldAttachSkillsToEachEnemy()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute, ResistanceInterieure]);
        var policy = CreatePolicyThatReturns([FragmentDoute, ResistanceInterieure]);
        var generator = CreateGenerator(gateway, policy);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Enemies.Should().HaveCount(2);
        draft.Enemies.Should().AllSatisfy(e => e.Skills.Should().NotBeEmpty());
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrow_WhenEnemyReferencesMissingSkillDefinition()
    {
        var unknownSkillEnemy = FragmentDoute with
        {
            SkillKeys = ["skill.unknown.missing"]
        };

        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { unknownSkillEnemy });

        gateway
            .Setup(g => g.ListSkillDefinitionsByKeysAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var policy = CreatePolicyThatReturns([unknownSkillEnemy]);
        var generator = CreateGenerator(gateway, policy);

        var act = () => generator.GenerateAsync(DefaultContext);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Missing skill definitions for keys: skill.unknown.missing*");
    }

    [Fact]
    public async Task GenerateAsync_ShouldNotExecuteSkills()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var generator = CreateGenerator(gateway);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Enemies.Single().Skills.Should().NotBeEmpty();
        draft.Enemies.Single().Skills.Single().ManaCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateAsync_ShouldBeDeterministic_ForSameInput()
    {
        var gateway = CreateGatewayWithSkills([ResistanceInterieure, FragmentDoute]);
        var policy = CreatePolicyThatReturns([FragmentDoute, ResistanceInterieure]);
        var generator = CreateGenerator(gateway, policy);

        var draft1 = await generator.GenerateAsync(DefaultContext);
        var draft2 = await generator.GenerateAsync(DefaultContext);

        draft1.Enemies.Select(e => e.EnemyKey)
            .Should().Equal(draft2.Enemies.Select(e => e.EnemyKey));
    }

    [Fact]
    public async Task GenerateAsync_ShouldUseEnemiesSelectedByCompositionPolicy()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute, ResistanceInterieure]);
        var policy = new Mock<IEncounterCompositionPolicy>();
        policy
            .Setup(p => p.Compose(It.IsAny<EncounterCompositionContext>()))
            .Returns(new EncounterCompositionResult(
                DifficultyBudget: 3,
                EnemyCount: 2,
                SelectedEnemies: new[] { FragmentDoute, ResistanceInterieure }));

        var generator = new CombatEncounterDraftGenerator(gateway.Object, policy.Object);
        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Enemies.Should().HaveCount(2);
        draft.Enemies.Should().Contain(e => e.EnemyKey == FragmentDoute.Key);
        draft.Enemies.Should().Contain(e => e.EnemyKey == ResistanceInterieure.Key);
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrow_WhenCompositionPolicyThrows()
    {
        var gateway = CreateGatewayWithSkills([FragmentDoute]);
        var policy = new Mock<IEncounterCompositionPolicy>();
        policy
            .Setup(p => p.Compose(It.IsAny<EncounterCompositionContext>()))
            .Throws(new InvalidOperationException("Policy failure"));

        var generator = new CombatEncounterDraftGenerator(gateway.Object, policy.Object);
        var act = () => generator.GenerateAsync(DefaultContext);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Policy failure");
    }
}
