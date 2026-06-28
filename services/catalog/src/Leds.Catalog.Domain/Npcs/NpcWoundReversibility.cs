namespace Leds.Catalog.Domain.Npcs;

// Authored per wound (decision Q23a): some wounds never re-close within a run.
public enum NpcWoundReversibility
{
    Irreversible = 0,
    SoothableByScore = 1,
    SoothableByAct = 2
}