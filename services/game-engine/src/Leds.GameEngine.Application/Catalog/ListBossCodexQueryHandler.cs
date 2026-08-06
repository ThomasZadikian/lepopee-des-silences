using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Combats.Typing;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class ListBossCodexQueryHandler
    : IRequestHandler<ListBossCodexQuery, ListBossCodexResponse>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public ListBossCodexQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<ListBossCodexResponse> Handle(
        ListBossCodexQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _catalogGateway.ListActiveEnemyDefinitionsAsync(cancellationToken);
        var bosses = definitions
            .Where(definition => string.Equals(definition.Archetype, "Boss", StringComparison.OrdinalIgnoreCase)
                || definition.Tags.Contains("boss", StringComparer.OrdinalIgnoreCase))
            .Select(definition => new BossCodexEntry(
                definition.Key,
                definition.DisplayName,
                definition.Description,
                EmotionalTypeCode.ParseRequired(
                    definition.Registre,
                    $"Boss '{definition.Key}' emotional register").ToString().ToLowerInvariant(),
                definition.CompatibleRoomTypes,
                definition.Menace > 0 ? definition.Menace : definition.BaseDifficulty))
            .OrderBy(definition => definition.Threat)
            .ThenBy(definition => definition.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ListBossCodexResponse(bosses);
    }
}
