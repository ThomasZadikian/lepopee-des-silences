using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Application.Protocol;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.InteractWithRoomNpc;

public sealed class InteractWithRoomNpcCommandHandler
    : IRequestHandler<InteractWithRoomNpcCommand, InteractWithRoomNpcResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly LocalRuleProtocolEvaluator _localRuleProtocolEvaluator;
    private readonly ICatalogContentGateway _catalogContentGateway;

    public InteractWithRoomNpcCommandHandler(
        IRunRepository runRepository,
        LocalRuleProtocolEvaluator localRuleProtocolEvaluator,
        ICatalogContentGateway catalogContentGateway)
    {
        _runRepository = runRepository;
        _localRuleProtocolEvaluator = localRuleProtocolEvaluator;
        _catalogContentGateway = catalogContentGateway;
    }

    public async Task<InteractWithRoomNpcResponse> Handle(
        InteractWithRoomNpcCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var actor = run.InteractWithRoomNpc(new RoomNpcId(request.RoomNpcId));
        var notices = _localRuleProtocolEvaluator.EvaluateNpcInteraction(
            run.CurrentRoom,
            actor.CatalogNpcKey);
        var definition = (await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken))
            .FirstOrDefault(npc => string.Equals(
                npc.Key,
                actor.CatalogNpcKey,
                StringComparison.OrdinalIgnoreCase));
        var relationship = run.GetNpcRelationship(actor.CatalogNpcKey);
        var dialogue = definition is null || relationship is null
            ? null
            : NpcDialogueViewFactory.Build(definition, relationship, run);
        if (dialogue is null || !dialogue.EncounterActive)
        {
            run.EndNpcEncounter();
        }
        await _runRepository.UpdateAsync(run, cancellationToken);

        return new InteractWithRoomNpcResponse(
            RunDto.FromDomain(run),
            RoomNpcDto.FromDomain(actor),
            notices.Select(notice => new RoomNpcInteractionNoticeDto(
                notice.RuleKey,
                notice.RuleName,
                notice.Result.Outcome.ToString(),
                notice.Result.Message)).ToArray(),
            dialogue);
    }
}
