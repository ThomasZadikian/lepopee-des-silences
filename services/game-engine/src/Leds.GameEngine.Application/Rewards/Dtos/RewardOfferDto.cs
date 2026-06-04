using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.Application.Rewards.Dtos;

public sealed record RewardOfferDto(
    Guid Id,
    string Source,
    string State,
    IReadOnlyCollection<RewardChoiceDto> Choices,
    Guid? SelectedChoiceId)
{
    public static RewardOfferDto FromDomain(RewardOffer offer)
    {
        return new RewardOfferDto(
            offer.Id.Value,
            offer.Source.ToString(),
            offer.State.ToString(),
            offer.Choices.Select(RewardChoiceDto.FromDomain).ToArray(),
            offer.SelectedChoiceId?.Value);
    }
}

public sealed record RewardChoiceDto(
    Guid Id,
    string RewardType,
    string Label,
    string Description,
    string PayloadKey)
{
    public static RewardChoiceDto FromDomain(RewardChoice choice)
    {
        return new RewardChoiceDto(
            choice.Id.Value,
            choice.RewardType.ToString(),
            choice.Label,
            choice.Description,
            choice.PayloadKey);
    }
}
