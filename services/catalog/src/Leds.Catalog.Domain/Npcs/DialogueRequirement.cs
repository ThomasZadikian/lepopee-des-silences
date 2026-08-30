namespace Leds.Catalog.Domain.Npcs;

// Guards the visibility of a choice/node. Evaluated against the runtime relationship.
public sealed record DialogueRequirement(
    DialogueRequirementKind Kind,
    string? FlagKey = null,
    string? WoundKey = null,
    WoundState? RequiredWoundState = null,
    int? RequiredRelationshipScore = null);