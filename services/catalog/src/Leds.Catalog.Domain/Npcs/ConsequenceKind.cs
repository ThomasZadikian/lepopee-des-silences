namespace Leds.Catalog.Domain.Npcs;

// L1 consequence vocabulary. L2/L3 add PalaceAttitudeShift, MarkovShift, EnqueueEvent.
public enum ConsequenceKind
{
    Narrative = 0,
    RewardOrCurseRoll = 1,
    TriggerUniqueCombat = 2,
    AdjustRelationship = 3,
    SetMemoryFlag = 4,
    ArmWound = 5,
    SootheWound = 6
}