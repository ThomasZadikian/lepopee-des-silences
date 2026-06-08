using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Interlude.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Interlude.GetInterlude;

public sealed class GetInterludeQueryHandler
    : IRequestHandler<GetInterludeQuery, GetInterludeResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IInterludeNodeProvider _nodeProvider;

    public GetInterludeQueryHandler(
        IRunRepository runRepository,
        IInterludeNodeProvider nodeProvider)
    {
        _runRepository = runRepository;
        _nodeProvider = nodeProvider;
    }

    public async Task<GetInterludeResponse> Handle(
        GetInterludeQuery request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status != RunStatus.Interlude)
        {
            throw new DomainException(
                "Cannot retrieve interlude: run is not in Interlude state.");
        }

        var domainNodes = _nodeProvider.GetNodes(run);
        var nodeDtos = domainNodes.Select(InterludeNodeDto.FromDomain).ToArray();
        var actions = InterludeDto.BuildDefaultActions();

        return new GetInterludeResponse(
            InterludeDto.FromDomain(run, nodeDtos, actions));
    }
}
