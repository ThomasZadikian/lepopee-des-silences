using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;

namespace Leds.GameEngine.Application.Players;

/// <summary>
/// Enforces per-archetype skill equip restrictions ahead of the talent system: a skill
/// with a non-empty <c>AllowedArchetypes</c> list (see catalog <c>SkillDefinition</c>)
/// can only be equipped by a character whose <see cref="CharacterArchetypeProvider"/>
/// archetype is in that list. Skills with an empty list stay universally equippable —
/// this only restricts skills explicitly authored to belong to one archetype (e.g. a
/// companion's signature move), closing the loophole where an item's GrantSkill effect
/// or a Grimoire consumable could teach ANY character ANY skill regardless of role.
/// </summary>
public sealed class SkillArchetypeGate
{
    private readonly ICatalogContentGateway _catalogGateway;

    public SkillArchetypeGate(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task EnsureCanEquipAsync(
        string? characterDefinitionKey, string skillKey, CancellationToken cancellationToken)
    {
        var skill = await _catalogGateway.GetSkillDefinitionByKeyAsync(skillKey, cancellationToken);
        if (skill is null || skill.AllowedArchetypes is not { Count: > 0 } allowedArchetypes)
        {
            return;
        }

        var archetype = CharacterArchetypeProvider.Resolve(characterDefinitionKey);
        if (archetype == CharacterArchetypeProvider.Adaptive)
        {
            return;
        }

        if (!allowedArchetypes.Contains(archetype, StringComparer.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                $"'{skill.DisplayName}' n'est pas compatible avec cet archétype ({archetype}).");
        }
    }
}
