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
/// Format CSV plutôt que tables jointes, par cohérence avec <c>RoomGrid</c> côté exploration :
/// un combat est toujours chargé en entier avec son agrégat, jamais interrogé par morceaux.
/// </para>
/// </remarks>
public static class TacticalCombatPersistenceMapper
{
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
            Status = combat.Status.ToString(),

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
            PostDeathBasicAttackOnlyEnabled = combat.PostDeathBasicAttackOnlyEnabled,
            NextActionRestrictedToBasicAttack = combat.NextActionRestrictedToBasicAttack,
            TapisPropreEnabled = combat.TapisPropreEnabled,
            ThirdCupHealCorruptionEnabled = combat.ThirdCupHealCorruptionEnabled,
            FalaiseWindEnabled = combat.FalaiseWindEnabled,
            PresentationsEnabled = combat.PresentationsEnabled,
            MiroirEnabled = combat.MiroirEnabled,
            HasMirrorTriggered = combat.HasMirrorTriggered,
            ForgottenSkillKey = combat.ForgottenSkillKey,

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
                .Select(p => $"{p.Key}:{p.Value.X},{p.Value.Y},{combat.FacingOf(p.Key)}")),
            TacticalTurnStatesCsv = string.Join(';', combat.Allies.Concat(combat.Enemies)
                .Select(c =>
                {
                    var turn = combat.TurnStateOf(c.Id.Value);
                    return $"{c.Id.Value}:{(turn.HasMoved ? 1 : 0)},{(turn.HasActed ? 1 : 0)}";
                })),
            TacticalUsedOnceSkillKeysCsv = string.Join(';', combat.UsedOnceSkillKeys),
            TacticalSkillCooldownsCsv = string.Join(';', combat.Allies.Concat(combat.Enemies)
                .SelectMany(c => combat.CooldownsOf(c.Id.Value)
                    .Select(pair => $"{c.Id.Value}:{pair.Key},{pair.Value}"))),
            TacticalEscapeX = combat.EscapePosition?.X,
            TacticalEscapeY = combat.EscapePosition?.Y,
            TacticalRiskTier = combat.RiskTier.ToString(),
            TacticalEquippedItemsCsv = string.Join(';', combat.EquippedItemKeys
                .Where(pair => pair.Value.Count > 0)
                .Select(pair => $"{pair.Key}:{string.Join(',', pair.Value)}")),
            TacticalActivationCountsCsv = string.Join(
                ';',
                combat.ActivationCounts.Select(pair => $"{pair.Key}:{pair.Value}")),
            TacticalLastMagicCsv = string.Join(
                ';',
                combat.LastActivationUsedMagic.Select(pair =>
                    $"{pair.Key}:{(pair.Value ? 1 : 0)}")),
            TacticalCannotReviveCsv = string.Join(';', combat.CannotRevive),

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

        // Ordonnancement stable pour que l'affichage ne dépende pas de celui rendu par la base.
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
            entity.PostDeathBasicAttackOnlyEnabled,
            entity.NextActionRestrictedToBasicAttack,
            entity.TapisPropreEnabled,
            entity.ThirdCupHealCorruptionEnabled,
            entity.HitCounter,
            entity.HasFirstHitLanded,
            entity.CurrentTick,
            ParseStringList(entity.TacticalUsedOnceSkillKeysCsv),
            ParseFacings(entity.TacticalPositionsCsv),
            ParseSkillCooldowns(entity.TacticalSkillCooldownsCsv),
            entity.TacticalEscapeX.HasValue && entity.TacticalEscapeY.HasValue
                ? new GridPosition(entity.TacticalEscapeX.Value, entity.TacticalEscapeY.Value)
                : null,
            string.IsNullOrWhiteSpace(entity.TacticalRiskTier)
                ? RiskTier.Calme
                : Enum.Parse<RiskTier>(entity.TacticalRiskTier),
            ParseEquippedItems(entity.TacticalEquippedItemsCsv),
            ParseActivationCounts(entity.TacticalActivationCountsCsv),
            ParseBooleanMap(entity.TacticalLastMagicCsv),
            ParseGuidList(entity.TacticalCannotReviveCsv),
            entity.FalaiseWindEnabled,
            entity.PresentationsEnabled,
            entity.MiroirEnabled,
            entity.HasMirrorTriggered,
            entity.ForgottenSkillKey);
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

    private static List<string> ParseStringList(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : [.. csv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

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

    private static Dictionary<Guid, TacticalFacing> ParseFacings(string? csv)
    {
        var facings = new Dictionary<Guid, TacticalFacing>();
        if (string.IsNullOrWhiteSpace(csv))
            return facings;

        foreach (var entry in csv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.IndexOf(':');
            var id = Guid.Parse(entry[..separator]);
            var values = entry[(separator + 1)..].Split(',');
            facings[id] = values.Length >= 3
                ? Enum.Parse<TacticalFacing>(values[2])
                : TacticalFacing.South;
        }

        return facings;
    }

    private static Dictionary<(Guid CombatantId, string SkillKey), int> ParseSkillCooldowns(
        string? csv)
    {
        var cooldowns = new Dictionary<(Guid CombatantId, string SkillKey), int>();
        if (string.IsNullOrWhiteSpace(csv))
            return cooldowns;

        foreach (var entry in csv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idSeparator = entry.IndexOf(':');
            var valueSeparator = entry.LastIndexOf(',');
            var combatantId = Guid.Parse(entry[..idSeparator]);
            var skillKey = entry[(idSeparator + 1)..valueSeparator];
            var remaining = int.Parse(
                entry[(valueSeparator + 1)..],
                CultureInfo.InvariantCulture);
            cooldowns[(combatantId, skillKey)] = remaining;
        }

        return cooldowns;
    }

    private static Dictionary<Guid, IReadOnlyCollection<string>> ParseEquippedItems(string? csv)
    {
        var items = new Dictionary<Guid, IReadOnlyCollection<string>>();
        if (string.IsNullOrWhiteSpace(csv))
            return items;

        foreach (var entry in csv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.IndexOf(':');
            if (separator <= 0)
                continue;

            items[Guid.Parse(entry[..separator])] = entry[(separator + 1)..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return items;
    }

    private static Dictionary<Guid, int> ParseActivationCounts(string? csv)
    {
        var counts = new Dictionary<Guid, int>();
        if (string.IsNullOrWhiteSpace(csv))
            return counts;

        foreach (var entry in csv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.IndexOf(':');
            if (separator > 0)
                counts[Guid.Parse(entry[..separator])] = int.Parse(
                    entry[(separator + 1)..],
                    CultureInfo.InvariantCulture);
        }

        return counts;
    }

    private static Dictionary<Guid, bool> ParseBooleanMap(string? csv)
    {
        var values = new Dictionary<Guid, bool>();
        if (string.IsNullOrWhiteSpace(csv))
            return values;

        foreach (var entry in csv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.IndexOf(':');
            if (separator > 0)
                values[Guid.Parse(entry[..separator])] = entry[(separator + 1)..] == "1";
        }

        return values;
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
