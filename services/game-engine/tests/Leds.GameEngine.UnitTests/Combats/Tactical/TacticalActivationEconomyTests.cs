using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalActivationEconomyTests
{
    [Fact]
    public void Activation_ShouldRegenerateManaAndDecrementCooldown()
    {
        var skill = CombatantSkill.Create(
            "skill.cooldown",
            "Geste",
            "Damage",
            "SingleEnemy",
            "Damage",
            manaCost: 0,
            chargeCost: 0,
            basePower: 10,
            cooldown: 2, emotionalRegister: "Neutral");
        var ally = Combatant.Create(
            CombatantId.New(),
            "player.self",
            "Allié",
            CombatantSide.Player,
            "Porteur",
            maxVitality: 40,
            currentVitality: 40,
            guard: 0,
            baseGuard: 0,
            mana: 0,
            charge: 0,
            skills: [skill],
            speed: 20,
            maxMana: 20);
        var enemy = Combatant.CreateEnemy(
            "enemy.test",
            "Ennemi",
            "Bruiser",
            40,
            speed: 10,
            mana: 20);
        var battlefield = TacticalBattlefield.Rehydrate(
            2,
            1,
            [0, 0],
            [true, true],
            [true, true]);
        var combat = TacticalCombat.Create(
            CombatId.New(),
            new RunId(Guid.NewGuid()),
            new RoomId(Guid.NewGuid()),
            new NodeId(Guid.NewGuid()),
            battlefield,
            [(ally, new GridPosition(0, 0))],
            [(enemy, new GridPosition(1, 0))],
            DateTime.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        ally.Mana.Should().Be(1, "the opening activation also regenerates Mana");
        combat.MarkActiveCombatantActed(skill);
        combat.RemainingCooldown(ally.Id.Value, skill.Key).Should().Be(2);

        combat.AdvanceToNextCombatant();
        combat.AdvanceToNextCombatant();

        combat.ActiveCombatantId.Should().Be(ally.Id.Value);
        combat.RemainingCooldown(ally.Id.Value, skill.Key).Should().Be(1);
        ally.Mana.Should().Be(2);
    }
}
