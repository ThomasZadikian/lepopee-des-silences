namespace Leds.GameEngine.Application.Runs.GetRunReputation;

public sealed record GetRunReputationResponse(
    Guid RunId,
    IReadOnlyCollection<NpcReputationDto> Npcs);

/// <param name="AggregateState">"Latent" | "Tendu" | "Rompu" — the most severe of the NPC's wounds.</param>
public sealed record NpcReputationDto(
    string NpcKey,
    string DisplayName,
    string EmotionalRegister,
    int RelationshipScore,
    string AggregateState,
    int TimesMet,
    IReadOnlyCollection<NpcOfferingReputationDto> Offerings);

/// <param name="RequiredRelationshipScore">Null when the offering carries no score threshold
/// (it may still gate on other conditions, e.g. a memory flag, not reflected here).</param>
/// <param name="ScoreThresholdMet">True when no score threshold is set, or the current
/// relationship score already meets it — does not account for non-score conditions.</param>
public sealed record NpcOfferingReputationDto(
    string Key,
    string Kind,
    bool IsMajor,
    int? RequiredRelationshipScore,
    bool ScoreThresholdMet);
