namespace Leds.GameEngine.Application.Events.ChooseEventOption;

// A concrete effect that actually landed (for explicit text + floating feedback).
public sealed record AppliedConsequenceEffect(string Kind, int Amount, string Label);