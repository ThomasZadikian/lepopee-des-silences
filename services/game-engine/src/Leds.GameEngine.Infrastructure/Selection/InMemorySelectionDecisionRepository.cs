using Leds.GameEngine.Application.Selection.Ports;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.Infrastructure.Selection;

public sealed class InMemorySelectionDecisionRepository : ISelectionDecisionRepository
{
    private readonly List<SelectionDecision> _decisions = [];

    public Task AddAsync(SelectionDecision decision, CancellationToken cancellationToken = default)
    {
        _decisions.Add(decision);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IReadOnlyCollection<SelectionDecision> decisions, CancellationToken cancellationToken = default)
    {
        _decisions.AddRange(decisions);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SelectionDecision>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var results = _decisions
            .Where(d => d.RunId == runId)
            .OrderBy(d => d.CreatedAtUtc)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyCollection<SelectionDecision>>(results);
    }
}
