using Leds.GameEngine.Domain.Protocol;

namespace Leds.GameEngine.Application.Protocol;

/// <summary>
/// Resolves the authored <see cref="LocalRule"/> definitions a room generator/evaluator needs —
/// mirrors <see cref="RoomMaps.IRoomStructuralProfileProvider"/>'s split between "what a room
/// generator asks for by catalog key" and "what a rule evaluator looks up by its own key" once a
/// trigger has to be checked. Not Catalog/Seeder-backed yet (see
/// <see cref="Infrastructure.Generation.RoomMaps.Hall.HallEntreeLayout"/>'s own remarks on the
/// same gap for geometry) — SFD Hall d'entrée §V itself notes the exhaustive rule list "reste à
/// écrire", so a hardcoded provider is the honest state of the content today, not a shortcut
/// around a Catalog mechanism that already exists.
/// </summary>
public interface ILocalRuleProvider
{
    /// <summary>Every rule a room generator should attach <see cref="LocalRuleState"/> tracking
    /// for, keyed by the room's catalog key (e.g. "room.halldentree"). Empty for any room with no
    /// authored protocol.</summary>
    IReadOnlyList<LocalRule> GetRulesForRoom(string catalogRoomKey);

    /// <summary>A single rule by its own <see cref="LocalRule.Key"/> — what an evaluator needs
    /// once it already knows which <see cref="LocalRuleState"/> it's checking.</summary>
    LocalRule? GetByKey(string localRuleKey);
}
