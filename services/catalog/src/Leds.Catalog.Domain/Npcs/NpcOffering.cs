namespace Leds.Catalog.Domain.Npcs;

// A mechanical gift a PNJ can make: a skill/item key to grant, or a stat-point amount.
// Referenced by key from a dialogue choice's GrantOffering consequence, the same way
// a RewardOrCurseRoll consequence references a RewardCursePoolKey rather than embedding
// the pool inline. IsMajor gates the "never given twice" lifetime invariant.
public sealed record NpcOffering(
    string Key,
    NpcOfferingKind Kind,
    string? TargetKey,
    int Amount,
    bool IsMajor,
    IReadOnlyList<DialogueRequirement> UnlockConditions,
    // Only meaningful when Kind == Companion — the authored combat kit the recruited
    // companion fights with. Null on every other offering kind.
    CompanionKitSpec? CompanionKit = null);
