using FluentAssertions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.Tactical;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using Moq;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalEnemyTurnDriverTests
{
    /// <summary>
    /// A DoT ticking on the SECOND enemy's activation (handed off to mid-chain by the first
    /// enemy's own end of turn) must surface as its own Tick event — this is what lets the
    /// client show a floating damage number for a DoT tick with no actor decision behind it,
    /// same as any other impact (see TacticalImpactRecorder.BuildTickEvent).
    /// </summary>
    [Fact]
    public void PlayWhileEnemyHasInitiative_ShouldSurfaceATickEvent_WhenADotFiresOnAChainedHandoff()
    {
        var fast = Combatant.CreateEnemy("enemy.fast", "Rapide", "Skirmisher", 30, speed: 30);
        var dotted = Combatant.CreateEnemy("enemy.dotted", "Empoisonné", "Guard", 30, speed: 20);
        var player = Combatant.CreateAlly("player.self", "Ariane", "Bruiser", 40);
        var combat = CreateCombat(fast, dotted, player);

        // Anchored at currentTick 0 with a one-turn interval: fires the instant `dotted`
        // activates for the very first time (StatusClockFor is per-combatant, starting at 0).
        dotted.ApplyStatusEffect(CombatStatusEffect.Create(
            "test.poison", "Poison", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: CombatTime.TicksPerTurn * 10,
            magnitude: 5, tickInterval: CombatTime.TicksPerTurn));

        var resolver = new Mock<ICombatSkillEffectResolver>();
        var clock = Mock.Of<IClock>(
            c => c.UtcNow == DateTimeOffset.Parse("2026-08-03T10:00:00Z"));
        var driver = new TacticalEnemyTurnDriver(resolver.Object, clock, bossBehaviors: []);

        var result = driver.PlayWhileEnemyHasInitiative(combat);

        var tick = result.Events.Should().ContainSingle(e => e.Kind == TacticalCombatEventDto.TickKind)
            .Which;
        tick.ActorId.Should().Be(dotted.Id.Value);
        tick.Impacts.Should().ContainSingle();
        tick.Impacts[0].VitalityDelta.Should().Be(5);
        tick.Impacts[0].Defeated.Should().BeFalse();
    }

    [Fact]
    public void PlayWhileEnemyHasInitiative_ShouldApplyHealToEnemyAlly_NotPlayerPrey()
    {
        var heal = CombatantSkill.Create(
            "skill.enemy.heal",
            "Suture",
            "Heal",
            "SingleAlly",
            "Heal",
            manaCost: 0,
            chargeCost: 0,
            basePower: 8,
            category: "Magic");
        var actor = Combatant.CreateEnemy(
            "enemy.healer", "Chirurgien", "Support", 30, [heal], speed: 20);
        var woundedAlly = Combatant.CreateEnemy(
            "enemy.wounded", "Patient", "Guard", 30, speed: 5);
        woundedAlly.ApplyDamage(20);
        var player = Combatant.CreateAlly("player.self", "Ariane", "Bruiser", 40);
        var combat = CreateCombat(actor, woundedAlly, player);

        IReadOnlyCollection<Combatant>? resolvedTargets = null;
        var resolver = new Mock<ICombatSkillEffectResolver>();
        resolver
            .Setup(r => r.Resolve(
                combat,
                actor,
                heal,
                It.IsAny<IReadOnlyCollection<Combatant>>()))
            .Callback<ICombatContext, Combatant, CombatantSkill, IReadOnlyCollection<Combatant>>(
                (_, _, _, targets) => resolvedTargets = targets)
            .Returns(new CombatSkillEffectResolution([]));
        var clock = Mock.Of<IClock>(
            c => c.UtcNow == DateTimeOffset.Parse("2026-07-30T10:00:00Z"));

        var driver = new TacticalEnemyTurnDriver(resolver.Object, clock, bossBehaviors: []);
        driver.PlayWhileEnemyHasInitiative(combat);

        resolvedTargets.Should().ContainSingle()
            .Which.Should().BeSameAs(woundedAlly);
        resolvedTargets.Should().NotContain(player);
    }

    private static TacticalCombat CreateCombat(
        Combatant actor,
        Combatant woundedAlly,
        Combatant player)
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            4,
            2,
            Enumerable.Repeat(0, 8).ToArray(),
            Enumerable.Repeat(true, 8).ToArray(),
            Enumerable.Repeat(true, 8).ToArray());

        return TacticalCombat.Create(
            CombatId.New(),
            new RunId(Guid.NewGuid()),
            new RoomId(Guid.NewGuid()),
            new NodeId(Guid.NewGuid()),
            battlefield,
            [(player, new GridPosition(3, 0))],
            [
                (actor, new GridPosition(0, 0)),
                (woundedAlly, new GridPosition(1, 0)),
            ],
            DateTime.UtcNow);
    }
}
