namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedRareEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string RareEventProfileKey)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Rare,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);