namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedCurseEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string PalaceLawDefinitionKey,
    string PalaceLawDefinitionVersion)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Curse,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);