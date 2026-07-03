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
        string? sourceEnemyDisplayName)
    {
        Id = id;
        RewardType = rewardType;
        Label = label;
        Description = description;
        PayloadKey = payloadKey;
        SourceEnemyKey = sourceEnemyKey;
        SourceEnemyDisplayName = sourceEnemyDisplayName;
    }

    public RewardChoiceId Id { get; }

    public RewardType RewardType { get; }

    public string Label { get; }

    public string Description { get; }

    public string PayloadKey { get; }

    /// <summary>Null when this choice came from the generic fallback loot pool, not a specific enemy.</summary>
    public string? SourceEnemyKey { get; }

    public string? SourceEnemyDisplayName { get; }

    public static RewardChoice Rehydrate(
        RewardChoiceId id,
        RewardType rewardType,
        string label,
        string description,
        string payloadKey,
        string? sourceEnemyKey = null,
        string? sourceEnemyDisplayName = null)
    {
        return new RewardChoice(id, rewardType, label, description, payloadKey, sourceEnemyKey, sourceEnemyDisplayName);
    }

    public static RewardChoice Create(
        RewardType rewardType,
        string label,
        string description,
        string payloadKey,
        string? sourceEnemyKey = null,
        string? sourceEnemyDisplayName = null)
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

        return new RewardChoice(
            RewardChoiceId.New(),
            rewardType,
            label.Trim(),
            description.Trim(),
            payloadKey.Trim(),
            string.IsNullOrWhiteSpace(sourceEnemyKey) ? null : sourceEnemyKey.Trim(),
            string.IsNullOrWhiteSpace(sourceEnemyDisplayName) ? null : sourceEnemyDisplayName.Trim());
    }
}