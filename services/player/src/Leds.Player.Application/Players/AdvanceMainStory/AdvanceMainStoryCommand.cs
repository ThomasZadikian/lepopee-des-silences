using MediatR;

namespace Leds.Player.Application.Players.AdvanceMainStory;

public sealed record AdvanceMainStoryCommand(
    Guid PlayerId,
    string SequenceKey,
    string SequenceVersion,
    string StepKey,
    string? CheckpointKey,
    IReadOnlyCollection<string> UnlockedRoomKeys,
    IReadOnlyCollection<string> VisibleRoomKeys,
    bool Complete) : IRequest<PlayerProfileDto>;
