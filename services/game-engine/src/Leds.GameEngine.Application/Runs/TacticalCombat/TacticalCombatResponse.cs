using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

/// <summary>
/// Réponse commune aux commandes tactiques : l'état de la run et celui du champ de bataille.
/// </summary>
public sealed record TacticalCombatResponse(
    RunDto Run,
    TacticalCombatRuntimeDto Combat,
    IReadOnlyCollection<CombatLogEntryDto> LogEntries,
    /// <summary>
    /// La mise en scène de ce qui vient d'être résolu, dans l'ordre. Le client la déroule à son
    /// rythme ; `Combat` porte déjà l'état final, qui fait foi.
    /// </summary>
    IReadOnlyList<TacticalCombatEventDto> Events);
