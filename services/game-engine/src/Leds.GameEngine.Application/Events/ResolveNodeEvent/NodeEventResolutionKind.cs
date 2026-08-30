namespace Leds.GameEngine.Application.Events.ResolveNodeEvent;

public enum NodeEventResolutionKind
{
    CombatStarted = 0,
    EliteEncounterStarted = 1,
    RewardGranted = 2,
    NarrativeFragmentRevealed = 3,
    TomePageUnlocked = 4,
    RestResolved = 5,
    TradeOffered = 6,
    PalaceLawOffered = 7,
    CurseOffered = 8,
    RareEventResolved = 9,       // Kept for backward compat — no longer emitted
    RoomBossEncounterStarted = 10,
    FinalBossEncounterStarted = 11,
    RareCombatStarted = 12
}