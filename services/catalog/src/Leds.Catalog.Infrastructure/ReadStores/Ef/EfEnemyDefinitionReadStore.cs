using System.Text.Json;
using Leds.Catalog.Application.Enemies.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Enemies;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfEnemyDefinitionReadStore : IEnemyDefinitionReadStore
{
    private readonly CatalogDbContext _context;

    public EfEnemyDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IEnemyDefinition>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.EnemyDefinitions
            .Include(e => e.StatBlock)
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IEnemyDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.EnemyDefinitions
            .Include(e => e.StatBlock)
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyCollection<IEnemyDefinition>> ListByRoomTypeAsync(string roomType, CancellationToken cancellationToken)
    {
        var entities = await _context.EnemyDefinitions
            .Include(e => e.StatBlock)
            .Where(e => e.Status == "Active" && e.CompatibleRoomTypesJson.Contains(roomType))
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IReadOnlyCollection<IEnemyDefinition>> ListCompatibleAsync(string roomType, int riskLevel, CancellationToken cancellationToken)
    {
        var entities = await _context.EnemyDefinitions
            .Include(e => e.StatBlock)
            .Where(e => e.Status == "Active"
                && e.CompatibleRoomTypesJson.Contains(roomType)
                && e.MinRiskLevel <= riskLevel
                && e.MaxRiskLevel >= riskLevel)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    private static IEnemyDefinition MapToDomain(EnemyDefinitionEntity entity)
    {
        var compatibleRoomTypes = JsonSerializer.Deserialize<List<string>>(entity.CompatibleRoomTypesJson) ?? [];
        var tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? [];
        var skillKeys = JsonSerializer.Deserialize<List<string>>(entity.SkillKeysJson) ?? [];
        var boundRoomKeys = JsonSerializer.Deserialize<List<string>>(entity.BoundRoomKeysJson) ?? [];

        return EnemyDefinition.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            entity.Archetype,
            entity.BaseDifficulty,
            entity.MinRiskLevel,
            entity.MaxRiskLevel,
            compatibleRoomTypes,
            tags,
            skillKeys,
            Enum.Parse<CatalogContentStatus>(entity.Status),
            attackPower: entity.StatBlock?.AttackPower ?? 0,
            defense: entity.StatBlock?.Defense ?? 0,
            speed: entity.StatBlock?.Speed ?? 10,
            focus: entity.StatBlock?.Focus ?? 0,
            initiative: entity.StatBlock?.Initiative ?? 0,
            mana: entity.StatBlock?.Mana ?? 0,
            magicAttack: entity.StatBlock?.MagicAttack ?? 0,
            magicDefense: entity.StatBlock?.MagicDefense ?? 0,
            menace: entity.MenaceLevel,
            rarity: entity.Rarity,
            registre: entity.Registre,
            boundRoomKeys: boundRoomKeys);
    }
}
