using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

// Historically this fetched a catalog EventTemplate/EnemyTemplate to fill
// EventTemplateKey/EnemyTemplateKey below, but those fields are only ever read
// by the (now-removed) throwaway CombatInstance seeding step in
// ResolveCurrentEventCommandHandler — the real encounter is composed from
// EnemyDefinition content via ICombatEncounterDraftGenerator regardless of
// what's returned here. Keeping the fields (for the record shape) but
// populating them locally avoids a dependency on catalog content that was
// never actually seeded.
public sealed class CombatEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string DefaultEventTemplateKey = "event-combat-shadow-v1";
    private const string DefaultEnemyTemplateKey = "enemy-shadow-v1";
    private const string TemplateVersion = "1.0";

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.Combat, NodeEventType.Elite };

    public Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        ResolvedNodeEventContent content = context.EventType == NodeEventType.Elite
            ? new ResolvedEliteEventContent(
                EventTemplateKey: DefaultEventTemplateKey,
                EventTemplateVersion: TemplateVersion,
                Tags: [],
                EnemyTemplateKey: DefaultEnemyTemplateKey,
                EnemyTemplateVersion: TemplateVersion,
                RiskLevel: context.RiskLevel)
            : new ResolvedCombatEventContent(
                EventTemplateKey: DefaultEventTemplateKey,
                EventTemplateVersion: TemplateVersion,
                Tags: [],
                EnemyTemplateKey: DefaultEnemyTemplateKey,
                EnemyTemplateVersion: TemplateVersion,
                RiskLevel: context.RiskLevel);

        return Task.FromResult(Result<ResolvedNodeEventContent>.Success(content));
    }
}