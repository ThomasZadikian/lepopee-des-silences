using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

public sealed class StartRunCommandHandler : IRequestHandler<StartRunCommand, StartRunResponse>
{
    private readonly IRunGenerator _runGenerator;
    private readonly IRunRepository _runRepository;
    private readonly IClock _clock;

    public StartRunCommandHandler(
        IRunGenerator runGenerator,
        IRunRepository runRepository,
        IClock clock)
    {
        _runGenerator = runGenerator;
        _runRepository = runRepository;
        _clock = clock;
    }

    public async Task<StartRunResponse> Handle(
        StartRunCommand request,
        CancellationToken cancellationToken)
    {
        var seed = _runGenerator.GenerateSeed();
        var initialRoom = await _runGenerator.GenerateInitialRoomAsync(seed, cancellationToken);

        var run = Run.StartNew(
            request.PlayerId,
            seed,
            _runGenerator.GeneratorVersion,
            _runGenerator.MarkovMatrixVersion,
            initialRoom,
            _clock.UtcNow);

        await _runRepository.AddAsync(run, cancellationToken);

        return new StartRunResponse(RunDto.FromDomain(run));
    }
}