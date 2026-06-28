using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.ResolveCurrentEvent;

public sealed record ResolveCurrentEventResponse(
    RunDto Run,
    ResolvedNodeEventOutcomeDto Outcome,
    CombatEncounterDraftDto? EncounterDraft = null,
    CombatRuntimeDto? Combat = null,
    NpcDialogueViewDto? NpcDialogue = null);