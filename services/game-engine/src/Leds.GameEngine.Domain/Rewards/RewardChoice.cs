using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Rewards;

public sealed class RewardChoice
{
    private RewardChoice(
        RewardChoiceId id,
        RewardType rewardType,
        string label,
        string description,
        string payloadKey,
        string? sourceEnemyKey,
        string? sourceEnemyDisplayName,
        int palaceShardCost,
        int himLitShardCost)
    {
        Id = id;
        RewardType = rewardType;
        Label = label;
        Description = description;
        PayloadKey = payloadKey;
        SourceEnemyKey = sourceEnemyKey;
        SourceEnemyDisplayName = sourceEnemyDisplayName;
        PalaceShardCost = palaceShardCost;
        HimLitShardCost = himLitShardCost;
    }

    public RewardChoiceId Id { get; }

    public RewardType RewardType { get; }

    public string Label { get; }

    public string Description { get; }

    public string PayloadKey { get; }

    /// <summary>Null when this choice came from the generic fallback loot pool, not a specific enemy.</summary>
    public string? SourceEnemyKey { get; }

    public string? SourceEnemyDisplayName { get; }

    /// <summary>Cost in "Éclats du Palais" to select this choice. Zero for every
    /// reward that isn't a merchant purchase.</summary>
    public int PalaceShardCost { get; }

    /// <summary>Cost in "Éclats de Him'Lit" to select this choice. Zero for every
    /// reward that isn't a merchant purchase of an Epic+ item.</summary>
    public int HimLitShardCost { get; }

    public static RewardChoice Rehydrate(
        RewardChoiceId id,
        RewardType rewardType,
        string label,
        string description,
        string payloadKey,
        string? sourceEnemyKey = null,
        string? sourceEnemyDisplayName = null,
        int palaceShardCost = 0,
        int himLitShardCost = 0)
    {
        return new RewardChoice(
            id, rewardType, label, description, payloadKey,
            sourceEnemyKey, sourceEnemyDisplayName, palaceShardCost, himLitShardCost);
    }

    public static RewardChoice Create(
        RewardType rewardType,
        string label,
        string description,
        string payloadKey,
        string? sourceEnemyKey = null,
        string? sourceEnemyDisplayName = null,
        int palaceShardCost = 0,
        int himLitShardCost = 0)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException("Reward choice label is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Reward choice description is required.");
        }

        if (string.IsNullOrWhiteSpace(payloadKey))
        {
            throw new DomainException("Reward choice payload key is required.");
        }

        if (palaceShardCost < 0 || himLitShardCost < 0)
        {
            throw new DomainException("Reward choice cost cannot be negative.");
        }

        return new RewardChoice(
            RewardChoiceId.New(),
            rewardType,
            label.Trim(),
            description.Trim(),
            payloadKey.Trim(),
            string.IsNullOrWhiteSpace(sourceEnemyKey) ? null : sourceEnemyKey.Trim(),
            string.IsNullOrWhiteSpace(sourceEnemyDisplayName) ? null : sourceEnemyDisplayName.Trim(),
            palaceShardCost,
            himLitShardCost);
    }
}