namespace Leds.Player.Application.Internal.ConsumeRunOutcome;

public sealed record ConsumeRunOutcomeResponse(
    bool Processed,
    string? Reason);
