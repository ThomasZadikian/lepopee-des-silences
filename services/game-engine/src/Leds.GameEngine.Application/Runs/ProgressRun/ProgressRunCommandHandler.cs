using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ProgressRun;

public sealed class ProgressRunCommandHandler
    : IRequestHandler<ProgressRunCommand, ProgressRunResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ICurrentEventChoiceRequirementResolver _choiceRequirementResolver;

    public ProgressRunCommandHandler(
        IRunRepository runRepository,
        ICurrentEventChoiceRequirementResolver choiceRequirementResolver)
    {
        _runRepository = runRepository;
        _choiceRequirementResolver = choiceRequirementResolver;
    }

    public async Task<ProgressRunResponse> Handle(
        ProgressRunCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.HasActiveCombat)
        {
            throw new DomainException(
                "Cannot progress while a combat is active.");
        }

        if (run.HasPendingRewardOffer)
        {
            throw new DomainException(
                "Cannot progress while a pending reward offer requires selection.");
        }

        if (run.Status == RunStatus.RoomResolved)
        {
            throw new DomainException(
                "Cannot progress: current room is cleared. Await the Interlude or MoveToNextRoom transition.");
        }

        if (run.Status == RunStatus.Interlude)
        {
            throw new DomainException(
                "Cannot progress: run is in Interlude. Navigate the interlude hub or enter the next room.");
        }

        var room = run.CurrentRoom;

        var resolvedNode = room.Nodes.SingleOrDefault(node =>
            node.Row == room.CurrentNodeDepth &&
            node.State == NodeState.Resolved);

        if (resolvedNode is not null &&
            _choiceRequirementResolver.RequiresChoice(resolvedNode) &&
            !resolvedNode.HasChosenEventOption)
        {
            throw new DomainException(
                "Current event requires a player choice before progressing.");
        }

        run.ProgressCurrentRoom();

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new ProgressRunResponse(RunDto.FromDomain(run));
    }
}