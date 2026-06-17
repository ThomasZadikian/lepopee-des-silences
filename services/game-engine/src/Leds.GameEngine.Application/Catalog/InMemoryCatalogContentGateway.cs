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
                SkillKeys: ["skill-shadow-strike-v1"]),
            ["enemy.threshold.echo"] = new EnemyTemplateSnapshot(
                Key: "enemy.threshold.echo",
                Name: "Écho",
                Description: "Un souvenir affaibli qui refuse de s'effacer.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 18,
                BaseAttack: 6,
                BaseDefense: 2,
                BaseSpeed: 5,
                Affinity: "Memory",
                SkillKeys: ["skill.basic.strike"]),
            ["enemy.threshold.splinter"] = new EnemyTemplateSnapshot(
                Key: "enemy.threshold.splinter",
                Name: "Éclat",
                Description: "Un fragment durci par le silence. Résistant mais lent.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 28,
                BaseAttack: 5,
                BaseDefense: 6,
                BaseSpeed: 3,
                Affinity: "Silence",
                SkillKeys: ["skill.basic.shield", "skill.basic.strike"]),
            ["enemy.threshold.whisper"] = new EnemyTemplateSnapshot(
                Key: "enemy.threshold.whisper",
                Name: "Murmure",
                Description: "Un souffle agressif qui transperce le silence.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 15,
                BaseAttack: 9,
                BaseDefense: 2,
                BaseSpeed: 7,
                Affinity: "Shadow",
                SkillKeys: ["skill.basic.strike", "skill.basic.swift"]),
            ["enemy.threshold.guardian-fragment"] = new EnemyTemplateSnapshot(
                Key: "enemy.threshold.guardian-fragment",
                Name: "Fragment Gardien",
                Description: "Un éclat protégé par une volonté persistante. Rare et résistant.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 35,
                BaseAttack: 6,
                BaseDefense: 7,
                BaseSpeed: 4,
                Affinity: "Void",
                SkillKeys: ["skill.basic.shield", "skill.basic.strike", "skill.basic.taunt"]),
            ["enemy.threshold.fracture"] = new EnemyTemplateSnapshot(
                Key: "enemy.threshold.fracture",
                Name: "Fracture",
                Description: "Une brèche dans la cohérence du seuil. Dangereuse et instable.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 22,
                BaseAttack: 8,
                BaseDefense: 4,
                BaseSpeed: 6,
                Affinity: "Chaos",
                SkillKeys: ["skill.basic.strike", "skill.basic.charge"])
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
                ImpactDomains: ["Generation", "Events", "Narrative"],
                EffectSetKey: "effect.law.silence-weight",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("ModifyGenerationWeight", "SelectionContext", 0.10m, "Flat", "UntilRunEnds", "Additive", null, 0, null, "generation.silence", null)
                ]),
            ["law-aegis-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-aegis-v1",
                Name: "Loi de l'Égide",
                Description: "La première garde du héros se renforce.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 20,
                ImpactDomains: ["Combat"],
                EffectSetKey: "effect.law.aegis",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("AddStartingGuard", "Run", 8m, "Flat", "UntilRunEnds", "Additive", null, 0, null, null, null)
                ]),
            ["law-siege-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-siege-v1",
                Name: "Loi du Siège",
                Description: "Les prochains affrontements gagnent en pression.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 30,
                ImpactDomains: ["Combat"],
                EffectSetKey: "effect.law.siege",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("ModifyDifficultyMultiplier", "Run", 0.10m, "Flat", "UntilRunEnds", "Additive", null, 0, null, null, null)
                ]),
            ["law-carnage-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-carnage-v1",
                Name: "Loi du Carnage",
                Description: "La puissance d'attaque du héros augmente.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 40,
                ImpactDomains: ["Combat"],
                EffectSetKey: "effect.law.carnage",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("ModifyAttackPower", "Run", 0.10m, "Flat", "UntilRunEnds", "Additive", null, 0, null, null, null)
                ]),
            ["law-tempest-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-tempest-v1",
                Name: "Loi de la Pluie",
                Description: "La Room actuelle est traversée par la Pluie.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 50,
                ImpactDomains: ["Combat"],
                EffectSetKey: "effect.law.climate-rain",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("ApplyRoomClimate", "CurrentRoom", 0m, "TagOnly", "UntilRoomEnds", "UniqueBySource", "Rain", 0, null, null, null)
                ]),
            ["law-hail-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-hail-v1",
                Name: "Loi de la Grêle",
                Description: "La Room actuelle est traversée par la Grêle.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 60,
                ImpactDomains: ["Combat"],
                EffectSetKey: "effect.law.climate-hail",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("ApplyRoomClimate", "CurrentRoom", 0m, "TagOnly", "UntilRoomEnds", "UniqueBySource", "Hail", 0, null, null, null)
                ]),
            ["law-drought-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-drought-v1",
                Name: "Loi de la Canicule",
                Description: "La Room actuelle est écrasée par la Canicule.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 70,
                ImpactDomains: ["Combat"],
                EffectSetKey: "effect.law.climate-heatwave",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("ApplyRoomClimate", "CurrentRoom", 0m, "TagOnly", "UntilRoomEnds", "UniqueBySource", "Heatwave", 0, null, null, null)
                ]),
            ["law-grey-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-grey-v1",
                Name: "Loi de la Grisaille",
                Description: "La Room actuelle est recouverte de Grisaille.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 80,
                ImpactDomains: ["Combat"],
                EffectSetKey: "effect.law.climate-grey",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("ApplyRoomClimate", "CurrentRoom", 0m, "TagOnly", "UntilRoomEnds", "UniqueBySource", "Grey", 0, null, null, null)
                ])
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

    public Task<IReadOnlyCollection<PalaceLawDefinitionSnapshot>> ListActivePalaceLawDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<PalaceLawDefinitionSnapshot>>(
            PalaceLawDefinitions.Values
                .Where(definition => string.Equals(definition.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .OrderBy(definition => definition.Priority)
                .ThenBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray());
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

    private static readonly IReadOnlyDictionary<string, CatalogEnemyDefinition> EnemyDefinitions =
        new Dictionary<string, CatalogEnemyDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["enemy.threshold.doubt-fragment"] = new CatalogEnemyDefinition(
                Key: "enemy.threshold.doubt-fragment",
                DisplayName: "Fragment de Doute",
                Description: "Un éclat de silence hésitant, première manifestation du Palais.",
                Archetype: "Fragile",
                CompatibleRoomTypes: ["Threshold"],
                BaseDifficulty: 1,
                MinRiskLevel: 1,
                MaxRiskLevel: 2,
                Tags: ["threshold", "fragile", "echo"],
                SkillKeys: ["skill.basic.strike"]),
            ["enemy.threshold.inner-resistance"] = new CatalogEnemyDefinition(
                Key: "enemy.threshold.inner-resistance",
                DisplayName: "Résistance Intérieure",
                Description: "La première défense du Palais contre les pèlerins.",
                Archetype: "Guard",
                CompatibleRoomTypes: ["Threshold"],
                BaseDifficulty: 2,
                MinRiskLevel: 2,
                MaxRiskLevel: 3,
                Tags: ["threshold", "guard", "will"],
                SkillKeys: ["skill.basic.strike", "skill.basic.shield"]),
            ["enemy.threshold.echo"] = new CatalogEnemyDefinition(
                Key: "enemy.threshold.echo",
                DisplayName: "Écho",
                Description: "Un souvenir affaibli qui refuse de s'effacer.",
                Archetype: "Fragile",
                CompatibleRoomTypes: ["Threshold", "Memory"],
                BaseDifficulty: 1,
                MinRiskLevel: 1,
                MaxRiskLevel: 2,
                Tags: ["threshold", "fragile", "echo"],
                SkillKeys: ["skill.basic.strike"]),
            ["enemy.threshold.splinter"] = new CatalogEnemyDefinition(
                Key: "enemy.threshold.splinter",
                DisplayName: "Éclat",
                Description: "Un fragment durci par le silence. Résistant mais lent.",
                Archetype: "Guard",
                CompatibleRoomTypes: ["Threshold"],
                BaseDifficulty: 2,
                MinRiskLevel: 1,
                MaxRiskLevel: 3,
                Tags: ["threshold", "guard", "resilient"],
                SkillKeys: ["skill.basic.shield", "skill.basic.strike"]),
            ["enemy.threshold.whisper"] = new CatalogEnemyDefinition(
                Key: "enemy.threshold.whisper",
                DisplayName: "Murmure",
                Description: "Un souffle agressif qui transperce le silence.",
                Archetype: "Skirmisher",
                CompatibleRoomTypes: ["Threshold"],
                BaseDifficulty: 2,
                MinRiskLevel: 1,
                MaxRiskLevel: 3,
                Tags: ["threshold", "skirmisher", "aggressive"],
                SkillKeys: ["skill.basic.strike", "skill.basic.swift"]),
            ["enemy.threshold.guardian-fragment"] = new CatalogEnemyDefinition(
                Key: "enemy.threshold.guardian-fragment",
                DisplayName: "Fragment Gardien",
                Description: "Un éclat protégé par une volonté persistante. Rare et résistant.",
                Archetype: "Guard",
                CompatibleRoomTypes: ["Threshold"],
                BaseDifficulty: 3,
                MinRiskLevel: 2,
                MaxRiskLevel: 4,
                Tags: ["threshold", "guard", "rare"],
                SkillKeys: ["skill.basic.shield", "skill.basic.strike", "skill.basic.taunt"]),
            ["enemy.threshold.fracture"] = new CatalogEnemyDefinition(
                Key: "enemy.threshold.fracture",
                DisplayName: "Fracture",
                Description: "Une brèche dans la cohérence du seuil. Dangereuse et instable.",
                Archetype: "Bruiser",
                CompatibleRoomTypes: ["Threshold", "Forest", "Rupture"],
                BaseDifficulty: 3,
                MinRiskLevel: 2,
                MaxRiskLevel: 4,
                Tags: ["threshold", "bruiser", "instable"],
                SkillKeys: ["skill.basic.strike", "skill.basic.charge"]),
            ["enemy.forest.rooted-regret"] = new CatalogEnemyDefinition(
                Key: "enemy.forest.rooted-regret",
                DisplayName: "Regret Enraciné",
                Description: "Une mémoire douloureuse qui refuse de disparaître.",
                Archetype: "Bruiser",
                CompatibleRoomTypes: ["Forest"],
                BaseDifficulty: 2,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["forest", "bruiser", "memory"],
                SkillKeys: ["skill.basic.strike", "skill.basic.charge"]),
            ["enemy.forest.whispering-branch"] = new CatalogEnemyDefinition(
                Key: "enemy.forest.whispering-branch",
                DisplayName: "Branche Murmurante",
                Description: "Les branches du Palais murmurent des secrets oubliés.",
                Archetype: "Support",
                CompatibleRoomTypes: ["Forest"],
                BaseDifficulty: 2,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["forest", "support", "whisper"],
                SkillKeys: ["skill.basic.heal", "skill.basic.strike"]),
            ["enemy.rupture.broken-thought"] = new CatalogEnemyDefinition(
                Key: "enemy.rupture.broken-thought",
                DisplayName: "Pensée Brisée",
                Description: "Un raisonnement interrompu par la Rupture.",
                Archetype: "Skirmisher",
                CompatibleRoomTypes: ["Rupture"],
                BaseDifficulty: 3,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["rupture", "skirmisher", "thought"],
                SkillKeys: ["skill.basic.strike", "skill.basic.swift"]),
            ["enemy.rupture.contradiction"] = new CatalogEnemyDefinition(
                Key: "enemy.rupture.contradiction",
                DisplayName: "Contradiction",
                Description: "Une impossibilité logique devenue agressive.",
                Archetype: "Disruptor",
                CompatibleRoomTypes: ["Rupture"],
                BaseDifficulty: 4,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["rupture", "disruptor", "paradox"],
                SkillKeys: ["skill.basic.strike", "skill.basic.disable"]),
            ["enemy.silence.mute-witness"] = new CatalogEnemyDefinition(
                Key: "enemy.silence.mute-witness",
                DisplayName: "Témoin Muet",
                Description: "Il observe sans jamais parler. Sa présence suffit.",
                Archetype: "Guard",
                CompatibleRoomTypes: ["Silence"],
                BaseDifficulty: 3,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["silence", "guard", "witness"],
                SkillKeys: ["skill.basic.shield", "skill.basic.strike"]),
            ["enemy.silence.absent-voice"] = new CatalogEnemyDefinition(
                Key: "enemy.silence.absent-voice",
                DisplayName: "Voix Absente",
                Description: "Un cri qui n'a jamais été poussé. Il pèse sur l'âme.",
                Archetype: "Disruptor",
                CompatibleRoomTypes: ["Silence"],
                BaseDifficulty: 4,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["silence", "disruptor", "voice"],
                SkillKeys: ["skill.basic.disable", "skill.basic.strike"]),
            ["enemy.memory.archived-wound"] = new CatalogEnemyDefinition(
                Key: "enemy.memory.archived-wound",
                DisplayName: "Blessure Archivée",
                Description: "Une douleur conservée dans les archives du Palais.",
                Archetype: "Bruiser",
                CompatibleRoomTypes: ["Memory"],
                BaseDifficulty: 4,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["memory", "bruiser", "wound"],
                SkillKeys: ["skill.basic.strike", "skill.basic.charge"]),
            ["enemy.memory.named-loss"] = new CatalogEnemyDefinition(
                Key: "enemy.memory.named-loss",
                DisplayName: "Perte Nommée",
                Description: "Chaque perte a un nom dans les archives du Palais.",
                Archetype: "Support",
                CompatibleRoomTypes: ["Memory"],
                BaseDifficulty: 4,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["memory", "support", "loss"],
                SkillKeys: ["skill.basic.heal", "skill.basic.buff"]),
            ["enemy.antechamber.door-keeper"] = new CatalogEnemyDefinition(
                Key: "enemy.antechamber.door-keeper",
                DisplayName: "Gardien de Porte",
                Description: "Il garde l'entrée de l'Antichambre. Il ne laisse passer personne.",
                Archetype: "Guard",
                CompatibleRoomTypes: ["Antechamber"],
                BaseDifficulty: 5,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["antechamber", "guard", "door"],
                SkillKeys: ["skill.basic.shield", "skill.basic.strike", "skill.basic.taunt"]),
            ["enemy.antechamber.last-refusal"] = new CatalogEnemyDefinition(
                Key: "enemy.antechamber.last-refusal",
                DisplayName: "Dernier Refus",
                Description: "Le dernier obstacle avant le Final. Il ne cédera pas.",
                Archetype: "Bruiser",
                CompatibleRoomTypes: ["Antechamber"],
                BaseDifficulty: 5,
                MinRiskLevel: 1,
                MaxRiskLevel: 5,
                Tags: ["antechamber", "bruiser", "final-stand"],
                SkillKeys: ["skill.basic.strike", "skill.basic.charge", "skill.basic.enrage"]),
            ["enemy.final.silent-double"] = new CatalogEnemyDefinition(
                Key: "enemy.final.silent-double",
                DisplayName: "Double Silencieux",
                Description: "Votre propre silence reflété par le Palais.",
                Archetype: "Elite",
                CompatibleRoomTypes: ["Final"],
                BaseDifficulty: 8,
                MinRiskLevel: 4,
                MaxRiskLevel: 5,
                Tags: ["final", "elite", "mirror"],
                SkillKeys: ["skill.basic.strike", "skill.basic.swift", "skill.basic.disable"]),
            ["enemy.final.last-echo"] = new CatalogEnemyDefinition(
                Key: "enemy.final.last-echo",
                DisplayName: "Dernier Écho",
                Description: "Le dernier son avant le silence éternel.",
                Archetype: "Elite",
                CompatibleRoomTypes: ["Final"],
                BaseDifficulty: 9,
                MinRiskLevel: 4,
                MaxRiskLevel: 5,
                Tags: ["final", "elite", "echo"],
                SkillKeys: ["skill.basic.strike", "skill.basic.heal", "skill.basic.buff"]),

            // ── Boss enemy definitions (MinRiskLevel = MaxRiskLevel = 5) ──────────────
            // Used by EncounterCompositionPolicy.SelectRoomBossEnemies for RoomBoss encounters.
            // Each has a high BaseDifficulty to ensure unambiguous selection.
            ["boss.threshold.warden"] = new CatalogEnemyDefinition(
                Key: "boss.threshold.warden",
                DisplayName: "Gardien du Seuil",
                Description: "Boss de la Room Threshold. Premier gardien de run.",
                Archetype: "Boss",
                CompatibleRoomTypes: ["Threshold"],
                BaseDifficulty: 10,
                MinRiskLevel: 5,
                MaxRiskLevel: 5,
                Tags: ["boss", "threshold"],
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.forest.rootbound-memory"] = new CatalogEnemyDefinition(
                Key: "boss.forest.rootbound-memory",
                DisplayName: "Gardien des Racines",
                Description: "Boss de la Room Forest. Mémoire organique du Palais.",
                Archetype: "Boss",
                CompatibleRoomTypes: ["Forest"],
                BaseDifficulty: 10,
                MinRiskLevel: 5,
                MaxRiskLevel: 5,
                Tags: ["boss", "forest"],
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.rupture.fractured-echo"] = new CatalogEnemyDefinition(
                Key: "boss.rupture.fractured-echo",
                DisplayName: "Fragment de Rupture",
                Description: "Boss de la Room Rupture. Instable et agressif.",
                Archetype: "Boss",
                CompatibleRoomTypes: ["Rupture"],
                BaseDifficulty: 10,
                MinRiskLevel: 5,
                MaxRiskLevel: 5,
                Tags: ["boss", "rupture"],
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.silence.mute-herald"] = new CatalogEnemyDefinition(
                Key: "boss.silence.mute-herald",
                DisplayName: "Voix Éteinte",
                Description: "Boss de la Room Silence. Systémique et mutique.",
                Archetype: "Boss",
                CompatibleRoomTypes: ["Silence"],
                BaseDifficulty: 10,
                MinRiskLevel: 5,
                MaxRiskLevel: 5,
                Tags: ["boss", "silence"],
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.memory.archivist"] = new CatalogEnemyDefinition(
                Key: "boss.memory.archivist",
                DisplayName: "Archiviste des Échos",
                Description: "Boss de la Room Memory. Lié au Tome et aux fragments.",
                Archetype: "Boss",
                CompatibleRoomTypes: ["Memory"],
                BaseDifficulty: 10,
                MinRiskLevel: 5,
                MaxRiskLevel: 5,
                Tags: ["boss", "memory"],
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.antechamber.last-door"] = new CatalogEnemyDefinition(
                Key: "boss.antechamber.last-door",
                DisplayName: "Gardien de l'Antichambre",
                Description: "Boss de la Room Antechamber. Avant-poste du Final.",
                Archetype: "Boss",
                CompatibleRoomTypes: ["Antechamber"],
                BaseDifficulty: 12,
                MinRiskLevel: 5,
                MaxRiskLevel: 5,
                Tags: ["boss", "antechamber"],
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.final.himlit"] = new CatalogEnemyDefinition(
                Key: "boss.final.himlit",
                DisplayName: "Him'Lit",
                Description: "Boss de la Room Final. Le silence originel.",
                Archetype: "Boss",
                CompatibleRoomTypes: ["Final"],
                BaseDifficulty: 15,
                MinRiskLevel: 5,
                MaxRiskLevel: 5,
                Tags: ["boss", "final"],
                SkillKeys: ["skill-boss-void-slam-v1"])
        };

    private static readonly IReadOnlyDictionary<string, CatalogSkillDefinition> SkillDefinitions =
        new Dictionary<string, CatalogSkillDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["skill.basic.strike"] = new CatalogSkillDefinition(
                Key: "skill.basic.strike",
                DisplayName: "Frappe",
                Description: "Une attaque de base qui inflige des degats legers a un ennemi.",
                SkillType: "Damage",
                TargetingType: "SingleEnemy",
                EffectType: "Damage",
                ManaCost: 5,
                ChargeCost: 0,
                BasePower: 10,
                Tags: ["basic", "damage"]),
            ["skill.basic.guard"] = new CatalogSkillDefinition(
                Key: "skill.basic.guard",
                DisplayName: "Garde",
                Description: "Une posture defensive qui reduit les degats subis pendant un tour.",
                SkillType: "Defense",
                TargetingType: "Self",
                EffectType: "Guard",
                ManaCost: 0,
                ChargeCost: 0,
                BasePower: 5,
                Tags: ["basic", "defense"]),
            ["skill.basic.weaken"] = new CatalogSkillDefinition(
                Key: "skill.basic.weaken",
                DisplayName: "Affaiblissement",
                Description: "Une malédiction qui reduit la puissance d'un ennemi.",
                SkillType: "Debuff",
                TargetingType: "SingleEnemy",
                EffectType: "Debuff",
                ManaCost: 4,
                ChargeCost: 0,
                BasePower: 0,
                Tags: ["basic", "debuff"]),
            ["skill.basic.disrupt"] = new CatalogSkillDefinition(
                Key: "skill.basic.disrupt",
                DisplayName: "Perturbation",
                Description: "Une interference qui desorganise les competences ennemies.",
                SkillType: "Debuff",
                TargetingType: "SingleEnemy",
                EffectType: "Debuff",
                ManaCost: 6,
                ChargeCost: 1,
                BasePower: 0,
                Tags: ["basic", "disrupt"]),
            ["skill.basic.focus"] = new CatalogSkillDefinition(
                Key: "skill.basic.focus",
                DisplayName: "Concentration",
                Description: "Un etat de focalisation qui augmente la puissance du prochain sort.",
                SkillType: "Buff",
                TargetingType: "Self",
                EffectType: "Buff",
                ManaCost: 2,
                ChargeCost: 0,
                BasePower: 0,
                Tags: ["basic", "buff"]),
            ["skill.basic.shield"] = new CatalogSkillDefinition(
                Key: "skill.basic.shield",
                DisplayName: "Bouclier",
                Description: "Un bouclier qui absorbe les degats pendant un tour.",
                SkillType: "Defense",
                TargetingType: "Self",
                EffectType: "Guard",
                ManaCost: 0,
                ChargeCost: 0,
                BasePower: 5,
                Tags: ["basic", "shield"]),
            ["skill.basic.heal"] = new CatalogSkillDefinition(
                Key: "skill.basic.heal",
                DisplayName: "Soin",
                Description: "Soigne un allié en restaurant ses points de vie.",
                SkillType: "Heal",
                TargetingType: "SingleAlly",
                EffectType: "Heal",
                ManaCost: 6,
                ChargeCost: 0,
                BasePower: 15,
                Tags: ["basic", "heal"]),
            ["skill.basic.charge"] = new CatalogSkillDefinition(
                Key: "skill.basic.charge",
                DisplayName: "Charge",
                Description: "Une charge puissante qui inflige des degats supplémentaires.",
                SkillType: "Damage",
                TargetingType: "SingleEnemy",
                EffectType: "Damage",
                ManaCost: 7,
                ChargeCost: 1,
                BasePower: 18,
                Tags: ["basic", "charge"]),
            ["skill.basic.swift"] = new CatalogSkillDefinition(
                Key: "skill.basic.swift",
                DisplayName: "Rapidité",
                Description: "Une attaque rapide qui peut frapper avant la réaction ennemie.",
                SkillType: "Damage",
                TargetingType: "SingleEnemy",
                EffectType: "Damage",
                ManaCost: 4,
                ChargeCost: 0,
                BasePower: 7,
                Tags: ["basic", "swift"]),
            ["skill.basic.disable"] = new CatalogSkillDefinition(
                Key: "skill.basic.disable",
                DisplayName: "Neutralisation",
                Description: "Une compétence qui désactive temporairement les capacités ennemies.",
                SkillType: "Debuff",
                TargetingType: "SingleEnemy",
                EffectType: "Debuff",
                ManaCost: 8,
                ChargeCost: 1,
                BasePower: 0,
                Tags: ["basic", "disable"]),
            ["skill.basic.taunt"] = new CatalogSkillDefinition(
                Key: "skill.basic.taunt",
                DisplayName: "Provocation",
                Description: "Force l'ennemi à cibler le lanceur.",
                SkillType: "Utility",
                TargetingType: "SingleEnemy",
                EffectType: "Utility",
                ManaCost: 3,
                ChargeCost: 0,
                BasePower: 0,
                Tags: ["basic", "taunt"]),
            ["skill.basic.enrage"] = new CatalogSkillDefinition(
                Key: "skill.basic.enrage",
                DisplayName: "Enragement",
                Description: "Augmente la puissance d'attaque au prix de la défense.",
                SkillType: "Buff",
                TargetingType: "Self",
                EffectType: "Buff",
                ManaCost: 5,
                ChargeCost: 1,
                BasePower: 0,
                Tags: ["basic", "enrage"]),
            ["skill.basic.buff"] = new CatalogSkillDefinition(
                Key: "skill.basic.buff",
                DisplayName: "Renforcement",
                Description: "Améliore les capacités d'un allié pour plusieurs tours.",
                SkillType: "Buff",
                TargetingType: "SingleAlly",
                EffectType: "Buff",
                ManaCost: 5,
                ChargeCost: 0,
                BasePower: 0,
                Tags: ["basic", "buff"]),
            ["skill-boss-void-slam-v1"] = new CatalogSkillDefinition(
                Key: "skill-boss-void-slam-v1",
                DisplayName: "Void Slam",
                Description: "Frappe du vide. Puissante attaque de boss.",
                SkillType: "Damage",
                TargetingType: "SingleEnemy",
                EffectType: "Damage",
                ManaCost: 0,
                ChargeCost: 1,
                BasePower: 14,
                Tags: ["boss", "damage", "void"])
        };

    public Task<CatalogSkillDefinition?> GetSkillDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult<CatalogSkillDefinition?>(null);
        }

        var definition = SkillDefinitions.GetValueOrDefault(key.Trim());
        return Task.FromResult(definition);
    }

    public Task<IReadOnlyCollection<CatalogSkillDefinition>> ListSkillDefinitionsByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0)
        {
            return Task.FromResult<IReadOnlyCollection<CatalogSkillDefinition>>([]);
        }

        var distinctKeys = keys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var results = SkillDefinitions.Values
            .Where(d => distinctKeys.Any(k =>
                string.Equals(d.Key, k, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CatalogSkillDefinition>>(results);
    }

    public Task<IReadOnlyCollection<CatalogSkillDefinition>> ListSkillDefinitionsByTypeAsync(
        string skillType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillType))
        {
            return Task.FromResult<IReadOnlyCollection<CatalogSkillDefinition>>([]);
        }

        var trimmed = skillType.Trim();
        var results = SkillDefinitions.Values
            .Where(d => string.Equals(d.SkillType, trimmed, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CatalogSkillDefinition>>(results);
    }

    public Task<CatalogEnemyDefinition?> GetEnemyDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult<CatalogEnemyDefinition?>(null);
        }

        var definition = EnemyDefinitions.GetValueOrDefault(key.Trim());
        return Task.FromResult(definition);
    }

    public Task<IReadOnlyCollection<CatalogEnemyDefinition>> ListEnemyDefinitionsByRoomTypeAsync(
        string roomType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomType))
        {
            return Task.FromResult<IReadOnlyCollection<CatalogEnemyDefinition>>([]);
        }

        var trimmed = roomType.Trim();
        var results = EnemyDefinitions.Values
            .Where(d => d.CompatibleRoomTypes.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CatalogEnemyDefinition>>(results);
    }

    public Task<IReadOnlyCollection<CatalogEnemyDefinition>> ListCompatibleEnemyDefinitionsAsync(
        string roomType,
        int riskLevel,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomType))
        {
            return Task.FromResult<IReadOnlyCollection<CatalogEnemyDefinition>>([]);
        }

        var trimmed = roomType.Trim();
        var results = EnemyDefinitions.Values
            .Where(d =>
                d.CompatibleRoomTypes.Contains(trimmed, StringComparer.OrdinalIgnoreCase) &&
                d.MinRiskLevel <= riskLevel &&
                riskLevel <= d.MaxRiskLevel)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CatalogEnemyDefinition>>(results);
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
