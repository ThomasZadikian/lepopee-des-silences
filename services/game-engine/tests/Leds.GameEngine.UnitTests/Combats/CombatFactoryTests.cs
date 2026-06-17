using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatFactoryTests
{
    private static CombatEncounterDraft CreateDraft(
        int allyCount = 1,
        int enemyCount = 1,
        int enemyBaseDifficulty = 3,
        bool includeSkills = false,
        IReadOnlyCollection<CombatEncounterDraftSkill>? enemySkills = null)
    {
        var allies = Enumerable.Range(0, allyCount).Select(i =>
            new CombatEncounterDraftAlly(
                $"player.{i}",
                $"Hero{i}",
                "Fighter",
                Array.Empty<string>())).ToArray();

        var enemies = Enumerable.Range(0, enemyCount).Select(i =>
        {
            var skills = enemySkills ?? (includeSkills
                ? new[]
                {
                    new CombatEncounterDraftSkill(
                        "skill.basic.strike", "Frappe", "Attack.", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
                        Array.Empty<string>())
                }
                : Array.Empty<CombatEncounterDraftSkill>());

            return new CombatEncounterDraftEnemy(
                $"enemy.{i}",
                $"Enemy{i}",
                $"Description {i}",
                "Guard",
                enemyBaseDifficulty,
                1,
                5,
                Array.Empty<string>(),
                skills.Select(s => s.Key).ToArray(),
                skills);
        }).ToArray();

        return new CombatEncounterDraft(
            RunId: Guid.NewGuid(),
            RoomId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            RoomType: "Threshold",
            RoomIndex: 1,
            RiskLevel: 3,
            EncounterType: "Combat",
            Enemies: enemies,
            Allies: allies);
    }

    [Fact]
    public void CreateFromDraft_ShouldCreateCombat()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.Should().NotBeNull();
        combat.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateFromDraft_ShouldUseProvidedCombatId()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var combatId = CombatId.New();

        var combat = factory.CreateFromDraft(combatId, draft);

        combat.Id.Should().Be(combatId);
    }

    [Fact]
    public void CreateFromDraft_ShouldCreateAlliesFromDraft()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 2);

        var combat = factory.CreateFromDraft(draft);

        combat.Allies.Should().HaveCount(2);
        combat.Allies.Select(a => a.SourceKey).Should()
            .Contain("player.0").And.Contain("player.1");
    }

    [Fact]
    public void CreateFromDraft_ShouldCreateEnemiesFromDraft()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyCount: 2);

        var combat = factory.CreateFromDraft(draft);

        combat.Enemies.Should().HaveCount(2);
        combat.Enemies.Select(e => e.SourceKey).Should()
            .Contain("enemy.0").And.Contain("enemy.1");
    }

    [Fact]
    public void CreateFromDraft_ShouldMapEnemySkills()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var combat = factory.CreateFromDraft(draft);

        var enemy = combat.Enemies.Single();
        enemy.Skills.Should().HaveCount(1);
        enemy.Skills.Single().Key.Should().Be("skill.basic.strike");
        enemy.Skills.Single().DisplayName.Should().Be("Frappe");
    }

    [Fact]
    public void CreateFromDraft_ShouldNormalizeEnemyCurrentGuardSkill()
    {
        var factory = new CombatFactory();
        var guardSkill = new CombatEncounterDraftSkill(
            "skill.basic.guard", "Garde", "Guard.", "Defense", "Self", "AddCurrentGuard", 0, 0, 5,
            Array.Empty<string>());
        var draft = CreateDraft(enemySkills: [guardSkill]);

        var combat = factory.CreateFromDraft(draft);

        var skill = combat.Enemies.Single().Skills.Single();
        skill.EffectType.Should().Be("Guard");
        skill.BasePower.Should().Be(5);
    }

    [Fact]
    public void CreateFromDraft_ShouldInitializeEnemyVitalityFromBaseDifficulty()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyBaseDifficulty: 5);

        var combat = factory.CreateFromDraft(draft);

        var enemy = combat.Enemies.Single();
        enemy.MaxVitality.Should().Be(40 + 5 * 10);
        enemy.CurrentVitality.Should().Be(enemy.MaxVitality);
    }

    [Fact]
    public void CreateFromDraft_ShouldInitializePlayerVitality()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        var ally = combat.Allies.Single();
        ally.MaxVitality.Should().Be(100);
        ally.CurrentVitality.Should().Be(100);
    }

    [Fact]
    public void CreateFromDraft_ShouldGiveDefaultSkillsToAllies()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        var ally = combat.Allies.Single();
        ally.Skills.Select(s => s.Key).Should()
            .Contain("skill.basic.strike")
            .And.Contain("skill.basic.guard");
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyRainClimateStartingGuardBonus()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 2)]);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(5);
        ally.BaseGuard.Should().Be(5);
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyHeatwaveClimateEnemyPowerBonus()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var baseline = factory.CreateFromDraft(draft);
        var heatwave = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 3)]);

        var baselinePower = baseline.Enemies.Single().Skills.Single().BasePower;
        var heatwavePower = heatwave.Enemies.Single().Skills.Single().BasePower;
        heatwavePower.Should().BeGreaterThan(baselinePower);
    }

    [Fact]
    public void CreateFromDraft_ShouldSetCombatActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.Status.Should().Be(CombatStatus.Active);
    }

    [Fact]
    public void CreateFromDraft_ShouldSetTurnNumberToOne()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.TurnNumber.Should().Be(1);
    }

    [Fact]
    public void CreateFromDraft_ShouldSetFirstAllyAsActiveCombatant()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 2);

        var combat = factory.CreateFromDraft(draft);

        var firstAllyId = combat.Allies.First().Id;
        combat.ActiveCombatantId.Should().Be(firstAllyId);
    }

    [Fact]
    public void CreateFromDraft_ShouldBeDeterministic_ForSameDraft()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyCount: 2);

        var combat1 = factory.CreateFromDraft(draft);
        var combat2 = factory.CreateFromDraft(draft);

        combat1.Allies.Should().HaveSameCount(combat2.Allies);
        combat1.Enemies.Should().HaveSameCount(combat2.Enemies);
        combat1.TurnNumber.Should().Be(combat2.TurnNumber);
        combat1.Status.Should().Be(combat2.Status);
    }

    [Fact]
    public void CreateFromDraft_ShouldThrow_WhenDraftHasNoEnemy()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyCount: 0);

        var act = () => factory.CreateFromDraft(draft);

        act.Should().Throw<DomainException>().WithMessage("Combat requires at least one enemy.");
    }

    [Fact]
    public void CreateFromDraft_ShouldThrow_WhenDraftHasNoAlly()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 0);

        var act = () => factory.CreateFromDraft(draft);

        act.Should().Throw<DomainException>().WithMessage("Combat requires at least one ally.");
    }

    // -----------------------------------------------------------------------
    // StartingGuardBonus
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldApplyStartingGuardBonus_WhenModifierProvided()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var modifier = RunModifier.Create(
            RunModifierType.StartingGuardBonus,
            8,
            RunModifierDuration.UntilRunEnds,
            "RunItem",
            "item.consumable.guard-shard");

        var combat = factory.CreateFromDraft(draft, runModifiers: new[] { modifier });

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(8,
            because: "An unconsumed StartingGuardBonus modifier with value 8 should set ally guard to 8.");
    }

    [Fact]
    public void CreateFromDraft_ShouldSumMultipleStartingGuardBonusModifiers()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var modifiers = new[]
        {
            RunModifier.Create(RunModifierType.StartingGuardBonus, 8, RunModifierDuration.UntilRunEnds, "RunItem", "item.guard-shard.1"),
            RunModifier.Create(RunModifierType.StartingGuardBonus, 6, RunModifierDuration.UntilRunEnds, "RunItem", "item.guard-shard.2"),
        };

        var combat = factory.CreateFromDraft(draft, runModifiers: modifiers);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(14,
            because: "Two guard bonus modifiers (8 + 6) should stack to 14.");
    }

    [Fact]
    public void CreateFromDraft_ShouldNotApplyConsumedStartingGuardBonusModifiers()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var modifier = RunModifier.Create(
            RunModifierType.StartingGuardBonus,
            8,
            RunModifierDuration.UntilRunEnds,
            "RunItem",
            "item.consumable.guard-shard");
        modifier.Consume(DateTime.UtcNow);

        var combat = factory.CreateFromDraft(draft, runModifiers: new[] { modifier });

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(0,
            because: "A consumed StartingGuardBonus modifier must not contribute to starting guard.");
    }

    [Fact]
    public void CreateFromDraft_ShouldUseZeroGuard_WhenNoRunModifiersProvided()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(0,
            because: "Without any RunModifiers the starting guard defaults to 0.");
    }

    private static RunModifier CreateClimateModifier(Guid roomId, double value)
    {
        return RunModifier.Create(
            RunModifierType.RoomClimate,
            value,
            RunModifierDuration.UntilRoomEnds,
            "PalaceLaw",
            "law-climate-test",
            expiresAtRoomId: roomId);
    }
}
