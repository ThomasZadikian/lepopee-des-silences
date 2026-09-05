using System.Text.Json;
using Leds.Catalog.Domain.Items;
using Leds.Catalog.Domain.Rewards.Loot;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed partial class CatalogSeedRunner
{
    private const string PalaceItemVersion = "palace-items-1.0.0";

    private async Task SeedPalaceItemsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in PalaceItems)
        {
            await UpsertPalaceItemAsync(definition, cancellationToken);
        }
    }

    private async Task SeedPalaceItemLootAsync(CancellationToken cancellationToken)
    {
        foreach (var table in PalaceLootTables)
        {
            await UpsertEnemyLootTableAsync(
                table.EnemyKey,
                $"Butin de {table.DisplayName}",
                $"Objets laissés par {table.DisplayName}. Les Éclats et points de compétence sont crédités séparément.",
                table.Entries.Select(e => new LootEntry(e.ItemKey, e.Percent)).ToArray(),
                cancellationToken);
        }
    }

    private async Task UpsertPalaceItemAsync(
        PalaceItemSeed definition,
        CancellationToken cancellationToken)
    {
        ItemTypeCatalog.Parse(definition.Category);
        ItemRarityCatalog.Parse(definition.Rarity);

        var item = await _ctx.ItemDefinitions
            .FirstOrDefaultAsync(i => i.Key == definition.Key, cancellationToken);
        var isNew = item is null;
        item ??= new ItemDefinitionEntity { Id = Guid.NewGuid(), Key = definition.Key, CreatedAtUtc = _now };
        if (isNew)
        {
            _ctx.ItemDefinitions.Add(item);
        }

        // definition.Effects also feeds DeriveAlwaysOnEquipmentEffects below (the live,
        // actually-read equip-time bonus system) — it isn't dead data, only its former
        // second destination (a per-item EffectSet/EffectDefinition row set) was: nothing
        // ever read it back for an item, EffectValue/EffectRunType already being the sole
        // source of truth for what a used item does (see ItemDefinitionEntity.EffectRunType).
        var equipmentEffects = (definition.EquipmentEffects ?? [])
            .Concat(DeriveAlwaysOnEquipmentEffects(definition))
            .ToArray();
        var equipmentEffectsJson = JsonSerializer.Serialize(equipmentEffects, J);

        item.Name = definition.Name;
        item.DisplayName = definition.Name;
        item.Description = definition.Description;
        item.NarrativeText = definition.Description;
        item.Version = PalaceItemVersion;
        item.Status = "Active";
        item.Category = definition.Category;
        item.FlavorTag = definition.FlavorTag;
        item.Rarity = definition.Rarity;
        item.UsageMode = definition.UsageMode;
        item.Lifecycle = definition.Lifecycle;
        item.StackPolicy = definition.MaxStack > 1 ? "Additive" : "None";
        item.MaxStack = definition.MaxStack;
        item.IsUsableInCombat = definition.IsUsableInCombat;
        item.IsUsableOutsideCombat = definition.IsUsableOutsideCombat;
        item.MinDepth = definition.MinDepth;
        item.MaxDepth = null;
        item.BaseWeight = definition.BaseWeight;
        item.SelectionGroup = definition.SelectionGroup;
        item.Duration = definition.Lifecycle == "PersistentMeta" ? "Permanent" : "RunOnly";
        item.EffectValue = definition.EffectValue;
        item.EffectRunType = definition.EffectRunType;
        item.TacticalRange = definition.TacticalRange;
        item.TacticalAreaShape = definition.TacticalShape;
        item.RequiresLineOfSight = definition.RequiresLineOfSight;
        item.BasicAttackPower = definition.BasicAttackPower;
        item.BasicAttackCategory = definition.BasicAttackCategory;
        item.Price = 0;
        item.EquipmentEffectsJson = equipmentEffectsJson;
        var allowedSlots = ResolvePalaceItemAllowedSlots(definition.Key, definition.Category);
        EquipmentDefinitionMetadata.Validate(allowedSlots, null, []);
        item.AllowedSlotsJson = JsonSerializer.Serialize(allowedSlots, J);
        item.UniqueEquipGroup = null;
        item.ProficiencyTagsJson = "[]";
        item.IsContainer = false;
        item.ContainerCapacity = null;
        item.IsLiquid = false;
        item.ReadablePagesJson = "[]";
        item.UpdatedAtUtc = _now;
    }

    private static IReadOnlyList<string> ResolvePalaceItemAllowedSlots(string key, string category)
    {
        if (category == "Weapon") return ["MainWeapon"];
        if (category == "Relic") return ["Relic"];
        if (category != "Equipment") return [];

        return key switch
        {
            "item.gants-service-muet" or "item.gantelet-trempe" => ["Hand"],
            "item.epingle-protocole" => ["Chest"],
            "item.couronne-sel" or "item.cornes-ivoire" => ["Head"],
            _ => ["Relic"]
        };
    }

    private static IReadOnlyCollection<ItemEquipmentEffect> DeriveAlwaysOnEquipmentEffects(
        PalaceItemSeed definition)
    {
        if (definition.UsageMode is not ("Equip" or "Passive"))
            return [];

        var derived = new List<ItemEquipmentEffect>();
        foreach (var effect in definition.Effects.Where(effect =>
                     effect.Type == "StatModifier"
                     && effect.Target == "Wearer"
                     && effect.Condition is null
                     && effect.Value.HasValue
                     && effect.Duration == "WhileEquipped"))
        {
            derived.AddRange(DeriveStatEffects(effect, condition: null));
        }

        // Conditional bonuses (e.g. Boussole du Pèlerin's "+10% Vitesse dans la
        // Montagne", Couronne de sel's weather-gated magic attack) carry their raw
        // "room:X"/"weather:X" condition through to game-engine instead of being
        // baked into PlayerStatMerger's static run-start stats — CombatFactory
        // re-evaluates them fresh every combat against the current room/climate.
        foreach (var effect in definition.Effects.Where(effect =>
                     effect.Type == "StatModifier"
                     && effect.Target == "Wearer"
                     && effect.Condition is { } condition
                     && (condition.StartsWith("room:") || condition.StartsWith("weather:"))
                     && effect.Value.HasValue
                     && effect.Duration is "WhileEquipped" or "WhileWeatherActive"))
        {
            derived.AddRange(DeriveStatEffects(effect, effect.Condition));
        }

        // Encrier de poche's "+5% dégâts des effets périodiques [appliqués par le
        // porteur]" — the DamageOverTime-duration half of this item ("leur première
        // application dure une activation supplémentaire") is exposed separately as a
        // Catalog-authored RuntimeBehavior consumed by CombatSkillEffectResolver.
        foreach (var effect in definition.Effects.Where(effect =>
                     effect.Type == "PeriodicDamageModifier"
                     && effect.Target == "EffectsAppliedByWearer"
                     && effect.Value.HasValue
                     && effect.Duration == "WhileEquipped"))
        {
            derived.Add(new ItemEquipmentEffect(
                ItemEquipmentEffectKind.DotDamageBonusPercent,
                Amount: decimal.ToInt32(effect.Value!.Value)));
        }

        return derived;
    }

    private static IEnumerable<ItemEquipmentEffect> DeriveStatEffects(PalaceEffect effect, string? condition)
    {
        var kind = effect.ValueMode == "Percent"
            ? ItemEquipmentEffectKind.StatBonusPercent
            : ItemEquipmentEffectKind.StatBonus;
        var amount = decimal.ToInt32(effect.Value!.Value);
        string[] stats = effect.BehaviorTag switch
        {
            "attack" => ["AttackPower"],
            "defense" => ["Defense"],
            "speed" => ["Speed"],
            "focus" => ["Focus"],
            "magic-attack" => ["MagicAttack"],
            "magic-defense" => ["MagicDefense"],
            "mana" => ["Mana"],
            "vitality" => ["MaxVitality"],
            "all-stats" =>
            [
                "MaxVitality", "AttackPower", "Defense", "Speed",
                "Focus", "MagicAttack", "MagicDefense", "Mana"
            ],
            _ => Array.Empty<string>()
        };

        return stats.Select(stat => new ItemEquipmentEffect(kind, stat, amount, Condition: condition));
    }

    private static PalaceEffect Fx(
        string type,
        string target,
        decimal? value,
        string mode,
        string duration,
        string? condition = null,
        string stack = "Additive",
        string? behavior = null)
        => new(type, target, value, mode, duration, stack, condition, behavior);

    private static readonly IReadOnlyList<PalaceItemSeed> PalaceItems =
    [
        // Armes — elles remplacent le contrat de l'attaque de base.
        P("item.weapon.lame-seuil", "Lame du Seuil",
            "Une lame courte, fiable dans les couloirs étroits du Palais.",
            "Weapon", "Arme", "Common", "Equip", lifecycle: "PersistentMeta",
            range: 1, shape: "Single", los: false,
            basicAttackPower: 11, basicAttackCategory: "Physical"),
        P("item.weapon.lance-pelerin", "Lance du Pèlerin",
            "Une hampe longue qui permet de frapper au-delà de la première case.",
            "Weapon", "Arme", "Uncommon", "Equip", lifecycle: "PersistentMeta", pool: "Montagne",
            range: 2, shape: "Single", los: true,
            basicAttackPower: 10, basicAttackCategory: "Physical"),
        P("item.weapon.arc-relieur", "Arc du Relieur",
            "Ses traits suivent les lignes que le Palais accepte encore de montrer.",
            "Weapon", "Arme", "Uncommon", "Equip", lifecycle: "PersistentMeta", pool: "Labyrinthe",
            range: 5, shape: "Single", los: true,
            basicAttackPower: 9, basicAttackCategory: "Physical"),
        P("item.weapon.marteau-forge", "Marteau de la Forge",
            "Lent et brutal, conçu pour briser la Garde au contact.",
            "Weapon", "Arme", "Rare", "Equip", lifecycle: "PersistentMeta", pool: "Enfers",
            range: 1, shape: "Single", los: false,
            basicAttackPower: 14, basicAttackCategory: "Physical"),
        P("item.weapon.baton-silences", "Bâton des Silences",
            "Canalise une frappe magique à travers les salles du Palais.",
            "Weapon", "Arme", "Rare", "Equip", lifecycle: "PersistentMeta", pool: "Palier,Labyrinthe",
            range: 4, shape: "Single", los: true,
            basicAttackPower: 12, basicAttackCategory: "Magic"),

        // Accessoires
        P("item.gants-service-muet", "Gants du service muet",
            "+5% Défense ; +10% supplémentaires si aucune compétence magique n'a été utilisée à l'activation précédente.",
            "Equipment", "Accessory", "Common", "Equip", lifecycle: "PersistentMeta", effects:
            [
                Fx("StatModifier", "Wearer", 5, "Percent", "WhileEquipped", behavior: "defense"),
                Fx("StatModifier", "Wearer", 10, "Percent", "BearerActivation",
                    "previous-activation:no-magic-skill", behavior: "defense")
            ], equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "defense-after-no-magic")]),
        P("item.epingle-protocole", "Épingle du protocole",
            "Les affaiblissements appliqués par le porteur durent une activation supplémentaire.",
            "Equipment", "Accessory", "Uncommon", "Equip", lifecycle: "PersistentMeta", effects:
            [Fx("StatusDurationModifier", "HostileEffectsAppliedByWearer", 1, "BearerActivations", "WhileEquipped")],
            equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "hostile-status-duration-plus-one")]),
        P("item.encrier-poche", "Encrier de poche",
            "+5% dégâts des effets périodiques ; leur première application dure une activation supplémentaire.",
            "Equipment", "Accessory", "Uncommon", "Equip", lifecycle: "PersistentMeta", pool: "Palier,Labyrinthe", effects:
            [
                Fx("PeriodicDamageModifier", "EffectsAppliedByWearer", 5, "Percent", "WhileEquipped"),
                Fx("StatusDurationModifier", "PeriodicEffectsAppliedByWearer", 1, "BearerActivations", "first-stack")
            ], equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "first-dot-duration-plus-one")]),
        P("item.aiguille-relieur", "Aiguille du Relieur",
            "Une fois par combat, prolonge de 25% la durée restante de tous les DoT de l'équipe sur une cible.",
            "Equipment", "Accessory", "Rare", "UseInCombat", combat: true, range: 999, shape: "Single",
            pool: "room.labyrinthe", effects:
            [Fx("ExtendPeriodicDuration", "SingleCombatant", 25, "Percent", "Immediate", "once-per-combat", behavior: "round-up")],
            equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "tactical-extend-periodic-duration")]),
        P("item.gantelet-trempe", "Gantelet de trempe",
            "+10% Attaque ; chaque buff d'Attaque reçu accorde +2 Défense pour la même durée.",
            "Equipment", "Accessory", "Uncommon", "Equip", lifecycle: "PersistentMeta", pool: "Enfers,room.enfer3", effects:
            [
                Fx("StatModifier", "Wearer", 10, "Percent", "WhileEquipped", behavior: "attack"),
                Fx("StatModifier", "Wearer", 2, "Flat", "SourceStatusDuration",
                    "on-receive:attack-buff;max-stacks:5", behavior: "defense")
            ]),
        P("item.boussole-pelerin", "Boussole du Pèlerin",
            "+10% Vitesse dans la Montagne ; les Stations ennemies exigent une Prière supplémentaire.",
            "Equipment", "Accessory", "Common", "Equip", lifecycle: "PersistentMeta", pool: "Montagne", effects:
            [
                Fx("StatModifier", "Wearer", 10, "Percent", "WhileEquipped", "room:Montagne", behavior: "speed"),
                Fx("StationPrayerRequirement", "EnemyStations", 1, "Flat", "WhileInRoom", "room:Montagne")
            ]),
        P("item.couronne-sel", "Couronne de sel",
            "Immunité au poison ; sous Pluie violacée, +10% Attaque magique et -2 Mana aux compétences de l'équipe.",
            "Equipment", "Accessory", "Epic", "Equip", lifecycle: "PersistentMeta", pool: "loot.imperatrice", effects:
            [
                Fx("StatusImmunity", "Wearer", null, "Boolean", "WhileEquipped", behavior: "poison"),
                Fx("StatModifier", "Wearer", 10, "Percent", "WhileWeatherActive", "weather:PluieViolacee", behavior: "magic-attack"),
                Fx("ManaCostModifier", "Team", -2, "Flat", "WhileWeatherActive", "weather:PluieViolacee;max-stacks:5")
            ]),
        P("item.cornes-ivoire", "Petites cornes d'ivoire",
            "La première attaque de mêlée réussie subie par combat est annulée et renvoie 50% des dégâts finaux.",
            "Equipment", "Accessory", "Epic", "Equip", lifecycle: "PersistentMeta", pool: "room.cellulehopital", effects:
            [Fx("ReflectMeleeDamage", "Attacker", 50, "Percent", "Immediate",
                "first-successful-melee-hit-per-combat", behavior: "damage-only;generate-charge")],
            equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "reflect-first-melee-hit")]),
        P("item.iris-amethyste", "Iris améthyste",
            "Une fois par combat, force un ennemi non-boss à attaquer son propre camp ; coûte 1% de Vitalité maximale pour la run.",
            "Equipment", "Accessory", "Epic", "UseInCombat", combat: true, effects:
            [
                Fx("MindControl", "SingleEnemy", 1, "BearerActivations", "non-boss;once-per-combat", behavior: "ai-selects-target-and-skill"),
                Fx("MaxVitalityCost", "Wearer", 1, "Percent", "Run", "minimum-current-and-max:1")
            ], equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "tactical-mind-control")]),
        P("item.aiguille-arret", "Aiguille d'arrêt",
            "Une fois par combat, ralentit tous les ennemis pendant deux activations : Vitesse, Movement et portées divisés par deux.",
            "Equipment", "Accessory", "Legendary", "UseInCombat", combat: true, range: 999, shape: "Map", los: false, minDepth: 3,
            effects:
            [Fx("TemporalSlow", "AllEnemies", 50, "Percent", "2TargetActivations",
                "once-per-combat;max-stacks:5", behavior: "speed-movement-range;floor;minimum:1")],
            equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "tactical-temporal-slow")]),
        P("item.diapason-audela", "Diapason de l'au-delà",
            "À la mort du porteur, disparition irréversible pour le combat, +10% à toutes les statistiques de l'équipe et lancement gratuit du sort signature.",
            "Equipment", "Accessory", "Rare", "Equip", lifecycle: "PersistentMeta", effects:
            [
                Fx("StatModifier", "Team", 10, "Percent", "Combat", "on-wearer-defeated;max-stacks:5", behavior: "all-stats"),
                Fx("CastSignatureSkill", "NearestRelevantTarget", null, "FreeCast", "Immediate",
                    "on-wearer-defeated", behavior: "ignore-all-costs-and-cooldown"),
                Fx("PreventRevive", "Wearer", null, "Boolean", "Combat", "on-wearer-defeated")
            ], equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "prevent-revive-signature-on-death")]),
        P("item.ombrelle-jardinier", "Ombrelle du jardinier",
            "Ignore les malus météorologiques ; sous Accalmie, +5% à toutes les statistiques.",
            "Equipment", "Accessory", "Rare", "Equip", lifecycle: "PersistentMeta", pool: "room.jardin", effects:
            [
                Fx("IgnoreWeatherPenalties", "Wearer", null, "Boolean", "WhileEquipped"),
                Fx("StatModifier", "Wearer", 5, "Percent", "WhileWeatherActive", "weather:Accalmie", behavior: "all-stats")
            ]),
        P("item.grain-choeur", "Grain du chœur",
            "Les compétences de registre Silence coûtent -2 Mana ; le Silence appliqué dure une activation supplémentaire.",
            "Equipment", "Accessory", "Uncommon", "Equip", lifecycle: "PersistentMeta", pool: "Enfers", effects:
            [
                Fx("ManaCostModifier", "Wearer", -2, "Flat", "WhileEquipped", "register:Silence;max-stacks:5"),
                Fx("StatusDurationModifier", "SilenceAppliedByWearer", 1, "BearerActivations", "first-stack")
            ], equipmentEffects:
            [
                new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "silence-mana-minus-two"),
                new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "silence-duration-plus-one")
            ]),

        // Consommables de soin
        P("item.the-seuil", "Thé du seuil",
            "Restaure 25% de Vitalité maximale ; hors combat, restaure aussi 10% de Mana maximale.",
            "Consumable", "Consumable", "Common", "UseAnywhere", stack: 20, combat: true, outside: true,
            effectRunType: "HealPercent", effectValue: 25, effects:
            [
                Fx("Heal", "SingleAlly", 25, "PercentMaxVitality", "Immediate"),
                Fx("ManaRestore", "SingleAlly", 10, "PercentMaxMana", "Immediate", "outside-combat")
            ]),
        P("item.eau-carafe", "Eau de la carafe",
            "Restaure 15% de Vitalité maximale ; si la blessure du Majordome est rompue, applique à la place un poison de 4 dégâts pendant 5 activations.",
            "Consumable", "Consumable", "Common", "UseAnywhere", stack: 20, combat: true, outside: true,
            effectRunType: "ConditionalHealOrPoison", effectValue: 15, effects:
            [
                Fx("Heal", "SingleAlly", 15, "PercentMaxVitality", "Immediate", "wound:w-tapis:not-broken"),
                Fx("PeriodicDamage", "SingleAlly", 4, "Flat", "5TargetActivations", "wound:w-tapis:broken", behavior: "poison")
            ]),
        P("item.compresse-blanche", "Compresse blanche",
            "Restaure 20% de Vitalité maximale et retire une stack d'un DoT choisi.",
            "Consumable", "Consumable", "Uncommon", "UseAnywhere", stack: 20, combat: true, outside: true,
            effectRunType: "HealPercentAndCleanseDot", effectValue: 20, pool: "Hopital", effects:
            [
                Fx("Heal", "SingleAlly", 20, "PercentMaxVitality", "Immediate"),
                Fx("DispelStatusStack", "SingleAlly", 1, "Flat", "Immediate", "status:periodic-damage")
            ]),
        P("item.larme-violacee", "Larme violacée",
            "Restaure 50% de Vitalité maximale et applique Silence pendant une activation.",
            "Consumable", "Consumable", "Rare", "UseAnywhere", stack: 20, combat: true, outside: true,
            effectRunType: "HealPercentAndSilence", effectValue: 50, pool: "Falaise,Enfers", effects:
            [
                Fx("Heal", "SingleAlly", 50, "PercentMaxVitality", "Immediate"),
                Fx("ApplyStatus", "SingleAlly", 1, "Stack", "1TargetActivation", behavior: "silence")
            ]),
        P("item.eclat-ecarlate", "Éclat écarlate",
            "Réanime un allié à 50% de Vitalité maximale ; l'utilisateur perd 5% de Vitalité maximale pour la run, sans pouvoir descendre sous 1.",
            "Consumable", "Consumable", "Epic", "UseInCombat", stack: 20, combat: true,
            range: 999, shape: "Map", los: false, effectRunType: "RevivePercent", effectValue: 50, pool: "Enfers,room.enfer3", effects:
            [
                Fx("Revive", "AnyDefeatedAlly", 50, "PercentMaxVitality", "Immediate", behavior: "spawn-near-user"),
                Fx("MaxVitalityCost", "User", 5, "Percent", "Run", "minimum-current-and-max:1")
            ]),
        P("item.fiole-brume", "Fiole de brume",
            "Restaure 15% de Vitalité maximale et accorde +15 esquive pour le combat actuel ou le prochain.",
            "Consumable", "Consumable", "Uncommon", "UseAnywhere", stack: 20, combat: true, outside: true,
            effectRunType: "HealPercentAndEvasion", effectValue: 15, effects:
            [
                Fx("Heal", "SingleAlly", 15, "PercentMaxVitality", "Immediate"),
                Fx("StatModifier", "SingleAlly", 15, "Points", "CombatOrNextCombat", behavior: "evasion")
            ]),

        // Progression
        P("item.grain-chapelet", "Grain de chapelet", "+2 points de compétence à chaque membre de l'équipe.",
            "SkillEssence", "Essence de sort", "Common", "UseOutsideCombat", stack: 20, outside: true,
            effectRunType: "GrantTeamSkillPoints", effectValue: 2,
            effects: [Fx("GrantSkillPoints", "Team", 2, "Flat", "Immediate")]),
        P("item.osselet-grave", "Osselet gravé", "+5 points de compétence à l'équipe, ou +7 dans la Calamité.",
            "SkillEssence", "Essence de sort", "Uncommon", "UseOutsideCombat", stack: 20, outside: true, pool: "Enfers", effects:
            // Calamité (+7) is resolved from the current room by the game engine.
            [
                Fx("GrantSkillPoints", "Team", 5, "Flat", "Immediate", "room:not-enfer1"),
                Fx("GrantSkillPoints", "Team", 7, "Flat", "Immediate", "room:enfer1")
            ], effectRunType: "GrantTeamSkillPoints", effectValue: 5),
        P("item.cristal-resonance", "Cristal de résonance",
            "+10 points de compétence à l'équipe ; les prochains Gardiens de Crystal commencent avec +2 Résonance, cumulable cinq fois.",
            "SkillEssence", "Essence de sort", "Rare", "UseOutsideCombat", stack: 20, outside: true, pool: "SousTerrains,CaverneCrystal", effects:
            [
                Fx("GrantSkillPoints", "Team", 10, "Flat", "Immediate"),
                Fx("NextEncounterModifier", "CrystalGuardians", 2, "Flat", "UntilTriggered", "max-stacks:5", behavior: "resonance")
            ], effectRunType: "GrantTeamSkillPoints", effectValue: 10),
        P("item.page-arrachee", "Page arrachée",
            "Au choix : +15 points de compétence à l'équipe ou un troisième emplacement de compétence temporaire pour la run.",
            "SkillEssence", "Essence de sort", "Epic", "UseOutsideCombat", stack: 1, outside: true, pool: "room.palier", effects:
            [
                Fx("Choice", "User", null, "Choice", "Immediate", behavior: "team-skill-points:15|temporary-skill-slot:+1")
            ], effectRunType: "GrantTeamSkillPoints", effectValue: 15),
        P("item.dent-de-lait", "Dent de lait",
            "+1 point de compétence à l'équipe ; 10% de chance déterministe d'accorder +4 supplémentaires.",
            "SkillEssence", "Essence de sort", "Common", "UseOutsideCombat", stack: 20, outside: true, effects:
            [
                Fx("GrantSkillPoints", "Team", 1, "Flat", "Immediate"),
                Fx("GrantSkillPoints", "Team", 4, "Flat", "Immediate", "deterministic-chance:10")
            ], effectRunType: "GrantTeamSkillPoints", effectValue: 1),

        // Reliques
        P("item.cahier-noir", "Cahier noir",
            "Une fois par salle, inscrit un ennemi non-boss : après quatre activations globales, 40 dégâts ignorant la Défense mais absorbés par la Garde ; coût 1% Vitalité max.",
            "Relic", "Relic", "Legendary", "UseInCombat", combat: true, minDepth: 3, effects:
            [
                Fx("DelayedDirectDamage", "SingleEnemy", 40, "Flat", "4GlobalActivations", "non-boss;once-per-room", behavior: "ignore-defense;guard-first"),
                Fx("MaxVitalityCost", "Wearer", 1, "Percent", "Run", "minimum-current-and-max:1")
            ]),
        P("item.yeux-marchand", "Yeux du marchand",
            "À l'acquisition, le porteur perd 25% de Vitalité maximale ; révèle Vitalité exacte, menace et prochaine action des ennemis.",
            "Relic", "Relic", "Epic", "Equip", lifecycle: "PersistentMeta", effects:
            [
                Fx("MaxVitalityCost", "Wearer", 25, "Percent", "Run", "on-acquire;minimum-current-and-max:1"),
                Fx("RevealEnemyIntent", "Team", null, "Boolean", "WhileEquipped", behavior: "vitality-threat-next-action")
            ]),
        P("item.fleche-meridienne", "Flèche méridienne",
            "À l'acquisition : 50% d'invoquer un Écho intérieur une fois par combat, sinon perte de 30% de Vitalité actuelle.",
            "Relic", "Relic", "Legendary", "Equip", lifecycle: "PersistentMeta", effects:
            [
                Fx("DeterministicChoice", "Wearer", 50, "Percent", "Run", behavior: "grant-echo|lose-current-vitality:30"),
                Fx("SummonEcho", "AdjacentCell", 60, "PercentWearerStats", "2BearerActivations",
                    "once-per-combat", behavior: "controlled-during-wearer-turn;weapon-contract")
            ]),
        P("item.metronome-choeur", "Métronome du chœur",
            "Toutes les cinq activations globales, l'ennemi vivant le plus rapide reçoit Silence pendant une activation.",
            "Relic", "Relic", "Rare", "Passive", pool: "Enfers", effects:
            [Fx("ApplyStatus", "FastestEnemy", 1, "Stack", "1TargetActivation", "every:5-global-activations", behavior: "silence")],
            equipmentEffects:
            [new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "team-silence-every-five-activations")]),
        P("item.tapis-poche", "Tapis de poche",
            "Au début des combats du Hall, des Couloirs ou du Palier, les ennemis subissent -2 Vitesse jusqu'à la fin de leur première activation.",
            "Relic", "Relic", "Rare", "Passive", pool: "room.halldentree", effects:
            [Fx("StatModifier", "AllEnemies", -2, "Flat", "UntilFirstTargetActivation",
                "room:Hall|Couloirs|Palier", behavior: "speed;recalculate-initiative-on-expire")]),
        P("item.clef-sans-porte", "Clef sans porte",
            "Une fois par étage : ouvre une salle verrouillée ou dissipe un enfermement sur toute l'équipe.",
            "Relic", "Relic", "Epic", "UseAnywhere", combat: true, outside: true, range: 999, shape: "Map", los: false, effects:
            [Fx("SharedFloorCharge", "Run", 1, "Charge", "Floor", behavior: "unlock-room|dispel-team-confinement")]),
        P("item.boite-musique-felee", "Boîte à musique fêlée",
            "À l'engagement : les ennemis Effroi sont ralentis pour leur première activation ; les ennemis Mélancolie avancent de cinq cases.",
            "Relic", "Relic", "Epic", "Passive", pool: "room.hopital", effects:
            [
                Fx("TemporalSlow", "EnemiesByRegister", 50, "Percent", "1TargetActivation", "register:Effroi", behavior: "speed-movement-range"),
                Fx("ForcedMovement", "EnemiesByRegister", 5, "Cells", "Immediate", "register:Melancolie", behavior: "toward-nearest-ally")
            ]),
        P("item.cercle-craie", "Cercle de craie",
            "Hors combat, deux charges par run : détruit un objet et tire un objet différent de même rareté.",
            "Relic", "Relic", "Rare", "UseOutsideCombat", outside: true, pool: "room.cellule", effects:
            [Fx("TransmuteItem", "SelectedInventoryItem", 2, "RunCharges", "Run", behavior: "same-rarity;exclude-source")]),

        // Grimoires
        P("item.tome-marees", "Tome des marées", "Apprend temporairement Déluge mineur.",
            "Grimoire", "Grimoire", "Epic", "UseOutsideCombat", stack: 20, outside: true, pool: "loot.imperatrice",
            effectRunType: "GrantTemporarySkill",
            effects: [Fx("GrantTemporarySkill", "User", null, "SkillKey", "Run", behavior: "skill.temp.deluge-mineur")],
            equipmentEffects: [new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantSkill, SkillKey: "skill.temp.deluge-mineur")]),
        P("item.feuillet-copiste", "Feuillet du copiste", "Apprend temporairement Écriture appliquée.",
            "Grimoire", "Grimoire", "Rare", "UseOutsideCombat", stack: 20, outside: true, pool: "Labyrinthe",
            effectRunType: "GrantTemporarySkill",
            effects: [Fx("GrantTemporarySkill", "User", null, "SkillKey", "Run", behavior: "skill.temp.ecriture-appliquee")],
            equipmentEffects: [new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantSkill, SkillKey: "skill.temp.ecriture-appliquee")]),
        P("item.braise-volee", "Braise volée", "Apprend temporairement Souffle emprunté ; si l'offrande supérieure est connue, accorde 8 points de compétence.",
            "Grimoire", "Grimoire", "Rare", "UseOutsideCombat", stack: 20, outside: true, pool: "room.enfer3",
            effectRunType: "GrantTemporarySkill", effects:
            [
                Fx("GrantTemporarySkill", "User", null, "SkillKey", "Run", "skill:not-known:souffle-forge", behavior: "skill.temp.souffle-emprunte"),
                Fx("GrantSkillPoints", "Team", 8, "Flat", "Immediate", "skill:known:souffle-forge")
            ], equipmentEffects:
            [
                new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantSkill, SkillKey: "skill.temp.souffle-emprunte"),
                new ItemEquipmentEffect(ItemEquipmentEffectKind.RuntimeBehavior, BehaviorCode: "known-forge-skill-awards-eight-points")
            ]),
        P("item.retable-portatif", "Retable portatif", "Apprend temporairement Prière ; en présence de Pénitents, crée une Station près du lanceur.",
            "Grimoire", "Grimoire", "Uncommon", "UseOutsideCombat", stack: 20, outside: true, pool: "Montagne",
            effectRunType: "GrantTemporarySkill",
            effects: [Fx("GrantTemporarySkill", "User", null, "SkillKey", "Run", behavior: "canon.skill.priere-aspiration")],
            equipmentEffects: [new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantSkill, SkillKey: "canon.skill.priere-aspiration")]),
        P("item.carnet-croquis", "Carnet de croquis", "Apprend temporairement Construction éphémère.",
            "Grimoire", "Grimoire", "Epic", "UseOutsideCombat", stack: 1, outside: true, pool: "room.cellule",
            effectRunType: "GrantTemporarySkill",
            effects: [Fx("GrantTemporarySkill", "User", null, "SkillKey", "Run", behavior: "skill.temp.construction-ephemere")],
            equipmentEffects: [new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantSkill, SkillKey: "skill.temp.construction-ephemere")]),

        // Instruments météo
        P("item.girouette-os", "Girouette d'os", "Hors combat, trois charges par run : relance la météo de la salle actuelle.",
            "WeatherInstrument", "Instrument météorologique", "Uncommon", "UseOutsideCombat", stack: 3, outside: true,
            effectRunType: "RerollWeather", effects:
            [Fx("RerollWeather", "CurrentRoom", 3, "RunCharges", "Run", "room:not-weather-immune")]),
        P("item.flacon-orage", "Flacon d'orage", "Impose Orage pendant trois salles ; une salle immunisée consomme tout de même une charge de durée.",
            "WeatherInstrument", "Instrument météorologique", "Rare", "UseOutsideCombat", stack: 20, outside: true,
            effectRunType: "ForceWeatherOrage", effectValue: 3, effects:
            [Fx("ForceWeather", "NextRooms", 3, "Rooms", "3Rooms", "room:not-weather-immune", behavior: "Orage;immune-room-consumes-duration")]),
        P("item.pierre-accalmie", "Pierre d'accalmie", "Impose Accalmie dans la salle actuelle.",
            "WeatherInstrument", "Instrument météorologique", "Rare", "UseOutsideCombat", stack: 20, outside: true,
            effectRunType: "ForceWeatherAccalmie", effectValue: 1, pool: "Montagne", effects:
            [Fx("ForceWeather", "CurrentRoom", 1, "Room", "CurrentRoom", "room:not-weather-immune", behavior: "Accalmie")]),
        P("item.barometre-palais", "Baromètre du Palais",
            "Révèle la météo de la prochaine salle ; une fois par étage, permet de choisir cette météo.",
            "WeatherInstrument", "Instrument météorologique", "Rare", "UseOutsideCombat", outside: true, effects:
            [
                Fx("RevealWeather", "NextRoom", null, "Boolean", "WhileEquipped"),
                Fx("ChooseWeather", "NextRoom", 1, "FloorCharge", "Floor", "room:not-weather-immune")
            ]),
        P("item.cendrier-forgeron", "Cendrier du Forgeron",
            "Sous Pluie de cendres : régénère 2% Vitalité max par activation et +10% dégâts de feu ; aucun effet sous Pluie violacée.",
            "Equipment", "Accessory", "Uncommon", "Equip", lifecycle: "PersistentMeta", pool: "Enfers", effects:
            [
                Fx("PeriodicHeal", "Wearer", 2, "PercentMaxVitality", "EachBearerActivation", "weather:PluieDeCendres"),
                Fx("DamageModifier", "Wearer", 10, "Percent", "WhileWeatherActive", "weather:PluieDeCendres", behavior: "fire")
            ]),

        // Héritages
        P("item.sceau-invite", "Sceau de l'invité reconnu",
            "Au début de chaque run, accorde +100 réputation auprès d'un PNJ choisi.",
            "Relic", "Heritage", "Epic", "MetaPassive", lifecycle: "PersistentMeta", effects:
            [Fx("GrantReputation", "SelectedNpc", 100, "Flat", "RunStart")]),
        P("item.eclat-faille", "Éclat de la Faille",
            "+1 relance de nœud d'objet par run.",
            "Relic", "Heritage", "Epic", "MetaPassive", lifecycle: "PersistentMeta", effects:
            [Fx("ItemNodeReroll", "Run", 1, "RunCharge", "Run")]),
        P("item.memoire-erika", "Mémoire d'Erika",
            "Le premier nœud d'objet de chaque run propose quatre choix au lieu de trois.",
            "Relic", "Heritage", "Rare", "MetaPassive", lifecycle: "PersistentMeta", effects:
            [Fx("ItemNodeChoiceCount", "FirstItemNode", 1, "AdditionalChoice", "Run")]),
        P("item.cicatrice-encre", "Cicatrice d'encre",
            "+2% dégâts des DoT, définitif, cumulable cinq fois.",
            "Relic", "Heritage", "Rare", "MetaPassive", lifecycle: "PersistentMeta", stack: 5, effects:
            [Fx("PeriodicDamageModifier", "Owner", 2, "Percent", "Permanent", "max-stacks:5")]),
        P("item.medaille-fin-annee", "Médaille de fin d'année",
            "Une fois par run, la première mort d'un compagnon est annulée : retour à 30% Vitalité et +5% Vitesse jusqu'à la fin du combat.",
            "Relic", "Heritage", "Legendary", "MetaPassive", lifecycle: "PersistentMeta", effects:
            [
                Fx("AutoRevive", "FirstDefeatedCompanion", 30, "PercentMaxVitality", "Immediate", "once-per-run"),
                Fx("StatModifier", "RevivedCompanion", 5, "Percent", "Combat", behavior: "speed")
            ])
    ];

    private static readonly IReadOnlyList<PalaceLootTableSeed> PalaceLootTables =
    [
        L("canon.enemy.veilleur-tapis", "Veilleur du Tapis", ("item.gants-service-muet", 10), ("item.the-seuil", 12), ("item.epingle-protocole", 5), ("item.weapon.lame-seuil", 8)),
        L("canon.enemy.porteur-plateau", "Porteur de Plateau", ("item.eau-carafe", 18), ("item.the-seuil", 12)),
        L("canon.enemy.echo-politesse", "l'Écho de Politesse", ("item.fiole-brume", 14), ("item.epingle-protocole", 6)),
        L("canon.enemy.sentinelle-seuil", "la Sentinelle du Seuil", ("item.gants-service-muet", 15), ("item.tapis-poche", 3)),
        L("canon.enemy.copiste-aveugle", "Copiste Aveugle", ("item.feuillet-copiste", 6), ("item.encrier-poche", 9)),
        L("canon.enemy.encrier-vivant", "l'Encrier Vivant", ("item.encrier-poche", 16)),
        L("canon.enemy.page-inachevee", "la Page Inachevée", ("item.feuillet-copiste", 8), ("item.larme-violacee", 3)),
        L("canon.enemy.relieur", "le Relieur", ("item.aiguille-relieur", 20), ("item.feuillet-copiste", 15), ("item.cicatrice-encre", 100), ("item.weapon.arc-relieur", 8)),
        L("canon.enemy.squelette-souvenir", "le Squelette de Souvenir", ("item.osselet-grave", 14), ("item.dent-de-lait", 6)),
        L("canon.enemy.porteur-cendre", "le Porteur de Cendre", ("item.osselet-grave", 22), ("item.girouette-os", 7)),
        L("canon.enemy.choeur-muet", "le Chœur Muet", ("item.grain-choeur", 18), ("item.metronome-choeur", 6), ("item.larme-violacee", 5)),
        L("canon.enemy.berger-ordres", "le Berger d'Ordres", ("item.flacon-orage", 6), ("item.iris-amethyste", 2)),
        L("canon.enemy.agneau-inverse", "l'Agneau Inversé", ("item.fiole-brume", 8)),
        L("canon.enemy.creation-instable", "la Création Instable", ("item.gantelet-trempe", 10)),
        L("canon.enemy.marteau-vivant", "le Marteau Vivant", ("item.braise-volee", 9), ("item.gantelet-trempe", 12), ("item.eclat-ecarlate", 2), ("item.weapon.marteau-forge", 10)),
        L("canon.enemy.sentinelle-fonte", "la Sentinelle de Fonte", ("item.cendrier-forgeron", 12)),
        L("canon.enemy.scorie-rampante", "la Scorie Rampante", ("item.cendrier-forgeron", 8)),
        L("canon.enemy.infirmiere-deni", "l'Infirmière du Déni", ("item.compresse-blanche", 25)),
        L("canon.enemy.souvenir-alite", "le Souvenir Alité", ("item.compresse-blanche", 20), ("item.boite-musique-felee", 2)),
        L("canon.enemy.regisseur-blanc", "le Régisseur des Couloirs Blancs", ("item.clef-sans-porte", 15), ("item.compresse-blanche", 20)),
        L("canon.enemy.pelerin-sans-visage", "le Pèlerin Sans Visage", ("item.grain-chapelet", 30), ("item.retable-portatif", 5), ("item.weapon.lance-pelerin", 9)),
        L("canon.enemy.prieur-lituique", "le Prieur Lituique", ("item.retable-portatif", 14), ("item.weapon.baton-silences", 7)),
        L("canon.enemy.frayeur-exhumee", "la Frayeur Exhumée", ("item.pierre-accalmie", 12), ("item.fleche-meridienne", 1)),
        L("canon.enemy.promeneur-fige", "le Promeneur Figé", ("item.dent-de-lait", 5)),
        L("canon.enemy.jardinier-sans-ombre", "le Jardinier Sans Ombre", ("item.ombrelle-jardinier", 18)),
        L("canon.enemy.gardien-intemporel", "le Gardien Intemporel", ("item.cristal-resonance", 25), ("item.aiguille-arret", 1)),
        L("canon.enemy.eclat-eveille", "l'Éclat Éveillé", ("item.cristal-resonance", 18)),
        L("canon.enemy.echo-colere", "l'Écho de Colère", ("item.flacon-orage", 8)),
        L("canon.enemy.echo-peur", "l'Écho de Peur", ("item.fiole-brume", 16)),
        L("canon.enemy.echo-tristesse", "l'Écho de Tristesse", ("item.larme-violacee", 10)),
        L("canon.enemy.imperatrice", "l'Impératrice", ("item.couronne-sel", 35), ("item.tome-marees", 20), ("item.larme-violacee", 60), ("item.fleche-meridienne", 3))
    ];

    private static PalaceLootTableSeed L(
        string enemyKey,
        string displayName,
        params (string ItemKey, int Percent)[] entries)
        => new(enemyKey, displayName, entries.Select(e => new PalaceLootEntrySeed(e.ItemKey, e.Percent)).ToArray());

    private static PalaceItemSeed P(
        string key,
        string name,
        string description,
        string category,
        string flavorTag,
        string rarity,
        string usageMode,
        string lifecycle = "RuntimeRunOnly",
        int stack = 1,
        bool combat = false,
        bool outside = false,
        int range = 2,
        string shape = "Diamond",
        bool los = true,
        int effectValue = 0,
        string? effectRunType = null,
        string pool = "Global",
        int baseWeight = 100,
        int? minDepth = null,
        IReadOnlyList<PalaceEffect>? effects = null,
        IReadOnlyList<ItemEquipmentEffect>? equipmentEffects = null,
        int? basicAttackPower = null,
        string? basicAttackCategory = null)
        => new(
            key, name, description, category, flavorTag, rarity, usageMode, lifecycle,
            stack, combat, outside, range, shape, los, effectValue, effectRunType,
            pool, baseWeight, minDepth, $"palace-items:{rarity}", effects ?? [], equipmentEffects,
            basicAttackPower, basicAttackCategory);

    private sealed record PalaceItemSeed(
        string Key,
        string Name,
        string Description,
        string Category,
        string FlavorTag,
        string Rarity,
        string UsageMode,
        string Lifecycle,
        int MaxStack,
        bool IsUsableInCombat,
        bool IsUsableOutsideCombat,
        int TacticalRange,
        string TacticalShape,
        bool RequiresLineOfSight,
        int EffectValue,
        string? EffectRunType,
        string Pool,
        int BaseWeight,
        int? MinDepth,
        string SelectionGroup,
        IReadOnlyList<PalaceEffect> Effects,
        IReadOnlyList<ItemEquipmentEffect>? EquipmentEffects,
        int? BasicAttackPower,
        string? BasicAttackCategory);

    private sealed record PalaceEffect(
        string Type,
        string Target,
        decimal? Value,
        string ValueMode,
        string Duration,
        string StackPolicy,
        string? Condition,
        string? BehaviorTag);

    private sealed record PalaceLootTableSeed(
        string EnemyKey,
        string DisplayName,
        IReadOnlyList<PalaceLootEntrySeed> Entries);

    private sealed record PalaceLootEntrySeed(string ItemKey, int Percent);
}
