using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.MoveParty;

/// <summary>One <see cref="Protocol.LocalRuleTriggerOutcome"/>, shaped for the API — the message
/// to show and whether the party is only being informed or has actually transgressed.</summary>
public sealed record LocalRuleNoticeDto(
    string RuleKey,
    string RuleName,
    string Outcome,
    string? Message);

public sealed record MovePartyResponse(
    RunDto Run,
    IReadOnlyCollection<Guid> CollectedItemIds,
    IReadOnlyCollection<Guid> BlockedItemIds,
    IReadOnlyCollection<LocalRuleNoticeDto> LocalRuleNotices);
