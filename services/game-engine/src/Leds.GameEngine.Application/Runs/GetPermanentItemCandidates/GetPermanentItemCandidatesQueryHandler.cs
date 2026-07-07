using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GetPermanentItemCandidates;

public sealed class GetPermanentItemCandidatesQueryHandler
    : IRequestHandler<GetPermanentItemCandidatesQuery, GetPermanentItemCandidatesResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ICatalogContentGateway _catalogGateway;

    public GetPermanentItemCandidatesQueryHandler(
        IRunRepository runRepository,
        ICatalogContentGateway catalogGateway)
    {
        _runRepository = runRepository;
        _catalogGateway = catalogGateway;
    }

    public async Task<GetPermanentItemCandidatesResponse> Handle(
        GetPermanentItemCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var distinctKeys = run.RunItems
            .Select(item => item.DefinitionKey)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var candidates = new List<PermanentItemCandidateDto>();

        foreach (var key in distinctKeys)
        {
            var result = await _catalogGateway.GetItemDefinitionByKeyAsync(key, cancellationToken);
            if (result.IsSuccess && result.Value.IsPermanentEligible)
            {
                candidates.Add(new PermanentItemCandidateDto(
                    result.Value.Key,
                    result.Value.DisplayName,
                    result.Value.Description,
                    result.Value.Rarity));
            }
        }

        return new GetPermanentItemCandidatesResponse(run.Id.Value, candidates);
    }
}
