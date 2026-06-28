using System.Text.Json;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Domain.Npcs;
using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed class CatalogSeedRunner
{
    private const string SeedKey = "base-catalog";
    private const string LegacyVersion = "alpha-0.5.5";
    private const string DataModelVersion = "alpha-0.8.1";
    private const string CatalogGatewayContentVersion = "alpha-0.8.1-catalog-content-gateway";
    private const string CatalogTemplatesVersion = "alpha-0.9.2-catalog-templates";
    private const string CatalogAntechamberFixVersion = "alpha-1.0.1-antechamber-boss-fix";
    private const string NpcSystemVersion = "npc-system-0.1-majordome";

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
        await ApplyCatalogAntechamberBossFixSeedAsync(cancellationToken);
        await ApplyNpcSystemSeedAsync(cancellationToken);
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

    private static readonly JsonSerializerOptions NpcSeedJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task ApplyNpcSystemSeedAsync(CancellationToken cancellationToken)
    {
        if (await HasSeedVersionAsync(NpcSystemVersion, cancellationToken))
        {
            _logger.LogInformation("Seed {SeedKey} version {Version} already applied. Skipping.", SeedKey, NpcSystemVersion);
            return;
        }

        _logger.LogInformation("Applying seed {SeedKey} version {Version}...", SeedKey, NpcSystemVersion);

        var now = DateTime.UtcNow;
        await SeedMajordomeAsync(now, cancellationToken);

        AddSeedVersion(NpcSystemVersion);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed {SeedKey} version {Version} applied successfully.", SeedKey, NpcSystemVersion);
    }

    private async Task SeedMajordomeAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (!await _context.RewardCursePools.AnyAsync(p => p.Key == "pool.majordome.eau-benigne", cancellationToken))
        {
            var benign = new List<RewardCurseEntry>
            {
                new(RewardCurseEntryKind.Reward, "Heal", null, 15, null),
                new(RewardCurseEntryKind.Reward, "Heal", null, 9, null)
            };
            _context.RewardCursePools.Add(new RewardCursePoolEntity
            {
                Id = Guid.NewGuid(),
                Key = "pool.majordome.eau-benigne",
                Name = "Eau du Majordome — bienveillante",
                Description = "Ce que l'eau offre quand le seuil est respecté.",
                Version = "1.0",
                Status = "Active",
                EntriesJson = JsonSerializer.Serialize(benign, NpcSeedJsonOptions),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (!await _context.RewardCursePools.AnyAsync(p => p.Key == "pool.majordome.eau-poison", cancellationToken))
        {
            var poison = new List<RewardCurseEntry>
            {
                new(RewardCurseEntryKind.Curse, "GrantCurse", "curse.old-wound", 0, null),
                new(RewardCurseEntryKind.Curse, "Damage", null, 12, null)
            };
            _context.RewardCursePools.Add(new RewardCursePoolEntity
            {
                Id = Guid.NewGuid(),
                Key = "pool.majordome.eau-poison",
                Name = "Eau du Majordome — empoisonnée",
                Description = "Ce que l'offrande devient quand le seuil est souillé.",
                Version = "1.0",
                Status = "Active",
                EntriesJson = JsonSerializer.Serialize(poison, NpcSeedJsonOptions),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (await _context.NpcDefinitions.AnyAsync(n => n.Key == "npc.majordome", cancellationToken))
        {
            return;
        }

        var persona = new NpcPersona(
            "Courtois, attentif, d'une politesse glaçante",
            EmotionalRegister.Silence,
            new[] { "le respect du seuil", "la propreté du tapis" },
            new[] { "thé", "eau", "attention" });

        var wounds = new List<NpcWound>
        {
            new(
                "w-tapis",
                EmotionalRegister.Rupture,
                NpcWoundReversibility.Irreversible,
                TenseThreshold: -2,
                RuptureThreshold: -4,
                new[] { new NpcTransgression("w-tapis", "tapis-souille", -5) },
                "Le seuil a été souillé. Cela ne se pardonne pas.")
        };

        var seuil = new NpcDialogueNode(
            "seuil",
            "Le Majordome",
            new[]
            {
                "Entrez. Le thé est encore chaud.",
                "Veillez à vos pas — ce tapis a connu des hôtes moins soigneux."
            },
            new[]
            {
                new NpcDialogueChoice(
                    "salir", "Entrer sans essuyer vos pieds",
                    Array.Empty<DialogueRequirement>(),
                    new[]
                    {
                        new DialogueConsequence(ConsequenceKind.SetMemoryFlag, MemoryFlag: "tapis-souille"),
                        new DialogueConsequence(ConsequenceKind.Narrative, NarrativeFragmentKey: "Vos semelles laissent une trace sombre sur le tapis. Le Majordome ne dit rien.")
                    },
                    NextNodeKey: "seuil"),

                new NpcDialogueChoice(
                    "boire", "Boire l'eau",
                    Array.Empty<DialogueRequirement>(),
                    new[]
                    {
                        new DialogueConsequence(ConsequenceKind.RewardOrCurseRoll, WhenWoundState: WoundState.Latent, RewardCursePoolKey: "pool.majordome.eau-benigne"),
                        new DialogueConsequence(ConsequenceKind.Narrative, WhenWoundState: WoundState.Rompu, NarrativeFragmentKey: "Le thé a un goût d'amertume que vous ne sauriez nommer."),
                        new DialogueConsequence(ConsequenceKind.RewardOrCurseRoll, WhenWoundState: WoundState.Rompu, RewardCursePoolKey: "pool.majordome.eau-poison")
                    },
                    NextNodeKey: null),

                new NpcDialogueChoice(
                    "questionner", "L'interroger sur le tapis",
                    Array.Empty<DialogueRequirement>(),
                    new[]
                    {
                        new DialogueConsequence(ConsequenceKind.Narrative, NarrativeFragmentKey: "Il sourit. Ses mains, elles, se crispent.")
                    },
                    NextNodeKey: "confidence"),

                new NpcDialogueChoice(
                    "partir", "S'éloigner",
                    Array.Empty<DialogueRequirement>(),
                    new[]
                    {
                        new DialogueConsequence(ConsequenceKind.Narrative, NarrativeFragmentKey: "Vous reculez. Le Majordome incline la tête, impeccable.")
                    },
                    NextNodeKey: null)
            });

        var confidence = new NpcDialogueNode(
            "confidence",
            "Le Majordome",
            new[] { "« Le seuil se respecte. Toujours. Ceux qui l'oublient… ne reviennent pas. »" },
            new[]
            {
                new NpcDialogueChoice(
                    "comprendre", "Hocher la tête",
                    Array.Empty<DialogueRequirement>(),
                    new[]
                    {
                        new DialogueConsequence(ConsequenceKind.AdjustRelationship, RelationshipDelta: 1),
                        new DialogueConsequence(ConsequenceKind.Narrative, NarrativeFragmentKey: "Quelque chose dans son regard s'apaise, à peine.")
                    },
                    NextNodeKey: "seuil"),

                new NpcDialogueChoice(
                    "partir", "S'éloigner",
                    Array.Empty<DialogueRequirement>(),
                    new[] { new DialogueConsequence(ConsequenceKind.Narrative, NarrativeFragmentKey: "Vous le laissez à son seuil.") },
                    NextNodeKey: null)
            });

        var graph = new NpcDialogueGraph(
            "npc.majordome.dialogue",
            "1.0",
            "seuil",
            new Dictionary<string, NpcDialogueNode>
            {
                ["seuil"] = seuil,
                ["confidence"] = confidence
            });

        _context.NpcDefinitions.Add(new NpcDefinitionEntity
        {
            Id = Guid.NewGuid(),
            Key = "npc.majordome",
            Name = "Le Majordome",
            DisplayName = "Le Majordome",
            Description = "Une présence du seuil : il accueille, il sert, il veille. Et il n'oublie rien.",
            Version = "1.0",
            Status = "Active",
            MinDepth = null,
            MaxDepth = null,
            CompatibleRoomTypesJson = "[]",
            CompatiblePalaceRoomStatesJson = "[]",
            CompatibleRoomClimatesJson = "[]",
            TagsJson = "[]",
            EmotionalAffinity = "Silence",
            IsRecurring = true,
            PersonaJson = JsonSerializer.Serialize(persona, NpcSeedJsonOptions),
            WoundsJson = JsonSerializer.Serialize(wounds, NpcSeedJsonOptions),
            DialogueGraphJson = JsonSerializer.Serialize(graph, NpcSeedJsonOptions),
            EncounterKeysJson = "[]",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
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
                EffectType = "Damage",
                CostType = "None",
                ManaCost = 5,
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
            },
            new SkillDefinitionEntity
            {
                Id = Guid.Parse("d3e4f5a6-b7c8-4d9e-0f1a-2b3c4d5e6f7a"),
                Key = "skill.basic.weaken",
                Name = "Affaiblissement",
                DisplayName = "Affaiblissement",
                Description = "Une mal\u00e9diction qui r\u00e9duit la puissance d'un ennemi.",
                Version = LegacyVersion,
                Status = "Active",
                SkillType = "Debuff",
                TargetingType = "SingleEnemy",
                TargetingMode = "SingleEnemy",
                EffectType = "Debuff",
                CostType = "None",
                ManaCost = 4,
                ChargeCost = 0,
                BasePower = 0,
                Power = 0,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SkillDefinitionEntity
            {
                Id = Guid.Parse("e4f5a6b7-c8d9-4e0f-1a2b-3c4d5e6f7a8b"),
                Key = "skill.basic.disrupt",
                Name = "Perturbation",
                DisplayName = "Perturbation",
                Description = "Une interf\u00e9rence qui d\u00e9sorganise les comp\u00e9tences ennemies.",
                Version = LegacyVersion,
                Status = "Active",
                SkillType = "Debuff",
                TargetingType = "SingleEnemy",
                TargetingMode = "SingleEnemy",
                EffectType = "Debuff",
                CostType = "None",
                ManaCost = 6,
                ChargeCost = 1,
                BasePower = 0,
                Power = 0,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SkillDefinitionEntity
            {
                Id = Guid.Parse("f5a6b7c8-d9e0-4f1a-2b3c-4d5e6f7a8b9c"),
                Key = "skill.basic.focus",
                Name = "Concentration",
                DisplayName = "Concentration",
                Description = "Un \u00e9tat de focalisation qui augmente la puissance du prochain sort.",
                Version = LegacyVersion,
                Status = "Active",
                SkillType = "Buff",
                TargetingType = "Self",
                TargetingMode = "Self",
                EffectType = "Buff",
                CostType = "None",
                ManaCost = 2,
                ChargeCost = 0,
                BasePower = 0,
                Power = 0,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var skill in skills)
        {
            var existing = await _context.SkillDefinitions
                .FirstOrDefaultAsync(s => s.Key == skill.Key, cancellationToken);

            if (existing is not null)
            {
                existing.Name = skill.Name;
                existing.DisplayName = skill.DisplayName;
                existing.Description = skill.Description;
                existing.SkillType = skill.SkillType;
                existing.TargetingType = skill.TargetingType;
                existing.TargetingMode = skill.TargetingMode;
                existing.EffectType = skill.EffectType;
                existing.CostType = skill.CostType;
                existing.ManaCost = skill.ManaCost;
                existing.ChargeCost = skill.ChargeCost;
                existing.BasePower = skill.BasePower;
                existing.Power = skill.Power;
                existing.Accuracy = skill.Accuracy;
                existing.ActionCost = skill.ActionCost;
                existing.BaseWeight = skill.BaseWeight;
                existing.UpdatedAtUtc = now;
            }
            else
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
            },
            new EnemyDefinitionEntity
            {
                Id = Guid.Parse("e7f8a9b0-c1d2-4e3f-4a5b-6c7d8e9f0a1b"),
                Key = "enemy.threshold.doubt-fragment",
                Name = "Fragment de Doute",
                DisplayName = "Fragment de Doute",
                Description = "Un eclat de doute emanant du seuil.",
                Version = LegacyVersion,
                Status = "Active",
                Archetype = "Fragile",
                Family = "Threshold",
                Rank = "Common",
                Role = "DPS",
                BaseDifficulty = 1,
                EncounterWeight = 1,
                MinRiskLevel = 1,
                MaxRiskLevel = 2,
                MinDepth = 1,
                MaxDepth = 1,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Threshold\"]",
                TagsJson = "[\"threshold\"]",
                SkillKeysJson = "[\"skill.basic.strike\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new EnemyDefinitionEntity
            {
                Id = Guid.Parse("f8a9b0c1-d2e3-4f4a-5b6c-7d8e9f0a1b2c"),
                Key = "enemy.silence.mute-witness",
                Name = "Temoin Muet",
                DisplayName = "Temoin Muet",
                Description = "Un temoin silencieux du palais.",
                Version = LegacyVersion,
                Status = "Active",
                Archetype = "Shadow",
                Family = "Silence",
                Rank = "Common",
                Role = "DPS",
                BaseDifficulty = 1,
                EncounterWeight = 1,
                MinRiskLevel = 1,
                MaxRiskLevel = 20,
                MinDepth = 1,
                MaxDepth = 3,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Silence\"]",
                TagsJson = "[\"silence\"]",
                SkillKeysJson = "[\"skill.basic.strike\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new EnemyDefinitionEntity
            {
                Id = Guid.Parse("a9b0c1d2-e3f4-4f5a-6b7c-8d9e0f1a2b3c"),
                Key = "enemy.silence.absent-voice",
                Name = "Voix Absente",
                DisplayName = "Voix Absente",
                Description = "Une presence privee de voix.",
                Version = LegacyVersion,
                Status = "Active",
                Archetype = "Trauma",
                Family = "Silence",
                Rank = "Common",
                Role = "Disruptor",
                BaseDifficulty = 1,
                EncounterWeight = 1,
                MinRiskLevel = 1,
                MaxRiskLevel = 25,
                MinDepth = 1,
                MaxDepth = 4,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Silence\"]",
                TagsJson = "[\"silence\"]",
                SkillKeysJson = "[\"skill.basic.strike\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new EnemyDefinitionEntity
            {
                Id = Guid.Parse("b0c1d2e3-f4a5-4f6b-7c8d-9e0f1a2b3c4d"),
                Key = "enemy.final.silent-double",
                Name = "Double Silencieux",
                DisplayName = "Double Silencieux",
                Description = "Un reflet du silence dans la salle finale.",
                Version = LegacyVersion,
                Status = "Active",
                Archetype = "Shadow",
                Family = "Final",
                Rank = "Elite",
                Role = "DPS",
                BaseDifficulty = 3,
                EncounterWeight = 1,
                MinRiskLevel = 40,
                MaxRiskLevel = 80,
                MinDepth = 8,
                MaxDepth = 12,
                IsElite = true,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Final\"]",
                TagsJson = "[\"final\",\"elite\"]",
                SkillKeysJson = "[\"skill.basic.strike\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new EnemyDefinitionEntity
            {
                Id = Guid.Parse("c1d2e3f4-a5b6-4f7c-8d9e-0f1a2b3c4d5e"),
                Key = "enemy.final.last-echo",
                Name = "Dernier Echo",
                DisplayName = "Dernier Echo",
                Description = "Le dernier echo avant l'oubli.",
                Version = LegacyVersion,
                Status = "Active",
                Archetype = "Trauma",
                Family = "Final",
                Rank = "Elite",
                Role = "Disruptor",
                BaseDifficulty = 3,
                EncounterWeight = 1,
                MinRiskLevel = 35,
                MaxRiskLevel = 75,
                MinDepth = 7,
                MaxDepth = 12,
                IsElite = true,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Final\"]",
                TagsJson = "[\"final\",\"elite\"]",
                SkillKeysJson = "[\"skill.basic.strike\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var enemy in enemies)
        {
            var existing = await _context.EnemyDefinitions
                .FirstOrDefaultAsync(e => e.Key == enemy.Key, cancellationToken);

            if (existing is not null)
            {
                existing.Name = enemy.Name;
                existing.DisplayName = enemy.DisplayName;
                existing.Description = enemy.Description;
                existing.Archetype = enemy.Archetype;
                existing.BaseDifficulty = enemy.BaseDifficulty;
                existing.MinRiskLevel = enemy.MinRiskLevel;
                existing.MaxRiskLevel = enemy.MaxRiskLevel;
                existing.CompatibleRoomTypesJson = enemy.CompatibleRoomTypesJson;
                existing.TagsJson = enemy.TagsJson;
                existing.SkillKeysJson = enemy.SkillKeysJson;
                existing.UpdatedAtUtc = now;
            }
            else
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

        if (!await _context.ItemDefinitions.AnyAsync(i => i.Key == "item-memory-potion", cancellationToken))
        {
            _context.ItemDefinitions.Add(new ItemDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "item-memory-potion",
                Name = "Potion de M\u00e9moire",
                DisplayName = "Potion de M\u00e9moire",
                Description = "Restaure une ressource mentale pendant la run.",
                Version = "1.0",
                Status = "Active",
                Category = "Consumable",
                ItemType = "Memory",
                Rarity = "Common",
                UsageMode = "UseOutsideCombat",
                Lifecycle = "RuntimeRunOnly",
                StackPolicy = "Additive",
                MaxStack = 3,
                IsUsableInCombat = false,
                IsUsableOutsideCombat = true,
                Duration = "RunOnly",
                EffectValue = 25,
                Price = 10,
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

        await SeedSkillDefinitionsAsync(cancellationToken);
        await SeedEnemyDefinitionsAsync(cancellationToken);
        await SeedEnemyTemplatesAsync(now, cancellationToken);
        await SeedSkillTemplatesAsync(now, cancellationToken);
        await SeedEventTemplatesAsync(now, cancellationToken);
        await SeedRoomBossCatalogAsync(now, cancellationToken);
        await AddCatalogGatewayItemDefinitionsAsync(now, cancellationToken);

        AddSeedVersion(CatalogTemplatesVersion);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed {SeedKey} version {Version} applied successfully.", SeedKey, CatalogTemplatesVersion);
    }

    private async Task ApplyCatalogAntechamberBossFixSeedAsync(CancellationToken cancellationToken)
    {
        if (await HasSeedVersionAsync(CatalogAntechamberFixVersion, cancellationToken))
        {
            _logger.LogInformation("Seed {SeedKey} version {Version} already applied. Skipping.", SeedKey, CatalogAntechamberFixVersion);
            return;
        }

        _logger.LogInformation("Applying seed {SeedKey} version {Version}...", SeedKey, CatalogAntechamberFixVersion);

        var now = DateTime.UtcNow;

        await SeedAntechamberSkillsAsync(now, cancellationToken);
        await SeedAntechamberEnemiesAsync(now, cancellationToken);

        await AddRoomBossAsync("room-boss.memory.archivist", "Archiviste des Échos", "Memory", "enemy.threshold.echo", cancellationToken);
        await AddRoomBossAsync("room-boss.forest.rootbound-memory", "Gardien des Racines", "Forest", "enemy.threshold.fracture", cancellationToken);
        await AddRoomBossAsync("room-boss.rupture.fractured-echo", "Fragment de Rupture", "Rupture", "enemy.threshold.fracture", cancellationToken);
        await AddRoomBossAsync("room-boss.silence.mute-herald", "Voix Éteinte", "Silence", "enemy.silence.mute-witness", cancellationToken);
        await AddRoomBossAsync("room-boss.antechamber.last-door", "Gardien de l'Antichambre", "Antechamber", "enemy.antechamber.door-keeper", cancellationToken);
        await AddRoomBossAsync("room-boss.final.himlit", "Him'Lit", "Final", "enemy.final.silent-double", cancellationToken);

        AddSeedVersion(CatalogAntechamberFixVersion);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed {SeedKey} version {Version} applied successfully.", SeedKey, CatalogAntechamberFixVersion);
    }

    private async Task SeedAntechamberSkillsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var skills = new[]
        {
            new SkillDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "skill.basic.shield",
                Name = "Bouclier",
                DisplayName = "Bouclier",
                Description = "Un bouclier qui absorbe les degats pendant un tour.",
                Version = CatalogAntechamberFixVersion,
                Status = "Active",
                SkillType = "Defense",
                TargetingType = "Self",
                TargetingMode = "Self",
                EffectType = "Guard",
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
            },
            new SkillDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "skill.basic.taunt",
                Name = "Provocation",
                DisplayName = "Provocation",
                Description = "Force l'ennemi à cibler le lanceur.",
                Version = CatalogAntechamberFixVersion,
                Status = "Active",
                SkillType = "Utility",
                TargetingType = "SingleEnemy",
                TargetingMode = "SingleEnemy",
                EffectType = "Utility",
                CostType = "Mana",
                ManaCost = 3,
                ChargeCost = 0,
                BasePower = 0,
                Power = 0,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SkillDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "skill.basic.charge",
                Name = "Charge",
                DisplayName = "Charge",
                Description = "Une charge puissante qui inflige des degats supplémentaires.",
                Version = CatalogAntechamberFixVersion,
                Status = "Active",
                SkillType = "Damage",
                TargetingType = "SingleEnemy",
                TargetingMode = "SingleEnemy",
                EffectType = "Damage",
                CostType = "Mana",
                ManaCost = 7,
                ChargeCost = 1,
                BasePower = 18,
                Power = 18,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SkillDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "skill.basic.enrage",
                Name = "Enragement",
                DisplayName = "Enragement",
                Description = "Augmente la puissance d'attaque au prix de la défense.",
                Version = CatalogAntechamberFixVersion,
                Status = "Active",
                SkillType = "Buff",
                TargetingType = "Self",
                TargetingMode = "Self",
                EffectType = "Buff",
                CostType = "Mana",
                ManaCost = 5,
                ChargeCost = 1,
                BasePower = 0,
                Power = 0,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var skill in skills)
        {
            var existing = await _context.SkillDefinitions
                .FirstOrDefaultAsync(s => s.Key == skill.Key, cancellationToken);
            if (existing is null)
            {
                _context.SkillDefinitions.Add(skill);
            }
        }
    }

    private async Task SeedAntechamberEnemiesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var enemies = new[]
        {
            new EnemyDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "enemy.antechamber.door-keeper",
                Name = "Gardien de Porte",
                DisplayName = "Gardien de Porte",
                Description = "Il garde l'entr\u00e9e de l'Antichambre. Il ne laisse passer personne.",
                Version = CatalogAntechamberFixVersion,
                Status = "Active",
                Archetype = "Guard",
                Family = "Antechamber",
                Rank = "Common",
                Role = "Tank",
                BaseDifficulty = 5,
                EncounterWeight = 1,
                MinRiskLevel = 1,
                MaxRiskLevel = 5,
                MinDepth = 1,
                MaxDepth = 5,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Antechamber\"]",
                TagsJson = "[\"antechamber\",\"guard\",\"door\"]",
                SkillKeysJson = "[\"skill.basic.shield\",\"skill.basic.strike\",\"skill.basic.taunt\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new EnemyDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = "enemy.antechamber.last-refusal",
                Name = "Dernier Refus",
                DisplayName = "Dernier Refus",
                Description = "Le dernier obstacle avant le Final. Il ne c\u00e9dera pas.",
                Version = CatalogAntechamberFixVersion,
                Status = "Active",
                Archetype = "Bruiser",
                Family = "Antechamber",
                Rank = "Common",
                Role = "DPS",
                BaseDifficulty = 5,
                EncounterWeight = 1,
                MinRiskLevel = 1,
                MaxRiskLevel = 5,
                MinDepth = 1,
                MaxDepth = 5,
                BaseWeight = 1,
                CompatibleRoomTypesJson = "[\"Antechamber\"]",
                TagsJson = "[\"antechamber\",\"bruiser\",\"final-stand\"]",
                SkillKeysJson = "[\"skill.basic.strike\",\"skill.basic.charge\",\"skill.basic.enrage\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var enemy in enemies)
        {
            var existing = await _context.EnemyDefinitions
                .FirstOrDefaultAsync(e => e.Key == enemy.Key, cancellationToken);
            if (existing is null)
            {
                _context.EnemyDefinitions.Add(enemy);
            }
        }
    }

    private async Task CleanupInvalidEnumEntitiesAsync(CancellationToken cancellationToken)
    {
        var invalidEnemyElements = new[] { "Nature", "Chaos", "Void" };
        var badEnemies = await _context.EnemyTemplates
            .Where(e => invalidEnemyElements.Contains(e.Element))
            .ToListAsync(cancellationToken);
        if (badEnemies.Count > 0)
        {
            _context.EnemyTemplates.RemoveRange(badEnemies);
        }

        var invalidEventOutcomes = new[] { "RoomBossEncounterStarted", "RareCombatStarted" };
        var badEvents = await _context.EventTemplates
            .Where(e => invalidEventOutcomes.Contains(e.DefaultOutcomeKind))
            .ToListAsync(cancellationToken);
        if (badEvents.Count > 0)
        {
            _context.EventTemplates.RemoveRange(badEvents);
        }

        if (badEnemies.Count > 0 || badEvents.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedEnemyTemplatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        await AddEnemyTemplateAsync("enemy-shadow-wolf", "Loup d\u2019Ombre", "Une entité née dans un couloir du Palais.", "Trauma", "Shadow", 120, 18, 8, 14, 10, 5, 25, 12, now, cancellationToken);
        await AddEnemyTemplateAsync("enemy-shadow-v1", "Ombre du Palais", "Une ombre du Palais bloque le chemin.", "Trauma", "Shadow", 80, 14, 6, 12, 6, 3, 20, 8, now, cancellationToken);
        await AddEnemyTemplateAsync("enemy-rare-v1", "Écho rare", "Une apparition rare du Palais.", "Memory", "Shadow", 100, 18, 8, 14, 8, 5, 35, 16, now, cancellationToken);
        await AddEnemyTemplateAsync("boss.threshold.warden-v1", "Warden of the Threshold", "The first sentinel guarding the entrance to the Memory Palace.", "Boss", "Neutral", 160, 24, 10, 10, 12, 8, 75, 30, now, cancellationToken);
        await AddEnemyTemplateAsync("boss.forest.rootbound-memory-v1", "Rootbound Memory", "An ancient entity whose roots dig deep into forgotten epochs.", "Boss", "Memory", 180, 22, 12, 8, 14, 10, 85, 35, now, cancellationToken);
        await AddEnemyTemplateAsync("boss.rupture.fractured-echo-v1", "Fractured Echo", "A shattered remnant of a once-coherent thought.", "Boss", "Rupture", 170, 28, 8, 14, 8, 8, 90, 40, now, cancellationToken);
        await AddEnemyTemplateAsync("boss.silence.mute-herald-v1", "Mute Herald", "A silent messenger whose presence absorbs all sound.", "Boss", "Silence", 190, 24, 14, 8, 16, 12, 95, 45, now, cancellationToken);
        await AddEnemyTemplateAsync("boss.antechamber.last-door-v1", "The Last Door", "The final barrier before the deepest memories.", "Boss", "Neutral", 220, 30, 16, 10, 18, 14, 120, 55, now, cancellationToken);
        await AddEnemyTemplateAsync("boss.memory.archivist-v1", "Archivist of Lost Moments", "The keeper of forgotten memories.", "Boss", "Memory", 200, 26, 12, 12, 14, 16, 105, 50, now, cancellationToken);
        await AddEnemyTemplateAsync("boss.final.himlit-v1", "Himlit", "The final entity at the heart of the Memory Palace.", "Boss", "Neutral", 260, 34, 18, 12, 20, 18, 200, 80, now, cancellationToken);
    }

    private async Task SeedSkillTemplatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        await AddOrUpdateSkillTemplateAsync("skill-shadow-bite", "Morsure d\u2019Ombre", "Une attaque de rupture silencieuse.", "Shadow", "Damage", "SingleEnemy", 3, 1, 35, 0, now, cancellationToken);
        await AddOrUpdateSkillTemplateAsync("skill-memory-mend", "Suture de Mémoire", "Restaure une partie de soi.", "Memory", "Heal", "SingleAlly", 4, 1, 0, 25, now, cancellationToken);
    }

    private async Task AddOrUpdateSkillTemplateAsync(
        string key,
        string name,
        string description,
        string element,
        string effectType,
        string targetType,
        int manaCost,
        int chargeCost,
        int basePower,
        int healPower,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _context.SkillTemplates
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (existing is not null)
        {
            existing.Name = name;
            existing.Description = description;
            existing.Element = element;
            existing.EffectType = effectType;
            existing.TargetType = targetType;
            existing.ManaCost = manaCost;
            existing.ChargeCost = chargeCost;
            existing.BasePower = basePower;
            existing.HealPower = healPower;
            existing.UpdatedAtUtc = now;
            return;
        }

        _context.SkillTemplates.Add(new SkillTemplateEntity
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            Description = description,
            Version = "catalog-0.1.0",
            Status = "Active",
            Element = element,
            EffectType = effectType,
            TargetType = targetType,
            ManaCost = manaCost,
            ChargeCost = chargeCost,
            BasePower = basePower,
            HealPower = healPower,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private async Task SeedEventTemplatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        await AddEventTemplateAsync("event-memory-threshold-v1", "Mémoire du seuil", "Une mémoire apparaît à l'entrée du Palais.", "Memory", "TomePageUnlocked", 5, 20, true, "[\"memory\",\"threshold\",\"elise\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-law-silence-v1", "Écho du Silence", "Une Loi du Palais se manifeste dans la pièce.", "Law", "PalaceLawApplied", 10, 35, true, "[\"law\",\"silence\",\"palace\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-combat-shadow-v1", "Combat d'ombre", "Une ombre du Palais bloque le chemin.", "Combat", "CombatStarted", 15, 45, false, "[\"combat\",\"shadow\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-rare-encounter-v1", "Rencontre rare", "Une présence inhabituelle traverse le Palais.", "Rare", "RareEventResolved", 20, 60, false, "[\"rare\",\"combat\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-boss.threshold.warden-v1", "Warden of the Threshold", "Le gardien du seuil se manifeste.", "RoomBoss", "RoomBossStarted", 40, 90, false, "[\"boss\",\"threshold\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-boss.forest.rootbound-memory-v1", "Rootbound Memory", "La mémoire enracinée ferme le passage.", "RoomBoss", "RoomBossStarted", 40, 90, false, "[\"boss\",\"forest\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-boss.rupture.fractured-echo-v1", "Fractured Echo", "Un écho fracturé rompt le silence.", "RoomBoss", "RoomBossStarted", 40, 90, false, "[\"boss\",\"rupture\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-boss.silence.mute-herald-v1", "Mute Herald", "Le héraut muet impose sa loi.", "RoomBoss", "RoomBossStarted", 40, 90, false, "[\"boss\",\"silence\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-boss.antechamber.last-door-v1", "The Last Door", "La dernière porte refuse de s'ouvrir.", "RoomBoss", "RoomBossStarted", 40, 90, false, "[\"boss\",\"antechamber\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-boss.memory.archivist-v1", "Archivist of Lost Moments", "L'archiviste réclame les souvenirs perdus.", "RoomBoss", "RoomBossStarted", 40, 90, false, "[\"boss\",\"memory\"]", now, cancellationToken);
        await AddEventTemplateAsync("event-boss.final.himlit-v1", "Himlit", "Le coeur du Palais répond enfin.", "RoomBoss", "RoomBossStarted", 80, 100, false, "[\"boss\",\"final\"]", now, cancellationToken);
    }

    private async Task AddEnemyTemplateAsync(
        string key,
        string name,
        string description,
        string archetype,
        string element,
        int maxHealth,
        int strength,
        int intelligence,
        int speed,
        int physicalResistance,
        int magicalResistance,
        int experienceReward,
        int goldReward,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _context.EnemyTemplates
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        if (existing is not null)
        {
            existing.Name = name;
            existing.Description = description;
            existing.Archetype = archetype;
            existing.Element = element;
            existing.MaxHealth = maxHealth;
            existing.Strength = strength;
            existing.Intelligence = intelligence;
            existing.Speed = speed;
            existing.PhysicalResistance = physicalResistance;
            existing.MagicalResistance = magicalResistance;
            existing.ExperienceReward = experienceReward;
            existing.GoldReward = goldReward;
            existing.UpdatedAtUtc = now;
            return;
        }

        _context.EnemyTemplates.Add(new EnemyTemplateEntity
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            Description = description,
            Version = "catalog-0.1.0",
            Status = "Active",
            Archetype = archetype,
            Element = element,
            MaxHealth = maxHealth,
            Strength = strength,
            Intelligence = intelligence,
            Speed = speed,
            PhysicalResistance = physicalResistance,
            MagicalResistance = magicalResistance,
            ExperienceReward = experienceReward,
            GoldReward = goldReward,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private async Task AddEventTemplateAsync(
        string key,
        string name,
        string description,
        string type,
        string defaultOutcomeKind,
        int minRiskLevel,
        int maxRiskLevel,
        bool requiresPlayerChoice,
        string narrativeTagsJson,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _context.EventTemplates
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        if (existing is not null)
        {
            existing.Name = name;
            existing.Description = description;
            existing.Type = type;
            existing.DefaultOutcomeKind = defaultOutcomeKind;
            existing.MinRiskLevel = minRiskLevel;
            existing.MaxRiskLevel = maxRiskLevel;
            existing.RequiresPlayerChoice = requiresPlayerChoice;
            existing.NarrativeTagsJson = narrativeTagsJson;
            existing.UpdatedAtUtc = now;
            return;
        }

        _context.EventTemplates.Add(new EventTemplateEntity
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            Description = description,
            Version = "event-1.0.0",
            Status = "Active",
            Type = type,
            DefaultOutcomeKind = defaultOutcomeKind,
            MinRiskLevel = minRiskLevel,
            MaxRiskLevel = maxRiskLevel,
            RequiresPlayerChoice = requiresPlayerChoice,
            NarrativeTagsJson = narrativeTagsJson,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private async Task SeedRoomBossCatalogAsync(DateTime now, CancellationToken cancellationToken)
    {
        // Remove old-style duplicates (from DataModelSeed alpha-0.8.1) that conflict by RoomType.
        // This runs every time to clean up existing databases, not just on first seed.
        var oldRoomTypes = new[] { "Threshold", "Forest", "Rupture", "Silence", "Antechamber", "Memory", "Final" };
        var oldEntries = await _context.RoomBossDefinitions
            .Where(r => oldRoomTypes.Contains(r.RoomType) && !r.Key.StartsWith("boss."))
            .ToListAsync(cancellationToken);
        if (oldEntries.Count > 0)
        {
            _context.RoomBossDefinitions.RemoveRange(oldEntries);
            await _context.SaveChangesAsync(cancellationToken);
        }

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
