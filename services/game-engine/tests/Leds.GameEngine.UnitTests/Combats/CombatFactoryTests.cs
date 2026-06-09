using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatFactoryTests
{
    private static CombatEncounterDraft CreateDraft(
        int allyCount = 1,
        int enemyCount = 1,
        int enemyBaseDifficulty = 3,
        bool includeSkills = false)
    {
        var allies = Enumerable.Range(0, allyCount).Select(i =>
            new CombatEncounterDraftAlly(
                $"player.{i}",
                $"Hero{i}",
                "Fighter",
                Array.Empty<string>())).ToArray();

        var enemies = Enumerable.Range(0, enemyCount).Select(i =>
        {
            var skills = includeSkills
                ? new[]
                {
                    new CombatEncounterDraftSkill(
                        "skill.basic.strike", "Frappe", "Attack.", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
                        Array.Empty<string>())
                }
                : Array.Empty<CombatEncounterDraftSkill>();

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
}
