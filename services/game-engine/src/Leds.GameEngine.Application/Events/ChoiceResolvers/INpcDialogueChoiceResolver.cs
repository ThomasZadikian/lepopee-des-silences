using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public interface INpcDialogueChoiceResolver
{
    Task<CurrentEventChoiceResolutionResult> ResolveNpcDialogueChoiceAsync(
        Run run,
        MapNode? sourceNode,
        string choiceId,
        CancellationToken cancellationToken = default);
}
