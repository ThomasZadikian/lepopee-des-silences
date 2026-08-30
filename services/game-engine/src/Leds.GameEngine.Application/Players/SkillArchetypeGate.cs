using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;

namespace Leds.GameEngine.Application.Players;

/// <summary>
/// Enforces per-archetype skill equip restrictions ahead of the talent system: a skill
/// with a non-empty <c>AllowedArchetypes</c> list (see catalog <c>SkillDefinition</c>)
/// can only be equipped by a character whose Catalog-authored archetype is in that list.
/// Skills with an empty list stay universally equippable —
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

        var characters = await _catalogGateway.ListCharacterCombatDefinitionsAsync(cancellationToken);
        var character = characters.SingleOrDefault(definition => string.Equals(
            definition.DefinitionKey, characterDefinitionKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new ConflictException(
                $"Aucune définition Catalog n'existe pour le personnage '{characterDefinitionKey}'.");

        var archetype = character.CombatArchetypeCode;
        if (string.Equals(archetype, "Adaptive", StringComparison.OrdinalIgnoreCase))
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
