namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedEliteEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string EnemyTemplateKey,
    string EnemyTemplateVersion,
    int RiskLevel)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Elite,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);