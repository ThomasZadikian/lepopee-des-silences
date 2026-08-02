using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.SetRoomRiskTier;

public sealed class SetRoomRiskTierCommandHandler
    : IRequestHandler<SetRoomRiskTierCommand, SetRoomRiskTierResponse>
{
    private readonly IRunRepository _runRepository;

    public SetRoomRiskTierCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<SetRoomRiskTierResponse> Handle(
        SetRoomRiskTierCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        run.SetRoomDesiredRiskTier(request.Tier);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SetRoomRiskTierResponse(RunDto.FromDomain(run));
    }
}
