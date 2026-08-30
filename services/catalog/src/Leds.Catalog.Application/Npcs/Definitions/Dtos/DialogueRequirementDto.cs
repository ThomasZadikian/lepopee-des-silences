using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record DialogueRequirementDto(
    string Kind,
    string? FlagKey,
    string? WoundKey,
    string? RequiredWoundState,
    int? RequiredRelationshipScore = null)
{
    public static DialogueRequirementDto FromDomain(DialogueRequirement r) => new(
        r.Kind.ToString(),
        r.FlagKey,
        r.WoundKey,
        r.RequiredWoundState?.ToString(),
        r.RequiredRelationshipScore);
}