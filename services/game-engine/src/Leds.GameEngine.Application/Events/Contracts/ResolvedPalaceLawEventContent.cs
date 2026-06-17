namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedPalaceLawEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string PalaceLawDefinitionKey,
    string PalaceLawName,
    string PalaceLawDescription,
    string PalaceLawDefinitionVersion)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.PalaceLaw,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);
