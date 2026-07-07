using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.UnitTests.Common;

public sealed class StubCatalogContentGateway : ICatalogContentGateway
{
    private static readonly IReadOnlyDictionary<string, EnemyTemplateSnapshot> EnemyTemplates =
        new Dictionary<string, EnemyTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["enemy-shadow-v1"] = new EnemyTemplateSnapshot(
                Key: "enemy-shadow-v1",
                Name: "Shadow",
                Description: "A basic shadow enemy",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 30,
                BaseAttack: 8,
                BaseDefense: 3,
                BaseSpeed: 5,
                Affinity: "Neutral",
                SkillKeys: ["skill.basic.strike"]),
            ["enemy-void-walker-v1"] = new EnemyTemplateSnapshot(
                Key: "enemy-void-walker-v1",
                Name: "Void Walker",
                Description: "A void-infused walker",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 45,
                BaseAttack: 12,
                BaseDefense: 5,
                BaseSpeed: 4,
                Affinity: "Void",
                SkillKeys: ["skill.basic.strike", "skill.basic.shield"]),
            ["enemy-rare-v1"] = new EnemyTemplateSnapshot(
                Key: "enemy-rare-v1",
                Name: "Rare Echo",
                Description: "A rare combat apparition",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 55,
                BaseAttack: 14,
                BaseDefense: 5,
                BaseSpeed: 6,
                Affinity: "Neutral",
                SkillKeys: ["skill.basic.strike"]),
            ["boss.threshold.warden-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.threshold.warden-v1",
                Name: "Gardien du Seuil",
                Description: "Premier gardien de la run. Veille sur le seuil du Palais des Silences.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 120,
                BaseAttack: 18,
                BaseDefense: 10,
                BaseSpeed: 6,
                Affinity: "Neutral",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.forest.rootbound-memory-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.forest.rootbound-memory-v1",
                Name: "Gardien des Racines",
                Description: "Mémoire organique du Palais. Ses racines plongent dans les silences oubliés.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 100,
                BaseAttack: 14,
                BaseDefense: 8,
                BaseSpeed: 5,
                Affinity: "Nature",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.rupture.fractured-echo-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.rupture.fractured-echo-v1",
                Name: "Fragment de Rupture",
                Description: "Instable et agressif. Une brèche dans la cohérence du Palais.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 110,
                BaseAttack: 20,
                BaseDefense: 6,
                BaseSpeed: 8,
                Affinity: "Chaos",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.silence.mute-herald-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.silence.mute-herald-v1",
                Name: "Voix Éteinte",
                Description: "Systémique, mutique. Altère les règles de la pièce par sa seule présence.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 90,
                BaseAttack: 16,
                BaseDefense: 12,
                BaseSpeed: 4,
                Affinity: "Void",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.memory.archivist-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.memory.archivist-v1",
                Name: "Archiviste des Échos",
                Description: "Lié au Tome et aux fragments de mémoire. Connaît chaque silence.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 95,
                BaseAttack: 15,
                BaseDefense: 9,
                BaseSpeed: 6,
                Affinity: "Neutral",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.antechamber.last-door-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.antechamber.last-door-v1",
                Name: "Gardien de l'Antichambre",
                Description: "Avant-poste du Final. Aucun pèlerin n'a franchi cette porte.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 140,
                BaseAttack: 22,
                BaseDefense: 14,
                BaseSpeed: 5,
                Affinity: "Neutral",
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["boss.final.himlit-v1"] = new EnemyTemplateSnapshot(
                Key: "boss.final.himlit-v1",
                Name: "Him'Lit",
                Description: "Le silence originel. La source du Palais.",
                Version: "1.0.0",
                Status: "Active",
                BaseHealth: 200,
                BaseAttack: 28,
                BaseDefense: 18,
                BaseSpeed: 7,
                Affinity: "Void",
                SkillKeys: ["skill-boss-void-slam-v1"])
        };

