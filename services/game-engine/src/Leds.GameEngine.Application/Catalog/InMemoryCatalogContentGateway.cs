using Leds.GameEngine.Application.Catalog;
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
            ["boss.threshold.warden-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.threshold.warden-v1",
                Name: "Gardien du Seuil",
                Description: "Boss de la Room Threshold. Premier gardien de run.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 50,
                BaseAttack: 10,
                BaseDefense: 6,
                BaseSpeed: 8,
                Affinity: "Void",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.forest.rootbound-memory-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.forest.rootbound-memory-v1",
                Name: "Gardien des Racines",
                Description: "Boss de la Room Forest. Mémoire organique du Palais.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 55,
                BaseAttack: 9,
                BaseDefense: 6,
                BaseSpeed: 7,
                Affinity: "Nature",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.rupture.fractured-echo-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.rupture.fractured-echo-v1",
                Name: "Fragment de Rupture",
                Description: "Boss de la Room Rupture. Instable et agressif.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 55,
                BaseAttack: 11,
                BaseDefense: 4,
                BaseSpeed: 9,
                Affinity: "Chaos",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.silence.mute-herald-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.silence.mute-herald-v1",
                Name: "Voix Éteinte",
                Description: "Boss de la Room Silence. Systémique, mutique, altère les règles.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 45,
                BaseAttack: 10,
                BaseDefense: 5,
                BaseSpeed: 9,
                Affinity: "Silence",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.memory.archivist-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.memory.archivist-v1",
                Name: "Archiviste des Échos",
                Description: "Boss de la Room Memory. Lié au Tome et aux fragments.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 50,
                BaseAttack: 9,
                BaseDefense: 6,
                BaseSpeed: 8,
                Affinity: "Memory",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.antechamber.last-door-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.antechamber.last-door-v1",
                Name: "Gardien de l'Antichambre",
                Description: "Boss de la Room Antechamber. Avant-poste du Final.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 52,
                BaseAttack: 10,
                BaseDefense: 5,
                BaseSpeed: 9,
                Affinity: "Void",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.final.himlit-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.final.himlit-v1",
                Name: "Him'Lit",
                Description: "Boss de la Room Final. Le silence originel.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 60,
                BaseAttack: 14,
                BaseDefense: 8,
                BaseSpeed: 10,
                Affinity: "Void",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["enemy-rare-v1"] = new EnemyTemplateSnapshot(
                Key: "enemy-rare-v1",
                Name: "Rare Entity",
                Description: "Rare enemy used by the Game Engine for rare combat encounters.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 40,
                BaseAttack: 9,
                BaseDefense: 5,
                BaseSpeed: 7,
                Affinity: "Void",
                SkillKeys: ["skill-shadow-strike-v1"])
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
            ["event-boss.threshold.warden-v1"] = new EventTemplateSnapshot(
                Key: "event-boss.threshold.warden-v1",
                Name: "Rencontre — Gardien du Seuil",
                Description: "Événement boss de la Room Threshold.",
                Version: "1.0.0",
                Status: "Active",
                Type: "RoomBoss",
                DefaultOutcomeKind: "BossEncounterStarted",
                MinRiskLevel: 10,
                MaxRiskLevel: 90,
                RequiresPlayerChoice: false,
                NarrativeTags: ["boss", "threshold"]),
            ["event-boss.forest.rootbound-memory-v1"] = new EventTemplateSnapshot(
                Key: "event-boss.forest.rootbound-memory-v1",
                Name: "Rencontre — Gardien des Racines",
                Description: "Événement boss de la Room Forest.",
                Version: "1.0.0",
                Status: "Active",
                Type: "RoomBoss",
                DefaultOutcomeKind: "BossEncounterStarted",
                MinRiskLevel: 10,
                MaxRiskLevel: 90,
                RequiresPlayerChoice: false,
                NarrativeTags: ["boss", "forest"]),
            ["event-boss.rupture.fractured-echo-v1"] = new EventTemplateSnapshot(
                Key: "event-boss.rupture.fractured-echo-v1",
                Name: "Rencontre — Fragment de Rupture",
                Description: "Événement boss de la Room Rupture.",
                Version: "1.0.0",
                Status: "Active",
                Type: "RoomBoss",
                DefaultOutcomeKind: "BossEncounterStarted",
                MinRiskLevel: 25,
                MaxRiskLevel: 90,
                RequiresPlayerChoice: false,
                NarrativeTags: ["boss", "rupture"]),
            ["event-boss.silence.mute-herald-v1"] = new EventTemplateSnapshot(
                Key: "event-boss.silence.mute-herald-v1",
                Name: "Rencontre — Voix Éteinte",
                Description: "Événement boss de la Room Silence.",
                Version: "1.0.0",
                Status: "Active",
                Type: "RoomBoss",
                DefaultOutcomeKind: "BossEncounterStarted",
                MinRiskLevel: 10,
                MaxRiskLevel: 90,
                RequiresPlayerChoice: false,
                NarrativeTags: ["boss", "silence"]),
            ["event-boss.memory.archivist-v1"] = new EventTemplateSnapshot(
                Key: "event-boss.memory.archivist-v1",
                Name: "Rencontre — Archiviste des Échos",
                Description: "Événement boss de la Room Memory.",
                Version: "1.0.0",
                Status: "Active",
                Type: "RoomBoss",
                DefaultOutcomeKind: "BossEncounterStarted",
                MinRiskLevel: 10,
                MaxRiskLevel: 90,
                RequiresPlayerChoice: false,
                NarrativeTags: ["boss", "memory"]),
            ["event-boss.antechamber.last-door-v1"] = new EventTemplateSnapshot(
                Key: "event-boss.antechamber.last-door-v1",
                Name: "Rencontre — Gardien de l'Antichambre",
                Description: "Événement boss de la Room Antechamber.",
                Version: "1.0.0",
                Status: "Active",
                Type: "RoomBoss",
                DefaultOutcomeKind: "BossEncounterStarted",
                MinRiskLevel: 10,
                MaxRiskLevel: 90,
                RequiresPlayerChoice: false,
                NarrativeTags: ["boss", "antechamber"]),
            ["event-boss.final.himlit-v1"] = new EventTemplateSnapshot(
                Key: "event-boss.final.himlit-v1",
                Name: "Rencontre — Him'Lit",
                Description: "Événement boss de la Room Final. Le silence originel.",
                Version: "1.0.0",
                Status: "Active",
                Type: "RoomBoss",
                DefaultOutcomeKind: "BossEncounterStarted",
                MinRiskLevel: 30,
                MaxRiskLevel: 100,
                RequiresPlayerChoice: false,
                NarrativeTags: ["boss", "final"]),
            ["event-rare-encounter-v1"] = new EventTemplateSnapshot(
                Key: "event-rare-encounter-v1",
                Name: "Rare Encounter Event",
                Description: "Rare encounter event used by the Game Engine for rare combat encounters.",
                Version: "1.0.0",
                Status: "Active",
                Type: "Rare",
                DefaultOutcomeKind: "RareCombatStarted",
                MinRiskLevel: 10,
                MaxRiskLevel: 40,
                RequiresPlayerChoice: false,
                NarrativeTags: ["test", "rare"])
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

    private static readonly IReadOnlyDictionary<string, CatalogRoomBossProfile> RoomBossProfiles =
        new Dictionary<string, CatalogRoomBossProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["Threshold"] = new CatalogRoomBossProfile(
                Key: "boss.threshold.warden",
                DisplayName: "Gardien du Seuil",
                Description: "Premier gardien de la run. Veille sur le seuil du Palais des Silences.",
                RoomType: "Threshold",
                BaseDifficulty: 70,
                Tags: ["boss", "threshold", "guardian"]),
            ["Forest"] = new CatalogRoomBossProfile(
                Key: "boss.forest.rootbound-memory",
                DisplayName: "Gardien des Racines",
                Description: "Mémoire organique du Palais. Ses racines plongent dans les silences oubliés.",
                RoomType: "Forest",
                BaseDifficulty: 45,
                Tags: ["boss", "forest", "nature"]),
            ["Rupture"] = new CatalogRoomBossProfile(
                Key: "boss.rupture.fractured-echo",
                DisplayName: "Fragment de Rupture",
                Description: "Instable et agressif. Une brèche dans la cohérence du Palais.",
                RoomType: "Rupture",
                BaseDifficulty: 65,
                Tags: ["boss", "rupture", "chaos"]),
            ["Silence"] = new CatalogRoomBossProfile(
                Key: "boss.silence.mute-herald",
                DisplayName: "Voix Éteinte",
                Description: "Systémique, mutique. Altère les règles de la pièce par sa seule présence.",
                RoomType: "Silence",
                BaseDifficulty: 50,
                Tags: ["boss", "silence", "void"]),
            ["Antechamber"] = new CatalogRoomBossProfile(
                Key: "boss.antechamber.last-door",
                DisplayName: "Gardien de l'Antichambre",
                Description: "Avant-poste du Final. Aucun pèlerin n'a franchi cette porte.",
                RoomType: "Antechamber",
                BaseDifficulty: 85,
                Tags: ["boss", "antechamber", "elite"]),
            ["Memory"] = new CatalogRoomBossProfile(
                Key: "boss.memory.archivist",
                DisplayName: "Archiviste des Échos",
                Description: "Lié au Tome et aux fragments de mémoire. Connaît chaque silence.",
                RoomType: "Memory",
                BaseDifficulty: 40,
                Tags: ["boss", "memory", "lore"]),
            ["Final"] = new CatalogRoomBossProfile(
                Key: "boss.final.himlit",
                DisplayName: "Him'Lit",
                Description: "Le silence originel. La source du Palais.",
                RoomType: "Final",
                BaseDifficulty: 100,
                Tags: ["boss", "final", "himlit"])
        };

    public Task<CatalogRoomBossProfile?> GetRoomBossProfileAsync(
        string roomType,
        CancellationToken cancellationToken = default)
    {
        var profile = string.IsNullOrWhiteSpace(roomType)
            ? null
            : RoomBossProfiles.GetValueOrDefault(roomType.Trim());
        return Task.FromResult(profile);
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