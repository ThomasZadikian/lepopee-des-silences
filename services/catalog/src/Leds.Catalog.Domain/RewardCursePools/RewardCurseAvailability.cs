namespace Leds.Catalog.Domain.RewardCursePools;

public sealed record RewardCurseAvailability(
    RewardCursePoolFilterKind Kind,
    int Value);