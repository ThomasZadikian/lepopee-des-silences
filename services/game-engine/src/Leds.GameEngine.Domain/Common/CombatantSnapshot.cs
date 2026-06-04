using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed class CombatantSnapshot
{
    private CombatantSnapshot(
        CombatantId id,
        string templateKey,
        string displayName,
        CombatantSide side,
        int maxHealth,
        int currentHealth,
        int attack,
        int defense,
        int speed)
    {
        Id = id;
        TemplateKey = templateKey;
        DisplayName = displayName;
        Side = side;
        MaxHealth = maxHealth;
        CurrentHealth = currentHealth;
        Attack = attack;
        Defense = defense;
        Speed = speed;
    }

    public CombatantId Id { get; }

    public string TemplateKey { get; }

    public string DisplayName { get; }

    public CombatantSide Side { get; }

    public int MaxHealth { get; }

    public int CurrentHealth { get; private set; }

    public int Attack { get; }

    public int Defense { get; }

    public int Speed { get; }

    public bool IsDefeated => CurrentHealth <= 0;

    public static CombatantSnapshot Create(
        string templateKey,
        string displayName,
        CombatantSide side,
        int maxHealth,
        int attack,
        int defense,
        int speed)
    {
        return Create(
            CombatantId.New(),
            templateKey,
            displayName,
            side,
            maxHealth,
            currentHealth: maxHealth,
            attack,
            defense,
            speed);
    }

    public static CombatantSnapshot Create(
        CombatantId id,
        string templateKey,
        string displayName,
        CombatantSide side,
        int maxHealth,
        int currentHealth,
        int attack,
        int defense,
        int speed)
    {
        if (id.Value == Guid.Empty)
        {
            throw new DomainException("Combatant id is required.");
        }

        if (string.IsNullOrWhiteSpace(templateKey))
        {
            throw new DomainException("Combatant template key is required.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("Combatant display name is required.");
        }

        if (maxHealth <= 0)
        {
            throw new DomainException("Combatant max health must be greater than zero.");
        }

        if (currentHealth < 0 || currentHealth > maxHealth)
        {
            throw new DomainException("Combatant current health must be between zero and max health.");
        }

        if (attack < 0)
        {
            throw new DomainException("Combatant attack must be non-negative.");
        }

        if (defense < 0)
        {
            throw new DomainException("Combatant defense must be non-negative.");
        }

        if (speed < 0)
        {
            throw new DomainException("Combatant speed must be non-negative.");
        }

        return new CombatantSnapshot(
            id,
            templateKey.Trim(),
            displayName.Trim(),
            side,
            maxHealth,
            currentHealth,
            attack,
            defense,
            speed);
    }

    public void ReceiveDamage(int damage)
    {
        if (damage <= 0)
        {
            throw new DomainException("Damage must be greater than zero.");
        }

        if (IsDefeated)
        {
            throw new DomainException("Defeated combatant cannot receive damage.");
        }

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
    }
}