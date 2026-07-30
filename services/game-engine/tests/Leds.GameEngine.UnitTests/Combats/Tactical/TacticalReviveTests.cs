using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalReviveTests
{
    [Fact]
    public void ReviveNear_ShouldPlaceAllyOnNearestFreeCellAndRebuildInitiative()
    {
        var user = CreateAlly("ally.user", "Utilisateur", 40, 10);
        var defeated = CreateAlly("ally.down", "À terre", 50, 30);
        var enemy = Combatant.CreateEnemy("enemy.test", "Ennemi", "Bruiser", 40, speed: 20);
        defeated.MarkDefeated();

        var battlefield = TacticalBattlefield.Rehydrate(
            3, 3,
            Enumerable.Repeat(0, 9).ToArray(),
            Enumerable.Repeat(true, 9).ToArray(),
            Enumerable.Repeat(true, 9).ToArray());
        var combat = TacticalCombat.Create(
            CombatId.New(),
            new RunId(Guid.NewGuid()),
            new RoomId(Guid.NewGuid()),
            new NodeId(Guid.NewGuid()),
            battlefield,
            [
                (user, new GridPosition(1, 1)),
                (defeated, new GridPosition(0, 0))
            ],
            [(enemy, new GridPosition(2, 2))],
            DateTime.UtcNow);

        var placed = combat.ReviveNear(defeated.Id.Value, user.Id.Value, 25);

        defeated.IsDefeated.Should().BeFalse();
        defeated.CurrentVitality.Should().Be(25);
        placed.ManhattanDistanceTo(new GridPosition(1, 1)).Should().Be(1);
        combat.InitiativeOrder[0].Should().Be(defeated.Id.Value);
        combat.ActiveCombatantId.Should().Be(defeated.Id.Value);
    }

    private static Combatant CreateAlly(string key, string name, int vitality, int speed)
        => Combatant.Create(
            CombatantId.New(),
            key,
            name,
            CombatantSide.Player,
            "Support",
            vitality,
            vitality,
            0,
            0,
            0,
            0,
            [],
            speed: speed);
}
