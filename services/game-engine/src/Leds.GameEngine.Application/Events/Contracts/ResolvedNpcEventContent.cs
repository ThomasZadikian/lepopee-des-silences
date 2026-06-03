namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedNpcEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string NpcProfileKey,
    string InteractionProfileKey)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Npc,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);