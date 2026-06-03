namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedCombatEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string EnemyTemplateKey,
    string EnemyTemplateVersion,
    int RiskLevel)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Combat,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);