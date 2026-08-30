namespace Leds.Catalog.Domain.Npcs;

public enum NpcOfferingKind
{
    Skill = 0,
    Item = 1,
    StatPoint = 2,
    // Boosts the run's relationship score with a DIFFERENT NPC than the one granting
    // the offering (TargetKey = the other NPC's key, Amount = the score delta) —
    // e.g. Araran vouching for the player with Tovma et Mané.
    ReputationBoost = 3,
    // Recruits the NPC as a permanent companion (TargetKey = the companion's
    // character definition key, e.g. "character.thomas") — added to the player's
    // roster for life, fights alongside the protagonist in every future run.
    Companion = 4,
    // Awards a flat amount of the player's persistent currency ("Éclats du Palais").
    // Amount = how much to award. No spend mechanism exists yet — this is a pure
    // counter for now (see John's rare offering).
    Currency = 5
}
