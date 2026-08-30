namespace Leds.GameEngine.Application.Events.Contracts;

/// <summary>
/// Application-level resolved content for a node event.
/// This is not a final runtime aggregate such as CombatInstance or RewardOffer.
/// It is the typed bridge between node generation and runtime domain modules.
/// </summary>
public abstract record ResolvedNodeEventContent(
    ResolvedEventContentKind Kind,
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags);