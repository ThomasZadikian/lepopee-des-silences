using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Infrastructure.Combats.EncounterComposition;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class EncounterCompositionPolicyLawTests
{
    private static EncounterCompositionPolicy CreateSut() => new();

    private static IReadOnlyCollection<CatalogEnemyDefinition> SomeEnemies() =>
[
    new CatalogEnemyDefinition(
        Key: "enemy.fragile.v1",
        DisplayName: "Fragile",
        Description: "",
        Archetype: "Fragile",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 1,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: ["fragile"],
        SkillKeys: ["skill.basic.strike"],
        Menace: 1),

    new CatalogEnemyDefinition(
        Key: "enemy.skirmisher.v1",
        DisplayName: "Skirmisher",
        Description: "",
        Archetype: "Skirmisher",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 2,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: [],
        SkillKeys: ["skill.basic.strike"],
        Menace: 2),

    new CatalogEnemyDefinition(
        Key: "enemy.bruiser.v1",
        DisplayName: "Bruiser",
        Description: "",
        Archetype: "Bruiser",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 3,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: [],
        SkillKeys: ["skill.basic.strike"],
        Menace: 3),
];

    [Fact]
    public void Compose_ShouldIncreaseBudget_WhenGenerationLawIsActive()
    {
        var generationLaw = ActivePalaceLaw.Rehydrate(
            PalaceLawId.New(),
            key: "law-echo-v1",
            name: "Loi de l'Écho",
            version: "1.0.0",
            domains: [PalaceLawDomain.Generation]);

        var contextWithLaw = new EncounterCompositionContext(
            RoomType: "Forest",
            RoomIndex: 0,
            RiskLevel: 2,
            EncounterType: "Combat",
            AvailableEnemies: SomeEnemies(),
            ActivePalaceLaws: [generationLaw]);

        var contextWithoutLaw = new EncounterCompositionContext(
            RoomType: "Forest",
            RoomIndex: 0,
            RiskLevel: 2,
            EncounterType: "Combat",
            AvailableEnemies: SomeEnemies());

        var sut = CreateSut();
        var resultWith = sut.Compose(contextWithLaw);
        var resultWithout = sut.Compose(contextWithoutLaw);

        resultWith.DifficultyBudget.Should().Be(resultWithout.DifficultyBudget + 1);
    }

    [Fact]
    public void Compose_ShouldCapBudgetBonus_AtThreeGenerationLaws()
    {
        var laws = Enumerable.Range(1, 5)
            .Select(i => ActivePalaceLaw.Rehydrate(
                PalaceLawId.New(),
                key: $"law-gen-{i}",
                name: $"Loi {i}",
                version: "1.0.0",
                domains: [PalaceLawDomain.Generation]))
            .ToArray();

        var contextFiveLaws = new EncounterCompositionContext(
            RoomType: "Forest",
            RoomIndex: 0,
            RiskLevel: 2,
            EncounterType: "Combat",
            AvailableEnemies: SomeEnemies(),
            ActivePalaceLaws: laws);

        var threeOfLaws = laws.Take(3).ToArray();
        var contextThreeLaws = new EncounterCompositionContext(
            RoomType: "Forest",
            RoomIndex: 0,
            RiskLevel: 2,
            EncounterType: "Combat",
            AvailableEnemies: SomeEnemies(),
            ActivePalaceLaws: threeOfLaws);

        var sut = CreateSut();
        var resultFive = sut.Compose(contextFiveLaws);
        var resultThree = sut.Compose(contextThreeLaws);

        // Budget capped at +3 — 5 laws ne donnent pas plus que 3
        resultFive.DifficultyBudget.Should().Be(resultThree.DifficultyBudget);
    }

    [Fact]
    public void Compose_ShouldNotIncreaseBudget_WhenLawDoesNotTargetGeneration()
    {
        var combatLaw = ActivePalaceLaw.Rehydrate(
            PalaceLawId.New(),
            key: "law-combat-v1",
            name: "Loi du Combat",
            version: "1.0.0",
            domains: [PalaceLawDomain.Combat]);

        var contextWithNonGenerationLaw = new EncounterCompositionContext(
            RoomType: "Forest",
            RoomIndex: 0,
            RiskLevel: 2,
            EncounterType: "Combat",
            AvailableEnemies: SomeEnemies(),
            ActivePalaceLaws: [combatLaw]);

        var contextWithoutLaw = new EncounterCompositionContext(
            RoomType: "Forest",
            RoomIndex: 0,
            RiskLevel: 2,
            EncounterType: "Combat",
            AvailableEnemies: SomeEnemies());

        var sut = CreateSut();
        var resultWith = sut.Compose(contextWithNonGenerationLaw);
        var resultWithout = sut.Compose(contextWithoutLaw);

        resultWith.DifficultyBudget.Should().Be(resultWithout.DifficultyBudget);
    }

    [Fact]
    public void Compose_ShouldNotIncreaseBudget_WhenNoLawsAreActive()
    {
        var contextNoLaws = new EncounterCompositionContext(
            RoomType: "Forest",
            RoomIndex: 0,
            RiskLevel: 3,
            EncounterType: "Combat",
            AvailableEnemies: SomeEnemies());

        var sut = CreateSut();
        var result = sut.Compose(contextNoLaws);

        // Pas de régression : budget = valeur attendue sans lois
        result.DifficultyBudget.Should().Be(4); // RiskLevel 3 = base 4
    }
}
