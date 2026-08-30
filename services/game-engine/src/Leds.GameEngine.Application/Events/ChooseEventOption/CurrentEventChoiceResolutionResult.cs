using Leds.GameEngine.Application.Events.Dtos;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public sealed record CurrentEventChoiceResolutionResult(
    string ChoiceId,
    bool Accepted,
    string Message,
    IReadOnlyCollection<NarrativeFragmentDto> NarrativeFragments,
    bool EncounterCompleted = true,
    IReadOnlyCollection<AppliedConsequenceEffect>? AppliedEffects = null)
{
    public static CurrentEventChoiceResolutionResult Create(
        string choiceId,
        bool accepted,
        string message,
        IReadOnlyCollection<NarrativeFragmentDto>? narrativeFragments = null,
        bool encounterCompleted = true,
        IReadOnlyCollection<AppliedConsequenceEffect>? appliedEffects = null)
    {
        return new CurrentEventChoiceResolutionResult(
            choiceId,
            accepted,
            message,
            narrativeFragments ?? Array.Empty<NarrativeFragmentDto>(),
            encounterCompleted,
            appliedEffects ?? Array.Empty<AppliedConsequenceEffect>());
    }
}