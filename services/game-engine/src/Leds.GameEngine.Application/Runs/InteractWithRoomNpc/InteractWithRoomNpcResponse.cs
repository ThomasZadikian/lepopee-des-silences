using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.InteractWithRoomNpc;

public sealed record InteractWithRoomNpcResponse(
    RunDto Run,
    RoomNpcDto Actor,
    IReadOnlyCollection<RoomNpcInteractionNoticeDto> LocalRuleNotices);

public sealed record RoomNpcInteractionNoticeDto(
    string RuleKey,
    string RuleName,
    string Outcome,
    string? Message);
