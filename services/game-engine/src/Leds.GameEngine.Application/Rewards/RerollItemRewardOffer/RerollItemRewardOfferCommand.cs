using MediatR;

namespace Leds.GameEngine.Application.Rewards.RerollItemRewardOffer;

/// <summary>
/// "Loi de la Chandelle" (law.chandelle): rerolls the run's pending item-node reward
/// offer, consuming one free reroll charge (RunModifierType.ItemNodeRerollCharge).
/// </summary>
public sealed record RerollItemRewardOfferCommand(Guid RunId) : IRequest<RerollItemRewardOfferResponse>;
