using System.Text.Json;
using Leds.Catalog.Application.Archetypes;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed partial class CatalogSeedRunner
{
    private async Task SeedArchetypesAsync(CancellationToken cancellationToken)
    {
        await UpsertItemAsync(
            "item.starter.porteur.weapon", "Lame du Porteur", "Arme initiale du Porteur.",
            "Weapon", "Arme", "Common", "Permanent", false, 0, cancellationToken,
            allowedSlots: ["MainWeapon"], proficiencyTags: ["light-weapon"]);
        await UpsertItemAsync(
            "item.starter.porteur.cape", "Cape du Porteur", "Cape initiale du Porteur.",
            "Equipment", "Starter", "Common", "Permanent", false, 0, cancellationToken,
            allowedSlots: ["Cape"], proficiencyTags: ["cloth"]);
        await UpsertItemAsync(
            "item.starter.porteur.chest", "Tunique du Porteur", "Torse initial du Porteur.",
            "Equipment", "Starter", "Common", "Permanent", false, 0, cancellationToken,
            allowedSlots: ["Chest"], proficiencyTags: ["cloth"]);
        await UpsertItemAsync(
            "item.starter.porteur.hand", "Gants du Porteur", "Gants initiaux du Porteur.",
            "Equipment", "Starter", "Common", "Permanent", false, 0, cancellationToken,
            allowedSlots: ["Hand"], proficiencyTags: ["cloth"]);
        await UpsertItemAsync(
            "item.starter.porteur.legs", "Pantalon du Porteur", "Jambières initiales du Porteur.",
            "Equipment", "Starter", "Common", "Permanent", false, 0, cancellationToken,
            allowedSlots: ["Legs"], proficiencyTags: ["cloth"]);
        await UpsertItemAsync(
            "item.starter.porteur.feet", "Chaussures du Porteur", "Chaussures initiales du Porteur.",
            "Equipment", "Starter", "Common", "Permanent", false, 0, cancellationToken,
            allowedSlots: ["Feet"], proficiencyTags: ["cloth"]);

        var entity = await _ctx.ArchetypeDefinitions
            .FirstOrDefaultAsync(item => item.Key == "archetype.porteur", cancellationToken);
        var isNew = entity is null;
        entity ??= new ArchetypeDefinitionEntity
        {
            Id = Guid.NewGuid(),
            Key = "archetype.porteur",
            CreatedAtUtc = _now
        };
        if (isNew) _ctx.ArchetypeDefinitions.Add(entity);

        entity.DisplayName = "Le Porteur";
        entity.Description = "Archétype polyvalent actuellement validé par le projet.";
        entity.Version = "archetypes-1.0.0";
        entity.Status = "Active";
        entity.BaseStatsJson = JsonSerializer.Serialize(new ArchetypeBaseStatsDto(
            MaxVitality: 100, AttackPower: 12, MagicAttack: 6, Defense: 6, MagicDefense: 3,
            StartingGuard: 0, Speed: 10, Initiative: 10, Focus: 0, Mana: 85, Charge: 0, Movement: 4), J);
        entity.ProficiencyTagsJson = JsonSerializer.Serialize(new[] { "cloth", "light-weapon" }, J);
        entity.StarterEquipmentJson = JsonSerializer.Serialize(new[]
        {
            new StarterEquipmentDto("item.starter.porteur.weapon", "MainWeapon"),
            new StarterEquipmentDto("item.starter.porteur.cape", "Cape"),
            new StarterEquipmentDto("item.starter.porteur.chest", "Chest"),
            new StarterEquipmentDto("item.starter.porteur.hand", "Hand"),
            new StarterEquipmentDto("item.starter.porteur.legs", "Legs"),
            new StarterEquipmentDto("item.starter.porteur.feet", "Feet")
        }, J);
        entity.StarterKnownSkillsJson = JsonSerializer.Serialize(new[] { "skill.basic.guard" }, J);
        entity.StarterEquippedSkillsJson = JsonSerializer.Serialize(new[] { "skill.basic.guard" }, J);
        entity.UpdatedAtUtc = _now;
    }
}
