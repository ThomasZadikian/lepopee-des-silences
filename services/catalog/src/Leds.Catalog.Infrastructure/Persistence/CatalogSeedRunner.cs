using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed class CatalogSeedRunner
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<CatalogSeedRunner> _logger;

    public CatalogSeedRunner(CatalogDbContext context, ILogger<CatalogSeedRunner> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ApplyBaseSeedAsync(CancellationToken cancellationToken = default)
    {
        const string seedKey = "base-catalog";
        const string version = "alpha-0.5.5";

        var alreadyApplied = await _context.CatalogSeedVersions
            .AnyAsync(v => v.SeedKey == seedKey && v.Version == version, cancellationToken);

        if (alreadyApplied)
        {
            _logger.LogInformation("Seed {SeedKey} version {Version} already applied. Skipping.", seedKey, version);
            return;
        }

        _logger.LogInformation("Applying seed {SeedKey} version {Version}...", seedKey, version);

        await SeedSkillDefinitionsAsync(cancellationToken);
        await SeedEnemyDefinitionsAsync(cancellationToken);
        await SeedItemDefinitionsAsync(cancellationToken);
        await SeedPalaceLawDefinitionsAsync(cancellationToken);

        _context.CatalogSeedVersions.Add(new CatalogSeedVersionEntity
        {
            Id = Guid.NewGuid(),
            SeedKey = seedKey,
            Version = version,
            AppliedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seed {SeedKey} version {Version} applied successfully.", seedKey, version);
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
                Description = "Une attaque basique.",
                Version = "alpha-0.5.5",
                Status = "Active",
                SkillType = "Damage",
                TargetingType = "SingleEnemy",
                EffectType = "Damage",
                ManaCost = 0,
                ChargeCost = 0,
                BasePower = 10,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SkillDefinitionEntity
            {
                Id = Guid.Parse("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"),
                Key = "skill.basic.guard",
                Name = "Garde",
                Description = "Une défense basique.",
                Version = "alpha-0.5.5",
                Status = "Active",
                SkillType = "Defense",
                TargetingType = "Self",
                EffectType = "Guard",
                ManaCost = 0,
                ChargeCost = 0,
                BasePower = 5,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var skill in skills)
        {
            var exists = await _context.SkillDefinitions
                .AnyAsync(s => s.Key == skill.Key, cancellationToken);

            if (!exists)
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
                Description = "Une créature du seuil.",
                Version = "alpha-0.5.5",
                Status = "Active",
                Archetype = "Trauma",
                BaseDifficulty = 1,
                MinRiskLevel = 1,
                MaxRiskLevel = 30,
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
                Description = "Une entité fragmentée.",
                Version = "alpha-0.5.5",
                Status = "Active",
                Archetype = "Shadow",
                BaseDifficulty = 2,
                MinRiskLevel = 20,
                MaxRiskLevel = 60,
                CompatibleRoomTypesJson = "[\"Threshold\",\"Forest\",\"Rupture\"]",
                TagsJson = "[\"threshold\",\"elite\"]",
                SkillKeysJson = "[\"skill.basic.strike\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var enemy in enemies)
        {
            var exists = await _context.EnemyDefinitions
                .AnyAsync(e => e.Key == enemy.Key, cancellationToken);

            if (!exists)
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
                Description = "Restaure un peu de vitalité.",
                Version = "alpha-0.5.5",
                Status = "Active",
                Category = "Consumable",
                Rarity = "Common",
                Duration = "RunOnly",
                EffectValue = 15,
                Price = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var item in items)
        {
            var exists = await _context.ItemDefinitions
                .AnyAsync(i => i.Key == item.Key, cancellationToken);

            if (!exists)
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
                Description = "Le silence affecte la génération.",
                Version = "alpha-0.5.5",
                Status = "Active",
                Visibility = "Visible",
                Priority = 1,
                ImpactDomainsJson = "[\"Generation\"]",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        foreach (var law in laws)
        {
            var exists = await _context.PalaceLawDefinitions
                .AnyAsync(l => l.Key == law.Key, cancellationToken);

            if (!exists)
            {
                _context.PalaceLawDefinitions.Add(law);
            }
        }
    }
}
