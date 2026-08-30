using System.Security.Cryptography;
using System.Text;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Domain.Combats.Tactical;

namespace Leds.GameEngine.Application.Combats.Tactical;

/// <summary>
/// Active abilities supplied by equipped Palace objects. Their deterministic ids let the
/// existing item-selection UI address them without pretending they are consumables in the
/// shared bag.
/// </summary>
public static class TacticalEquipmentAbilityCatalog
{
    public sealed record Ability(
        Guid Id,
        string ItemKey,
        string BehaviorCode,
        string DisplayName,
        int Range,
        TacticalAreaShape Shape,
        bool RequiresLineOfSight,
        string TargetingType,
        string UseKey);

    private static readonly IReadOnlyDictionary<string, (string Name, int Range, TacticalAreaShape Shape, bool Los, string Target)> Definitions =
        new Dictionary<string, (string, int, TacticalAreaShape, bool, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["tactical-extend-periodic-duration"] = (
                "Aiguille du Relieur", 999, TacticalAreaShape.Single, true, "SingleEnemy"),
            ["tactical-temporal-slow"] = (
                "Aiguille d'arrêt", 999, TacticalAreaShape.Map, false, "AllEnemies"),
            ["tactical-mind-control"] = (
                "Iris améthyste", 2, TacticalAreaShape.Diamond, true, "SingleEnemy"),
        };

    public static IReadOnlyCollection<CombatUsableItemDto> GetUsable(TacticalCombat combat)
    {
        if (combat.ActiveCombatantId is not { } actorId)
            return [];

        return Definitions
            .Where(pair => combat.TryGetEquipmentBehaviorSource(actorId, pair.Key, out _))
            .Select(pair =>
            {
                combat.TryGetEquipmentBehaviorSource(actorId, pair.Key, out var itemKey);
                return Create(actorId, itemKey, pair.Key, pair.Value);
            })
            .Where(ability => !combat.HasUsedOnceSkill(ability.UseKey))
            .Select(ability => new CombatUsableItemDto(
                ability.Id,
                ability.ItemKey,
                ability.DisplayName,
                "EquipmentAbility",
                0,
                1,
                ability.TargetingType,
                ability.Range,
                ability.Shape.ToString(),
                ability.RequiresLineOfSight))
            .ToArray();
    }

    public static bool TryResolve(
        TacticalCombat combat,
        Guid actorId,
        Guid abilityId,
        out Ability ability)
    {
        foreach (var pair in Definitions)
        {
            if (!combat.TryGetEquipmentBehaviorSource(actorId, pair.Key, out var itemKey))
                continue;

            var candidate = Create(actorId, itemKey, pair.Key, pair.Value);
            if (candidate.Id == abilityId)
            {
                ability = candidate;
                return true;
            }
        }

        ability = null!;
        return false;
    }

    private static Ability Create(
        Guid actorId,
        string itemKey,
        string behaviorCode,
        (string Name, int Range, TacticalAreaShape Shape, bool Los, string Target) definition)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{actorId:N}:{itemKey}"));
        return new Ability(
            new Guid(bytes[..16]),
            itemKey,
            behaviorCode,
            definition.Name,
            definition.Range,
            definition.Shape,
            definition.Los,
            definition.Target,
            $"equipment:{behaviorCode}");
    }
}
