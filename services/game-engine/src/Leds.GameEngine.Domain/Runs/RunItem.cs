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
}
