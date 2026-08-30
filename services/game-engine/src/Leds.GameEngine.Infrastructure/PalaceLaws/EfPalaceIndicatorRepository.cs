using Leds.GameEngine.Application.PalaceLaws.Ports;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.Infrastructure.PalaceLaws;

public sealed class EfPalaceIndicatorRepository : IPalaceIndicatorRepository
{
    private readonly GameEngineDbContext _dbContext;

    public EfPalaceIndicatorRepository(GameEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PalaceIndicator indicator, CancellationToken cancellationToken = default)
    {
        _dbContext.PalaceIndicators.Add(ToEntity(indicator));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PalaceIndicator>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.PalaceIndicators
            .Where(i => i.RunId == runId)
            .OrderBy(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    private static PalaceIndicatorEntity ToEntity(PalaceIndicator i) => new()
    {
        Id = i.Id, RunId = i.RunId, IndicatorKey = i.IndicatorKey, DisplayLabel = i.DisplayLabel,
        NarrativeText = i.NarrativeText, Intensity = i.Intensity, SourceDecisionId = i.SourceDecisionId,
        CreatedAtUtc = i.CreatedAtUtc, ExpiresAtUtc = i.ExpiresAtUtc
    };

    private static PalaceIndicator ToDomain(PalaceIndicatorEntity e) =>
        PalaceIndicator.Rehydrate(e.Id, e.RunId, e.IndicatorKey, e.DisplayLabel, e.NarrativeText,
            e.Intensity, e.SourceDecisionId, e.CreatedAtUtc, e.ExpiresAtUtc);
}
