using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class RunItem
{
    public const int CanonicalConsumableStackLimit = 20;

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
        DateTime createdAtUtc,
        string? definitionVersion,
        string? narrativeText,
        string? category,
        string? usageMode,
        string? lifecycle,
        int? maxStack,
        string? effectSetKey,
        string? effectSummary,
        bool? isUsableInCombat,
        bool? isUsableOutsideCombat,
        Guid? sourceRewardOptionId,
        bool isContainer,
        int? containerCapacity,
        bool isLiquid,
        string? containedLiquidDefinitionKey,
        int tacticalRange,
        string tacticalAreaShape,
        bool requiresLineOfSight)
    {
        Id = id;
        DefinitionKey = definitionKey;
        DefinitionVersion = definitionVersion;
        DisplayName = displayName;
        Description = description;
        NarrativeText = narrativeText;
        Type = type;
        Rarity = rarity;
        Category = category;
        UsageMode = usageMode;
        Lifecycle = lifecycle;
        Quantity = quantity;
        MaxStack = maxStack;
        EffectType = effectType;
        EffectAmount = effectAmount;
        EffectSetKey = effectSetKey;
        EffectSummary = effectSummary;
        IsUsableInCombat = isUsableInCombat ?? IsCombatEffect(effectType);
        IsUsableOutsideCombat = isUsableOutsideCombat ?? true;
        SourceRewardOptionId = sourceRewardOptionId;
        CreatedAtUtc = createdAtUtc;
        IsContainer = isContainer;
        ContainerCapacity = containerCapacity;
        IsLiquid = isLiquid;
        ContainedLiquidDefinitionKey = containedLiquidDefinitionKey;
        TacticalRange = Math.Max(0, tacticalRange);
        TacticalAreaShape = string.IsNullOrWhiteSpace(tacticalAreaShape)
            ? "Single"
            : tacticalAreaShape.Trim();
        RequiresLineOfSight = requiresLineOfSight;
    }

    public RunItemId Id { get; }
    public string DefinitionKey { get; }
    public string? DefinitionVersion { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string? NarrativeText { get; }
    public RunItemType Type { get; }
    public RunItemRarity Rarity { get; }
    public string? Category { get; }
    public string? UsageMode { get; }
    public string? Lifecycle { get; }
    public int Quantity { get; private set; }
    public int? MaxStack { get; }
    public int EffectiveMaxStack => Type == RunItemType.Consumable
        ? Math.Clamp(MaxStack ?? CanonicalConsumableStackLimit, 1, CanonicalConsumableStackLimit)
        : 1;
    public RunItemEffectType EffectType { get; }
    public int EffectAmount { get; }
    public string? EffectSetKey { get; }
    public string? EffectSummary { get; }
    public bool IsUsableInCombat { get; }
    public bool IsUsableOutsideCombat { get; }
    public Guid? SourceRewardOptionId { get; }
    public DateTime CreatedAtUtc { get; }
    public bool IsContainer { get; }
    public int? ContainerCapacity { get; }
    public bool IsLiquid { get; }
    public string? ContainedLiquidDefinitionKey { get; private set; }
    public int TacticalRange { get; }
    public string TacticalAreaShape { get; }
    public bool RequiresLineOfSight { get; }

    public static RunItem Create(
        string definitionKey,
        string displayName,
        string description,
        RunItemType type,
        RunItemRarity rarity,
        int quantity,
        RunItemEffectType effectType,
        int effectAmount,
        bool isContainer = false,
        int? containerCapacity = null,
        bool isLiquid = false)
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
            DateTime.UtcNow,
            definitionVersion: null,
            narrativeText: null,
            category: null,
            usageMode: null,
            lifecycle: null,
            maxStack: null,
            effectSetKey: null,
            effectSummary: null,
            isUsableInCombat: null,
            isUsableOutsideCombat: null,
            sourceRewardOptionId: null,
            isContainer: isContainer,
            containerCapacity: containerCapacity,
            isLiquid: isLiquid,
            containedLiquidDefinitionKey: null,
            tacticalRange: 1,
            tacticalAreaShape: "Single",
            requiresLineOfSight: false);
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
        DateTime createdAtUtc,
        string? definitionVersion = null,
        string? narrativeText = null,
        string? category = null,
        string? usageMode = null,
        string? lifecycle = null,
        int? maxStack = null,
        string? effectSetKey = null,
        string? effectSummary = null,
        bool? isUsableInCombat = null,
        bool? isUsableOutsideCombat = null,
        Guid? sourceRewardOptionId = null,
        bool isContainer = false,
        int? containerCapacity = null,
        bool isLiquid = false,
        string? containedLiquidDefinitionKey = null,
        int tacticalRange = 1,
        string tacticalAreaShape = "Single",
        bool requiresLineOfSight = false)
    {
        return new RunItem(
            id, definitionKey, displayName, description,
            type, rarity, quantity, effectType, effectAmount, createdAtUtc,
            definitionVersion, narrativeText, category, usageMode, lifecycle,
            maxStack, effectSetKey, effectSummary, isUsableInCombat, isUsableOutsideCombat,
            sourceRewardOptionId, isContainer, containerCapacity, isLiquid, containedLiquidDefinitionKey,
            tacticalRange, tacticalAreaShape, requiresLineOfSight);
    }

    public void AddQuantity(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Quantity to add must be positive.");

        if (!CanAddQuantity(amount))
            throw new DomainException(
                $"Item '{DefinitionKey}' cannot exceed its stack limit of {EffectiveMaxStack}.");

        Quantity += amount;
    }

    public bool CanAddQuantity(int amount)
        => amount > 0 && Quantity <= EffectiveMaxStack - amount;

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

    public void PourLiquidInto(string liquidDefinitionKey)
    {
        if (!IsContainer)
            throw new DomainException($"Item '{DefinitionKey}' is not a container and cannot hold a liquid.");
        if (string.IsNullOrWhiteSpace(liquidDefinitionKey))
            throw new DomainException("Liquid definition key is required.");
        if (ContainedLiquidDefinitionKey is not null)
            throw new DomainException($"Item '{DefinitionKey}' already holds a liquid — empty it first.");

        ContainedLiquidDefinitionKey = liquidDefinitionKey.Trim();
    }

    public void EmptyContents()
    {
        if (!IsContainer)
            throw new DomainException($"Item '{DefinitionKey}' is not a container.");
        if (ContainedLiquidDefinitionKey is null)
            throw new DomainException($"Item '{DefinitionKey}' is already empty.");

        ContainedLiquidDefinitionKey = null;
    }

    public bool IsBattleItem => EffectType is
      RunItemEffectType.Heal
      or RunItemEffectType.ManaRestore
      or RunItemEffectType.ChargeRestore
      or RunItemEffectType.HealAndManaRestorePercent
      or RunItemEffectType.HealPercent
      or RunItemEffectType.ConditionalHealOrPoison
      or RunItemEffectType.HealPercentAndCleanseDot
      or RunItemEffectType.HealPercentAndSilence
      or RunItemEffectType.RevivePercent
      or RunItemEffectType.HealPercentAndEvasion;

    public string BattleTargetingType => EffectType == RunItemEffectType.RevivePercent
        ? "DefeatedAlly"
        : "SingleAlly";

    public bool IsUsable =>
        Type == RunItemType.Consumable
        && Quantity > 0
        && IsBattleItem;

    private static bool IsCombatEffect(RunItemEffectType effectType) => effectType is
        RunItemEffectType.Heal
        or RunItemEffectType.Guard
        or RunItemEffectType.ManaRestore
        or RunItemEffectType.ChargeRestore
        or RunItemEffectType.HealAndManaRestorePercent
        or RunItemEffectType.HealPercent
        or RunItemEffectType.ConditionalHealOrPoison
        or RunItemEffectType.HealPercentAndCleanseDot
        or RunItemEffectType.HealPercentAndSilence
        or RunItemEffectType.RevivePercent
        or RunItemEffectType.HealPercentAndEvasion;
}
