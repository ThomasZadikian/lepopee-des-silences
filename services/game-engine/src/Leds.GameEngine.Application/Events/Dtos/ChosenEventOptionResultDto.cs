using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ChooseEventOption;

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