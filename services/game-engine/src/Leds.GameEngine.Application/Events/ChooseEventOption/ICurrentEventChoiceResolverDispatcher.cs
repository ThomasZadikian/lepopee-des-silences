namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public interface ICurrentEventChoiceResolverDispatcher
{
    Task<CurrentEventChoiceResolutionResult> ResolveAsync(
        CurrentEventChoiceResolutionContext context,
        CancellationToken cancellationToken = default);
}
