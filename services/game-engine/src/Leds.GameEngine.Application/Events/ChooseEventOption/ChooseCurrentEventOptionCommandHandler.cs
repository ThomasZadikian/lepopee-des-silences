using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.Dtos;
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
    private readonly ICatalogContentGateway _catalogContentGateway;

    public ChooseCurrentEventOptionCommandHandler(
        IRunRepository runRepository,
        ICurrentEventChoiceResolverDispatcher choiceResolverDispatcher,
        ICatalogContentGateway catalogContentGateway)
    {
        _runRepository = runRepository;
        _choiceResolverDispatcher = choiceResolverDispatcher;
        _catalogContentGateway = catalogContentGateway;
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
            node.Row == room.CurrentNodeDepth &&
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

        var result = await _choiceResolverDispatcher.ResolveAsync(context, cancellationToken);

        // Only finalise the node when the encounter is truly over. A multi-step NPC
        // dialogue keeps the node "resolved" (awaiting choice) until the graph ends.
        if (result.Accepted && result.EncounterCompleted)
        {
            resolvedNode.ChooseEventOption(result.ChoiceId);
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        // If the NPC dialogue continues, surface the next node (lines + eligible choices).
        var npcDialogue = await BuildOngoingNpcDialogueAsync(run, cancellationToken);

        return new ChooseCurrentEventOptionResponse(
            RunDto.FromDomain(run),
            ChosenEventOptionResultDto.FromResult(result),
            npcDialogue);
    }

    private async Task<NpcDialogueViewDto?> BuildOngoingNpcDialogueAsync(
        Run run,
        CancellationToken cancellationToken)
    {
        var activeNpcKey = run.ActiveNpcKey;
        if (string.IsNullOrWhiteSpace(activeNpcKey))
        {
            return null;
        }

        var relationship = run.GetNpcRelationship(activeNpcKey);
        if (relationship is null)
        {
            return null;
        }

        var npcs = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var npc = npcs.FirstOrDefault(n => string.Equals(n.Key, activeNpcKey, StringComparison.OrdinalIgnoreCase));

        return npc is null ? null : NpcDialogueViewFactory.Build(npc, relationship, run);
    }
}