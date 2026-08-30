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
/// Le champ de bataille est la salle d'exploration <b>vidée de ses nœuds</b> : reliefs, trous et
/// obstacles subsistent, le reste s'efface. C'est ce que
/// <see cref="TacticalBattlefield.FromRoomGrid"/> fait, et c'est pourquoi le combat tactique
/// hérite gratuitement de la variété topographique déjà produite par la génération de salle.
/// </para>
/// </remarks>
public sealed class TacticalCombatFactory : ITacticalCombatFactory
{
    /// <summary>
    /// Demi-largeur de l'arène locale autour du groupe. // BALANCE KNOB
    /// </summary>
    /// <remarks>
    /// 10 donne une arène 21×21 : assez pour couvrir la fourchette d'engagement actuelle
    /// (<see cref="TacticalDeployment.MaxEngagementDistance"/> = 20, atteignable en diagonale
    /// dans une fenêtre de rayon 10) tout en restant nettement plus petite qu'une grande salle —
    /// c'est exactement le but recherché : un combat déclenché n'importe où dans la salle ne
    /// s'étend plus à sa totalité. Sur les salles d'aujourd'hui (26×18 ou moins), l'arène couvre
    /// déjà presque toute la hauteur et une bonne partie de la largeur, donc le comportement
    /// existant change à peine.
    /// </remarks>
    private const int ArenaRadius = 10;

    public TacticalCombat CreateFromRoster(
        CombatId combatId,
        CombatRoster roster,
        Room room,
        NodeId nodeId,
        Run run,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(room);

        // Le groupe se dépiaute là où le joueur l'avait mené : l'ancre est sa case d'exploration,
        // et c'est aussi le centre de l'arène locale — recadrée aux limites de la salle par
        // FromRoomGridRegion près d'un bord ou d'un coin.
        var arenaSize = (2 * ArenaRadius) + 1;
        var battlefield = TacticalBattlefield.FromRoomGridRegion(
            room.Grid,
            room.Grid.PartyX - ArenaRadius,
            room.Grid.PartyY - ArenaRadius,
            arenaSize,
            arenaSize);

        var anchor = new GridPosition(
            room.Grid.PartyX - battlefield.OriginX, room.Grid.PartyY - battlefield.OriginY);

        var allies = roster.Allies.ToArray();
        var enemies = roster.Enemies.ToArray();

        var allyCells = TacticalDeployment.DeployAllies(battlefield, anchor, allies.Length);

        // Le rôle authoré de chaque créature décide de sa distance d'engagement — les gardes au
        // contact, les soutiens en retrait. Cf. TacticalDeployment.PreferredBandFor.
        var enemyCells = TacticalDeployment.DeployEnemies(
            battlefield,
            allyCells,
            [.. enemies.Select(e => e.Archetype)]);
        var equipment = allies.ToDictionary(
            ally => ally.Id.Value,
            ally => (IReadOnlyCollection<string>)(run.PlayerSnapshot?.Characters
                .FirstOrDefault(character => ally.CharacterInstanceId == character.CharacterId)
                ?.EquippedItemKeys
                .Concat(ally.EquipmentBehaviorCodes.Select(code => $"behavior:{code}"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? ally.EquipmentBehaviorCodes.Select(code => $"behavior:{code}").ToArray()));

        var selectedNode = room.GetNode(nodeId);
        // The player's chosen/generated tier lives directly on the node
        // (selectedNode.CombatRiskTier — set at generation and, for Combat/Rare nodes,
        // overridable via the "Difficulté" room selector / SetRoomRiskTierCommand). Elite
        // and RoomBoss/FinalBoss keep a fixed danger tier by design, mirroring
        // ResolveCurrentEventCommandHandler.GenerateEncounterDraft's catalogRiskLevel — but
        // Combat/Rare MUST reuse the node's own tier as-is. Reconstructing it from the
        // deployed enemy headcount (the previous approach) is lossy: Dangereux and Perilleux
        // both field 4 enemies, so a "Périlleux" pick silently landed here as Dangereux —
        // understating AI aggression (TacticalEnemyAi) and the RiskTier-gated behaviors in
        // TacticalEnemyTurnDriver, on top of the identical bug fixed at draft-generation time.
        var riskTier = selectedNode.EventType switch
        {
            NodeEventType.RoomBoss or NodeEventType.FinalBoss => RiskTier.Fatal,
            NodeEventType.Elite => RiskTier.Dangereux,
            NodeEventType.Rare => (RiskTier)Math.Max(
                (int)RiskTier.Tendu, (int)(selectedNode.CombatRiskTier ?? RiskTier.Tendu)),
            _ => selectedNode.CombatRiskTier ?? RiskTier.Tendu
        };
        var escapePosition = selectedNode.EventType == NodeEventType.Combat
            ? ResolveEscapePosition(battlefield, allyCells, enemyCells)
            : null;

        return TacticalCombat.Create(
            combatId,
            run.Id,
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
            roster.HealingBlocked,
            roster.PostDeathBasicAttackOnlyEnabled,
            roster.TapisPropreEnabled,
            roster.ThirdCupHealCorruptionEnabled,
            escapePosition,
            riskTier,
            equipment,
            roster.FalaiseWindEnabled,
            roster.PresentationsEnabled,
            roster.MiroirEnabled,
            roster.ForgottenSkillKey,
            run.EmotionalAffinityMatrix);
    }

    private static GridPosition? ResolveEscapePosition(
        TacticalBattlefield battlefield,
        IReadOnlyCollection<GridPosition> allies,
        IReadOnlyCollection<GridPosition> enemies)
    {
        var allyX = allies.Average(cell => cell.X);
        var allyY = allies.Average(cell => cell.Y);
        var enemyX = enemies.Average(cell => cell.X);
        var enemyY = enemies.Average(cell => cell.Y);
        var dx = enemyX - allyX;
        var dy = enemyY - allyY;

        var border = Enumerable.Range(0, battlefield.Width)
            .SelectMany(x => new[]
            {
                new GridPosition(x, 0),
                new GridPosition(x, battlefield.Height - 1)
            })
            .Concat(Enumerable.Range(1, Math.Max(0, battlefield.Height - 2))
                .SelectMany(y => new[]
                {
                    new GridPosition(0, y),
                    new GridPosition(battlefield.Width - 1, y)
                }))
            .Distinct()
            .Where(battlefield.IsWalkable)
            .ToArray();

        var reachable = allies
            .SelectMany(origin => TacticalMovement.ReachableCells(
                battlefield,
                origin,
                battlefield.Width * battlefield.Height * 4,
                new HashSet<GridPosition>()).Keys)
            .ToHashSet();

        var accessible = border.Where(reachable.Contains).ToArray();
        if (accessible.Length == 0)
            return null;

        // Projection vers le bord situé derrière le centre ennemi. Si ce bord est impraticable,
        // le tri retombe naturellement sur la case de bord accessible la plus proche.
        var ideal = Math.Abs(dx) >= Math.Abs(dy)
            ? new GridPosition(dx >= 0 ? battlefield.Width - 1 : 0,
                Math.Clamp((int)Math.Round(enemyY), 0, battlefield.Height - 1))
            : new GridPosition(
                Math.Clamp((int)Math.Round(enemyX), 0, battlefield.Width - 1),
                dy >= 0 ? battlefield.Height - 1 : 0);

        return accessible
            .OrderBy(cell => cell.ManhattanDistanceTo(ideal))
            .ThenByDescending(cell => ((cell.X - allyX) * dx) + ((cell.Y - allyY) * dy))
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .First();
    }
}
