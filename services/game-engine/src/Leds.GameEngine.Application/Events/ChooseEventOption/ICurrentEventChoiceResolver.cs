using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public interface ICurrentEventChoiceResolver
{
    NodeEventType EventType { get; }

    CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context);
}