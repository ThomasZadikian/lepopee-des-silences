using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Combats.EncounterComposition;

namespace Leds.GameEngine.UnitTests.Combats.EncounterComposition;

public sealed class EncounterCompositionPolicyTests
{
    private static readonly CatalogEnemyDefinition FragileEnemy = new(
        Key: "enemy.fragile.v1",
        DisplayName: "Fragile",
        Description: "",
        Archetype: "Fragile",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 1,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: ["fragile"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly CatalogEnemyDefinition SupportEnemy = new(
        Key: "enemy.support.v1",
        DisplayName: "Support",
        Description: "",
        Archetype: "Support",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 2,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: ["support"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly CatalogEnemyDefinition SkirmisherEnemy = new(
        Key: "enemy.skirmisher.v1",
        DisplayName: "Skirmisher",
        Description: "",
        Archetype: "Skirmisher",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 3,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: ["skirmisher"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly CatalogEnemyDefinition GuardEnemy = new(
        Key: "enemy.guard.v1",
        DisplayName: "Guard",
        Description: "",
        Archetype: "Guard",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 4,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: ["guard"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly CatalogEnemyDefinition DisruptorEnemy = new(
        Key: "enemy.disruptor.v1",
        DisplayName: "Disruptor",
        Description: "",
        Archetype: "Disruptor",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 5,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: ["disruptor"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly CatalogEnemyDefinition BruiserEnemy = new(
        Key: "enemy.bruiser.v1",
        DisplayName: "Bruiser",
        Description: "",
        Archetype: "Bruiser",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 6,
        MinRiskLevel: 1,
        MaxRiskLevel: 5,
        Tags: ["bruiser"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly CatalogEnemyDefinition EliteEnemy = new(
        Key: "enemy.elite.v1",
        DisplayName: "Elite",
        Description: "",
        Archetype: "Elite",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 10,
        MinRiskLevel: 3,
        MaxRiskLevel: 5,
        Tags: ["elite"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly CatalogEnemyDefinition TaggedEliteEnemy = new(
        Key: "enemy.tag-elite.v1",
        DisplayName: "Tagged Elite",
        Description: "",
        Archetype: "Bruiser",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 12,
        MinRiskLevel: 3,
        MaxRiskLevel: 5,
        Tags: ["elite"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly CatalogEnemyDefinition HighDifficultyEnemy = new(
        Key: "enemy.high-difficulty.v1",
        DisplayName: "High Difficulty",
        Description: "",
        Archetype: "Bruiser",
        CompatibleRoomTypes: ["Any"],
        BaseDifficulty: 15,
        MinRiskLevel: 4,
        MaxRiskLevel: 5,
        Tags: ["boss"],
        SkillKeys: ["skill.basic.strike"]);

    private static readonly EncounterCompositionPolicy Policy = new();

    private static EncounterCompositionContext CreateContext(
        int riskLevel,
        string encounterType = "Combat",
        int roomIndex = 0,
        IReadOnlyCollection<CatalogEnemyDefinition>? enemies = null,
        PalaceRoomState palaceRoomState = PalaceRoomState.Neutral,
        string? roomKey = null)
    {
        return new EncounterCompositionContext(
            RoomType: "Threshold",
            RoomIndex: roomIndex,
            RiskLevel: riskLevel,
            EncounterType: encounterType,
            AvailableEnemies: enemies ?? new[] { FragileEnemy, SupportEnemy, GuardEnemy },
            PalaceRoomState: palaceRoomState,
            RoomKey: roomKey);
    }

    // --- Validation ---

    [Fact]
    public void Compose_ShouldThrow_WhenAvailableEnemiesIsEmpty()
    {
        var context = CreateContext(riskLevel: 2, enemies: []);

        var act = () => Policy.Compose(context);

        act.Should().Throw<DomainException>()
            .WithMessage("*No compatible enemy definitions*");
    }

    [Fact]
    public void Compose_ShouldThrow_WhenRiskLevelIsBelowOne()
    {
        var context = CreateContext(riskLevel: 0);

        var act = () => Policy.Compose(context);

        act.Should().Throw<DomainException>()
            .WithMessage("*Invalid risk level*");
    }

    [Fact]
    public void Compose_ShouldThrow_WhenRiskLevelIsAboveFive()
    {
        var context = CreateContext(riskLevel: 6);

        var act = () => Policy.Compose(context);

        act.Should().Throw<DomainException>()
            .WithMessage("*Invalid risk level*");
    }

    [Fact]
    public void Compose_ShouldThrow_WhenEncounterTypeIsUnknown()
    {
        var context = CreateContext(riskLevel: 2, encounterType: "Unknown");

        var act = () => Policy.Compose(context);

        act.Should().Throw<DomainException>()
            .WithMessage("*Unknown encounter type*");
    }

    [Fact]
    public void Compose_ShouldThrow_WhenNoEnemiesFitRiskLevel()
    {
        var enemies = new[] { HighDifficultyEnemy };
        var context = CreateContext(riskLevel: 1, enemies: enemies);

        var act = () => Policy.Compose(context);

        act.Should().Throw<DomainException>()
            .WithMessage("*No compatible enemy definitions*");
    }

    // --- BoundRoomKeys (Bestiaire) ---

    [Fact]
    public void Compose_ShouldExcludeBoundEnemy_WhenRoomKeyDoesNotMatch()
    {
        var boundEnemy = FragileEnemy with { Key = "enemy.veilleur-tapis", BoundRoomKeys = ["room.halldentree"] };
        var context = CreateContext(riskLevel: 2, enemies: [boundEnemy, SupportEnemy], roomKey: "room.couloirs");

        var result = Policy.Compose(context);

        result.SelectedEnemies.Should().NotContain(e => e.Key == "enemy.veilleur-tapis");
    }

    [Fact]
    public void Compose_ShouldIncludeBoundEnemy_WhenRoomKeyMatches()
    {
        var boundEnemy = FragileEnemy with { Key = "enemy.veilleur-tapis", BoundRoomKeys = ["room.halldentree"] };
        var context = CreateContext(riskLevel: 2, enemies: [boundEnemy], roomKey: "room.halldentree");

        var result = Policy.Compose(context);

        result.SelectedEnemies.Should().Contain(e => e.Key == "enemy.veilleur-tapis");
    }

    [Fact]
    public void Compose_ShouldIncludeUnboundEnemy_RegardlessOfRoomKey()
    {
        var context = CreateContext(riskLevel: 2, enemies: [FragileEnemy], roomKey: "room.some-other-room");

        var result = Policy.Compose(context);

        result.SelectedEnemies.Should().Contain(e => e.Key == FragileEnemy.Key,
            because: "an enemy with no BoundRoomKeys is unrestricted, mirroring NpcEncounterSelector's NPC behavior.");
    }

    [Fact]
    public void Compose_ShouldExcludeBoundEnemy_WhenRoomKeyIsNull()
    {
        var boundEnemy = FragileEnemy with { Key = "enemy.veilleur-tapis", BoundRoomKeys = ["room.halldentree"] };
        var context = CreateContext(riskLevel: 2, enemies: [boundEnemy, SupportEnemy], roomKey: null);

        var result = Policy.Compose(context);

        result.SelectedEnemies.Should().NotContain(e => e.Key == "enemy.veilleur-tapis");
    }

    // --- Budget ---

    [Fact]
    public void Compose_ShouldUseBudgetTwo_ForRiskLevelOneCombat()
    {
        var context = CreateContext(riskLevel: 1);

        var result = Policy.Compose(context);

        result.DifficultyBudget.Should().Be(2);
    }

    [Fact]
    public void Compose_ShouldUseBudgetThree_ForRiskLevelTwoCombat()
    {
        var context = CreateContext(riskLevel: 2);

        var result = Policy.Compose(context);

        result.DifficultyBudget.Should().Be(3);
    }

    [Fact]
    public void Compose_ShouldUseBudgetFour_ForRiskLevelThreeCombat()
    {
        var context = CreateContext(riskLevel: 3);

        var result = Policy.Compose(context);

        result.DifficultyBudget.Should().Be(4);
    }

    [Fact]
    public void Compose_ShouldUseBudgetFive_ForRiskLevelFourCombat()
    {
        var context = CreateContext(riskLevel: 4);

        var result = Policy.Compose(context);

        result.DifficultyBudget.Should().Be(5);
    }

    [Fact]
    public void Compose_ShouldUseBudgetSeven_ForRiskLevelFiveCombat()
    {
        var context = CreateContext(riskLevel: 5);

        var result = Policy.Compose(context);

        result.DifficultyBudget.Should().Be(7);
    }

    [Fact]
    public void Compose_ShouldIncreaseBudget_ForElite()
    {
        var combatContext = CreateContext(riskLevel: 3, encounterType: "Combat");
        var eliteContext = CreateContext(riskLevel: 3, encounterType: "Elite", enemies: [EliteEnemy]);

        var combatResult = Policy.Compose(combatContext);
        var eliteResult = Policy.Compose(eliteContext);

        eliteResult.DifficultyBudget.Should().Be(combatResult.DifficultyBudget + 2);
    }

    [Fact]
    public void Compose_ShouldIncreaseBudget_ForRare()
    {
        var combatContext = CreateContext(riskLevel: 3, encounterType: "Combat");
        var rareContext = CreateContext(riskLevel: 3, encounterType: "Rare");

        var combatResult = Policy.Compose(combatContext);
        var rareResult = Policy.Compose(rareContext);

        rareResult.DifficultyBudget.Should().Be(combatResult.DifficultyBudget + 1);
    }

    [Fact]
    public void Compose_ShouldIncreaseBudget_ForDeepRoom()
    {
        var shallow = CreateContext(riskLevel: 3, roomIndex: 0);
        var deep = CreateContext(riskLevel: 3, roomIndex: 4);

        var shallowResult = Policy.Compose(shallow);
        var deepResult = Policy.Compose(deep);

        deepResult.DifficultyBudget.Should().Be(shallowResult.DifficultyBudget + 1);
    }

    [Fact]
    public void Compose_ShouldIncreaseBudget_ForVeryDeepRoom()
    {
        var shallow = CreateContext(riskLevel: 3, roomIndex: 0);
        var veryDeep = CreateContext(riskLevel: 3, roomIndex: 7);

        var shallowResult = Policy.Compose(shallow);
        var deepResult = Policy.Compose(veryDeep);

        deepResult.DifficultyBudget.Should().Be(shallowResult.DifficultyBudget + 2);
    }

    // --- Enemy count ---

    [Fact]
    public void Compose_ShouldSelectAtLeastOneEnemy_ForCombat()
    {
        var context = CreateContext(riskLevel: 3);

        var result = Policy.Compose(context);

        result.EnemyCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Compose_ShouldSelectAtMostThreeEnemies_ForCombat()
    {
        var context = CreateContext(riskLevel: 5);

        var result = Policy.Compose(context);

        result.EnemyCount.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public void Compose_ShouldSelectAtMostTwoEnemies_ForElite()
    {
        var context = CreateContext(riskLevel: 5, encounterType: "Elite", enemies: [EliteEnemy, GuardEnemy]);

        var result = Policy.Compose(context);

        result.EnemyCount.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public void Compose_ShouldSelectAtMostTwoEnemies_ForRare()
    {
        var context = CreateContext(riskLevel: 5, encounterType: "Rare");

        var result = Policy.Compose(context);

        result.EnemyCount.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public void Compose_ShouldSelectOneEnemy_ForRoomBoss()
    {
        var context = CreateContext(riskLevel: 5, encounterType: "RoomBoss", enemies: [HighDifficultyEnemy]);

        var result = Policy.Compose(context);

        result.EnemyCount.Should().Be(1);
    }

    [Fact]
    public void Compose_ShouldSelectOneEnemy_ForFinalBoss()
    {
        var context = CreateContext(riskLevel: 5, encounterType: "FinalBoss", enemies: [HighDifficultyEnemy]);

        var result = Policy.Compose(context);

        result.EnemyCount.Should().Be(1);
        result.SelectedEnemies.Should().Contain(e => e.Key == HighDifficultyEnemy.Key);
    }

    // --- Archetypes ---

    [Fact]
    public void Compose_ShouldPreferEliteEnemy_ForEliteEncounter_WhenAvailable()
    {
        var context = CreateContext(
            riskLevel: 4,
            encounterType: "Elite",
            enemies: [GuardEnemy, EliteEnemy]);

        var result = Policy.Compose(context);

        result.SelectedEnemies.Should().Contain(e => e.Key == EliteEnemy.Key);
    }

    [Fact]
    public void Compose_ShouldPreferTaggedEliteEnemy_ForEliteEncounter_WhenAvailable()
    {
        var context = CreateContext(
            riskLevel: 4,
            encounterType: "Elite",
            enemies: [GuardEnemy, TaggedEliteEnemy]);

        var result = Policy.Compose(context);

        result.SelectedEnemies.Should().Contain(e => e.Key == TaggedEliteEnemy.Key);
    }

    [Fact]
    public void Compose_ShouldPreferSupportOrDisruptor_ForRareEncounter_WhenAvailable()
    {
        var context = CreateContext(
            riskLevel: 3,
            encounterType: "Rare",
            enemies: [GuardEnemy, SupportEnemy]);

        var result = Policy.Compose(context);

        result.SelectedEnemies.Should().Contain(e => e.Key == SupportEnemy.Key);
    }

    [Fact]
    public void Compose_ShouldUseArchetypeCosts_ToStayWithinBudget()
    {
        var enemies = new[] { FragileEnemy, GuardEnemy };
        var context = CreateContext(riskLevel: 1, enemies: enemies);

        var result = Policy.Compose(context);

        var totalCost = result.SelectedEnemies
            .Sum(e => e.Archetype switch
            {
                "Fragile" => 1,
                "Guard" => 2,
                _ => 2
            });
        totalCost.Should().BeLessThanOrEqualTo(result.DifficultyBudget);
    }

    [Fact]
    public void Compose_ShouldFallbackToLowestCostEnemy_WhenNoCombinationFits()
    {
        var expensiveEnemy = FragileEnemy with { Archetype = "Bruiser", Key = "enemy.bruiser.v1", BaseDifficulty = 6 };
        var enemies = new[] { expensiveEnemy };
        var context = CreateContext(riskLevel: 1, encounterType: "Combat", enemies: enemies);

        var result = Policy.Compose(context);

        result.SelectedEnemies.Should().Contain(e => e.Key == expensiveEnemy.Key);
    }

    // --- Determinism ---

    [Fact]
    public void Compose_ShouldReturnSameResult_ForSameContext()
    {
        var context = CreateContext(riskLevel: 3, enemies: [SupportEnemy, GuardEnemy, FragileEnemy]);

        var result1 = Policy.Compose(context);
        var result2 = Policy.Compose(context);

        result1.SelectedEnemies.Select(e => e.Key)
            .Should().Equal(result2.SelectedEnemies.Select(e => e.Key));
        result1.DifficultyBudget.Should().Be(result2.DifficultyBudget);
        result1.EnemyCount.Should().Be(result2.EnemyCount);
    }

    [Fact]
    public void Compose_ShouldOrderSelectionsDeterministically_ByKeyAsTieBreaker()
    {
        var sameCostEnemies = new[]
        {
            FragileEnemy with { Key = "enemy.zzz.last" },
            FragileEnemy with { Key = "enemy.aaa.first" },
        };
        var context = CreateContext(riskLevel: 1, enemies: sameCostEnemies);

        var result = Policy.Compose(context);

        result.SelectedEnemies.First().Key.Should().Be("enemy.aaa.first");
    }

    // --- RoomProgression ---

    [Fact]
    public void Compose_ShouldIncreaseDifficultyBudget_WhenRoomIndexIsHigh()
    {
        var early = CreateContext(riskLevel: 3, roomIndex: 1);
        var mid = CreateContext(riskLevel: 3, roomIndex: 4);
        var late = CreateContext(riskLevel: 3, roomIndex: 7);

        var earlyResult = Policy.Compose(early);
        var midResult = Policy.Compose(mid);
        var lateResult = Policy.Compose(late);

        midResult.DifficultyBudget.Should().Be(earlyResult.DifficultyBudget + 1);
        lateResult.DifficultyBudget.Should().Be(earlyResult.DifficultyBudget + 2);
    }

    // --- PalaceRoomState archetype preference ---

    [Fact]
    public void Compose_WithSilent_ShouldPreferGuardOverSkirmisher()
    {
        var guard = GuardEnemy with { BaseDifficulty = 2 };
        var skirmisher = SkirmisherEnemy with { BaseDifficulty = 4 };
        var enemies = new[] { skirmisher, guard };

        var neutral = CreateContext(riskLevel: 4, enemies: enemies);
        var silent = CreateContext(riskLevel: 4, enemies: enemies, palaceRoomState: PalaceRoomState.Silent);

        var neutralResult = Policy.Compose(neutral);
        var silentResult = Policy.Compose(silent);

        neutralResult.SelectedEnemies.First().Archetype.Should().Be("Skirmisher");
        silentResult.SelectedEnemies.First().Archetype.Should().Be("Guard");
    }

    [Fact]
    public void Compose_WithSilent_ShouldSupportFallback_WhenNoPreferredArchetypeAvailable()
    {
        var enemies = new[] { SkirmisherEnemy, FragileEnemy };

        var context = CreateContext(riskLevel: 3, enemies: enemies, palaceRoomState: PalaceRoomState.Silent);

        var act = () => Policy.Compose(context);

        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithPainful_ShouldPreferDisruptorOverGuard()
    {
        var disruptor = DisruptorEnemy with { BaseDifficulty = 2 };
        var guard = GuardEnemy with { BaseDifficulty = 4 };
        var enemies = new[] { guard, disruptor };

        var neutral = CreateContext(riskLevel: 4, enemies: enemies);
        var painful = CreateContext(riskLevel: 4, enemies: enemies, palaceRoomState: PalaceRoomState.Painful);

        var neutralResult = Policy.Compose(neutral);
        var painfulResult = Policy.Compose(painful);

        neutralResult.SelectedEnemies.First().Archetype.Should().Be("Guard");
        painfulResult.SelectedEnemies.First().Archetype.Should().Be("Disruptor");
    }

    [Fact]
    public void Compose_WithEnraged_ShouldPreferSkirmisherOverGuard()
    {
        var skirmisher = SkirmisherEnemy with { BaseDifficulty = 2 };
        var guard = GuardEnemy with { BaseDifficulty = 4 };
        var enemies = new[] { guard, skirmisher };

        var neutral = CreateContext(riskLevel: 4, enemies: enemies);
        var enraged = CreateContext(riskLevel: 4, enemies: enemies, palaceRoomState: PalaceRoomState.Enraged);

        var neutralResult = Policy.Compose(neutral);
        var enragedResult = Policy.Compose(enraged);

        neutralResult.SelectedEnemies.First().Archetype.Should().Be("Guard");
        enragedResult.SelectedEnemies.First().Archetype.Should().Be("Skirmisher");
    }

    [Fact]
    public void Compose_WithViolent_ShouldPreferFragileOverSupport()
    {
        var fragile = FragileEnemy with { BaseDifficulty = 2 };
        var support = SupportEnemy with { BaseDifficulty = 4 };
        var enemies = new[] { support, fragile };

        var neutral = CreateContext(riskLevel: 4, enemies: enemies);
        var violent = CreateContext(riskLevel: 4, enemies: enemies, palaceRoomState: PalaceRoomState.Violent);

        var neutralResult = Policy.Compose(neutral);
        var violentResult = Policy.Compose(violent);

        neutralResult.SelectedEnemies.First().Archetype.Should().Be("Support");
        violentResult.SelectedEnemies.First().Archetype.Should().Be("Fragile");
    }

    [Fact]
    public void Compose_WithPalaceRoomState_ShouldPreserveDeterminism()
    {
        var enemies = new[] { GuardEnemy, SkirmisherEnemy, DisruptorEnemy };

        var context = CreateContext(riskLevel: 4, enemies: enemies, palaceRoomState: PalaceRoomState.Silent);

        var result1 = Policy.Compose(context);
        var result2 = Policy.Compose(context);

        result1.SelectedEnemies.Select(e => e.Key)
            .Should().Equal(result2.SelectedEnemies.Select(e => e.Key));
    }
}