namespace Leds.GameEngine.Domain.Actors;

/// <summary>
/// Common runtime identity for every embodied character. It deliberately carries no movement
/// or combat behavior: RoomNpc and Combatant keep their specialized engines while sharing a
/// stable instance identity and Catalog definition boundary.
/// </summary>
public interface IActorInstance
{
    Guid ActorInstanceId { get; }

    string ActorDefinitionKey { get; }

    ActorKind ActorKind { get; }
}

public enum ActorKind
{
    Protagonist = 1,
    Companion = 2,
    Npc = 3,
    Enemy = 4
}
