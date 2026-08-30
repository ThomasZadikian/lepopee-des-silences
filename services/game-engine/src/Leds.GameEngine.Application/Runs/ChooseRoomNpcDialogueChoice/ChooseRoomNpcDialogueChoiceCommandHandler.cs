using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ChooseRoomNpcDialogueChoice;

public sealed class ChooseRoomNpcDialogueChoiceCommandHandler
    : IRequestHandler<ChooseRoomNpcDialogueChoiceCommand, ChooseCurrentEventOptionResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly INpcDialogueChoiceResolver _choiceResolver;
    private readonly ICatalogContentGateway _catalogContentGateway;

    public ChooseRoomNpcDialogueChoiceCommandHandler(
        IRunRepository runRepository,
        INpcDialogueChoiceResolver choiceResolver,
        ICatalogContentGateway catalogContentGateway)
    {
        _runRepository = runRepository;
        _choiceResolver = choiceResolver;
        _catalogContentGateway = catalogContentGateway;
    }

    public async Task<ChooseCurrentEventOptionResponse> Handle(
        ChooseRoomNpcDialogueChoiceCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var actor = run.CurrentRoom.RoomNpcs.FirstOrDefault(npc =>
            npc.Id == new RoomNpcId(request.RoomNpcId))
            ?? throw new DomainException("Room NPC does not belong to this room.");

        if (!string.Equals(run.ActiveNpcKey, actor.CatalogNpcKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("This room NPC has no active dialogue.");
        }

        var distance = Math.Abs(actor.X - run.CurrentRoom.Grid.PartyX)
            + Math.Abs(actor.Y - run.CurrentRoom.Grid.PartyY);
        if (distance > 1)
        {
            throw new DomainException("The party must remain next to a room NPC to continue dialogue.");
        }

        var result = await _choiceResolver.ResolveNpcDialogueChoiceAsync(
            run,
            null,
            request.ChoiceId,
            cancellationToken);

        await _runRepository.UpdateAsync(run, cancellationToken);

        var dialogue = await BuildOngoingDialogueAsync(run, cancellationToken);
        return new ChooseCurrentEventOptionResponse(
            RunDto.FromDomain(run),
            ChosenEventOptionResultDto.FromResult(result),
            dialogue);
    }

    private async Task<Leds.GameEngine.Application.Events.Dtos.NpcDialogueViewDto?> BuildOngoingDialogueAsync(
        Run run,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(run.ActiveNpcKey))
        {
            return null;
        }

        var relationship = run.GetNpcRelationship(run.ActiveNpcKey);
        if (relationship is null)
        {
            return null;
        }

        var definitions = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var definition = definitions.FirstOrDefault(npc => string.Equals(
            npc.Key,
            run.ActiveNpcKey,
            StringComparison.OrdinalIgnoreCase));

        return definition is null ? null : NpcDialogueViewFactory.Build(definition, relationship, run);
    }
}
