using Leds.GameEngine.Application.Events.Dtos;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public sealed record ChosenEventOptionResultDto(
    string ChoiceId,
    bool Accepted,
    string Message,
    IReadOnlyCollection<NarrativeFragmentDto> NarrativeFragments)
{
    public static ChosenEventOptionResultDto FromResult(
        CurrentEventChoiceResolutionResult result)
    {
        return new ChosenEventOptionResultDto(
            result.ChoiceId,
            result.Accepted,
            result.Message,
            result.NarrativeFragments);
    }
}