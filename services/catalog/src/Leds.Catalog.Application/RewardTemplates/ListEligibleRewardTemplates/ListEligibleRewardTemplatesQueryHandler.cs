using Leds.Catalog.Application.RewardTemplates.Ports;
using MediatR;

namespace Leds.Catalog.Application.RewardTemplates.ListEligibleRewardTemplates;

public sealed class ListEligibleRewardTemplatesQueryHandler
    : IRequestHandler<ListEligibleRewardTemplatesQuery, ListEligibleRewardTemplatesResponse>
{
    private readonly IRewardTemplateReadStore _readStore;

    public ListEligibleRewardTemplatesQueryHandler(IRewardTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListEligibleRewardTemplatesResponse> Handle(
        ListEligibleRewardTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _readStore.ListEligibleDtosAsync(
            new RewardTemplateEligibilityQueryContext(
                request.SourceType,
                request.Depth,
                request.CombatTier,
                request.DifficultyMultiplier,
                request.RewardPowerMultiplier),
            cancellationToken);

        return new ListEligibleRewardTemplatesResponse(templates.ToArray());
    }
}
