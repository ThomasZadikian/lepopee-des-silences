using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Catalog;

/// <summary>
/// Temporary in-memory implementation of the Catalog gateway.
/// It validates the Game Engine ↔ Catalog boundary before HTTP integration.
/// </summary>
public sealed class InMemoryCatalogContentGateway : ICatalogContentGateway
{
    private static readonly IReadOnlyDictionary<string, EnemyTemplateSnapshot> EnemyTemplates =
        new Dictionary<string, EnemyTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["enemy-shadow-v1"] = new EnemyTemplateSnapshot(
                Key: "enemy-shadow-v1",
                Name: "Shadow Test Enemy",
                Description: "Neutral test enemy used by the Game Engine catalog gateway.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 30,
                BaseAttack: 8,
                BaseDefense: 4,
                BaseSpeed: 6,
                Affinity: "Shadow",
                SkillKeys: ["skill-shadow-strike-v1"]),
            ["boss-threshold-guardian-v1"] = new EnemyTemplateSnapshot(
                Key: "boss-threshold-guardian-v1",
                Name: "Threshold Guardian",
                Description: "Room boss used by the Game Engine for boss encounters.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 50,
                BaseAttack: 10,
                BaseDefense: 6,
                BaseSpeed: 8,
                Affinity: "Void",
                SkillKeys: ["skill-boss-void-slam-v1"])
        };

    private static readonly IReadOnlyDictionary<string, SkillTemplateSnapshot> SkillTemplates =
        new Dictionary<string, SkillTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["skill-shadow-strike-v1"] = new SkillTemplateSnapshot(
                Key: "skill-shadow-strike-v1",
                Name: "Shadow Strike",
                Description: "Neutral test skill used by the Game Engine catalog gateway.",
                Version: "1.0.0",
                Status: "Active",
                SkillType: "Shadow",
                Power: 10,
                Cost: 1,
                CostType: "Charge",
                TargetingMode: "SingleEnemy",
                EffectTags: ["damage"]),
            ["skill-boss-void-slam-v1"] = new SkillTemplateSnapshot(
                Key: "skill-boss-void-slam-v1",
                Name: "Void Slam",
                Description: "Boss skill used by the Game Engine for boss encounters.",
                Version: "1.0.0",
                Status: "Active",
                SkillType: "Void",
                Power: 14,
                Cost: 1,
                CostType: "Charge",
                TargetingMode: "SingleEnemy",
                EffectTags: ["damage"])
        };

    private static readonly IReadOnlyDictionary<string, ItemTemplateSnapshot> ItemTemplates =
        new Dictionary<string, ItemTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["item-memory-fragment-v1"] = new ItemTemplateSnapshot(
                Key: "item-memory-fragment-v1",
                Name: "Memory Fragment",
                Description: "Neutral test item used by the Game Engine catalog gateway.",
                Version: "1.0.0",
                Status: "Active",
                ItemType: "RunResource",
                Rarity: "Common",
                IsTemporary: true,
                EffectTags: ["resource"])
        };

    private static readonly IReadOnlyDictionary<string, EventTemplateSnapshot> EventTemplates =
        new Dictionary<string, EventTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["event-combat-shadow-v1"] = new EventTemplateSnapshot(
                Key: "event-combat-shadow-v1",
                Name: "Shadow Combat Event",
                Description: "Neutral test event used by the Game Engine catalog gateway.",
                Version: "1.0.0",
                Status: "Active",
                Type: "Combat",
                DefaultOutcomeKind: "CombatStarted",
                MinRiskLevel: 5,
                MaxRiskLevel: 25,
                RequiresPlayerChoice: false,
                NarrativeTags: ["test", "combat"]),
            ["event-boss-threshold-guardian-v1"] = new EventTemplateSnapshot(
                Key: "event-boss-threshold-guardian-v1",
                Name: "Threshold Guardian Boss Event",
                Description: "Boss event used by the Game Engine for room boss encounters.",
                Version: "1.0.0",
                Status: "Active",
                Type: "RoomBoss",
                DefaultOutcomeKind: "BossEncounterStarted",
                MinRiskLevel: 10,
                MaxRiskLevel: 50,
                RequiresPlayerChoice: false,
                NarrativeTags: ["test", "boss"])
        };

    private static readonly IReadOnlyDictionary<string, PalaceLawDefinitionSnapshot> PalaceLawDefinitions =
        new Dictionary<string, PalaceLawDefinitionSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["law-silence-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-silence-v1",
                Name: "Silence Law",
                Description: "Neutral test law used by the Game Engine catalog gateway.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 10,
                ImpactDomains: ["Generation", "Events", "Narrative"])
        };

    public Task<Result<EnemyTemplateSnapshot>> GetEnemyTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            EnemyTemplates,
            key,
            "catalog.enemy_template_not_found",
            "Enemy template was not found."));
    }

    public Task<Result<SkillTemplateSnapshot>> GetSkillTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            SkillTemplates,
            key,
            "catalog.skill_template_not_found",
            "Skill template was not found."));
    }

    public Task<Result<ItemTemplateSnapshot>> GetItemTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            ItemTemplates,
            key,
            "catalog.item_template_not_found",
            "Item template was not found."));
    }

    public Task<Result<EventTemplateSnapshot>> GetEventTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            EventTemplates,
            key,
            "catalog.event_template_not_found",
            "Event template was not found."));
    }

    public Task<Result<PalaceLawDefinitionSnapshot>> GetPalaceLawDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            PalaceLawDefinitions,
            key,
            "catalog.palace_law_definition_not_found",
            "Palace law definition was not found."));
    }

    private static Result<TSnapshot> GetByKey<TSnapshot>(
        IReadOnlyDictionary<string, TSnapshot> source,
        string key,
        string errorCode,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<TSnapshot>.Failure(Error.Create(
                "catalog.key_required",
                "Catalog content key is required."));
        }

        return source.TryGetValue(key.Trim(), out var snapshot)
            ? Result<TSnapshot>.Success(snapshot)
            : Result<TSnapshot>.Failure(Error.Create(errorCode, errorMessage));
    }
}