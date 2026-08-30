using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.RemovePalaceLaw;

public sealed class RemovePalaceLawCommandHandler
    : IRequestHandler<RemovePalaceLawCommand, RemovePalaceLawResponse>
{
    private readonly IRunRepository _runRepository;

    public RemovePalaceLawCommandHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<RemovePalaceLawResponse> Handle(
        RemovePalaceLawCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        run.RemovePalaceLaw(request.LawKey);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new RemovePalaceLawResponse(RunDto.FromDomain(run));
    }
}
