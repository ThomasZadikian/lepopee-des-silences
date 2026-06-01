using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public sealed class ChooseCurrentEventOptionCommandHandler
    : IRequestHandler<ChooseCurrentEventOptionCommand, ChooseCurrentEventOptionResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ICurrentEventChoiceResolverDispatcher _choiceResolverDispatcher;

    public ChooseCurrentEventOptionCommandHandler(
        IRunRepository runRepository,
        ICurrentEventChoiceResolverDispatcher choiceResolverDispatcher)
    {
        _runRepository = runRepository;
        _choiceResolverDispatcher = choiceResolverDispatcher;
    }

    public async Task<ChooseCurrentEventOptionResponse> Handle(
        ChooseCurrentEventOptionCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        var room = run.CurrentRoom;

        if (room.State != RoomState.NodeResolved)
        {
            throw new DomainException(
                "Current event must be resolved before choosing an event option.");
        }

        var resolvedNode = room.Nodes.SingleOrDefault(node =>
            node.NodeDepth == room.CurrentNodeDepth &&
            node.State == NodeState.Resolved);

        if (resolvedNode is null)
        {
            throw new DomainException(
                "No resolved current event is waiting for a player choice.");
        }

        var context = new CurrentEventChoiceResolutionContext(
            run,
            room,
            resolvedNode,
            request.ChoiceId);

        var result = _choiceResolverDispatcher.Resolve(context);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ChooseCurrentEventOptionResponse(
            RunDto.FromDomain(run),
            ChosenEventOptionResultDto.FromResult(result));
    }
}