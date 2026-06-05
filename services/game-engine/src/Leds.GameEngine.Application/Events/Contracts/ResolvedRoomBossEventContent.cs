namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedRoomBossEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string EnemyTemplateKey,
    string EnemyTemplateVersion,
    int RiskLevel)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Boss,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);
