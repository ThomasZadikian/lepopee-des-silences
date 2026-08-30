namespace Leds.GameEngine.Domain.Dialogue;

/// <summary>
/// What makes an <see cref="AmbientConversation"/> eligible to fire — SFD Système global de
/// dialogues §6: PNJ-to-PNJ or monologue, triggered by equipment/progression/nearby NPCs/room/
/// micro-event.
/// </summary>
public enum AmbientTriggerType
{
    Equipment = 0,
    Progression = 1,
    NearbyNpc = 2,
    Room = 3,
    MicroEvent = 4,
}
