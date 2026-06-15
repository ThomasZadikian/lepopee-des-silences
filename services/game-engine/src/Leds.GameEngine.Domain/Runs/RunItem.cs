using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class RunItem
{
    private RunItem(
        RunItemId id,
        string definitionKey,
        string displayName,
        string description,
        RunItemType type,
        RunItemRarity rarity,
        int quantity,
        RunItemEffectType effectType,
        int effectAmount,
        DateTime createdAtUtc)
    {
        Id = id;
        DefinitionKey = definitionKey;
        DisplayName = displayName;
        Description = description;
        Type = type;
        Rarity = rarity;
        Quantity = quantity;
        EffectType = effectType;
        EffectAmount = effectAmount;
        CreatedAtUtc = createdAtUtc;
    }

    public RunItemId Id { get; }
    public string DefinitionKey { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public RunItemType Type { get; }
    public RunItemRarity Rarity { get; }
    public int Quantity { get; private set; }
    public RunItemEffectType EffectType { get; }
    public int EffectAmount { get; }
    public DateTime CreatedAtUtc { get; }

    public static RunItem Create(
        string definitionKey,
        string displayName,
        string description,
        RunItemType type,
        RunItemRarity rarity,
        int quantity,
        RunItemEffectType effectType,
        int effectAmount)
    {
        if (string.IsNullOrWhiteSpace(definitionKey))
            throw new DomainException("Item definition key is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Item display name is required.");
        if (quantity <= 0)
            throw new DomainException("Item quantity must be positive.");

        return new RunItem(
            RunItemId.New(),
            definitionKey.Trim(),
            displayName.Trim(),
            description?.Trim() ?? string.Empty,
            type,
            rarity,
            quantity,
            effectType,
            effectAmount,
            DateTime.UtcNow);
    }

    public static RunItem Rehydrate(
        RunItemId id,
        string definitionKey,
        string displayName,
        string description,
        RunItemType type,
        RunItemRarity rarity,
        int quantity,
        RunItemEffectType effectType,
        int effectAmount,
        DateTime createdAtUtc)
    {
        return new RunItem(
            id, definitionKey, displayName, description,
            type, rarity, quantity, effectType, effectAmount, createdAtUtc);
    }

    public void AddQuantity(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Quantity to add must be positive.");
        Quantity += amount;
    }

    public void ConsumeOne()
    {
        if (Type != RunItemType.Consumable)
            throw new DomainException(
                $"Item '{DefinitionKey}' is not consumable and cannot be used from inventory.");

        if (Quantity <= 0)
            throw new DomainException(
                $"Item '{DefinitionKey}' has no remaining uses.");

        Quantity--;
    }

    /// <summary>
    /// Vrai pour les effets activables pendant un combat (soins, garde, mana, charge).
    /// Faux pour les effets passifs, narratifs ou hors-combat.
    /// </summary>
    public bool IsBattleItem => EffectType is
      RunItemEffectType.Heal
      or RunItemEffectType.ManaRestore
      or RunItemEffectType.ChargeRestore;

    /// <summary>
    /// Type de ciblage attendu par le frontend pour cet effet en combat.
    /// Heal → SingleAlly (guérit un allié choisi).
    /// Autres → Self (s'applique toujours au joueur).
    /// </summary>
    public string BattleTargetingType => EffectType == RunItemEffectType.Heal
        ? "SingleAlly"
        : "Self";

    /// <summary>
    /// Vrai si l'objet peut être utilisé manuellement (consommable, quantité > 0, effet activable).
    /// </summary>
    public bool IsUsable =>
        Type == RunItemType.Consumable
        && Quantity > 0
        && IsBattleItem;
}