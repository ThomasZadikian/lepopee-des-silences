using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Common;

public sealed class CoverageClosureDomainTests
{
    [Fact]
    public void CombatAction_ShouldValidateEveryBasicAttackInvariant()
    {
        var actor = CombatantId.New();
        var target = CombatantId.New();

        CombatAction.BasicAttack(actor, target).ActionType.Should().Be(CombatActionType.BasicAttack);
        FluentActions.Invoking(() => CombatAction.BasicAttack(new CombatantId(Guid.Empty), target))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatAction.BasicAttack(actor, new CombatantId(Guid.Empty)))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatAction.BasicAttack(actor, actor))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void RoomBossProfile_ShouldValidateRequiredTextAndTrimValidValues()
    {
        FluentActions.Invoking(() => RoomBossProfile.Create(" ", "Boss", default, "Danger", "enemy"))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => RoomBossProfile.Create("boss", " ", default, "Danger", "enemy"))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => RoomBossProfile.Create("boss", "Boss", default, " ", "enemy"))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => RoomBossProfile.Create("boss", "Boss", default, "Danger", " "))
            .Should().Throw<DomainException>();

        var valid = RoomBossProfile.Create(" boss ", " Boss ", default, " Danger ", " enemy ");
        valid.BossId.Should().Be("boss");
        valid.Name.Should().Be("Boss");
        valid.DangerHint.Should().Be("Danger");
        valid.EnemyTemplateKey.Should().Be("enemy");
    }

    [Fact]
    public void CombatantSnapshot_ShouldValidateCreationAndDamageLifecycle()
    {
        var id = CombatantId.New();
        Action create = () => CombatantSnapshot.Create(id, "template", "Hero", CombatantSide.Player, 100, 100, 10, 5, 4);
        create.Should().NotThrow();

        FluentActions.Invoking(() => CombatantSnapshot.Create(new CombatantId(Guid.Empty), "template", "Hero", CombatantSide.Player, 100, 100, 10, 5, 4)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatantSnapshot.Create(id, " ", "Hero", CombatantSide.Player, 100, 100, 10, 5, 4)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatantSnapshot.Create(id, "template", " ", CombatantSide.Player, 100, 100, 10, 5, 4)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatantSnapshot.Create(id, "template", "Hero", CombatantSide.Player, 0, 0, 10, 5, 4)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatantSnapshot.Create(id, "template", "Hero", CombatantSide.Player, 100, -1, 10, 5, 4)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatantSnapshot.Create(id, "template", "Hero", CombatantSide.Player, 100, 101, 10, 5, 4)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatantSnapshot.Create(id, "template", "Hero", CombatantSide.Player, 100, 100, -1, 5, 4)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatantSnapshot.Create(id, "template", "Hero", CombatantSide.Player, 100, 100, 10, -1, 4)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => CombatantSnapshot.Create(id, "template", "Hero", CombatantSide.Player, 100, 100, 10, 5, -1)).Should().Throw<DomainException>();

        var snapshot = CombatantSnapshot.Create(" template ", " Hero ", CombatantSide.Player, 10, 2, 1, 3);
        snapshot.TemplateKey.Should().Be("template");
        snapshot.DisplayName.Should().Be("Hero");
        FluentActions.Invoking(() => snapshot.ReceiveDamage(0)).Should().Throw<DomainException>();
        snapshot.ReceiveDamage(50);
        snapshot.IsDefeated.Should().BeTrue();
        snapshot.CurrentHealth.Should().Be(0);
        FluentActions.Invoking(() => snapshot.ReceiveDamage(1)).Should().Throw<DomainException>();
    }

    [Fact]
    public void GridRoomLayoutTemplate_ShouldValidateEveryBoundary()
    {
        static GridRoomLayoutTemplate Build(
            string key = "grid",
            string version = "1",
            int width = 3,
            int height = 3,
            int movement = 0,
            int min = 1,
            int max = 3,
            int x = 0,
            int y = 0) =>
            new(key, version, default, width, height, movement, min, max, x, y);

        FluentActions.Invoking(() => Build(key: " ")).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(version: " ")).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(width: 0)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(height: 0)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(movement: -1)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(min: 0)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(min: 3, max: 2)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(width: 2, height: 2, max: 5)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(x: -1)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(x: 3)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(y: -1)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Build(y: 3)).Should().Throw<DomainException>();

        var valid = Build(key: " grid ", version: " 1 ", movement: 2, x: 1, y: 2);
        valid.Key.Should().Be("grid");
        valid.Version.Should().Be("1");
        valid.MovementBudget.Should().Be(2);
    }

    [Fact]
    public void NodeEvent_ShouldCoverAllLifecycleTransitions()
    {
        FluentActions.Invoking(() => NodeEvent.Create(NodeEventType.Combat, 0)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => NodeEvent.Create(NodeEventType.Combat, 5)).Should().Throw<DomainException>();

        var resolved = NodeEvent.Create(NodeEventType.Combat, 1);
        resolved.IsPlanned.Should().BeTrue();
        resolved.Resolve();
        resolved.IsResolved.Should().BeTrue();
        resolved.IsTerminal.Should().BeTrue();
        FluentActions.Invoking(resolved.Resolve).Should().Throw<DomainException>();
        FluentActions.Invoking(resolved.Close).Should().Throw<DomainException>();

        var closed = NodeEvent.Create(NodeEventType.Npc, 4);
        closed.Close();
        closed.IsClosed.Should().BeTrue();
        closed.IsTerminal.Should().BeTrue();
        FluentActions.Invoking(closed.Close).Should().NotThrow();
        FluentActions.Invoking(closed.Resolve).Should().Throw<DomainException>();
    }

    [Fact]
    public void EmotionalAffinityMatrixSnapshot_ShouldValidateShapeValuesAndLookupMisses()
    {
        var complete = CompleteAffinityRules().ToArray();

        FluentActions.Invoking(() => EmotionalAffinityMatrixSnapshot.Create(" ", complete))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => EmotionalAffinityMatrixSnapshot.Create("1", null!))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => EmotionalAffinityMatrixSnapshot.Create("1", complete[..^1]))
            .Should().Throw<DomainException>();

        var invalidMultiplier = complete.ToArray();
        invalidMultiplier[0] = invalidMultiplier[0] with { Multiplier = double.NaN };
        FluentActions.Invoking(() => EmotionalAffinityMatrixSnapshot.Create("1", invalidMultiplier))
            .Should().Throw<DomainException>();

        var negativeMultiplier = complete.ToArray();
        negativeMultiplier[0] = negativeMultiplier[0] with { Multiplier = -0.1 };
        FluentActions.Invoking(() => EmotionalAffinityMatrixSnapshot.Create("1", negativeMultiplier))
            .Should().Throw<DomainException>();

        var duplicate = complete.ToArray();
        duplicate[^1] = duplicate[0];
        FluentActions.Invoking(() => EmotionalAffinityMatrixSnapshot.Create("1", duplicate))
            .Should().Throw<DomainException>();

        var matrix = EmotionalAffinityMatrixSnapshot.Create(" 1 ", complete);
        matrix.Version.Should().Be("1");
        matrix.Rules.Should().HaveCount(complete.Length);
        matrix.Resolve(EmotionalType.Neutral, EmotionalType.Neutral).Should().Be(DamageEffectiveness.Neutral);
        matrix.ResolveMultiplier(EmotionalType.Neutral, EmotionalType.Neutral).Should().Be(1.0);
        FluentActions.Invoking(() => matrix.Resolve((EmotionalType)999, EmotionalType.Neutral))
            .Should().Throw<DomainException>();
        FluentActions.Invoking(() => matrix.ResolveMultiplier(EmotionalType.Neutral, (EmotionalType)999))
            .Should().Throw<DomainException>();
    }

    private static IEnumerable<EmotionalAffinityRuleSnapshot> CompleteAffinityRules()
    {
        foreach (var attack in Enum.GetValues<EmotionalType>())
        foreach (var defense in Enum.GetValues<EmotionalType>())
            yield return new EmotionalAffinityRuleSnapshot(attack, defense, DamageEffectiveness.Neutral, 1.0);
    }
}
