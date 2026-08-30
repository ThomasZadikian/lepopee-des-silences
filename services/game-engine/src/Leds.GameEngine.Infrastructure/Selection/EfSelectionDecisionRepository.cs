using Leds.GameEngine.Application.Selection.Ports;
using Leds.GameEngine.Domain.Selection;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.Infrastructure.Selection;

public sealed class EfSelectionDecisionRepository : ISelectionDecisionRepository
{
    private readonly GameEngineDbContext _dbContext;

    public EfSelectionDecisionRepository(GameEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SelectionDecision decision, CancellationToken cancellationToken = default)
    {
        var entity = ToEntity(decision);
        _dbContext.SelectionDecisions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<SelectionDecision> decisions, CancellationToken cancellationToken = default)
    {
        var entities = decisions.Select(ToEntity).ToList();
        _dbContext.SelectionDecisions.AddRange(entities);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SelectionDecision>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.SelectionDecisions
            .Where(d => d.RunId == runId)
            .OrderBy(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    private static SelectionDecisionEntity ToEntity(SelectionDecision d) => new()
    {
        Id = d.Id,
        RunId = d.RunId,
        DecisionType = d.DecisionType,
        ContextKey = d.ContextKey,
        SelectedKey = d.SelectedKey,
        SelectionGroup = d.SelectionGroup,
        Seed = d.Seed,
        AlgorithmVersion = d.AlgorithmVersion,
        CreatedAtUtc = d.CreatedAtUtc,
        MatrixVersion = d.MatrixVersion,
        InfluenceSummaryKey = d.InfluenceSummaryKey
    };

    private static SelectionDecision ToDomain(SelectionDecisionEntity e) =>
        SelectionDecision.Rehydrate(
            e.Id, e.RunId, e.DecisionType, e.ContextKey,
            e.SelectedKey, e.SelectionGroup, e.Seed,
            e.AlgorithmVersion, e.CreatedAtUtc,
            e.MatrixVersion, e.InfluenceSummaryKey);
}
