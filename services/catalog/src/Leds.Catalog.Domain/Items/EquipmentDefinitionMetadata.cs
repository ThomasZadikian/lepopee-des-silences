using System.Text.RegularExpressions;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Items;

public static partial class EquipmentDefinitionMetadata
{
    private static readonly HashSet<string> SlotKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "Head", "Neck", "Shoulders", "Cape", "Chest", "Wrist", "Hand", "Waist",
        "Legs", "Feet", "Ring", "Relic", "MainWeapon", "OffWeapon"
    };

    public static void Validate(
        IReadOnlyCollection<string> allowedSlots,
        string? uniqueEquipGroup,
        IReadOnlyCollection<string> proficiencyTags)
    {
        ArgumentNullException.ThrowIfNull(allowedSlots);
        ArgumentNullException.ThrowIfNull(proficiencyTags);

        foreach (var slot in allowedSlots)
        {
            if (!SlotKinds.Contains(slot))
                throw new DomainException($"Unknown equipment slot kind '{slot}'.");
        }

        if (!string.IsNullOrWhiteSpace(uniqueEquipGroup) && !TagPattern().IsMatch(uniqueEquipGroup))
            throw new DomainException($"Unique equip group '{uniqueEquipGroup}' is invalid.");

        foreach (var tag in proficiencyTags)
        {
            if (string.IsNullOrWhiteSpace(tag) || !TagPattern().IsMatch(tag))
                throw new DomainException($"Equipment proficiency tag '{tag}' is invalid.");
        }

        if (allowedSlots.Count != allowedSlots.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new DomainException("Allowed equipment slots must be unique.");
        if (proficiencyTags.Count != proficiencyTags.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new DomainException("Equipment proficiency tags must be unique.");
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();
}
