using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.Application.Selection.Ports;

public interface ISelectionDecisionRepository
{
    Task AddAsync(SelectionDecision decision, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyCollection<SelectionDecision> decisions, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SelectionDecision>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);
}
