using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;
using Moq;

namespace Leds.GameEngine.UnitTests.Combats.EncounterDrafts;

/// <summary>
/// Chantier 6 (diversifier la composition des ennemis) : plus l'effectif d'une rencontre
/// grandit, plus chaque ennemi individuel doit être affaibli — sans quoi relever le plafond
/// d'effectif (EncounterCompositionPolicy) rendrait les combats plus durs plutôt que plus
/// variés. Fixtures locales (avec <c>Registre</c> renseigné) plutôt que celles du fichier
/// voisin, dont le <c>Registre</c> manquant casse tout ce qui appelle <c>GenerateAsync</c>.
/// </summary>
public sealed class CombatEncounterDraftGeneratorGroupSizeTests
{
    private static CatalogEnemyDefinition MakeEnemy(string key, int attackPower) => new(
        Key: key,
        DisplayName: key,
        Description: "",
        Archetype: "Fragile",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 1,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: [],
        SkillKeys: [],
        Menace: 1,
        AttackPower: attackPower,
        Defense: attackPower,
        MagicAttack: attackPower,
        MagicDefense: attackPower,
        Registre: "Neutral");

    private static readonly CombatEncounterDraftContext DefaultContext = new(
        RunId: Guid.NewGuid(),
        RoomId: Guid.NewGuid(),
        NodeId: Guid.NewGuid(),
        RoomType: "Threshold",
        RoomIndex: 0,
        RiskLevel: 2,
        EncounterType: "Combat",
        EnemyCount: 1,
        PartyAllies:
        [
            new CombatEncounterDraftAlly(
                AllyKey: "player.self",
                DisplayName: "Hero",
                Role: "Protagonist",
                Tags: [],
                EmotionalRegister: "Neutral",
                IsProtagonist: true,
                CharacterInstanceId: Guid.NewGuid()),
        ]);

    private static CombatEncounterDraftGenerator CreateGenerator(CatalogEnemyDefinition[] enemies)
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enemies);
        gateway
            .Setup(g => g.ListSkillDefinitionsByKeysAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var policy = new Mock<IEncounterCompositionPolicy>();
        policy
            .Setup(p => p.Compose(It.IsAny<EncounterCompositionContext>()))
            .Returns(new EncounterCompositionResult(
                DifficultyBudget: 20,
                EnemyCount: enemies.Length,
                SelectedEnemies: enemies));

        var riskProfileResolver = new Mock<ICombatRiskProfileResolver>();
        riskProfileResolver
            .Setup(r => r.Resolve(It.IsAny<Leds.GameEngine.Domain.Nodes.NodeEventType>(), It.IsAny<int>()))
            .Returns(new CombatRiskProfile(CombatTier.Normal, RiskTier.Tendu, 1.0, 1.0, 1.0, 0));

        return new CombatEncounterDraftGenerator(gateway.Object, policy.Object, riskProfileResolver.Object);
    }

    [Fact]
    public async Task GenerateAsync_ShouldLeaveStatsUnchanged_ForASoloEnemy()
    {
        var enemy = MakeEnemy("enemy.solo", attackPower: 100);
        var generator = CreateGenerator([enemy]);

        var draft = await generator.GenerateAsync(DefaultContext);

        draft.Enemies.Single().AttackPower.Should().Be(100);
    }

    [Fact]
    public async Task GenerateAsync_ShouldWeakenEveryEnemy_WhenTwoAreDeployedTogether()
    {
        var enemies = new[]
        {
            MakeEnemy("enemy.pair.a", attackPower: 100),
            MakeEnemy("enemy.pair.b", attackPower: 100),
        };
        var generator = CreateGenerator(enemies);

        var draft = await generator.GenerateAsync(DefaultContext);

        // ceil(100 * 0.92) = 92 for both — no Elite/Rare preferred pick in a "Combat" encounter.
        draft.Enemies.Should().OnlyContain(e => e.AttackPower == 92);
    }

    [Fact]
    public async Task GenerateAsync_ShouldWeakenEnemiesFurther_AsTheSquadGrows()
    {
        var pairEnemies = new[]
        {
            MakeEnemy("enemy.pair.a", attackPower: 100),
            MakeEnemy("enemy.pair.b", attackPower: 100),
        };
        var quintetEnemies = Enumerable.Range(0, 5)
            .Select(i => MakeEnemy($"enemy.quintet.{i}", attackPower: 100))
            .ToArray();

        var pairDraft = await CreateGenerator(pairEnemies).GenerateAsync(DefaultContext);
        var quintetDraft = await CreateGenerator(quintetEnemies).GenerateAsync(DefaultContext);

        var pairAttack = pairDraft.Enemies.First().AttackPower;
        var quintetAttack = quintetDraft.Enemies.First().AttackPower;

        quintetAttack.Should().BeLessThan(pairAttack);
        // ceil(100 * 0.72) = 72 at five enemies.
        quintetAttack.Should().Be(72);
    }

    [Fact]
    public async Task GenerateAsync_ShouldClampTheGroupSizeMultiplier_BeyondTheEncounterCeiling()
    {
        var sevenEnemies = Enumerable.Range(0, 7)
            .Select(i => MakeEnemy($"enemy.septet.{i}", attackPower: 100))
            .ToArray();

        var draft = await CreateGenerator(sevenEnemies).GenerateAsync(DefaultContext);

        // ceil(100 * 0.62) = 62 — the weakest tier, at the encounter's hard ceiling of 7.
        draft.Enemies.Should().OnlyContain(e => e.AttackPower == 62);
    }
}
