using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Protocol;

/// <summary>
/// A room-local protocol: condition → information/avertissement → transgression → gravité →
/// conséquences (SFD Hall d'entrée §V). Deliberately generic and distinct from the global "Lois
/// du Palais" (<see cref="PalaceLaws.PalaceLaw"/>) — a LocalRule is authored per room/zone, never
/// affects the whole run, and its consequences are room-local reactions (an NPC's attention, a
/// closed path, a Veilleur), not run-wide mechanical modifiers.
/// <para>
/// This is the static/authored definition — condition, messages, consequence ladder. Run-scoped
/// progress against it (has the party been informed yet, how much severity has accumulated) lives
/// in the separate <see cref="LocalRuleState"/>, mirroring how <see cref="Npcs.RoomNpc"/> and
/// <see cref="Npcs.NpcRelationship"/> split "what's authored" from "what a run has done with it".
/// </para>
/// </summary>
public sealed class LocalRule
{
    private readonly List<(int X, int Y)> _conditionCells;
    private readonly List<LocalRuleConsequence> _consequences;

    private LocalRule(
        LocalRuleId id,
        string key,
        string name,
        LocalRuleConditionType conditionType,
        List<(int X, int Y)> conditionCells,
        string? conditionTargetKey,
        string infoMessage,
        string warningMessage,
        List<LocalRuleConsequence> consequences)
    {
        Id = id;
        Key = key;
        Name = name;
        ConditionType = conditionType;
        _conditionCells = conditionCells;
        ConditionTargetKey = conditionTargetKey;
        InfoMessage = infoMessage;
        WarningMessage = warningMessage;
        _consequences = consequences;
    }

    public LocalRuleId Id { get; }

    /// <summary>Data-driven identity — the Catalog key once this rule is Catalog/Seeder-backed
    /// (Ensemble 4's Majordome rule is the first concrete instance).</summary>
    public string Key { get; }

    public string Name { get; }

    public LocalRuleConditionType ConditionType { get; }

    /// <summary>Populated for <see cref="LocalRuleConditionType.ZoneEntry"/> only — a threshold is
    /// simply a one-cell zone, not a separate representation.</summary>
    public IReadOnlyCollection<(int X, int Y)> ConditionCells => _conditionCells.AsReadOnly();

    /// <summary>Populated for ObjectInteraction/NpcInteraction only — the catalog key of the
    /// object/NPC that trips this rule.</summary>
    public string? ConditionTargetKey { get; }

    /// <summary>Shown the first time the condition is met — informs without punishing.</summary>
    public string InfoMessage { get; }

    /// <summary>Shown on the first actual transgression (the condition met again after the party
    /// has already been informed).</summary>
    public string WarningMessage { get; }

    /// <summary>Graduated ladder, ascending by <see cref="LocalRuleConsequence.SeverityThreshold"/>.
    /// Never empty and never has Combat below its highest threshold (Contrat/SFD Hall §V: no
    /// automatic combat on a single misstep).</summary>
    public IReadOnlyList<LocalRuleConsequence> Consequences => _consequences.AsReadOnly();

    public static LocalRule Create(
        string key,
        string name,
        LocalRuleConditionType conditionType,
        string infoMessage,
        string warningMessage,
        IReadOnlyCollection<LocalRuleConsequence> consequences,
        IReadOnlyCollection<(int X, int Y)>? conditionCells = null,
        string? conditionTargetKey = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Local rule key is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Local rule name is required.");
        }

        if (string.IsNullOrWhiteSpace(infoMessage))
        {
            throw new DomainException("Local rule info message is required.");
        }

        if (string.IsNullOrWhiteSpace(warningMessage))
        {
            throw new DomainException("Local rule warning message is required.");
        }

        var cellList = conditionCells?.Distinct().ToList() ?? [];
        var trimmedTargetKey = string.IsNullOrWhiteSpace(conditionTargetKey) ? null : conditionTargetKey.Trim();

        if (conditionType == LocalRuleConditionType.ZoneEntry && cellList.Count == 0)
        {
            throw new DomainException("A zone-entry local rule needs at least one condition cell.");
        }

        if (conditionType != LocalRuleConditionType.ZoneEntry && cellList.Count > 0)
        {
            throw new DomainException("Only a zone-entry local rule can carry condition cells.");
        }

        if (conditionType != LocalRuleConditionType.ZoneEntry && trimmedTargetKey is null)
        {
            throw new DomainException($"A {conditionType} local rule needs a condition target key.");
        }

        if (conditionType == LocalRuleConditionType.ZoneEntry && trimmedTargetKey is not null)
        {
            throw new DomainException("A zone-entry local rule cannot carry a condition target key.");
        }

        var consequenceList = consequences?.ToList()
            ?? throw new DomainException("Local rule consequences are required.");

        if (consequenceList.Count == 0)
        {
            throw new DomainException("A local rule needs at least one consequence.");
        }

        var orderedConsequences = consequenceList.OrderBy(c => c.SeverityThreshold).ToList();

        if (orderedConsequences.Select(c => c.SeverityThreshold).Distinct().Count() != orderedConsequences.Count)
        {
            throw new DomainException("Local rule consequences must have distinct severity thresholds.");
        }

        return new LocalRule(
            LocalRuleId.New(), key.Trim(), name.Trim(), conditionType, cellList, trimmedTargetKey,
            infoMessage.Trim(), warningMessage.Trim(), orderedConsequences);
    }
}
