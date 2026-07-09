namespace Leds.Catalog.Domain.Npcs;

public enum DialogueRequirementKind
{
    FlagPresent = 0,
    FlagAbsent = 1,
    WoundStateAtLeast = 2,
    RelationshipScoreAtLeast = 3,
    // Evaluated against the run's inventory (not the NPC relationship): true when the
    // player currently owns at least one container-type RunItem (e.g. a fiole).
    PlayerHasContainerItem = 4
}