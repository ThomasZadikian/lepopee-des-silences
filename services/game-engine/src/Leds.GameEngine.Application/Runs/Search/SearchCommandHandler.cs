using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.Search;

public sealed class SearchCommandHandler : IRequestHandler<SearchCommand, SearchResponse>
{
    private readonly IRunRepository _runRepository;

    public SearchCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<SearchResponse> Handle(
        SearchCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        run.SearchAtPartyPosition();

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SearchResponse(RunDto.FromDomain(run));
    }
}
