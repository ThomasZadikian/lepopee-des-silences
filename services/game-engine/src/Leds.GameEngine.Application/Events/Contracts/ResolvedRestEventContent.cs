namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedRestEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    int RecoveryRatio)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Rest,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);