namespace Leds.GameEngine.Application.Runs.GetPermanentItemCandidates;

public sealed record GetPermanentItemCandidatesResponse(
    Guid RunId,
    IReadOnlyCollection<PermanentItemCandidateDto> Candidates);

/// <summary>
/// A run item eligible for the end-of-run permanent-backpack selection screen
/// (SFD "Système d'équipement et sac permanent" § 6) — derived from the catalog's
/// <c>ItemDuration.Permanent</c> flag on each distinct item obtained during the run.
/// </summary>
public sealed record PermanentItemCandidateDto(
    string ItemDefinitionKey,
    string DisplayName,
    string Description,
    string Rarity);
