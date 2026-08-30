namespace Leds.Catalog.Domain.Npcs;

public enum DialogueRequirementKind
{
    FlagPresent = 0,
    FlagAbsent = 1,
    WoundStateAtLeast = 2,
    RelationshipScoreAtLeast = 3,
    // Evaluated against the run's inventory (not the NPC relationship): true when the
    // player currently owns at least one container-type RunItem (e.g. a fiole).
    PlayerHasContainerItem = 4,
    // Evaluated against the run's current Attack/Defense/Speed/Focus (not the NPC
    // relationship): true when no single stat is more than ~50% below the strongest
    // one (an authored "optimization" threshold) — e.g. the Architect's trigger.
    PlayerStatsBalanced = 5,
    PlayerStatsUnbalanced = 6,
    // Evaluated against the run's player roster (not the NPC relationship): FlagKey
    // holds the companion's character definition key (e.g. "character.elise").
    PlayerHasCompanion = 7,
    PlayerLacksCompanion = 8
}