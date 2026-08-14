using Leds.GameEngine.Application.Protocol;
using Leds.GameEngine.Domain.Protocol;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps.Hall;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

/// <summary>
/// Statically-defined <see cref="LocalRule"/> set, mirroring
/// <see cref="HardcodedRoomStructuralProfileProvider"/>'s own pattern — only the Hall has
/// authored protocol content today (<see cref="HallEntreeProtocol"/>), every other catalog room
/// has none.
/// </summary>
public sealed class HardcodedLocalRuleProvider : ILocalRuleProvider
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<LocalRule>> RulesByCatalogRoomKey =
        new Dictionary<string, IReadOnlyList<LocalRule>>(StringComparer.OrdinalIgnoreCase)
        {
            ["room.halldentree"] = HallEntreeProtocol.Rules,
        };

    private static readonly IReadOnlyDictionary<string, LocalRule> RulesByKey =
        RulesByCatalogRoomKey.Values.SelectMany(rules => rules).ToDictionary(rule => rule.Key, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<LocalRule> GetRulesForRoom(string catalogRoomKey)
    {
        return RulesByCatalogRoomKey.TryGetValue(catalogRoomKey, out var rules) ? rules : [];
    }

    public LocalRule? GetByKey(string localRuleKey)
    {
        return RulesByKey.TryGetValue(localRuleKey, out var rule) ? rule : null;
    }
}
