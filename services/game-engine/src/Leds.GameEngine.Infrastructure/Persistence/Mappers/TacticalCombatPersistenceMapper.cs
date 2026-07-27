using System.Globalization;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;

namespace Leds.GameEngine.Infrastructure.Persistence.Mappers;

/// <summary>
/// Aller-retour d'un combat tactique avec sa table.
/// </summary>
/// <remarks>
/// <para>
/// Les combattants passent par <see cref="CombatPersistenceMapper"/> sans rien de spécifique :
/// ce sont exactement les mêmes objets qu'en ATB, avec les mêmes stats et les mêmes statuts.
/// Seul ce qui est propre au déroulé tactique — terrain, positions, ordre d'initiative, état de
/// tour — est sérialisé ici.
/// </para>
/// <para>
/// Format CSV plutôt que tables jointes, par cohérence avec <c>RoomGrid</c> côté exploration :
/// un combat est toujours chargé en entier avec son agrégat, jamais interrogé par morceaux.
/// </para>
/// </remarks>
public static class TacticalCombatPersistenceMapper
{
    public const string KindDiscriminator = "Tactical";

    public static CombatEntity ToEntity(TacticalCombat combat, Guid runId)
    {
        ArgumentNullException.ThrowIfNull(combat);

        var field = combat.Battlefield;
        var elevation = new List<string>(field.Width * field.Height);
        var walkable = new List<string>(field.Width * field.Height);
        var floor = new List<string>(field.Width * field.Height);

        for (var y = 0; y < field.Height; y++)
        {
            for (var x = 0; x < field.Width; x++)
            {
                var cell = new GridPosition(x, y);
                var isWalkable = field.IsWalkable(cell);
                walkable.Add(isWalkable ? "1" : "0");
                floor.Add(field.IsFloor(cell) ? "1" : "0");
                // Hors du praticable, l'élévation n'est pas définie : on écrit un 0 neutre, que
                // la relecture ne réintroduira jamais comme du sol (c'est walkable qui tranche).
                elevation.Add((isWalkable ? field.ElevationAt(cell) : 0)
                    .ToString(CultureInfo.InvariantCulture));
            }
        }

        return new CombatEntity
        {
            Id = combat.Id.Value,
            RunId = runId,
            RoomId = combat.RoomId.Value,
            NodeId = combat.NodeId.Value,
            Kind = KindDiscriminator,
            Status = combat.Status.ToString(),

            // Les colonnes de tempo ATB n'ont pas de sens ici : le tactique compte en rounds.
            TurnNumber = combat.RoundNumber,
            CurrentTick = combat.CurrentTick,

            HitCounter = combat.HitCounter,
            HasFirstHitLanded = combat.HasFirstHitLanded,
            HitCounterDoubleDamageEnabled = combat.HitCounterDoubleDamageEnabled,
            FirstHitCriticalEnabled = combat.FirstHitCriticalEnabled,
            LowHpDamageAmplificationEnabled = combat.LowHpDamageAmplificationEnabled,
            DotDurationExtensionTicks = combat.DotDurationExtensionTicks,
            DuelDamageAsymmetryEnabled = combat.DuelDamageAsymmetryEnabled,
            DotMagnitudeBonus = combat.DotMagnitudeBonus,
            HealingBlocked = combat.HealingBlocked,

            ActiveCombatantId = combat.ActiveCombatantId,
            CreatedAtUtc = combat.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow,

            TacticalWidth = field.Width,
            TacticalHeight = field.Height,
            TacticalElevationCsv = string.Join(',', elevation),
            TacticalWalkableCsv = string.Join(',', walkable),
            TacticalFloorCsv = string.Join(',', floor),
            TacticalRoundNumber = combat.RoundNumber,
            TacticalActiveIndex = combat.InitiativeOrder.Count == 0
                ? 0
                : Math.Max(0, combat.InitiativeOrder
                    .ToList()
                    .IndexOf(combat.ActiveCombatantId ?? Guid.Empty)),
            TacticalInitiativeOrderCsv = string.Join(';', combat.InitiativeOrder),
            TacticalPositionsCsv = string.Join(';', combat.Positions
                .Select(p => $"{p.Key}:{p.Value.X},{p.Value.Y}")),
            TacticalTurnStatesCsv = string.Join(';', combat.Allies.Concat(combat.Enemies)
                .Select(c =>
                {
                    var turn = combat.TurnStateOf(c.Id.Value);
                    return $"{c.Id.Value}:{(turn.HasMoved ? 1 : 0)},{(turn.HasActed ? 1 : 0)}";
                })),

            Combatants = [.. combat.Allies.Concat(combat.Enemies)
                .Select(c => CombatPersistenceMapper.ToEntity(c, combat.Id.Value))],
        };
    }

