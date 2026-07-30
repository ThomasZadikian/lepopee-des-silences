using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.Tactical;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using Moq;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalEnemyTurnDriverTests
{
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

        var driver = new TacticalEnemyTurnDriver(resolver.Object, clock);
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
