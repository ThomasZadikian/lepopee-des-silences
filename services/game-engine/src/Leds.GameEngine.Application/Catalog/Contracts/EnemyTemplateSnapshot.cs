namespace Leds.GameEngine.Application.Catalog.Contracts;

/// <summary>
/// Runtime-oriented snapshot of an enemy template owned by the Catalog service.
/// This contract belongs to the Game Engine and must not reference Catalog domain entities.
/// </summary>
public sealed record EnemyTemplateSnapshot(
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    int BaseHealth,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string Affinity,
    IReadOnlyCollection<string> SkillKeys);