    public static TacticalCombat ToDomain(CombatEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var width = entity.TacticalWidth
            ?? throw new InvalidOperationException(
                $"Combat '{entity.Id}' is marked tactical but carries no battlefield width.");
        var height = entity.TacticalHeight
            ?? throw new InvalidOperationException(
                $"Combat '{entity.Id}' is marked tactical but carries no battlefield height.");

        var elevation = ParseIntCsv(entity.TacticalElevationCsv, width * height);
        var walkable = ParseIntCsv(entity.TacticalWalkableCsv, width * height)
            .Select(v => v != 0)
            .ToArray();

        // `TacticalFloorCsv` est arrivé après les premières colonnes tactiques : absent, la
        // reconstruction retombe sur « praticable = dans la salle ».
        var floor = string.IsNullOrWhiteSpace(entity.TacticalFloorCsv)
            ? null
            : ParseIntCsv(entity.TacticalFloorCsv, width * height).Select(v => v != 0).ToArray();

        var battlefield = TacticalBattlefield.Rehydrate(
            width, height, elevation, walkable, floor);

        var combatants = entity.Combatants.Select(CombatPersistenceMapper.ToDomain).ToList();

        // Même ordonnancement stable qu'en ATB : le protagoniste d'abord, pour que l'ordre
        // d'affichage ne dépende pas de celui que la base a rendu.
        var allies = combatants
            .Where(c => c.Side == CombatantSide.Player)
            .OrderBy(c => string.Equals(c.SourceKey, "player.self", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(c => c.SourceKey, StringComparer.Ordinal)
            .ThenBy(c => c.Id.Value)
            .ToList();
        var enemies = combatants
            .Where(c => c.Side == CombatantSide.Enemy)
            .OrderBy(c => c.SourceKey, StringComparer.Ordinal)
            .ThenBy(c => c.Id.Value)
            .ToList();

        return TacticalCombat.Rehydrate(
            new CombatId(entity.Id),
            new RunId(entity.RunId),
            new RoomId(entity.RoomId),
            new NodeId(entity.NodeId),
            battlefield,
            allies,
            enemies,
            ParsePositions(entity.TacticalPositionsCsv),
            ParseTurnStates(entity.TacticalTurnStatesCsv),
            ParseGuidList(entity.TacticalInitiativeOrderCsv),
            entity.TacticalActiveIndex ?? 0,
            entity.TacticalRoundNumber ?? 1,
            Enum.Parse<CombatStatus>(entity.Status),
            entity.CreatedAtUtc,
            entity.HitCounterDoubleDamageEnabled,
            entity.FirstHitCriticalEnabled,
            entity.LowHpDamageAmplificationEnabled,
            entity.DotDurationExtensionTicks,
            entity.DuelDamageAsymmetryEnabled,
            entity.DotMagnitudeBonus,
            entity.HealingBlocked,
            entity.HitCounter,
            entity.HasFirstHitLanded,
            entity.CurrentTick);
    }

    private static int[] ParseIntCsv(string? csv, int expectedLength)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new int[expectedLength];

        var values = csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => int.Parse(v, CultureInfo.InvariantCulture))
            .ToArray();

        if (values.Length != expectedLength)
            throw new InvalidOperationException(
                $"Expected {expectedLength} tactical grid values but read {values.Length}.");

        return values;
    }

    private static List<Guid> ParseGuidList(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : [.. csv.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse)];

    private static Dictionary<Guid, GridPosition> ParsePositions(string? csv)
    {
        var positions = new Dictionary<Guid, GridPosition>();

        if (string.IsNullOrWhiteSpace(csv))
            return positions;

        foreach (var entry in csv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.IndexOf(':');
            var id = Guid.Parse(entry[..separator]);
            var coordinates = entry[(separator + 1)..].Split(',');

            positions[id] = new GridPosition(
                int.Parse(coordinates[0], CultureInfo.InvariantCulture),
                int.Parse(coordinates[1], CultureInfo.InvariantCulture));
        }

        return positions;
    }

    private static Dictionary<Guid, TacticalCombat.TacticalTurnState> ParseTurnStates(string? csv)
    {
        var states = new Dictionary<Guid, TacticalCombat.TacticalTurnState>();

        if (string.IsNullOrWhiteSpace(csv))
            return states;

        foreach (var entry in csv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.IndexOf(':');
            var id = Guid.Parse(entry[..separator]);
            var flags = entry[(separator + 1)..].Split(',');

            states[id] = new TacticalCombat.TacticalTurnState(
                HasMoved: flags[0] == "1",
                HasActed: flags[1] == "1");
        }

        return states;
    }
}
