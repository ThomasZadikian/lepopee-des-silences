using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseCaliceInfini;

public sealed class UseCaliceInfiniCommandHandler
    : IRequestHandler<UseCaliceInfiniCommand, UseCaliceInfiniResponse>
{
    private readonly IRunRepository _runRepository;

    public UseCaliceInfiniCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<UseCaliceInfiniResponse> Handle(
        UseCaliceInfiniCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        run.UseCaliceInfini(request.TargetCombatantId);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new UseCaliceInfiniResponse(RunDto.FromDomain(run));
    }
}