    private static readonly IReadOnlyDictionary<string, SkillTemplateSnapshot> SkillTemplates =
        new Dictionary<string, SkillTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["skill-boss-void-slam-v1"] = new SkillTemplateSnapshot(
                Key: "skill-boss-void-slam-v1",
                Name: "Void Slam",
                Description: "Frappe du vide. Puissante attaque de boss.",
                Version: "1.0.0",
                Status: "Active",
                SkillType: "Damage",
                Power: 14,
                Cost: 1,
                CostType: "Charge",
                TargetingMode: "SingleEnemy",
                EffectTags: ["boss", "damage", "void"])
        };

    private static readonly IReadOnlyDictionary<string, ItemTemplateSnapshot> ItemTemplates =
        new Dictionary<string, ItemTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["item-memory-fragment-v1"] = new ItemTemplateSnapshot(
                Key: "item-memory-fragment-v1",
                Name: "Fragment de Mémoire",
                Description: "Un éclat de souvenir. Peut être utilisé pour restaurer un peu de vitalité.",
                Version: "1.0.0",
                Status: "Active",
                ItemType: "Consumable",
                Rarity: "Common",
                IsTemporary: true,
                EffectTags: ["heal", "memory"]),
            ["canon.item.lanterne"] = new ItemTemplateSnapshot(
                Key: "canon.item.lanterne",
                Name: "Lanterne à huile",
                Description: "Seules les chaumières éclairées ne furent pas touchées. La lumière est un abri.",
                Version: "canon-1.0.0",
                Status: "Active",
                ItemType: "Consumable",
                Rarity: "Common",
                IsTemporary: true,
                EffectTags: ["light"])
        };

    private static readonly IReadOnlyDictionary<string, EventTemplateSnapshot> EventTemplates =
        new Dictionary<string, EventTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["event-combat-shadow-v1"] = new EventTemplateSnapshot(
                Key: "event-combat-shadow-v1",
                Name: "Combat Shadow",
                Description: "A basic shadow combat encounter",
                Version: "1.0.0",
                Status: "Active",
                Type: "Combat",
                DefaultOutcomeKind: "Combat",
                MinRiskLevel: 1,
                MaxRiskLevel: 3,
                RequiresPlayerChoice: false,
                NarrativeTags: ["combat", "shadow"]),
            ["event-rare-encounter-v1"] = EventTemplate("event-rare-encounter-v1", "Rare Encounter", "Rare"),
            ["event-boss.threshold.warden-v1"] = EventTemplate("event-boss.threshold.warden-v1", "Threshold Warden", "RoomBoss"),
            ["event-boss.forest.rootbound-memory-v1"] = EventTemplate("event-boss.forest.rootbound-memory-v1", "Rootbound Memory", "RoomBoss"),
            ["event-boss.rupture.fractured-echo-v1"] = EventTemplate("event-boss.rupture.fractured-echo-v1", "Fractured Echo", "RoomBoss"),
            ["event-boss.silence.mute-herald-v1"] = EventTemplate("event-boss.silence.mute-herald-v1", "Mute Herald", "RoomBoss"),
            ["event-boss.memory.archivist-v1"] = EventTemplate("event-boss.memory.archivist-v1", "Archivist", "RoomBoss"),
            ["event-boss.antechamber.last-door-v1"] = EventTemplate("event-boss.antechamber.last-door-v1", "Last Door", "RoomBoss"),
            ["event-boss.final.himlit-v1"] = EventTemplate("event-boss.final.himlit-v1", "Him'Lit", "RoomBoss")
        };

    private static EventTemplateSnapshot EventTemplate(string key, string name, string type)
    {
        return new EventTemplateSnapshot(
            Key: key,
            Name: name,
            Description: name,
            Version: "1.0.0",
            Status: "Active",
            Type: type,
            DefaultOutcomeKind: type,
            MinRiskLevel: 1,
            MaxRiskLevel: 5,
            RequiresPlayerChoice: false,
            NarrativeTags: [type.ToLowerInvariant()]);
    }

    private static readonly IReadOnlyDictionary<string, PalaceLawDefinitionSnapshot> PalaceLawDefinitions =
        new Dictionary<string, PalaceLawDefinitionSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["law-silence-v1"] = new PalaceLawDefinitionSnapshot(
                Key: "law-silence-v1",
                Name: "Loi du Silence",
                Description: "Le silence alourdit chaque action.",
                Version: "1.0.0",
                Status: "Active",
                Visibility: "Visible",
                Priority: 10,
                ImpactDomains: ["Combat"],
                EffectSetKey: "effectset.law-silence-v1",
                Effects:
                [
                    new CatalogEffectDefinitionSnapshot("ModifyDifficultyMultiplier", "Run", 0.10m, "Flat", "UntilRunEnds", "Additive", null, 0, null, null, null),
                    new CatalogEffectDefinitionSnapshot("ModifyRewardPowerMultiplier", "Run", 0.15m, "Flat", "UntilRunEnds", "Additive", null, 0, null, null, null)
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

    private static readonly IReadOnlyDictionary<string, CatalogCurseDefinitionSnapshot> CurseDefinitions =
        new Dictionary<string, CatalogCurseDefinitionSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["curse.old-wound"] = new CatalogCurseDefinitionSnapshot(
                "curse.old-wound",
                "1.0.0",
                "Vieille blessure",
                "Une blessure ancienne qui se rouvre au plus mauvais moment.",
                "Le corps porte ses propres souvenirs.",
                3,
                "NextCombatOnly",
                null,
                "effectset.curse-old-wound"),
            ["curse.weight-of-silence"] = new CatalogCurseDefinitionSnapshot(
                "curse.weight-of-silence",
                "1.0.0",
                "Poids du silence",
                "Le silence devient une charge mentale supplémentaire.",
                null,
                5,
                "NextCombatOnly",
                null,
                "effectset.curse-weight-of-silence")
        };

    private static readonly IReadOnlyDictionary<string, CatalogItemDefinitionSnapshot> ItemDefinitions =
        new Dictionary<string, CatalogItemDefinitionSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["item.consumable.minor-heal"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.minor-heal",
                "1.0",
                "Baume de mémoire",
                "Restaure une partie de la vitalité.",
                null,
                "Consumable",
                "Heal",
                "Common",
                "UseInCombat",
                "RuntimeRunOnly",
                "Additive",
                99,
                true,
                true,
                null),
            ["item.consumable.guard-shard"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.guard-shard",
                "1.0",
                "Éclat de garde",
                "Offre une protection permanente pendant la run.",
                null,
                "Consumable",
                "Guard",
                "Uncommon",
                "UseInCombat",
                "RuntimeRunOnly",
                "Additive",
                99,
                true,
                false,
                null),
            ["item.consumable.eclat-de-garde"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.eclat-de-garde",
                "alpha-0.8.1",
                "Eclat de garde",
                "Renforce la garde initiale du prochain combat.",
                null,
                "Consumable",
                "Guard",
                "Common",
                "UseOnNode",
                "RuntimeRunOnly",
                "Additive",
                3,
                false,
                true,
                "effect.item.eclat-de-garde"),
            ["item.consumable.peau-de-serpent"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.peau-de-serpent",
                "1.0",
                "Peau de serpent",
                "Une mue encore souple, qui renforce la garde du prochain combat.",
                null,
                "Consumable",
                "Guard",
                "Common",
                "UseOnNode",
                "RuntimeRunOnly",
                "Additive",
                3,
                false,
                true,
                null),
            ["item.consumable.crocs-figes"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.crocs-figes",
                "1.0",
                "Crocs figes",
                "Des crocs durcis, encore charges de venin residuel.",
                null,
                "Consumable",
                "Damage",
                "Uncommon",
                "UseInCombat",
                "RuntimeRunOnly",
                "Additive",
                3,
                true,
                false,
                null),
            ["item.consumable.venin-cristallise"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.venin-cristallise",
                "1.0",
                "Venin cristallise",
                "Un venin fige en cristal, rare et instable.",
                null,
                "Consumable",
                "Damage",
                "Rare",
                "UseInCombat",
                "RuntimeRunOnly",
                "Additive",
                2,
                true,
                false,
                null),
            ["item.consumable.fragment-de-silence"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.fragment-de-silence",
                "1.0",
                "Fragment de silence",
                "Un eclat muet, souvenir d'un temoin qui n'a jamais parle.",
                null,
                "Consumable",
                "Memory",
                "Uncommon",
                "NotUsable",
                "RuntimeRunOnly",
                "Additive",
                3,
                false,
                false,
                null),
            ["item.consumable.oeil-de-verre"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.oeil-de-verre",
                "1.0",
                "Oeil de verre",
                "Un oeil fige qui semble encore observer.",
                null,
                "Consumable",
                "Focus",
                "Rare",
                "UseOnNode",
                "RuntimeRunOnly",
                "Additive",
                2,
                false,
                true,
                null),
            ["item.consumable.eclat-instable"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.eclat-instable",
                "1.0",
                "Eclat instable",
                "Un fragment de la Fracture, encore charge d'energie.",
                null,
                "Consumable",
                "Damage",
                "Uncommon",
                "UseInCombat",
                "RuntimeRunOnly",
                "Additive",
                3,
                true,
                false,
                null),
            ["item.consumable.reliquaire-fele"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.reliquaire-fele",
                "1.0",
                "Reliquaire fele",
                "Un reliquaire fendu qui protege malgre tout.",
                null,
                "Consumable",
                "Guard",
                "Epic",
                "UseOnNode",
                "RuntimeRunOnly",
                "Additive",
                1,
                false,
                true,
                null),
            ["item.consumable.poussiere-de-couloir"] = new CatalogItemDefinitionSnapshot(
                "item.consumable.poussiere-de-couloir",
                "1.0",
                "Poussiere de couloir",
                "Une poussiere banale, sans valeur particuliere.",
                null,
                "Consumable",
                "Narrative",
                "Common",
                "NotUsable",
                "RuntimeRunOnly",
                "Additive",
                5,
                false,
                false,
                null)
        };

    private static readonly IReadOnlyDictionary<string, CatalogEffectSetSnapshot> EffectSets =
        new Dictionary<string, CatalogEffectSetSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["effectset.law-silence-v1"] = EffectSet("effectset.law-silence-v1",
                Effect("ModifyDifficultyMultiplier", "Run", 0.10m, "UntilRunEnds", order: 1),
                Effect("ModifyRewardPowerMultiplier", "Run", 0.15m, "UntilRunEnds", order: 2)),
            ["effectset.law-fracture-v1"] = EffectSet("effectset.law-fracture-v1",
                Effect("AddStartingGuard", "Run", 15m, "UntilRunEnds")),
            ["effectset.curse-old-wound"] = EffectSet("effectset.curse-old-wound",
                Effect("ModifyDifficultyMultiplier", "NextCombat", 0.10m, "NextCombatOnly")),
            ["effectset.curse-weight-of-silence"] = EffectSet("effectset.curse-weight-of-silence",
                Effect("ModifyDifficultyMultiplier", "NextCombat", 0.20m, "NextCombatOnly")),
            ["effect.item.minor-heal"] = EffectSet("effect.item.minor-heal",
                Effect("HealVitality", "Self", 15m, "Immediate", stackPolicy: "None")),
            ["effect.item.eclat-de-garde"] = EffectSet("effect.item.eclat-de-garde",
                Effect("AddStartingGuard", "NextCombat", 8m, "UntilRunEnds"))
        };

    private static readonly IReadOnlyDictionary<string, CatalogRewardTemplateSnapshot> RewardTemplates =
        new Dictionary<string, CatalogRewardTemplateSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["reward.combat.default"] = RewardTemplate("reward.combat.default", "Récompense de combat", "Récompense standard après un combat.", "Combat", [
                RewardOption("Heal", "Soin léger", "Soin léger", null, null, 10),
                RewardOption("Heal", "Souffle retrouvé", "Souffle retrouvé", null, null, 14),
                RewardOption("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", 15)]),
            ["reward.combat.rare"] = RewardTemplate("reward.combat.rare", "Récompense rare", "Récompense pour un combat rare.", "Rare", [
                RewardOption("Heal", "Soin rare", "Soin rare", null, null, 15),
                RewardOption("Heal", "Répit lucide", "Répit lucide", null, null, 20),
                RewardOption("Heal", "Soin substantiel", "Soin substantiel", null, null, 25)]),
            ["reward.combat.elite"] = RewardTemplate("reward.combat.elite", "Récompense élite", "Récompense pour un combat élite.", "Elite", [
                RewardOption("Heal", "Soin important", "Soin important", null, null, 20),
                RewardOption("Heal", "Volonté restaurée", "Volonté restaurée", null, null, 28),
                RewardOption("Heal", "Suture mentale", "Suture mentale", null, null, 36)]),
            ["reward.combat.boss"] = RewardTemplate("reward.combat.boss", "Récompense de boss", "Récompense pour avoir vaincu un boss.", "RoomBoss", [
                RewardOption("Heal", "Soin majeur", "Soin majeur", null, null, 30),
                RewardOption("Heal", "Souffle du Gardien", "Souffle du Gardien", null, null, 42),
                RewardOption("Heal", "Silence recomposé", "Silence recomposé", null, null, 54)]),
            ["reward.item.default"] = RewardTemplate("reward.item.default", "Récompense d'objet", "Récompense d'un noeud objet.", "NodeEvent", [
                RewardOption("TemporaryItem", "Éclat de garde", "Éclat de garde", "item.consumable.guard-shard", "Item", 8),
                RewardOption("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", 15),
                RewardOption("Heal", "Souffle du passé", "Souffle du passé", null, null, 10)]),
            ["reward.merchant.default"] = RewardTemplate("reward.merchant.default", "Offre du marchand", "Récompense proposée par un marchand.", "NodeEvent", [
                RewardOption("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", 15),
                RewardOption("TemporaryItem", "Éclat de garde", "Éclat de garde", "item.consumable.guard-shard", "Item", 8),
                RewardOption("Heal", "Soin du marchand", "Soin du marchand", null, null, 12)])
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

    public Task<Result<CatalogCurseDefinitionSnapshot>> GetCurseDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            CurseDefinitions,
            key,
            "catalog.curse_definition_not_found",
            "Curse definition was not found."));
    }

    public Task<IReadOnlyCollection<CatalogCurseDefinitionSnapshot>> ListAvailableCurseDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<CatalogCurseDefinitionSnapshot>>(
            CurseDefinitions.Values
                .OrderBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    public Task<Result<CatalogItemDefinitionSnapshot>> GetItemDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            ItemDefinitions,
            key,
            "catalog.item_definition_not_found",
            "Item definition was not found."));
    }

    public Task<Result<CatalogEffectSetSnapshot>> GetEffectSetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            EffectSets,
            key,
            "catalog.effect_set_not_found",
            "Effect set was not found."));
    }

    public Task<Result<CatalogRewardTemplateSnapshot>> GetRewardTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetByKey(
            RewardTemplates,
            key,
            "catalog.reward_template_not_found",
            "Reward template was not found."));
    }

    public Task<IReadOnlyCollection<CatalogRewardTemplateSnapshot>> ListEligibleRewardTemplatesAsync(
        RewardTemplateEligibilityContext context,
        CancellationToken cancellationToken = default)
    {
        var eligible = RewardTemplates.Values
            .Where(template => template.SourceType == context.SourceType)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CatalogRewardTemplateSnapshot>>(eligible);
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
                SkillKeys: ["skill-boss-void-slam-v1"]),
            ["enemy.forest.chimere-serpentaire"] = new CatalogEnemyDefinition(
                Key: "enemy.forest.chimere-serpentaire",
                DisplayName: "Chimere Serpentaire",
                Description: "Une creature composite qui rampe entre les racines de la foret.",
                Archetype: "Beast",
                CompatibleRoomTypes: ["Forest", "Threshold"],
                BaseDifficulty: 1,
                MinRiskLevel: 1,
                MaxRiskLevel: 30,
                Tags: ["forest", "beast"],
                SkillKeys: ["skill.basic.strike"])
        };

    private static readonly IReadOnlyDictionary<string, CatalogEnemyLootTable> EnemyLootTables =
        new Dictionary<string, CatalogEnemyLootTable>(StringComparer.OrdinalIgnoreCase)
        {
            ["enemy.forest.chimere-serpentaire"] = new CatalogEnemyLootTable(
                "loot.enemy.forest.chimere-serpentaire",
                "enemy.forest.chimere-serpentaire",
                "Butin de la Chimere Serpentaire",
                "Ce que laisse une chimere serpentaire vaincue.",
                "1.0",
                [
                    new CatalogLootEntry("item.consumable.peau-de-serpent", 45),
                    new CatalogLootEntry("item.consumable.crocs-figes", 25),
                    new CatalogLootEntry("item.consumable.minor-heal", 30),
                    new CatalogLootEntry("item.consumable.venin-cristallise", 8)
                ]),
            ["enemy.silence.mute-witness"] = new CatalogEnemyLootTable(
                "loot.enemy.silence.mute-witness",
                "enemy.silence.mute-witness",
                "Butin du Temoin Muet",
                "Ce que laisse un temoin muet vaincu.",
                "1.0",
                [
                    new CatalogLootEntry("item.consumable.eclat-de-garde", 40),
                    new CatalogLootEntry("item.consumable.fragment-de-silence", 35),
                    new CatalogLootEntry("item.consumable.minor-heal", 20),
                    new CatalogLootEntry("item.consumable.oeil-de-verre", 10)
                ]),
            ["enemy.threshold.fracture"] = new CatalogEnemyLootTable(
                "loot.enemy.threshold.fracture",
                "enemy.threshold.fracture",
                "Butin de Fracture",
                "Ce que laisse une Fracture vaincue.",
                "1.0",
                [
                    new CatalogLootEntry("item.consumable.eclat-instable", 50),
                    new CatalogLootEntry("item.consumable.eclat-de-garde", 30),
                    new CatalogLootEntry("item.consumable.reliquaire-fele", 12)
                ])
        };

    private static readonly CatalogGenericLootPool GenericLootPool = new(
        "loot.generic.fallback",
        "Trouvailles du Palais",
        "Ce que le Palais offre quand le butin d'un ennemi ne suffit pas.",
        "1.0",
        [
            new CatalogLootEntry("item.consumable.minor-heal", 60),
            new CatalogLootEntry("item.consumable.eclat-de-garde", 40),
            new CatalogLootEntry("item.consumable.poussiere-de-couloir", 25)
        ]);

    private static readonly IReadOnlyDictionary<string, CatalogNpcDefinition> NpcDefinitions =
        new Dictionary<string, CatalogNpcDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["npc-neutral-traveler"] = new CatalogNpcDefinition(
                Key: "npc-neutral-traveler",
                DisplayName: "Voyageur Neutre",
                Description: "Un vagabond sans attache qui traverse les couloirs du Palais.",
                Tags: ["generic", "neutral"],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: []),
            ["npc-silent-witness"] = new CatalogNpcDefinition(
                Key: "npc-silent-witness",
                DisplayName: "Témoin Silencieux",
                Description: "Une silhouette figée dans le silence, observatrice immuable.",
                Tags: ["silence", "witness"],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [PalaceRoomState.Silent],
                CompatibleRoomClimates: []),
            ["npc-desert-exile"] = new CatalogNpcDefinition(
                Key: "npc-desert-exile",
                DisplayName: "Exilé du Désert",
                Description: "Un habitant des sables brûlants, marqué par la chaleur éternelle.",
                Tags: ["desert", "exile"],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: ["Heatwave"]),
            ["npc-rage-beggar"] = new CatalogNpcDefinition(
                Key: "npc-rage-beggar",
                DisplayName: "Mendiant de Rage",
                Description: "Un être consumé par une colère ancienne, prêt à exploser.",
                Tags: ["rage", "aggressive"],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [PalaceRoomState.Enraged],
                CompatibleRoomClimates: []),
            ["npc-rain-memory-keeper"] = new CatalogNpcDefinition(
                Key: "npc-rain-memory-keeper",
                DisplayName: "Gardien des Mémoires de Pluie",
                Description: "Un archiviste dont les souvenirs s'écoulent comme la pluie.",
                Tags: ["rain", "memory"],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: ["Rain"]),
            ["npc.test.offering-giver"] = new CatalogNpcDefinition(
                Key: "npc.test.offering-giver",
                DisplayName: "Donneuse de Test",
                Description: "PNJ de test portant des offres mécaniques.",
                Tags: [],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: [],
                BoundRoomKeys: ["room.room08"],
                Offerings:
                [
                    new CatalogNpcOffering("offer.skill", "Skill", "skill.basic.strike", 0, IsMajor: true, UnlockConditions: []),
                    new CatalogNpcOffering("offer.stat", "StatPoint", null, 1, IsMajor: false, UnlockConditions: []),
                    new CatalogNpcOffering("offer.item", "Item", "item.consumable.minor-heal", 1, IsMajor: false, UnlockConditions: []),
                    new CatalogNpcOffering("offer.skill.gated", "Skill", "skill.basic.strike", 0, IsMajor: true,
                        UnlockConditions: [new CatalogDialogueRequirement("RelationshipScoreAtLeast", null, null, null, 5)])
                ],
                DialogueGraph: new CatalogNpcDialogueGraph(
                    "npc.test.offering-giver.dialogue", "1.0", "start",
                    new Dictionary<string, CatalogNpcDialogueNode>
                    {
                        ["start"] = new CatalogNpcDialogueNode(
                            "start", "Donneuse de Test",
                            ["Que veux-tu ?"],
                            [
                                new CatalogNpcDialogueChoice(
                                    "take-skill", "Prendre la compétence",
                                    [], [new CatalogDialogueConsequence(
                                        "GrantOffering", null, null, null, null, 0, null, null, "offer.skill", null, null, null)],
                                    null),
                                new CatalogNpcDialogueChoice(
                                    "take-stat", "Prendre le point de compétence",
                                    [], [new CatalogDialogueConsequence(
                                        "GrantOffering", null, null, null, null, 0, null, null, "offer.stat", null, null, null)],
                                    null),
                                new CatalogNpcDialogueChoice(
                                    "take-item", "Prendre l'objet",
                                    [], [new CatalogDialogueConsequence(
                                        "GrantOffering", null, null, null, null, 0, null, null, "offer.item", null, null, null)],
                                    null),
                                new CatalogNpcDialogueChoice(
                                    "take-milestone", "Sceller un souvenir",
                                    [], [new CatalogDialogueConsequence(
                                        "PersistReputationMilestone", null, null, null, null, 0, "trust-earned", null, null, null, null, null)],
                                    null),
                                new CatalogNpcDialogueChoice(
                                    "take-gated-skill", "Prendre la compétence réservée à la confiance",
                                    [], [new CatalogDialogueConsequence(
                                        "GrantOffering", null, null, null, null, 0, null, null, "offer.skill.gated", null, null, null)],
                                    null)
                            ])
                    })),
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

    public Task<IReadOnlyCollection<CatalogNpcDefinition>> ListNpcDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        var results = NpcDefinitions.Values.ToArray();
        return Task.FromResult<IReadOnlyCollection<CatalogNpcDefinition>>(results);
    }

    public Task<IReadOnlyCollection<CatalogRewardCursePool>> ListRewardCursePoolsAsync(
    CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<CatalogRewardCursePool>>([]);
    }
    public Task<IReadOnlyCollection<CatalogRoomDefinition>> ListRoomDefinitionsAsync(
    CancellationToken cancellationToken = default)
    => Task.FromResult<IReadOnlyCollection<CatalogRoomDefinition>>([]);

    public Task<IReadOnlyCollection<CatalogWorldDefinition>> ListWorldDefinitionsAsync(
    CancellationToken cancellationToken = default)
    => Task.FromResult<IReadOnlyCollection<CatalogWorldDefinition>>([]);

    public Task<IReadOnlyCollection<CatalogRoomThemeAffinity>> ListRoomThemeAffinitiesAsync(
    CancellationToken cancellationToken = default)
    => Task.FromResult<IReadOnlyCollection<CatalogRoomThemeAffinity>>([]);

    public Task<IReadOnlyCollection<CatalogNpcReputationAffinity>> ListNpcReputationAffinitiesAsync(
    CancellationToken cancellationToken = default)
    => Task.FromResult<IReadOnlyCollection<CatalogNpcReputationAffinity>>([]);

    public Task<IReadOnlyCollection<CatalogRoomTypeDefinition>> ListRoomTypeDefinitionsAsync(
    CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<CatalogRoomTypeDefinition>>(
[
    new CatalogRoomTypeDefinition("room-type.threshold", "Seuil", "Threshold", 0, int.MaxValue),
            new CatalogRoomTypeDefinition("room-type.memory", "Mémoire", "Memory", 1, int.MaxValue),
            new CatalogRoomTypeDefinition("room-type.forest", "Forêt", "Forest", 1, int.MaxValue),
            new CatalogRoomTypeDefinition("room-type.rupture", "Rupture", "Rupture", 1, int.MaxValue),
            new CatalogRoomTypeDefinition("room-type.silence", "Silence", "Silence", 1, int.MaxValue),
            new CatalogRoomTypeDefinition("room-type.antechamber", "Antichambre", "Antechamber", 1, int.MaxValue),
        ]);
    }

    public Task<CatalogEnemyLootTable?> GetEnemyLootTableByKeyAsync(
        string enemyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(enemyKey))
        {
            return Task.FromResult<CatalogEnemyLootTable?>(null);
        }

        var table = EnemyLootTables.GetValueOrDefault(enemyKey.Trim());
        return Task.FromResult(table);
    }

    public Task<CatalogGenericLootPool?> GetActiveGenericLootPoolAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<CatalogGenericLootPool?>(GenericLootPool);
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

    private static CatalogEffectSetSnapshot EffectSet(
        string key,
        params CatalogEffectDefinitionSnapshot[] effects)
    {
        return new CatalogEffectSetSnapshot(key, "1.0.0", effects);
    }

    private static CatalogEffectDefinitionSnapshot Effect(
        string effectType,
        string targetScope,
        decimal value,
        string duration,
        string valueMode = "Flat",
        string stackPolicy = "Additive",
        string? condition = null,
        int order = 0,
        string? behaviorTag = null,
        string? generationTag = null,
        string? selectionGroup = null)
    {
        return new CatalogEffectDefinitionSnapshot(
            effectType,
            targetScope,
            value,
            valueMode,
            duration,
            stackPolicy,
            condition,
            order,
            behaviorTag,
            generationTag,
            selectionGroup);
    }

    private static CatalogRewardTemplateSnapshot RewardTemplate(
        string key,
        string displayName,
        string description,
        string sourceType,
        IReadOnlyCollection<CatalogRewardTemplateOptionSnapshot> options)
    {
        return new CatalogRewardTemplateSnapshot(key, "1.0", displayName, description, sourceType, 2, 3, options);
    }


    private static CatalogRewardTemplateOptionSnapshot RewardOption(
        string rewardType,
        string label,
        string description,
        string? payloadKey,
        string? payloadType,
        int baseAmount)
    {
        return new CatalogRewardTemplateOptionSnapshot(
            rewardType,
            label,
            description,
            payloadKey,
            payloadType,
            null,
            baseAmount,
            "Flat",
            1);
    }
}
