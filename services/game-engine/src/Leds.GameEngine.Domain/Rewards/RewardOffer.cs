using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Rewards;

public sealed class RewardOffer
{
    private readonly List<RewardChoice> _choices;

    private RewardOffer(
        RewardOfferId id,
        RewardSource source,
        IReadOnlyCollection<RewardChoice> choices,
        RewardOfferState state,
        CombatRiskProfile? combatScaling = null,
        RewardChoiceId? selectedChoiceId = null)
    {
        Id = id;
        Source = source;
        _choices = choices.ToList();
        State = state;
        CombatScaling = combatScaling;
        SelectedChoiceId = selectedChoiceId;
    }

    public RewardOfferId Id { get; }

    public RewardSource Source { get; }

    public IReadOnlyCollection<RewardChoice> Choices => _choices.AsReadOnly();

    public RewardOfferState State { get; private set; }

    public RewardChoiceId? SelectedChoiceId { get; private set; }

    /// <summary>
    /// Populated for combat encounters (Combat, Rare, Elite, RoomBoss, FinalBoss).
    /// Null for non-combat node rewards (Item, Rest, Npc, Law, Merchant, Curse).
    /// </summary>
    public CombatRiskProfile? CombatScaling { get; }

    public bool IsPending => State == RewardOfferState.Pending;

    public static RewardOffer Create(
        RewardSource source,
        IReadOnlyCollection<RewardChoice> choices,
        CombatRiskProfile? combatScaling = null)
    {
        var choiceList = choices?.ToList()
            ?? throw new DomainException("Reward choices are required.");

        if (choiceList.Count == 0)
        {
            throw new DomainException("Reward offer must have at least one choice.");
        }

        if (choiceList.Select(choice => choice.Id).Distinct().Count() != choiceList.Count)
        {
            throw new DomainException("Reward choice ids must be unique.");
        }

        return new RewardOffer(
            RewardOfferId.New(),
            source,
            choiceList,
            RewardOfferState.Pending,
            combatScaling);
    }

    public void SelectChoice(RewardChoiceId choiceId)
    {
        if (State != RewardOfferState.Pending)
        {
            throw new DomainException("Only a pending reward offer can be selected.");
        }

        if (!_choices.Any(choice => choice.Id == choiceId))
        {
            throw new DomainException("Reward choice was not found in the offer.");
        }

        SelectedChoiceId = choiceId;
        State = RewardOfferState.Selected;
    }
}