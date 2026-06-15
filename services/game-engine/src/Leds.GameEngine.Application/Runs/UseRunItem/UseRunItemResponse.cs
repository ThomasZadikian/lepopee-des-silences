using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.UseRunItem;

public sealed record UseRunItemResponse(
    Guid RunId,
    Guid ItemId,
    string EffectType,
    int EffectAmount,
    bool ItemDepleted,
    bool UsedInCombat,
    PlayerRuntimeStateDto PlayerState,
    CombatRuntimeDto? Combat = null,
    IReadOnlyCollection<CombatLogEntryDto>? LogEntries = null);