using System.Text.Json;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed class CatalogSeedRunner
{
    private const string SeedKey = "base-catalog";
    private const string LegacyVersion = "alpha-0.5.5";
    private const string DataModelVersion = "alpha-0.8.1";
    private const string CatalogGatewayContentVersion = "alpha-0.8.1-catalog-content-gateway";
    private const string CatalogTemplatesVersion = "alpha-0.8.2-catalog-templates";

    private readonly CatalogDbContext _context;
    private readonly ILogger<CatalogSeedRunner> _logger;

    public CatalogSeedRunner(CatalogDbContext context, ILogger<CatalogSeedRunner> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ApplyBaseSeedAsync(CancellationToken cancellationToken = default)
    {
        await ApplyLegacySeedAsync(cancellationToken);
        await ApplyDataModelSeedAsync(cancellationToken);
        await ApplyCatalogGatewayContentSeedAsync(cancellationToken);
        await ApplyCatalogTemplatesSeedAsync(cancellationToken);
    }

    private async Task ApplyLegacySeedAsync(CancellationToken cancellationToken)
    {
        if (await HasSeedVersionAsync(LegacyVersion, cancellationToken))
        {
            _logger.LogInformation("Seed {SeedKey} version {Version} already applied. Skipping.", SeedKey, LegacyVersion);
            return;
        }

        _logger.LogInformation("Applying seed {SeedKey} version {Version}...", SeedKey, LegacyVersion);

        await SeedSkillDefinitionsAsync(cancellationToken);
        await SeedEnemyDefinitionsAsync(cancellationToken);
        await SeedItemDefinitionsAsync(cancellationToken);
        await SeedPalaceLawDefinitionsAsync(cancellationToken);
        AddSeedVersion(LegacyVersion);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed {SeedKey} version {Version} applied successfully.", SeedKey, LegacyVersion);
    }

    private async Task ApplyDataModelSeedAsync(CancellationToken cancellationToken)
    {
        if (await HasSeedVersionAsync(DataModelVersion, cancellationToken))
        {
            _logger.LogInformation("Seed {SeedKey} version {Version} already applied. Skipping.", SeedKey, DataModelVersion);
            return;
        }

        _logger.LogInformation("Applying seed {SeedKey} version {Version}...", SeedKey, DataModelVersion);

        var now = DateTime.UtcNow;

        await SeedCatalogTagsAsync(now, cancellationToken);
        await SeedEffectSetsAsync(now, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await AlignExistingDefinitionsAsync(cancellationToken);
        await SeedEnemyRelationsAsync(cancellationToken);
        await SeedAdditionalItemsAsync(now, cancellationToken);
        await SeedAdditionalLawsAndCursesAsync(now, cancellationToken);
        await SeedRewardTemplatesAsync(now, cancellationToken);
        await SeedRoomsAsync(now, cancellationToken);
        AddSeedVersion(DataModelVersion);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed {SeedKey} version {Version} applied successfully.", SeedKey, DataModelVersion);
    }

    private async Task ApplyCatalogGatewayContentSeedAsync(CancellationToken cancellationToken)
    {
        if (await HasSeedVersionAsync(CatalogGatewayContentVersion, cancellationToken))
        {
            _logger.LogInformation("Seed {SeedKey} version {Version} already applied. Skipping.", SeedKey, CatalogGatewayContentVersion);
            return;
        }

        _logger.LogInformation("Applying seed {SeedKey} version {Version}...", SeedKey, CatalogGatewayContentVersion);

        var now = DateTime.UtcNow;
        await AddCatalogGatewayItemDefinitionsAsync(now, cancellationToken);
        await AddEffectSetAsync("effectset.curse-old-wound", "Vieille blessure", "Une blessure ancienne se rouvre.", "ModifyDifficultyMultiplier", "NextCombat", 0.10m, "NextCombatOnly", "UniqueBySource", now, cancellationToken);
        await AddEffectSetAsync("effectset.curse-weight-of-silence", "Poids du silence", "Le silence augmente la pression du prochain combat.", "ModifyDifficultyMultiplier", "NextCombat", 0.20m, "NextCombatOnly", "UniqueBySource", now, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var effectIds = await EffectSetIdsAsync(cancellationToken);
        await AddCurseAsync(
            "curse.old-wound",
            "Vieille blessure",
            "Une blessure ancienne qui se rouvre au plus mauvais moment.",
            "Le corps porte ses propres souvenirs.",
            3,
            "NextCombatOnly",
            null,
            "effectset.curse-old-wound",
            effectIds,
            now,
            cancellationToken);
        await AddCurseAsync(
            "curse.weight-of-silence",
            "Poids du silence",
            "Le silence devient une charge mentale supplémentaire.",
            null,
            5,
            "NextCombatOnly",
            null,
            "effectset.curse-weight-of-silence",
            effectIds,
            now,
            cancellationToken);
        await AddCatalogGatewayRewardTemplatesAsync(now, cancellationToken);

        AddSeedVersion(CatalogGatewayContentVersion);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed {SeedKey} version {Version} applied successfully.", SeedKey, CatalogGatewayContentVersion);
    }

    private Task<bool> HasSeedVersionAsync(string version, CancellationToken cancellationToken)
    {
        return _context.CatalogSeedVersions.AnyAsync(v => v.SeedKey == SeedKey && v.Version == version, cancellationToken);
    }

    private void AddSeedVersion(string version)
    {
        _context.CatalogSeedVersions.Add(new CatalogSeedVersionEntity
        {
            Id = Guid.NewGuid(),
            SeedKey = SeedKey,
            Version = version,
            AppliedAtUtc = DateTime.UtcNow
        });
    }

    private async Task SeedSkillDefinitionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var skills = new[]
        {
            new SkillDefinitionEntity
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"),
                Key = "skill.basic.strike",
                Name = "Frappe",
                DisplayName = "Frappe",
                Description = "Une attaque basique.",
                Version = LegacyVersion,
                Status = "Active",
                SkillType = "Damage",
                TargetingType = "SingleEnemy",
                TargetingMode = "SingleEnemy",
                EffectType = "DamageVitality",
                CostType = "None",
                ManaCost = 0,
                ChargeCost = 0,
                BasePower = 10,
                Power = 10,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SkillDefinitionEntity
            {
                Id = Guid.Parse("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"),
                Key = "skill.basic.guard",
                Name = "Garde",
                DisplayName = "Garde",
                Description = "Une défense basique.",
                Version = LegacyVersion,
                Status = "Active",
                SkillType = "Defense",
                TargetingType = "Self",
                TargetingMode = "Self",
                EffectType = "AddCurrentGuard",
                CostType = "None",
                ManaCost = 0,
                ChargeCost = 0,
                BasePower = 5,
                Power = 5,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var skill in skills)
        {
            if (!await _context.SkillDefinitions.AnyAsync(s => s.Key == skill.Key, cancellationToken))
            {
                _context.SkillDefinitions.Add(skill);
            }
        }
    }

    private async Task SeedEnemyDefinitionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var enemies = new[]
        {
            new EnemyDefinitionEntity
            {
                Id = Guid.Parse("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"),
                Key = "enemy.threshold.echo",
                Name = "Echo",
                DisplayName = "Echo",
                Description = "Une creature du seuil.",
                Version = LegacyVersion,
                Status = "Active",
                Archetype = "Trauma",
                Family = "Threshold",
                Rank = "Common",
                Role = "DPS",
                BaseDifficulty = 1,
                EncounterWeight = 1,
                MinRiskLevel = 1,
                MaxRiskLevel = 30,
                MinDepth = 1,
                MaxDepth = 3,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Threshold\",\"Memory\"]",
                TagsJson = "[\"threshold\",\"common\"]",
                SkillKeysJson = "[\"skill.basic.strike\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new EnemyDefinitionEntity
            {
                Id = Guid.Parse("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"),
                Key = "enemy.threshold.fracture",
                Name = "Fracture",
                DisplayName = "Fracture",
                Description = "Une entite fragmentee.",
                Version = LegacyVersion,
                Status = "Active",
                Archetype = "Shadow",
                Family = "Threshold",
                Rank = "Elite",
                Role = "Disruptor",
                BaseDifficulty = 2,
                EncounterWeight = 1,
                MinRiskLevel = 20,
                MaxRiskLevel = 60,
                MinDepth = 2,
                MaxDepth = 5,
                IsElite = true,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Threshold\",\"Forest\",\"Rupture\"]",
                TagsJson = "[\"threshold\",\"elite\"]",
                SkillKeysJson = "[\"skill.basic.strike\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var enemy in enemies)
        {
            if (!await _context.EnemyDefinitions.AnyAsync(e => e.Key == enemy.Key, cancellationToken))
            {
                _context.EnemyDefinitions.Add(enemy);
            }
        }
    }

    private async Task SeedItemDefinitionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var items = new[]
        {
            new ItemDefinitionEntity
            {
                Id = Guid.Parse("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b"),
                Key = "item.consumable.minor-heal",
                Name = "Soin mineur",
                DisplayName = "Soin mineur",
                Description = "Restaure un peu de vitalite.",
                Version = LegacyVersion,
                Status = "Active",
                Category = "Consumable",
                ItemType = "Heal",
                Rarity = "Common",
                UsageMode = "UseInCombat",
                Lifecycle = "RuntimeRunOnly",
                StackPolicy = "Additive",
                MaxStack = 3,
                IsUsableInCombat = true,
                IsUsableOutsideCombat = false,
                Duration = "RunOnly",
                EffectValue = 15,
                Price = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var item in items)
        {
            if (!await _context.ItemDefinitions.AnyAsync(i => i.Key == item.Key, cancellationToken))
            {
                _context.ItemDefinitions.Add(item);
            }
        }
    }

    private async Task SeedPalaceLawDefinitionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var laws = new[]
        {
            new PalaceLawDefinitionEntity
            {
                Id = Guid.Parse("f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c"),
                Key = "law.threshold.silence-weight",
                Name = "Poids du silence",
                DisplayName = "Poids du silence",
                Description = "Le silence affecte la generation.",
                Version = LegacyVersion,
                Status = "Active",
                Scope = "Run",
                Duration = "UntilRunEnds",
                Severity = 1,
                Visibility = "Visible",
                Priority = 1,
                ImpactDomainsJson = "[\"Generation\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var law in laws)
        {
            if (!await _context.PalaceLawDefinitions.AnyAsync(l => l.Key == law.Key, cancellationToken))
            {
                _context.PalaceLawDefinitions.Add(law);
            }
        }
    }

    private async Task SeedCatalogTagsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var tags = new[]
        {
            ("tag.enemy.threshold", "Seuil", "enemy"),
            ("tag.enemy.common", "Commun", "enemy"),
            ("tag.enemy.elite", "Elite", "enemy"),
            ("tag.item.consumable", "Consommable", "item"),
            ("tag.room.threshold", "Salle du seuil", "room"),
            ("tag.room.rare", "Salle rare", "room"),
            ("tag.room.cultural-echo", "Echo culturel", "room"),
            ("tag.generation.alchemical", "Alchimique", "generation"),
            ("tag.behavior.paranoid", "Paranoiaque", "behavior")
        };

        foreach (var tag in tags)
        {
            if (!await _context.CatalogTags.AnyAsync(t => t.TagKey == tag.Item1, cancellationToken))
            {
                _context.CatalogTags.Add(new CatalogTagEntity
                {
                    Id = Guid.NewGuid(),
                    TagKey = tag.Item1,
                    DisplayName = tag.Item2,
                    Category = tag.Item3,
                    CreatedAtUtc = now
                });
            }
        }
    }

    private async Task SeedEffectSetsAsync(DateTime now, CancellationToken cancellationToken)
    {
        await AddEffectSetAsync("effect.skill.basic-strike", "Frappe", "Degats directs de base.", "DamageVitality", "SingleEnemy", 10, "Immediate", "None", now, cancellationToken);
        await AddEffectSetAsync("effect.skill.basic-guard", "Garde", "Ajoute de la garde courante.", "AddCurrentGuard", "Self", 5, "CurrentCombat", "Additive", now, cancellationToken);
        await AddEffectSetAsync("effect.item.minor-heal", "Soin mineur", "Restaure de la vitalite.", "HealVitality", "Self", 15, "Immediate", "None", now, cancellationToken);
        await AddEffectSetAsync("effect.item.eclat-de-garde", "Eclat de garde", "Renforce la garde initiale du prochain combat.", "AddStartingGuard", "NextCombat", 8, "UntilRunEnds", "Additive", now, cancellationToken);
        await AddEffectSetAsync("effect.law.silence-weight", "Poids du silence", "Influence de generation silencieuse.", "ModifyGenerationWeight", "SelectionContext", 0.10m, "UntilRunEnds", "Additive", now, cancellationToken, generationTag: "generation.silence");
        await AddEffectSetAsync("effect.law.mefiance-des-echos", "La Mefiance des Echos", "Influence comportementale paranoiaque.", "ModifyEnemyBehavior", "AllEnemies", 0.15m, "UntilRoomEnds", "Additive", now, cancellationToken, behaviorTag: "behavior.paranoid");
        await AddEffectSetAsync("effect.law.aegis", "Loi de l'Egide", "Renforce la garde initiale.", "AddStartingGuard", "Run", 8m, "UntilRunEnds", "Additive", now, cancellationToken);
        await AddEffectSetAsync("effect.law.siege", "Loi du Siege", "Augmente la pression des combats.", "ModifyDifficultyMultiplier", "Run", 0.10m, "UntilRunEnds", "Additive", now, cancellationToken);
        await AddEffectSetAsync("effect.law.carnage", "Loi du Carnage", "Augmente la puissance d'attaque.", "ModifyAttackPower", "Run", 0.10m, "UntilRunEnds", "Additive", now, cancellationToken);
        await AddEffectSetAsync("effect.law.climate-rain", "Climat Pluie", "Applique la Pluie a la Room actuelle.", "ApplyRoomClimate", "CurrentRoom", null, "UntilRoomEnds", "UniqueBySource", now, cancellationToken, condition: "Rain");
        await AddEffectSetAsync("effect.law.climate-hail", "Climat Grele", "Applique la Grele a la Room actuelle.", "ApplyRoomClimate", "CurrentRoom", null, "UntilRoomEnds", "UniqueBySource", now, cancellationToken, condition: "Hail");
        await AddEffectSetAsync("effect.law.climate-heatwave", "Climat Canicule", "Applique la Canicule a la Room actuelle.", "ApplyRoomClimate", "CurrentRoom", null, "UntilRoomEnds", "UniqueBySource", now, cancellationToken, condition: "Heatwave");
        await AddEffectSetAsync("effect.law.climate-grey", "Climat Grisaille", "Applique la Grisaille a la Room actuelle.", "ApplyRoomClimate", "CurrentRoom", null, "UntilRoomEnds", "UniqueBySource", now, cancellationToken, condition: "Grey");
        await AddEffectSetAsync("effect.curse.souffle-lourd", "Souffle lourd", "Augmente la pression du prochain combat.", "ModifyDifficultyMultiplier", "NextCombat", 0.10m, "NextCombatOnly", "UniqueBySource", now, cancellationToken);
        await AddEffectSetAsync("effect.mechanic.equivalent-exchange", "Echange equivalent", "Chaque puissance offerte exige une dette.", "ApplyNarrativePressure", "Room", null, "UntilRoomEnds", "UniqueBySource", now, cancellationToken, generationTag: "generation.alchemical");
    }

    private async Task AddEffectSetAsync(
        string key,
        string displayName,
        string description,
        string effectType,
        string targetScope,
        decimal? value,
        string duration,
        string stackPolicy,
        DateTime now,
        CancellationToken cancellationToken,
        string? condition = null,
        string? behaviorTag = null,
        string? generationTag = null)
    {
        if (await _context.EffectSets.AnyAsync(e => e.Key == key, cancellationToken))
        {
            return;
        }

        var effectSet = new EffectSetEntity
        {
            Id = Guid.NewGuid(),
            Key = key,
            DisplayName = displayName,
            Description = description,
            Version = DataModelVersion,
            Status = "Active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Effects =
            [
                new EffectDefinitionEntity
                {
                    Id = Guid.NewGuid(),
                    EffectType = effectType,
                    TargetScope = targetScope,
                    Value = value,
                    ValueMode = value.HasValue ? "Flat" : "TagOnly",
                    Duration = duration,
                    StackPolicy = stackPolicy,
                    Condition = condition,
                    Order = 0,
                    BehaviorTag = behaviorTag,
                    GenerationTag = generationTag
                }
            ]
        };

        _context.EffectSets.Add(effectSet);
    }

    private async Task AlignExistingDefinitionsAsync(CancellationToken cancellationToken)
    {
        await AlignSkillsAsync(cancellationToken);
        await AlignEnemiesAsync(cancellationToken);
        await AlignItemsAsync(cancellationToken);
        await AlignLawsAsync(cancellationToken);
    }

    private async Task AlignSkillsAsync(CancellationToken cancellationToken)
    {
        var effectIds = await EffectSetIdsAsync(cancellationToken);
        var skills = await _context.SkillDefinitions.ToListAsync(cancellationToken);

        foreach (var skill in skills)
        {
            skill.DisplayName = string.IsNullOrWhiteSpace(skill.DisplayName) ? skill.Name : skill.DisplayName;
            skill.TargetingMode = string.IsNullOrWhiteSpace(skill.TargetingMode) ? skill.TargetingType : skill.TargetingMode;
            skill.CostType = skill.ManaCost > 0 ? "Mana" : skill.ChargeCost > 0 ? "Charge" : "None";
            skill.CostAmount = skill.ManaCost > 0 ? skill.ManaCost : skill.ChargeCost;
            skill.Power = skill.Power == 0 ? skill.BasePower : skill.Power;
            skill.Accuracy = skill.Accuracy == 0 ? 100 : skill.Accuracy;
            skill.ActionCost = skill.ActionCost == 0 ? 10 : skill.ActionCost;
            skill.BaseWeight = skill.BaseWeight == 0 ? 1 : skill.BaseWeight;
            skill.UpdatedAtUtc = DateTime.UtcNow;
        }

        SetEffectSet(skills, "skill.basic.strike", effectIds, "effect.skill.basic-strike");
        SetEffectSet(skills, "skill.basic.guard", effectIds, "effect.skill.basic-guard");
    }

    private async Task AlignEnemiesAsync(CancellationToken cancellationToken)
    {
        var enemies = await _context.EnemyDefinitions.ToListAsync(cancellationToken);
        foreach (var enemy in enemies)
        {
            enemy.DisplayName = string.IsNullOrWhiteSpace(enemy.DisplayName) ? enemy.Name : enemy.DisplayName;
            enemy.Family ??= "Threshold";
            enemy.Rank = string.IsNullOrWhiteSpace(enemy.Rank) ? "Common" : enemy.Rank;
            enemy.EncounterWeight = enemy.EncounterWeight == 0 ? 1 : enemy.EncounterWeight;
            enemy.MinDepth ??= 1;
            enemy.MaxDepth ??= 5;
            enemy.BaseWeight = enemy.BaseWeight == 0 ? enemy.EncounterWeight : enemy.BaseWeight;
            enemy.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private async Task AlignItemsAsync(CancellationToken cancellationToken)
    {
        var effectIds = await EffectSetIdsAsync(cancellationToken);
        var items = await _context.ItemDefinitions.ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.DisplayName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.Name : item.DisplayName;
            item.ItemType = string.IsNullOrWhiteSpace(item.ItemType) ? "Heal" : item.ItemType;
            item.UsageMode = string.IsNullOrWhiteSpace(item.UsageMode) ? "UseInCombat" : item.UsageMode;
            item.Lifecycle = string.IsNullOrWhiteSpace(item.Lifecycle) ? "RuntimeRunOnly" : item.Lifecycle;
            item.StackPolicy = string.IsNullOrWhiteSpace(item.StackPolicy) ? "Additive" : item.StackPolicy;
            item.MaxStack = item.MaxStack == 0 ? 1 : item.MaxStack;
            item.BaseWeight = item.BaseWeight == 0 ? 1 : item.BaseWeight;
            item.IsUsableInCombat = item.IsUsableInCombat || item.UsageMode == "UseInCombat";
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        SetEffectSet(items, "item.consumable.minor-heal", effectIds, "effect.item.minor-heal");
    }

    private async Task AlignLawsAsync(CancellationToken cancellationToken)
    {
        var effectIds = await EffectSetIdsAsync(cancellationToken);
        var laws = await _context.PalaceLawDefinitions.ToListAsync(cancellationToken);

        foreach (var law in laws)
        {
            law.DisplayName = string.IsNullOrWhiteSpace(law.DisplayName) ? law.Name : law.DisplayName;
            law.Scope = string.IsNullOrWhiteSpace(law.Scope) ? "Run" : law.Scope;
            law.Duration = string.IsNullOrWhiteSpace(law.Duration) ? "UntilRunEnds" : law.Duration;
            law.Severity = law.Severity == 0 ? 1 : law.Severity;
            law.BaseWeight = law.BaseWeight == 0 ? 1 : law.BaseWeight;
            law.UpdatedAtUtc = DateTime.UtcNow;
        }

        SetEffectSet(laws, "law.threshold.silence-weight", effectIds, "effect.law.silence-weight");
    }

    private async Task<Dictionary<string, Guid>> EffectSetIdsAsync(CancellationToken cancellationToken)
    {
        return await _context.EffectSets.ToDictionaryAsync(e => e.Key, e => e.Id, cancellationToken);
    }

    private static void SetEffectSet(IEnumerable<SkillDefinitionEntity> skills, string definitionKey, Dictionary<string, Guid> effectIds, string effectKey)
    {
        var skill = skills.FirstOrDefault(s => s.Key == definitionKey);
        if (skill is not null && effectIds.TryGetValue(effectKey, out var effectSetId)) skill.EffectSetId = effectSetId;
    }

    private static void SetEffectSet(IEnumerable<ItemDefinitionEntity> items, string definitionKey, Dictionary<string, Guid> effectIds, string effectKey)
    {
        var item = items.FirstOrDefault(s => s.Key == definitionKey);
        if (item is not null && effectIds.TryGetValue(effectKey, out var effectSetId)) item.EffectSetId = effectSetId;
    }

    private static void SetEffectSet(IEnumerable<PalaceLawDefinitionEntity> laws, string definitionKey, Dictionary<string, Guid> effectIds, string effectKey)
    {
        var law = laws.FirstOrDefault(s => s.Key == definitionKey);
        if (law is not null && effectIds.TryGetValue(effectKey, out var effectSetId)) law.EffectSetId = effectSetId;
    }

    private async Task SeedEnemyRelationsAsync(CancellationToken cancellationToken)
    {
        var enemies = await _context.EnemyDefinitions.ToListAsync(cancellationToken);
        var tags = await _context.CatalogTags.ToDictionaryAsync(t => t.TagKey, cancellationToken);

        foreach (var enemy in enemies)
        {
            if (!await _context.EnemyStatBlocks.AnyAsync(s => s.EnemyDefinitionId == enemy.Id, cancellationToken))
            {
                _context.EnemyStatBlocks.Add(new EnemyStatBlockEntity
                {
                    Id = Guid.NewGuid(),
                    EnemyDefinitionId = enemy.Id,
                    MaxVitality = enemy.IsElite ? 32 : 20,
                    AttackPower = enemy.IsElite ? 8 : 5,
                    Defense = enemy.IsElite ? 3 : 1,
                    StartingGuard = enemy.IsElite ? 4 : 0,
                    Speed = enemy.IsElite ? 12 : 10,
                    Initiative = 0,
                    Recovery = 0,
                    Focus = 0
                });
            }

            var skillKeys = JsonSerializer.Deserialize<string[]>(enemy.SkillKeysJson) ?? [];
            foreach (var skillKey in skillKeys)
            {
                if (!await _context.EnemySkillLinks.AnyAsync(l => l.EnemyDefinitionId == enemy.Id && l.SkillDefinitionKey == skillKey, cancellationToken))
                {
                    _context.EnemySkillLinks.Add(new EnemySkillLinkEntity { EnemyDefinitionId = enemy.Id, SkillDefinitionKey = skillKey });
                }
            }

            var tagKeys = JsonSerializer.Deserialize<string[]>(enemy.TagsJson) ?? [];
            foreach (var tagKey in tagKeys.Select(ToTagKey))
            {
                if (tags.TryGetValue(tagKey, out var tag)
                    && !await _context.Set<EnemyTagEntity>().AnyAsync(l => l.EnemyDefinitionId == enemy.Id && l.TagId == tag.Id, cancellationToken))
                {
                    _context.Set<EnemyTagEntity>().Add(new EnemyTagEntity { EnemyDefinitionId = enemy.Id, TagId = tag.Id });
                }
            }
        }
    }

    private async Task SeedAdditionalItemsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var effectIds = await EffectSetIdsAsync(cancellationToken);

        if (!await _context.ItemDefinitions.AnyAsync(i => i.Key == "item.consumable.eclat-de-garde", cancellationToken))
        {
            _context.ItemDefinitions.Add(new ItemDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "item.consumable.eclat-de-garde",
                Name = "Eclat de garde",
                DisplayName = "Eclat de garde",
                Description = "Renforce la garde initiale du prochain combat.",
                Version = DataModelVersion,
                Status = "Active",
                Category = "Consumable",
                ItemType = "Guard",
                Rarity = "Common",
                UsageMode = "UseOnNode",
                Lifecycle = "RuntimeRunOnly",
                StackPolicy = "Additive",
                MaxStack = 3,
                IsUsableInCombat = false,
                IsUsableOutsideCombat = true,
                EffectSetId = effectIds["effect.item.eclat-de-garde"],
                MinDepth = 1,
                BaseWeight = 1,
                Duration = "RunOnly",
                EffectValue = 8,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
    }

    private async Task SeedAdditionalLawsAndCursesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var effectIds = await EffectSetIdsAsync(cancellationToken);

        if (!await _context.PalaceLawDefinitions.AnyAsync(l => l.Key == "law.threshold.mefiance-des-echos", cancellationToken))
        {
            _context.PalaceLawDefinitions.Add(new PalaceLawDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "law.threshold.mefiance-des-echos",
                Name = "La Mefiance des Echos",
                DisplayName = "La Mefiance des Echos",
                Description = "Les ennemis deviennent plus mefiants.",
                Version = DataModelVersion,
                Status = "Active",
                Scope = "Room",
                Duration = "UntilRoomEnds",
                Trigger = "RoomEntered",
                Severity = 1,
                EffectSetId = effectIds["effect.law.mefiance-des-echos"],
                BaseWeight = 1,
                MinDepth = 1,
                SelectionGroup = "law.threshold",
                Visibility = "PartiallyVisible",
                Priority = 1,
                ImpactDomainsJson = "[\"Generation\",\"Combat\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await AddPalaceLawAsync("law-silence-v1", "Loi du Silence", "Le silence deforme la generation.", "Run", "UntilRunEnds", "Visible", 10, "[\"Generation\",\"Narrative\"]", "effect.law.silence-weight", effectIds, now, cancellationToken);
        await AddPalaceLawAsync("law-aegis-v1", "Loi de l'Egide", "La premiere garde du heros se renforce.", "Run", "UntilRunEnds", "Visible", 20, "[\"Combat\"]", "effect.law.aegis", effectIds, now, cancellationToken);
        await AddPalaceLawAsync("law-siege-v1", "Loi du Siege", "Les prochains affrontements gagnent en pression.", "Run", "UntilRunEnds", "Visible", 30, "[\"Combat\"]", "effect.law.siege", effectIds, now, cancellationToken);
        await AddPalaceLawAsync("law-carnage-v1", "Loi du Carnage", "La puissance d'attaque du heros augmente.", "Run", "UntilRunEnds", "Visible", 40, "[\"Combat\"]", "effect.law.carnage", effectIds, now, cancellationToken);
        await AddPalaceLawAsync("law-tempest-v1", "Loi de la Pluie", "La Room actuelle est traversee par la Pluie.", "Room", "UntilRoomEnds", "Visible", 50, "[\"Combat\"]", "effect.law.climate-rain", effectIds, now, cancellationToken);
        await AddPalaceLawAsync("law-hail-v1", "Loi de la Grele", "La Room actuelle est traversee par la Grele.", "Room", "UntilRoomEnds", "Visible", 60, "[\"Combat\"]", "effect.law.climate-hail", effectIds, now, cancellationToken);
        await AddPalaceLawAsync("law-drought-v1", "Loi de la Canicule", "La Room actuelle est ecrasee par la Canicule.", "Room", "UntilRoomEnds", "Visible", 70, "[\"Combat\"]", "effect.law.climate-heatwave", effectIds, now, cancellationToken);
        await AddPalaceLawAsync("law-grey-v1", "Loi de la Grisaille", "La Room actuelle est recouverte de Grisaille.", "Room", "UntilRoomEnds", "Visible", 80, "[\"Combat\"]", "effect.law.climate-grey", effectIds, now, cancellationToken);

        if (!await _context.CurseDefinitions.AnyAsync(c => c.Key == "curse.threshold.souffle-lourd", cancellationToken))
        {
            _context.CurseDefinitions.Add(new CurseDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "curse.threshold.souffle-lourd",
                DisplayName = "Souffle lourd",
                Description = "Le prochain combat pese davantage.",
                Severity = 1,
                Duration = "NextCombatOnly",
                Trigger = "NextCombatStarted",
                EffectSetId = effectIds["effect.curse.souffle-lourd"],
                BaseWeight = 1,
                MinDepth = 1,
                SelectionGroup = "curse.threshold",
                Version = DataModelVersion,
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
    }

    private async Task AddPalaceLawAsync(
        string key,
        string displayName,
        string description,
        string scope,
        string duration,
        string visibility,
        int priority,
        string impactDomainsJson,
        string effectSetKey,
        Dictionary<string, Guid> effectIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await _context.PalaceLawDefinitions.AnyAsync(l => l.Key == key, cancellationToken))
        {
            return;
        }

        _context.PalaceLawDefinitions.Add(new PalaceLawDefinitionEntity
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = displayName,
            DisplayName = displayName,
            Description = description,
            Version = DataModelVersion,
            Status = "Active",
            Scope = scope,
            Duration = duration,
            Severity = 1,
            EffectSetId = effectIds[effectSetKey],
            BaseWeight = 1,
            MinDepth = 1,
            SelectionGroup = "law.runtime",
            Visibility = visibility,
            Priority = priority,
            ImpactDomainsJson = impactDomainsJson,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private async Task AddCurseAsync(
        string key,
        string displayName,
        string description,
        string? narrativeText,
        int severity,
        string duration,
        string? trigger,
        string effectSetKey,
        Dictionary<string, Guid> effectIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await _context.CurseDefinitions.AnyAsync(c => c.Key == key, cancellationToken))
        {
            return;
        }

        _context.CurseDefinitions.Add(new CurseDefinitionEntity
        {
            Id = Guid.NewGuid(),
            Key = key,
            DisplayName = displayName,
            Description = description,
            NarrativeText = narrativeText,
            Severity = severity,
            Duration = duration,
            Trigger = trigger,
            EffectSetId = effectIds[effectSetKey],
            BaseWeight = 1,
            MinDepth = 1,
            SelectionGroup = "curse.gateway",
            Version = "1.0.0",
            Status = "Active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private async Task AddCatalogGatewayItemDefinitionsAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (!await _context.ItemDefinitions.AnyAsync(i => i.Key == "item.consumable.guard-shard", cancellationToken))
        {
            _context.ItemDefinitions.Add(new ItemDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "item.consumable.guard-shard",
                Name = "Eclat de garde",
                DisplayName = "Eclat de garde",
                Description = "Offre une protection permanente pendant la run.",
                Version = "1.0",
                Status = "Active",
                Category = "Consumable",
                ItemType = "Guard",
                Rarity = "Uncommon",
                UsageMode = "UseInCombat",
                Lifecycle = "RuntimeRunOnly",
                StackPolicy = "Additive",
                MaxStack = 99,
                IsUsableInCombat = true,
                IsUsableOutsideCombat = false,
                Duration = "RunOnly",
                EffectValue = 0,
                Price = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
    }

    private async Task AddCatalogGatewayRewardTemplatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        await AddRewardTemplateAsync("reward.combat.default", "Récompense de combat", "Récompense standard après un combat.", "Combat", now, cancellationToken,
            RewardOption("Heal", "Soin léger", "Soin léger", null, 10),
            RewardOption("Heal", "Souffle retrouvé", "Souffle retrouvé", null, 14),
            RewardOption("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", 15));
        await AddRewardTemplateAsync("reward.combat.rare", "Récompense rare", "Récompense pour un combat rare.", "Rare", now, cancellationToken,
            RewardOption("Heal", "Soin rare", "Soin rare", null, 15),
            RewardOption("Heal", "Répit lucide", "Répit lucide", null, 20),
            RewardOption("Heal", "Soin substantiel", "Soin substantiel", null, 25));
        await AddRewardTemplateAsync("reward.combat.elite", "Récompense élite", "Récompense pour un combat élite.", "Elite", now, cancellationToken,
            RewardOption("Heal", "Soin important", "Soin important", null, 20),
            RewardOption("Heal", "Volonté restaurée", "Volonté restaurée", null, 28),
            RewardOption("Heal", "Suture mentale", "Suture mentale", null, 36));
        await AddRewardTemplateAsync("reward.combat.boss", "Récompense de boss", "Récompense pour avoir vaincu un boss.", "RoomBoss", now, cancellationToken,
            RewardOption("Heal", "Soin majeur", "Soin majeur", null, 30),
            RewardOption("Heal", "Souffle du Gardien", "Souffle du Gardien", null, 42),
            RewardOption("Heal", "Silence recomposé", "Silence recomposé", null, 54));
        await AddRewardTemplateAsync("reward.item.default", "Récompense d'objet", "Récompense d'un noeud objet.", "NodeEvent", now, cancellationToken,
            RewardOption("TemporaryItem", "Éclat de garde", "Éclat de garde", "item.consumable.guard-shard", 8),
            RewardOption("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", 15),
            RewardOption("Heal", "Souffle du passé", "Souffle du passé", null, 10));
        await AddRewardTemplateAsync("reward.merchant.default", "Offre du marchand", "Récompense proposée par un marchand.", "NodeEvent", now, cancellationToken,
            RewardOption("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", 15),
            RewardOption("TemporaryItem", "Éclat de garde", "Éclat de garde", "item.consumable.guard-shard", 8),
            RewardOption("Heal", "Soin du marchand", "Soin du marchand", null, 12));
    }

    private async Task AddRewardTemplateAsync(
        string key,
        string displayName,
        string description,
        string sourceType,
        DateTime now,
        CancellationToken cancellationToken,
        params RewardTemplateOptionEntity[] options)
    {
        if (await _context.RewardTemplates.AnyAsync(template => template.Key == key, cancellationToken))
        {
            return;
        }

        _context.RewardTemplates.Add(new RewardTemplateEntity
        {
            Id = Guid.NewGuid(),
            Key = key,
            DisplayName = displayName,
            Description = description,
            SourceType = sourceType,
            MinOptions = 2,
            MaxOptions = 3,
            BaseWeight = 1,
            SelectionGroup = key,
            Version = "1.0",
            Status = "Active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Options = options.ToList()
        });
    }

    private static RewardTemplateOptionEntity RewardOption(
        string rewardType,
        string label,
        string description,
        string? payloadKey,
        int baseAmount)
    {
        return new RewardTemplateOptionEntity
        {
            Id = Guid.NewGuid(),
            RewardType = rewardType,
            Label = label,
            Description = description,
            PayloadKey = payloadKey,
            BaseAmount = baseAmount,
            ScalingMode = "Flat",
            Weight = 1
        };
    }

    private async Task SeedRewardTemplatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (await _context.RewardTemplates.AnyAsync(r => r.Key == "reward.combat.basic", cancellationToken))
        {
            return;
        }

        _context.RewardTemplates.Add(new RewardTemplateEntity
        {
            Id = Guid.NewGuid(),
            Key = "reward.combat.basic",
            DisplayName = "Butin de combat",
            Description = "Options simples apres un combat.",
            SourceType = "Combat",
            MinOptions = 1,
            MaxOptions = 2,
            MinDepth = 1,
            BaseWeight = 1,
            SelectionGroup = "reward.combat",
            Version = DataModelVersion,
            Status = "Active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Options =
            [
                new RewardTemplateOptionEntity
                {
                    Id = Guid.NewGuid(),
                    RewardType = "TemporaryItem",
                    Label = "Soin mineur",
                    Description = "Obtenir un soin mineur.",
                    PayloadKey = "item.consumable.minor-heal",
                    Weight = 1
                },
                new RewardTemplateOptionEntity
                {
                    Id = Guid.NewGuid(),
                    RewardType = "TemporaryItem",
                    Label = "Eclat de garde",
                    Description = "Obtenir un eclat de garde.",
                    PayloadKey = "item.consumable.eclat-de-garde",
                    Weight = 1
                }
            ]
        });
    }

    private async Task SeedRoomsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var effectIds = await EffectSetIdsAsync(cancellationToken);

        await AddRoomTypeAsync("room-type.threshold", "Salle du seuil", "Threshold", "Common", cancellationToken);
        await AddRoomTypeAsync("room-type.cultural-echo", "Echo culturel", "Alchemical", "Rare", cancellationToken);
        await AddRoomMechanicAsync("mechanic.equivalent-exchange", "Echange equivalent", "EquivalentExchange", effectIds["effect.mechanic.equivalent-exchange"], cancellationToken);
        await AddRoomBossAsync("room-boss.threshold.fracture", "Fracture du seuil", "Threshold", "enemy.threshold.fracture", cancellationToken);
        await AddRoomPoolsAsync(cancellationToken);

        if (!await _context.RoomDefinitions.AnyAsync(r => r.Key == "room.threshold.entry", cancellationToken))
        {
            _context.RoomDefinitions.Add(new RoomDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "room.threshold.entry",
                DisplayName = "Seuil silencieux",
                Description = "Une salle d'entree instable.",
                RoomFamily = "PalaceCore",
                RoomRarity = "Common",
                Theme = "Threshold",
                MinDepth = 1,
                MaxDepth = 3,
                BaseWeight = 1,
                SelectionGroup = "room.threshold",
                EnemyPoolKey = "room-pool.enemy.threshold-basic",
                RewardPoolKey = "room-pool.reward.basic",
                LawPoolKey = "room-pool.law.threshold",
                CursePoolKey = "room-pool.curse.threshold",
                Version = DataModelVersion,
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (!await _context.RoomDefinitions.AnyAsync(r => r.Key == "room.rare.creuset-equivalences", cancellationToken))
        {
            _context.RoomDefinitions.Add(new RoomDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "room.rare.creuset-equivalences",
                DisplayName = "Le Creuset des Équivalences",
                Description = "Une salle où chaque puissance offerte exige une dette.",
                NarrativeText = "Le metal de la promesse chante dans les murs.",
                RoomFamily = "CulturalEcho",
                RoomRarity = "Rare",
                Theme = "Alchemical",
                MinDepth = 2,
                BaseWeight = 1,
                SelectionGroup = "room.rare",
                EnemyPoolKey = "room-pool.enemy.threshold-basic",
                RewardPoolKey = "room-pool.reward.basic",
                LawPoolKey = "room-pool.law.threshold",
                CursePoolKey = "room-pool.curse.threshold",
                SpecialMechanicKey = "mechanic.equivalent-exchange",
                IsUnique = false,
                IsCulturalEcho = true,
                Version = DataModelVersion,
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
    }

    private async Task AddRoomTypeAsync(string key, string displayName, string theme, string rarity, CancellationToken cancellationToken)
    {
        if (!await _context.RoomTypeDefinitions.AnyAsync(r => r.Key == key, cancellationToken))
        {
            _context.RoomTypeDefinitions.Add(new RoomTypeDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                Description = displayName,
                Theme = theme,
                DefaultRarity = rarity,
                MinDepth = 1,
                Version = DataModelVersion,
                Status = "Active"
            });
        }
    }

    private async Task AddRoomMechanicAsync(string key, string displayName, string mechanicType, Guid effectSetId, CancellationToken cancellationToken)
    {
        if (!await _context.RoomSpecialMechanics.AnyAsync(r => r.Key == key, cancellationToken))
        {
            _context.RoomSpecialMechanics.Add(new RoomSpecialMechanicEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                Description = "Chaque puissance offerte exige une dette.",
                MechanicType = mechanicType,
                EffectSetId = effectSetId,
                Version = DataModelVersion,
                Status = "Active"
            });
        }
    }

    private async Task AddRoomBossAsync(string key, string displayName, string roomType, string enemyKey, CancellationToken cancellationToken)
    {
        if (!await _context.RoomBossDefinitions.AnyAsync(r => r.Key == key, cancellationToken))
        {
            var now = DateTime.UtcNow;
            _context.RoomBossDefinitions.Add(new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                Description = "Un boss de salle lie au seuil.",
                RoomType = roomType,
                EnemyDefinitionKey = enemyKey,
                DangerHint = "Rupture instable",
                BaseDifficulty = 1,
                BaseWeight = 1,
                SelectionGroup = "boss.threshold",
                Version = DataModelVersion,
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
    }

    private async Task AddRoomPoolsAsync(CancellationToken cancellationToken)
    {
        if (!await _context.RoomEnemyPools.AnyAsync(p => p.Key == "room-pool.enemy.threshold-basic", cancellationToken))
        {
            _context.RoomEnemyPools.Add(new RoomEnemyPoolEntity
            {
                Id = Guid.NewGuid(),
                Key = "room-pool.enemy.threshold-basic",
                DisplayName = "Ennemis du seuil",
                Description = "Pool minimal d'ennemis du seuil.",
                MinDepth = 1,
                SelectionGroup = "enemy.threshold",
                Version = DataModelVersion,
                Status = "Active",
                Entries =
                [
                    new RoomEnemyPoolEntryEntity { Id = Guid.NewGuid(), EnemyDefinitionKey = "enemy.threshold.echo", Weight = 2, MinCount = 1, MaxCount = 2 },
                    new RoomEnemyPoolEntryEntity { Id = Guid.NewGuid(), EnemyDefinitionKey = "enemy.threshold.fracture", Weight = 1, MinDepth = 2, MinCount = 1, MaxCount = 1, RequiredTag = "tag.enemy.elite" }
                ]
            });
        }

        if (!await _context.RoomRewardPools.AnyAsync(p => p.Key == "room-pool.reward.basic", cancellationToken))
        {
            _context.RoomRewardPools.Add(new RoomRewardPoolEntity
            {
                Id = Guid.NewGuid(),
                Key = "room-pool.reward.basic",
                DisplayName = "Recompenses de base",
                Description = "Pool minimal de recompenses.",
                MinDepth = 1,
                SelectionGroup = "reward.basic",
                Version = DataModelVersion,
                Status = "Active",
                Entries =
                [
                    new RoomRewardPoolEntryEntity { Id = Guid.NewGuid(), RewardTemplateKey = "reward.combat.basic", Weight = 1 }
                ]
            });
        }

        if (!await _context.RoomLawPools.AnyAsync(p => p.Key == "room-pool.law.threshold", cancellationToken))
        {
            _context.RoomLawPools.Add(new RoomLawPoolEntity
            {
                Id = Guid.NewGuid(),
                Key = "room-pool.law.threshold",
                DisplayName = "Lois du seuil",
                Description = "Pool minimal de lois.",
                MinDepth = 1,
                SelectionGroup = "law.threshold",
                Version = DataModelVersion,
                Status = "Active",
                Entries =
                [
                    new RoomLawPoolEntryEntity { Id = Guid.NewGuid(), LawDefinitionKey = "law.threshold.silence-weight", Weight = 1 },
                    new RoomLawPoolEntryEntity { Id = Guid.NewGuid(), LawDefinitionKey = "law.threshold.mefiance-des-echos", Weight = 1 }
                ]
            });
        }

        if (!await _context.RoomCursePools.AnyAsync(p => p.Key == "room-pool.curse.threshold", cancellationToken))
        {
            _context.RoomCursePools.Add(new RoomCursePoolEntity
            {
                Id = Guid.NewGuid(),
                Key = "room-pool.curse.threshold",
                DisplayName = "Maledictions du seuil",
                Description = "Pool minimal de maledictions.",
                MinDepth = 1,
                SelectionGroup = "curse.threshold",
                Version = DataModelVersion,
                Status = "Active",
                Entries =
                [
                    new RoomCursePoolEntryEntity { Id = Guid.NewGuid(), CurseDefinitionKey = "curse.threshold.souffle-lourd", Weight = 1 }
                ]
            });
        }
    }

    private async Task ApplyCatalogTemplatesSeedAsync(CancellationToken cancellationToken)
    {
        if (await HasSeedVersionAsync(CatalogTemplatesVersion, cancellationToken))
        {
            _logger.LogInformation("Seed {SeedKey} version {Version} already applied. Skipping.", SeedKey, CatalogTemplatesVersion);
            return;
        }

        _logger.LogInformation("Applying seed {SeedKey} version {Version}...", SeedKey, CatalogTemplatesVersion);

        var now = DateTime.UtcNow;

        await SeedEnemyTemplatesAsync(now, cancellationToken);
        await SeedSkillTemplatesAsync(now, cancellationToken);
        await SeedEventTemplatesAsync(now, cancellationToken);
        await SeedRoomBossCatalogAsync(now, cancellationToken);

        AddSeedVersion(CatalogTemplatesVersion);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed {SeedKey} version {Version} applied successfully.", SeedKey, CatalogTemplatesVersion);
    }

    private async Task SeedEnemyTemplatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (await _context.EnemyTemplates.AnyAsync(e => e.Key == "enemy-shadow-wolf", cancellationToken))
        {
            return;
        }

        _context.EnemyTemplates.Add(new EnemyTemplateEntity
        {
            Id = Guid.NewGuid(),
            Key = "enemy-shadow-wolf",
            Name = "Loup d'Ombre",
            Description = "Une entité née dans un couloir du Palais.",
            Version = "catalog-0.1.0",
            Status = "Active",
            Archetype = "Trauma",
            Element = "Shadow",
            MaxHealth = 120,
            Strength = 18,
            Intelligence = 8,
            Speed = 14,
            PhysicalResistance = 10,
            MagicalResistance = 5,
            ExperienceReward = 25,
            GoldReward = 12,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private async Task SeedSkillTemplatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (await _context.SkillTemplates.AnyAsync(s => s.Key == "skill-shadow-bite", cancellationToken))
        {
            return;
        }

        _context.SkillTemplates.AddRange(
            new SkillTemplateEntity
            {
                Id = Guid.NewGuid(),
                Key = "skill-shadow-bite",
                Name = "Morsure d'Ombre",
                Description = "Une attaque de rupture silencieuse.",
                Version = "catalog-0.1.0",
                Status = "Active",
                Element = "Shadow",
                EffectType = "Damage",
                TargetType = "SingleEnemy",
                ManaCost = 3,
                ChargeCost = 1,
                BasePower = 35,
                HealPower = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SkillTemplateEntity
            {
                Id = Guid.NewGuid(),
                Key = "skill-memory-mend",
                Name = "Suture de Mémoire",
                Description = "Restaure une partie de soi.",
                Version = "catalog-0.1.0",
                Status = "Active",
                Element = "Memory",
                EffectType = "Heal",
                TargetType = "SingleAlly",
                ManaCost = 4,
                ChargeCost = 1,
                BasePower = 0,
                HealPower = 25,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
    }

    private async Task SeedEventTemplatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (await _context.EventTemplates.AnyAsync(e => e.Key == "event-memory-threshold-v1", cancellationToken))
        {
            return;
        }

        _context.EventTemplates.AddRange(
            new EventTemplateEntity
            {
                Id = Guid.NewGuid(),
                Key = "event-memory-threshold-v1",
                Name = "Mémoire du seuil",
                Description = "Une mémoire apparaît à l'entrée du Palais.",
                Version = "event-1.0.0",
                Status = "Active",
                Type = "Memory",
                DefaultOutcomeKind = "TomePageUnlocked",
                MinRiskLevel = 5,
                MaxRiskLevel = 20,
                RequiresPlayerChoice = true,
                NarrativeTagsJson = "[\"memory\",\"threshold\",\"elise\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new EventTemplateEntity
            {
                Id = Guid.NewGuid(),
                Key = "event-law-silence-v1",
                Name = "Écho du Silence",
                Description = "Une Loi du Palais se manifeste dans la pièce.",
                Version = "event-1.0.0",
                Status = "Active",
                Type = "Law",
                DefaultOutcomeKind = "PalaceLawApplied",
                MinRiskLevel = 10,
                MaxRiskLevel = 35,
                RequiresPlayerChoice = true,
                NarrativeTagsJson = "[\"law\",\"silence\",\"palace\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new EventTemplateEntity
            {
                Id = Guid.NewGuid(),
                Key = "event-combat-shadow-v1",
                Name = "Combat d'ombre",
                Description = "Une ombre du Palais bloque le chemin.",
                Version = "event-1.0.0",
                Status = "Active",
                Type = "Combat",
                DefaultOutcomeKind = "CombatStarted",
                MinRiskLevel = 15,
                MaxRiskLevel = 45,
                RequiresPlayerChoice = false,
                NarrativeTagsJson = "[\"combat\",\"shadow\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
    }

    private async Task SeedRoomBossCatalogAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (await _context.RoomBossDefinitions.AnyAsync(r => r.Key == "boss.threshold.warden", cancellationToken))
        {
            return;
        }

        _context.RoomBossDefinitions.AddRange(
            new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "boss.threshold.warden",
                DisplayName = "Warden of the Threshold",
                Description = "The first sentinel guarding the entrance to the Memory Palace, a being of pure vigilance.",
                RoomType = "Threshold",
                BaseDifficulty = 1,
                TagsJson = "[\"sentinel\",\"guardian\",\"threshold\"]",
                Version = "1.0.0",
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "boss.forest.rootbound-memory",
                DisplayName = "Rootbound Memory",
                Description = "An ancient entity whose roots dig deep into forgotten epochs, anchoring the forest's will.",
                RoomType = "Forest",
                BaseDifficulty = 2,
                TagsJson = "[\"ancient\",\"forest\",\"roots\"]",
                Version = "1.0.0",
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "boss.rupture.fractured-echo",
                DisplayName = "Fractured Echo",
                Description = "A shattered remnant of a once-coherent thought, now a cacophony of broken memories.",
                RoomType = "Rupture",
                BaseDifficulty = 3,
                TagsJson = "[\"shattered\",\"echo\",\"rupture\"]",
                Version = "1.0.0",
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "boss.silence.mute-herald",
                DisplayName = "Mute Herald",
                Description = "A silent messenger whose presence absorbs all sound, leaving only dread in the void.",
                RoomType = "Silence",
                BaseDifficulty = 4,
                TagsJson = "[\"silent\",\"herald\",\"void\"]",
                Version = "1.0.0",
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "boss.antechamber.last-door",
                DisplayName = "The Last Door",
                Description = "The final barrier before the deepest memories, an immovable ward of immense presence.",
                RoomType = "Antechamber",
                BaseDifficulty = 5,
                TagsJson = "[\"barrier\",\"ward\",\"antechamber\"]",
                Version = "1.0.0",
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "boss.memory.archivist",
                DisplayName = "Archivist of Lost Moments",
                Description = "The keeper of forgotten memories, cataloging every experience that slips through the cracks of time.",
                RoomType = "Memory",
                BaseDifficulty = 4,
                TagsJson = "[\"archivist\",\"memory\",\"keeper\"]",
                Version = "1.0.0",
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "boss.final.himlit",
                DisplayName = "Himlit",
                Description = "The final, eldritch entity at the heart of the Memory Palace. Its true name is a whisper lost in the abyss of ultimate remembrance.",
                RoomType = "Final",
                BaseDifficulty = 10,
                TagsJson = "[\"final\",\"eldritch\",\"heart\"]",
                Version = "1.0.0",
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
    }

    private static string ToTagKey(string tag)
    {
        return tag switch
        {
            "threshold" => "tag.enemy.threshold",
            "common" => "tag.enemy.common",
            "elite" => "tag.enemy.elite",
            _ => tag
        };
    }
}
