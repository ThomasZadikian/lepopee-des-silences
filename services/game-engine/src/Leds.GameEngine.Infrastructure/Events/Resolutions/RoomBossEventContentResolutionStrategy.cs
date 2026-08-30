using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Events.Resolution;

// See CombatEventContentResolutionStrategy — same reasoning: EventTemplateKey/
// EnemyTemplateKey below are vestigial (the real boss encounter is built from
// EnemyDefinition/RoomBossDefinition content via ICombatEncounterDraftGenerator's
// EncounterCompositionPolicy.SelectRoomBossEnemies), so this no longer depends on
// catalog EventTemplate/EnemyTemplate content that isn't seeded. IRoomBossProfileResolver
// (which IS real, backed by RoomBossDefinition) still determines which boss this is.
public sealed class RoomBossEventContentResolutionStrategy : IEventContentResolutionStrategy
{
    private const string TemplateVersion = "1.0";

    private readonly IRoomBossProfileResolver _bossProfileResolver;

    public RoomBossEventContentResolutionStrategy(IRoomBossProfileResolver bossProfileResolver)
    {
        _bossProfileResolver = bossProfileResolver;
    }

    public IReadOnlyCollection<NodeEventType> SupportedEventTypes { get; } =
        new[] { NodeEventType.RoomBoss };

    public async Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var bossProfile = await _bossProfileResolver.ResolveAsync(context.RoomType, cancellationToken);

        ResolvedNodeEventContent content = new ResolvedRoomBossEventContent(
            EventTemplateKey: $"event-{bossProfile.EnemyTemplateKey}",
            EventTemplateVersion: TemplateVersion,
            Tags: [],
            EnemyTemplateKey: bossProfile.EnemyTemplateKey,
            EnemyTemplateVersion: TemplateVersion,
            RiskLevel: context.RiskLevel);

        return Result<ResolvedNodeEventContent>.Success(content);
    }
}