namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedCurseEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string PalaceLawDefinitionKey,
    string PalaceLawName,
    string PalaceLawDescription,
    string PalaceLawDefinitionVersion)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Curse,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);
