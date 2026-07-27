using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats.Tactical;

/// <summary>
/// Emballe un <see cref="CombatRoster"/> en combat tactique : il faut un terrain et des places.
/// </summary>
/// <remarks>
/// <para>
/// Le pendant ATB de cette classe n'existe pas — l'ATB n'a besoin de rien de plus que le roster,
/// et son emballage tient en un appel dans <see cref="CombatFactory"/>. Le tactique demande deux
/// choses que l'ATB ignore : sur quoi on se bat, et où chacun se tient. C'est exactement la
/// frontière d'indépendance de la SFD v2 §2 — le déroulé diffère, le roster ne bouge pas.
/// </para>
/// <para>
/// Le champ de bataille est la salle d'exploration <b>vidée de ses nœuds</b> : reliefs, trous et
/// obstacles subsistent, le reste s'efface. C'est ce que
/// <see cref="TacticalBattlefield.FromRoomGrid"/> fait, et c'est pourquoi le combat tactique
/// hérite gratuitement de la variété topographique déjà produite par la génération de salle.
/// </para>
/// </remarks>
public sealed class TacticalCombatFactory : ITacticalCombatFactory
{
    public TacticalCombat CreateFromRoster(
        CombatId combatId,
        CombatRoster roster,
        Room room,
        NodeId nodeId,
        RunId runId,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(room);

        var battlefield = TacticalBattlefield.FromRoomGrid(room.Grid);

        // Le groupe se dépiaute là où le joueur l'avait mené : l'ancre est sa case d'exploration.
        var anchor = new GridPosition(room.Grid.PartyX, room.Grid.PartyY);

        var allies = roster.Allies.ToArray();
        var enemies = roster.Enemies.ToArray();

        var allyCells = TacticalDeployment.DeployAllies(battlefield, anchor, allies.Length);

        // Le rôle authoré de chaque créature décide de sa distance d'engagement — les gardes au
        // contact, les soutiens en retrait. Cf. TacticalDeployment.PreferredBandFor.
        var enemyCells = TacticalDeployment.DeployEnemies(
            battlefield,
            allyCells,
            [.. enemies.Select(e => e.Archetype)]);

        return TacticalCombat.Create(
            combatId,
            runId,
            room.Id,
            nodeId,
            battlefield,
            [.. allies.Select((c, i) => (c, allyCells[i]))],
            [.. enemies.Select((c, i) => (c, enemyCells[i]))],
            createdAtUtc,
            roster.HitCounterDoubleDamageEnabled,
            roster.FirstHitCriticalEnabled,
            roster.LowHpDamageAmplificationEnabled,
            roster.DotDurationExtensionTicks,
            roster.DuelDamageAsymmetryEnabled,
            roster.DotMagnitudeBonus,
            roster.HealingBlocked);
    }
}
