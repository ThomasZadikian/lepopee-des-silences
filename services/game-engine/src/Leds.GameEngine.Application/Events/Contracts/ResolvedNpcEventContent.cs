namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedNpcEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string NpcProfileKey,
    string InteractionProfileKey,
    string NpcDisplayName = "",
    string NpcDescription = "")
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Npc,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);