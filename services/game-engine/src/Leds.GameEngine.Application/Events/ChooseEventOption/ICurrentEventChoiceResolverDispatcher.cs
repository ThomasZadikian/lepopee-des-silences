namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public interface ICurrentEventChoiceResolverDispatcher
{
    CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context);
}