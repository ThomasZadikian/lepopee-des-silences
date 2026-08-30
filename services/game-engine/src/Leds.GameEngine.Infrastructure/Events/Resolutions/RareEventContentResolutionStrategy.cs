using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

// See CombatEventContentResolutionStrategy — same reasoning: EventTemplateKey/
// EnemyTemplateKey below are vestigial (the real encounter is built from
// EnemyDefinition content via ICombatEncounterDraftGenerator), so this no
// longer depends on catalog EventTemplate/EnemyTemplate content that isn't seeded.
public sealed class RareEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string DefaultEventTemplateKey = "event-rare-encounter-v1";
    private const string DefaultEnemyTemplateKey = "enemy-rare-v1";
    private const string TemplateVersion = "1.0";

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Rare };

    public Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        ResolvedNodeEventContent content = new ResolvedRareCombatEventContent(
            EventTemplateKey: DefaultEventTemplateKey,
            EventTemplateVersion: TemplateVersion,
            Tags: [],
            EnemyTemplateKey: DefaultEnemyTemplateKey,
            EnemyTemplateVersion: TemplateVersion,
            RiskLevel: context.RiskLevel);

        return Task.FromResult(Result<ResolvedNodeEventContent>.Success(content));
    }
}