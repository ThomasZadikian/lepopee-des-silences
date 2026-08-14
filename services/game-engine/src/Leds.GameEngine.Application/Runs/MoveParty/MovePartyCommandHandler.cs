using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Protocol;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.MoveParty;

public sealed class MovePartyCommandHandler : IRequestHandler<MovePartyCommand, MovePartyResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly LocalRuleProtocolEvaluator _localRuleProtocolEvaluator;

    public MovePartyCommandHandler(
        IRunRepository runRepository,
        LocalRuleProtocolEvaluator localRuleProtocolEvaluator)
    {
        _runRepository = runRepository;
        _localRuleProtocolEvaluator = localRuleProtocolEvaluator;
    }

    public async Task<MovePartyResponse> Handle(
        MovePartyCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var previousX = run.CurrentRoom.Grid.PartyX;
        var previousY = run.CurrentRoom.Grid.PartyY;

        var pickups = run.MoveParty(request.TargetX, request.TargetY);

        var localRuleOutcomes = _localRuleProtocolEvaluator.EvaluateZoneCrossings(
            run.CurrentRoom, previousX, previousY, pickups.TraversedCells);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new MovePartyResponse(
            RunDto.FromDomain(run),
            pickups.CollectedItemIds.Select(id => id.Value).ToArray(),
            pickups.BlockedItemIds.Select(id => id.Value).ToArray(),
            localRuleOutcomes
                .Select(o => new LocalRuleNoticeDto(o.RuleKey, o.RuleName, o.Result.Outcome.ToString(), o.Result.Message))
                .ToArray());
    }
}
