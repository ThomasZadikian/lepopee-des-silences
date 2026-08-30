using Leds.Catalog.Domain.Items;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed partial class CatalogSeedRunner
{
    private const string CanonicalWeaponVersion = "trpg-weapons-1.0.0";

    private async Task SeedCanonicalWeaponsAsync(CancellationToken cancellationToken)
    {
        foreach (var weapon in CanonicalWeapons)
        {
            ItemRarityCatalog.Parse(weapon.Rarity);

            var item = await _ctx.ItemDefinitions
                .FirstOrDefaultAsync(i => i.Key == weapon.Key, cancellationToken);

            if (item is null)
            {
                item = new ItemDefinitionEntity
                {
                    Id = Guid.NewGuid(),
                    Key = weapon.Key,
                    CreatedAtUtc = _now
                };
                _ctx.ItemDefinitions.Add(item);
            }

            item.Name = weapon.Name;
            item.DisplayName = weapon.Name;
            item.Description = weapon.Description;
            item.NarrativeText = weapon.Description;
            item.Version = CanonicalWeaponVersion;
            item.Status = "Active";
            item.Category = nameof(ItemCategory.Weapon);
            item.FlavorTag = "Arme";
            item.Rarity = weapon.Rarity;
            item.UsageMode = "Equip";
            item.Lifecycle = "PersistentMeta";
            item.StackPolicy = "None";
            item.MaxStack = 1;
            item.IsUsableInCombat = false;
            item.IsUsableOutsideCombat = false;
            item.MinDepth = weapon.MinDepth;
            item.MaxDepth = null;
            item.BaseWeight = weapon.BaseWeight;
            item.SelectionGroup = "trpg-weapons";
            item.Duration = "Permanent";
            item.EffectValue = 0;
            item.EffectRunType = null;
            item.TacticalRange = weapon.Range;
            item.TacticalAreaShape = weapon.Shape;
            item.RequiresLineOfSight = weapon.RequiresLineOfSight;
            item.BasicAttackPower = weapon.Power;
            item.BasicAttackCategory = weapon.Category;
            item.Price = weapon.Price;
            item.EquipmentEffectsJson = weapon.EquipmentEffectsJson;
            item.IsContainer = false;
            item.ContainerCapacity = null;
            item.IsLiquid = false;
            item.ReadablePagesJson = "[]";
            item.UpdatedAtUtc = _now;
        }
    }

    private static readonly IReadOnlyList<CanonicalWeaponSeed> CanonicalWeapons =
    [
        new(
            "weapon.lame-seuil",
            "Lame du Seuil",
            "Une arme équilibrée, sûre et directe.",
            "Common", 18, "Physical", 1, "Single", false, 0, 120, 80, "[]"),
        new(
            "weapon.dagues-murmure",
            "Dagues du Murmure",
            "Des lames courtes qui privilégient le Focus et les ouvertures de dos.",
            "Uncommon", 15, "Physical", 1, "Single", false, 1, 90, 125,
            """[{"Kind":"StatBonus","StatKind":"Focus","Amount":4,"SkillKey":null,"AffinityRegister":null}]"""),
        new(
            "weapon.lance-pelerin",
            "Lance du Pèlerin",
            "Une hampe longue capable d'atteindre une cible à deux cases.",
            "Uncommon", 17, "Physical", 2, "Single", true, 1, 85, 130, "[]"),
        new(
            "weapon.espadon-cendre",
            "Espadon de Cendre",
            "Une lame lourde dont le balayage couvre une croix rapprochée.",
            "Rare", 25, "Physical", 1, "Cross", false, 2, 55, 220,
            """[{"Kind":"StatBonusPercent","StatKind":"Speed","Amount":-10,"SkillKey":null,"AffinityRegister":null}]"""),
        new(
            "weapon.arc-souvenir",
            "Arc du Souvenir",
            "Une arme de précision conçue pour exploiter la hauteur.",
            "Common", 16, "Physical", 5, "Single", true, 0, 100, 95, "[]"),
        new(
            "weapon.arbalete-protocole",
            "Arbalète du Protocole",
            "Un trait lent mais puissant, destiné aux cibles isolées.",
            "Rare", 23, "Physical", 4, "Single", true, 2, 50, 210,
            """[{"Kind":"StatBonusPercent","StatKind":"Speed","Amount":-5,"SkillKey":null,"AffinityRegister":null}]"""),
        new(
            "weapon.baton-resonance",
            "Bâton de Résonance",
            "Un foyer magique stable pour les attaques à moyenne portée.",
            "Common", 17, "Magic", 4, "Single", true, 0, 100, 100, "[]"),
        new(
            "weapon.grimoire-orage",
            "Grimoire de l'Orage",
            "Une arme magique rare dont la décharge éclate en croix.",
            "Epic", 24, "Magic", 4, "Cross", true, 3, 30, 320,
            """[{"Kind":"StatBonusPercent","StatKind":"MagicAttack","Amount":10,"SkillKey":null,"AffinityRegister":null}]""")
    ];

    private sealed record CanonicalWeaponSeed(
        string Key,
        string Name,
        string Description,
        string Rarity,
        int Power,
        string Category,
        int Range,
        string Shape,
        bool RequiresLineOfSight,
        int MinDepth,
        int BaseWeight,
        int Price,
        string EquipmentEffectsJson);
}
