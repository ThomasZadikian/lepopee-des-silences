namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedRareCombatEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string EnemyTemplateKey,
    string EnemyTemplateVersion,
    int RiskLevel)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.RareCombat,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);
