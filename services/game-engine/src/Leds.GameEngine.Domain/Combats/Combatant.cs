using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed class Combatant
{
    private Combatant(
        CombatantId id,
        string sourceKey,
        string displayName,
        CombatantSide side,
        string archetype,
        int maxVitality,
        int currentVitality,
        int guard,
        int mana,
        int charge,
        CombatantStatus status,
        IReadOnlyCollection<CombatantSkill> skills)
    {
        Id = id;
        SourceKey = sourceKey;
        DisplayName = displayName;
        Side = side;
        Archetype = archetype;
        MaxVitality = maxVitality;
        CurrentVitality = currentVitality;
        Guard = guard;
        Mana = mana;
        Charge = charge;
        Status = status;
        Skills = skills;
    }

    public CombatantId Id { get; }
    public string SourceKey { get; }
    public string DisplayName { get; }
    public CombatantSide Side { get; }
    public string Archetype { get; }
    public int MaxVitality { get; }
    public int CurrentVitality { get; private set; }
    public int Guard { get; private set; }
    public int Mana { get; private set; }
    public int Charge { get; private set; }
    public CombatantStatus Status { get; private set; }
    public IReadOnlyCollection<CombatantSkill> Skills { get; }

    public bool IsDefeated => Status == CombatantStatus.Defeated;

    public static Combatant CreateAlly(
        string sourceKey,
        string displayName,
        string archetype,
        int maxVitality,
        IReadOnlyCollection<CombatantSkill>? skills = null)
    {
        return Create(
            CombatantId.New(),
            sourceKey,
            displayName,
            CombatantSide.Player,
            archetype,
            maxVitality,
            currentVitality: maxVitality,
            guard: 0,
            mana: 0,
            charge: 0,
            skills);
    }

    public static Combatant CreateEnemy(
        string sourceKey,
        string displayName,
        string archetype,
        int maxVitality,
        IReadOnlyCollection<CombatantSkill>? skills = null)
    {
        return Create(
            CombatantId.New(),
            sourceKey,
            displayName,
            CombatantSide.Enemy,
            archetype,
            maxVitality,
            currentVitality: maxVitality,
            guard: 0,
            mana: 0,
            charge: 0,
            skills);
    }

    public static Combatant Create(
        CombatantId id,
        string sourceKey,
        string displayName,
        CombatantSide side,
        string archetype,
        int maxVitality,
        int currentVitality,
        int guard,
        int mana,
        int charge,
        IReadOnlyCollection<CombatantSkill>? skills = null)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException("Combatant id is required.");

        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new DomainException("Combatant source key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Combatant display name is required.");

        if (maxVitality <= 0)
            throw new DomainException("Combatant max vitality must be greater than zero.");

        if (currentVitality < 0 || currentVitality > maxVitality)
            throw new DomainException("Combatant current vitality must be between zero and max vitality.");

        if (guard < 0)
            throw new DomainException("Combatant guard must be non-negative.");

        if (mana < 0)
            throw new DomainException("Combatant mana must be non-negative.");

        if (charge < 0)
            throw new DomainException("Combatant charge must be non-negative.");

        return new Combatant(
            id,
            sourceKey.Trim(),
            displayName.Trim(),
            side,
            archetype,
            maxVitality,
            currentVitality,
            guard,
            mana,
            charge,
            CombatantStatus.Active,
            skills?.ToArray() ?? Array.Empty<CombatantSkill>());
    }

    public void MarkDefeated()
    {
        if (Status == CombatantStatus.Defeated)
            throw new DomainException("Combatant is already defeated.");

        Status = CombatantStatus.Defeated;
        CurrentVitality = 0;
    }
}
