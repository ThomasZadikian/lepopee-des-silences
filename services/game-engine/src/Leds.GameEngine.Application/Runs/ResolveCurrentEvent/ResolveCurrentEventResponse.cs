using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.ResolveCurrentEvent;

public sealed record ResolveCurrentEventResponse(
    RunDto Run,
    ResolvedNodeEventOutcomeDto Outcome,
    CombatEncounterDraftDto? EncounterDraft = null,
    NpcDialogueViewDto? NpcDialogue = null,
    TacticalCombatRuntimeDto? TacticalCombat = null,
    /// <summary>
    /// La mise en scène des tours ennemis déjà joués à l'ouverture, quand l'initiative revient
    /// à l'adversaire.
    /// </summary>
    IReadOnlyList<TacticalCombatEventDto>? TacticalEvents = null);
