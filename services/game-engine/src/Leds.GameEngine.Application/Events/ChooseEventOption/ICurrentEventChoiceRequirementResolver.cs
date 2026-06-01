using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public interface ICurrentEventChoiceRequirementResolver
{
    bool RequiresChoice(Node node);
}