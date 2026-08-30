using System.Text.Json;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.Infrastructure.Persistence.Repositories;

public sealed class EfCombatActionRecordRepository : ICombatActionRecordRepository
{
    private readonly GameEngineDbContext _dbContext;

    public EfCombatActionRecordRepository(GameEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(IReadOnlyCollection<CombatActionRecord> records, CancellationToken cancellationToken)
    {
        var entities = records.Select(ToEntity).ToArray();
        await _dbContext.CombatActionRecords.AddRangeAsync(entities, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CombatActionRecord>> GetByCombatIdAsync(Guid combatId, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.CombatActionRecords
            .Where(r => r.CombatId == combatId)
            .OrderBy(r => r.TurnNumber)
            .ThenBy(r => r.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    private static CombatActionRecordEntity ToEntity(CombatActionRecord record)
    {
        return new CombatActionRecordEntity
        {
            Id = record.Id,
            CombatId = record.CombatId,
            TurnNumber = record.TurnNumber,
            ActorId = record.ActorId,
            ActorSide = record.ActorSide,
            SkillKey = record.SkillKey,
            SkillDisplayName = record.SkillDisplayName,
            TargetIds = JsonSerializer.Serialize(record.TargetIds),
            SourceType = record.SourceType,
            SourceKey = record.SourceKey,
            RawDamage = record.RawDamage,
            MitigatedDamage = record.MitigatedDamage,
            VitalityDamage = record.VitalityDamage,
            GuardDamage = record.GuardDamage,
            GuardAbsorbed = record.GuardAbsorbed,
            GuardGained = record.GuardGained,
            DamageDealt = record.DamageDealt,
            DamageTaken = record.DamageTaken,
            HealingDone = record.HealingDone,
            HealingReceived = record.HealingReceived,
            EffectsApplied = JsonSerializer.Serialize(record.EffectsApplied),
            OccurredAtUtc = record.OccurredAtUtc
        };
    }

    private static CombatActionRecord ToDomain(CombatActionRecordEntity entity)
    {
        var targetIds = JsonSerializer.Deserialize<Guid[]>(entity.TargetIds) ?? [];
        var effectsApplied = JsonSerializer.Deserialize<string[]>(entity.EffectsApplied) ?? [];

        return CombatActionRecord.Rehydrate(
            entity.Id,
            entity.CombatId,
            entity.TurnNumber,
            entity.ActorId,
            entity.ActorSide,
            entity.SkillKey,
            entity.SkillDisplayName,
            targetIds,
            entity.SourceType,
            entity.SourceKey,
            entity.RawDamage,
            entity.MitigatedDamage,
            entity.VitalityDamage,
            entity.GuardDamage,
            entity.GuardAbsorbed,
            entity.GuardGained,
            entity.DamageDealt,
            entity.DamageTaken,
            entity.HealingDone,
            entity.HealingReceived,
            effectsApplied,
            entity.OccurredAtUtc);
    }
}
