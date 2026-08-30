namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// How an NPC reacts to a player's dialogue claim — SFD Système global de dialogues §5.3: the
/// NPC believes/doubts/detects a lie according to personality (authored thresholds), knowledge
/// (does the claim contradict a confirmed fact) and relation (<see cref="RelationshipAxis.Suspicion"/>).
/// </summary>
public enum LieAssessment
{
    /// <summary>Taken at face value.</summary>
    Believed = 0,

    /// <summary>Neither accepted nor challenged — the NPC is unsure.</summary>
    Doubted = 1,

    /// <summary>Recognized as false.</summary>
    Detected = 2,
}
