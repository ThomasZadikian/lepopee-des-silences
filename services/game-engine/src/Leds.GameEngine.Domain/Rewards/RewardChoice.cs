using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Rewards;

public sealed class RewardChoice
{
    private RewardChoice(
        RewardChoiceId id,
        RewardType rewardType,
        string label,
        string description,
        string payloadKey)
    {
        Id = id;
        RewardType = rewardType;
        Label = label;
        Description = description;
        PayloadKey = payloadKey;
    }

    public RewardChoiceId Id { get; }

    public RewardType RewardType { get; }

    public string Label { get; }

    public string Description { get; }

    public string PayloadKey { get; }

    public static RewardChoice Create(
        RewardType rewardType,
        string label,
        string description,
        string payloadKey)
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
            payloadKey.Trim());
    }
}
