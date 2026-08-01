using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Common.Factories;

/// <summary>
/// Crée un <see cref="TacticalCombat"/> minimal pour les tests unitaires qui ont besoin
/// d'un combat actif sur un <see cref="Run"/>. Remplace l'ancienne API <c>Combat.Create</c>
/// qui n'existe plus depuis la migration vers le combat tactique.
/// </summary>
public static class TestTacticalCombatHelper
{
    public static TacticalCombat Create(
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        Combatant[] allies,
        Combatant[] enemies,
        CombatId? combatId = null,
        bool hitCounterDoubleDamageEnabled = false,
        bool firstHitCriticalEnabled = false,
        bool lowHpDamageAmplificationEnabled = false,
        int dotDurationExtensionTicks = 0,
        bool duelDamageAsymmetryEnabled = false,
        int dotMagnitudeBonus = 0,
        bool healingBlocked = false,
        bool thirdCupHealCorruptionEnabled = false,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? equippedItemKeys = null)
    {
        var grid = RoomGrid.CreateInitial(
            width: 4, height: 4, movementBudget: 26,
            startX: 0, startY: 0, nodes: []);

        var battlefield = TacticalBattlefield.FromRoomGrid(grid);

        var allyPositions = allies
            .Select((a, i) => (a, new GridPosition(0, i)))
            .ToArray();

        var enemyPositions = enemies
            .Select((e, i) => (e, new GridPosition(3, i)))
            .ToArray();

        return TacticalCombat.Create(
            combatId ?? CombatId.New(),
            runId,
            roomId,
            nodeId,
            battlefield,
            allyPositions,
            enemyPositions,
            DateTime.UtcNow,
            hitCounterDoubleDamageEnabled: hitCounterDoubleDamageEnabled,
            firstHitCriticalEnabled: firstHitCriticalEnabled,
            lowHpDamageAmplificationEnabled: lowHpDamageAmplificationEnabled,
            dotDurationExtensionTicks: dotDurationExtensionTicks,
            duelDamageAsymmetryEnabled: duelDamageAsymmetryEnabled,
            dotMagnitudeBonus: dotMagnitudeBonus,
            healingBlocked: healingBlocked,
            thirdCupHealCorruptionEnabled: thirdCupHealCorruptionEnabled,
            equippedItemKeys: equippedItemKeys);
    }

    /// <summary>
    /// Advances <paramref name="combat"/> by exactly <paramref name="rounds"/> full rounds
    /// (<see cref="TacticalCombat.RoundNumber"/>, aliased as <c>TurnNumber</c>) — the
    /// replacement for the old ATB <c>Combat.AdvanceTurn()</c>, which incremented a flat
    /// per-call tick. <see cref="TacticalCombat.AdvanceToNextCombatant"/> only increments
    /// the round once every combatant in the initiative order has acted, so looping until
    /// <see cref="TacticalCombat.RoundNumber"/> reaches the target keeps the call correct
    /// regardless of how many combatants are in the fixture.
    /// </summary>
    public static void AdvanceRounds(TacticalCombat combat, int rounds)
    {
        var target = combat.RoundNumber + rounds;
        while (combat.RoundNumber < target)
        {
            combat.AdvanceToNextCombatant();
        }
    }

    public static (Run Run, TacticalCombat Combat) CreateRunWithCombat(
        Run run,
        Combatant[] allies,
        Combatant[] enemies)
    {
        var combat = Create(
            run.Id,
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies);

        run.StartTacticalCombat(combat);
        return (run, combat);
    }
}
