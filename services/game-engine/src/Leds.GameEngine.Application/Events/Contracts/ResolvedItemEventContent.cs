namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedItemEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string ItemTemplateKey,
    string ItemTemplateVersion,
    string RewardProfile)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Item,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);