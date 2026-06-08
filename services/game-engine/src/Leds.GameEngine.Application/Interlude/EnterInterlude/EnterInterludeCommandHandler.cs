using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Interlude.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Interlude.EnterInterlude;

public sealed class EnterInterludeCommandHandler
    : IRequestHandler<EnterInterludeCommand, EnterInterludeResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IInterludeNodeProvider _nodeProvider;

    public EnterInterludeCommandHandler(
        IRunRepository runRepository,
        IInterludeNodeProvider nodeProvider)
    {
        _runRepository = runRepository;
        _nodeProvider = nodeProvider;
    }

    public async Task<EnterInterludeResponse> Handle(
        EnterInterludeCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        // Domain method validates RoomResolved, no active combat, no pending reward
        run.EnterInterlude();

        await _runRepository.UpdateAsync(run, cancellationToken);

        var domainNodes = _nodeProvider.GetNodes(run);
        var nodeDtos = domainNodes.Select(InterludeNodeDto.FromDomain).ToArray();
        var actions = InterludeDto.BuildDefaultActions();

        return new EnterInterludeResponse(
            InterludeDto.FromDomain(run, nodeDtos, actions));
    }
}
