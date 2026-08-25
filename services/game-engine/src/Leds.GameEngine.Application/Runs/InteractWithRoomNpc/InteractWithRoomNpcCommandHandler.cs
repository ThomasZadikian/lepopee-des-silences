using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
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

    public InteractWithRoomNpcCommandHandler(
        IRunRepository runRepository,
        LocalRuleProtocolEvaluator localRuleProtocolEvaluator)
    {
        _runRepository = runRepository;
        _localRuleProtocolEvaluator = localRuleProtocolEvaluator;
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
        await _runRepository.UpdateAsync(run, cancellationToken);

        return new InteractWithRoomNpcResponse(
            RunDto.FromDomain(run),
            RoomNpcDto.FromDomain(actor),
            notices.Select(notice => new RoomNpcInteractionNoticeDto(
                notice.RuleKey,
                notice.RuleName,
                notice.Result.Outcome.ToString(),
                notice.Result.Message)).ToArray());
    }
}
