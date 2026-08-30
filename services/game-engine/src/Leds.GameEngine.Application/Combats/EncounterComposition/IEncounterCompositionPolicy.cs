namespace Leds.GameEngine.Application.Combats.EncounterComposition;

public interface IEncounterCompositionPolicy
{
    EncounterCompositionResult Compose(EncounterCompositionContext context);
}