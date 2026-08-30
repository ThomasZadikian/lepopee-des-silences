namespace Leds.Catalog.Domain.RewardCursePools;

// ResultKind is interpreted by the game-engine when applying the draw
// (e.g. "Heal","Damage","Guard","Mana","GrantCurse","GrantLaw"). TargetKey carries
// the content key when granting a curse/law. Availability gates the entry by run context.
public sealed record RewardCurseEntry(
    RewardCurseEntryKind Kind,
    string ResultKind,
    string? TargetKey = null,
    int Amount = 0,
    IReadOnlyList<RewardCurseAvailability>? Availability = null);