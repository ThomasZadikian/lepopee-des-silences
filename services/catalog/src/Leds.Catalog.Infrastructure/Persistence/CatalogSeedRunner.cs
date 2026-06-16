using System.Text.Json;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed class CatalogSeedRunner
{
    private const string SeedKey = "base-catalog";
    private const string LegacyVersion = "alpha-0.5.5";
    private const string DataModelVersion = "alpha-0.7.1";

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
            _context.RoomBossDefinitions.Add(new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                Description = "Un boss de salle lie au seuil.",
                RoomType = roomType,
                EnemyDefinitionKey = enemyKey,
                DangerHint = "Rupture instable",
                BaseWeight = 1,
                SelectionGroup = "boss.threshold",
                Version = DataModelVersion,
                Status = "Active"
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
