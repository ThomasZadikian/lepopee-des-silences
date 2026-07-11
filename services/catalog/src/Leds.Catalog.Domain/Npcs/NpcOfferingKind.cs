namespace Leds.Catalog.Domain.Npcs;

public enum NpcOfferingKind
{
    Skill = 0,
    Item = 1,
    StatPoint = 2,
    // Boosts the run's relationship score with a DIFFERENT NPC than the one granting
    // the offering (TargetKey = the other NPC's key, Amount = the score delta) —
    // e.g. Araran vouching for the player with Tovma and Mané.
    ReputationBoost = 3
}
